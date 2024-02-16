using System.Threading;
using System.Threading.Tasks;

namespace System.IO;

internal sealed class UnmanagedMemoryStreamWrapper : MemoryStream
{
	private readonly UnmanagedMemoryStream _unmanagedStream;

	public override bool CanRead => _unmanagedStream.CanRead;

	public override bool CanSeek => _unmanagedStream.CanSeek;

	public override bool CanWrite => _unmanagedStream.CanWrite;

	public override int Capacity
	{
		get
		{
			return (int)_unmanagedStream.Capacity;
		}
		set
		{
			throw new IOException(SR.IO_FixedCapacity);
		}
	}

	public override long Length => _unmanagedStream.Length;

	public override long Position
	{
		get
		{
			return _unmanagedStream.Position;
		}
		set
		{
			_unmanagedStream.Position = value;
		}
	}

	internal UnmanagedMemoryStreamWrapper(UnmanagedMemoryStream stream)
	{
		_unmanagedStream = stream;
	}

	protected override void Dispose(bool disposing)
	{
		try
		{
			if (disposing)
			{
				_unmanagedStream.Dispose();
			}
		}
		finally
		{
			base.Dispose(disposing);
		}
	}

	public override void Flush()
	{
		_unmanagedStream.Flush();
	}

	public override byte[] GetBuffer()
	{
		throw new UnauthorizedAccessException(SR.UnauthorizedAccess_MemStreamBuffer);
	}

	public override bool TryGetBuffer(out ArraySegment<byte> buffer)
	{
		buffer = default(ArraySegment<byte>);
		return false;
	}

	public override int Read(byte[] buffer, int offset, int count)
	{
		return _unmanagedStream.Read(buffer, offset, count);
	}

	public override int Read(Span<byte> buffer)
	{
		return _unmanagedStream.Read(buffer);
	}

	public override int ReadByte()
	{
		return _unmanagedStream.ReadByte();
	}

	public override long Seek(long offset, SeekOrigin loc)
	{
		return _unmanagedStream.Seek(offset, loc);
	}

	public override byte[] ToArray()
	{
		byte[] array = new byte[_unmanagedStream.Length];
		_unmanagedStream.Read(array, 0, (int)_unmanagedStream.Length);
		return array;
	}

	public override void Write(byte[] buffer, int offset, int count)
	{
		_unmanagedStream.Write(buffer, offset, count);
	}

	public override void Write(ReadOnlySpan<byte> buffer)
	{
		_unmanagedStream.Write(buffer);
	}

	public override void WriteByte(byte value)
	{
		_unmanagedStream.WriteByte(value);
	}

	public override void WriteTo(Stream stream)
	{
		if (stream == null)
		{
			throw new ArgumentNullException("stream", SR.ArgumentNull_Stream);
		}
		byte[] array = ToArray();
		stream.Write(array, 0, array.Length);
	}

	public override void SetLength(long value)
	{
		base.SetLength(value);
	}

	public override Task CopyToAsync(Stream destination, int bufferSize, CancellationToken cancellationToken)
	{
		if (destination == null)
		{
			throw new ArgumentNullException("destination");
		}
		if (bufferSize <= 0)
		{
			throw new ArgumentOutOfRangeException("bufferSize", SR.ArgumentOutOfRange_NeedPosNum);
		}
		if (!CanRead && !CanWrite)
		{
			ThrowHelper.ThrowObjectDisposedException_StreamClosed(null);
		}
		if (!destination.CanRead && !destination.CanWrite)
		{
			ThrowHelper.ThrowObjectDisposedException_StreamClosed("destination");
		}
		if (!CanRead)
		{
			ThrowHelper.ThrowNotSupportedException_UnreadableStream();
		}
		if (!destination.CanWrite)
		{
			ThrowHelper.ThrowNotSupportedException_UnwritableStream();
		}
		return _unmanagedStream.CopyToAsync(destination, bufferSize, cancellationToken);
	}

	public override Task FlushAsync(CancellationToken cancellationToken)
	{
		return _unmanagedStream.FlushAsync(cancellationToken);
	}

	public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
	{
		return _unmanagedStream.ReadAsync(buffer, offset, count, cancellationToken);
	}

	public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default(CancellationToken))
	{
		return _unmanagedStream.ReadAsync(buffer, cancellationToken);
	}

	public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
	{
		return _unmanagedStream.WriteAsync(buffer, offset, count, cancellationToken);
	}

	public override ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default(CancellationToken))
	{
		return _unmanagedStream.WriteAsync(buffer, cancellationToken);
	}
}
