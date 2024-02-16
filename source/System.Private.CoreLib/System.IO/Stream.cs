using System.Buffers;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace System.IO;

public abstract class Stream : MarshalByRefObject, IDisposable, IAsyncDisposable
{
	private struct ReadWriteParameters
	{
		internal byte[] Buffer;

		internal int Offset;

		internal int Count;
	}

	private sealed class ReadWriteTask : Task<int>, ITaskCompletionAction
	{
		internal readonly bool _isRead;

		internal readonly bool _apm;

		internal bool _endCalled;

		internal Stream _stream;

		internal byte[] _buffer;

		internal readonly int _offset;

		internal readonly int _count;

		private AsyncCallback _callback;

		private ExecutionContext _context;

		private static ContextCallback s_invokeAsyncCallback;

		bool ITaskCompletionAction.InvokeMayRunArbitraryCode => true;

		internal void ClearBeginState()
		{
			_stream = null;
			_buffer = null;
		}

		public ReadWriteTask(bool isRead, bool apm, Func<object, int> function, object state, Stream stream, byte[] buffer, int offset, int count, AsyncCallback callback)
			: base(function, state, CancellationToken.None, TaskCreationOptions.DenyChildAttach)
		{
			_isRead = isRead;
			_apm = apm;
			_stream = stream;
			_buffer = buffer;
			_offset = offset;
			_count = count;
			if (callback != null)
			{
				_callback = callback;
				_context = ExecutionContext.Capture();
				AddCompletionAction(this);
			}
		}

		private static void InvokeAsyncCallback(object completedTask)
		{
			ReadWriteTask readWriteTask = (ReadWriteTask)completedTask;
			AsyncCallback callback = readWriteTask._callback;
			readWriteTask._callback = null;
			callback(readWriteTask);
		}

		void ITaskCompletionAction.Invoke(Task completingTask)
		{
			ExecutionContext context = _context;
			if (context == null)
			{
				AsyncCallback callback = _callback;
				_callback = null;
				callback(completingTask);
			}
			else
			{
				_context = null;
				ContextCallback callback2 = InvokeAsyncCallback;
				ExecutionContext.RunInternal(context, callback2, this);
			}
		}
	}

	private sealed class NullStream : Stream
	{
		public override bool CanRead => true;

		public override bool CanWrite => true;

		public override bool CanSeek => true;

		public override long Length => 0L;

		public override long Position
		{
			get
			{
				return 0L;
			}
			set
			{
			}
		}

		internal NullStream()
		{
		}

		public override void CopyTo(Stream destination, int bufferSize)
		{
		}

		public override Task CopyToAsync(Stream destination, int bufferSize, CancellationToken cancellationToken)
		{
			if (!cancellationToken.IsCancellationRequested)
			{
				return Task.CompletedTask;
			}
			return Task.FromCanceled(cancellationToken);
		}

		protected override void Dispose(bool disposing)
		{
		}

		public override void Flush()
		{
		}

		public override Task FlushAsync(CancellationToken cancellationToken)
		{
			if (!cancellationToken.IsCancellationRequested)
			{
				return Task.CompletedTask;
			}
			return Task.FromCanceled(cancellationToken);
		}

		public override IAsyncResult BeginRead(byte[] buffer, int offset, int count, AsyncCallback callback, object state)
		{
			return TaskToApm.Begin(Task<int>.s_defaultResultTask, callback, state);
		}

		public override int EndRead(IAsyncResult asyncResult)
		{
			return TaskToApm.End<int>(asyncResult);
		}

		public override IAsyncResult BeginWrite(byte[] buffer, int offset, int count, AsyncCallback callback, object state)
		{
			return TaskToApm.Begin(Task.CompletedTask, callback, state);
		}

		public override void EndWrite(IAsyncResult asyncResult)
		{
			TaskToApm.End(asyncResult);
		}

		public override int Read(byte[] buffer, int offset, int count)
		{
			return 0;
		}

		public override int Read(Span<byte> buffer)
		{
			return 0;
		}

