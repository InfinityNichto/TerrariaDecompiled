using System.IO;
using System.Net.Mime;
using System.Text;

namespace System.Net;

internal sealed class Base64Stream : DelegatedStream, IEncodableStream
{
	private sealed class ReadAsyncResult : System.Net.LazyAsyncResult
	{
		private readonly Base64Stream _parent;

		private readonly byte[] _buffer;

		private readonly int _offset;

		private readonly int _count;

		private int _read;

		private static readonly AsyncCallback s_onRead = OnRead;

		internal ReadAsyncResult(Base64Stream parent, byte[] buffer, int offset, int count, AsyncCallback callback, object state)
			: base(null, state, callback)
		{
			_parent = parent;
			_buffer = buffer;
			_offset = offset;
			_count = count;
		}

		private bool CompleteRead(IAsyncResult result)
		{
			_read = _parent.BaseStream.EndRead(result);
			if (_read == 0)
			{
				InvokeCallback();
				return true;
			}
			_read = _parent.DecodeBytes(_buffer, _offset, _read);
			if (_read > 0)
			{
				InvokeCallback();
				return true;
			}
			return false;
		}

		internal void Read()
		{
			IAsyncResult asyncResult;
			do
			{
				asyncResult = _parent.BaseStream.BeginRead(_buffer, _offset, _count, s_onRead, this);
			}
			while (asyncResult.CompletedSynchronously && !CompleteRead(asyncResult));
		}

		private static void OnRead(IAsyncResult result)
		{
			if (result.CompletedSynchronously)
			{
				return;
			}
			ReadAsyncResult readAsyncResult = (ReadAsyncResult)result.AsyncState;
			try
			{
				if (!readAsyncResult.CompleteRead(result))
				{
					readAsyncResult.Read();
				}
			}
			catch (Exception result2)
			{
				if (readAsyncResult.IsCompleted)
				{
					throw;
				}
				readAsyncResult.InvokeCallback(result2);
			}
		}

		internal static int End(IAsyncResult result)
		{
			ReadAsyncResult readAsyncResult = (ReadAsyncResult)result;
			readAsyncResult.InternalWaitForCompletion();
			return readAsyncResult._read;
		}
	}

	private sealed class WriteAsyncResult : System.Net.LazyAsyncResult
	{
		private static readonly AsyncCallback s_onWrite = OnWrite;

		private readonly Base64Stream _parent;

		private readonly byte[] _buffer;

		private readonly int _offset;

		private readonly int _count;

		private int _written;

		internal WriteAsyncResult(Base64Stream parent, byte[] buffer, int offset, int count, AsyncCallback callback, object state)
			: base(null, state, callback)
		{
			_parent = parent;
			_buffer = buffer;
			_offset = offset;
			_count = count;
		}

		internal void Write()
		{
			while (true)
			{
				_written += _parent.EncodeBytes(_buffer, _offset + _written, _count - _written, dontDeferFinalBytes: false, shouldAppendSpaceToCRLF: false);
				if (_written < _count)
				{
					IAsyncResult asyncResult = _parent.BaseStream.BeginWrite(_parent.WriteState.Buffer, 0, _parent.WriteState.Length, s_onWrite, this);
					if (asyncResult.CompletedSynchronously)
					{
						CompleteWrite(asyncResult);
						continue;
					}
					break;
				}
				InvokeCallback();
				break;
			}
		}

		private void CompleteWrite(IAsyncResult result)
		{
			_parent.BaseStream.EndWrite(result);
			_parent.WriteState.Reset();
		}

		private static void OnWrite(IAsyncResult result)
		{
			if (result.CompletedSynchronously)
			{
				return;
			}
			WriteAsyncResult writeAsyncResult = (WriteAsyncResult)result.AsyncState;
			try
			{
				writeAsyncResult.CompleteWrite(result);
				writeAsyncResult.Write();
			}
			catch (Exception result2)
			{
				if (writeAsyncResult.IsCompleted)
				{
					throw;
				}
				writeAsyncResult.InvokeCallback(result2);
			}
		}

