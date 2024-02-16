using System.Buffers;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Sources;
using Microsoft.Win32.SafeHandles;

namespace System.IO.Pipes;

public abstract class PipeStream : Stream
{
	internal abstract class PipeValueTaskSource : IValueTaskSource<int>, IValueTaskSource
	{
		internal unsafe static readonly IOCompletionCallback s_ioCallback = IOCallback;

		internal readonly PreAllocatedOverlapped _preallocatedOverlapped;

		internal readonly PipeStream _pipeStream;

		internal MemoryHandle _memoryHandle;

		internal ManualResetValueTaskSourceCore<int> _source;

		internal unsafe NativeOverlapped* _overlapped;

		internal CancellationTokenRegistration _cancellationRegistration;

		internal ulong _result;

		internal short Version => _source.Version;

		protected PipeValueTaskSource(PipeStream pipeStream)
		{
			_pipeStream = pipeStream;
			_source.RunContinuationsAsynchronously = true;
			_preallocatedOverlapped = new PreAllocatedOverlapped(s_ioCallback, this, null);
		}

		internal void Dispose()
		{
			ReleaseResources();
			_preallocatedOverlapped.Dispose();
		}

		internal unsafe void PrepareForOperation(ReadOnlyMemory<byte> memory = default(ReadOnlyMemory<byte>))
		{
			_result = 0uL;
			_memoryHandle = memory.Pin();
			_overlapped = _pipeStream._threadPoolBinding.AllocateNativeOverlapped(_preallocatedOverlapped);
		}

		public ValueTaskSourceStatus GetStatus(short token)
		{
			return _source.GetStatus(token);
		}

		public void OnCompleted(Action<object> continuation, object state, short token, ValueTaskSourceOnCompletedFlags flags)
		{
			_source.OnCompleted(continuation, state, token, flags);
		}

		void IValueTaskSource.GetResult(short token)
		{
			GetResult(token);
		}

		public int GetResult(short token)
		{
			try
			{
				return _source.GetResult(token);
			}
			finally
			{
				_pipeStream.TryToReuse(this);
			}
		}

		internal unsafe void RegisterForCancellation(CancellationToken cancellationToken)
		{
			if (!cancellationToken.CanBeCanceled)
			{
				return;
			}
			try
			{
				_cancellationRegistration = cancellationToken.UnsafeRegister(delegate(object s, CancellationToken token)
				{
					PipeValueTaskSource pipeValueTaskSource = (PipeValueTaskSource)s;
					if (!pipeValueTaskSource._pipeStream.SafePipeHandle.IsInvalid)
					{
						try
						{
							global::Interop.Kernel32.CancelIoEx(pipeValueTaskSource._pipeStream.SafePipeHandle, pipeValueTaskSource._overlapped);
						}
						catch (ObjectDisposedException)
						{
						}
					}
				}, this);
			}
			catch (OutOfMemoryException)
			{
			}
		}

		internal unsafe void ReleaseResources()
		{
			_cancellationRegistration.Dispose();
			_memoryHandle.Dispose();
			if (_overlapped != null)
			{
				_pipeStream._threadPoolBinding.FreeNativeOverlapped(_overlapped);
				_overlapped = null;
			}
		}

		internal void FinishedScheduling()
		{
			ulong num = Interlocked.Exchange(ref _result, 1uL);
			if (num != 0L)
			{
				Complete((uint)num, (uint)(int)(num >> 32) & 0x7FFFFFFFu);
			}
		}

		private unsafe static void IOCallback(uint errorCode, uint numBytes, NativeOverlapped* pOverlapped)
		{
			PipeValueTaskSource pipeValueTaskSource = (PipeValueTaskSource)ThreadPoolBoundHandle.GetNativeOverlappedState(pOverlapped);
			if (Interlocked.Exchange(ref pipeValueTaskSource._result, 0x8000000000000000uL | ((ulong)numBytes << 32) | errorCode) != 0L)
			{
				pipeValueTaskSource.Complete(errorCode, numBytes);
			}
		}

