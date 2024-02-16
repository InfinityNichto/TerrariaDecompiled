using System.Buffers.Binary;
using System.Runtime.CompilerServices;
using System.Text;

namespace System.IO;

public class BinaryReader : IDisposable
{
	private readonly Stream _stream;

	private readonly byte[] _buffer;

	private readonly Decoder _decoder;

	private byte[] _charBytes;

	private char[] _charBuffer;

	private readonly int _maxCharsSize;

	private readonly bool _2BytesPerChar;

	private readonly bool _isMemoryStream;

	private readonly bool _leaveOpen;

	private bool _disposed;

	public virtual Stream BaseStream => _stream;

	public BinaryReader(Stream input)
		: this(input, Encoding.UTF8, leaveOpen: false)
	{
	}

	public BinaryReader(Stream input, Encoding encoding)
		: this(input, encoding, leaveOpen: false)
	{
	}

	public BinaryReader(Stream input, Encoding encoding, bool leaveOpen)
	{
		if (input == null)
		{
			throw new ArgumentNullException("input");
		}
		if (encoding == null)
		{
			throw new ArgumentNullException("encoding");
		}
		if (!input.CanRead)
		{
			throw new ArgumentException(SR.Argument_StreamNotReadable);
		}
		_stream = input;
		_decoder = encoding.GetDecoder();
		_maxCharsSize = encoding.GetMaxCharCount(128);
		int num = encoding.GetMaxByteCount(1);
		if (num < 16)
		{
			num = 16;
		}
		_buffer = new byte[num];
		_2BytesPerChar = encoding is UnicodeEncoding;
		_isMemoryStream = _stream.GetType() == typeof(MemoryStream);
		_leaveOpen = leaveOpen;
	}

	protected virtual void Dispose(bool disposing)
	{
		if (!_disposed)
		{
			if (disposing && !_leaveOpen)
			{
				_stream.Close();
			}
			_disposed = true;
		}
	}

	public void Dispose()
	{
		Dispose(disposing: true);
	}

	public virtual void Close()
	{
		Dispose(disposing: true);
	}

	private void ThrowIfDisposed()
	{
		if (_disposed)
		{
			ThrowHelper.ThrowObjectDisposedException_FileClosed();
		}
	}

	public virtual int PeekChar()
	{
		ThrowIfDisposed();
		if (!_stream.CanSeek)
		{
			return -1;
		}
		long position = _stream.Position;
		int result = Read();
		_stream.Position = position;
		return result;
	}

	public virtual int Read()
	{
		ThrowIfDisposed();
		int num = 0;
		long num2 = 0L;
		if (_stream.CanSeek)
		{
			num2 = _stream.Position;
		}
		if (_charBytes == null)
		{
			_charBytes = new byte[128];
		}
		Span<char> chars = stackalloc char[1];
		while (num == 0)
		{
			int num3 = ((!_2BytesPerChar) ? 1 : 2);
			int num4 = _stream.ReadByte();
			_charBytes[0] = (byte)num4;
			if (num4 == -1)
			{
				num3 = 0;
			}
			if (num3 == 2)
			{
				num4 = _stream.ReadByte();
				_charBytes[1] = (byte)num4;
				if (num4 == -1)
				{
					num3 = 1;
				}
			}
			if (num3 == 0)
			{
				return -1;
			}
			try
			{
				num = _decoder.GetChars(new ReadOnlySpan<byte>(_charBytes, 0, num3), chars, flush: false);
			}
			catch
			{
				if (_stream.CanSeek)
				{
					_stream.Seek(num2 - _stream.Position, SeekOrigin.Current);
				}
				throw;
			}
		}
		return chars[0];
	}

