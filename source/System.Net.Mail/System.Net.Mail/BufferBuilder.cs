using System.Text;

namespace System.Net.Mail;

internal sealed class BufferBuilder
{
	private byte[] _buffer;

	private int _offset;

	internal int Length => _offset;

	internal BufferBuilder()
		: this(256)
	{
	}

	internal BufferBuilder(int initialSize)
	{
		_buffer = new byte[initialSize];
	}

	private void EnsureBuffer(int count)
	{
		if (count > _buffer.Length - _offset)
		{
			byte[] array = new byte[(_buffer.Length * 2 > _buffer.Length + count) ? (_buffer.Length * 2) : (_buffer.Length + count)];
			Buffer.BlockCopy(_buffer, 0, array, 0, _offset);
			_buffer = array;
		}
	}

	internal void Append(byte value)
	{
		EnsureBuffer(1);
		_buffer[_offset++] = value;
	}

	internal void Append(byte[] value)
	{
		Append(value, 0, value.Length);
	}

	internal void Append(byte[] value, int offset, int count)
	{
		EnsureBuffer(count);
		Buffer.BlockCopy(value, offset, _buffer, _offset, count);
		_offset += count;
	}

	internal void Append(string value)
	{
		Append(value, allowUnicode: false);
	}

	internal void Append(string value, bool allowUnicode)
	{
		if (!string.IsNullOrEmpty(value))
		{
			Append(value, 0, value.Length, allowUnicode);
		}
	}

	internal void Append(string value, int offset, int count, bool allowUnicode)
	{
		if (allowUnicode)
		{
			int byteCount = Encoding.UTF8.GetByteCount(value, offset, count);
			EnsureBuffer(byteCount);
			Encoding.UTF8.GetBytes(value, offset, count, _buffer, _offset);
			_offset += byteCount;
		}
		else
		{
			Append(value, offset, count);
		}
	}

	internal void Append(string value, int offset, int count)
	{
		EnsureBuffer(count);
		for (int i = 0; i < count; i++)
		{
			char c = value[offset + i];
			if (c > 'ÿ')
			{
				throw new FormatException(System.SR.Format(System.SR.MailHeaderFieldInvalidCharacter, c));
			}
			_buffer[_offset + i] = (byte)c;
		}
		_offset += count;
	}

	internal byte[] GetBuffer()
	{
		return _buffer;
	}

	internal void Reset()
	{
		_offset = 0;
	}
}
