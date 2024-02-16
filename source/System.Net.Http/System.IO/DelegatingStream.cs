using System.Threading;
using System.Threading.Tasks;

namespace System.IO;

internal abstract class DelegatingStream : Stream
{
	private readonly Stream _innerStream;

	public override bool CanRead => _innerStream.CanRead;

	public override bool CanSeek => _innerStream.CanSeek;

	public override bool CanWrite => _innerStream.CanWrite;

	public override long Length => _innerStream.Length;

	public override long Position
	{
		get
		{
			return _innerStream.Position;
		}
		set
		{
			_innerStream.Position = value;
		}
	}

	public override int ReadTimeout
	{
		get
		{
			return _innerStream.ReadTimeout;
		}
		set
		{
			_innerStream.ReadTimeout = value;
		}
	}

	public override bool CanTimeout => _innerStream.CanTimeout;

	public override int WriteTimeout
	{
		get
		{
			return _innerStream.WriteTimeout;
		}
		set
		{
			_innerStream.WriteTimeout = value;
		}
	}

	protected DelegatingStream(Stream innerStream)
	{
		_innerStream = innerStream;
	}

	protected override void Dispose(bool disposing)
	{
		if (disposing)
		{
			_innerStream.Dispose();
		}
		base.Dispose(disposing);
	}

	public override ValueTask DisposeAsync()
	{
		return _innerStream.DisposeAsync();
	}

	public override long Seek(long offset, SeekOrigin origin)
	{
		return _innerStream.Seek(offset, origin);
	}

	public override int Read(byte[] buffer, int offset, int count)
	{
		return _innerStream.Read(buffer, offset, count);
	}

	public override int Read(Span<byte> buffer)
	{
		return _innerStream.Read(buffer);
	}

	public override int ReadByte()
	{
		return _innerStream.ReadByte();
	}

	public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
	{
		return _innerStream.ReadAsync(buffer, offset, count, cancellationToken);
	}

	public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default(CancellationToken))
	{
		return _innerStream.ReadAsync(buffer, cancellationToken);
	}

	public override IAsyncResult BeginRead(byte[] buffer, int offset, int count, AsyncCallback callback, object state)
	{
		return _innerStream.BeginRead(buffer, offset, count, callback, state);
	}

	public override int EndRead(IAsyncResult asyncResult)
	{
		return _innerStream.EndRead(asyncResult);
	}

	public override void Flush()
	{
		_innerStream.Flush();
	}

	public override Task FlushAsync(CancellationToken cancellationToken)
	{
		return _innerStream.FlushAsync(cancellationToken);
	}

	public override void SetLength(long value)
	{
		_innerStream.SetLength(value);
	}

	public override void Write(byte[] buffer, int offset, int count)
	{
		_innerStream.Write(buffer, offset, count);
	}

	public override void Write(ReadOnlySpan<byte> buffer)
	{
		_innerStream.Write(buffer);
	}

	public override void WriteByte(byte value)
	{
		_innerStream.WriteByte(value);
	}

	public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
	{
		return _innerStream.WriteAsync(buffer, offset, count, cancellationToken);
	}

	public override ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default(CancellationToken))
	{
		return _innerStream.WriteAsync(buffer, cancellationToken);
	}

	public override IAsyncResult BeginWrite(byte[] buffer, int offset, int count, AsyncCallback callback, object state)
	{
		return _innerStream.BeginWrite(buffer, offset, count, callback, state);
	}

	public override void EndWrite(IAsyncResult asyncResult)
	{
		_innerStream.EndWrite(asyncResult);
	}

	public override Task CopyToAsync(Stream destination, int bufferSize, CancellationToken cancellationToken)
	{
		return _innerStream.CopyToAsync(destination, bufferSize, cancellationToken);
	}
}
