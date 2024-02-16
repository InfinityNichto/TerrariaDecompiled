using System.Buffers;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Win32.SafeHandles;

namespace System.IO.Strategies;

internal sealed class Net5CompatFileStreamStrategy : FileStreamStrategy
{
	private class CompletionSource : TaskCompletionSource<int>
	{
		internal unsafe static readonly IOCompletionCallback s_ioCallback = IOCallback;

		private static Action<object> s_cancelCallback;

		private readonly Net5CompatFileStreamStrategy _strategy;

		private readonly int _numBufferedBytes;

		private CancellationTokenRegistration _cancellationRegistration;

		private unsafe NativeOverlapped* _overlapped;

		private long _result;

		internal unsafe NativeOverlapped* Overlapped => _overlapped;

		internal unsafe CompletionSource(Net5CompatFileStreamStrategy strategy, PreAllocatedOverlapped preallocatedOverlapped, int numBufferedBytes, byte[] bytes)
			: base(TaskCreationOptions.RunContinuationsAsynchronously)
		{
			_numBufferedBytes = numBufferedBytes;
			_strategy = strategy;
			_result = 0L;
			_overlapped = ((bytes != null && strategy.CompareExchangeCurrentOverlappedOwner(this, null) == null) ? strategy._fileHandle.ThreadPoolBinding.AllocateNativeOverlapped(preallocatedOverlapped) : strategy._fileHandle.ThreadPoolBinding.AllocateNativeOverlapped(s_ioCallback, this, bytes));
		}

		public void SetCompletedSynchronously(int numBytes)
		{
			ReleaseNativeResource();
			TrySetResult(numBytes + _numBufferedBytes);
		}

		public unsafe void RegisterForCancellation(CancellationToken cancellationToken)
		{
			if (_overlapped != null)
			{
				Action<object> callback = Cancel;
				long num = Interlocked.CompareExchange(ref _result, 17179869184L, 0L);
				switch (num)
				{
				case 0L:
					_cancellationRegistration = cancellationToken.UnsafeRegister(callback, this);
					num = Interlocked.Exchange(ref _result, 0L);
					break;
				default:
					num = Interlocked.Exchange(ref _result, 0L);
					break;
				case 34359738368L:
					break;
				}
				if (num != 0L && num != 34359738368L && num != 17179869184L)
				{
					CompleteCallback((ulong)num);
				}
			}
		}

		internal unsafe virtual void ReleaseNativeResource()
		{
			_cancellationRegistration.Dispose();
			if (_overlapped != null)
			{
				_strategy._fileHandle.ThreadPoolBinding.FreeNativeOverlapped(_overlapped);
				_overlapped = null;
			}
			_strategy.CompareExchangeCurrentOverlappedOwner(null, this);
		}

		internal unsafe static void IOCallback(uint errorCode, uint numBytes, NativeOverlapped* pOverlapped)
		{
			object nativeOverlappedState = ThreadPoolBoundHandle.GetNativeOverlappedState(pOverlapped);
			CompletionSource completionSource = ((!(nativeOverlappedState is Net5CompatFileStreamStrategy net5CompatFileStreamStrategy)) ? ((CompletionSource)nativeOverlappedState) : net5CompatFileStreamStrategy._currentOverlappedOwner);
			CompletionSource completionSource2 = completionSource;
			ulong num = ((errorCode == 0 || errorCode == 109 || errorCode == 232) ? (0x100000000uL | numBytes) : (0x200000000uL | errorCode));
			if (Interlocked.Exchange(ref completionSource2._result, (long)num) == 0L && Interlocked.Exchange(ref completionSource2._result, 34359738368L) != 0L)
			{
				completionSource2.CompleteCallback(num);
			}
		}

		private void CompleteCallback(ulong packedResult)
		{
			CancellationToken token = _cancellationRegistration.Token;
			ReleaseNativeResource();
			long num = (long)packedResult & -4294967296L;
			if (num == 8589934592L)
			{
				int num2 = (int)(packedResult & 0xFFFFFFFFu);
				if (num2 == 995)
				{
					TrySetCanceled(token.IsCancellationRequested ? token : new CancellationToken(canceled: true));
					return;
				}
				Exception exceptionForWin32Error = Win32Marshal.GetExceptionForWin32Error(num2);
				exceptionForWin32Error.SetCurrentStackTrace();
				TrySetException(exceptionForWin32Error);
			}
			else
			{
				TrySetResult((int)(packedResult & 0xFFFFFFFFu) + _numBufferedBytes);
			}
		}