		internal static void End(IAsyncResult result)
		{
			WriteAsyncResult writeAsyncResult = (WriteAsyncResult)result;
			writeAsyncResult.InternalWaitForCompletion();
		}
	}

	private sealed class ReadStateInfo
	{
		internal byte Val { get; set; }

		internal byte Pos { get; set; }
	}

	private readonly Base64WriteStateInfo _writeState;

	private ReadStateInfo _readState;

	private readonly IByteEncoder _encoder;

	private static ReadOnlySpan<byte> Base64DecodeMap => new byte[256]
	{
		255, 255, 255, 255, 255, 255, 255, 255, 255, 255,
		255, 255, 255, 255, 255, 255, 255, 255, 255, 255,
		255, 255, 255, 255, 255, 255, 255, 255, 255, 255,
		255, 255, 255, 255, 255, 255, 255, 255, 255, 255,
		255, 255, 255, 62, 255, 255, 255, 63, 52, 53,
		54, 55, 56, 57, 58, 59, 60, 61, 255, 255,
		255, 255, 255, 255, 255, 0, 1, 2, 3, 4,
		5, 6, 7, 8, 9, 10, 11, 12, 13, 14,
		15, 16, 17, 18, 19, 20, 21, 22, 23, 24,
		25, 255, 255, 255, 255, 255, 255, 26, 27, 28,
		29, 30, 31, 32, 33, 34, 35, 36, 37, 38,
		39, 40, 41, 42, 43, 44, 45, 46, 47, 48,
		49, 50, 51, 255, 255, 255, 255, 255, 255, 255,
		255, 255, 255, 255, 255, 255, 255, 255, 255, 255,
		255, 255, 255, 255, 255, 255, 255, 255, 255, 255,
		255, 255, 255, 255, 255, 255, 255, 255, 255, 255,
		255, 255, 255, 255, 255, 255, 255, 255, 255, 255,
		255, 255, 255, 255, 255, 255, 255, 255, 255, 255,
		255, 255, 255, 255, 255, 255, 255, 255, 255, 255,
		255, 255, 255, 255, 255, 255, 255, 255, 255, 255,
		255, 255, 255, 255, 255, 255, 255, 255, 255, 255,
		255, 255, 255, 255, 255, 255, 255, 255, 255, 255,
		255, 255, 255, 255, 255, 255, 255, 255, 255, 255,
		255, 255, 255, 255, 255, 255, 255, 255, 255, 255,
		255, 255, 255, 255, 255, 255, 255, 255, 255, 255,
		255, 255, 255, 255, 255, 255
	};

	private ReadStateInfo ReadState => _readState ?? (_readState = new ReadStateInfo());

	internal WriteStateInfoBase WriteState => _writeState;

	internal Base64Stream(Stream stream, Base64WriteStateInfo writeStateInfo)
		: base(stream)
	{
		_writeState = new Base64WriteStateInfo();
		_encoder = new Base64Encoder(_writeState, writeStateInfo.MaxLineLength);
	}

	internal Base64Stream(Base64WriteStateInfo writeStateInfo)
		: base(new MemoryStream())
	{
		_writeState = writeStateInfo;
		_encoder = new Base64Encoder(_writeState, writeStateInfo.MaxLineLength);
	}

	public override IAsyncResult BeginRead(byte[] buffer, int offset, int count, AsyncCallback callback, object state)
	{
		Stream.ValidateBufferArguments(buffer, offset, count);
		ReadAsyncResult readAsyncResult = new ReadAsyncResult(this, buffer, offset, count, callback, state);
		readAsyncResult.Read();
		return readAsyncResult;
	}

