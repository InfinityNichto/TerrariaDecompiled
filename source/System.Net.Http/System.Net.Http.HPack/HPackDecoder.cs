using System.Numerics;

namespace System.Net.Http.HPack;

internal sealed class HPackDecoder
{
	private enum State : byte
	{
		Ready,
		HeaderFieldIndex,
		HeaderNameIndex,
		HeaderNameLength,
		HeaderNameLengthContinue,
		HeaderName,
		HeaderValueLength,
		HeaderValueLengthContinue,
		HeaderValue,
		DynamicTableSizeUpdate
	}

	private readonly int _maxDynamicTableSize;

	private readonly int _maxHeadersLength;

	private readonly DynamicTable _dynamicTable;

	private IntegerDecoder _integerDecoder;

	private byte[] _stringOctets;

	private byte[] _headerNameOctets;

	private byte[] _headerValueOctets;

	private (int start, int length)? _headerNameRange;

	private (int start, int length)? _headerValueRange;

	private State _state;

	private byte[] _headerName;

	private int _headerStaticIndex;

	private int _stringIndex;

	private int _stringLength;

	private int _headerNameLength;

	private int _headerValueLength;

	private bool _index;

	private bool _huffman;

	private bool _headersObserved;

	public HPackDecoder(int maxDynamicTableSize = 4096, int maxHeadersLength = 65536)
		: this(maxDynamicTableSize, maxHeadersLength, new DynamicTable(maxDynamicTableSize))
	{
	}

	internal HPackDecoder(int maxDynamicTableSize, int maxHeadersLength, DynamicTable dynamicTable)
	{
		_maxDynamicTableSize = maxDynamicTableSize;
		_maxHeadersLength = maxHeadersLength;
		_dynamicTable = dynamicTable;
		_stringOctets = new byte[4096];
		_headerNameOctets = new byte[4096];
		_headerValueOctets = new byte[4096];
	}

	public void Decode(ReadOnlySpan<byte> data, bool endHeaders, IHttpHeadersHandler handler)
	{
		DecodeInternal(data, handler);
		CheckIncompleteHeaderBlock(endHeaders);
	}

	private void DecodeInternal(ReadOnlySpan<byte> data, IHttpHeadersHandler handler)
	{
		int currentIndex = 0;
		do
		{
			switch (_state)
			{
			case State.Ready:
				Parse(data, ref currentIndex, handler);
				break;
			case State.HeaderFieldIndex:
				ParseHeaderFieldIndex(data, ref currentIndex, handler);
				break;
			case State.HeaderNameIndex:
				ParseHeaderNameIndex(data, ref currentIndex, handler);
				break;
			case State.HeaderNameLength:
				ParseHeaderNameLength(data, ref currentIndex, handler);
				break;
			case State.HeaderNameLengthContinue:
				ParseHeaderNameLengthContinue(data, ref currentIndex, handler);
				break;
			case State.HeaderName:
				ParseHeaderName(data, ref currentIndex, handler);
				break;
			case State.HeaderValueLength:
				ParseHeaderValueLength(data, ref currentIndex, handler);
				break;
			case State.HeaderValueLengthContinue:
				ParseHeaderValueLengthContinue(data, ref currentIndex, handler);
				break;
			case State.HeaderValue:
				ParseHeaderValue(data, ref currentIndex, handler);
				break;
			case State.DynamicTableSizeUpdate:
				ParseDynamicTableSizeUpdate(data, ref currentIndex);
				break;
			default:
				throw new NotImplementedException(_state.ToString());
			}
		}
		while (currentIndex < data.Length);
		if (_headerNameRange.HasValue)
		{
			EnsureStringCapacity(ref _headerNameOctets);
			_headerName = _headerNameOctets;
			ReadOnlySpan<byte> readOnlySpan = data.Slice(_headerNameRange.GetValueOrDefault().start, _headerNameRange.GetValueOrDefault().length);
			readOnlySpan.CopyTo(_headerName);
			_headerNameLength = readOnlySpan.Length;
			_headerNameRange = null;
		}
	}

