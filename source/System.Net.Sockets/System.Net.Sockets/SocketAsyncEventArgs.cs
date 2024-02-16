using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Net.Internals;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Sources;
using Microsoft.Win32.SafeHandles;

namespace System.Net.Sockets;

public class SocketAsyncEventArgs : EventArgs, IDisposable
{
	private sealed class MultiConnectSocketAsyncEventArgs : SocketAsyncEventArgs, IValueTaskSource
	{
		private ManualResetValueTaskSourceCore<bool> _mrvtsc;

		private int _isCompleted;

		public short Version => _mrvtsc.Version;

		public MultiConnectSocketAsyncEventArgs()
			: base(unsafeSuppressExecutionContextFlow: false)
		{
		}

		public void GetResult(short token)
		{
			_mrvtsc.GetResult(token);
		}

		public ValueTaskSourceStatus GetStatus(short token)
		{
			return _mrvtsc.GetStatus(token);
		}

		public void OnCompleted(Action<object> continuation, object state, short token, ValueTaskSourceOnCompletedFlags flags)
		{
			_mrvtsc.OnCompleted(continuation, state, token, flags);
		}

		public void Reset()
		{
			_mrvtsc.Reset();
		}

		protected override void OnCompleted(SocketAsyncEventArgs e)
		{
			_mrvtsc.SetResult(result: true);
		}

		public bool ReachedCoordinationPointFirst()
		{
			return Interlocked.Exchange(ref _isCompleted, 1) == 0;
		}
	}

	private enum AsyncProcessingState : byte
	{
		None,
		InProcess,
		Set
	}

	private enum PinState : byte
	{
		None,
		MultipleBuffer,
		SendPackets
	}

	private Socket _acceptSocket;

	private Socket _connectSocket;

	private Memory<byte> _buffer;

	private int _offset;

	private int _count;

	private bool _bufferIsExplicitArray;

	private IList<ArraySegment<byte>> _bufferList;

	private List<ArraySegment<byte>> _bufferListInternal;

	private int _bytesTransferred;

	private bool _disconnectReuseSocket;

	private SocketAsyncOperation _completedOperation;

	private IPPacketInformation _receiveMessageFromPacketInfo;

	private EndPoint _remoteEndPoint;

	private int _sendPacketsSendSize;

	private SendPacketsElement[] _sendPacketsElements;

	private TransmitFileOptions _sendPacketsFlags;

	private SocketError _socketError;

	private Exception _connectByNameError;

	private SocketFlags _socketFlags;

	private object _userToken;

	private byte[] _acceptBuffer;

	private int _acceptAddressBufferCount;

	internal System.Net.Internals.SocketAddress _socketAddress;

	private readonly bool _flowExecutionContext;

	private ExecutionContext _context;

	private static readonly ContextCallback s_executionCallback = ExecutionCallback;

	private Socket _currentSocket;

	private bool _userSocket;

	private bool _disposeCalled;

	private int _operating;

	private CancellationTokenSource _multipleConnectCancellation;

	private volatile AsyncProcessingState _asyncProcessingState;

	private MemoryHandle _singleBufferHandle;

	private WSABuffer[] _wsaBufferArrayPinned;

	private MemoryHandle[] _multipleBufferMemoryHandles;

	private byte[] _wsaMessageBufferPinned;

	private byte[] _controlBufferPinned;

	private WSABuffer[] _wsaRecvMsgWSABufferArrayPinned;

	private GCHandle _socketAddressGCHandle;

	private System.Net.Internals.SocketAddress _pinnedSocketAddress;

	private SafeFileHandle[] _sendPacketsFileHandles;

	private PreAllocatedOverlapped _preAllocatedOverlapped;

	private readonly StrongBox<SocketAsyncEventArgs> _strongThisRef = new StrongBox<SocketAsyncEventArgs>();

	private CancellationTokenRegistration _registrationToCancelPendingIO;

	private unsafe NativeOverlapped* _pendingOverlappedForCancellation;

	private PinState _pinState;

	private unsafe static readonly IOCompletionCallback s_completionPortCallback = delegate(uint errorCode, uint numBytes, NativeOverlapped* nativeOverlapped)
	{
		StrongBox<SocketAsyncEventArgs> strongBox = (StrongBox<SocketAsyncEventArgs>)ThreadPoolBoundHandle.GetNativeOverlappedState(nativeOverlapped);
		SocketAsyncEventArgs value = strongBox.Value;
		if (errorCode == 0)
		{
			value.FreeNativeOverlapped(nativeOverlapped);
			value.FinishOperationAsyncSuccess((int)numBytes, SocketFlags.None);
		}
		else
		{
			value.HandleCompletionPortCallbackError(errorCode, numBytes, nativeOverlapped);
		}
	};

	public Socket? AcceptSocket
	{
		get
		{
			return _acceptSocket;
		}
		set
		{
			_acceptSocket = value;
		}
	}

	public Socket? ConnectSocket => _connectSocket;

	public byte[]? Buffer
	{
		get
		{
			if (_bufferIsExplicitArray)
			{
				ArraySegment<byte> segment;
				bool flag = MemoryMarshal.TryGetArray((ReadOnlyMemory<byte>)_buffer, out segment);
				return segment.Array;
			}
			return null;
		}
	}

	public Memory<byte> MemoryBuffer => _buffer;

	public int Offset => _offset;

	public int Count => _count;

	public TransmitFileOptions SendPacketsFlags
	{
		get
		{
			return _sendPacketsFlags;
		}
		set
		{
			_sendPacketsFlags = value;
		}
	}

	public IList<ArraySegment<byte>>? BufferList
	{
		get
		{
			return _bufferList;
		}
		set
		{
			StartConfiguring();
			try
			{
				if (value != null)
				{
					if (!_buffer.Equals(default(Memory<byte>)))
					{
						throw new ArgumentException(System.SR.Format(System.SR.net_ambiguousbuffers, "Buffer"));
					}
					int count = value.Count;
					if (_bufferListInternal == null)
					{
						_bufferListInternal = new List<ArraySegment<byte>>(count);
					}
					else
					{
						_bufferListInternal.Clear();
					}
					for (int i = 0; i < count; i++)
					{
						ArraySegment<byte> arraySegment = value[i];
						RangeValidationHelpers.ValidateSegment(arraySegment);
						_bufferListInternal.Add(arraySegment);
					}
				}
				else
				{
					_bufferListInternal?.Clear();
				}
				_bufferList = value;
				SetupMultipleBuffers();
			}
			finally
			{
				Complete();
			}
		}
	}

	public int BytesTransferred => _bytesTransferred;

	public bool DisconnectReuseSocket
	{
		get
		{
			return _disconnectReuseSocket;
		}
		set
		{
			_disconnectReuseSocket = value;
		}
	}

	public SocketAsyncOperation LastOperation => _completedOperation;

	public IPPacketInformation ReceiveMessageFromPacketInfo => _receiveMessageFromPacketInfo;

	public EndPoint? RemoteEndPoint
	{
		get
		{
			return _remoteEndPoint;
		}
		set
		{
			_remoteEndPoint = value;
		}
	}

	public SendPacketsElement[]? SendPacketsElements
	{
		get
		{
			return _sendPacketsElements;
		}
		set
		{
			StartConfiguring();
			try
			{
				_sendPacketsElements = value;
			}
			finally
			{
				Complete();
			}
		}
	}

	public int SendPacketsSendSize
	{
		get
		{
			return _sendPacketsSendSize;
		}
		set
		{
			_sendPacketsSendSize = value;
		}
	}

	public SocketError SocketError
	{
		get
		{
			return _socketError;
		}
		set
		{
			_socketError = value;
		}
	}

	public Exception? ConnectByNameError => _connectByNameError;