		private void Complete(uint errorCode, uint numBytes)
		{
			ReleaseResources();
			CompleteCore(errorCode, numBytes);
		}

		private protected abstract void CompleteCore(uint errorCode, uint numBytes);
	}

	internal sealed class ReadWriteValueTaskSource : PipeValueTaskSource
	{
		internal readonly bool _isWrite;

		internal ReadWriteValueTaskSource(PipeStream stream, bool isWrite)
			: base(stream)
		{
			_isWrite = isWrite;
		}

		private protected override void CompleteCore(uint errorCode, uint numBytes)
		{
			if (!_isWrite)
			{
				bool completion = true;
				switch (errorCode)
				{
				case 109u:
				case 232u:
				case 233u:
					errorCode = 0u;
					break;
				case 234u:
					errorCode = 0u;
					completion = false;
					break;
				}
				_pipeStream.UpdateMessageCompletion(completion);
			}
			switch (errorCode)
			{
			case 0u:
				_source.SetResult((int)numBytes);
				break;
			case 995u:
			{
				CancellationToken token = _cancellationRegistration.Token;
				_source.SetException(token.IsCancellationRequested ? new OperationCanceledException(token) : new OperationCanceledException());
				break;
			}
			default:
				_source.SetException(_pipeStream.WinIOError((int)errorCode));
				break;
			}
		}
	}

	internal sealed class ConnectionValueTaskSource : PipeValueTaskSource
	{
		internal ConnectionValueTaskSource(NamedPipeServerStream server)
			: base(server)
		{
		}

		private protected override void CompleteCore(uint errorCode, uint numBytes)
		{
			switch (errorCode)
			{
			case 0u:
			case 535u:
				_pipeStream.State = PipeState.Connected;
				_source.SetResult((int)numBytes);
				break;
			case 995u:
			{
				CancellationToken token = _cancellationRegistration.Token;
				_source.SetException((token.CanBeCanceled && !token.IsCancellationRequested) ? Error.GetOperationAborted() : new OperationCanceledException(token));
				break;
			}
			default:
				_source.SetException(System.IO.Win32Marshal.GetExceptionForWin32Error((int)errorCode));
				break;
			}
		}
	}

	private SafePipeHandle _handle;

	private bool _canRead;

	private bool _canWrite;

	private bool _isAsync;

	private bool _isCurrentUserOnly;

	private bool _isMessageComplete;

	private bool _isFromExistingHandle;

	private bool _isHandleExposed;

	private PipeTransmissionMode _readMode;

	private PipeTransmissionMode _transmissionMode;

	private PipeDirection _pipeDirection;

	private uint _outBufferSize;

	private PipeState _state;

	internal ThreadPoolBoundHandle _threadPoolBinding;

	private ReadWriteValueTaskSource _reusableReadValueTaskSource;

	private ReadWriteValueTaskSource _reusableWriteValueTaskSource;

	public bool IsConnected
	{
		get
		{
			return State == PipeState.Connected;
		}
		protected set
		{
			_state = (value ? PipeState.Connected : PipeState.Disconnected);
		}
	}

	public bool IsAsync => _isAsync;

	public bool IsMessageComplete
	{
		get
		{
			if (_state == PipeState.WaitingToConnect)
			{
				throw new InvalidOperationException(System.SR.InvalidOperation_PipeNotYetConnected);
			}
			if (_state == PipeState.Disconnected)
			{
				throw new InvalidOperationException(System.SR.InvalidOperation_PipeDisconnected);
			}
			if (_handle == null)
			{
				throw new InvalidOperationException(System.SR.InvalidOperation_PipeHandleNotSet);
			}
			if (_state == PipeState.Closed || (_handle != null && _handle.IsClosed))
			{
				throw Error.GetPipeNotOpen();
			}
			if (_readMode != PipeTransmissionMode.Message)
			{
				throw new InvalidOperationException(System.SR.InvalidOperation_PipeReadModeNotMessage);
			}
			return _isMessageComplete;
		}
	}