	private void ParseDynamicTableSizeUpdate(ReadOnlySpan<byte> data, ref int currentIndex)
	{
		if (TryDecodeInteger(data, ref currentIndex, out var result))
		{
			SetDynamicHeaderTableSize(result);
			_state = State.Ready;
		}
	}

	private void ParseHeaderValueLength(ReadOnlySpan<byte> data, ref int currentIndex, IHttpHeadersHandler handler)
	{
		if (currentIndex >= data.Length)
		{
			return;
		}
		byte b = data[currentIndex++];
		_huffman = IsHuffmanEncoded(b);
		if (_integerDecoder.BeginTryDecode((byte)(b & 0xFFFFFF7Fu), 7, out var result))
		{
			OnStringLength(result, State.HeaderValue);
			if (result == 0)
			{
				OnString(State.Ready);
				ProcessHeaderValue(data, handler);
			}
			else
			{
				ParseHeaderValue(data, ref currentIndex, handler);
			}
		}
		else
		{
			_state = State.HeaderValueLengthContinue;
			ParseHeaderValueLengthContinue(data, ref currentIndex, handler);
		}
	}

	private void ParseHeaderNameLengthContinue(ReadOnlySpan<byte> data, ref int currentIndex, IHttpHeadersHandler handler)
	{
		if (TryDecodeInteger(data, ref currentIndex, out var result))
		{
			OnStringLength(result, State.HeaderName);
			ParseHeaderName(data, ref currentIndex, handler);
		}
	}

	private void ParseHeaderValueLengthContinue(ReadOnlySpan<byte> data, ref int currentIndex, IHttpHeadersHandler handler)
	{
		if (TryDecodeInteger(data, ref currentIndex, out var result))
		{
			OnStringLength(result, State.HeaderValue);
			ParseHeaderValue(data, ref currentIndex, handler);
		}
	}

	private void ParseHeaderFieldIndex(ReadOnlySpan<byte> data, ref int currentIndex, IHttpHeadersHandler handler)
	{
		if (TryDecodeInteger(data, ref currentIndex, out var result))
		{
			OnIndexedHeaderField(result, handler);
		}
	}

	private void ParseHeaderNameIndex(ReadOnlySpan<byte> data, ref int currentIndex, IHttpHeadersHandler handler)
	{
		if (TryDecodeInteger(data, ref currentIndex, out var result))
		{
			OnIndexedHeaderName(result);
			ParseHeaderValueLength(data, ref currentIndex, handler);
		}
	}

	private void ParseHeaderNameLength(ReadOnlySpan<byte> data, ref int currentIndex, IHttpHeadersHandler handler)
	{
		if (currentIndex >= data.Length)
		{
			return;
		}
		byte b = data[currentIndex++];
		_huffman = IsHuffmanEncoded(b);
		if (_integerDecoder.BeginTryDecode((byte)(b & 0xFFFFFF7Fu), 7, out var result))
		{
			if (result == 0)
			{
				throw new HPackDecodingException(System.SR.Format(System.SR.net_http_invalid_header_name, ""));
			}
			OnStringLength(result, State.HeaderName);
			ParseHeaderName(data, ref currentIndex, handler);
		}
		else
		{
			_state = State.HeaderNameLengthContinue;
			ParseHeaderNameLengthContinue(data, ref currentIndex, handler);
		}
	}