	public SocketFlags SocketFlags
	{
		get
		{
			return _socketFlags;
		}
		set
		{
			_socketFlags = value;
		}
	}

	public object? UserToken
	{
		get
		{
			return _userToken;
		}
		set
		{
			_userToken = value;
		}
	}

	internal bool HasMultipleBuffers => _bufferList != null;

	private unsafe IntPtr PtrSocketAddressBuffer
	{
		get
		{
			fixed (byte* ptr = &_pinnedSocketAddress.Buffer[0])
			{
				void* ptr2 = ptr;
				return (IntPtr)ptr2;
			}
		}
	}

	private IntPtr PtrSocketAddressBufferSize => PtrSocketAddressBuffer + _socketAddress.GetAddressSizeOffset();

	public event EventHandler<SocketAsyncEventArgs>? Completed;

	public SocketAsyncEventArgs()
		: this(unsafeSuppressExecutionContextFlow: false)
	{
	}

	public SocketAsyncEventArgs(bool unsafeSuppressExecutionContextFlow)
	{
		_flowExecutionContext = !unsafeSuppressExecutionContextFlow;
		InitializeInternals();
	}

	private void OnCompletedInternal()
	{
		if (SocketsTelemetry.Log.IsEnabled())
		{
			AfterConnectAcceptTelemetry();
		}
		OnCompleted(this);
	}

	protected virtual void OnCompleted(SocketAsyncEventArgs e)
	{
		this.Completed?.Invoke(e._currentSocket, e);
	}

	private void AfterConnectAcceptTelemetry()
	{
		switch (LastOperation)
		{
		case SocketAsyncOperation.Accept:
			SocketsTelemetry.Log.AfterAccept(SocketError);
			break;
		case SocketAsyncOperation.Connect:
			SocketsTelemetry.Log.AfterConnect(SocketError);
			break;
		}
	}

	public void SetBuffer(int offset, int count)
	{
		StartConfiguring();
		try
		{
			if (!_buffer.Equals(default(Memory<byte>)))
			{
				if ((uint)offset > _buffer.Length)
				{
					throw new ArgumentOutOfRangeException("offset");
				}
				if ((uint)count > _buffer.Length - offset)
				{
					throw new ArgumentOutOfRangeException("count");
				}
				if (!_bufferIsExplicitArray)
				{
					throw new InvalidOperationException(System.SR.InvalidOperation_BufferNotExplicitArray);
				}
				_offset = offset;
				_count = count;
			}
		}
		finally
		{
			Complete();
		}
	}

	internal void CopyBufferFrom(SocketAsyncEventArgs source)
	{
		StartConfiguring();
		try
		{
			_buffer = source._buffer;
			_offset = source._offset;
			_count = source._count;
			_bufferIsExplicitArray = source._bufferIsExplicitArray;
		}
		finally
		{
			Complete();
		}
	}

	public void SetBuffer(byte[]? buffer, int offset, int count)
	{
		StartConfiguring();
		try
		{
			if (buffer == null)
			{
				_buffer = default(Memory<byte>);
				_offset = 0;
				_count = 0;
				_bufferIsExplicitArray = false;
				return;
			}
			if (_bufferList != null)
			{
				throw new ArgumentException(System.SR.Format(System.SR.net_ambiguousbuffers, "BufferList"));
			}
			if ((uint)offset > buffer.Length)
			{
				throw new ArgumentOutOfRangeException("offset");
			}
			if ((uint)count > buffer.Length - offset)
			{
				throw new ArgumentOutOfRangeException("count");
			}
			_buffer = buffer;
			_offset = offset;
			_count = count;
			_bufferIsExplicitArray = true;
		}
		finally
		{
			Complete();
		}
	}

	public void SetBuffer(Memory<byte> buffer)
	{
		StartConfiguring();
		try
		{
			if (buffer.Length != 0 && _bufferList != null)
			{
				throw new ArgumentException(System.SR.Format(System.SR.net_ambiguousbuffers, "BufferList"));
			}
			_buffer = buffer;
			_offset = 0;
			_count = buffer.Length;
			_bufferIsExplicitArray = false;
		}
		finally
		{
			Complete();
		}
	}

	internal void SetResults(SocketError socketError, int bytesTransferred, SocketFlags flags)
	{
		_socketError = socketError;
		_connectByNameError = null;
		_bytesTransferred = bytesTransferred;
		_socketFlags = flags;
	}

	internal void SetResults(Exception exception, int bytesTransferred, SocketFlags flags)
	{
		_connectByNameError = exception;
		_bytesTransferred = bytesTransferred;
		_socketFlags = flags;
		if (exception == null)
		{
			_socketError = SocketError.Success;
		}
		else if (exception is SocketException ex)
		{
			_socketError = ex.SocketErrorCode;
		}
		else
		{
			_socketError = SocketError.SocketError;
		}
	}

	private static void ExecutionCallback(object state)
	{
		SocketAsyncEventArgs socketAsyncEventArgs = (SocketAsyncEventArgs)state;
		socketAsyncEventArgs.OnCompletedInternal();
	}

	internal void Complete()
	{
		CompleteCore();
		_context = null;
		_operating = 0;
		if (_disposeCalled)
		{
			Dispose();
		}
	}

	public void Dispose()
	{
		_disposeCalled = true;
		if (Interlocked.CompareExchange(ref _operating, 2, 0) == 0)
		{
			FreeInternals();
			FinishOperationSendPackets();
			GC.SuppressFinalize(this);
		}
	}

	~SocketAsyncEventArgs()
	{
		if (!Environment.HasShutdownStarted)
		{
			FreeInternals();
		}
	}

	private void StartConfiguring()
	{
		int num = Interlocked.CompareExchange(ref _operating, -1, 0);
		if (num != 0)
		{
			ThrowForNonFreeStatus(num);
		}
	}

	private void ThrowForNonFreeStatus(int status)
	{
		throw (status == 2) ? new ObjectDisposedException(GetType().FullName) : new InvalidOperationException(System.SR.net_socketopinprogress);
	}

	internal void StartOperationCommon(Socket socket, SocketAsyncOperation operation)
	{
		int num = Interlocked.CompareExchange(ref _operating, 1, 0);
		if (num != 0)
		{
			ThrowForNonFreeStatus(num);
		}
		_completedOperation = operation;
		_currentSocket = socket;
		if (_flowExecutionContext || (SocketsTelemetry.Log.IsEnabled() && (operation == SocketAsyncOperation.Connect || operation == SocketAsyncOperation.Accept)))
		{
			_context = ExecutionContext.Capture();
		}
		StartOperationCommonCore();
	}

	private void StartOperationCommonCore()
	{
		_strongThisRef.Value = this;
	}

	internal void StartOperationAccept()
	{
		_acceptAddressBufferCount = 2 * (Socket.GetAddressSize(_currentSocket._rightEndPoint) + 16);
		if (!_buffer.Equals(default(Memory<byte>)))
		{
			if (_count < _acceptAddressBufferCount)
			{
				throw new ArgumentException(System.SR.Format(System.SR.net_buffercounttoosmall, "Count"));
			}
		}
		else if (_acceptBuffer == null || _acceptBuffer.Length < _acceptAddressBufferCount)
		{
			_acceptBuffer = new byte[_acceptAddressBufferCount];
		}
	}

	internal void StartOperationConnect(bool saeaMultiConnectCancelable, bool userSocket)
	{
		_multipleConnectCancellation = (saeaMultiConnectCancelable ? new CancellationTokenSource() : null);
		_connectSocket = null;
		_userSocket = userSocket;
	}

	internal void CancelConnectAsync()
	{
		if (_operating == 1 && _completedOperation == SocketAsyncOperation.Connect)
		{
			CancellationTokenSource multipleConnectCancellation = _multipleConnectCancellation;
			if (multipleConnectCancellation != null)
			{
				multipleConnectCancellation.Cancel();
			}
			else
			{
				_currentSocket?.Dispose();
			}
		}
	}