		public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
		{
			if (!cancellationToken.IsCancellationRequested)
			{
				return Task.FromResult(0);
			}
			return Task.FromCanceled<int>(cancellationToken);
		}

		public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken)
		{
			if (!cancellationToken.IsCancellationRequested)
			{
				return default(ValueTask<int>);
			}
			return ValueTask.FromCanceled<int>(cancellationToken);
		}

		public override int ReadByte()
		{
			return -1;
		}

		public override void Write(byte[] buffer, int offset, int count)
		{
		}

		public override void Write(ReadOnlySpan<byte> buffer)
		{
		}

		public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
		{
			if (!cancellationToken.IsCancellationRequested)
			{
				return Task.CompletedTask;
			}
			return Task.FromCanceled(cancellationToken);
		}

		public override ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default(CancellationToken))
		{
			if (!cancellationToken.IsCancellationRequested)
			{
				return default(ValueTask);
			}
			return ValueTask.FromCanceled(cancellationToken);
		}

		public override void WriteByte(byte value)
		{
		}

		public override long Seek(long offset, SeekOrigin origin)
		{
			return 0L;
		}

		public override void SetLength(long length)
		{
		}
	}

	private sealed class SyncStream : Stream, IDisposable
	{
		private readonly Stream _stream;

		public override bool CanRead => _stream.CanRead;

		public override bool CanWrite => _stream.CanWrite;

		public override bool CanSeek => _stream.CanSeek;

		public override bool CanTimeout => _stream.CanTimeout;

		public override long Length
		{
			get
			{
				lock (_stream)
				{
					return _stream.Length;
				}
			}
		}

		public override long Position
		{
			get
			{
				lock (_stream)
				{
					return _stream.Position;
				}
			}
			set
			{
				lock (_stream)
				{
					_stream.Position = value;
				}
			}
		}

		public override int ReadTimeout
		{
			get
			{
				return _stream.ReadTimeout;
			}
			set
			{
				_stream.ReadTimeout = value;
			}
		}

		public override int WriteTimeout
		{
			get
			{
				return _stream.WriteTimeout;
			}
			set
			{
				_stream.WriteTimeout = value;
			}
		}

		internal SyncStream(Stream stream)
		{
			_stream = stream ?? throw new ArgumentNullException("stream");
		}

		public override void Close()
		{
			lock (_stream)
			{
				try
				{
					_stream.Close();
				}
				finally
				{
					base.Dispose(disposing: true);
				}
			}
		}

		protected override void Dispose(bool disposing)
		{
			lock (_stream)
			{
				try
				{
					if (disposing)
					{
						((IDisposable)_stream).Dispose();
					}
				}
				finally
				{
					base.Dispose(disposing);
				}
			}
		}

		public override ValueTask DisposeAsync()
		{
			lock (_stream)
			{
				return _stream.DisposeAsync();
			}
		}

		public override void Flush()
		{
			lock (_stream)
			{
				_stream.Flush();
			}
		}

		public override int Read(byte[] bytes, int offset, int count)
		{
			lock (_stream)
			{
				return _stream.Read(bytes, offset, count);
			}
		}

		public override int Read(Span<byte> buffer)
		{
			lock (_stream)
			{
				return _stream.Read(buffer);
			}
		}

		public override int ReadByte()
		{
			lock (_stream)
			{
				return _stream.ReadByte();
			}
		}

		public override IAsyncResult BeginRead(byte[] buffer, int offset, int count, AsyncCallback callback, object state)
		{
			bool flag = _stream.HasOverriddenBeginEndRead();
			lock (_stream)
			{
				IAsyncResult result;
				if (!flag)
				{
					IAsyncResult asyncResult = _stream.BeginReadInternal(buffer, offset, count, callback, state, serializeAsynchronously: true, apm: true);
					result = asyncResult;
				}
				else
				{
					result = _stream.BeginRead(buffer, offset, count, callback, state);
				}
				return result;
			}
		}

		public override int EndRead(IAsyncResult asyncResult)
		{
			if (asyncResult == null)
			{
				ThrowHelper.ThrowArgumentNullException(ExceptionArgument.asyncResult);
			}
			lock (_stream)
			{
				return _stream.EndRead(asyncResult);
			}
		}

		public override long Seek(long offset, SeekOrigin origin)
		{
			lock (_stream)
			{
				return _stream.Seek(offset, origin);
			}
		}

		public override void SetLength(long length)
		{
			lock (_stream)
			{
				_stream.SetLength(length);
			}
		}

		public override void Write(byte[] bytes, int offset, int count)
		{
			lock (_stream)
			{
				_stream.Write(bytes, offset, count);
			}
		}

		public override void Write(ReadOnlySpan<byte> buffer)
		{
			lock (_stream)
			{
				_stream.Write(buffer);
			}
		}

		public override void WriteByte(byte b)
		{
			lock (_stream)
			{
				_stream.WriteByte(b);
			}
		}

		public override IAsyncResult BeginWrite(byte[] buffer, int offset, int count, AsyncCallback callback, object state)
		{
			bool flag = _stream.HasOverriddenBeginEndWrite();
			lock (_stream)
			{
				IAsyncResult result;
				if (!flag)
				{
					IAsyncResult asyncResult = _stream.BeginWriteInternal(buffer, offset, count, callback, state, serializeAsynchronously: true, apm: true);
					result = asyncResult;
				}
				else
				{
					result = _stream.BeginWrite(buffer, offset, count, callback, state);
				}
				return result;
			}
		}

		public override void EndWrite(IAsyncResult asyncResult)
		{
			if (asyncResult == null)
			{
				ThrowHelper.ThrowArgumentNullException(ExceptionArgument.asyncResult);
			}
			lock (_stream)
			{
				_stream.EndWrite(asyncResult);
			}
		}
	}

	public static readonly Stream Null = new NullStream();

	private protected SemaphoreSlim _asyncActiveSemaphore;

	public abstract bool CanRead { get; }

	public abstract bool CanWrite { get; }

	public abstract bool CanSeek { get; }

	public virtual bool CanTimeout => false;

	public abstract long Length { get; }

	public abstract long Position { get; set; }

	public virtual int ReadTimeout
	{
		get
		{
			throw new InvalidOperationException(SR.InvalidOperation_TimeoutsNotSupported);
		}
		set
		{
			throw new InvalidOperationException(SR.InvalidOperation_TimeoutsNotSupported);
		}
	}

	public virtual int WriteTimeout
	{
		get
		{
			throw new InvalidOperationException(SR.InvalidOperation_TimeoutsNotSupported);
		}
		set
		{
			throw new InvalidOperationException(SR.InvalidOperation_TimeoutsNotSupported);
		}
	}

	[MethodImpl(MethodImplOptions.InternalCall)]
	private extern bool HasOverriddenBeginEndRead();

	[MethodImpl(MethodImplOptions.InternalCall)]
	private extern bool HasOverriddenBeginEndWrite();

	[MemberNotNull("_asyncActiveSemaphore")]
	private protected SemaphoreSlim EnsureAsyncActiveSemaphoreInitialized()
	{
		return Volatile.Read(ref _asyncActiveSemaphore) ?? Interlocked.CompareExchange(ref _asyncActiveSemaphore, new SemaphoreSlim(1, 1), null) ?? _asyncActiveSemaphore;
	}

	public void CopyTo(Stream destination)
	{
		CopyTo(destination, GetCopyBufferSize());
	}

	public virtual void CopyTo(Stream destination, int bufferSize)
	{
		ValidateCopyToArguments(destination, bufferSize);
		if (!CanRead)
		{
			if (CanWrite)
			{
				ThrowHelper.ThrowNotSupportedException_UnreadableStream();
			}
			ThrowHelper.ThrowObjectDisposedException_StreamClosed(GetType().Name);
		}
		byte[] array = ArrayPool<byte>.Shared.Rent(bufferSize);
		try
		{
			int count;
			while ((count = Read(array, 0, array.Length)) != 0)
			{
				destination.Write(array, 0, count);
			}
		}
		finally
		{
			ArrayPool<byte>.Shared.Return(array);
		}
	}

	public Task CopyToAsync(Stream destination)
	{
		return CopyToAsync(destination, GetCopyBufferSize());
	}

	public Task CopyToAsync(Stream destination, int bufferSize)
	{
		return CopyToAsync(destination, bufferSize, CancellationToken.None);
	}

	public Task CopyToAsync(Stream destination, CancellationToken cancellationToken)
	{
		return CopyToAsync(destination, GetCopyBufferSize(), cancellationToken);
	}

	public virtual Task CopyToAsync(Stream destination, int bufferSize, CancellationToken cancellationToken)
	{
		ValidateCopyToArguments(destination, bufferSize);
		if (!CanRead)
		{
			if (CanWrite)
			{
				ThrowHelper.ThrowNotSupportedException_UnreadableStream();
			}
			ThrowHelper.ThrowObjectDisposedException_StreamClosed(GetType().Name);
		}
		return Core(this, destination, bufferSize, cancellationToken);
		static async Task Core(Stream source, Stream destination, int bufferSize, CancellationToken cancellationToken)
		{
			byte[] buffer = ArrayPool<byte>.Shared.Rent(bufferSize);
			try
			{
				int length;
				while ((length = await source.ReadAsync(new Memory<byte>(buffer), cancellationToken).ConfigureAwait(continueOnCapturedContext: false)) != 0)
				{
					await destination.WriteAsync(new ReadOnlyMemory<byte>(buffer, 0, length), cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
				}
			}
			finally
			{
				ArrayPool<byte>.Shared.Return(buffer);
			}
		}
	}

	private int GetCopyBufferSize()
	{
		int num = 81920;
		if (CanSeek)
		{
			long length = Length;
			long position = Position;
			if (length <= position)
			{
				num = 1;
			}
			else
			{
				long num2 = length - position;
				if (num2 > 0)
				{
					num = (int)Math.Min(num, num2);
				}
			}
		}
		return num;
	}

	public void Dispose()
	{
		Close();
	}

	public virtual void Close()
	{
		Dispose(disposing: true);
		GC.SuppressFinalize(this);
	}

	protected virtual void Dispose(bool disposing)
	{
	}

	public virtual ValueTask DisposeAsync()
	{
		try
		{
			Dispose();
			return default(ValueTask);
		}
		catch (Exception exception)
		{
			return ValueTask.FromException(exception);
		}
	}

	public abstract void Flush();

	public Task FlushAsync()
	{
		return FlushAsync(CancellationToken.None);
	}

	public virtual Task FlushAsync(CancellationToken cancellationToken)
	{
		return Task.Factory.StartNew(delegate(object state)
		{
			((Stream)state).Flush();
		}, this, cancellationToken, TaskCreationOptions.DenyChildAttach, TaskScheduler.Default);
	}

	[Obsolete("CreateWaitHandle has been deprecated. Use the ManualResetEvent(false) constructor instead.")]
	protected virtual WaitHandle CreateWaitHandle()
	{
		return new ManualResetEvent(initialState: false);
	}

	public virtual IAsyncResult BeginRead(byte[] buffer, int offset, int count, AsyncCallback? callback, object? state)
	{
		return BeginReadInternal(buffer, offset, count, callback, state, serializeAsynchronously: false, apm: true);
	}

	internal Task<int> BeginReadInternal(byte[] buffer, int offset, int count, AsyncCallback callback, object state, bool serializeAsynchronously, bool apm)
	{
		ValidateBufferArguments(buffer, offset, count);
		if (!CanRead)
		{
			ThrowHelper.ThrowNotSupportedException_UnreadableStream();
		}
		SemaphoreSlim semaphoreSlim = EnsureAsyncActiveSemaphoreInitialized();
		Task task = null;
		if (serializeAsynchronously)
		{
			task = semaphoreSlim.WaitAsync();
		}
		else
		{
			semaphoreSlim.Wait();
		}
		ReadWriteTask readWriteTask = new ReadWriteTask(isRead: true, apm, delegate
		{
			ReadWriteTask readWriteTask2 = Task.InternalCurrent as ReadWriteTask;
			try
			{
				return readWriteTask2._stream.Read(readWriteTask2._buffer, readWriteTask2._offset, readWriteTask2._count);
			}
			finally
			{
				if (!readWriteTask2._apm)
				{
					readWriteTask2._stream.FinishTrackingAsyncOperation(readWriteTask2);
				}
				readWriteTask2.ClearBeginState();
			}
		}, state, this, buffer, offset, count, callback);
		if (task != null)
		{
			RunReadWriteTaskWhenReady(task, readWriteTask);
		}
		else
		{
			RunReadWriteTask(readWriteTask);
		}
		return readWriteTask;
	}

	public virtual int EndRead(IAsyncResult asyncResult)
	{
		if (asyncResult == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.asyncResult);
		}
		ReadWriteTask readWriteTask = asyncResult as ReadWriteTask;
		if (readWriteTask == null || !readWriteTask._isRead)
		{
			ThrowHelper.ThrowArgumentException(ExceptionResource.InvalidOperation_WrongAsyncResultOrEndCalledMultiple);
		}
		else if (readWriteTask._endCalled)
		{
			ThrowHelper.ThrowInvalidOperationException(ExceptionResource.InvalidOperation_WrongAsyncResultOrEndCalledMultiple);
		}
		try
		{
			return readWriteTask.GetAwaiter().GetResult();
		}
		finally
		{
			FinishTrackingAsyncOperation(readWriteTask);
		}
	}

	public Task<int> ReadAsync(byte[] buffer, int offset, int count)
	{
		return ReadAsync(buffer, offset, count, CancellationToken.None);
	}

	public virtual Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
	{
		if (!cancellationToken.IsCancellationRequested)
		{
			return BeginEndReadAsync(buffer, offset, count);
		}
		return Task.FromCanceled<int>(cancellationToken);
	}

	public virtual ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default(CancellationToken))
	{
		if (MemoryMarshal.TryGetArray((ReadOnlyMemory<byte>)buffer, out ArraySegment<byte> segment))
		{
			return new ValueTask<int>(ReadAsync(segment.Array, segment.Offset, segment.Count, cancellationToken));
		}
		byte[] array = ArrayPool<byte>.Shared.Rent(buffer.Length);
		return FinishReadAsync(ReadAsync(array, 0, buffer.Length, cancellationToken), array, buffer);
		static async ValueTask<int> FinishReadAsync(Task<int> readTask, byte[] localBuffer, Memory<byte> localDestination)
		{
			try
			{
				int num = await readTask.ConfigureAwait(continueOnCapturedContext: false);
				new ReadOnlySpan<byte>(localBuffer, 0, num).CopyTo(localDestination.Span);
				return num;
			}
			finally
			{
				ArrayPool<byte>.Shared.Return(localBuffer);
			}
		}
	}

	private Task<int> BeginEndReadAsync(byte[] buffer, int offset, int count)
	{
		if (!HasOverriddenBeginEndRead())
		{
			return BeginReadInternal(buffer, offset, count, null, null, serializeAsynchronously: true, apm: false);
		}
		return TaskFactory<int>.FromAsyncTrim(this, new ReadWriteParameters
		{
			Buffer = buffer,
			Offset = offset,
			Count = count
		}, (Stream stream, ReadWriteParameters args, AsyncCallback callback, object state) => stream.BeginRead(args.Buffer, args.Offset, args.Count, callback, state), (Stream stream, IAsyncResult asyncResult) => stream.EndRead(asyncResult));
	}

	public virtual IAsyncResult BeginWrite(byte[] buffer, int offset, int count, AsyncCallback? callback, object? state)
	{
		return BeginWriteInternal(buffer, offset, count, callback, state, serializeAsynchronously: false, apm: true);
	}

	internal Task BeginWriteInternal(byte[] buffer, int offset, int count, AsyncCallback callback, object state, bool serializeAsynchronously, bool apm)
	{
		ValidateBufferArguments(buffer, offset, count);
		if (!CanWrite)
		{
			ThrowHelper.ThrowNotSupportedException_UnwritableStream();
		}
		SemaphoreSlim semaphoreSlim = EnsureAsyncActiveSemaphoreInitialized();
		Task task = null;
		if (serializeAsynchronously)
		{
			task = semaphoreSlim.WaitAsync();
		}
		else
		{
			semaphoreSlim.Wait();
		}
		ReadWriteTask readWriteTask = new ReadWriteTask(isRead: false, apm, delegate
		{
			ReadWriteTask readWriteTask2 = Task.InternalCurrent as ReadWriteTask;
			try
			{
				readWriteTask2._stream.Write(readWriteTask2._buffer, readWriteTask2._offset, readWriteTask2._count);
				return 0;
			}
			finally
			{
				if (!readWriteTask2._apm)
				{
					readWriteTask2._stream.FinishTrackingAsyncOperation(readWriteTask2);
				}
				readWriteTask2.ClearBeginState();
			}
		}, state, this, buffer, offset, count, callback);
		if (task != null)
		{
			RunReadWriteTaskWhenReady(task, readWriteTask);
		}
		else
		{
			RunReadWriteTask(readWriteTask);
		}
		return readWriteTask;
	}

	private static void RunReadWriteTaskWhenReady(Task asyncWaiter, ReadWriteTask readWriteTask)
	{
		if (asyncWaiter.IsCompleted)
		{
			RunReadWriteTask(readWriteTask);
			return;
		}
		asyncWaiter.ContinueWith(delegate(Task t, object state)
		{
			ReadWriteTask readWriteTask2 = (ReadWriteTask)state;
			RunReadWriteTask(readWriteTask2);
		}, readWriteTask, default(CancellationToken), TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);
	}

	private static void RunReadWriteTask(ReadWriteTask readWriteTask)
	{
		readWriteTask.m_taskScheduler = TaskScheduler.Default;
		readWriteTask.ScheduleAndStart(needsProtection: false);
	}

	private void FinishTrackingAsyncOperation(ReadWriteTask task)
	{
		task._endCalled = true;
		_asyncActiveSemaphore.Release();
	}

	public virtual void EndWrite(IAsyncResult asyncResult)
	{
		if (asyncResult == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.asyncResult);
		}
		ReadWriteTask readWriteTask = asyncResult as ReadWriteTask;
		if (readWriteTask == null || readWriteTask._isRead)
		{
			ThrowHelper.ThrowArgumentException(ExceptionResource.InvalidOperation_WrongAsyncResultOrEndCalledMultiple);
		}
		else if (readWriteTask._endCalled)
		{
			ThrowHelper.ThrowInvalidOperationException(ExceptionResource.InvalidOperation_WrongAsyncResultOrEndCalledMultiple);
		}
		try
		{
			readWriteTask.GetAwaiter().GetResult();
		}
		finally
		{
			FinishTrackingAsyncOperation(readWriteTask);
		}
	}

	public Task WriteAsync(byte[] buffer, int offset, int count)
	{
		return WriteAsync(buffer, offset, count, CancellationToken.None);
	}

	public virtual Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
	{
		if (!cancellationToken.IsCancellationRequested)
		{
			return BeginEndWriteAsync(buffer, offset, count);
		}
		return Task.FromCanceled(cancellationToken);
	}

	public virtual ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default(CancellationToken))
	{
		if (MemoryMarshal.TryGetArray(buffer, out var segment))
		{
			return new ValueTask(WriteAsync(segment.Array, segment.Offset, segment.Count, cancellationToken));
		}
		byte[] array = ArrayPool<byte>.Shared.Rent(buffer.Length);
		buffer.Span.CopyTo(array);
		return new ValueTask(FinishWriteAsync(WriteAsync(array, 0, buffer.Length, cancellationToken), array));
	}

	private static async Task FinishWriteAsync(Task writeTask, byte[] localBuffer)
	{
		try
		{
			await writeTask.ConfigureAwait(continueOnCapturedContext: false);
		}
		finally
		{
			ArrayPool<byte>.Shared.Return(localBuffer);
		}
	}

	private Task BeginEndWriteAsync(byte[] buffer, int offset, int count)
	{
		if (!HasOverriddenBeginEndWrite())
		{
			return BeginWriteInternal(buffer, offset, count, null, null, serializeAsynchronously: true, apm: false);
		}
		return TaskFactory<VoidTaskResult>.FromAsyncTrim(this, new ReadWriteParameters
		{
			Buffer = buffer,
			Offset = offset,
			Count = count
		}, (Stream stream, ReadWriteParameters args, AsyncCallback callback, object state) => stream.BeginWrite(args.Buffer, args.Offset, args.Count, callback, state), delegate(Stream stream, IAsyncResult asyncResult)
		{
			stream.EndWrite(asyncResult);
			return default(VoidTaskResult);
		});
	}

	public abstract long Seek(long offset, SeekOrigin origin);

	public abstract void SetLength(long value);

	public abstract int Read(byte[] buffer, int offset, int count);

	public virtual int Read(Span<byte> buffer)
	{
		byte[] array = ArrayPool<byte>.Shared.Rent(buffer.Length);
		try
		{
			int num = Read(array, 0, buffer.Length);
			if ((uint)num > (uint)buffer.Length)
			{
				throw new IOException(SR.IO_StreamTooLong);
			}
			new ReadOnlySpan<byte>(array, 0, num).CopyTo(buffer);
			return num;
		}
		finally
		{
			ArrayPool<byte>.Shared.Return(array);
		}
	}

	public virtual int ReadByte()
	{
		byte[] array = new byte[1];
		if (Read(array, 0, 1) != 0)
		{
			return array[0];
		}
		return -1;
	}

	public abstract void Write(byte[] buffer, int offset, int count);

	public virtual void Write(ReadOnlySpan<byte> buffer)
	{
		byte[] array = ArrayPool<byte>.Shared.Rent(buffer.Length);
		try
		{
			buffer.CopyTo(array);
			Write(array, 0, buffer.Length);
		}
		finally
		{
			ArrayPool<byte>.Shared.Return(array);
		}
	}

	public virtual void WriteByte(byte value)
	{
		Write(new byte[1] { value }, 0, 1);
	}

	public static Stream Synchronized(Stream stream)
	{
		if (stream != null)
		{
			if (!(stream is SyncStream))
			{
				return new SyncStream(stream);
			}
			return stream;
		}
		throw new ArgumentNullException("stream");
	}

	[Obsolete("Do not call or override this method.")]
	protected virtual void ObjectInvariant()
	{
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	protected static void ValidateBufferArguments(byte[] buffer, int offset, int count)
	{
		if (buffer == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.buffer);
		}
		if (offset < 0)
		{
			ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.offset, ExceptionResource.ArgumentOutOfRange_NeedNonNegNum);
		}
		if ((uint)count > buffer.Length - offset)
		{
			ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.count, ExceptionResource.Argument_InvalidOffLen);
		}
	}

	protected static void ValidateCopyToArguments(Stream destination, int bufferSize)
	{
		if (destination == null)
		{
			throw new ArgumentNullException("destination");
		}
		if (bufferSize <= 0)
		{
			throw new ArgumentOutOfRangeException("bufferSize", bufferSize, SR.ArgumentOutOfRange_NeedPosNum);
		}
		if (!destination.CanWrite)
		{
			if (destination.CanRead)
			{
				ThrowHelper.ThrowNotSupportedException_UnwritableStream();
			}
			ThrowHelper.ThrowObjectDisposedException_StreamClosed(destination.GetType().Name);
		}
	}
}
