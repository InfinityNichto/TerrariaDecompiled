using System.Buffers;
using System.Net.Http.HPack;
using System.Numerics;

namespace System.Net.Http.QPack;

internal sealed class QPackDecoder : IDisposable
{
	private enum State
	{
		RequiredInsertCount,
		RequiredInsertCountContinue,
		Base,
		BaseContinue,
		CompressedHeaders,
		HeaderFieldIndex,
		HeaderNameIndex,
		HeaderNameLength,
		HeaderNameLengthContinue,
		HeaderName,
		HeaderValueLength,
		HeaderValueLengthContinue,
		HeaderValue,
		DynamicTableSizeUpdate,
		PostBaseIndex,
		LiteralHeaderFieldWithNameReference,
		HeaderNameIndexPostBase
	}

	private readonly int _maxHeadersLength;

	private State _state;

	private byte[] _stringOctets;

	private byte[] _headerNameOctets;

	private byte[] _headerValueOctets;

	private bool _huffman;

	private int? _index;

	private byte[] _headerName;

	private int _headerNameLength;

	private int _headerValueLength;

	private int _stringLength;

	private int _stringIndex;

	private IntegerDecoder _integerDecoder;

	private static ArrayPool<byte> Pool => ArrayPool<byte>.Shared;

	private static void ReturnAndGetNewPooledArray(ref byte[] buffer, int newSize)
	{
		byte[] array = buffer;
		buffer = null;
		Pool.Return(array, clearArray: true);
		buffer = Pool.Rent(newSize);
	}

	public QPackDecoder(int maxHeadersLength)
	{
		_maxHeadersLength = maxHeadersLength;
		_stringOctets = Pool.Rent(64);
		_headerNameOctets = Pool.Rent(64);
		_headerValueOctets = Pool.Rent(64);
	}

	public void Dispose()
	{
		if (_stringOctets != null)
		{
			Pool.Return(_stringOctets, clearArray: true);
			_stringOctets = null;
		}
		if (_headerNameOctets != null)
		{
			Pool.Return(_headerNameOctets, clearArray: true);
			_headerNameOctets = null;
		}
		if (_headerValueOctets != null)
		{
			Pool.Return(_headerValueOctets, clearArray: true);
			_headerValueOctets = null;
		}
	}

	public void Reset()
	{
		_state = State.RequiredInsertCount;
	}

	public void Decode(ReadOnlySpan<byte> headerBlock, IHttpHeadersHandler handler)
	{
		ReadOnlySpan<byte> readOnlySpan = headerBlock;
		for (int i = 0; i < readOnlySpan.Length; i++)
		{
			byte b = readOnlySpan[i];
			OnByte(b, handler);
		}
	}

	private void OnByte(byte b, IHttpHeadersHandler handler)
	{
		int result;
		switch (_state)
		{
		case State.RequiredInsertCount:
			if (_integerDecoder.BeginTryDecode(b, 8, out result))
			{
				OnRequiredInsertCount(result);
			}
			else
			{
				_state = State.RequiredInsertCountContinue;
			}
			break;
		case State.RequiredInsertCountContinue:
			if (_integerDecoder.TryDecode(b, out result))
			{
				OnRequiredInsertCount(result);
			}
			break;
		case State.Base:
		{
			int num = -129 & b;
			if (_integerDecoder.BeginTryDecode(b, 7, out result))
			{
				OnBase(result);
			}
			else
			{
				_state = State.BaseContinue;
			}
			break;
		}
		case State.BaseContinue:
			if (_integerDecoder.TryDecode(b, out result))
			{
				OnBase(result);
			}
			break;
		case State.CompressedHeaders:
			switch (BitOperations.LeadingZeroCount(b))
			{
			case 24:
			{
				int num = 0x3F & b;
				if ((b & 0x40) != 64)
				{
					ThrowDynamicTableNotSupported();
				}
				if (_integerDecoder.BeginTryDecode((byte)num, 6, out result))
				{
					OnIndexedHeaderField(result, handler);
				}
				else
				{
					_state = State.HeaderFieldIndex;
				}
				break;
			}
			case 25:
			{
				if ((0x10 & b) != 16)
				{
					ThrowDynamicTableNotSupported();
				}
				int num = b & 0xF;
				if (_integerDecoder.BeginTryDecode((byte)num, 4, out result))
				{
					OnIndexedHeaderName(result);
				}
				else
				{
					_state = State.HeaderNameIndex;
				}
				break;
			}
			case 26:
			{
				_huffman = (b & 8) != 0;
				int num = b & 7;
				if (_integerDecoder.BeginTryDecode((byte)num, 3, out result))
				{
					if (result == 0)
					{
						throw new QPackDecodingException(System.SR.Format(System.SR.net_http_invalid_header_name, ""));
					}
					OnStringLength(result, State.HeaderName);
				}
				else
				{
					_state = State.HeaderNameLength;
				}
				break;
			}
			case 27:
			{
				int num = -241 & b;
				if (_integerDecoder.BeginTryDecode((byte)num, 4, out result))
				{
					OnPostBaseIndex(result, handler);
				}
				else
				{
					_state = State.PostBaseIndex;
				}
				break;
			}
			default:
			{
				int num = b & 7;
				if (_integerDecoder.BeginTryDecode((byte)num, 3, out result))
				{
					OnIndexedHeaderNamePostBase(result);
				}
				else
				{
					_state = State.HeaderNameIndexPostBase;
				}
				break;
			}
			}
			break;
		case State.HeaderNameLength:
			if (_integerDecoder.TryDecode(b, out result))
			{
				if (result == 0)
				{
					throw new QPackDecodingException(System.SR.Format(System.SR.net_http_invalid_header_name, ""));
				}
				OnStringLength(result, State.HeaderName);
			}
			break;
		case State.HeaderName:
			_stringOctets[_stringIndex++] = b;
			if (_stringIndex == _stringLength)
			{
				OnString(State.HeaderValueLength);
			}
			break;
		case State.HeaderNameIndex:
			if (_integerDecoder.TryDecode(b, out result))
			{
				OnIndexedHeaderName(result);
			}
			break;
		case State.HeaderNameIndexPostBase:
			if (_integerDecoder.TryDecode(b, out result))
			{
				OnIndexedHeaderNamePostBase(result);
			}
			break;
		case State.HeaderValueLength:
			_huffman = (b & 0x80) != 0;
			if (_integerDecoder.BeginTryDecode((byte)(b & 0xFFFFFF7Fu), 7, out result))
			{
				OnStringLength(result, State.HeaderValue);
				if (result == 0)
				{
					ProcessHeaderValue(handler);
				}
			}
			else
			{
				_state = State.HeaderValueLengthContinue;
			}
			break;
		case State.HeaderValueLengthContinue:
			if (_integerDecoder.TryDecode(b, out result))
			{
				OnStringLength(result, State.HeaderValue);
				if (result == 0)
				{
					ProcessHeaderValue(handler);
				}
			}
			break;
		case State.HeaderValue:
			_stringOctets[_stringIndex++] = b;
			if (_stringIndex == _stringLength)
			{
				ProcessHeaderValue(handler);
			}
			break;
		case State.HeaderFieldIndex:
			if (_integerDecoder.TryDecode(b, out result))
			{
				OnIndexedHeaderField(result, handler);
			}
			break;
		case State.PostBaseIndex:
			if (_integerDecoder.TryDecode(b, out result))
			{
				OnPostBaseIndex(result, handler);
			}
			break;
		case State.HeaderNameLengthContinue:
		case State.DynamicTableSizeUpdate:
		case State.LiteralHeaderFieldWithNameReference:
			break;
		}
	}