	internal void FinishOperationSyncFailure(SocketError socketError, int bytesTransferred, SocketFlags flags)
	{
		SetResults(socketError, bytesTransferred, flags);
		Socket currentSocket = _currentSocket;
		if (currentSocket != null)
		{
			currentSocket.UpdateStatusAfterSocketError(socketError);
			if (_completedOperation == SocketAsyncOperation.Connect && !_userSocket)
			{
				currentSocket.Dispose();
				_currentSocket = null;
			}
		}
		SocketAsyncOperation completedOperation = _completedOperation;
		if (completedOperation == SocketAsyncOperation.SendPackets)
		{
			FinishOperationSendPackets();
		}
		Complete();
	}

	internal void FinishOperationAsyncFailure(SocketError socketError, int bytesTransferred, SocketFlags flags)
	{
		ExecutionContext context = _context;
		FinishOperationSyncFailure(socketError, bytesTransferred, flags);
		if (context == null)
		{
			OnCompletedInternal();
		}
		else
		{
			ExecutionContext.Run(context, s_executionCallback, this);
		}
	}

	internal bool DnsConnectAsync(DnsEndPoint endPoint, SocketType socketType, ProtocolType protocolType)
	{
		CancellationToken cancellationToken2 = _multipleConnectCancellation?.Token ?? default(CancellationToken);
		Task<IPAddress[]> hostAddressesAsync = Dns.GetHostAddressesAsync(endPoint.Host, endPoint.AddressFamily, cancellationToken2);
		MultiConnectSocketAsyncEventArgs multiConnectSocketAsyncEventArgs = new MultiConnectSocketAsyncEventArgs();
		multiConnectSocketAsyncEventArgs.CopyBufferFrom(this);
		Core(multiConnectSocketAsyncEventArgs, hostAddressesAsync, endPoint.Port, socketType, protocolType, cancellationToken2);
		return multiConnectSocketAsyncEventArgs.ReachedCoordinationPointFirst();
		async Task Core(MultiConnectSocketAsyncEventArgs internalArgs, Task<IPAddress[]> addressesTask, int port, SocketType socketType, ProtocolType protocolType, CancellationToken cancellationToken)
		{
			Socket tempSocketIPv4 = null;
			Socket tempSocketIPv5 = null;
			Exception caughtException = null;
			try
			{
				SocketError lastError = SocketError.NoData;
				IPAddress[] array = await addressesTask.ConfigureAwait(continueOnCapturedContext: false);
				foreach (IPAddress iPAddress in array)
				{
					Socket socket = null;
					if (_currentSocket != null)
					{
						if (!_currentSocket.CanTryAddressFamily(iPAddress.AddressFamily))
						{
							continue;
						}
						socket = _currentSocket;
					}
					else
					{
						if (iPAddress.AddressFamily == AddressFamily.InterNetworkV6)
						{
							Socket socket2 = tempSocketIPv5;
							if (socket2 == null)
							{
								Socket socket3;
								tempSocketIPv5 = (socket3 = (Socket.OSSupportsIPv6 ? new Socket(AddressFamily.InterNetworkV6, socketType, protocolType) : null));
								socket2 = socket3;
							}
							socket = socket2;
							if (socket != null && iPAddress.IsIPv4MappedToIPv6)
							{
								socket.DualMode = true;
							}
						}
						else if (iPAddress.AddressFamily == AddressFamily.InterNetwork)
						{
							Socket socket4 = tempSocketIPv4;
							if (socket4 == null)
							{
								Socket socket3;
								tempSocketIPv4 = (socket3 = (Socket.OSSupportsIPv4 ? new Socket(AddressFamily.InterNetwork, socketType, protocolType) : null));
								socket4 = socket3;
							}
							socket = socket4;
						}
						if (socket == null)
						{
							continue;
						}
					}
					socket.ReplaceHandleIfNecessaryAfterFailedConnect();
					if (internalArgs.RemoteEndPoint is IPEndPoint iPEndPoint)
					{
						iPEndPoint.Address = iPAddress;
					}
					else
					{
						internalArgs.RemoteEndPoint = new IPEndPoint(iPAddress, port);
					}
					if (socket.ConnectAsync(internalArgs))
					{
						using (cancellationToken.UnsafeRegister(delegate(object s)
						{
							Socket.CancelConnectAsync((SocketAsyncEventArgs)s);
						}, internalArgs))
						{
							await new ValueTask(internalArgs, internalArgs.Version).ConfigureAwait(continueOnCapturedContext: false);
						}
					}
					if (internalArgs.SocketError == SocketError.Success)
					{
						return;
					}
					if (cancellationToken.IsCancellationRequested)
					{
						throw new SocketException(995);
					}
					lastError = internalArgs.SocketError;
					internalArgs.Reset();
				}
				caughtException = (Exception)(object)new SocketException((int)lastError);
			}
			catch (ObjectDisposedException)
			{
				caughtException = (Exception)(object)new SocketException(995);
			}
			catch (Exception ex2)
			{
				caughtException = ex2;
			}
			finally
			{
				if (tempSocketIPv4 != null && !tempSocketIPv4.Connected)
				{
					tempSocketIPv4.Dispose();
				}
				if (tempSocketIPv5 != null && !tempSocketIPv5.Connected)
				{
					tempSocketIPv5.Dispose();
				}
				if (_currentSocket != null && ((!_userSocket && !_currentSocket.Connected) || caughtException is OperationCanceledException || caughtException is SocketException { SocketErrorCode: SocketError.OperationAborted }))
				{
					_currentSocket.Dispose();
				}
				if (caughtException != null)
				{
					SetResults(caughtException, 0, SocketFlags.None);
					_currentSocket?.UpdateStatusAfterSocketError(_socketError);
				}
				else
				{
					SetResults(SocketError.Success, internalArgs.BytesTransferred, internalArgs.SocketFlags);
					_connectSocket = (_currentSocket = internalArgs.ConnectSocket);
				}
				if (SocketsTelemetry.Log.IsEnabled())
				{
					LogBytesTransferEvents(_connectSocket?.SocketType, SocketAsyncOperation.Connect, internalArgs.BytesTransferred);
				}
				Complete();
				internalArgs.Dispose();
				if (!internalArgs.ReachedCoordinationPointFirst())
				{
					OnCompleted(this);
				}
			}
		}
	}

