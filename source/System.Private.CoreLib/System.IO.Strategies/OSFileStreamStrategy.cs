using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Win32.SafeHandles;

namespace System.IO.Strategies;

internal abstract class OSFileStreamStrategy : FileStreamStrategy
{
	protected readonly SafeFileHandle _fileHandle;

	private readonly FileAccess _access;

	protected long _filePosition;

	private long _length = -1L;

	private long _appendStart;

	private bool _lengthCanBeCached;

	internal override bool IsAsync => _fileHandle.IsAsync;

	public sealed override bool CanSeek => _fileHandle.CanSeek;

	public sealed override bool CanRead
	{
		get
		{
			if (!_fileHandle.IsClosed)
			{
				return (_access & FileAccess.Read) != 0;
			}
			return false;
		}
	}

	public sealed override bool CanWrite
	{
		get
		{
			if (!_fileHandle.IsClosed)
			{
				return (_access & FileAccess.Write) != 0;
			}
			return false;
		}
	}

	public sealed override long Length
	{
		get
		{
			if (!LengthCachingSupported)
			{
				return RandomAccess.GetFileLength(_fileHandle);
			}
			if (_length < 0)
			{
				_length = RandomAccess.GetFileLength(_fileHandle);
			}
			return _length;
		}
	}

	private bool LengthCachingSupported
	{
		get
		{
			OperatingSystem.IsWindows();
			return _lengthCanBeCached;
		}
	}

	public sealed override long Position
	{
		get
		{
			return _filePosition;
		}
		set
		{
			_filePosition = value;
		}
	}

	internal sealed override string Name => _fileHandle.Path ?? SR.IO_UnknownFileName;

	internal sealed override bool IsClosed => _fileHandle.IsClosed;

	internal sealed override SafeFileHandle SafeFileHandle
	{
		get
		{
			if (CanSeek)
			{
				FileStreamHelpers.Seek(_fileHandle, _filePosition, SeekOrigin.Begin);
			}
			_lengthCanBeCached = false;
			_length = -1L;
			return _fileHandle;
		}
	}

	internal OSFileStreamStrategy(SafeFileHandle handle, FileAccess access)
	{
		_access = access;
		handle.EnsureThreadPoolBindingInitialized();
		if (handle.CanSeek)
		{
			_filePosition = FileStreamHelpers.Seek(handle, 0L, SeekOrigin.Current);
		}
		else
		{
			_filePosition = 0L;
		}
		_fileHandle = handle;
	}

	internal OSFileStreamStrategy(string path, FileMode mode, FileAccess access, FileShare share, FileOptions options, long preallocationSize)
	{
		string fullPath = Path.GetFullPath(path);
		_access = access;
		_lengthCanBeCached = (share & FileShare.Write) == 0 && (access & FileAccess.Write) == 0;
		_fileHandle = SafeFileHandle.Open(fullPath, mode, access, share, options, preallocationSize);
		try
		{
			if (mode == FileMode.Append && CanSeek)
			{
				_appendStart = (_filePosition = Length);
			}
			else
			{
				_appendStart = -1L;
			}
		}
		catch
		{
			_fileHandle.Dispose();
			_fileHandle = null;
			throw;
		}
	}

	internal void OnIncompleteOperation(int expectedBytesTransferred, int actualBytesTransferred)
	{
		Interlocked.Add(ref _filePosition, actualBytesTransferred - expectedBytesTransferred);
	}

	public sealed override ValueTask DisposeAsync()
	{
		if (_fileHandle != null && !_fileHandle.IsClosed)
		{
			_fileHandle.ThreadPoolBinding?.Dispose();
			_fileHandle.Dispose();
		}
		return ValueTask.CompletedTask;
	}

	internal sealed override void DisposeInternal(bool disposing)
	{
		Dispose(disposing);
	}

	protected sealed override void Dispose(bool disposing)
	{
		if (disposing && _fileHandle != null && !_fileHandle.IsClosed)
		{
			_fileHandle.ThreadPoolBinding?.Dispose();
			_fileHandle.Dispose();
		}
	}

	public sealed override void Flush()
	{
	}

	public sealed override Task FlushAsync(CancellationToken cancellationToken)
	{
		return Task.CompletedTask;
	}

	internal sealed override void Flush(bool flushToDisk)
	{
		if (flushToDisk && CanWrite)
		{
			FileStreamHelpers.FlushToDisk(_fileHandle);
		}
	}

	public sealed override long Seek(long offset, SeekOrigin origin)
	{
		if (origin < SeekOrigin.Begin || origin > SeekOrigin.End)
		{
			throw new ArgumentException(SR.Argument_InvalidSeekOrigin, "origin");
		}
		if (_fileHandle.IsClosed)
		{
			ThrowHelper.ThrowObjectDisposedException_FileClosed();
		}
		if (!CanSeek)
		{
			ThrowHelper.ThrowNotSupportedException_UnseekableStream();
		}
		long filePosition = _filePosition;
		long num = origin switch
		{
			SeekOrigin.Begin => offset, 
			SeekOrigin.End => Length + offset, 
			_ => _filePosition + offset, 
		};
		if (num >= 0)
		{
			_filePosition = num;
		}
		else
		{
			FileStreamHelpers.ThrowInvalidArgument(_fileHandle);
		}
		if (_appendStart != -1 && num < _appendStart)
		{
			_filePosition = filePosition;
			throw new IOException(SR.IO_SeekAppendOverwrite);
		}
		return num;
	}