	private void Parse(ReadOnlySpan<byte> data, ref int currentIndex, IHttpHeadersHandler handler)
	{
		if (currentIndex >= data.Length)
		{
			return;
		}
		byte b = data[currentIndex++];
		switch (BitOperations.LeadingZeroCount(b))
		{
		case 24:
		{
			_headersObserved = true;
			int num = b & -129;
			if (_integerDecoder.BeginTryDecode((byte)num, 7, out var result2))
			{
				OnIndexedHeaderField(result2, handler);
				break;
			}
			_state = State.HeaderFieldIndex;
			ParseHeaderFieldIndex(data, ref currentIndex, handler);
			break;
		}
		case 25:
			ParseLiteralHeaderField(data, ref currentIndex, b, 192, 6, index: true, handler);
			break;
		default:
			ParseLiteralHeaderField(data, ref currentIndex, b, 240, 4, index: false, handler);
			break;
		case 27:
			ParseLiteralHeaderField(data, ref currentIndex, b, 240, 4, index: false, handler);
			break;
		case 26:
		{
			if (_headersObserved)
			{
				throw new HPackDecodingException(System.SR.net_http_hpack_late_dynamic_table_size_update);
			}
			if (_integerDecoder.BeginTryDecode((byte)(b & 0xFFFFFF1Fu), 5, out var result))
			{
				SetDynamicHeaderTableSize(result);
				break;
			}
			_state = State.DynamicTableSizeUpdate;
			ParseDynamicTableSizeUpdate(data, ref currentIndex);
			break;
		}
		}
	}

	private void ParseLiteralHeaderField(ReadOnlySpan<byte> data, ref int currentIndex, byte b, byte mask, byte indexPrefix, bool index, IHttpHeadersHandler handler)
	{
		_headersObserved = true;
		_index = index;
		int num = b & ~mask;
		int result;
		if (num == 0)
		{
			_state = State.HeaderNameLength;
			ParseHeaderNameLength(data, ref currentIndex, handler);
		}
		else if (_integerDecoder.BeginTryDecode((byte)num, indexPrefix, out result))
		{
			OnIndexedHeaderName(result);
			ParseHeaderValueLength(data, ref currentIndex, handler);
		}
		else
		{
			_state = State.HeaderNameIndex;
			ParseHeaderNameIndex(data, ref currentIndex, handler);
		}
	}

	private void ParseHeaderName(ReadOnlySpan<byte> data, ref int currentIndex, IHttpHeadersHandler handler)
	{
		int num = Math.Min(_stringLength - _stringIndex, data.Length - currentIndex);
		if (num == _stringLength && !_huffman)
		{
			_headerNameRange = (currentIndex, num);
			currentIndex += num;
			_state = State.HeaderValueLength;
			return;
		}
		data.Slice(currentIndex, num).CopyTo(_stringOctets.AsSpan(_stringIndex));
		_stringIndex += num;
		currentIndex += num;
		if (_stringIndex == _stringLength)
		{
			OnString(State.HeaderValueLength);
			ParseHeaderValueLength(data, ref currentIndex, handler);
		}
	}

	private void ParseHeaderValue(ReadOnlySpan<byte> data, ref int currentIndex, IHttpHeadersHandler handler)
	{
		int num = Math.Min(_stringLength - _stringIndex, data.Length - currentIndex);
		if (num == _stringLength && !_huffman)
		{
			_headerValueRange = (currentIndex, num);
			currentIndex += num;
			_state = State.Ready;
			ProcessHeaderValue(data, handler);
			return;
		}
		data.Slice(currentIndex, num).CopyTo(_stringOctets.AsSpan(_stringIndex));
		_stringIndex += num;
		currentIndex += num;
		if (_stringIndex == _stringLength)
		{
			OnString(State.Ready);
			ProcessHeaderValue(data, handler);
		}
	}

	private void CheckIncompleteHeaderBlock(bool endHeaders)
	{
		if (endHeaders)
		{
			if (_state != 0)
			{
				throw new HPackDecodingException(System.SR.net_http_hpack_incomplete_header_block);
			}
			_headersObserved = false;
		}
	}

