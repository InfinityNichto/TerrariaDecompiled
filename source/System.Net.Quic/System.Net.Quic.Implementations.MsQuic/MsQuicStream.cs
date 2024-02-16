using System.Buffers;
using System.IO;
using System.Net.Quic.Implementations.MsQuic.Internal;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace System.Net.Quic.Implementations.MsQuic;

internal sealed class MsQuicStream : QuicStreamProvider
{
	private sealed class State
	{
		public SafeMsQuicStreamHandle Handle;

		public GCHandle StateGCHandle;

		public MsQuicStream Stream;

		public MsQuicConnection.State ConnectionState;

		public string TraceId;

		public ReadState ReadState;

		public long ReadErrorCode = -1L;

		public MsQuicNativeMethods.QuicBuffer[] ReceiveQuicBuffers = Array.Empty<MsQuicNativeMethods.QuicBuffer>();

		public int ReceiveQuicBuffersCount;

		public int ReceiveQuicBuffersTotalBytes;

		public bool ReceiveIsFinal;

		public Memory<byte> ReceiveUserBuffer;

		public CancellationTokenRegistration ReceiveCancellationRegistration;

		public readonly ResettableCompletionSource<int> ReceiveResettableCompletionSource = new ResettableCompletionSource<int>();

		public SendState SendState;

		public long SendErrorCode = -1L;

		public MemoryHandle[] BufferArrays = new MemoryHandle[1];

		public IntPtr SendQuicBuffers;

		public int SendBufferMaxCount;

		public int SendBufferCount;

		public readonly ResettableCompletionSource<uint> SendResettableCompletionSource = new ResettableCompletionSource<uint>();

		public ShutdownWriteState ShutdownWriteState;

		public readonly TaskCompletionSource ShutdownWriteCompletionSource = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

		public ShutdownState ShutdownState;

		public int ShutdownDone;

		public readonly TaskCompletionSource ShutdownCompletionSource = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

		public void Cleanup()
		{
			if (System.Net.NetEventSource.Log.IsEnabled())
			{
				System.Net.NetEventSource.Info(this, $"{TraceId} releasing handles.", "Cleanup");
			}
			ShutdownState = ShutdownState.Finished;
			CleanupSendState(this);
			Handle?.Dispose();
			Marshal.FreeHGlobal(SendQuicBuffers);
			SendQuicBuffers = IntPtr.Zero;
			if (StateGCHandle.IsAllocated)
			{
				StateGCHandle.Free();
			}
			ConnectionState?.RemoveStream(null);
		}
	}

	private enum ReadState
	{
		None,
		IndividualReadComplete,
		PendingRead,
		ReadsCompleted,
		Aborted,
		ConnectionClosed,
		Closed
	}

	private enum ShutdownWriteState
	{
		None,
		Canceled,
		Finished,
		ConnectionClosed
	}

	private enum ShutdownState
	{
		None,
		Canceled,
		Pending,
		Finished,
		ConnectionClosed
	}

	private enum SendState
	{
		None,
		Pending,
		Finished,
		Aborted,
		ConnectionClosed,
		Closed
	}

	internal static readonly MsQuicNativeMethods.StreamCallbackDelegate s_streamDelegate = NativeCallbackHandler;

	private readonly State _state = new State();

	private readonly bool _canRead;

	private readonly bool _canWrite;

	private long _streamId = -1L;

	private int _disposed;

	private int _readTimeout = -1;

	private int _writeTimeout = -1;

	internal override bool CanRead
	{
		get
		{
			if (_disposed == 0)
			{
				return _canRead;
			}
			return false;
		}
	}

	internal override bool CanWrite
	{
		get
		{
			if (_disposed == 0)
			{
				return _canWrite;
			}
			return false;
		}
	}

	internal override bool ReadsCompleted => _state.ReadState == ReadState.ReadsCompleted;

	internal override bool CanTimeout => true;

	internal override int ReadTimeout
	{
		get
		{
			ThrowIfDisposed();
			return _readTimeout;
		}
		set
		{
			ThrowIfDisposed();
			if (value <= 0 && value != -1)
			{
				throw new ArgumentOutOfRangeException("value", System.SR.net_quic_timeout_use_gt_zero);
			}
			_readTimeout = value;
		}
	}

	internal override int WriteTimeout
	{
		get
		{
			ThrowIfDisposed();
			return _writeTimeout;
		}
		set
		{
			ThrowIfDisposed();
			if (value <= 0 && value != -1)
			{
				throw new ArgumentOutOfRangeException("value", System.SR.net_quic_timeout_use_gt_zero);
			}
			_writeTimeout = value;
		}
	}

	internal override long StreamId
	{
		get
		{
			ThrowIfDisposed();
			if (_streamId == -1)
			{
				_streamId = GetStreamId();
			}
			return _streamId;
		}
	}

	internal string TraceId()
	{
		return _state.TraceId;
	}

	internal MsQuicStream(MsQuicConnection.State connectionState, SafeMsQuicStreamHandle streamHandle, QUIC_STREAM_OPEN_FLAGS flags)
	{
		if (!connectionState.TryAddStream(this))
		{
			throw new ObjectDisposedException("QuicConnection");
		}
		_state.ConnectionState = connectionState;
		_state.Handle = streamHandle;
		_canRead = true;
		_canWrite = !flags.HasFlag(QUIC_STREAM_OPEN_FLAGS.UNIDIRECTIONAL);
		if (!_canWrite)
		{
			_state.SendState = SendState.Closed;
		}
		_state.StateGCHandle = GCHandle.Alloc(_state);
		try
		{
			MsQuicApi.Api.SetCallbackHandlerDelegate(_state.Handle, s_streamDelegate, GCHandle.ToIntPtr(_state.StateGCHandle));
		}
		catch
		{
			_state.StateGCHandle.Free();
			throw;
		}
		_state.TraceId = MsQuicTraceHelper.GetTraceId(_state.Handle);
		if (System.Net.NetEventSource.Log.IsEnabled())
		{
			System.Net.NetEventSource.Info(_state, $"{TraceId()} Inbound {(flags.HasFlag(QUIC_STREAM_OPEN_FLAGS.UNIDIRECTIONAL) ? "uni" : "bi")}directional stream created in connection {_state.ConnectionState.TraceId}.", ".ctor");
		}
	}