	internal void FinishOperationSyncSuccess(int bytesTransferred, SocketFlags flags)
	{
		SetResults(SocketError.Success, bytesTransferred, flags);
		if (System.Net.NetEventSource.Log.IsEnabled() && bytesTransferred > 0)
		{
			LogBuffer(bytesTransferred);
		}
		SocketError socketError = SocketError.Success;
		switch (_completedOperation)
		{
		case SocketAsyncOperation.Accept:
		{
			System.Net.Internals.SocketAddress socketAddress2 = IPEndPointExtensions.Serialize(_currentSocket._rightEndPoint);
			socketError = FinishOperationAccept(socketAddress2);
			if (socketError == SocketError.Success)
			{
				_acceptSocket = _currentSocket.UpdateAcceptSocket(_acceptSocket, _currentSocket._rightEndPoint.Create(socketAddress2));
				if (System.Net.NetEventSource.Log.IsEnabled())
				{
					try
					{
						System.Net.NetEventSource.Accepted(_acceptSocket, _acceptSocket.RemoteEndPoint, _acceptSocket.LocalEndPoint);
					}
					catch (ObjectDisposedException)
					{
					}
				}
			}
			else
			{
				SetResults(socketError, bytesTransferred, flags);
				_acceptSocket = null;
				_currentSocket.UpdateStatusAfterSocketError(socketError);
			}
			break;
		}
		case SocketAsyncOperation.Connect:
			socketError = FinishOperationConnect();
			if (socketError == SocketError.Success)
			{
				if (System.Net.NetEventSource.Log.IsEnabled())
				{
					try
					{
						System.Net.NetEventSource.Connected(_currentSocket, _currentSocket.LocalEndPoint, _currentSocket.RemoteEndPoint);
					}
					catch (ObjectDisposedException)
					{
					}
				}
				_currentSocket.SetToConnected();
				_connectSocket = _currentSocket;
			}
			else
			{
				SetResults(socketError, bytesTransferred, flags);
				_currentSocket.UpdateStatusAfterSocketError(socketError);
			}
			break;
		case SocketAsyncOperation.Disconnect:
			_currentSocket.SetToDisconnected();
			_currentSocket._remoteEndPoint = null;
			break;
		case SocketAsyncOperation.ReceiveFrom:
		{
			_socketAddress.InternalSize = GetSocketAddressSize();
			System.Net.Internals.SocketAddress socketAddress = IPEndPointExtensions.Serialize(_remoteEndPoint);
			if (!socketAddress.Equals(_socketAddress))
			{
				try
				{
					_remoteEndPoint = _remoteEndPoint.Create(_socketAddress);
				}
				catch
				{
				}
			}
			break;
		}
		case SocketAsyncOperation.ReceiveMessageFrom:
		{
			_socketAddress.InternalSize = GetSocketAddressSize();
			System.Net.Internals.SocketAddress socketAddress = IPEndPointExtensions.Serialize(_remoteEndPoint);
			if (!socketAddress.Equals(_socketAddress))
			{
				try
				{
					_remoteEndPoint = _remoteEndPoint.Create(_socketAddress);
				}
				catch
				{
				}
			}
			FinishOperationReceiveMessageFrom();
			break;
		}
		case SocketAsyncOperation.SendPackets:
			FinishOperationSendPackets();
			break;
		}
		if (SocketsTelemetry.Log.IsEnabled())
		{
			LogBytesTransferEvents(_currentSocket?.SocketType, _completedOperation, bytesTransferred);
		}
		Complete();
	}

	internal void FinishOperationAsyncSuccess(int bytesTransferred, SocketFlags flags)
	{
		ExecutionContext context = _context;
		FinishOperationSyncSuccess(bytesTransferred, flags);
		if (context == null)
		{
			OnCompletedInternal();
		}
		else
		{
			ExecutionContext.Run(context, s_executionCallback, this);
		}
	}

	private void FinishOperationSync(SocketError socketError, int bytesTransferred, SocketFlags flags)
	{
		if (socketError == SocketError.Success)
		{
			FinishOperationSyncSuccess(bytesTransferred, flags);
		}
		else
		{
			FinishOperationSyncFailure(socketError, bytesTransferred, flags);
		}
		if (SocketsTelemetry.Log.IsEnabled())
		{
			AfterConnectAcceptTelemetry();
		}
	}

	private static void LogBytesTransferEvents(SocketType? socketType, SocketAsyncOperation operation, int bytesTransferred)
	{
		switch (operation)
		{
		case SocketAsyncOperation.Accept:
		case SocketAsyncOperation.Receive:
		case SocketAsyncOperation.ReceiveFrom:
		case SocketAsyncOperation.ReceiveMessageFrom:
			SocketsTelemetry.Log.BytesReceived(bytesTransferred);
			if (socketType == SocketType.Dgram)
			{
				SocketsTelemetry.Log.DatagramReceived();
			}
			break;
		case SocketAsyncOperation.Connect:
		case SocketAsyncOperation.Send:
		case SocketAsyncOperation.SendPackets:
		case SocketAsyncOperation.SendTo:
			SocketsTelemetry.Log.BytesSent(bytesTransferred);
			if (socketType == SocketType.Dgram)
			{
				SocketsTelemetry.Log.DatagramSent();
			}
			break;
		case SocketAsyncOperation.Disconnect:
			break;
		}
	}

	[MemberNotNull("_preAllocatedOverlapped")]
	private void InitializeInternals()
	{
		_preAllocatedOverlapped = PreAllocatedOverlapped.UnsafeCreate(s_completionPortCallback, _strongThisRef, null);
		if (System.Net.NetEventSource.Log.IsEnabled())
		{
			System.Net.NetEventSource.Info(this, $"new PreAllocatedOverlapped {_preAllocatedOverlapped}", "InitializeInternals");
		}
	}

	private void FreeInternals()
	{
		FreePinHandles();
		FreeOverlapped();
	}

	private unsafe NativeOverlapped* AllocateNativeOverlapped()
	{
		ThreadPoolBoundHandle orAllocateThreadPoolBoundHandle = _currentSocket.GetOrAllocateThreadPoolBoundHandle();
		return orAllocateThreadPoolBoundHandle.AllocateNativeOverlapped(_preAllocatedOverlapped);
	}

	private unsafe void FreeNativeOverlapped(NativeOverlapped* overlapped)
	{
		_currentSocket.SafeHandle.IOCPBoundHandle.FreeNativeOverlapped(overlapped);
	}

	private unsafe void RegisterToCancelPendingIO(NativeOverlapped* overlapped, CancellationToken cancellationToken)
	{
		_pendingOverlappedForCancellation = overlapped;
		_registrationToCancelPendingIO = cancellationToken.UnsafeRegister(delegate(object s)
		{
			SocketAsyncEventArgs socketAsyncEventArgs = (SocketAsyncEventArgs)s;
			SafeSocketHandle safeHandle = socketAsyncEventArgs._currentSocket.SafeHandle;
			if (!safeHandle.IsClosed)
			{
				try
				{
					bool flag = global::Interop.Kernel32.CancelIoEx(safeHandle, socketAsyncEventArgs._pendingOverlappedForCancellation);
					if (System.Net.NetEventSource.Log.IsEnabled())
					{
						System.Net.NetEventSource.Info(socketAsyncEventArgs, flag ? "Socket operation canceled." : $"CancelIoEx failed with error '{Marshal.GetLastWin32Error()}'.", "RegisterToCancelPendingIO");
					}
				}
				catch (ObjectDisposedException)
				{
				}
			}
		}, this);
	}

	private unsafe SocketError GetIOCPResult(bool success, NativeOverlapped* overlapped)
	{
		if (success)
		{
			if (_currentSocket.SafeHandle.SkipCompletionPortOnSuccess)
			{
				FreeNativeOverlapped(overlapped);
				return SocketError.Success;
			}
			return SocketError.IOPending;
		}
		SocketError lastSocketError = SocketPal.GetLastSocketError();
		if (lastSocketError != SocketError.IOPending)
		{
			FreeNativeOverlapped(overlapped);
			return lastSocketError;
		}
		return SocketError.IOPending;
	}

	private unsafe SocketError ProcessIOCPResult(bool success, int bytesTransferred, NativeOverlapped* overlapped)
	{
		SocketError iOCPResult = GetIOCPResult(success, overlapped);
		if (iOCPResult != SocketError.IOPending)
		{
			FinishOperationSync(iOCPResult, bytesTransferred, SocketFlags.None);
		}
		return iOCPResult;
	}

	private unsafe SocketError ProcessIOCPResultWithDeferredAsyncHandling(bool success, int bytesTransferred, NativeOverlapped* overlapped, Memory<byte> bufferToPin, CancellationToken cancellationToken = default(CancellationToken))
	{
		SocketError iOCPResult = GetIOCPResult(success, overlapped);
		if (iOCPResult == SocketError.IOPending)
		{
			RegisterToCancelPendingIO(overlapped, cancellationToken);
			_singleBufferHandle = bufferToPin.Pin();
			_asyncProcessingState = AsyncProcessingState.Set;
		}
		else
		{
			_asyncProcessingState = AsyncProcessingState.None;
			FinishOperationSync(iOCPResult, bytesTransferred, SocketFlags.None);
		}
		return iOCPResult;
	}

