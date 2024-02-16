using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace System.Xml;

internal abstract class XmlStreamNodeWriter : XmlNodeWriter
{
	private Stream _stream;

	private readonly byte[] _buffer;

	private int _offset;

	private bool _ownsStream;

	private const int bufferLength = 512;

	private const int maxBytesPerChar = 3;

	private Encoding _encoding;

	private static readonly UTF8Encoding s_UTF8Encoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true);

	public byte[] StreamBuffer => _buffer;

	public int BufferOffset => _offset;

	public int Position => (int)_stream.Position + _offset;

	protected XmlStreamNodeWriter()
	{
		_buffer = new byte[512];
	}

	protected void SetOutput(Stream stream, bool ownsStream, Encoding encoding)
	{
		_stream = stream;
		_ownsStream = ownsStream;
		_offset = 0;
		_encoding = encoding;
	}

	private int GetByteCount(char[] chars)
	{
		if (_encoding == null)
		{
			return s_UTF8Encoding.GetByteCount(chars);
		}
		return _encoding.GetByteCount(chars);
	}

	protected byte[] GetBuffer(int count, out int offset)
	{
		int offset2 = _offset;
		if (offset2 + count <= 512)
		{
			offset = offset2;
		}
		else
		{
			FlushBuffer();
			offset = 0;
		}
		return _buffer;
	}

	protected async Task<BytesWithOffset> GetBufferAsync(int count)
	{
		int offset = _offset;
		int offset2;
		if (offset + count <= 512)
		{
			offset2 = offset;
		}
		else
		{
			await FlushBufferAsync().ConfigureAwait(continueOnCapturedContext: false);
			offset2 = 0;
		}
		return new BytesWithOffset(_buffer, offset2);
	}

	protected void Advance(int count)
	{
		_offset += count;
	}

	private void EnsureByte()
	{
		if (_offset >= 512)
		{
			FlushBuffer();
		}
	}

	protected void WriteByte(byte b)
	{
		EnsureByte();
		_buffer[_offset++] = b;
	}

	protected Task WriteByteAsync(byte b)
	{
		if (_offset >= 512)
		{
			return FlushBufferAndWriteByteAsync(b);
		}
		_buffer[_offset++] = b;
		return Task.CompletedTask;
	}

	private async Task FlushBufferAndWriteByteAsync(byte b)
	{
		await FlushBufferAsync().ConfigureAwait(continueOnCapturedContext: false);
		_buffer[_offset++] = b;
	}

	protected void WriteByte(char ch)
	{
		WriteByte((byte)ch);
	}

	protected Task WriteByteAsync(char ch)
	{
		return WriteByteAsync((byte)ch);
	}

	protected void WriteBytes(byte b1, byte b2)
	{
		byte[] buffer = _buffer;
		int num = _offset;
		if (num + 1 >= 512)
		{
			FlushBuffer();
			num = 0;
		}
		buffer[num] = b1;
		buffer[num + 1] = b2;
		_offset += 2;
	}

	protected Task WriteBytesAsync(byte b1, byte b2)
	{
		if (_offset + 1 >= 512)
		{
			return FlushAndWriteBytesAsync(b1, b2);
		}
		_buffer[_offset++] = b1;
		_buffer[_offset++] = b2;
		return Task.CompletedTask;
	}

	private async Task FlushAndWriteBytesAsync(byte b1, byte b2)
	{
		await FlushBufferAsync().ConfigureAwait(continueOnCapturedContext: false);
		_buffer[_offset++] = b1;
		_buffer[_offset++] = b2;
	}

	protected void WriteBytes(char ch1, char ch2)
	{
		WriteBytes((byte)ch1, (byte)ch2);
	}

	protected Task WriteBytesAsync(char ch1, char ch2)
	{
		return WriteBytesAsync((byte)ch1, (byte)ch2);
	}

	public void WriteBytes(byte[] byteBuffer, int byteOffset, int byteCount)
	{
		if (byteCount < 512)
		{
			int offset;
			byte[] buffer = GetBuffer(byteCount, out offset);
			Buffer.BlockCopy(byteBuffer, byteOffset, buffer, offset, byteCount);
			Advance(byteCount);
		}
		else
		{
			FlushBuffer();
			_stream.Write(byteBuffer, byteOffset, byteCount);
		}
	}

	protected unsafe void UnsafeWriteBytes(byte* bytes, int byteCount)
	{
		FlushBuffer();
		byte[] buffer = _buffer;
		while (byteCount >= 512)
		{
			for (int i = 0; i < 512; i++)
			{
				buffer[i] = bytes[i];
			}
			_stream.Write(buffer, 0, 512);
			bytes += 512;
			byteCount -= 512;
		}
		for (int j = 0; j < byteCount; j++)
		{
			buffer[j] = bytes[j];
		}
		_stream.Write(buffer, 0, byteCount);
	}

	protected unsafe void WriteUTF8Char(int ch)
	{
		if (ch < 128)
		{
			WriteByte((byte)ch);
		}
		else if (ch <= 65535)
		{
			char* ptr = stackalloc char[1];
			*ptr = (char)ch;
			UnsafeWriteUTF8Chars(ptr, 1);
		}
		else
		{
			SurrogateChar surrogateChar = new SurrogateChar(ch);
			char* ptr2 = stackalloc char[2];
			*ptr2 = surrogateChar.HighChar;
			ptr2[1] = surrogateChar.LowChar;
			UnsafeWriteUTF8Chars(ptr2, 2);
		}
	}

	protected void WriteUTF8Chars(byte[] chars, int charOffset, int charCount)
	{
		if (charCount < 512)
		{
			int offset;
			byte[] buffer = GetBuffer(charCount, out offset);
			Buffer.BlockCopy(chars, charOffset, buffer, offset, charCount);
			Advance(charCount);
		}
		else
		{
			FlushBuffer();
			_stream.Write(chars, charOffset, charCount);
		}
	}

	protected unsafe void WriteUTF8Chars(string value)
	{
		int length = value.Length;
		if (length > 0)
		{
			fixed (char* chars = value)
			{
				UnsafeWriteUTF8Chars(chars, length);
			}
		}
	}

	protected unsafe void UnsafeWriteUTF8Chars(char* chars, int charCount)
	{
		while (charCount > 170)
		{
			int num = 170;
			if ((chars[num - 1] & 0xFC00) == 55296)
			{
				num--;
			}
			int offset;
			byte[] buffer = GetBuffer(num * 3, out offset);
			Advance(UnsafeGetUTF8Chars(chars, num, buffer, offset));
			charCount -= num;
			chars += num;
		}
		if (charCount > 0)
		{
			int offset2;
			byte[] buffer2 = GetBuffer(charCount * 3, out offset2);
			Advance(UnsafeGetUTF8Chars(chars, charCount, buffer2, offset2));
		}
	}

	protected unsafe void UnsafeWriteUnicodeChars(char* chars, int charCount)
	{
		while (charCount > 256)
		{
			int num = 256;
			if ((chars[num - 1] & 0xFC00) == 55296)
			{
				num--;
			}
			int offset;
			byte[] buffer = GetBuffer(num * 2, out offset);
			Advance(UnsafeGetUnicodeChars(chars, num, buffer, offset));
			charCount -= num;
			chars += num;
		}
		if (charCount > 0)
		{
			int offset2;
			byte[] buffer2 = GetBuffer(charCount * 2, out offset2);
			Advance(UnsafeGetUnicodeChars(chars, charCount, buffer2, offset2));
		}
	}

	protected unsafe int UnsafeGetUnicodeChars(char* chars, int charCount, byte[] buffer, int offset)
	{
		char* ptr = chars + charCount;
		while (chars < ptr)
		{
			char c = *(chars++);
			buffer[offset++] = (byte)c;
			c = (char)((int)c >> 8);
			buffer[offset++] = (byte)c;
		}
		return charCount * 2;
	}

	protected unsafe int UnsafeGetUTF8Length(char* chars, int charCount)
	{
		char* ptr = chars + charCount;
		while (chars < ptr && *chars < '\u0080')
		{
			chars++;
		}
		if (chars == ptr)
		{
			return charCount;
		}
		char[] array = new char[ptr - chars];
		for (int i = 0; i < array.Length; i++)
		{
			array[i] = chars[i];
		}
		return (int)(chars - (ptr - charCount)) + GetByteCount(array);
	}

	protected unsafe int UnsafeGetUTF8Chars(char* chars, int charCount, byte[] buffer, int offset)
	{
		if (charCount > 0)
		{
			fixed (byte* ptr = &buffer[offset])
			{
				byte* ptr2 = ptr;
				byte* ptr3 = ptr2 + (buffer.Length - offset);
				char* ptr4 = chars + charCount;
				do
				{
					IL_0045:
					if (chars < ptr4)
					{
						char c = *chars;
						if (c < '\u0080')
						{
							*ptr2 = (byte)c;
							ptr2++;
							chars++;
							goto IL_0045;
						}
					}
					if (chars >= ptr4)
					{
						break;
					}
					char* ptr5 = chars;
					while (chars < ptr4 && *chars >= '\u0080')
					{
						chars++;
					}
					ptr2 += (_encoding ?? s_UTF8Encoding).GetBytes(ptr5, (int)(chars - ptr5), ptr2, (int)(ptr3 - ptr2));
				}
				while (chars < ptr4);
				return (int)(ptr2 - ptr);
			}
		}
		return 0;
	}

	protected virtual void FlushBuffer()
	{
		if (_offset != 0)
		{
			_stream.Write(_buffer, 0, _offset);
			_offset = 0;
		}
	}

	protected virtual Task FlushBufferAsync()
	{
		if (_offset != 0)
		{
			Task result = _stream.WriteAsync(_buffer, 0, _offset);
			_offset = 0;
			return result;
		}
		return Task.CompletedTask;
	}

	public override void Flush()
	{
		FlushBuffer();
		_stream.Flush();
	}

	public override async Task FlushAsync()
	{
		await FlushBufferAsync().ConfigureAwait(continueOnCapturedContext: false);
		await _stream.FlushAsync().ConfigureAwait(continueOnCapturedContext: false);
	}

	public override void Close()
	{
		if (_stream != null)
		{
			if (_ownsStream)
			{
				_stream.Dispose();
			}
			_stream = null;
		}
	}
}