	internal sealed override void Lock(long position, long length)
	{
		FileStreamHelpers.Lock(_fileHandle, CanWrite, position, length);
	}

	internal sealed override void Unlock(long position, long length)
	{
		FileStreamHelpers.Unlock(_fileHandle, position, length);
	}

	public sealed override void SetLength(long value)
	{
		if (_appendStart != -1 && value < _appendStart)
		{
			throw new IOException(SR.IO_SetLengthAppendTruncate);
		}
		SetLengthCore(value);
	}

	protected void SetLengthCore(long value)
	{
		FileStreamHelpers.SetFileLength(_fileHandle, value);
		if (LengthCachingSupported)
		{
			_length = value;
		}
		if (_filePosition > value)
		{
			_filePosition = value;
		}
	}

	public unsafe sealed override int ReadByte()
	{
		System.Runtime.CompilerServices.Unsafe.SkipInit(out byte result);
		if (Read(new Span<byte>(&result, 1)) == 0)
		{
			return -1;
		}
		return result;
	}

	public sealed override int Read(byte[] buffer, int offset, int count)
	{
		return Read(new Span<byte>(buffer, offset, count));
	}

	public sealed override int Read(Span<byte> buffer)
	{
		if (_fileHandle.IsClosed)
		{
			ThrowHelper.ThrowObjectDisposedException_FileClosed();
		}
		else if ((_access & FileAccess.Read) == 0)
		{
			ThrowHelper.ThrowNotSupportedException_UnreadableStream();
		}
		int num = RandomAccess.ReadAtOffset(_fileHandle, buffer, _filePosition);
		_filePosition += num;
		return num;
	}

	public unsafe sealed override void WriteByte(byte value)
	{
		Write(new ReadOnlySpan<byte>(&value, 1));
	}

	public override void Write(byte[] buffer, int offset, int count)
	{
		Write(new ReadOnlySpan<byte>(buffer, offset, count));
	}

	public sealed override void Write(ReadOnlySpan<byte> buffer)
	{
		if (_fileHandle.IsClosed)
		{
			ThrowHelper.ThrowObjectDisposedException_FileClosed();
		}
		else if ((_access & FileAccess.Write) == 0)
		{
			ThrowHelper.ThrowNotSupportedException_UnwritableStream();
		}
		RandomAccess.WriteAtOffset(_fileHandle, buffer, _filePosition);
		_filePosition += buffer.Length;
	}

	public sealed override IAsyncResult BeginWrite(byte[] buffer, int offset, int count, AsyncCallback callback, object state)
	{
		return TaskToApm.Begin(WriteAsync(buffer, offset, count), callback, state);
	}

	public sealed override void EndWrite(IAsyncResult asyncResult)
	{
		TaskToApm.End(asyncResult);
	}

	public sealed override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
	{
		return WriteAsync(new ReadOnlyMemory<byte>(buffer, offset, count), cancellationToken).AsTask();
	}

	public sealed override ValueTask WriteAsync(ReadOnlyMemory<byte> source, CancellationToken cancellationToken)
	{
		long fileOffset = (CanSeek ? (Interlocked.Add(ref _filePosition, source.Length) - source.Length) : (-1));
		return RandomAccess.WriteAtOffsetAsync(_fileHandle, source, fileOffset, cancellationToken, this);
	}

	public sealed override IAsyncResult BeginRead(byte[] buffer, int offset, int count, AsyncCallback callback, object state)
	{
		return TaskToApm.Begin(ReadAsync(buffer, offset, count), callback, state);
	}

	public sealed override int EndRead(IAsyncResult asyncResult)
	{
		return TaskToApm.End<int>(asyncResult);
	}

	public sealed override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
	{
		return ReadAsync(new Memory<byte>(buffer, offset, count), cancellationToken).AsTask();
	}

	public sealed override ValueTask<int> ReadAsync(Memory<byte> destination, CancellationToken cancellationToken)
	{
		if (!CanSeek)
		{
			return RandomAccess.ReadAtOffsetAsync(_fileHandle, destination, -1L, cancellationToken);
		}
		if (LengthCachingSupported && _length >= 0 && Volatile.Read(ref _filePosition) >= _length)
		{
			return ValueTask.FromResult(0);
		}
		long fileOffset = Interlocked.Add(ref _filePosition, destination.Length) - destination.Length;
		return RandomAccess.ReadAtOffsetAsync(_fileHandle, destination, fileOffset, cancellationToken, this);
	}
}