	internal unsafe SocketError DoOperationAccept(Socket socket, SafeSocketHandle handle, SafeSocketHandle acceptHandle, CancellationToken cancellationToken)
	{
		bool flag = _count != 0;
		Memory<byte> bufferToPin = (flag ? _buffer : ((Memory<byte>)_acceptBuffer));
		fixed (byte* ptr = &MemoryMarshal.GetReference(bufferToPin.Span))
		{
			NativeOverlapped* overlapped = AllocateNativeOverlapped();
			try
			{
				_asyncProcessingState = AsyncProcessingState.InProcess;
				int bytesReceived;
				bool success = socket.AcceptEx(handle, acceptHandle, (IntPtr)(flag ? (ptr + _offset) : ptr), flag ? (_count - _acceptAddressBufferCount) : 0, _acceptAddressBufferCount / 2, _acceptAddressBufferCount / 2, out bytesReceived, overlapped);
				return ProcessIOCPResultWithDeferredAsyncHandling(success, bytesReceived, overlapped, bufferToPin, cancellationToken);
			}
			catch
			{
				_asyncProcessingState = AsyncProcessingState.None;
				FreeNativeOverlapped(overlapped);
				throw;
			}
		}
	}

	internal SocketError DoOperationConnect(Socket socket, SafeSocketHandle handle)
	{
		SocketError socketError = SocketPal.Connect(handle, _socketAddress.Buffer, _socketAddress.Size);
		FinishOperationSync(socketError, 0, SocketFlags.None);
		return socketError;
	}

	internal unsafe SocketError DoOperationConnectEx(Socket socket, SafeSocketHandle handle)
	{
		PinSocketAddressBuffer();
		NativeOverlapped* overlapped = AllocateNativeOverlapped();
		try
		{
			_singleBufferHandle = _buffer.Pin();
			_asyncProcessingState = AsyncProcessingState.Set;
			int bytesSent;
			bool success = socket.ConnectEx(handle, PtrSocketAddressBuffer, _socketAddress.Size, (IntPtr)((byte*)_singleBufferHandle.Pointer + _offset), _count, out bytesSent, overlapped);
			return ProcessIOCPResult(success, bytesSent, overlapped);
		}
		catch
		{
			_asyncProcessingState = AsyncProcessingState.None;
			FreeNativeOverlapped(overlapped);
			_singleBufferHandle.Dispose();
			throw;
		}
	}

	internal unsafe SocketError DoOperationDisconnect(Socket socket, SafeSocketHandle handle, CancellationToken cancellationToken)
	{
		NativeOverlapped* overlapped = AllocateNativeOverlapped();
		try
		{
			_asyncProcessingState = AsyncProcessingState.InProcess;
			bool success = socket.DisconnectEx(handle, overlapped, DisconnectReuseSocket ? 2 : 0, 0);
			return ProcessIOCPResultWithDeferredAsyncHandling(success, 0, overlapped, Memory<byte>.Empty, cancellationToken);
		}
		catch
		{
			_asyncProcessingState = AsyncProcessingState.None;
			FreeNativeOverlapped(overlapped);
			throw;
		}
	}

	internal SocketError DoOperationReceive(SafeSocketHandle handle, CancellationToken cancellationToken)
	{
		if (_bufferList != null)
		{
			return DoOperationReceiveMultiBuffer(handle);
		}
		return DoOperationReceiveSingleBuffer(handle, cancellationToken);
	}

	internal unsafe SocketError DoOperationReceiveSingleBuffer(SafeSocketHandle handle, CancellationToken cancellationToken)
	{
		fixed (byte* ptr = &MemoryMarshal.GetReference(_buffer.Span))
		{
			NativeOverlapped* overlapped = AllocateNativeOverlapped();
			try
			{
				_asyncProcessingState = AsyncProcessingState.InProcess;
				WSABuffer wSABuffer = default(WSABuffer);
				wSABuffer.Length = _count;
				wSABuffer.Pointer = (IntPtr)(ptr + _offset);
				WSABuffer wSABuffer2 = wSABuffer;
				SocketFlags socketFlags = _socketFlags;
				int bytesTransferred;
				SocketError socketError = global::Interop.Winsock.WSARecv(handle, &wSABuffer2, 1, out bytesTransferred, ref socketFlags, overlapped, IntPtr.Zero);
				return ProcessIOCPResultWithDeferredAsyncHandling(socketError == SocketError.Success, bytesTransferred, overlapped, _buffer, cancellationToken);
			}
			catch
			{
				_asyncProcessingState = AsyncProcessingState.None;
				FreeNativeOverlapped(overlapped);
				throw;
			}
		}
	}

	internal unsafe SocketError DoOperationReceiveMultiBuffer(SafeSocketHandle handle)
	{
		NativeOverlapped* overlapped = AllocateNativeOverlapped();
		try
		{
			SocketFlags socketFlags = _socketFlags;
			int bytesTransferred;
			SocketError socketError = global::Interop.Winsock.WSARecv(handle, _wsaBufferArrayPinned, _bufferListInternal.Count, out bytesTransferred, ref socketFlags, overlapped, IntPtr.Zero);
			return ProcessIOCPResult(socketError == SocketError.Success, bytesTransferred, overlapped);
		}
		catch
		{
			FreeNativeOverlapped(overlapped);
			throw;
		}
	}

	internal SocketError DoOperationReceiveFrom(SafeSocketHandle handle, CancellationToken cancellationToken)
	{
		PinSocketAddressBuffer();
		if (_bufferList != null)
		{
			return DoOperationReceiveFromMultiBuffer(handle);
		}
		return DoOperationReceiveFromSingleBuffer(handle, cancellationToken);
	}

	internal unsafe SocketError DoOperationReceiveFromSingleBuffer(SafeSocketHandle handle, CancellationToken cancellationToken)
	{
		fixed (byte* ptr = &MemoryMarshal.GetReference(_buffer.Span))
		{
			NativeOverlapped* overlapped = AllocateNativeOverlapped();
			try
			{
				_asyncProcessingState = AsyncProcessingState.InProcess;
				WSABuffer wSABuffer = default(WSABuffer);
				wSABuffer.Length = _count;
				wSABuffer.Pointer = (IntPtr)(ptr + _offset);
				WSABuffer buffer = wSABuffer;
				SocketFlags socketFlags = _socketFlags;
				int bytesTransferred;
				SocketError socketError = global::Interop.Winsock.WSARecvFrom(handle, ref buffer, 1, out bytesTransferred, ref socketFlags, PtrSocketAddressBuffer, PtrSocketAddressBufferSize, overlapped, IntPtr.Zero);
				return ProcessIOCPResultWithDeferredAsyncHandling(socketError == SocketError.Success, bytesTransferred, overlapped, _buffer, cancellationToken);
			}
			catch
			{
				_asyncProcessingState = AsyncProcessingState.None;
				FreeNativeOverlapped(overlapped);
				throw;
			}
		}
	}

	internal unsafe SocketError DoOperationReceiveFromMultiBuffer(SafeSocketHandle handle)
	{
		NativeOverlapped* overlapped = AllocateNativeOverlapped();
		try
		{
			SocketFlags socketFlags = _socketFlags;
			int bytesTransferred;
			SocketError socketError = global::Interop.Winsock.WSARecvFrom(handle, _wsaBufferArrayPinned, _bufferListInternal.Count, out bytesTransferred, ref socketFlags, PtrSocketAddressBuffer, PtrSocketAddressBufferSize, overlapped, IntPtr.Zero);
			return ProcessIOCPResult(socketError == SocketError.Success, bytesTransferred, overlapped);
		}
		catch
		{
			FreeNativeOverlapped(overlapped);
			throw;
		}
	}

