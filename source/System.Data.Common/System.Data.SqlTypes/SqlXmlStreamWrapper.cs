using System.Data.Common;
using System.IO;

namespace System.Data.SqlTypes;

internal sealed class SqlXmlStreamWrapper : Stream
{
	private readonly Stream _stream;

	private long _lPosition;

	private bool _isClosed;

	public override bool CanRead
	{
		get
		{
			if (IsStreamClosed())
			{
				return false;
			}
			return _stream.CanRead;
		}
	}

	public override bool CanSeek
	{
		get
		{
			if (IsStreamClosed())
			{
				return false;
			}
			return _stream.CanSeek;
		}
	}

	public override bool CanWrite
	{
		get
		{
			if (IsStreamClosed())
			{
				return false;
			}
			return _stream.CanWrite;
		}
	}

	public override long Length
	{
		get
		{
			ThrowIfStreamClosed("get_Length");
			ThrowIfStreamCannotSeek("get_Length");
			return _stream.Length;
		}
	}

	public override long Position
	{
		get
		{
			ThrowIfStreamClosed("get_Position");
			ThrowIfStreamCannotSeek("get_Position");
			return _lPosition;
		}
		set
		{
			ThrowIfStreamClosed("set_Position");
			ThrowIfStreamCannotSeek("set_Position");
			if (value < 0 || value > _stream.Length)
			{
				throw new ArgumentOutOfRangeException("value");
			}
			_lPosition = value;
		}
	}

	internal SqlXmlStreamWrapper(Stream stream)
	{
		_stream = stream;
		_lPosition = 0L;
		_isClosed = false;
	}

	public override long Seek(long offset, SeekOrigin origin)
	{
		long num = 0L;
		ThrowIfStreamClosed("Seek");
		ThrowIfStreamCannotSeek("Seek");
		switch (origin)
		{
		case SeekOrigin.Begin:
			if (offset < 0 || offset > _stream.Length)
			{
				throw new ArgumentOutOfRangeException("offset");
			}
			_lPosition = offset;
			break;
		case SeekOrigin.Current:
			num = _lPosition + offset;
			if (num < 0 || num > _stream.Length)
			{
				throw new ArgumentOutOfRangeException("offset");
			}
			_lPosition = num;
			break;
		case SeekOrigin.End:
			num = _stream.Length + offset;
			if (num < 0 || num > _stream.Length)
			{
				throw new ArgumentOutOfRangeException("offset");
			}
			_lPosition = num;
			break;
		default:
			throw ADP.InvalidSeekOrigin("offset");
		}
		return _lPosition;
	}

	public override int Read(byte[] buffer, int offset, int count)
	{
		ThrowIfStreamClosed("Read");
		ThrowIfStreamCannotRead("Read");
		if (buffer == null)
		{
			throw new ArgumentNullException("buffer");
		}
		if (offset < 0 || offset > buffer.Length)
		{
			throw new ArgumentOutOfRangeException("offset");
		}
		if (count < 0 || count > buffer.Length - offset)
		{
			throw new ArgumentOutOfRangeException("count");
		}
		if (_stream.CanSeek && _stream.Position != _lPosition)
		{
			_stream.Seek(_lPosition, SeekOrigin.Begin);
		}
		int num = _stream.Read(buffer, offset, count);
		_lPosition += num;
		return num;
	}

	public override void Write(byte[] buffer, int offset, int count)
	{
		ThrowIfStreamClosed("Write");
		ThrowIfStreamCannotWrite("Write");
		if (buffer == null)
		{
			throw new ArgumentNullException("buffer");
		}
		if (offset < 0 || offset > buffer.Length)
		{
			throw new ArgumentOutOfRangeException("offset");
		}
		if (count < 0 || count > buffer.Length - offset)
		{
			throw new ArgumentOutOfRangeException("count");
		}
		if (_stream.CanSeek && _stream.Position != _lPosition)
		{
			_stream.Seek(_lPosition, SeekOrigin.Begin);
		}
		_stream.Write(buffer, offset, count);
		_lPosition += count;
	}

	public override int ReadByte()
	{
		ThrowIfStreamClosed("ReadByte");
		ThrowIfStreamCannotRead("ReadByte");
		if (_stream.CanSeek && _lPosition >= _stream.Length)
		{
			return -1;
		}
		if (_stream.CanSeek && _stream.Position != _lPosition)
		{
			_stream.Seek(_lPosition, SeekOrigin.Begin);
		}
		int result = _stream.ReadByte();
		_lPosition++;
		return result;
	}

	public override void WriteByte(byte value)
	{
		ThrowIfStreamClosed("WriteByte");
		ThrowIfStreamCannotWrite("WriteByte");
		if (_stream.CanSeek && _stream.Position != _lPosition)
		{
			_stream.Seek(_lPosition, SeekOrigin.Begin);
		}
		_stream.WriteByte(value);
		_lPosition++;
	}

	public override void SetLength(long value)
	{
		ThrowIfStreamClosed("SetLength");
		ThrowIfStreamCannotSeek("SetLength");
		_stream.SetLength(value);
		if (_lPosition > value)
		{
			_lPosition = value;
		}
	}

	public override void Flush()
	{
		if (_stream != null)
		{
			_stream.Flush();
		}
	}

	protected override void Dispose(bool disposing)
	{
		try
		{
			_isClosed = true;
		}
		finally
		{
			base.Dispose(disposing);
		}
	}

	private void ThrowIfStreamCannotSeek(string method)
	{
		if (!_stream.CanSeek)
		{
			throw new NotSupportedException(SQLResource.InvalidOpStreamNonSeekable(method));
		}
	}

	private void ThrowIfStreamCannotRead(string method)
	{
		if (!_stream.CanRead)
		{
			throw new NotSupportedException(SQLResource.InvalidOpStreamNonReadable(method));
		}
	}

	private void ThrowIfStreamCannotWrite(string method)
	{
		if (!_stream.CanWrite)
		{
			throw new NotSupportedException(SQLResource.InvalidOpStreamNonWritable(method));
		}
	}

	private void ThrowIfStreamClosed(string method)
	{
		if (IsStreamClosed())
		{
			throw new ObjectDisposedException(SQLResource.InvalidOpStreamClosed(method));
		}
	}

	private bool IsStreamClosed()
	{
		if (_isClosed || _stream == null || (!_stream.CanRead && !_stream.CanWrite && !_stream.CanSeek))
		{
			return true;
		}
		return false;
	}
}