	public override IAsyncResult BeginWrite(byte[] buffer, int offset, int count, AsyncCallback callback, object state)
	{
		Stream.ValidateBufferArguments(buffer, offset, count);
		WriteAsyncResult writeAsyncResult = new WriteAsyncResult(this, buffer, offset, count, callback, state);
		writeAsyncResult.Write();
		return writeAsyncResult;
	}

	public override void Close()
	{
		if (_writeState != null && WriteState.Length > 0)
		{
			_encoder.AppendPadding();
			FlushInternal();
		}
		base.Close();
	}

	public unsafe int DecodeBytes(byte[] buffer, int offset, int count)
	{
		fixed (byte* ptr = buffer)
		{
			byte* ptr2 = ptr + offset;
			byte* ptr3 = ptr2;
			byte* ptr4 = ptr2;
			byte* ptr5 = ptr2 + count;
			while (ptr3 < ptr5)
			{
				if (*ptr3 == 13 || *ptr3 == 10 || *ptr3 == 61 || *ptr3 == 32 || *ptr3 == 9)
				{
					ptr3++;
					continue;
				}
				byte b = Base64DecodeMap[*ptr3];
				if (b == byte.MaxValue)
				{
					throw new FormatException(System.SR.MailBase64InvalidCharacter);
				}
				switch (ReadState.Pos)
				{
				case 0:
					ReadState.Val = (byte)(b << 2);
					ReadState.Pos++;
					break;
				case 1:
					*(ptr4++) = (byte)(ReadState.Val + (b >> 4));
					ReadState.Val = (byte)(b << 4);
					ReadState.Pos++;
					break;
				case 2:
					*(ptr4++) = (byte)(ReadState.Val + (b >> 2));
					ReadState.Val = (byte)(b << 6);
					ReadState.Pos++;
					break;
				case 3:
					*(ptr4++) = (byte)(ReadState.Val + b);
					ReadState.Pos = 0;
					break;
				}
				ptr3++;
			}
			return (int)(ptr4 - ptr2);
		}
	}

	internal int EncodeBytes(byte[] buffer, int offset, int count, bool dontDeferFinalBytes, bool shouldAppendSpaceToCRLF)
	{
		return _encoder.EncodeBytes(buffer, offset, count, dontDeferFinalBytes, shouldAppendSpaceToCRLF);
	}

	public int EncodeString(string value, Encoding encoding)
	{
		return _encoder.EncodeString(value, encoding);
	}

	public string GetEncodedString()
	{
		return _encoder.GetEncodedString();
	}

	public override int EndRead(IAsyncResult asyncResult)
	{
		if (asyncResult == null)
		{
			throw new ArgumentNullException("asyncResult");
		}
		return ReadAsyncResult.End(asyncResult);
	}

	public override void EndWrite(IAsyncResult asyncResult)
	{
		if (asyncResult == null)
		{
			throw new ArgumentNullException("asyncResult");
		}
		WriteAsyncResult.End(asyncResult);
	}

	public override void Flush()
	{
		if (_writeState != null && WriteState.Length > 0)
		{
			FlushInternal();
		}
		base.Flush();
	}

	private void FlushInternal()
	{
		base.Write(WriteState.Buffer, 0, WriteState.Length);
		WriteState.Reset();
	}

	public override int Read(byte[] buffer, int offset, int count)
	{
		Stream.ValidateBufferArguments(buffer, offset, count);
		int num;
		do
		{
			num = base.Read(buffer, offset, count);
			if (num == 0)
			{
				return 0;
			}
			num = DecodeBytes(buffer, offset, num);
		}
		while (num <= 0);
		return num;
	}

	public override void Write(byte[] buffer, int offset, int count)
	{
		Stream.ValidateBufferArguments(buffer, offset, count);
		int num = 0;
		while (true)
		{
			num += EncodeBytes(buffer, offset + num, count - num, dontDeferFinalBytes: false, shouldAppendSpaceToCRLF: false);
			if (num < count)
			{
				FlushInternal();
				continue;
			}
			break;
		}
	}
}