	internal unsafe SocketError DoOperationReceiveMessageFrom(Socket socket, SafeSocketHandle handle, CancellationToken cancellationToken)
	{
		PinSocketAddressBuffer();
		if (_wsaMessageBufferPinned == null)
		{
			_wsaMessageBufferPinned = GC.AllocateUninitializedArray<byte>(sizeof(global::Interop.Winsock.WSAMsg), pinned: true);
		}
		IPAddress iPAddress = ((_socketAddress.Family == AddressFamily.InterNetworkV6) ? _socketAddress.GetIPAddress() : null);
		bool flag = _currentSocket.AddressFamily == AddressFamily.InterNetwork || (iPAddress?.IsIPv4MappedToIPv6 ?? false);
		if (_currentSocket.AddressFamily == AddressFamily.InterNetworkV6 && (_controlBufferPinned == null || _controlBufferPinned.Length != sizeof(global::Interop.Winsock.ControlDataIPv6)))
		{
			_controlBufferPinned = GC.AllocateUninitializedArray<byte>(sizeof(global::Interop.Winsock.ControlDataIPv6), pinned: true);
		}
		else if (flag && (_controlBufferPinned == null || _controlBufferPinned.Length != sizeof(global::Interop.Winsock.ControlData)))
		{
			_controlBufferPinned = GC.AllocateUninitializedArray<byte>(sizeof(global::Interop.Winsock.ControlData), pinned: true);
		}
		WSABuffer[] wsaRecvMsgWSABufferArray;
		uint wsaRecvMsgWSABufferCount;
		if (_bufferList == null)
		{
			if (_wsaRecvMsgWSABufferArrayPinned == null)
			{
				_wsaRecvMsgWSABufferArrayPinned = GC.AllocateUninitializedArray<WSABuffer>(1, pinned: true);
			}
			fixed (byte* ptr = &MemoryMarshal.GetReference(_buffer.Span))
			{
				_asyncProcessingState = AsyncProcessingState.InProcess;
				_wsaRecvMsgWSABufferArrayPinned[0].Pointer = (IntPtr)ptr + _offset;
				_wsaRecvMsgWSABufferArrayPinned[0].Length = _count;
				wsaRecvMsgWSABufferArray = _wsaRecvMsgWSABufferArrayPinned;
				wsaRecvMsgWSABufferCount = 1u;
				return Core();
			}
		}
		wsaRecvMsgWSABufferArray = _wsaBufferArrayPinned;
		wsaRecvMsgWSABufferCount = (uint)_bufferListInternal.Count;
		return Core();
		unsafe SocketError Core()
		{
			global::Interop.Winsock.WSAMsg* ptr2 = (global::Interop.Winsock.WSAMsg*)(void*)Marshal.UnsafeAddrOfPinnedArrayElement(_wsaMessageBufferPinned, 0);
			ptr2->socketAddress = PtrSocketAddressBuffer;
			ptr2->addressLength = (uint)_socketAddress.Size;
			fixed (WSABuffer* ptr3 = &wsaRecvMsgWSABufferArray[0])
			{
				void* ptr4 = ptr3;
				ptr2->buffers = (IntPtr)ptr4;
			}
			ptr2->count = wsaRecvMsgWSABufferCount;
			if (_controlBufferPinned != null)
			{
				fixed (byte* ptr5 = &_controlBufferPinned[0])
				{
					void* ptr6 = ptr5;
					ptr2->controlBuffer.Pointer = (IntPtr)ptr6;
				}
				ptr2->controlBuffer.Length = _controlBufferPinned.Length;
			}
			ptr2->flags = _socketFlags;
			NativeOverlapped* overlapped = AllocateNativeOverlapped();
			try
			{
				int bytesTransferred;
				SocketError socketError = socket.WSARecvMsg(handle, Marshal.UnsafeAddrOfPinnedArrayElement(_wsaMessageBufferPinned, 0), out bytesTransferred, overlapped, IntPtr.Zero);
				return (_bufferList == null) ? ProcessIOCPResultWithDeferredAsyncHandling(socketError == SocketError.Success, bytesTransferred, overlapped, _buffer, cancellationToken) : ProcessIOCPResult(socketError == SocketError.Success, bytesTransferred, overlapped);
			}
			catch
			{
				_asyncProcessingState = AsyncProcessingState.None;
				FreeNativeOverlapped(overlapped);
				throw;
			}
		}
	}

	internal SocketError DoOperationSend(SafeSocketHandle handle, CancellationToken cancellationToken)
	{
		if (_bufferList != null)
		{
			return DoOperationSendMultiBuffer(handle);
		}
		return DoOperationSendSingleBuffer(handle, cancellationToken);
	}

	internal unsafe SocketError DoOperationSendSingleBuffer(SafeSocketHandle handle, CancellationToken cancellationToken)
	{
		fixed (byte* ptr = &MemoryMarshal.GetReference(_buffer.Span))
		{
			NativeOverlapped* overlapped = AllocateNativeOverlapped();
			try
			{
				_asyncProcessingState = AsyncProcessingState.InProcess;
				WSABuffer wSABuffer = default(WSABuffer);
				wSABuffer.Length = _count;
				wSABuffer.Pointer = (IntPtr)(ptr + _offset);
				WSABuffer wSABuffer2 = wSABuffer;
				int bytesTransferred;
				SocketError socketError = global::Interop.Winsock.WSASend(handle, &wSABuffer2, 1, out bytesTransferred, _socketFlags, overlapped, IntPtr.Zero);
				return ProcessIOCPResultWithDeferredAsyncHandling(socketError == SocketError.Success, bytesTransferred, overlapped, _buffer, cancellationToken);
			}
			catch
			{
				_asyncProcessingState = AsyncProcessingState.None;
				FreeNativeOverlapped(overlapped);
				throw;
			}
		}
	}

	internal unsafe SocketError DoOperationSendMultiBuffer(SafeSocketHandle handle)
	{
		NativeOverlapped* overlapped = AllocateNativeOverlapped();
		try
		{
			int bytesTransferred;
			SocketError socketError = global::Interop.Winsock.WSASend(handle, _wsaBufferArrayPinned, _bufferListInternal.Count, out bytesTransferred, _socketFlags, overlapped, IntPtr.Zero);
			return ProcessIOCPResult(socketError == SocketError.Success, bytesTransferred, overlapped);
		}
		catch
		{
			FreeNativeOverlapped(overlapped);
			throw;
		}
	}

	internal unsafe SocketError DoOperationSendPackets(Socket socket, SafeSocketHandle handle, CancellationToken cancellationToken)
	{
		SendPacketsElement[] array = (SendPacketsElement[])_sendPacketsElements.Clone();
		int num = 0;
		int num2 = 0;
		int num3 = 0;
		SendPacketsElement[] array2 = array;
		foreach (SendPacketsElement sendPacketsElement in array2)
		{
			if (sendPacketsElement != null)
			{
				if (sendPacketsElement.FilePath != null)
				{
					num++;
				}
				else if (sendPacketsElement.FileStream != null)
				{
					num2++;
				}
				else if (sendPacketsElement.MemoryBuffer.HasValue && sendPacketsElement.Count > 0)
				{
					num3++;
				}
			}
		}
		if (num + num2 + num3 == 0)
		{
			FinishOperationSyncSuccess(0, SocketFlags.None);
			return SocketError.Success;
		}
		if (num > 0)
		{
			int num4 = 0;
			_sendPacketsFileHandles = new SafeFileHandle[num];
			try
			{
				SendPacketsElement[] array3 = array;
				foreach (SendPacketsElement sendPacketsElement2 in array3)
				{
					if (sendPacketsElement2 != null && sendPacketsElement2.FilePath != null)
					{
						_sendPacketsFileHandles[num4] = File.OpenHandle(sendPacketsElement2.FilePath, FileMode.Open, FileAccess.Read, FileShare.Read, FileOptions.None, 0L);
						num4++;
					}
				}
			}
			catch
			{
				for (int num5 = num4 - 1; num5 >= 0; num5--)
				{
					_sendPacketsFileHandles[num5].Dispose();
				}
				_sendPacketsFileHandles = null;
				throw;
			}
		}
		global::Interop.Winsock.TransmitPacketsElement[] array4 = SetupPinHandlesSendPackets(array, num, num2, num3);
		NativeOverlapped* overlapped = AllocateNativeOverlapped();
		try
		{
			_asyncProcessingState = AsyncProcessingState.InProcess;
			bool success = socket.TransmitPackets(handle, Marshal.UnsafeAddrOfPinnedArrayElement(array4, 0), array4.Length, _sendPacketsSendSize, overlapped, _sendPacketsFlags);
			return ProcessIOCPResultWithDeferredAsyncHandling(success, 0, overlapped, Memory<byte>.Empty, cancellationToken);
		}
		catch
		{
			_asyncProcessingState = AsyncProcessingState.None;
			FreeNativeOverlapped(overlapped);
			throw;
		}
	}

