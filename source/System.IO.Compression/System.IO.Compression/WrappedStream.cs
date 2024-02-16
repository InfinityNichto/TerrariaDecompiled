using System.Threading;
using System.Threading.Tasks;

namespace System.IO.Compression;

internal sealed class WrappedStream : Stream
{
	private readonly Stream _baseStream;

	private readonly bool _closeBaseStream;

	private readonly Action<ZipArchiveEntry> _onClosed;

	private readonly ZipArchiveEntry _zipArchiveEntry;

	private bool _isDisposed;

	public override long Length
	{
		get
		{
			ThrowIfDisposed();
			return _baseStream.Length;
		}
	}

	public override long Position
	{
		get
		{
			ThrowIfDisposed();
			return _baseStream.Position;
		}
		set
		{
			ThrowIfDisposed();
			ThrowIfCantSeek();
			_baseStream.Position = value;
		}
	}

	public override bool CanRead
	{
		get
		{
			if (!_isDisposed)
			{
				return _baseStream.CanRead;
			}
			return false;
		}
	}

	public override bool CanSeek
	{
		get
		{
			if (!_isDisposed)
			{
				return _baseStream.CanSeek;
			}
			return false;
		}
	}

	public override bool CanWrite
	{
		get
		{
			if (!_isDisposed)
			{
				return _baseStream.CanWrite;
			}
			return false;
		}
	}

	internal WrappedStream(Stream baseStream, bool closeBaseStream)
		: this(baseStream, closeBaseStream, null, null)
	{
	}

	private WrappedStream(Stream baseStream, bool closeBaseStream, ZipArchiveEntry entry, Action<ZipArchiveEntry> onClosed)
	{
		_baseStream = baseStream;
		_closeBaseStream = closeBaseStream;
		_onClosed = onClosed;
		_zipArchiveEntry = entry;
		_isDisposed = false;
	}

	internal WrappedStream(Stream baseStream, ZipArchiveEntry entry, Action<ZipArchiveEntry> onClosed)
		: this(baseStream, closeBaseStream: false, entry, onClosed)
	{
	}

	private void ThrowIfDisposed()
	{
		if (_isDisposed)
		{
			throw new ObjectDisposedException(GetType().ToString(), System.SR.HiddenStreamName);
		}
	}

	private void ThrowIfCantRead()
	{
		if (!CanRead)
		{
			throw new NotSupportedException(System.SR.ReadingNotSupported);
		}
	}

	private void ThrowIfCantWrite()
	{
		if (!CanWrite)
		{
			throw new NotSupportedException(System.SR.WritingNotSupported);
		}
	}

	private void ThrowIfCantSeek()
	{
		if (!CanSeek)
		{
			throw new NotSupportedException(System.SR.SeekingNotSupported);
		}
	}

	public override int Read(byte[] buffer, int offset, int count)
	{
		ThrowIfDisposed();
		ThrowIfCantRead();
		return _baseStream.Read(buffer, offset, count);
	}

	public override int Read(Span<byte> buffer)
	{
		ThrowIfDisposed();
		ThrowIfCantRead();
		return _baseStream.Read(buffer);
	}

	public override int ReadByte()
	{
		ThrowIfDisposed();
		ThrowIfCantRead();
		return _baseStream.ReadByte();
	}

	public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
	{
		ThrowIfDisposed();
		ThrowIfCantRead();
		return _baseStream.ReadAsync(buffer, offset, count, cancellationToken);
	}

	public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default(CancellationToken))
	{
		ThrowIfDisposed();
		ThrowIfCantRead();
		return _baseStream.ReadAsync(buffer, cancellationToken);
	}

	public override long Seek(long offset, SeekOrigin origin)
	{
		ThrowIfDisposed();
		ThrowIfCantSeek();
		return _baseStream.Seek(offset, origin);
	}

	public override void SetLength(long value)
	{
		ThrowIfDisposed();
		ThrowIfCantSeek();
		ThrowIfCantWrite();
		_baseStream.SetLength(value);
	}

	public override void Write(byte[] buffer, int offset, int count)
	{
		ThrowIfDisposed();
		ThrowIfCantWrite();
		_baseStream.Write(buffer, offset, count);
	}

	public override void Write(ReadOnlySpan<byte> source)
	{
		ThrowIfDisposed();
		ThrowIfCantWrite();
		_baseStream.Write(source);
	}

	public override void WriteByte(byte value)
	{
		ThrowIfDisposed();
		ThrowIfCantWrite();
		_baseStream.WriteByte(value);
	}

	public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
	{
		ThrowIfDisposed();
		ThrowIfCantWrite();
		return _baseStream.WriteAsync(buffer, offset, count, cancellationToken);
	}

	public override ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default(CancellationToken))
	{
		ThrowIfDisposed();
		ThrowIfCantWrite();
		return _baseStream.WriteAsync(buffer, cancellationToken);
	}

	public override void Flush()
	{
		ThrowIfDisposed();
		ThrowIfCantWrite();
		_baseStream.Flush();
	}

	public override Task FlushAsync(CancellationToken cancellationToken)
	{
		ThrowIfDisposed();
		ThrowIfCantWrite();
		return _baseStream.FlushAsync(cancellationToken);
	}

	protected override void Dispose(bool disposing)
	{
		if (disposing && !_isDisposed)
		{
			_onClosed?.Invoke(_zipArchiveEntry);
			if (_closeBaseStream)
			{
				_baseStream.Dispose();
			}
			_isDisposed = true;
		}
		base.Dispose(disposing);
	}
}