	internal MsQuicStream(MsQuicConnection.State connectionState, QUIC_STREAM_OPEN_FLAGS flags)
	{
		if (!connectionState.TryAddStream(this))
		{
			throw new ObjectDisposedException("QuicConnection");
		}
		_state.ConnectionState = connectionState;
		_canRead = !flags.HasFlag(QUIC_STREAM_OPEN_FLAGS.UNIDIRECTIONAL);
		_canWrite = true;
		_state.StateGCHandle = GCHandle.Alloc(_state);
		if (!_canRead)
		{
			_state.ReadState = ReadState.Closed;
		}
		try
		{
			uint status = MsQuicApi.Api.StreamOpenDelegate(connectionState.Handle, flags, s_streamDelegate, GCHandle.ToIntPtr(_state.StateGCHandle), out _state.Handle);
			QuicExceptionHelpers.ThrowIfFailed(status, "Failed to open stream to peer.");
			status = MsQuicApi.Api.StreamStartDelegate(_state.Handle, QUIC_STREAM_START_FLAGS.FAIL_BLOCKED);
			QuicExceptionHelpers.ThrowIfFailed(status, "Could not start stream.");
		}
		catch
		{
			_state.Handle?.Dispose();
			_state.StateGCHandle.Free();
			throw;
		}
		_state.TraceId = MsQuicTraceHelper.GetTraceId(_state.Handle);
		if (System.Net.NetEventSource.Log.IsEnabled())
		{
			System.Net.NetEventSource.Info(_state, $"{_state.TraceId} Outbound {(flags.HasFlag(QUIC_STREAM_OPEN_FLAGS.UNIDIRECTIONAL) ? "uni" : "bi")}directional stream created in connection {_state.ConnectionState.TraceId}.", ".ctor");
		}
	}

