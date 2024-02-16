using System.Threading;
using System.Threading.Tasks;
using Microsoft.Win32.SafeHandles;

namespace System.IO.Strategies;

internal sealed class DerivedFileStreamStrategy : FileStreamStrategy
{
	private readonly FileStreamStrategy _strategy;

	private readonly FileStream _fileStream;

	public override bool CanRead => _strategy.CanRead;

	public override bool CanWrite => _strategy.CanWrite;

	public override bool CanSeek => _strategy.CanSeek;

	public override long Length => _strategy.Length;

	public override long Position
	{
		get
		{
			return _strategy.Position;
		}
		set
		{
			_strategy.Position = value;
		}
	}

	internal override bool IsAsync => _strategy.IsAsync;

	internal override string Name => _strategy.Name;

	internal override SafeFileHandle SafeFileHandle
	{
		get
		{
			_fileStream.Flush(flushToDisk: false);
			return _strategy.SafeFileHandle;
		}
	}

	internal override bool IsClosed => _strategy.IsClosed;

	internal DerivedFileStreamStrategy(FileStream fileStream, FileStreamStrategy strategy)
	{
		_fileStream = fileStream;
		_strategy = strategy;
	}

	~DerivedFileStreamStrategy()
	{
		_fileStream.DisposeInternal(disposing: false);
	}

	internal override void Lock(long position, long length)
	{
		_strategy.Lock(position, length);
	}

	internal override void Unlock(long position, long length)
	{
		_strategy.Unlock(position, length);
	}

	public override long Seek(long offset, SeekOrigin origin)
	{
		return _strategy.Seek(offset, origin);
	}

	public override void SetLength(long value)
	{
		_strategy.SetLength(value);
	}

	public override int ReadByte()
	{
		return _strategy.ReadByte();
	}

	public override IAsyncResult BeginRead(byte[] buffer, int offset, int count, AsyncCallback callback, object state)
	{
		if (!_strategy.IsAsync)
		{
			return _fileStream.BaseBeginRead(buffer, offset, count, callback, state);
		}
		return _strategy.BeginRead(buffer, offset, count, callback, state);
	}

	public override int EndRead(IAsyncResult asyncResult)
	{
		if (!_strategy.IsAsync)
		{
			return _fileStream.BaseEndRead(asyncResult);
		}
		return _strategy.EndRead(asyncResult);
	}

	public override int Read(byte[] buffer, int offset, int count)
	{
		return _strategy.Read(buffer, offset, count);
	}

	public override int Read(Span<byte> buffer)
	{
		return _fileStream.BaseRead(buffer);
	}

	public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
	{
		return _fileStream.BaseReadAsync(buffer, offset, count, cancellationToken);
	}

	public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default(CancellationToken))
	{
		return _fileStream.BaseReadAsync(buffer, cancellationToken);
	}

	public override IAsyncResult BeginWrite(byte[] buffer, int offset, int count, AsyncCallback callback, object state)
	{
		if (!_strategy.IsAsync)
		{
			return _fileStream.BaseBeginWrite(buffer, offset, count, callback, state);
		}
		return _strategy.BeginWrite(buffer, offset, count, callback, state);
	}

	public override void EndWrite(IAsyncResult asyncResult)
	{
		if (_strategy.IsAsync)
		{
			_strategy.EndWrite(asyncResult);
		}
		else
		{
			_fileStream.BaseEndWrite(asyncResult);
		}
	}

	public override void WriteByte(byte value)
	{
		_strategy.WriteByte(value);
	}

	public override void Write(byte[] buffer, int offset, int count)
	{
		_strategy.Write(buffer, offset, count);
	}

	public override void Write(ReadOnlySpan<byte> buffer)
	{
		_fileStream.BaseWrite(buffer);
	}

	public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
	{
		return _fileStream.BaseWriteAsync(buffer, offset, count, cancellationToken);
	}

	public override ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default(CancellationToken))
	{
		return _fileStream.BaseWriteAsync(buffer, cancellationToken);
	}

	public override void Flush()
	{
		throw new InvalidOperationException("FileStream should never call this method.");
	}

	internal override void Flush(bool flushToDisk)
	{
		_strategy.Flush(flushToDisk);
	}

	public override Task FlushAsync(CancellationToken cancellationToken)
	{
		return _fileStream.BaseFlushAsync(cancellationToken);
	}

	public override Task CopyToAsync(Stream destination, int bufferSize, CancellationToken cancellationToken)
	{
		return _fileStream.BaseCopyToAsync(destination, bufferSize, cancellationToken);
	}

	public override ValueTask DisposeAsync()
	{
		return _fileStream.BaseDisposeAsync();
	}

	internal override void DisposeInternal(bool disposing)
	{
		_strategy.DisposeInternal(disposing);
		if (disposing)
		{
			GC.SuppressFinalize(this);
		}
	}
}
