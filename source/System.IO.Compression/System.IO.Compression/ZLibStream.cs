using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;

namespace System.IO.Compression;

public sealed class ZLibStream : Stream
{
	private DeflateStream _deflateStream;

	public override bool CanRead => _deflateStream?.CanRead ?? false;

	public override bool CanWrite => _deflateStream?.CanWrite ?? false;

	public override bool CanSeek => false;

	public override long Length
	{
		get
		{
			throw new NotSupportedException();
		}
	}

	public override long Position
	{
		get
		{
			throw new NotSupportedException();
		}
		set
		{
			throw new NotSupportedException();
		}
	}

	public Stream BaseStream => _deflateStream?.BaseStream;

	public ZLibStream(Stream stream, CompressionMode mode)
		: this(stream, mode, leaveOpen: false)
	{
	}

	public ZLibStream(Stream stream, CompressionMode mode, bool leaveOpen)
	{
		_deflateStream = new DeflateStream(stream, mode, leaveOpen, 15, -1L);
	}

	public ZLibStream(Stream stream, CompressionLevel compressionLevel)
		: this(stream, compressionLevel, leaveOpen: false)
	{
	}

	public ZLibStream(Stream stream, CompressionLevel compressionLevel, bool leaveOpen)
	{
		_deflateStream = new DeflateStream(stream, compressionLevel, leaveOpen, 15);
	}

	public override void Flush()
	{
		ThrowIfClosed();
		_deflateStream.Flush();
	}

	public override Task FlushAsync(CancellationToken cancellationToken)
	{
		ThrowIfClosed();
		return _deflateStream.FlushAsync(cancellationToken);
	}

	public override long Seek(long offset, SeekOrigin origin)
	{
		throw new NotSupportedException();
	}

	public override void SetLength(long value)
	{
		throw new NotSupportedException();
	}

	public override int ReadByte()
	{
		ThrowIfClosed();
		return _deflateStream.ReadByte();
	}

	public override IAsyncResult BeginRead(byte[] buffer, int offset, int count, AsyncCallback? asyncCallback, object? asyncState)
	{
		ThrowIfClosed();
		return _deflateStream.BeginRead(buffer, offset, count, asyncCallback, asyncState);
	}

	public override int EndRead(IAsyncResult asyncResult)
	{
		return _deflateStream.EndRead(asyncResult);
	}

	public override int Read(byte[] buffer, int offset, int count)
	{
		ThrowIfClosed();
		return _deflateStream.Read(buffer, offset, count);
	}

	public override int Read(Span<byte> buffer)
	{
		ThrowIfClosed();
		return _deflateStream.ReadCore(buffer);
	}

	public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
	{
		ThrowIfClosed();
		return _deflateStream.ReadAsync(buffer, offset, count, cancellationToken);
	}

	public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default(CancellationToken))
	{
		ThrowIfClosed();
		return _deflateStream.ReadAsyncMemory(buffer, cancellationToken);
	}

	public override IAsyncResult BeginWrite(byte[] buffer, int offset, int count, AsyncCallback? asyncCallback, object? asyncState)
	{
		ThrowIfClosed();
		return _deflateStream.BeginWrite(buffer, offset, count, asyncCallback, asyncState);
	}

	public override void EndWrite(IAsyncResult asyncResult)
	{
		_deflateStream.EndWrite(asyncResult);
	}

	public override void Write(byte[] buffer, int offset, int count)
	{
		ThrowIfClosed();
		_deflateStream.Write(buffer, offset, count);
	}

	public override void Write(ReadOnlySpan<byte> buffer)
	{
		ThrowIfClosed();
		_deflateStream.WriteCore(buffer);
	}

	public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
	{
		ThrowIfClosed();
		return _deflateStream.WriteAsync(buffer, offset, count, cancellationToken);
	}

	public override ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default(CancellationToken))
	{
		ThrowIfClosed();
		return _deflateStream.WriteAsyncMemory(buffer, cancellationToken);
	}

	public override void WriteByte(byte value)
	{
		ThrowIfClosed();
		_deflateStream.WriteByte(value);
	}

	public override void CopyTo(Stream destination, int bufferSize)
	{
		ThrowIfClosed();
		_deflateStream.CopyTo(destination, bufferSize);
	}

	public override Task CopyToAsync(Stream destination, int bufferSize, CancellationToken cancellationToken)
	{
		ThrowIfClosed();
		return _deflateStream.CopyToAsync(destination, bufferSize, cancellationToken);
	}

	protected override void Dispose(bool disposing)
	{
		try
		{
			if (disposing)
			{
				_deflateStream?.Dispose();
			}
			_deflateStream = null;
		}
		finally
		{
			base.Dispose(disposing);
		}
	}

	public override ValueTask DisposeAsync()
	{
		DeflateStream deflateStream = _deflateStream;
		if (deflateStream != null)
		{
			_deflateStream = null;
			return deflateStream.DisposeAsync();
		}
		return default(ValueTask);
	}

	private void ThrowIfClosed()
	{
		if (_deflateStream == null)
		{
			ThrowClosedException();
		}
	}

	[DoesNotReturn]
	private static void ThrowClosedException()
	{
		throw new ObjectDisposedException("ZLibStream", System.SR.ObjectDisposed_StreamClosed);
	}
}