	public SafePipeHandle SafePipeHandle
	{
		get
		{
			if (_handle == null)
			{
				throw new InvalidOperationException(System.SR.InvalidOperation_PipeHandleNotSet);
			}
			if (_handle.IsClosed)
			{
				throw Error.GetPipeNotOpen();
			}
			_isHandleExposed = true;
			return _handle;
		}
	}

	internal SafePipeHandle? InternalHandle => _handle;

	protected bool IsHandleExposed => _isHandleExposed;

	public override bool CanRead => _canRead;

	public override bool CanWrite => _canWrite;

	public override bool CanSeek => false;

	public override long Length
	{
		get
		{
			throw Error.GetSeekNotSupported();
		}
	}

	public override long Position
	{
		get
		{
			throw Error.GetSeekNotSupported();
		}
		set
		{
			throw Error.GetSeekNotSupported();
		}
	}

	internal PipeState State
	{
		get
		{
			return _state;
		}
		set
		{
			_state = value;
		}
	}

	internal bool IsCurrentUserOnly
	{
		get
		{
			return _isCurrentUserOnly;
		}
		set
		{
			_isCurrentUserOnly = value;
		}
	}

	public unsafe virtual PipeTransmissionMode TransmissionMode
	{
		get
		{
			CheckPipePropertyOperations();
			if (_isFromExistingHandle)
			{
				Unsafe.SkipInit(out uint num);
				if (!global::Interop.Kernel32.GetNamedPipeInfo(_handle, &num, null, null, null))
				{
					throw WinIOError(Marshal.GetLastPInvokeError());
				}
				if ((num & 4u) != 0)
				{
					return PipeTransmissionMode.Message;
				}
				return PipeTransmissionMode.Byte;
			}
			return _transmissionMode;
		}
	}

	public unsafe virtual int InBufferSize
	{
		get
		{
			CheckPipePropertyOperations();
			if (!CanRead)
			{
				throw new NotSupportedException(System.SR.NotSupported_UnreadableStream);
			}
			Unsafe.SkipInit(out uint result);
			if (!global::Interop.Kernel32.GetNamedPipeInfo(_handle, null, null, &result, null))
			{
				throw WinIOError(Marshal.GetLastPInvokeError());
			}
			return (int)result;
		}
	}

	public unsafe virtual int OutBufferSize
	{
		get
		{
			CheckPipePropertyOperations();
			if (!CanWrite)
			{
				throw new NotSupportedException(System.SR.NotSupported_UnwritableStream);
			}
			Unsafe.SkipInit(out uint outBufferSize);
			if (_pipeDirection == PipeDirection.Out)
			{
				outBufferSize = _outBufferSize;
				return (int)outBufferSize;
			}
			if (!global::Interop.Kernel32.GetNamedPipeInfo(_handle, null, &outBufferSize, null, null))
			{
				throw WinIOError(Marshal.GetLastPInvokeError());
			}
			return (int)outBufferSize;
		}
	}

	public unsafe virtual PipeTransmissionMode ReadMode
	{
		get
		{
			CheckPipePropertyOperations();
			if (_isFromExistingHandle || IsHandleExposed)
			{
				UpdateReadMode();
			}
			return _readMode;
		}
		set
		{
			CheckPipePropertyOperations();
			if (value < PipeTransmissionMode.Byte || value > PipeTransmissionMode.Message)
			{
				throw new ArgumentOutOfRangeException("value", System.SR.ArgumentOutOfRange_TransmissionModeByteOrMsg);
			}
			int num = (int)value << 1;
			if (!global::Interop.Kernel32.SetNamedPipeHandleState(_handle, &num, IntPtr.Zero, IntPtr.Zero))
			{
				throw WinIOError(Marshal.GetLastPInvokeError());
			}
			_readMode = value;
		}
	}