	internal SocketError DoOperationSendTo(SafeSocketHandle handle, CancellationToken cancellationToken)
	{
		PinSocketAddressBuffer();
		if (_bufferList != null)
		{
			return DoOperationSendToMultiBuffer(handle);
		}
		return DoOperationSendToSingleBuffer(handle, cancellationToken);
	}

	internal unsafe SocketError DoOperationSendToSingleBuffer(SafeSocketHandle handle, CancellationToken cancellationToken)
	{
		fixed (byte* ptr = &MemoryMarshal.GetReference(_buffer.Span))
		{
			NativeOverlapped* overlapped = AllocateNativeOverlapped();
			try
			{
				_asyncProcessingState = AsyncProcessingState.InProcess;
				WSABuffer wSABuffer = default(WSABuffer);
				wSABuffer.Length = _count;
				wSABuffer.Pointer = (IntPtr)(ptr + _offset);
				WSABuffer buffer = wSABuffer;
				int bytesTransferred;
				SocketError socketError = global::Interop.Winsock.WSASendTo(handle, ref buffer, 1, out bytesTransferred, _socketFlags, PtrSocketAddressBuffer, _socketAddress.Size, overlapped, IntPtr.Zero);
				return ProcessIOCPResultWithDeferredAsyncHandling(socketError == SocketError.Success, bytesTransferred, overlapped, _buffer, cancellationToken);
			}
			catch
			{
				_asyncProcessingState = AsyncProcessingState.None;
				FreeNativeOverlapped(overlapped);
				throw;
			}
		}
	}

	internal unsafe SocketError DoOperationSendToMultiBuffer(SafeSocketHandle handle)
	{
		NativeOverlapped* overlapped = AllocateNativeOverlapped();
		try
		{
			int bytesTransferred;
			SocketError socketError = global::Interop.Winsock.WSASendTo(handle, _wsaBufferArrayPinned, _bufferListInternal.Count, out bytesTransferred, _socketFlags, PtrSocketAddressBuffer, _socketAddress.Size, overlapped, IntPtr.Zero);
			return ProcessIOCPResult(socketError == SocketError.Success, bytesTransferred, overlapped);
		}
		catch
		{
			FreeNativeOverlapped(overlapped);
			throw;
		}
	}

	private void SetupMultipleBuffers()
	{
		if (_bufferListInternal == null || _bufferListInternal.Count == 0)
		{
			if (_pinState == PinState.MultipleBuffer)
			{
				FreePinHandles();
			}
			return;
		}
		FreePinHandles();
		try
		{
			int count = _bufferListInternal.Count;
			if (_multipleBufferMemoryHandles == null || _multipleBufferMemoryHandles.Length < count)
			{
				_multipleBufferMemoryHandles = new MemoryHandle[count];
			}
			for (int i = 0; i < count; i++)
			{
				_multipleBufferMemoryHandles[i] = _bufferListInternal[i].Array.AsMemory().Pin();
			}
			if (_wsaBufferArrayPinned == null || _wsaBufferArrayPinned.Length < count)
			{
				_wsaBufferArrayPinned = GC.AllocateUninitializedArray<WSABuffer>(count, pinned: true);
			}
			for (int j = 0; j < count; j++)
			{
				ArraySegment<byte> arraySegment = _bufferListInternal[j];
				_wsaBufferArrayPinned[j].Pointer = Marshal.UnsafeAddrOfPinnedArrayElement(arraySegment.Array, arraySegment.Offset);
				_wsaBufferArrayPinned[j].Length = arraySegment.Count;
			}
			_pinState = PinState.MultipleBuffer;
		}
		catch (Exception)
		{
			FreePinHandles();
			throw;
		}
	}

	private void PinSocketAddressBuffer()
	{
		if (_pinnedSocketAddress != _socketAddress)
		{
			if (_socketAddressGCHandle.IsAllocated)
			{
				_socketAddressGCHandle.Free();
			}
			_socketAddressGCHandle = GCHandle.Alloc(_socketAddress.Buffer, GCHandleType.Pinned);
			_socketAddress.CopyAddressSizeIntoBuffer();
			_pinnedSocketAddress = _socketAddress;
		}
	}

	private void FreeOverlapped()
	{
		if (_preAllocatedOverlapped != null)
		{
			_preAllocatedOverlapped.Dispose();
			_preAllocatedOverlapped = null;
		}
	}

	private void FreePinHandles()
	{
		_pinState = PinState.None;
		if (_asyncProcessingState != 0)
		{
			_asyncProcessingState = AsyncProcessingState.None;
			_singleBufferHandle.Dispose();
		}
		if (_multipleBufferMemoryHandles != null)
		{
			for (int i = 0; i < _multipleBufferMemoryHandles.Length; i++)
			{
				_multipleBufferMemoryHandles[i].Dispose();
				_multipleBufferMemoryHandles[i] = default(MemoryHandle);
			}
		}
		if (_socketAddressGCHandle.IsAllocated)
		{
			_socketAddressGCHandle.Free();
			_pinnedSocketAddress = null;
		}
	}