	private void OnStringLength(int length, State nextState)
	{
		if (length > _stringOctets.Length)
		{
			if (length > _maxHeadersLength)
			{
				throw new QPackDecodingException(System.SR.Format(System.SR.net_http_headers_exceeded_length, _maxHeadersLength));
			}
			ReturnAndGetNewPooledArray(ref _stringOctets, length);
		}
		_stringLength = length;
		_stringIndex = 0;
		_state = nextState;
	}

	private void ProcessHeaderValue(IHttpHeadersHandler handler)
	{
		OnString(State.CompressedHeaders);
		Span<byte> span = _headerValueOctets.AsSpan(0, _headerValueLength);
		int? index = _index;
		if (index.HasValue)
		{
			int valueOrDefault = index.GetValueOrDefault();
			handler.OnStaticIndexedHeader(valueOrDefault, span);
			_index = null;
		}
		else
		{
			Span<byte> span2 = _headerNameOctets.AsSpan(0, _headerNameLength);
			handler.OnHeader(span2, span);
		}
	}

	private void OnString(State nextState)
	{
		try
		{
			if (_state == State.HeaderName)
			{
				_headerNameLength = Decode(ref _headerNameOctets);
				_headerName = _headerNameOctets;
			}
			else
			{
				_headerValueLength = Decode(ref _headerValueOctets);
			}
		}
		catch (HuffmanDecodingException innerException)
		{
			throw new QPackDecodingException(System.SR.net_http_hpack_huffman_decode_failed, innerException);
		}
		_state = nextState;
		int Decode(ref byte[] dst)
		{
			if (_huffman)
			{
				return Huffman.Decode(new ReadOnlySpan<byte>(_stringOctets, 0, _stringLength), ref dst);
			}
			if (dst.Length < _stringLength)
			{
				ReturnAndGetNewPooledArray(ref dst, _stringLength);
			}
			Buffer.BlockCopy(_stringOctets, 0, dst, 0, _stringLength);
			return _stringLength;
		}
	}

	private void OnIndexedHeaderName(int index)
	{
		_index = index;
		_state = State.HeaderValueLength;
	}

	private void OnIndexedHeaderNamePostBase(int index)
	{
		ThrowDynamicTableNotSupported();
	}

	private void OnPostBaseIndex(int intResult, IHttpHeadersHandler handler)
	{
		ThrowDynamicTableNotSupported();
	}

	private void OnBase(int deltaBase)
	{
		if (deltaBase != 0)
		{
			ThrowDynamicTableNotSupported();
		}
		_state = State.CompressedHeaders;
	}

	private void OnRequiredInsertCount(int requiredInsertCount)
	{
		if (requiredInsertCount != 0)
		{
			ThrowDynamicTableNotSupported();
		}
		_state = State.Base;
	}

	private void OnIndexedHeaderField(int index, IHttpHeadersHandler handler)
	{
		handler.OnStaticIndexedHeader(index);
		_state = State.CompressedHeaders;
	}

	private static void ThrowDynamicTableNotSupported()
	{
		throw new QPackDecodingException(System.SR.net_http_qpack_no_dynamic_table);
	}
}