	protected PipeStream(PipeDirection direction, int bufferSize)
	{
		if (direction < PipeDirection.In || direction > PipeDirection.InOut)
		{
			throw new ArgumentOutOfRangeException("direction", System.SR.ArgumentOutOfRange_DirectionModeInOutOrInOut);
		}
		if (bufferSize < 0)
		{
			throw new ArgumentOutOfRangeException("bufferSize", System.SR.ArgumentOutOfRange_NeedNonNegNum);
		}
		Init(direction, PipeTransmissionMode.Byte, (uint)bufferSize);
	}

	protected PipeStream(PipeDirection direction, PipeTransmissionMode transmissionMode, int outBufferSize)
	{
		if (direction < PipeDirection.In || direction > PipeDirection.InOut)
		{
			throw new ArgumentOutOfRangeException("direction", System.SR.ArgumentOutOfRange_DirectionModeInOutOrInOut);
		}
		if (transmissionMode < PipeTransmissionMode.Byte || transmissionMode > PipeTransmissionMode.Message)
		{
			throw new ArgumentOutOfRangeException("transmissionMode", System.SR.ArgumentOutOfRange_TransmissionModeByteOrMsg);
		}
		if (outBufferSize < 0)
		{
			throw new ArgumentOutOfRangeException("outBufferSize", System.SR.ArgumentOutOfRange_NeedNonNegNum);
		}
		Init(direction, transmissionMode, (uint)outBufferSize);
	}

	private void Init(PipeDirection direction, PipeTransmissionMode transmissionMode, uint outBufferSize)
	{
		_readMode = transmissionMode;
		_transmissionMode = transmissionMode;
		_pipeDirection = direction;
		if ((_pipeDirection & PipeDirection.In) != 0)
		{
			_canRead = true;
		}
		if ((_pipeDirection & PipeDirection.Out) != 0)
		{
			_canWrite = true;
		}
		_outBufferSize = outBufferSize;
		_isMessageComplete = true;
		_state = PipeState.WaitingToConnect;
	}

	protected void InitializeHandle(SafePipeHandle? handle, bool isExposed, bool isAsync)
	{
		if (isAsync && handle != null)
		{
			InitializeAsyncHandle(handle);
		}
		_handle = handle;
		_isAsync = isAsync;
		_isHandleExposed = isExposed;
		_isFromExistingHandle = isExposed;
	}

	public unsafe override int ReadByte()
	{
		Unsafe.SkipInit(out byte result);
		if (Read(new Span<byte>(&result, 1)) <= 0)
		{
			return -1;
		}
		return result;
	}

	public unsafe override void WriteByte(byte value)
	{
		Write(new ReadOnlySpan<byte>(&value, 1));
	}

	public override void Flush()
	{
		CheckWriteOperations();
	}

	public override Task FlushAsync(CancellationToken cancellationToken)
	{
		try
		{
			Flush();
			return Task.CompletedTask;
		}
		catch (Exception exception)
		{
			return Task.FromException(exception);
		}
	}

	protected override void Dispose(bool disposing)
	{
		try
		{
			if (_handle != null && !_handle.IsClosed)
			{
				_handle.Dispose();
			}
			DisposeCore(disposing);
		}
		finally
		{
			base.Dispose(disposing);
		}
		_state = PipeState.Closed;
	}

	internal void UpdateMessageCompletion(bool completion)
	{
		_isMessageComplete = completion || _state == PipeState.Broken;
	}

	public override void SetLength(long value)
	{
		throw Error.GetSeekNotSupported();
	}

	public override long Seek(long offset, SeekOrigin origin)
	{
		throw Error.GetSeekNotSupported();
	}

	protected internal virtual void CheckPipePropertyOperations()
	{
		if (_handle == null)
		{
			throw new InvalidOperationException(System.SR.InvalidOperation_PipeHandleNotSet);
		}
		if (_state == PipeState.Closed || (_handle != null && _handle.IsClosed))
		{
			throw Error.GetPipeNotOpen();
		}
	}