	private unsafe global::Interop.Winsock.TransmitPacketsElement[] SetupPinHandlesSendPackets(SendPacketsElement[] sendPacketsElementsCopy, int sendPacketsElementsFileCount, int sendPacketsElementsFileStreamCount, int sendPacketsElementsBufferCount)
	{
		if (_pinState != 0)
		{
			FreePinHandles();
		}
		global::Interop.Winsock.TransmitPacketsElement[] array = GC.AllocateUninitializedArray<global::Interop.Winsock.TransmitPacketsElement>(sendPacketsElementsFileCount + sendPacketsElementsFileStreamCount + sendPacketsElementsBufferCount, pinned: true);
		if (_multipleBufferMemoryHandles == null || _multipleBufferMemoryHandles.Length < sendPacketsElementsBufferCount)
		{
			_multipleBufferMemoryHandles = new MemoryHandle[sendPacketsElementsBufferCount];
		}
		int num = 0;
		foreach (SendPacketsElement sendPacketsElement in sendPacketsElementsCopy)
		{
			if (sendPacketsElement != null && sendPacketsElement.MemoryBuffer.HasValue && sendPacketsElement.Count > 0)
			{
				_multipleBufferMemoryHandles[num] = sendPacketsElement.MemoryBuffer.Value.Pin();
				num++;
			}
		}
		int num2 = 0;
		int num3 = 0;
		int num4 = 0;
		foreach (SendPacketsElement sendPacketsElement2 in sendPacketsElementsCopy)
		{
			if (sendPacketsElement2 != null)
			{
				if (sendPacketsElement2.MemoryBuffer.HasValue && sendPacketsElement2.Count > 0)
				{
					array[num3].buffer = (IntPtr)_multipleBufferMemoryHandles[num2].Pointer;
					array[num3].length = (uint)sendPacketsElement2.Count;
					array[num3].flags = global::Interop.Winsock.TransmitPacketsElementFlags.Memory | (sendPacketsElement2.EndOfPacket ? global::Interop.Winsock.TransmitPacketsElementFlags.EndOfPacket : global::Interop.Winsock.TransmitPacketsElementFlags.None);
					num2++;
					num3++;
				}
				else if (sendPacketsElement2.FilePath != null)
				{
					array[num3].fileHandle = _sendPacketsFileHandles[num4].DangerousGetHandle();
					array[num3].fileOffset = sendPacketsElement2.OffsetLong;
					array[num3].length = (uint)sendPacketsElement2.Count;
					array[num3].flags = global::Interop.Winsock.TransmitPacketsElementFlags.File | (sendPacketsElement2.EndOfPacket ? global::Interop.Winsock.TransmitPacketsElementFlags.EndOfPacket : global::Interop.Winsock.TransmitPacketsElementFlags.None);
					num4++;
					num3++;
				}
				else if (sendPacketsElement2.FileStream != null)
				{
					array[num3].fileHandle = sendPacketsElement2.FileStream.SafeFileHandle.DangerousGetHandle();
					array[num3].fileOffset = sendPacketsElement2.OffsetLong;
					array[num3].length = (uint)sendPacketsElement2.Count;
					array[num3].flags = global::Interop.Winsock.TransmitPacketsElementFlags.File | (sendPacketsElement2.EndOfPacket ? global::Interop.Winsock.TransmitPacketsElementFlags.EndOfPacket : global::Interop.Winsock.TransmitPacketsElementFlags.None);
					num3++;
				}
			}
		}
		_pinState = PinState.SendPackets;
		return array;
	}

	internal void LogBuffer(int size)
	{
		if (_bufferList != null)
		{
			for (int i = 0; i < _bufferListInternal.Count; i++)
			{
				WSABuffer wSABuffer = _wsaBufferArrayPinned[i];
				System.Net.NetEventSource.DumpBuffer(this, wSABuffer.Pointer, Math.Min(wSABuffer.Length, size), "LogBuffer");
				if ((size -= wSABuffer.Length) <= 0)
				{
					break;
				}
			}
		}
		else if (_buffer.Length != 0)
		{
			System.Net.NetEventSource.DumpBuffer(this, _buffer, _offset, size, "LogBuffer");
		}
	}

	private unsafe SocketError FinishOperationAccept(System.Net.Internals.SocketAddress remoteSocketAddress)
	{
		bool success = false;
		SafeHandle safeHandle = _currentSocket.SafeHandle;
		SocketError socketError;
		try
		{
			safeHandle.DangerousAddRef(ref success);
			IntPtr pointer = safeHandle.DangerousGetHandle();
			bool flag = _count != 0;
			Memory<byte> memory = (flag ? _buffer : ((Memory<byte>)_acceptBuffer));
			fixed (byte* ptr = &MemoryMarshal.GetReference(memory.Span))
			{
				_currentSocket.GetAcceptExSockaddrs((IntPtr)(flag ? (ptr + _offset) : ptr), flag ? (_count - _acceptAddressBufferCount) : 0, _acceptAddressBufferCount / 2, _acceptAddressBufferCount / 2, out var _, out var _, out var remoteSocketAddress2, out remoteSocketAddress.InternalSize);
				Marshal.Copy(remoteSocketAddress2, remoteSocketAddress.Buffer, 0, remoteSocketAddress.Size);
			}
			socketError = global::Interop.Winsock.setsockopt(_acceptSocket.SafeHandle, SocketOptionLevel.Socket, SocketOptionName.UpdateAcceptContext, ref pointer, IntPtr.Size);
			if (socketError == SocketError.SocketError)
			{
				socketError = SocketPal.GetLastSocketError();
			}
		}
		catch (ObjectDisposedException)
		{
			socketError = SocketError.OperationAborted;
		}
		finally
		{
			if (success)
			{
				safeHandle.DangerousRelease();
			}
		}
		return socketError;
	}

	private unsafe SocketError FinishOperationConnect()
	{
		try
		{
			if (_currentSocket.SocketType != SocketType.Stream)
			{
				return SocketError.Success;
			}
			SocketError socketError = global::Interop.Winsock.setsockopt(_currentSocket.SafeHandle, SocketOptionLevel.Socket, SocketOptionName.UpdateConnectContext, null, 0);
			return (socketError == SocketError.SocketError) ? SocketPal.GetLastSocketError() : socketError;
		}
		catch (ObjectDisposedException)
		{
			return SocketError.OperationAborted;
		}
	}

	private unsafe int GetSocketAddressSize()
	{
		return *(int*)(void*)PtrSocketAddressBufferSize;
	}

	private void CompleteCore()
	{
		_strongThisRef.Value = null;
		if (_asyncProcessingState != 0)
		{
			CompleteCoreSpin();
		}
		unsafe void CompleteCoreSpin()
		{
			SpinWait spinWait = default(SpinWait);
			while (_asyncProcessingState == AsyncProcessingState.InProcess)
			{
				spinWait.SpinOnce();
			}
			_registrationToCancelPendingIO.Dispose();
			_registrationToCancelPendingIO = default(CancellationTokenRegistration);
			_pendingOverlappedForCancellation = null;
			_singleBufferHandle.Dispose();
			_asyncProcessingState = AsyncProcessingState.None;
		}
	}

	private unsafe void FinishOperationReceiveMessageFrom()
	{
		global::Interop.Winsock.WSAMsg* ptr = (global::Interop.Winsock.WSAMsg*)(void*)Marshal.UnsafeAddrOfPinnedArrayElement(_wsaMessageBufferPinned, 0);
		if (_controlBufferPinned.Length == sizeof(global::Interop.Winsock.ControlData))
		{
			_receiveMessageFromPacketInfo = SocketPal.GetIPPacketInformation((global::Interop.Winsock.ControlData*)(void*)ptr->controlBuffer.Pointer);
		}
		else if (_controlBufferPinned.Length == sizeof(global::Interop.Winsock.ControlDataIPv6))
		{
			_receiveMessageFromPacketInfo = SocketPal.GetIPPacketInformation((global::Interop.Winsock.ControlDataIPv6*)(void*)ptr->controlBuffer.Pointer);
		}
		else
		{
			_receiveMessageFromPacketInfo = default(IPPacketInformation);
		}
	}

	private void FinishOperationSendPackets()
	{
		if (_sendPacketsFileHandles != null)
		{
			for (int i = 0; i < _sendPacketsFileHandles.Length; i++)
			{
				_sendPacketsFileHandles[i]?.Dispose();
			}
			_sendPacketsFileHandles = null;
		}
	}

	private unsafe void HandleCompletionPortCallbackError(uint errorCode, uint numBytes, NativeOverlapped* nativeOverlapped)
	{
		SocketError socketError = (SocketError)errorCode;
		SocketFlags socketFlags = SocketFlags.None;
		if (socketError != SocketError.OperationAborted)
		{
			if (_currentSocket.Disposed)
			{
				socketError = SocketError.OperationAborted;
			}
			else
			{
				try
				{
					global::Interop.Winsock.WSAGetOverlappedResult(_currentSocket.SafeHandle, nativeOverlapped, out numBytes, wait: false, out socketFlags);
					socketError = SocketPal.GetLastSocketError();
				}
				catch
				{
					socketError = SocketError.OperationAborted;
				}
			}
		}
		FreeNativeOverlapped(nativeOverlapped);
		FinishOperationAsyncFailure(socketError, (int)numBytes, socketFlags);
	}
}