		private unsafe static void Cancel(object state)
		{
			CompletionSource completionSource = (CompletionSource)state;
			if (!completionSource._strategy._fileHandle.IsInvalid && !Interop.Kernel32.CancelIoEx(completionSource._strategy._fileHandle, completionSource._overlapped))
			{
				int lastPInvokeError = Marshal.GetLastPInvokeError();
				if (lastPInvokeError != 1168)
				{
					throw Win32Marshal.GetExceptionForWin32Error(lastPInvokeError);
				}
			}
		}

		public static CompletionSource Create(Net5CompatFileStreamStrategy strategy, PreAllocatedOverlapped preallocatedOverlapped, int numBufferedBytesRead, ReadOnlyMemory<byte> memory)
		{
			if (preallocatedOverlapped == null || !MemoryMarshal.TryGetArray(memory, out var segment) || !preallocatedOverlapped.IsUserObject(segment.Array))
			{
				return new MemoryFileStreamCompletionSource(strategy, numBufferedBytesRead, memory);
			}
			return new CompletionSource(strategy, preallocatedOverlapped, numBufferedBytesRead, segment.Array);
		}
	}

	private sealed class MemoryFileStreamCompletionSource : CompletionSource
	{
		private MemoryHandle _handle;

		internal MemoryFileStreamCompletionSource(Net5CompatFileStreamStrategy strategy, int numBufferedBytes, ReadOnlyMemory<byte> memory)
			: base(strategy, null, numBufferedBytes, null)
		{
			_handle = memory.Pin();
		}

		internal override void ReleaseNativeResource()
		{
			_handle.Dispose();
			base.ReleaseNativeResource();
		}
	}

	private byte[] _buffer;

	private readonly int _bufferLength;

	private readonly SafeFileHandle _fileHandle;

	private readonly FileAccess _access;

	private int _readPos;

	private int _readLength;

	private int _writePos;

	private readonly bool _useAsyncIO;

	private Task<int> _lastSynchronouslyCompletedTask;

	private long _filePosition;

	private bool _exposedHandle;

	private long _appendStart;

	private Task _activeBufferOperation = Task.CompletedTask;

	private PreAllocatedOverlapped _preallocatedOverlapped;

	private CompletionSource _currentOverlappedOwner;

	public override bool CanRead
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

	public override bool CanWrite
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

	internal override SafeFileHandle SafeFileHandle
	{
		get
		{
			Flush();
			_exposedHandle = true;
			return _fileHandle;
		}
	}

	internal override string Name => _fileHandle.Path ?? SR.IO_UnknownFileName;

	internal override bool IsAsync => _useAsyncIO;

	public override long Position
	{
		get
		{
			VerifyOSHandlePosition();
			return _filePosition - _readLength + _readPos + _writePos;
		}
		set
		{
			Seek(value, SeekOrigin.Begin);
		}
	}

	internal override bool IsClosed => _fileHandle.IsClosed;

	private bool HasActiveBufferOperation => !_activeBufferOperation.IsCompleted;

	public override bool CanSeek => _fileHandle.CanSeek;

	public override long Length
	{
		get
		{
			long num = RandomAccess.GetFileLength(_fileHandle);
			if (_writePos > 0 && _filePosition + _writePos > num)
			{
				num = _writePos + _filePosition;
			}
			return num;
		}
	}

	internal Net5CompatFileStreamStrategy(SafeFileHandle handle, FileAccess access, int bufferSize, bool isAsync)
	{
		_exposedHandle = true;
		_bufferLength = bufferSize;
		InitFromHandle(handle, access, isAsync);
		_access = access;
		_useAsyncIO = isAsync;
		_fileHandle = handle;
	}

	internal Net5CompatFileStreamStrategy(string path, FileMode mode, FileAccess access, FileShare share, int bufferSize, FileOptions options, long preallocationSize)
	{
		string fullPath = Path.GetFullPath(path);
		_access = access;
		_bufferLength = bufferSize;
		if ((options & FileOptions.Asynchronous) != 0)
		{
			_useAsyncIO = true;
		}
		_fileHandle = SafeFileHandle.Open(fullPath, mode, access, share, options, preallocationSize);
		try
		{
			Init(mode, path, options);
		}
		catch
		{
			_fileHandle.Dispose();
			_fileHandle = null;
			throw;
		}
	}

	~Net5CompatFileStreamStrategy()
	{
		Dispose(disposing: false);
	}

	internal override void DisposeInternal(bool disposing)
	{
		Dispose(disposing);
	}