	protected internal void CheckReadOperations()
	{
		if (_state == PipeState.WaitingToConnect)
		{
			throw new InvalidOperationException(System.SR.InvalidOperation_PipeNotYetConnected);
		}
		if (_state == PipeState.Disconnected)
		{
			throw new InvalidOperationException(System.SR.InvalidOperation_PipeDisconnected);
		}
		if (_handle == null)
		{
			throw new InvalidOperationException(System.SR.InvalidOperation_PipeHandleNotSet);
		}
		if (_state == PipeState.Closed || (_handle != null && _handle.IsClosed))
		{
			throw Error.GetPipeNotOpen();
		}
	}

	protected internal void CheckWriteOperations()
	{
		if (_state == PipeState.WaitingToConnect)
		{
			throw new InvalidOperationException(System.SR.InvalidOperation_PipeNotYetConnected);
		}
		if (_state == PipeState.Disconnected)
		{
			throw new InvalidOperationException(System.SR.InvalidOperation_PipeDisconnected);
		}
		if (_handle == null)
		{
			throw new InvalidOperationException(System.SR.InvalidOperation_PipeHandleNotSet);
		}
		if (_state == PipeState.Broken)
		{
			throw new IOException(System.SR.IO_PipeBroken);
		}
		if (_state == PipeState.Closed || (_handle != null && _handle.IsClosed))
		{
			throw Error.GetPipeNotOpen();
		}
	}

	public override int Read(byte[] buffer, int offset, int count)
	{
		if (_isAsync)
		{
			return ReadAsync(buffer, offset, count, CancellationToken.None).GetAwaiter().GetResult();
		}
		Stream.ValidateBufferArguments(buffer, offset, count);
		if (!CanRead)
		{
			throw Error.GetReadNotSupported();
		}
		CheckReadOperations();
		return ReadCore(new Span<byte>(buffer, offset, count));
	}

	public override int Read(Span<byte> buffer)
	{
		if (_isAsync)
		{
			return base.Read(buffer);
		}
		if (!CanRead)
		{
			throw Error.GetReadNotSupported();
		}
		CheckReadOperations();
		return ReadCore(buffer);
	}

	public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
	{
		Stream.ValidateBufferArguments(buffer, offset, count);
		if (!CanRead)
		{
			throw Error.GetReadNotSupported();
		}
		if (cancellationToken.IsCancellationRequested)
		{
			return Task.FromCanceled<int>(cancellationToken);
		}
		CheckReadOperations();
		if (!_isAsync)
		{
			return base.ReadAsync(buffer, offset, count, cancellationToken);
		}
		if (count == 0)
		{
			UpdateMessageCompletion(completion: false);
			return Task.FromResult(0);
		}
		return ReadAsyncCore(new Memory<byte>(buffer, offset, count), cancellationToken).AsTask();
	}