	internal override ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default(CancellationToken))
	{
		return WriteAsync(buffer, endStream: false, cancellationToken);
	}

	internal override ValueTask WriteAsync(ReadOnlySequence<byte> buffers, CancellationToken cancellationToken = default(CancellationToken))
	{
		return WriteAsync(buffers, endStream: false, cancellationToken);
	}

	internal override async ValueTask WriteAsync(ReadOnlySequence<byte> buffers, bool endStream, CancellationToken cancellationToken = default(CancellationToken))
	{
		ThrowIfDisposed();
		using (HandleWriteStartState(buffers.IsEmpty, cancellationToken))
		{
			await SendReadOnlySequenceAsync(buffers, endStream ? QUIC_SEND_FLAGS.FIN : QUIC_SEND_FLAGS.NONE).ConfigureAwait(continueOnCapturedContext: false);
			HandleWriteCompletedState();
		}
	}

	internal override ValueTask WriteAsync(ReadOnlyMemory<ReadOnlyMemory<byte>> buffers, CancellationToken cancellationToken = default(CancellationToken))
	{
		return WriteAsync(buffers, endStream: false, cancellationToken);
	}

	internal override async ValueTask WriteAsync(ReadOnlyMemory<ReadOnlyMemory<byte>> buffers, bool endStream, CancellationToken cancellationToken = default(CancellationToken))
	{
		ThrowIfDisposed();
		using (HandleWriteStartState(buffers.IsEmpty, cancellationToken))
		{
			await SendReadOnlyMemoryListAsync(buffers, endStream ? QUIC_SEND_FLAGS.FIN : QUIC_SEND_FLAGS.NONE).ConfigureAwait(continueOnCapturedContext: false);
			HandleWriteCompletedState();
		}
	}

	internal override async ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, bool endStream, CancellationToken cancellationToken = default(CancellationToken))
	{
		ThrowIfDisposed();
		using (HandleWriteStartState(buffer.IsEmpty, cancellationToken))
		{
			await SendReadOnlyMemoryAsync(buffer, endStream ? QUIC_SEND_FLAGS.FIN : QUIC_SEND_FLAGS.NONE).ConfigureAwait(continueOnCapturedContext: false);
			HandleWriteCompletedState();
		}
	}

	private CancellationTokenRegistration HandleWriteStartState(bool emptyBuffer, CancellationToken cancellationToken)
	{
		if (_state.SendState == SendState.Closed)
		{
			throw new InvalidOperationException(System.SR.net_quic_writing_notallowed);
		}
		if (_state.SendState == SendState.Aborted)
		{
			if (_state.SendErrorCode != -1)
			{
				throw new QuicStreamAbortedException(_state.SendErrorCode);
			}
			throw new OperationCanceledException(cancellationToken);
		}
		if (cancellationToken.IsCancellationRequested)
		{
			lock (_state)
			{
				if (_state.SendState == SendState.None || _state.SendState == SendState.Pending)
				{
					_state.SendState = SendState.Aborted;
				}
			}
			throw new OperationCanceledException(cancellationToken);
		}
		CancellationTokenRegistration result = cancellationToken.UnsafeRegister(delegate(object s, CancellationToken token)
		{
			State state = (State)s;
			bool flag = false;
			lock (state)
			{
				if (state.SendState == SendState.None || state.SendState == SendState.Pending)
				{
					state.SendState = SendState.Aborted;
					flag = true;
				}
			}
			if (flag)
			{
				state.SendResettableCompletionSource.CompleteException(ExceptionDispatchInfo.SetCurrentStackTrace(new OperationCanceledException("Write was canceled", token)));
			}
		}, _state);
		lock (_state)
		{
			if (_state.SendState == SendState.Aborted)
			{
				cancellationToken.ThrowIfCancellationRequested();
				if (_state.SendErrorCode != -1)
				{
					throw new QuicStreamAbortedException(_state.SendErrorCode);
				}
				throw new OperationCanceledException(System.SR.net_quic_sending_aborted);
			}
			if (_state.SendState == SendState.ConnectionClosed)
			{
				throw GetConnectionAbortedException(_state);
			}
			_state.SendState = ((!emptyBuffer) ? SendState.Pending : SendState.Finished);
			return result;
		}
	}

	private void HandleWriteCompletedState()
	{
		lock (_state)
		{
			if (_state.SendState == SendState.Finished)
			{
				_state.SendState = SendState.None;
			}
		}
	}

	private void HandleWriteFailedState()
	{
		lock (_state)
		{
			if (_state.SendState == SendState.Pending)
			{
				_state.SendState = SendState.Finished;
			}
		}
	}

	internal override ValueTask<int> ReadAsync(Memory<byte> destination, CancellationToken cancellationToken = default(CancellationToken))
	{
		ThrowIfDisposed();
		if (_state.ReadState == ReadState.Closed)
		{
			throw new InvalidOperationException(System.SR.net_quic_reading_notallowed);
		}
		if (System.Net.NetEventSource.Log.IsEnabled())
		{
			System.Net.NetEventSource.Info(_state, $"{TraceId()} Stream reading into Memory of '{destination.Length}' bytes.", "ReadAsync");
		}
		bool flag = false;
		ReadState readState;
		long readErrorCode;
		lock (_state)
		{
			readState = _state.ReadState;
			readErrorCode = _state.ReadErrorCode;
			if (readState != ReadState.PendingRead && cancellationToken.IsCancellationRequested)
			{
				readState = ReadState.Aborted;
				CleanupReadStateAndCheckPending(_state, ReadState.Aborted);
				flag = true;
			}
			switch (readState)
			{
			case ReadState.ReadsCompleted:
				return new ValueTask<int>(0);
			case ReadState.None:
				_state.ReceiveUserBuffer = destination;
				_state.Stream = this;
				_state.ReadState = ReadState.PendingRead;
				if (cancellationToken.CanBeCanceled)
				{
					_state.ReceiveCancellationRegistration = cancellationToken.UnsafeRegister(delegate(object obj, CancellationToken token)
					{
						State state = (State)obj;
						bool flag2;
						lock (state)
						{
							flag2 = CleanupReadStateAndCheckPending(state, ReadState.Aborted);
						}
						if (flag2)
						{
							state.ReceiveResettableCompletionSource.CompleteException(ExceptionDispatchInfo.SetCurrentStackTrace(new OperationCanceledException(token)));
						}
					}, _state);
				}
				else
				{
					_state.ReceiveCancellationRegistration = default(CancellationTokenRegistration);
				}
				return _state.ReceiveResettableCompletionSource.GetValueTask();
			case ReadState.IndividualReadComplete:
			{
				_state.ReadState = ReadState.None;
				int num = CopyMsQuicBuffersToUserBuffer(_state.ReceiveQuicBuffers.AsSpan(0, _state.ReceiveQuicBuffersCount), destination.Span);
				ReceiveComplete(num);
				if (num != _state.ReceiveQuicBuffersTotalBytes)
				{
					EnableReceive();
				}
				else if (_state.ReceiveIsFinal)
				{
					_state.ReadState = ReadState.ReadsCompleted;
				}
				return new ValueTask<int>(num);
			}
			}
		}
		Exception ex = null;
		return ValueTask.FromException<int>(ExceptionDispatchInfo.SetCurrentStackTrace(readState switch
		{
			ReadState.PendingRead => new InvalidOperationException("Only one read is supported at a time."), 
			ReadState.Aborted => flag ? new OperationCanceledException(cancellationToken) : ThrowHelper.GetStreamAbortedException(readErrorCode), 
			_ => GetConnectionAbortedException(_state), 
		}));
	}

	private unsafe static int CopyMsQuicBuffersToUserBuffer(ReadOnlySpan<MsQuicNativeMethods.QuicBuffer> sourceBuffers, Span<byte> destinationBuffer)
	{
		if (sourceBuffers.Length == 0)
		{
			return 0;
		}
		int length = destinationBuffer.Length;
		int num = 0;
		int num2 = 0;
		do
		{
			MsQuicNativeMethods.QuicBuffer quicBuffer = sourceBuffers[num2];
			num = Math.Min((int)quicBuffer.Length, destinationBuffer.Length);
			new Span<byte>(quicBuffer.Buffer, num).CopyTo(destinationBuffer);
			destinationBuffer = destinationBuffer.Slice(num);
		}
		while (destinationBuffer.Length != 0 && ++num2 < sourceBuffers.Length);
		return length - destinationBuffer.Length;
	}

	internal override void AbortRead(long errorCode)
	{
		if (_disposed != 1)
		{
			bool flag = false;
			lock (_state)
			{
				flag = CleanupReadStateAndCheckPending(_state, ReadState.Aborted);
			}
			if (flag)
			{
				_state.ReceiveResettableCompletionSource.CompleteException(ExceptionDispatchInfo.SetCurrentStackTrace(new QuicOperationAbortedException("Read was aborted")));
			}
			StartShutdown(QUIC_STREAM_SHUTDOWN_FLAGS.ABORT_RECEIVE, errorCode);
		}
	}

	internal override void AbortWrite(long errorCode)
	{
		if (_disposed == 1)
		{
			return;
		}
		bool flag = false;
		lock (_state)
		{
			if (_state.SendState < SendState.Aborted)
			{
				_state.SendState = SendState.Aborted;
			}
			if (_state.ShutdownWriteState == ShutdownWriteState.None)
			{
				_state.ShutdownWriteState = ShutdownWriteState.Canceled;
				flag = true;
			}
		}
		if (flag)
		{
			_state.ShutdownWriteCompletionSource.SetException(ExceptionDispatchInfo.SetCurrentStackTrace(new QuicOperationAbortedException("Write was aborted.")));
		}
		StartShutdown(QUIC_STREAM_SHUTDOWN_FLAGS.ABORT_SEND, errorCode);
	}

	private void StartShutdown(QUIC_STREAM_SHUTDOWN_FLAGS flags, long errorCode)
	{
		uint status = MsQuicApi.Api.StreamShutdownDelegate(_state.Handle, flags, errorCode);
		QuicExceptionHelpers.ThrowIfFailed(status, "StreamShutdown failed.");
	}

	internal override async ValueTask ShutdownCompleted(CancellationToken cancellationToken = default(CancellationToken))
	{
		ThrowIfDisposed();
		lock (_state)
		{
			if (_state.ShutdownState == ShutdownState.ConnectionClosed)
			{
				throw GetConnectionAbortedException(_state);
			}
		}
		using (cancellationToken.UnsafeRegister(delegate(object s, CancellationToken token)
		{
			State state = (State)s;
			bool flag = false;
			lock (state)
			{
				if (state.ShutdownState == ShutdownState.None)
				{
					state.ShutdownState = ShutdownState.Canceled;
					flag = true;
				}
			}
			if (flag)
			{
				state.ShutdownCompletionSource.SetException(ExceptionDispatchInfo.SetCurrentStackTrace(new OperationCanceledException("Wait for shutdown was canceled", token)));
			}
		}, _state))
		{
			await _state.ShutdownCompletionSource.Task.ConfigureAwait(continueOnCapturedContext: false);
		}
	}

	internal override ValueTask WaitForWriteCompletionAsync(CancellationToken cancellationToken = default(CancellationToken))
	{
		ThrowIfDisposed();
		lock (_state)
		{
			if (_state.ShutdownWriteState == ShutdownWriteState.ConnectionClosed)
			{
				throw GetConnectionAbortedException(_state);
			}
		}
		return new ValueTask(_state.ShutdownWriteCompletionSource.Task.WaitAsync(cancellationToken));
	}

	internal override void Shutdown()
	{
		ThrowIfDisposed();
		lock (_state)
		{
			if (_state.SendState < SendState.Finished)
			{
				_state.SendState = SendState.Finished;
			}
		}
		StartShutdown(QUIC_STREAM_SHUTDOWN_FLAGS.GRACEFUL, 0L);
	}

	internal override int Read(Span<byte> buffer)
	{
		ThrowIfDisposed();
		byte[] array = ArrayPool<byte>.Shared.Rent(buffer.Length);
		CancellationTokenSource cancellationTokenSource = null;
		try
		{
			if (_readTimeout > 0)
			{
				cancellationTokenSource = new CancellationTokenSource(_readTimeout);
			}
			int result = ReadAsync(new Memory<byte>(array, 0, buffer.Length), cancellationTokenSource?.Token ?? default(CancellationToken)).AsTask().GetAwaiter().GetResult();
			array.AsSpan(0, result).CopyTo(buffer);
			return result;
		}
		catch (OperationCanceledException) when (cancellationTokenSource?.IsCancellationRequested ?? false)
		{
			throw new IOException(System.SR.net_quic_timeout);
		}
		finally
		{
			ArrayPool<byte>.Shared.Return(array);
			cancellationTokenSource?.Dispose();
		}
	}

	internal override void Write(ReadOnlySpan<byte> buffer)
	{
		ThrowIfDisposed();
		CancellationTokenSource cancellationTokenSource = null;
		if (_writeTimeout > 0)
		{
			cancellationTokenSource = new CancellationTokenSource(_writeTimeout);
		}
		try
		{
			WriteAsync(buffer.ToArray()).AsTask().GetAwaiter().GetResult();
		}
		catch (OperationCanceledException) when (cancellationTokenSource?.IsCancellationRequested ?? false)
		{
			throw new IOException(System.SR.net_quic_timeout);
		}
		finally
		{
			cancellationTokenSource?.Dispose();
		}
	}

	internal override void Flush()
	{
		ThrowIfDisposed();
	}

	internal override Task FlushAsync(CancellationToken cancellationToken = default(CancellationToken))
	{
		ThrowIfDisposed();
		return Task.CompletedTask;
	}

	public override ValueTask DisposeAsync()
	{
		Dispose(disposing: true);
		return default(ValueTask);
	}

	public override void Dispose()
	{
		Dispose(disposing: true);
		GC.SuppressFinalize(this);
	}

	~MsQuicStream()
	{
		Dispose(disposing: false);
	}

	private void Dispose(bool disposing)
	{
		if (Interlocked.Exchange(ref _disposed, 1) != 0)
		{
			return;
		}
		if (System.Net.NetEventSource.Log.IsEnabled())
		{
			System.Net.NetEventSource.Info(_state, $"{TraceId()} Stream disposing {disposing}", "Dispose");
		}
		bool flag = false;
		bool flag2 = false;
		bool flag3 = false;
		bool flag4 = false;
		lock (_state)
		{
			if (_state.SendState < SendState.Aborted)
			{
				flag = true;
			}
			if (_state.ReadState < ReadState.ReadsCompleted || _state.ReadState == ReadState.Aborted)
			{
				flag2 = true;
				flag3 = CleanupReadStateAndCheckPending(_state, ReadState.Aborted);
			}
			if (_state.ShutdownState == ShutdownState.None)
			{
				_state.ShutdownState = ShutdownState.Pending;
			}
			flag4 = Interlocked.Exchange(ref _state.ShutdownDone, 1) == 2;
			if (flag4)
			{
				_state.ShutdownState = ShutdownState.Finished;
			}
		}
		if (flag)
		{
			try
			{
				StartShutdown(QUIC_STREAM_SHUTDOWN_FLAGS.GRACEFUL, 0L);
			}
			catch (ObjectDisposedException)
			{
			}
		}
		if (flag2)
		{
			try
			{
				StartShutdown(QUIC_STREAM_SHUTDOWN_FLAGS.ABORT_RECEIVE, 4294967295L);
			}
			catch (ObjectDisposedException)
			{
			}
		}
		if (flag3)
		{
			_state.ReceiveResettableCompletionSource.CompleteException(ExceptionDispatchInfo.SetCurrentStackTrace(new QuicOperationAbortedException("Read was canceled")));
		}
		if (flag4)
		{
			_state.Cleanup();
		}
		if (System.Net.NetEventSource.Log.IsEnabled())
		{
			System.Net.NetEventSource.Info(_state, $"{TraceId()} Stream disposed", "Dispose");
		}
	}

	private void EnableReceive()
	{
		uint status = MsQuicApi.Api.StreamReceiveSetEnabledDelegate(_state.Handle, enabled: true);
		QuicExceptionHelpers.ThrowIfFailed(status, "StreamReceiveSetEnabled failed.");
	}

	private static uint NativeCallbackHandler(IntPtr stream, IntPtr context, ref MsQuicNativeMethods.StreamEvent streamEvent)
	{
		State state = (State)GCHandle.FromIntPtr(context).Target;
		return HandleEvent(state, ref streamEvent);
	}

	private static uint HandleEvent(State state, ref MsQuicNativeMethods.StreamEvent evt)
	{
		if (System.Net.NetEventSource.Log.IsEnabled())
		{
			System.Net.NetEventSource.Info(state, $"{state.TraceId} Stream received event {evt.Type}", "HandleEvent");
		}
		try
		{
			return evt.Type switch
			{
				QUIC_STREAM_EVENT_TYPE.START_COMPLETE => HandleEventStartComplete(state, ref evt), 
				QUIC_STREAM_EVENT_TYPE.RECEIVE => HandleEventRecv(state, ref evt), 
				QUIC_STREAM_EVENT_TYPE.SEND_COMPLETE => HandleEventSendComplete(state, ref evt), 
				QUIC_STREAM_EVENT_TYPE.PEER_SEND_SHUTDOWN => HandleEventPeerSendShutdown(state), 
				QUIC_STREAM_EVENT_TYPE.PEER_SEND_ABORTED => HandleEventPeerSendAborted(state, ref evt), 
				QUIC_STREAM_EVENT_TYPE.PEER_RECEIVE_ABORTED => HandleEventPeerRecvAborted(state, ref evt), 
				QUIC_STREAM_EVENT_TYPE.SEND_SHUTDOWN_COMPLETE => HandleEventSendShutdownComplete(state, ref evt), 
				QUIC_STREAM_EVENT_TYPE.SHUTDOWN_COMPLETE => HandleEventShutdownComplete(state, ref evt), 
				_ => 0u, 
			};
		}
		catch (Exception ex)
		{
			if (System.Net.NetEventSource.Log.IsEnabled())
			{
				System.Net.NetEventSource.Error(state, $"{state.TraceId} Exception occurred during handling Stream {evt.Type} event: {ex}", "HandleEvent");
			}
			return 2151743491u;
		}
	}

	private unsafe static uint HandleEventRecv(State state, ref MsQuicNativeMethods.StreamEvent evt)
	{
		ref MsQuicNativeMethods.StreamEventDataReceive receive = ref evt.Data.Receive;
		if (System.Net.NetEventSource.Log.IsEnabled())
		{
			System.Net.NetEventSource.Info(state, FormattableStringFactory.Create("{0} Stream received {1} bytes{2}", state.TraceId, receive.TotalBufferLength, receive.Flags.HasFlag(QUIC_RECEIVE_FLAGS.FIN) ? " with FIN flag" : ""), "HandleEventRecv");
		}
		bool flag = false;
		int num;
		lock (state)
		{
			switch (state.ReadState)
			{
			case ReadState.None:
			{
				if ((uint)state.ReceiveQuicBuffers.Length < receive.BufferCount)
				{
					MsQuicNativeMethods.QuicBuffer[] receiveQuicBuffers = state.ReceiveQuicBuffers;
					state.ReceiveQuicBuffers = ArrayPool<MsQuicNativeMethods.QuicBuffer>.Shared.Rent((int)receive.BufferCount);
					if (receiveQuicBuffers.Length != 0)
					{
						ArrayPool<MsQuicNativeMethods.QuicBuffer>.Shared.Return(receiveQuicBuffers);
					}
				}
				for (uint num2 = 0u; num2 < receive.BufferCount; num2++)
				{
					state.ReceiveQuicBuffers[num2] = receive.Buffers[num2];
				}
				state.ReceiveQuicBuffersCount = (int)receive.BufferCount;
				state.ReceiveQuicBuffersTotalBytes = checked((int)receive.TotalBufferLength);
				state.ReceiveIsFinal = receive.Flags.HasFlag(QUIC_RECEIVE_FLAGS.FIN);
				if (state.ReceiveQuicBuffersTotalBytes == 0)
				{
					if (state.ReceiveIsFinal)
					{
						state.ReadState = ReadState.ReadsCompleted;
					}
					return 0u;
				}
				state.ReadState = ReadState.IndividualReadComplete;
				return 459749u;
			}
			case ReadState.PendingRead:
				state.ReceiveCancellationRegistration.Unregister();
				flag = true;
				state.Stream = null;
				state.ReadState = ReadState.None;
				num = CopyMsQuicBuffersToUserBuffer(new ReadOnlySpan<MsQuicNativeMethods.QuicBuffer>(receive.Buffers, (int)receive.BufferCount), state.ReceiveUserBuffer.Span);
				if (receive.Flags.HasFlag(QUIC_RECEIVE_FLAGS.FIN) && (uint)num == receive.TotalBufferLength)
				{
					state.ReadState = ReadState.ReadsCompleted;
				}
				state.ReceiveUserBuffer = null;
				break;
			default:
				return 0u;
			}
		}
		if (flag)
		{
			state.ReceiveResettableCompletionSource.Complete(num);
		}
		uint result = (((uint)num != receive.TotalBufferLength) ? 459998u : 0u);
		receive.TotalBufferLength = (uint)num;
		return result;
	}

	private static uint HandleEventPeerRecvAborted(State state, ref MsQuicNativeMethods.StreamEvent evt)
	{
		bool flag = false;
		bool flag2 = false;
		lock (state)
		{
			if (state.SendState == SendState.None || state.SendState == SendState.Pending)
			{
				flag = true;
			}
			if (state.ShutdownWriteState == ShutdownWriteState.None)
			{
				state.ShutdownWriteState = ShutdownWriteState.Canceled;
				flag2 = true;
			}
			state.SendState = SendState.Aborted;
			state.SendErrorCode = evt.Data.PeerReceiveAborted.ErrorCode;
		}
		if (flag)
		{
			state.SendResettableCompletionSource.CompleteException(ExceptionDispatchInfo.SetCurrentStackTrace(new QuicStreamAbortedException(state.SendErrorCode)));
		}
		if (flag2)
		{
			state.ShutdownWriteCompletionSource.SetException(ExceptionDispatchInfo.SetCurrentStackTrace(new QuicStreamAbortedException(state.SendErrorCode)));
		}
		return 0u;
	}

	private static uint HandleEventStartComplete(State state, ref MsQuicNativeMethods.StreamEvent evt)
	{
		return 0u;
	}

	private static uint HandleEventSendShutdownComplete(State state, ref MsQuicNativeMethods.StreamEvent evt)
	{
		if (evt.Data.SendShutdownComplete.Graceful != 0)
		{
			bool flag = false;
			lock (state)
			{
				if (state.ShutdownWriteState == ShutdownWriteState.None)
				{
					state.ShutdownWriteState = ShutdownWriteState.Finished;
					flag = true;
				}
			}
			if (flag)
			{
				state.ShutdownWriteCompletionSource.SetResult();
			}
		}
		return 0u;
	}

	private static uint HandleEventShutdownComplete(State state, ref MsQuicNativeMethods.StreamEvent evt)
	{
		MsQuicNativeMethods.StreamEventDataShutdownComplete shutdownComplete = evt.Data.ShutdownComplete;
		if (shutdownComplete.ConnectionShutdown != 0)
		{
			return HandleEventConnectionClose(state);
		}
		bool flag = false;
		bool flag2 = false;
		bool flag3 = false;
		lock (state)
		{
			if (System.Net.NetEventSource.Log.IsEnabled())
			{
				System.Net.NetEventSource.Info(state, $"{state.TraceId} Stream completing resettable event source.", "HandleEventShutdownComplete");
			}
			flag = CleanupReadStateAndCheckPending(state, ReadState.ReadsCompleted);
			if (state.ShutdownWriteState == ShutdownWriteState.None)
			{
				state.ShutdownWriteState = ShutdownWriteState.Finished;
				flag2 = true;
			}
			if (state.ShutdownState == ShutdownState.None)
			{
				state.ShutdownState = ShutdownState.Finished;
				flag3 = true;
			}
		}
		if (flag)
		{
			state.ReceiveResettableCompletionSource.Complete(0);
		}
		if (flag2)
		{
			state.ShutdownWriteCompletionSource.SetResult();
		}
		if (flag3)
		{
			state.ShutdownCompletionSource.SetResult();
		}
		if (Interlocked.Exchange(ref state.ShutdownDone, 2) == 1)
		{
			state.Cleanup();
		}
		return 0u;
	}

	private static uint HandleEventPeerSendAborted(State state, ref MsQuicNativeMethods.StreamEvent evt)
	{
		bool flag = false;
		lock (state)
		{
			flag = CleanupReadStateAndCheckPending(state, ReadState.Aborted);
			state.ReadErrorCode = evt.Data.PeerSendAborted.ErrorCode;
		}
		if (flag)
		{
			state.ReceiveResettableCompletionSource.CompleteException(ExceptionDispatchInfo.SetCurrentStackTrace(new QuicStreamAbortedException(state.ReadErrorCode)));
		}
		return 0u;
	}

	private static uint HandleEventPeerSendShutdown(State state)
	{
		bool flag = false;
		lock (state)
		{
			if (System.Net.NetEventSource.Log.IsEnabled())
			{
				System.Net.NetEventSource.Info(state, $"{state.TraceId} Stream completing resettable event source.", "HandleEventPeerSendShutdown");
			}
			flag = CleanupReadStateAndCheckPending(state, ReadState.ReadsCompleted);
		}
		if (flag)
		{
			state.ReceiveResettableCompletionSource.Complete(0);
		}
		return 0u;
	}

	private static uint HandleEventSendComplete(State state, ref MsQuicNativeMethods.StreamEvent evt)
	{
		MsQuicNativeMethods.StreamEventDataSendComplete sendComplete = evt.Data.SendComplete;
		bool flag = sendComplete.Canceled != 0;
		bool flag2 = false;
		lock (state)
		{
			if (state.SendState == SendState.Pending)
			{
				state.SendState = SendState.Finished;
				flag2 = true;
			}
			if (flag)
			{
				state.SendState = SendState.Aborted;
			}
		}
		if (flag2)
		{
			CleanupSendState(state);
			if (!flag)
			{
				state.SendResettableCompletionSource.Complete(0u);
			}
			else
			{
				state.SendResettableCompletionSource.CompleteException(ExceptionDispatchInfo.SetCurrentStackTrace(new OperationCanceledException("Write was canceled")));
			}
		}
		return 0u;
	}

	private static void CleanupSendState(State state)
	{
		lock (state)
		{
			for (int i = 0; i < state.SendBufferCount; i++)
			{
				state.BufferArrays[i].Dispose();
			}
		}
	}

	private unsafe ValueTask SendReadOnlyMemoryAsync(ReadOnlyMemory<byte> buffer, QUIC_SEND_FLAGS flags)
	{
		if (buffer.IsEmpty)
		{
			if ((flags & QUIC_SEND_FLAGS.FIN) == QUIC_SEND_FLAGS.FIN)
			{
				StartShutdown(QUIC_STREAM_SHUTDOWN_FLAGS.GRACEFUL, 0L);
			}
			return default(ValueTask);
		}
		MemoryHandle memoryHandle = buffer.Pin();
		if (_state.SendQuicBuffers == IntPtr.Zero)
		{
			_state.SendQuicBuffers = Marshal.AllocHGlobal(sizeof(MsQuicNativeMethods.QuicBuffer));
			_state.SendBufferMaxCount = 1;
		}
		MsQuicNativeMethods.QuicBuffer* ptr = (MsQuicNativeMethods.QuicBuffer*)(void*)_state.SendQuicBuffers;
		ptr->Length = (uint)buffer.Length;
		ptr->Buffer = (byte*)memoryHandle.Pointer;
		_state.BufferArrays[0] = memoryHandle;
		_state.SendBufferCount = 1;
		uint status = MsQuicApi.Api.StreamSendDelegate(_state.Handle, ptr, 1u, flags, IntPtr.Zero);
		if (!MsQuicStatusHelper.SuccessfulStatusCode(status))
		{
			HandleWriteFailedState();
			CleanupSendState(_state);
			QuicExceptionHelpers.ThrowIfFailed(status, "Could not send data to peer.");
		}
		return _state.SendResettableCompletionSource.GetTypelessValueTask();
	}

	private unsafe ValueTask SendReadOnlySequenceAsync(ReadOnlySequence<byte> buffers, QUIC_SEND_FLAGS flags)
	{
		if (buffers.IsEmpty)
		{
			if ((flags & QUIC_SEND_FLAGS.FIN) == QUIC_SEND_FLAGS.FIN)
			{
				StartShutdown(QUIC_STREAM_SHUTDOWN_FLAGS.GRACEFUL, 0L);
			}
			return default(ValueTask);
		}
		int num = 0;
		ReadOnlySequence<byte>.Enumerator enumerator = buffers.GetEnumerator();
		while (enumerator.MoveNext())
		{
			ReadOnlyMemory<byte> current = enumerator.Current;
			num++;
		}
		if (_state.SendBufferMaxCount < num)
		{
			Marshal.FreeHGlobal(_state.SendQuicBuffers);
			_state.SendQuicBuffers = IntPtr.Zero;
			_state.SendQuicBuffers = Marshal.AllocHGlobal(sizeof(MsQuicNativeMethods.QuicBuffer) * num);
			_state.SendBufferMaxCount = num;
			_state.BufferArrays = new MemoryHandle[num];
		}
		_state.SendBufferCount = num;
		num = 0;
		MsQuicNativeMethods.QuicBuffer* ptr = (MsQuicNativeMethods.QuicBuffer*)(void*)_state.SendQuicBuffers;
		ReadOnlySequence<byte>.Enumerator enumerator2 = buffers.GetEnumerator();
		while (enumerator2.MoveNext())
		{
			ReadOnlyMemory<byte> current2 = enumerator2.Current;
			MemoryHandle memoryHandle = current2.Pin();
			ptr[num].Length = (uint)current2.Length;
			ptr[num].Buffer = (byte*)memoryHandle.Pointer;
			_state.BufferArrays[num] = memoryHandle;
			num++;
		}
		uint status = MsQuicApi.Api.StreamSendDelegate(_state.Handle, ptr, (uint)num, flags, IntPtr.Zero);
		if (!MsQuicStatusHelper.SuccessfulStatusCode(status))
		{
			HandleWriteFailedState();
			CleanupSendState(_state);
			QuicExceptionHelpers.ThrowIfFailed(status, "Could not send data to peer.");
		}
		return _state.SendResettableCompletionSource.GetTypelessValueTask();
	}

	private unsafe ValueTask SendReadOnlyMemoryListAsync(ReadOnlyMemory<ReadOnlyMemory<byte>> buffers, QUIC_SEND_FLAGS flags)
	{
		if (buffers.IsEmpty)
		{
			if ((flags & QUIC_SEND_FLAGS.FIN) == QUIC_SEND_FLAGS.FIN)
			{
				StartShutdown(QUIC_STREAM_SHUTDOWN_FLAGS.GRACEFUL, 0L);
			}
			return default(ValueTask);
		}
		ReadOnlyMemory<byte>[] array = buffers.ToArray();
		uint num = (uint)array.Length;
		if (_state.SendBufferMaxCount < array.Length)
		{
			Marshal.FreeHGlobal(_state.SendQuicBuffers);
			_state.SendQuicBuffers = IntPtr.Zero;
			_state.SendQuicBuffers = Marshal.AllocHGlobal(sizeof(MsQuicNativeMethods.QuicBuffer) * array.Length);
			_state.SendBufferMaxCount = array.Length;
			_state.BufferArrays = new MemoryHandle[array.Length];
		}
		_state.SendBufferCount = array.Length;
		MsQuicNativeMethods.QuicBuffer* ptr = (MsQuicNativeMethods.QuicBuffer*)(void*)_state.SendQuicBuffers;
		for (int i = 0; i < num; i++)
		{
			ReadOnlyMemory<byte> readOnlyMemory = array[i];
			MemoryHandle memoryHandle = readOnlyMemory.Pin();
			ptr[i].Length = (uint)readOnlyMemory.Length;
			ptr[i].Buffer = (byte*)memoryHandle.Pointer;
			_state.BufferArrays[i] = memoryHandle;
		}
		uint status = MsQuicApi.Api.StreamSendDelegate(_state.Handle, ptr, num, flags, IntPtr.Zero);
		if (!MsQuicStatusHelper.SuccessfulStatusCode(status))
		{
			HandleWriteFailedState();
			CleanupSendState(_state);
			QuicExceptionHelpers.ThrowIfFailed(status, "Could not send data to peer.");
		}
		return _state.SendResettableCompletionSource.GetTypelessValueTask();
	}

	private void ReceiveComplete(int bufferLength)
	{
		uint status = MsQuicApi.Api.StreamReceiveCompleteDelegate(_state.Handle, (ulong)bufferLength);
		QuicExceptionHelpers.ThrowIfFailed(status, "Could not complete receive call.");
	}

	private long GetStreamId()
	{
		return (long)MsQuicParameterHelpers.GetULongParam(MsQuicApi.Api, _state.Handle, QUIC_PARAM_LEVEL.STREAM, 0u);
	}

	private void ThrowIfDisposed()
	{
		if (_disposed == 1)
		{
			throw new ObjectDisposedException("MsQuicStream");
		}
	}

	private static uint HandleEventConnectionClose(State state)
	{
		long abortErrorCode = state.ConnectionState.AbortErrorCode;
		if (System.Net.NetEventSource.Log.IsEnabled())
		{
			System.Net.NetEventSource.Info(state, state.TraceId + " Stream handling connection " + state.ConnectionState.TraceId + " close" + ((abortErrorCode != -1) ? $" with code {abortErrorCode}" : ""), "HandleEventConnectionClose");
		}
		bool flag = false;
		bool flag2 = false;
		bool flag3 = false;
		bool flag4 = false;
		lock (state)
		{
			flag = CleanupReadStateAndCheckPending(state, ReadState.ConnectionClosed);
			if (state.SendState == SendState.None || state.SendState == SendState.Pending)
			{
				flag2 = true;
			}
			state.SendState = SendState.ConnectionClosed;
			if (state.ShutdownWriteState == ShutdownWriteState.None)
			{
				flag3 = true;
			}
			state.ShutdownWriteState = ShutdownWriteState.ConnectionClosed;
			if (state.ShutdownState == ShutdownState.None)
			{
				flag4 = true;
			}
			state.ShutdownState = ShutdownState.ConnectionClosed;
		}
		if (flag)
		{
			state.ReceiveResettableCompletionSource.CompleteException(ExceptionDispatchInfo.SetCurrentStackTrace(GetConnectionAbortedException(state)));
		}
		if (flag2)
		{
			state.SendResettableCompletionSource.CompleteException(ExceptionDispatchInfo.SetCurrentStackTrace(GetConnectionAbortedException(state)));
		}
		if (flag3)
		{
			state.ShutdownWriteCompletionSource.SetException(ExceptionDispatchInfo.SetCurrentStackTrace(GetConnectionAbortedException(state)));
		}
		if (flag4)
		{
			state.ShutdownCompletionSource.SetException(ExceptionDispatchInfo.SetCurrentStackTrace(GetConnectionAbortedException(state)));
		}
		if (Interlocked.Exchange(ref state.ShutdownDone, 2) == 1)
		{
			state.Cleanup();
		}
		return 0u;
	}

	private static Exception GetConnectionAbortedException(State state)
	{
		return ThrowHelper.GetConnectionAbortedException(state.ConnectionState.AbortErrorCode);
	}

	private static bool CleanupReadStateAndCheckPending(State state, ReadState finalState)
	{
		bool result = false;
		if (state.ReadState == ReadState.PendingRead)
		{
			result = true;
			state.Stream = null;
			state.ReceiveUserBuffer = null;
			state.ReceiveCancellationRegistration.Unregister();
		}
		if (state.ReadState < ReadState.ReadsCompleted)
		{
			state.ReadState = finalState;
		}
		return result;
	}
}