	public virtual byte ReadByte()
	{
		return InternalReadByte();
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private byte InternalReadByte()
	{
		ThrowIfDisposed();
		int num = _stream.ReadByte();
		if (num == -1)
		{
			ThrowHelper.ThrowEndOfFileException();
		}
		return (byte)num;
	}

	[CLSCompliant(false)]
	public virtual sbyte ReadSByte()
	{
		return (sbyte)InternalReadByte();
	}

	public virtual bool ReadBoolean()
	{
		return InternalReadByte() != 0;
	}

	public virtual char ReadChar()
	{
		int num = Read();
		if (num == -1)
		{
			ThrowHelper.ThrowEndOfFileException();
		}
		return (char)num;
	}

	public virtual short ReadInt16()
	{
		return BinaryPrimitives.ReadInt16LittleEndian(InternalRead(2));
	}

	[CLSCompliant(false)]
	public virtual ushort ReadUInt16()
	{
		return BinaryPrimitives.ReadUInt16LittleEndian(InternalRead(2));
	}

	public virtual int ReadInt32()
	{
		return BinaryPrimitives.ReadInt32LittleEndian(InternalRead(4));
	}

	[CLSCompliant(false)]
	public virtual uint ReadUInt32()
	{
		return BinaryPrimitives.ReadUInt32LittleEndian(InternalRead(4));
	}

	public virtual long ReadInt64()
	{
		return BinaryPrimitives.ReadInt64LittleEndian(InternalRead(8));
	}

	[CLSCompliant(false)]
	public virtual ulong ReadUInt64()
	{
		return BinaryPrimitives.ReadUInt64LittleEndian(InternalRead(8));
	}

	public virtual Half ReadHalf()
	{
		return BitConverter.Int16BitsToHalf(BinaryPrimitives.ReadInt16LittleEndian(InternalRead(2)));
	}

	public virtual float ReadSingle()
	{
		return BitConverter.Int32BitsToSingle(BinaryPrimitives.ReadInt32LittleEndian(InternalRead(4)));
	}

	public virtual double ReadDouble()
	{
		return BitConverter.Int64BitsToDouble(BinaryPrimitives.ReadInt64LittleEndian(InternalRead(8)));
	}

	public virtual decimal ReadDecimal()
	{
		ReadOnlySpan<byte> span = InternalRead(16);
		try
		{
			return decimal.ToDecimal(span);
		}
		catch (ArgumentException innerException)
		{
			throw new IOException(SR.Arg_DecBitCtor, innerException);
		}
	}

	public virtual string ReadString()
	{
		ThrowIfDisposed();
		int num = 0;
		int num2 = Read7BitEncodedInt();
		if (num2 < 0)
		{
			throw new IOException(SR.Format(SR.IO_InvalidStringLen_Len, num2));
		}
		if (num2 == 0)
		{
			return string.Empty;
		}
		if (_charBytes == null)
		{
			_charBytes = new byte[128];
		}
		if (_charBuffer == null)
		{
			_charBuffer = new char[_maxCharsSize];
		}
		StringBuilder stringBuilder = null;
		do
		{
			int count = ((num2 - num > 128) ? 128 : (num2 - num));
			int num3 = _stream.Read(_charBytes, 0, count);
			if (num3 == 0)
			{
				ThrowHelper.ThrowEndOfFileException();
			}
			int chars = _decoder.GetChars(_charBytes, 0, num3, _charBuffer, 0);
			if (num == 0 && num3 == num2)
			{
				return new string(_charBuffer, 0, chars);
			}
			if (stringBuilder == null)
			{
				stringBuilder = StringBuilderCache.Acquire(Math.Min(num2, 360));
			}
			stringBuilder.Append(_charBuffer, 0, chars);
			num += num3;
		}
		while (num < num2);
		return StringBuilderCache.GetStringAndRelease(stringBuilder);
	}

	public virtual int Read(char[] buffer, int index, int count)
	{
		if (buffer == null)
		{
			throw new ArgumentNullException("buffer", SR.ArgumentNull_Buffer);
		}
		if (index < 0)
		{
			throw new ArgumentOutOfRangeException("index", SR.ArgumentOutOfRange_NeedNonNegNum);
		}
		if (count < 0)
		{
			throw new ArgumentOutOfRangeException("count", SR.ArgumentOutOfRange_NeedNonNegNum);
		}
		if (buffer.Length - index < count)
		{
			throw new ArgumentException(SR.Argument_InvalidOffLen);
		}
		ThrowIfDisposed();
		return InternalReadChars(new Span<char>(buffer, index, count));
	}

	public virtual int Read(Span<char> buffer)
	{
		ThrowIfDisposed();
		return InternalReadChars(buffer);
	}

	private int InternalReadChars(Span<char> buffer)
	{
		int num = 0;
		while (!buffer.IsEmpty)
		{
			int num2 = buffer.Length;
			if (_2BytesPerChar)
			{
				num2 <<= 1;
			}
			if (num2 > 1 && !(_decoder is DecoderNLS { HasState: false }))
			{
				num2--;
				if (_2BytesPerChar && num2 > 2)
				{
					num2 -= 2;
				}
			}
			ReadOnlySpan<byte> bytes;
			if (_isMemoryStream)
			{
				MemoryStream memoryStream = (MemoryStream)_stream;
				int start = memoryStream.InternalGetPosition();
				num2 = memoryStream.InternalEmulateRead(num2);
				bytes = new ReadOnlySpan<byte>(memoryStream.InternalGetBuffer(), start, num2);
			}
			else
			{
				if (_charBytes == null)
				{
					_charBytes = new byte[128];
				}
				if (num2 > 128)
				{
					num2 = 128;
				}
				num2 = _stream.Read(_charBytes, 0, num2);
				bytes = new ReadOnlySpan<byte>(_charBytes, 0, num2);
			}
			if (bytes.IsEmpty)
			{
				break;
			}
			int chars = _decoder.GetChars(bytes, buffer, flush: false);
			buffer = buffer.Slice(chars);
			num += chars;
		}
		return num;
	}

	public virtual char[] ReadChars(int count)
	{
		if (count < 0)
		{
			throw new ArgumentOutOfRangeException("count", SR.ArgumentOutOfRange_NeedNonNegNum);
		}
		ThrowIfDisposed();
		if (count == 0)
		{
			return Array.Empty<char>();
		}
		char[] array = new char[count];
		int num = InternalReadChars(new Span<char>(array));
		if (num != count)
		{
			char[] array2 = new char[num];
			Buffer.BlockCopy(array, 0, array2, 0, 2 * num);
			array = array2;
		}
		return array;
	}

	public virtual int Read(byte[] buffer, int index, int count)
	{
		if (buffer == null)
		{
			throw new ArgumentNullException("buffer", SR.ArgumentNull_Buffer);
		}
		if (index < 0)
		{
			throw new ArgumentOutOfRangeException("index", SR.ArgumentOutOfRange_NeedNonNegNum);
		}
		if (count < 0)
		{
			throw new ArgumentOutOfRangeException("count", SR.ArgumentOutOfRange_NeedNonNegNum);
		}
		if (buffer.Length - index < count)
		{
			throw new ArgumentException(SR.Argument_InvalidOffLen);
		}
		ThrowIfDisposed();
		return _stream.Read(buffer, index, count);
	}

	public virtual int Read(Span<byte> buffer)
	{
		ThrowIfDisposed();
		return _stream.Read(buffer);
	}

	public virtual byte[] ReadBytes(int count)
	{
		if (count < 0)
		{
			throw new ArgumentOutOfRangeException("count", SR.ArgumentOutOfRange_NeedNonNegNum);
		}
		ThrowIfDisposed();
		if (count == 0)
		{
			return Array.Empty<byte>();
		}
		byte[] array = new byte[count];
		int num = 0;
		do
		{
			int num2 = _stream.Read(array, num, count);
			if (num2 == 0)
			{
				break;
			}
			num += num2;
			count -= num2;
		}
		while (count > 0);
		if (num != array.Length)
		{
			byte[] array2 = new byte[num];
			Buffer.BlockCopy(array, 0, array2, 0, num);
			array = array2;
		}
		return array;
	}

	private ReadOnlySpan<byte> InternalRead(int numBytes)
	{
		if (_isMemoryStream)
		{
			return ((MemoryStream)_stream).InternalReadSpan(numBytes);
		}
		ThrowIfDisposed();
		int num = 0;
		do
		{
			int num2 = _stream.Read(_buffer, num, numBytes - num);
			if (num2 == 0)
			{
				ThrowHelper.ThrowEndOfFileException();
			}
			num += num2;
		}
		while (num < numBytes);
		return _buffer;
	}

	protected virtual void FillBuffer(int numBytes)
	{
		if (numBytes < 0 || numBytes > _buffer.Length)
		{
			throw new ArgumentOutOfRangeException("numBytes", SR.ArgumentOutOfRange_BinaryReaderFillBuffer);
		}
		int num = 0;
		int num2 = 0;
		ThrowIfDisposed();
		if (numBytes == 1)
		{
			num2 = _stream.ReadByte();
			if (num2 == -1)
			{
				ThrowHelper.ThrowEndOfFileException();
			}
			_buffer[0] = (byte)num2;
			return;
		}
		do
		{
			num2 = _stream.Read(_buffer, num, numBytes - num);
			if (num2 == 0)
			{
				ThrowHelper.ThrowEndOfFileException();
			}
			num += num2;
		}
		while (num < numBytes);
	}

	public int Read7BitEncodedInt()
	{
		uint num = 0u;
		byte b;
		for (int i = 0; i < 28; i += 7)
		{
			b = ReadByte();
			num |= (uint)((b & 0x7F) << i);
			if ((uint)b <= 127u)
			{
				return (int)num;
			}
		}
		b = ReadByte();
		if ((uint)b > 15u)
		{
			throw new FormatException(SR.Format_Bad7BitInt);
		}
		return (int)num | (b << 28);
	}

	public long Read7BitEncodedInt64()
	{
		ulong num = 0uL;
		byte b;
		for (int i = 0; i < 63; i += 7)
		{
			b = ReadByte();
			num |= ((ulong)b & 0x7FuL) << i;
			if ((uint)b <= 127u)
			{
				return (long)num;
			}
		}
		b = ReadByte();
		if ((uint)b > 1u)
		{
			throw new FormatException(SR.Format_Bad7BitInt);
		}
		return (long)(num | ((ulong)b << 63));
	}
}