	public override Task FlushAsync(CancellationToken cancellationToken)
	{
		try
		{
			FlushInternalBuffer();
		}
		catch (Exception exception)
		{
			return Task.FromException(exception);
		}
		return Task.CompletedTask;
	}

	public override int Read(byte[] buffer, int offset, int count)
	{
		if (!_useAsyncIO)
		{
			return ReadSpan(new Span<byte>(buffer, offset, count));
		}
		return ReadAsyncTask(buffer, offset, count, CancellationToken.None).GetAwaiter().GetResult();
	}

	public override int Read(Span<byte> buffer)
	{
		if (!_useAsyncIO)
		{
			if (_fileHandle.IsClosed)
			{
				ThrowHelper.ThrowObjectDisposedException_FileClosed();
			}
			return ReadSpan(buffer);
		}
		return base.Read(buffer);
	}

	public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
	{
		if (!_useAsyncIO)
		{
			return BeginReadInternal(buffer, offset, count, null, null, serializeAsynchronously: true, apm: false);
		}
		return ReadAsyncTask(buffer, offset, count, cancellationToken);
	}

	public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default(CancellationToken))
	{
		if (!_useAsyncIO)
		{
			if (!MemoryMarshal.TryGetArray((ReadOnlyMemory<byte>)buffer, out ArraySegment<byte> segment))
			{
				return base.ReadAsync(buffer, cancellationToken);
			}
			return new ValueTask<int>(BeginReadInternal(segment.Array, segment.Offset, segment.Count, null, null, serializeAsynchronously: true, apm: false));
		}
		int synchronousResult;
		Task<int> task = ReadAsyncInternal(buffer, cancellationToken, out synchronousResult);
		if (task == null)
		{
			return new ValueTask<int>(synchronousResult);
		}
		return new ValueTask<int>(task);
	}

	private Task<int> ReadAsyncTask(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
	{
		int synchronousResult;
		Task<int> task = ReadAsyncInternal(new Memory<byte>(buffer, offset, count), cancellationToken, out synchronousResult);
		if (task == null)
		{
			task = _lastSynchronouslyCompletedTask;
			if (task == null || task.Result != synchronousResult)
			{
				task = (_lastSynchronouslyCompletedTask = Task.FromResult(synchronousResult));
			}
		}
		return task;
	}

	public override void Write(byte[] buffer, int offset, int count)
	{
		if (_useAsyncIO)
		{
			WriteAsyncInternal(new ReadOnlyMemory<byte>(buffer, offset, count), CancellationToken.None).AsTask().GetAwaiter().GetResult();
		}
		else
		{
			WriteSpan(new ReadOnlySpan<byte>(buffer, offset, count));
		}
	}

	public override void Write(ReadOnlySpan<byte> buffer)
	{
		if (!_useAsyncIO)
		{
			if (_fileHandle.IsClosed)
			{
				ThrowHelper.ThrowObjectDisposedException_FileClosed();
			}
			WriteSpan(buffer);
		}
		else
		{
			base.Write(buffer);
		}
	}

	public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
	{
		if (!_useAsyncIO)
		{
			return BeginWriteInternal(buffer, offset, count, null, null, serializeAsynchronously: true, apm: false);
		}
		return WriteAsyncInternal(new ReadOnlyMemory<byte>(buffer, offset, count), cancellationToken).AsTask();
	}

	public override ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default(CancellationToken))
	{
		if (!_useAsyncIO)
		{
			if (!MemoryMarshal.TryGetArray(buffer, out var segment))
			{
				return base.WriteAsync(buffer, cancellationToken);
			}
			return new ValueTask(BeginWriteInternal(segment.Array, segment.Offset, segment.Count, null, null, serializeAsynchronously: true, apm: false));
		}
		return WriteAsyncInternal(buffer, cancellationToken);
	}

	public override void Flush()
	{
		Flush(flushToDisk: false);
	}

	internal override void Flush(bool flushToDisk)
	{
		FlushInternalBuffer();
		if (flushToDisk && CanWrite)
		{
			FileStreamHelpers.FlushToDisk(_fileHandle);
		}
	}

	private void VerifyOSHandlePosition()
	{
		if (!_exposedHandle || !CanSeek)
		{
			return;
		}
		long filePosition = _filePosition;
		long num = SeekCore(_fileHandle, 0L, SeekOrigin.Current);
		if (filePosition != num)
		{
			_readPos = (_readLength = 0);
			if (_writePos > 0)
			{
				_writePos = 0;
				throw new IOException(SR.IO_FileStreamHandlePosition);
			}
		}
	}

	private void PrepareForReading()
	{
		if (_fileHandle.IsClosed)
		{
			ThrowHelper.ThrowObjectDisposedException_FileClosed();
		}
		if (_readLength == 0 && !CanRead)
		{
			ThrowHelper.ThrowNotSupportedException_UnreadableStream();
		}
	}

	private long SeekCore(SafeFileHandle fileHandle, long offset, SeekOrigin origin, bool closeInvalidHandle = false)
	{
		return _filePosition = FileStreamHelpers.Seek(fileHandle, offset, origin, closeInvalidHandle);
	}

	private byte[] GetBuffer()
	{
		if (_buffer == null)
		{
			_buffer = new byte[_bufferLength];
			OnBufferAllocated();
		}
		return _buffer;
	}

	private void FlushInternalBuffer()
	{
		if (_writePos > 0)
		{
			FlushWriteBuffer();
		}
		else if (_readPos < _readLength && CanSeek)
		{
			FlushReadBuffer();
		}
	}

	private void FlushReadBuffer()
	{
		int num = _readPos - _readLength;
		if (num != 0)
		{
			SeekCore(_fileHandle, num, SeekOrigin.Current);
		}
		_readPos = (_readLength = 0);
	}

	public override int ReadByte()
	{
		PrepareForReading();
		byte[] buffer = GetBuffer();
		if (_readPos == _readLength)
		{
			FlushWriteBuffer();
			_readLength = FillReadBufferForReadByte();
			_readPos = 0;
			if (_readLength == 0)
			{
				return -1;
			}
		}
		return buffer[_readPos++];
	}

	public override void WriteByte(byte value)
	{
		PrepareForWriting();
		if (_writePos == _bufferLength)
		{
			FlushWriteBufferForWriteByte();
		}
		GetBuffer()[_writePos++] = value;
	}

	private void PrepareForWriting()
	{
		if (_fileHandle.IsClosed)
		{
			ThrowHelper.ThrowObjectDisposedException_FileClosed();
		}
		if (_writePos == 0)
		{
			if (!CanWrite)
			{
				ThrowHelper.ThrowNotSupportedException_UnwritableStream();
			}
			FlushReadBuffer();
		}
	}

	private void OnBufferAllocated()
	{
		if (_useAsyncIO)
		{
			_preallocatedOverlapped = PreAllocatedOverlapped.UnsafeCreate(CompletionSource.s_ioCallback, this, _buffer);
		}
	}

	public override IAsyncResult BeginRead(byte[] buffer, int offset, int count, AsyncCallback callback, object state)
	{
		if (!_useAsyncIO)
		{
			return base.BeginRead(buffer, offset, count, callback, state);
		}
		return TaskToApm.Begin(ReadAsyncTask(buffer, offset, count, CancellationToken.None), callback, state);
	}

	public override IAsyncResult BeginWrite(byte[] buffer, int offset, int count, AsyncCallback callback, object state)
	{
		if (!_useAsyncIO)
		{
			return base.BeginWrite(buffer, offset, count, callback, state);
		}
		return TaskToApm.Begin(WriteAsyncInternal(new ReadOnlyMemory<byte>(buffer, offset, count), CancellationToken.None).AsTask(), callback, state);
	}

	public override int EndRead(IAsyncResult asyncResult)
	{
		if (!_useAsyncIO)
		{
			return base.EndRead(asyncResult);
		}
		return TaskToApm.End<int>(asyncResult);
	}

	public override void EndWrite(IAsyncResult asyncResult)
	{
		if (!_useAsyncIO)
		{
			base.EndWrite(asyncResult);
		}
		else
		{
			TaskToApm.End(asyncResult);
		}
	}

	private void Init(FileMode mode, string originalPath, FileOptions options)
	{
		if (mode == FileMode.Append)
		{
			_appendStart = SeekCore(_fileHandle, 0L, SeekOrigin.End);
		}
		else
		{
			_appendStart = -1L;
		}
	}

	private void InitFromHandle(SafeFileHandle handle, FileAccess access, bool useAsyncIO)
	{
		InitFromHandleImpl(handle, useAsyncIO);
	}

	private void InitFromHandleImpl(SafeFileHandle handle, bool useAsyncIO)
	{
		handle.EnsureThreadPoolBindingInitialized();
		if (handle.CanSeek)
		{
			SeekCore(handle, 0L, SeekOrigin.Current);
		}
		else
		{
			_filePosition = 0L;
		}
	}

	protected override void Dispose(bool disposing)
	{
		try
		{
			if (_fileHandle != null && !_fileHandle.IsClosed && _writePos > 0)
			{
				try
				{
					FlushWriteBuffer(!disposing);
					return;
				}
				catch (Exception e) when (!disposing && FileStreamHelpers.IsIoRelatedException(e))
				{
					return;
				}
			}
		}
		finally
		{
			if (_fileHandle != null && !_fileHandle.IsClosed)
			{
				_fileHandle.ThreadPoolBinding?.Dispose();
				_fileHandle.Dispose();
			}
			_preallocatedOverlapped?.Dispose();
		}
	}

	public override async ValueTask DisposeAsync()
	{
		try
		{
			if (_fileHandle != null && !_fileHandle.IsClosed && _writePos > 0)
			{
				await FlushAsync(default(CancellationToken)).ConfigureAwait(continueOnCapturedContext: false);
			}
		}
		finally
		{
			if (_fileHandle != null && !_fileHandle.IsClosed)
			{
				_fileHandle.ThreadPoolBinding?.Dispose();
				_fileHandle.Dispose();
			}
			_preallocatedOverlapped?.Dispose();
			GC.SuppressFinalize(this);
		}
	}

	private Task FlushWriteAsync(CancellationToken cancellationToken)
	{
		if (_writePos == 0)
		{
			return Task.CompletedTask;
		}
		Task task = WriteAsyncInternalCore(new ReadOnlyMemory<byte>(GetBuffer(), 0, _writePos), cancellationToken);
		_writePos = 0;
		_activeBufferOperation = (HasActiveBufferOperation ? Task.WhenAll(_activeBufferOperation, task) : task);
		return task;
	}

	private void FlushWriteBufferForWriteByte()
	{
		FlushWriteBuffer();
	}

	private void FlushWriteBuffer(bool calledFromFinalizer = false)
	{
		if (_writePos == 0)
		{
			return;
		}
		if (_useAsyncIO)
		{
			Task task = FlushWriteAsync(CancellationToken.None);
			if (!calledFromFinalizer)
			{
				task.GetAwaiter().GetResult();
			}
		}
		else
		{
			WriteCore(new ReadOnlySpan<byte>(GetBuffer(), 0, _writePos));
		}
		_writePos = 0;
	}

	public override void SetLength(long value)
	{
		if (_writePos > 0)
		{
			FlushWriteBuffer();
		}
		else if (_readPos < _readLength)
		{
			FlushReadBuffer();
		}
		_readPos = 0;
		_readLength = 0;
		if (_appendStart != -1 && value < _appendStart)
		{
			throw new IOException(SR.IO_SetLengthAppendTruncate);
		}
		SetLengthCore(value);
	}

	private void SetLengthCore(long value)
	{
		VerifyOSHandlePosition();
		FileStreamHelpers.SetFileLength(_fileHandle, value);
		if (_filePosition > value)
		{
			SeekCore(_fileHandle, 0L, SeekOrigin.End);
		}
	}

	private int ReadSpan(Span<byte> destination)
	{
		bool flag = false;
		int num = _readLength - _readPos;
		if (num == 0)
		{
			if (!CanRead)
			{
				ThrowHelper.ThrowNotSupportedException_UnreadableStream();
			}
			if (_writePos > 0)
			{
				FlushWriteBuffer();
			}
			if (!CanSeek || destination.Length >= _bufferLength)
			{
				num = ReadNative(destination);
				_readPos = 0;
				_readLength = 0;
				return num;
			}
			num = ReadNative(GetBuffer());
			if (num == 0)
			{
				return 0;
			}
			flag = num < _bufferLength;
			_readPos = 0;
			_readLength = num;
		}
		if (num > destination.Length)
		{
			num = destination.Length;
		}
		new ReadOnlySpan<byte>(GetBuffer(), _readPos, num).CopyTo(destination);
		_readPos += num;
		if (_fileHandle.CanSeek && num < destination.Length && !flag)
		{
			int num2 = ReadNative(destination.Slice(num));
			num += num2;
			_readPos = 0;
			_readLength = 0;
		}
		return num;
	}

	private int FillReadBufferForReadByte()
	{
		if (!_useAsyncIO)
		{
			return ReadNative(_buffer);
		}
		return ReadNativeAsync(new Memory<byte>(_buffer), 0, CancellationToken.None).GetAwaiter().GetResult();
	}

	private unsafe int ReadNative(Span<byte> buffer)
	{
		VerifyOSHandlePosition();
		int errorCode;
		int num = ReadFileNative(_fileHandle, buffer, null, out errorCode);
		if (num == -1)
		{
			if (errorCode != 109)
			{
				if (errorCode == 87)
				{
					ThrowHelper.ThrowArgumentException_HandleNotSync("_fileHandle");
				}
				throw Win32Marshal.GetExceptionForWin32Error(errorCode, _fileHandle.Path);
			}
			num = 0;
		}
		_filePosition += num;
		return num;
	}

	public override long Seek(long offset, SeekOrigin origin)
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
		if (_writePos > 0)
		{
			FlushWriteBuffer();
		}
		else if (origin == SeekOrigin.Current)
		{
			offset -= _readLength - _readPos;
		}
		_readPos = (_readLength = 0);
		VerifyOSHandlePosition();
		long num = _filePosition + (_readPos - _readLength);
		long num2 = SeekCore(_fileHandle, offset, origin);
		if (_appendStart != -1 && num2 < _appendStart)
		{
			SeekCore(_fileHandle, num, SeekOrigin.Begin);
			throw new IOException(SR.IO_SeekAppendOverwrite);
		}
		if (_readLength > 0)
		{
			if (num == num2)
			{
				if (_readPos > 0)
				{
					Buffer.BlockCopy(GetBuffer(), _readPos, GetBuffer(), 0, _readLength - _readPos);
					_readLength -= _readPos;
					_readPos = 0;
				}
				if (_readLength > 0)
				{
					SeekCore(_fileHandle, _readLength, SeekOrigin.Current);
				}
			}
			else if (num - _readPos < num2 && num2 < num + _readLength - _readPos)
			{
				int num3 = (int)(num2 - num);
				Buffer.BlockCopy(GetBuffer(), _readPos + num3, GetBuffer(), 0, _readLength - (_readPos + num3));
				_readLength -= _readPos + num3;
				_readPos = 0;
				if (_readLength > 0)
				{
					SeekCore(_fileHandle, _readLength, SeekOrigin.Current);
				}
			}
			else
			{
				_readPos = 0;
				_readLength = 0;
			}
		}
		return num2;
	}

	private CompletionSource CompareExchangeCurrentOverlappedOwner(CompletionSource newSource, CompletionSource existingSource)
	{
		return Interlocked.CompareExchange(ref _currentOverlappedOwner, newSource, existingSource);
	}

	private void WriteSpan(ReadOnlySpan<byte> source)
	{
		if (_writePos == 0)
		{
			if (!CanWrite)
			{
				ThrowHelper.ThrowNotSupportedException_UnwritableStream();
			}
			if (_readPos < _readLength)
			{
				FlushReadBuffer();
			}
			_readPos = 0;
			_readLength = 0;
		}
		if (_writePos > 0)
		{
			int num = _bufferLength - _writePos;
			if (num > 0)
			{
				if (num >= source.Length)
				{
					source.CopyTo(GetBuffer().AsSpan(_writePos));
					_writePos += source.Length;
					return;
				}
				source.Slice(0, num).CopyTo(GetBuffer().AsSpan(_writePos));
				_writePos += num;
				source = source.Slice(num);
			}
			WriteCore(new ReadOnlySpan<byte>(GetBuffer(), 0, _writePos));
			_writePos = 0;
		}
		if (source.Length >= _bufferLength)
		{
			WriteCore(source);
		}
		else if (source.Length != 0)
		{
			source.CopyTo(GetBuffer().AsSpan(_writePos));
			_writePos = source.Length;
		}
	}

	private unsafe void WriteCore(ReadOnlySpan<byte> source)
	{
		VerifyOSHandlePosition();
		int errorCode;
		int num = WriteFileNative(_fileHandle, source, null, out errorCode);
		if (num == -1)
		{
			switch (errorCode)
			{
			case 232:
				break;
			case 87:
				throw new IOException(SR.IO_FileTooLongOrHandleNotSync);
			default:
				throw Win32Marshal.GetExceptionForWin32Error(errorCode, _fileHandle.Path);
			}
			num = 0;
		}
		_filePosition += num;
	}

	private Task<int> ReadAsyncInternal(Memory<byte> destination, CancellationToken cancellationToken, out int synchronousResult)
	{
		if (!CanRead)
		{
			ThrowHelper.ThrowNotSupportedException_UnreadableStream();
		}
		if (!_fileHandle.CanSeek)
		{
			if (_readPos < _readLength)
			{
				int num = Math.Min(_readLength - _readPos, destination.Length);
				new Span<byte>(GetBuffer(), _readPos, num).CopyTo(destination.Span);
				_readPos += num;
				synchronousResult = num;
				return null;
			}
			synchronousResult = 0;
			return ReadNativeAsync(destination, 0, cancellationToken);
		}
		if (_writePos > 0)
		{
			FlushWriteBuffer();
		}
		if (_readPos == _readLength)
		{
			if (destination.Length < _bufferLength)
			{
				Task<int> task = ReadNativeAsync(new Memory<byte>(GetBuffer()), 0, cancellationToken);
				_readLength = task.GetAwaiter().GetResult();
				int num2 = Math.Min(_readLength, destination.Length);
				new Span<byte>(GetBuffer(), 0, num2).CopyTo(destination.Span);
				_readPos = num2;
				synchronousResult = num2;
				return null;
			}
			_readPos = 0;
			_readLength = 0;
			synchronousResult = 0;
			return ReadNativeAsync(destination, 0, cancellationToken);
		}
		int num3 = Math.Min(_readLength - _readPos, destination.Length);
		new Span<byte>(GetBuffer(), _readPos, num3).CopyTo(destination.Span);
		_readPos += num3;
		if (num3 == destination.Length)
		{
			synchronousResult = num3;
			return null;
		}
		_readPos = 0;
		_readLength = 0;
		synchronousResult = 0;
		return ReadNativeAsync(destination.Slice(num3), num3, cancellationToken);
	}

	private unsafe Task<int> ReadNativeAsync(Memory<byte> destination, int numBufferedBytesRead, CancellationToken cancellationToken)
	{
		CompletionSource completionSource = CompletionSource.Create(this, _preallocatedOverlapped, numBufferedBytesRead, destination);
		NativeOverlapped* overlapped = completionSource.Overlapped;
		if (CanSeek)
		{
			long length = Length;
			VerifyOSHandlePosition();
			if (_filePosition + destination.Length > length)
			{
				destination = ((_filePosition > length) ? default(Memory<byte>) : destination.Slice(0, (int)(length - _filePosition)));
			}
			overlapped->OffsetLow = (int)_filePosition;
			overlapped->OffsetHigh = (int)(_filePosition >> 32);
			SeekCore(_fileHandle, destination.Length, SeekOrigin.Current);
		}
		int errorCode;
		int num = ReadFileNative(_fileHandle, destination.Span, overlapped, out errorCode);
		if (num == -1)
		{
			switch (errorCode)
			{
			case 109:
				overlapped->InternalLow = IntPtr.Zero;
				completionSource.SetCompletedSynchronously(0);
				break;
			default:
				if (!_fileHandle.IsClosed && CanSeek)
				{
					SeekCore(_fileHandle, 0L, SeekOrigin.Current);
				}
				completionSource.ReleaseNativeResource();
				if (errorCode == 38)
				{
					ThrowHelper.ThrowEndOfFileException();
					break;
				}
				throw Win32Marshal.GetExceptionForWin32Error(errorCode, _fileHandle.Path);
			case 997:
				if (cancellationToken.CanBeCanceled)
				{
					completionSource.RegisterForCancellation(cancellationToken);
				}
				break;
			}
		}
		return completionSource.Task;
	}

	private ValueTask WriteAsyncInternal(ReadOnlyMemory<byte> source, CancellationToken cancellationToken)
	{
		if (!CanWrite)
		{
			ThrowHelper.ThrowNotSupportedException_UnwritableStream();
		}
		bool flag = false;
		if (_fileHandle.CanSeek)
		{
			if (_writePos == 0)
			{
				if (_readPos < _readLength)
				{
					FlushReadBuffer();
				}
				_readPos = 0;
				_readLength = 0;
			}
			int num = _bufferLength - _writePos;
			if (source.Length < _bufferLength && !HasActiveBufferOperation && source.Length <= num)
			{
				source.Span.CopyTo(new Span<byte>(GetBuffer(), _writePos, source.Length));
				_writePos += source.Length;
				flag = true;
				if (source.Length != num)
				{
					return default(ValueTask);
				}
			}
		}
		Task task = null;
		if (_writePos > 0)
		{
			task = FlushWriteAsync(cancellationToken);
			if (flag || task.IsFaulted || task.IsCanceled)
			{
				return new ValueTask(task);
			}
		}
		Task task2 = WriteAsyncInternalCore(source, cancellationToken);
		return new ValueTask((task == null || task.Status == TaskStatus.RanToCompletion) ? task2 : ((task2.Status == TaskStatus.RanToCompletion) ? task : Task.WhenAll(task, task2)));
	}

	private unsafe Task WriteAsyncInternalCore(ReadOnlyMemory<byte> source, CancellationToken cancellationToken)
	{
		CompletionSource completionSource = CompletionSource.Create(this, _preallocatedOverlapped, 0, source);
		NativeOverlapped* overlapped = completionSource.Overlapped;
		if (CanSeek)
		{
			long length = Length;
			VerifyOSHandlePosition();
			if (_filePosition + source.Length > length)
			{
				SetLengthCore(_filePosition + source.Length);
			}
			overlapped->OffsetLow = (int)_filePosition;
			overlapped->OffsetHigh = (int)(_filePosition >> 32);
			SeekCore(_fileHandle, source.Length, SeekOrigin.Current);
		}
		int errorCode;
		int num = WriteFileNative(_fileHandle, source.Span, overlapped, out errorCode);
		if (num == -1)
		{
			switch (errorCode)
			{
			case 232:
				completionSource.SetCompletedSynchronously(0);
				return Task.CompletedTask;
			default:
				if (!_fileHandle.IsClosed && CanSeek)
				{
					SeekCore(_fileHandle, 0L, SeekOrigin.Current);
				}
				completionSource.ReleaseNativeResource();
				if (errorCode == 38)
				{
					ThrowHelper.ThrowEndOfFileException();
					break;
				}
				throw Win32Marshal.GetExceptionForWin32Error(errorCode, _fileHandle.Path);
			case 997:
				if (cancellationToken.CanBeCanceled)
				{
					completionSource.RegisterForCancellation(cancellationToken);
				}
				break;
			}
		}
		return completionSource.Task;
	}

	private unsafe int ReadFileNative(SafeFileHandle handle, Span<byte> bytes, NativeOverlapped* overlapped, out int errorCode)
	{
		return FileStreamHelpers.ReadFileNative(handle, bytes, overlapped, out errorCode);
	}

	private unsafe int WriteFileNative(SafeFileHandle handle, ReadOnlySpan<byte> buffer, NativeOverlapped* overlapped, out int errorCode)
	{
		int numBytesWritten = 0;
		int num;
		fixed (byte* bytes = &MemoryMarshal.GetReference(buffer))
		{
			num = ((overlapped == null) ? Interop.Kernel32.WriteFile(handle, bytes, buffer.Length, out numBytesWritten, overlapped) : Interop.Kernel32.WriteFile(handle, bytes, buffer.Length, IntPtr.Zero, overlapped));
		}
		if (num == 0)
		{
			errorCode = FileStreamHelpers.GetLastWin32ErrorAndDisposeHandleIfInvalid(handle);
			return -1;
		}
		errorCode = 0;
		return numBytesWritten;
	}

	public override Task CopyToAsync(Stream destination, int bufferSize, CancellationToken cancellationToken)
	{
		if (!_useAsyncIO)
		{
			return base.CopyToAsync(destination, bufferSize, cancellationToken);
		}
		if (_fileHandle.IsClosed)
		{
			ThrowHelper.ThrowObjectDisposedException_FileClosed();
		}
		if (!CanRead)
		{
			ThrowHelper.ThrowNotSupportedException_UnreadableStream();
		}
		if (cancellationToken.IsCancellationRequested)
		{
			return Task.FromCanceled<int>(cancellationToken);
		}
		return AsyncModeCopyToAsync(destination, bufferSize, cancellationToken);
	}

	private async Task AsyncModeCopyToAsync(Stream destination, int bufferSize, CancellationToken cancellationToken)
	{
		if (_writePos > 0)
		{
			await FlushWriteAsync(cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
		}
		if (GetBuffer() != null)
		{
			int num = _readLength - _readPos;
			if (num > 0)
			{
				await destination.WriteAsync(new ReadOnlyMemory<byte>(GetBuffer(), _readPos, num), cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
				_readPos = (_readLength = 0);
			}
		}
		bool canSeek = CanSeek;
		if (canSeek)
		{
			VerifyOSHandlePosition();
		}
		try
		{
			await FileStreamHelpers.AsyncModeCopyToAsync(_fileHandle, canSeek, _filePosition, destination, bufferSize, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
		}
		finally
		{
			if (!_fileHandle.IsClosed && CanSeek)
			{
				SeekCore(_fileHandle, 0L, SeekOrigin.End);
			}
		}
	}

	internal override void Lock(long position, long length)
	{
		FileStreamHelpers.Lock(_fileHandle, CanWrite, position, length);
	}

	internal override void Unlock(long position, long length)
	{
		FileStreamHelpers.Unlock(_fileHandle, position, length);
	}
}
