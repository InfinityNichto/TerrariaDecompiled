using System.ComponentModel;
using System.IO.Strategies;
using System.Runtime.Versioning;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Win32.SafeHandles;

namespace System.IO;

public class FileStream : Stream
{
	private readonly FileStreamStrategy _strategy;

	[Obsolete("FileStream.Handle has been deprecated. Use FileStream's SafeFileHandle property instead.")]
	public virtual IntPtr Handle => _strategy.Handle;

	public override bool CanRead => _strategy.CanRead;

	public override bool CanWrite => _strategy.CanWrite;

	public virtual SafeFileHandle SafeFileHandle => _strategy.SafeFileHandle;

	public virtual string Name => _strategy.Name;

	public virtual bool IsAsync => _strategy.IsAsync;

	public override long Length
	{
		get
		{
			if (_strategy.IsClosed)
			{
				ThrowHelper.ThrowObjectDisposedException_FileClosed();
			}
			else if (!CanSeek)
			{
				ThrowHelper.ThrowNotSupportedException_UnseekableStream();
			}
			return _strategy.Length;
		}
	}

	public override long Position
	{
		get
		{
			if (_strategy.IsClosed)
			{
				ThrowHelper.ThrowObjectDisposedException_FileClosed();
			}
			else if (!CanSeek)
			{
				ThrowHelper.ThrowNotSupportedException_UnseekableStream();
			}
			return _strategy.Position;
		}
		set
		{
			if (value < 0)
			{
				ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.value, ExceptionResource.ArgumentOutOfRange_NeedNonNegNum);
			}
			_strategy.Seek(value, SeekOrigin.Begin);
		}
	}

	public override bool CanSeek => _strategy.CanSeek;

	[EditorBrowsable(EditorBrowsableState.Never)]
	[Obsolete("This constructor has been deprecated. Use FileStream(SafeFileHandle handle, FileAccess access) instead.")]
	public FileStream(IntPtr handle, FileAccess access)
		: this(handle, access, ownsHandle: true, 4096, isAsync: false)
	{
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	[Obsolete("This constructor has been deprecated. Use FileStream(SafeFileHandle handle, FileAccess access) and optionally make a new SafeFileHandle with ownsHandle=false if needed instead.")]
	public FileStream(IntPtr handle, FileAccess access, bool ownsHandle)
		: this(handle, access, ownsHandle, 4096, isAsync: false)
	{
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	[Obsolete("This constructor has been deprecated. Use FileStream(SafeFileHandle handle, FileAccess access, int bufferSize) and optionally make a new SafeFileHandle with ownsHandle=false if needed instead.")]
	public FileStream(IntPtr handle, FileAccess access, bool ownsHandle, int bufferSize)
		: this(handle, access, ownsHandle, bufferSize, isAsync: false)
	{
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	[Obsolete("This constructor has been deprecated. Use FileStream(SafeFileHandle handle, FileAccess access, int bufferSize, bool isAsync) and optionally make a new SafeFileHandle with ownsHandle=false if needed instead.")]
	public FileStream(IntPtr handle, FileAccess access, bool ownsHandle, int bufferSize, bool isAsync)
	{
		SafeFileHandle safeFileHandle = new SafeFileHandle(handle, ownsHandle);
		try
		{
			ValidateHandle(safeFileHandle, access, bufferSize, isAsync);
			_strategy = FileStreamHelpers.ChooseStrategy(this, safeFileHandle, access, bufferSize, isAsync);
		}
		catch
		{
			GC.SuppressFinalize(safeFileHandle);
			throw;
		}
	}

	private static void ValidateHandle(SafeFileHandle handle, FileAccess access, int bufferSize)
	{
		if (handle.IsInvalid)
		{
			throw new ArgumentException(SR.Arg_InvalidHandle, "handle");
		}
		if (access < FileAccess.Read || access > FileAccess.ReadWrite)
		{
			throw new ArgumentOutOfRangeException("access", SR.ArgumentOutOfRange_Enum);
		}
		if (bufferSize < 0)
		{
			ThrowHelper.ThrowArgumentOutOfRangeException_NeedNonNegNum("bufferSize");
		}
		else if (handle.IsClosed)
		{
			ThrowHelper.ThrowObjectDisposedException_FileClosed();
		}
	}

	private static void ValidateHandle(SafeFileHandle handle, FileAccess access, int bufferSize, bool isAsync)
	{
		ValidateHandle(handle, access, bufferSize);
		if (isAsync && !handle.IsAsync)
		{
			ThrowHelper.ThrowArgumentException_HandleNotAsync("handle");
		}
		else if (!isAsync && handle.IsAsync)
		{
			ThrowHelper.ThrowArgumentException_HandleNotSync("handle");
		}
	}

	public FileStream(SafeFileHandle handle, FileAccess access)
		: this(handle, access, 4096)
	{
	}

	public FileStream(SafeFileHandle handle, FileAccess access, int bufferSize)
	{
		ValidateHandle(handle, access, bufferSize);
		_strategy = FileStreamHelpers.ChooseStrategy(this, handle, access, bufferSize, handle.IsAsync);
	}

	public FileStream(SafeFileHandle handle, FileAccess access, int bufferSize, bool isAsync)
	{
		ValidateHandle(handle, access, bufferSize, isAsync);
		_strategy = FileStreamHelpers.ChooseStrategy(this, handle, access, bufferSize, isAsync);
	}

	public FileStream(string path, FileMode mode)
		: this(path, mode, (mode == FileMode.Append) ? FileAccess.Write : FileAccess.ReadWrite, FileShare.Read, 4096, useAsync: false)
	{
	}

	public FileStream(string path, FileMode mode, FileAccess access)
		: this(path, mode, access, FileShare.Read, 4096, useAsync: false)
	{
	}

	public FileStream(string path, FileMode mode, FileAccess access, FileShare share)
		: this(path, mode, access, share, 4096, useAsync: false)
	{
	}

	public FileStream(string path, FileMode mode, FileAccess access, FileShare share, int bufferSize)
		: this(path, mode, access, share, bufferSize, useAsync: false)
	{
	}

	public FileStream(string path, FileMode mode, FileAccess access, FileShare share, int bufferSize, bool useAsync)
		: this(path, mode, access, share, bufferSize, useAsync ? FileOptions.Asynchronous : FileOptions.None)
	{
	}

	public FileStream(string path, FileMode mode, FileAccess access, FileShare share, int bufferSize, FileOptions options)
		: this(path, mode, access, share, bufferSize, options, 0L)
	{
	}

	public FileStream(string path, FileStreamOptions options)
	{
		if (path == null)
		{
			throw new ArgumentNullException("path", SR.ArgumentNull_Path);
		}
		if (path.Length == 0)
		{
			throw new ArgumentException(SR.Argument_EmptyPath, "path");
		}
		if (options == null)
		{
			throw new ArgumentNullException("options");
		}
		if ((options.Access & FileAccess.Read) != 0 && options.Mode == FileMode.Append)
		{
			throw new ArgumentException(SR.Argument_InvalidAppendMode, "options");
		}
		if ((options.Access & FileAccess.Write) == 0 && (options.Mode == FileMode.Truncate || options.Mode == FileMode.CreateNew || options.Mode == FileMode.Create || options.Mode == FileMode.Append))
		{
			throw new ArgumentException(SR.Format(SR.Argument_InvalidFileModeAndAccessCombo, options.Mode, options.Access), "options");
		}
		if (options.PreallocationSize > 0)
		{
			FileStreamHelpers.ValidateArgumentsForPreallocation(options.Mode, options.Access);
		}
		FileStreamHelpers.SerializationGuard(options.Access);
		_strategy = FileStreamHelpers.ChooseStrategy(this, path, options.Mode, options.Access, options.Share, options.BufferSize, options.Options, options.PreallocationSize);
	}

	private FileStream(string path, FileMode mode, FileAccess access, FileShare share, int bufferSize, FileOptions options, long preallocationSize)
	{
		FileStreamHelpers.ValidateArguments(path, mode, access, share, bufferSize, options, preallocationSize);
		_strategy = FileStreamHelpers.ChooseStrategy(this, path, mode, access, share, bufferSize, options, preallocationSize);
	}

	[UnsupportedOSPlatform("ios")]
	[UnsupportedOSPlatform("macos")]
	[UnsupportedOSPlatform("tvos")]
	public virtual void Lock(long position, long length)
	{
		if (position < 0 || length < 0)
		{
			ThrowHelper.ThrowArgumentOutOfRangeException_NeedNonNegNum((position < 0) ? "position" : "length");
		}
		else if (_strategy.IsClosed)
		{
			ThrowHelper.ThrowObjectDisposedException_FileClosed();
		}
		_strategy.Lock(position, length);
	}

	[UnsupportedOSPlatform("ios")]
	[UnsupportedOSPlatform("macos")]
	[UnsupportedOSPlatform("tvos")]
	public virtual void Unlock(long position, long length)
	{
		if (position < 0 || length < 0)
		{
			ThrowHelper.ThrowArgumentOutOfRangeException_NeedNonNegNum((position < 0) ? "position" : "length");
		}
		else if (_strategy.IsClosed)
		{
			ThrowHelper.ThrowObjectDisposedException_FileClosed();
		}
		_strategy.Unlock(position, length);
	}

	public override Task FlushAsync(CancellationToken cancellationToken)
	{
		if (cancellationToken.IsCancellationRequested)
		{
			return Task.FromCanceled(cancellationToken);
		}
		if (_strategy.IsClosed)
		{
			ThrowHelper.ThrowObjectDisposedException_FileClosed();
		}
		return _strategy.FlushAsync(cancellationToken);
	}

	public override int Read(byte[] buffer, int offset, int count)
	{
		ValidateReadWriteArgs(buffer, offset, count);
		return _strategy.Read(buffer, offset, count);
	}

	public override int Read(Span<byte> buffer)
	{
		return _strategy.Read(buffer);
	}

	public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
	{
		Stream.ValidateBufferArguments(buffer, offset, count);
		if (cancellationToken.IsCancellationRequested)
		{
			return Task.FromCanceled<int>(cancellationToken);
		}
		if (!_strategy.CanRead)
		{
			if (_strategy.IsClosed)
			{
				ThrowHelper.ThrowObjectDisposedException_FileClosed();
			}
			ThrowHelper.ThrowNotSupportedException_UnreadableStream();
		}
		return _strategy.ReadAsync(buffer, offset, count, cancellationToken);
	}

	public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default(CancellationToken))
	{
		if (cancellationToken.IsCancellationRequested)
		{
			return ValueTask.FromCanceled<int>(cancellationToken);
		}
		if (!_strategy.CanRead)
		{
			if (_strategy.IsClosed)
			{
				ThrowHelper.ThrowObjectDisposedException_FileClosed();
			}
			ThrowHelper.ThrowNotSupportedException_UnreadableStream();
		}
		return _strategy.ReadAsync(buffer, cancellationToken);
	}

	public override void Write(byte[] buffer, int offset, int count)
	{
		ValidateReadWriteArgs(buffer, offset, count);
		_strategy.Write(buffer, offset, count);
	}

	public override void Write(ReadOnlySpan<byte> buffer)
	{
		_strategy.Write(buffer);
	}

	public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
	{
		Stream.ValidateBufferArguments(buffer, offset, count);
		if (cancellationToken.IsCancellationRequested)
		{
			return Task.FromCanceled(cancellationToken);
		}
		if (!_strategy.CanWrite)
		{
			if (_strategy.IsClosed)
			{
				ThrowHelper.ThrowObjectDisposedException_FileClosed();
			}
			ThrowHelper.ThrowNotSupportedException_UnwritableStream();
		}
		return _strategy.WriteAsync(buffer, offset, count, cancellationToken);
	}

	public override ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default(CancellationToken))
	{
		if (cancellationToken.IsCancellationRequested)
		{
			return ValueTask.FromCanceled(cancellationToken);
		}
		if (!_strategy.CanWrite)
		{
			if (_strategy.IsClosed)
			{
				ThrowHelper.ThrowObjectDisposedException_FileClosed();
			}
			ThrowHelper.ThrowNotSupportedException_UnwritableStream();
		}
		return _strategy.WriteAsync(buffer, cancellationToken);
	}

	public override void Flush()
	{
		Flush(flushToDisk: false);
	}

	public virtual void Flush(bool flushToDisk)
	{
		if (_strategy.IsClosed)
		{
			ThrowHelper.ThrowObjectDisposedException_FileClosed();
		}
		_strategy.Flush(flushToDisk);
	}

	private void ValidateReadWriteArgs(byte[] buffer, int offset, int count)
	{
		Stream.ValidateBufferArguments(buffer, offset, count);
		if (_strategy.IsClosed)
		{
			ThrowHelper.ThrowObjectDisposedException_FileClosed();
		}
	}

	public override void SetLength(long value)
	{
		if (value < 0)
		{
			ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.value, ExceptionResource.ArgumentOutOfRange_NeedNonNegNum);
		}
		else if (_strategy.IsClosed)
		{
			ThrowHelper.ThrowObjectDisposedException_FileClosed();
		}
		else if (!CanSeek)
		{
			ThrowHelper.ThrowNotSupportedException_UnseekableStream();
		}
		else if (!CanWrite)
		{
			ThrowHelper.ThrowNotSupportedException_UnwritableStream();
		}
		_strategy.SetLength(value);
	}

	public override int ReadByte()
	{
		return _strategy.ReadByte();
	}

	public override void WriteByte(byte value)
	{
		_strategy.WriteByte(value);
	}

	protected override void Dispose(bool disposing)
	{
		_strategy.DisposeInternal(disposing);
	}

	internal void DisposeInternal(bool disposing)
	{
		Dispose(disposing);
	}

	public override ValueTask DisposeAsync()
	{
		return _strategy.DisposeAsync();
	}

	public override void CopyTo(Stream destination, int bufferSize)
	{
		Stream.ValidateCopyToArguments(destination, bufferSize);
		_strategy.CopyTo(destination, bufferSize);
	}

	public override Task CopyToAsync(Stream destination, int bufferSize, CancellationToken cancellationToken)
	{
		Stream.ValidateCopyToArguments(destination, bufferSize);
		return _strategy.CopyToAsync(destination, bufferSize, cancellationToken);
	}

	public override IAsyncResult BeginRead(byte[] buffer, int offset, int count, AsyncCallback? callback, object? state)
	{
		Stream.ValidateBufferArguments(buffer, offset, count);
		if (_strategy.IsClosed)
		{
			ThrowHelper.ThrowObjectDisposedException_FileClosed();
		}
		else if (!CanRead)
		{
			ThrowHelper.ThrowNotSupportedException_UnreadableStream();
		}
		return _strategy.BeginRead(buffer, offset, count, callback, state);
	}

	public override int EndRead(IAsyncResult asyncResult)
	{
		if (asyncResult == null)
		{
			throw new ArgumentNullException("asyncResult");
		}
		return _strategy.EndRead(asyncResult);
	}

	public override IAsyncResult BeginWrite(byte[] buffer, int offset, int count, AsyncCallback? callback, object? state)
	{
		Stream.ValidateBufferArguments(buffer, offset, count);
		if (_strategy.IsClosed)
		{
			ThrowHelper.ThrowObjectDisposedException_FileClosed();
		}
		else if (!CanWrite)
		{
			ThrowHelper.ThrowNotSupportedException_UnwritableStream();
		}
		return _strategy.BeginWrite(buffer, offset, count, callback, state);
	}

	public override void EndWrite(IAsyncResult asyncResult)
	{
		if (asyncResult == null)
		{
			throw new ArgumentNullException("asyncResult");
		}
		_strategy.EndWrite(asyncResult);
	}

	public override long Seek(long offset, SeekOrigin origin)
	{
		return _strategy.Seek(offset, origin);
	}

	internal Task BaseFlushAsync(CancellationToken cancellationToken)
	{
		return base.FlushAsync(cancellationToken);
	}

	internal int BaseRead(Span<byte> buffer)
	{
		return base.Read(buffer);
	}

	internal Task<int> BaseReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
	{
		return base.ReadAsync(buffer, offset, count, cancellationToken);
	}

	internal ValueTask<int> BaseReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default(CancellationToken))
	{
		return base.ReadAsync(buffer, cancellationToken);
	}

	internal void BaseWrite(ReadOnlySpan<byte> buffer)
	{
		base.Write(buffer);
	}

	internal Task BaseWriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
	{
		return base.WriteAsync(buffer, offset, count, cancellationToken);
	}

	internal ValueTask BaseWriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default(CancellationToken))
	{
		return base.WriteAsync(buffer, cancellationToken);
	}

	internal ValueTask BaseDisposeAsync()
	{
		return base.DisposeAsync();
	}

	internal Task BaseCopyToAsync(Stream destination, int bufferSize, CancellationToken cancellationToken)
	{
		return base.CopyToAsync(destination, bufferSize, cancellationToken);
	}

	internal IAsyncResult BaseBeginRead(byte[] buffer, int offset, int count, AsyncCallback callback, object state)
	{
		return base.BeginRead(buffer, offset, count, callback, state);
	}

	internal int BaseEndRead(IAsyncResult asyncResult)
	{
		return base.EndRead(asyncResult);
	}

	internal IAsyncResult BaseBeginWrite(byte[] buffer, int offset, int count, AsyncCallback callback, object state)
	{
		return base.BeginWrite(buffer, offset, count, callback, state);
	}

	internal void BaseEndWrite(IAsyncResult asyncResult)
	{
		base.EndWrite(asyncResult);
	}
}