	public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default(CancellationToken))
	{
		if (!_isAsync)
		{
			return base.ReadAsync(buffer, cancellationToken);
		}
		if (!CanRead)
		{
			throw Error.GetReadNotSupported();
		}
		if (cancellationToken.IsCancellationRequested)
		{
			return ValueTask.FromCanceled<int>(cancellationToken);
		}
		CheckReadOperations();
		if (buffer.Length == 0)
		{
			UpdateMessageCompletion(completion: false);
			return new ValueTask<int>(0);
		}
		return ReadAsyncCore(buffer, cancellationToken);
	}

	public override IAsyncResult BeginRead(byte[] buffer, int offset, int count, AsyncCallback? callback, object? state)
	{
		if (_isAsync)
		{
			return System.Threading.Tasks.TaskToApm.Begin(ReadAsync(buffer, offset, count, CancellationToken.None), callback, state);
		}
		return base.BeginRead(buffer, offset, count, callback, state);
	}

	public override int EndRead(IAsyncResult asyncResult)
	{
		if (_isAsync)
		{
			return System.Threading.Tasks.TaskToApm.End<int>(asyncResult);
		}
		return base.EndRead(asyncResult);
	}

	public override void Write(byte[] buffer, int offset, int count)
	{
		if (_isAsync)
		{
			WriteAsync(buffer, offset, count, CancellationToken.None).GetAwaiter().GetResult();
			return;
		}
		Stream.ValidateBufferArguments(buffer, offset, count);
		if (!CanWrite)
		{
			throw Error.GetWriteNotSupported();
		}
		CheckWriteOperations();
		WriteCore(new ReadOnlySpan<byte>(buffer, offset, count));
	}

	public override void Write(ReadOnlySpan<byte> buffer)
	{
		if (_isAsync)
		{
			base.Write(buffer);
			return;
		}
		if (!CanWrite)
		{
			throw Error.GetWriteNotSupported();
		}
		CheckWriteOperations();
		WriteCore(buffer);
	}

	public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
	{
		Stream.ValidateBufferArguments(buffer, offset, count);
		if (!CanWrite)
		{
			throw Error.GetWriteNotSupported();
		}
		if (cancellationToken.IsCancellationRequested)
		{
			return Task.FromCanceled<int>(cancellationToken);
		}
		CheckWriteOperations();
		if (!_isAsync)
		{
			return base.WriteAsync(buffer, offset, count, cancellationToken);
		}
		if (count == 0)
		{
			return Task.CompletedTask;
		}
		return WriteAsyncCore(new ReadOnlyMemory<byte>(buffer, offset, count), cancellationToken).AsTask();
	}

	public override ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default(CancellationToken))
	{
		if (!_isAsync)
		{
			return base.WriteAsync(buffer, cancellationToken);
		}
		if (!CanWrite)
		{
			throw Error.GetWriteNotSupported();
		}
		if (cancellationToken.IsCancellationRequested)
		{
			return ValueTask.FromCanceled(cancellationToken);
		}
		CheckWriteOperations();
		if (buffer.Length == 0)
		{
			return default(ValueTask);
		}
		return WriteAsyncCore(buffer, cancellationToken);
	}

	public override IAsyncResult BeginWrite(byte[] buffer, int offset, int count, AsyncCallback? callback, object? state)
	{
		if (_isAsync)
		{
			return System.Threading.Tasks.TaskToApm.Begin(WriteAsync(buffer, offset, count, CancellationToken.None), callback, state);
		}
		return base.BeginWrite(buffer, offset, count, callback, state);
	}

	public override void EndWrite(IAsyncResult asyncResult)
	{
		if (_isAsync)
		{
			System.Threading.Tasks.TaskToApm.End(asyncResult);
		}
		else
		{
			base.EndWrite(asyncResult);
		}
	}

	internal static string GetPipePath(string serverName, string pipeName)
	{
		string fullPath = Path.GetFullPath("\\\\" + serverName + "\\pipe\\" + pipeName);
		if (string.Equals(fullPath, "\\\\.\\pipe\\anonymous", StringComparison.OrdinalIgnoreCase))
		{
			throw new ArgumentOutOfRangeException("pipeName", System.SR.ArgumentOutOfRange_AnonymousReserved);
		}
		return fullPath;
	}

	internal void ValidateHandleIsPipe(SafePipeHandle safePipeHandle)
	{
		if (global::Interop.Kernel32.GetFileType(safePipeHandle) != 3)
		{
			throw new IOException(System.SR.IO_InvalidPipeHandle);
		}
	}

	private void InitializeAsyncHandle(SafePipeHandle handle)
	{
		_threadPoolBinding = ThreadPoolBoundHandle.BindHandle(handle);
	}

	internal virtual void TryToReuse(PipeValueTaskSource source)
	{
		source._source.Reset();
		if (source is ReadWriteValueTaskSource readWriteValueTaskSource && Interlocked.CompareExchange(ref readWriteValueTaskSource._isWrite ? ref _reusableWriteValueTaskSource : ref _reusableReadValueTaskSource, readWriteValueTaskSource, null) != null)
		{
			source._preallocatedOverlapped.Dispose();
		}
	}

	private void DisposeCore(bool disposing)
	{
		if (disposing)
		{
			_threadPoolBinding?.Dispose();
			Interlocked.Exchange(ref _reusableReadValueTaskSource, null)?.Dispose();
			Interlocked.Exchange(ref _reusableWriteValueTaskSource, null)?.Dispose();
		}
	}

	private unsafe int ReadCore(Span<byte> buffer)
	{
		if (buffer.Length == 0)
		{
			return 0;
		}
		fixed (byte* bytes = &MemoryMarshal.GetReference(buffer))
		{
			int numBytesRead = 0;
			if (global::Interop.Kernel32.ReadFile(_handle, bytes, buffer.Length, out numBytesRead, IntPtr.Zero) != 0)
			{
				_isMessageComplete = true;
				return numBytesRead;
			}
			int lastPInvokeError = Marshal.GetLastPInvokeError();
			_isMessageComplete = lastPInvokeError != 234;
			switch (lastPInvokeError)
			{
			case 234:
				return numBytesRead;
			case 109:
			case 233:
				State = PipeState.Broken;
				return 0;
			default:
				throw System.IO.Win32Marshal.GetExceptionForWin32Error(lastPInvokeError, string.Empty);
			}
		}
	}

	private unsafe ValueTask<int> ReadAsyncCore(Memory<byte> buffer, CancellationToken cancellationToken)
	{
		ReadWriteValueTaskSource readWriteValueTaskSource = Interlocked.Exchange(ref _reusableReadValueTaskSource, null) ?? new ReadWriteValueTaskSource(this, isWrite: false);
		try
		{
			readWriteValueTaskSource.PrepareForOperation(buffer);
			if (global::Interop.Kernel32.ReadFile(_handle, (byte*)readWriteValueTaskSource._memoryHandle.Pointer, buffer.Length, IntPtr.Zero, readWriteValueTaskSource._overlapped) == 0)
			{
				int lastPInvokeError = Marshal.GetLastPInvokeError();
				switch (lastPInvokeError)
				{
				case 997:
					readWriteValueTaskSource.RegisterForCancellation(cancellationToken);
					break;
				case 109:
				case 233:
					State = PipeState.Broken;
					readWriteValueTaskSource._overlapped->InternalLow = IntPtr.Zero;
					readWriteValueTaskSource.Dispose();
					UpdateMessageCompletion(completion: true);
					return new ValueTask<int>(0);
				default:
					readWriteValueTaskSource.Dispose();
					return ValueTask.FromException<int>(System.IO.Win32Marshal.GetExceptionForWin32Error(lastPInvokeError));
				case 234:
					break;
				}
			}
		}
		catch
		{
			readWriteValueTaskSource.Dispose();
			throw;
		}
		readWriteValueTaskSource.FinishedScheduling();
		return new ValueTask<int>(readWriteValueTaskSource, readWriteValueTaskSource.Version);
	}

	private unsafe void WriteCore(ReadOnlySpan<byte> buffer)
	{
		if (buffer.Length == 0)
		{
			return;
		}
		fixed (byte* bytes = &MemoryMarshal.GetReference(buffer))
		{
			int numBytesWritten = 0;
			if (global::Interop.Kernel32.WriteFile(_handle, bytes, buffer.Length, out numBytesWritten, IntPtr.Zero) == 0)
			{
				throw WinIOError(Marshal.GetLastPInvokeError());
			}
		}
	}

	private unsafe ValueTask WriteAsyncCore(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken)
	{
		ReadWriteValueTaskSource readWriteValueTaskSource = Interlocked.Exchange(ref _reusableWriteValueTaskSource, null) ?? new ReadWriteValueTaskSource(this, isWrite: true);
		try
		{
			readWriteValueTaskSource.PrepareForOperation(buffer);
			if (global::Interop.Kernel32.WriteFile(_handle, (byte*)readWriteValueTaskSource._memoryHandle.Pointer, buffer.Length, IntPtr.Zero, readWriteValueTaskSource._overlapped) == 0)
			{
				int lastPInvokeError = Marshal.GetLastPInvokeError();
				if (lastPInvokeError != 997)
				{
					readWriteValueTaskSource.Dispose();
					return ValueTask.FromException(ExceptionDispatchInfo.SetCurrentStackTrace(WinIOError(lastPInvokeError)));
				}
				readWriteValueTaskSource.RegisterForCancellation(cancellationToken);
			}
		}
		catch
		{
			readWriteValueTaskSource.Dispose();
			throw;
		}
		readWriteValueTaskSource.FinishedScheduling();
		return new ValueTask(readWriteValueTaskSource, readWriteValueTaskSource.Version);
	}

	[SupportedOSPlatform("windows")]
	public void WaitForPipeDrain()
	{
		CheckWriteOperations();
		if (!CanWrite)
		{
			throw Error.GetWriteNotSupported();
		}
		if (!global::Interop.Kernel32.FlushFileBuffers(_handle))
		{
			throw WinIOError(Marshal.GetLastPInvokeError());
		}
	}

	internal unsafe static global::Interop.Kernel32.SECURITY_ATTRIBUTES GetSecAttrs(HandleInheritability inheritability)
	{
		global::Interop.Kernel32.SECURITY_ATTRIBUTES result = default(global::Interop.Kernel32.SECURITY_ATTRIBUTES);
		result.nLength = (uint)sizeof(global::Interop.Kernel32.SECURITY_ATTRIBUTES);
		result.bInheritHandle = (((inheritability & HandleInheritability.Inheritable) != 0) ? global::Interop.BOOL.TRUE : global::Interop.BOOL.FALSE);
		return result;
	}

	internal unsafe static global::Interop.Kernel32.SECURITY_ATTRIBUTES GetSecAttrs(HandleInheritability inheritability, PipeSecurity pipeSecurity, ref GCHandle pinningHandle)
	{
		global::Interop.Kernel32.SECURITY_ATTRIBUTES secAttrs = GetSecAttrs(inheritability);
		if (pipeSecurity != null)
		{
			byte[] securityDescriptorBinaryForm = pipeSecurity.GetSecurityDescriptorBinaryForm();
			pinningHandle = GCHandle.Alloc(securityDescriptorBinaryForm, GCHandleType.Pinned);
			fixed (byte* ptr = securityDescriptorBinaryForm)
			{
				secAttrs.lpSecurityDescriptor = (IntPtr)ptr;
			}
		}
		return secAttrs;
	}

	private unsafe void UpdateReadMode()
	{
		Unsafe.SkipInit(out uint num);
		if (!global::Interop.Kernel32.GetNamedPipeHandleStateW(SafePipeHandle, &num, null, null, null, null, 0u))
		{
			throw WinIOError(Marshal.GetLastPInvokeError());
		}
		if ((num & 2u) != 0)
		{
			_readMode = PipeTransmissionMode.Message;
		}
		else
		{
			_readMode = PipeTransmissionMode.Byte;
		}
	}

	internal Exception WinIOError(int errorCode)
	{
		switch (errorCode)
		{
		case 109:
		case 232:
		case 233:
			_state = PipeState.Broken;
			return new IOException(System.SR.IO_PipeBroken, System.IO.Win32Marshal.MakeHRFromErrorCode(errorCode));
		case 38:
			return Error.GetEndOfFile();
		case 6:
			_handle.SetHandleAsInvalid();
			_state = PipeState.Broken;
			break;
		}
		return System.IO.Win32Marshal.GetExceptionForWin32Error(errorCode);
	}
}