	private void ProcessHeaderValue(ReadOnlySpan<byte> data, IHttpHeadersHandler handler)
	{
		ReadOnlySpan<byte> value = ((!_headerValueRange.HasValue) ? ((ReadOnlySpan<byte>)_headerValueOctets.AsSpan(0, _headerValueLength)) : data.Slice(_headerValueRange.GetValueOrDefault().start, _headerValueRange.GetValueOrDefault().length));
		if (_headerStaticIndex > 0)
		{
			handler.OnStaticIndexedHeader(_headerStaticIndex, value);
			if (_index)
			{
				_dynamicTable.Insert(H2StaticTable.Get(_headerStaticIndex - 1).Name, value);
			}
		}
		else
		{
			ReadOnlySpan<byte> name = ((!_headerNameRange.HasValue) ? ((ReadOnlySpan<byte>)_headerName.AsSpan(0, _headerNameLength)) : data.Slice(_headerNameRange.GetValueOrDefault().start, _headerNameRange.GetValueOrDefault().length));
			handler.OnHeader(name, value);
			if (_index)
			{
				_dynamicTable.Insert(name, value);
			}
		}
		_headerStaticIndex = 0;
		_headerNameRange = null;
		_headerValueRange = null;
	}

	public void CompleteDecode()
	{
		if (_state != 0)
		{
			throw new HPackDecodingException(System.SR.net_http_hpack_unexpected_end);
		}
	}

	private void OnIndexedHeaderField(int index, IHttpHeadersHandler handler)
	{
		if (index <= H2StaticTable.Count)
		{
			handler.OnStaticIndexedHeader(index);
		}
		else
		{
			ref readonly HeaderField dynamicHeader = ref GetDynamicHeader(index);
			handler.OnHeader(dynamicHeader.Name, dynamicHeader.Value);
		}
		_state = State.Ready;
	}

	private void OnIndexedHeaderName(int index)
	{
		if (index <= H2StaticTable.Count)
		{
			_headerStaticIndex = index;
		}
		else
		{
			_headerName = GetDynamicHeader(index).Name;
			_headerNameLength = _headerName.Length;
		}
		_state = State.HeaderValueLength;
	}

	private void OnStringLength(int length, State nextState)
	{
		if (length > _stringOctets.Length)
		{
			if (length > _maxHeadersLength)
			{
				throw new HPackDecodingException(System.SR.Format(System.SR.net_http_headers_exceeded_length, _maxHeadersLength));
			}
			_stringOctets = new byte[Math.Max(length, _stringOctets.Length * 2)];
		}
		_stringLength = length;
		_stringIndex = 0;
		_state = nextState;
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
			throw new HPackDecodingException(System.SR.net_http_hpack_huffman_decode_failed, innerException);
		}
		_state = nextState;
		int Decode(ref byte[] dst)
		{
			if (_huffman)
			{
				return Huffman.Decode(new ReadOnlySpan<byte>(_stringOctets, 0, _stringLength), ref dst);
			}
			EnsureStringCapacity(ref dst);
			Buffer.BlockCopy(_stringOctets, 0, dst, 0, _stringLength);
			return _stringLength;
		}
	}

	private void EnsureStringCapacity(ref byte[] dst)
	{
		if (dst.Length < _stringLength)
		{
			dst = new byte[Math.Max(_stringLength, dst.Length * 2)];
		}
	}

	private bool TryDecodeInteger(ReadOnlySpan<byte> data, ref int currentIndex, out int result)
	{
		while (currentIndex < data.Length)
		{
			if (_integerDecoder.TryDecode(data[currentIndex], out result))
			{
				currentIndex++;
				return true;
			}
			currentIndex++;
		}
		result = 0;
		return false;
	}

	private static bool IsHuffmanEncoded(byte b)
	{
		return (b & 0x80) != 0;
	}

	private ref readonly HeaderField GetDynamicHeader(int index)
	{
		try
		{
			return ref _dynamicTable[index - H2StaticTable.Count - 1];
		}
		catch (IndexOutOfRangeException)
		{
			throw new HPackDecodingException(System.SR.Format(System.SR.net_http_hpack_invalid_index, index));
		}
	}

	private void SetDynamicHeaderTableSize(int size)
	{
		if (size > _maxDynamicTableSize)
		{
			throw new HPackDecodingException(System.SR.Format(System.SR.net_http_hpack_large_table_size_update, size, _maxDynamicTableSize));
		}
		_dynamicTable.Resize(size);
	}
}
