using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Net.Internals;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Sources;
using Microsoft.Win32.SafeHandles;

namespace System.Net.Sockets;

public class Socket : IDisposable
{
	private sealed class TaskSocketAsyncEventArgs<TResult> : SocketAsyncEventArgs
	{
		internal AsyncTaskMethodBuilder<TResult> _builder;

		internal bool _accessed;

		internal bool _wrapExceptionsInIOExceptions;

		internal TaskSocketAsyncEventArgs()
			: base(unsafeSuppressExecutionContextFlow: true)
		{
		}

		internal AsyncTaskMethodBuilder<TResult> GetCompletionResponsibility(out bool responsibleForReturningToPool)
		{
			lock (this)
			{
				responsibleForReturningToPool = _accessed;
				_accessed = true;
				_ = _builder.Task;
				return _builder;
			}
		}
	}

	internal sealed class AwaitableSocketAsyncEventArgs : SocketAsyncEventArgs, IValueTaskSource, IValueTaskSource<int>, IValueTaskSource<Socket>, IValueTaskSource<SocketReceiveFromResult>, IValueTaskSource<SocketReceiveMessageFromResult>
	{
		private static readonly Action<object> s_completedSentinel = delegate
		{
			throw new InvalidOperationException(System.SR.Format(System.SR.net_sockets_valuetaskmisuse, "s_completedSentinel"));
		};

		private readonly Socket _owner;

		private bool _isReadForCaching;

		private Action<object> _continuation;

		private ExecutionContext _executionContext;

		private object _scheduler;

		private short _token;

		private CancellationToken _cancellationToken;

		public bool WrapExceptionsForNetworkStream { get; set; }

		public AwaitableSocketAsyncEventArgs(Socket owner, bool isReceiveForCaching)
			: base(unsafeSuppressExecutionContextFlow: true)
		{
			_owner = owner;
			_isReadForCaching = isReceiveForCaching;
		}

		private void Release()
		{
			_cancellationToken = default(CancellationToken);
			_token++;
			_continuation = null;
			if (Interlocked.CompareExchange(ref _isReadForCaching ? ref _owner._singleBufferReceiveEventArgs : ref _owner._singleBufferSendEventArgs, this, null) != null)
			{
				Dispose();
			}
		}

		protected override void OnCompleted(SocketAsyncEventArgs _)
		{
			Action<object> action = _continuation;
			if (action == null && (action = Interlocked.CompareExchange(ref _continuation, s_completedSentinel, null)) == null)
			{
				return;
			}
			object userToken = base.UserToken;
			base.UserToken = null;
			_continuation = s_completedSentinel;
			ExecutionContext executionContext = _executionContext;
			if (executionContext == null)
			{
				InvokeContinuation(action, userToken, forceAsync: false, requiresExecutionContextFlow: false);
				return;
			}
			_executionContext = null;
			ExecutionContext.Run(executionContext, delegate(object runState)
			{
				(AwaitableSocketAsyncEventArgs, Action<object>, object) tuple = ((AwaitableSocketAsyncEventArgs, Action<object>, object))runState;
				tuple.Item1.InvokeContinuation(tuple.Item2, tuple.Item3, forceAsync: false, requiresExecutionContextFlow: false);
			}, (this, action, userToken));
		}

		public ValueTask<Socket> AcceptAsync(Socket socket, CancellationToken cancellationToken)
		{
			if (socket.AcceptAsync(this, cancellationToken))
			{
				_cancellationToken = cancellationToken;
				return new ValueTask<Socket>(this, _token);
			}
			Socket acceptSocket = base.AcceptSocket;
			SocketError socketError = base.SocketError;
			Release();
			if (socketError != 0)
			{
				return ValueTask.FromException<Socket>(CreateException(socketError));
			}
			return new ValueTask<Socket>(acceptSocket);
		}

		public ValueTask<int> ReceiveAsync(Socket socket, CancellationToken cancellationToken)
		{
			if (socket.ReceiveAsync(this, cancellationToken))
			{
				_cancellationToken = cancellationToken;
				return new ValueTask<int>(this, _token);
			}
			int bytesTransferred = base.BytesTransferred;
			SocketError socketError = base.SocketError;
			Release();
			if (socketError != 0)
			{
				return ValueTask.FromException<int>(CreateException(socketError));
			}
			return new ValueTask<int>(bytesTransferred);
		}

		public ValueTask<SocketReceiveFromResult> ReceiveFromAsync(Socket socket, CancellationToken cancellationToken)
		{
			if (socket.ReceiveFromAsync(this, cancellationToken))
			{
				_cancellationToken = cancellationToken;
				return new ValueTask<SocketReceiveFromResult>(this, _token);
			}
			int bytesTransferred = base.BytesTransferred;
			EndPoint remoteEndPoint = base.RemoteEndPoint;
			SocketError socketError = base.SocketError;
			Release();
			if (socketError != 0)
			{
				return ValueTask.FromException<SocketReceiveFromResult>(CreateException(socketError));
			}
			SocketReceiveFromResult result = default(SocketReceiveFromResult);
			result.ReceivedBytes = bytesTransferred;
			result.RemoteEndPoint = remoteEndPoint;
			return new ValueTask<SocketReceiveFromResult>(result);
		}

		public ValueTask<SocketReceiveMessageFromResult> ReceiveMessageFromAsync(Socket socket, CancellationToken cancellationToken)
		{
			if (socket.ReceiveMessageFromAsync(this, cancellationToken))
			{
				_cancellationToken = cancellationToken;
				return new ValueTask<SocketReceiveMessageFromResult>(this, _token);
			}
			int bytesTransferred = base.BytesTransferred;
			EndPoint remoteEndPoint = base.RemoteEndPoint;
			SocketFlags socketFlags = base.SocketFlags;
			IPPacketInformation receiveMessageFromPacketInfo = base.ReceiveMessageFromPacketInfo;
			SocketError socketError = base.SocketError;
			Release();
			if (socketError != 0)
			{
				return ValueTask.FromException<SocketReceiveMessageFromResult>(CreateException(socketError));
			}
			SocketReceiveMessageFromResult result = default(SocketReceiveMessageFromResult);
			result.ReceivedBytes = bytesTransferred;
			result.RemoteEndPoint = remoteEndPoint;
			result.SocketFlags = socketFlags;
			result.PacketInformation = receiveMessageFromPacketInfo;
			return new ValueTask<SocketReceiveMessageFromResult>(result);
		}

		public ValueTask<int> SendAsync(Socket socket, CancellationToken cancellationToken)
		{
			if (socket.SendAsync(this, cancellationToken))
			{
				_cancellationToken = cancellationToken;
				return new ValueTask<int>(this, _token);
			}
			int bytesTransferred = base.BytesTransferred;
			SocketError socketError = base.SocketError;
			Release();
			if (socketError != 0)
			{
				return ValueTask.FromException<int>(CreateException(socketError));
			}
			return new ValueTask<int>(bytesTransferred);
		}

		public ValueTask SendAsyncForNetworkStream(Socket socket, CancellationToken cancellationToken)
		{
			if (socket.SendAsync(this, cancellationToken))
			{
				_cancellationToken = cancellationToken;
				return new ValueTask(this, _token);
			}
			SocketError socketError = base.SocketError;
			Release();
			if (socketError != 0)
			{
				return ValueTask.FromException(CreateException(socketError));
			}
			return default(ValueTask);
		}

		public ValueTask SendPacketsAsync(Socket socket, CancellationToken cancellationToken)
		{
			if (socket.SendPacketsAsync(this, cancellationToken))
			{
				_cancellationToken = cancellationToken;
				return new ValueTask(this, _token);
			}
			SocketError socketError = base.SocketError;
			Release();
			if (socketError != 0)
			{
				return ValueTask.FromException(CreateException(socketError));
			}
			return default(ValueTask);
		}

		public ValueTask<int> SendToAsync(Socket socket, CancellationToken cancellationToken)
		{
			if (socket.SendToAsync(this, cancellationToken))
			{
				_cancellationToken = cancellationToken;
				return new ValueTask<int>(this, _token);
			}
			int bytesTransferred = base.BytesTransferred;
			SocketError socketError = base.SocketError;
			Release();
			if (socketError != 0)
			{
				return ValueTask.FromException<int>(CreateException(socketError));
			}
			return new ValueTask<int>(bytesTransferred);
		}

		public ValueTask ConnectAsync(Socket socket)
		{
			try
			{
				if (socket.ConnectAsync(this, userSocket: true, saeaCancelable: false))
				{
					return new ValueTask(this, _token);
				}
			}
			catch
			{
				Release();
				throw;
			}
			SocketError socketError = base.SocketError;
			Release();
			if (socketError != 0)
			{
				return ValueTask.FromException(CreateException(socketError));
			}
			return default(ValueTask);
		}

		public ValueTask DisconnectAsync(Socket socket, CancellationToken cancellationToken)
		{
			if (socket.DisconnectAsync(this, cancellationToken))
			{
				_cancellationToken = cancellationToken;
				return new ValueTask(this, _token);
			}
			SocketError socketError = base.SocketError;
			Release();
			if (socketError != 0)
			{
				return ValueTask.FromException(CreateException(socketError));
			}
			return ValueTask.CompletedTask;
		}

		public ValueTaskSourceStatus GetStatus(short token)
		{
			if (token != _token)
			{
				ThrowIncorrectTokenException();
			}
			if ((object)_continuation == s_completedSentinel)
			{
				if (base.SocketError != 0)
				{
					return ValueTaskSourceStatus.Faulted;
				}
				return ValueTaskSourceStatus.Succeeded;
			}
			return ValueTaskSourceStatus.Pending;
		}

		public void OnCompleted(Action<object> continuation, object state, short token, ValueTaskSourceOnCompletedFlags flags)
		{
			if (token != _token)
			{
				ThrowIncorrectTokenException();
			}
			if ((flags & ValueTaskSourceOnCompletedFlags.FlowExecutionContext) != 0)
			{
				_executionContext = ExecutionContext.Capture();
			}
			if ((flags & ValueTaskSourceOnCompletedFlags.UseSchedulingContext) != 0)
			{
				SynchronizationContext current = SynchronizationContext.Current;
				if (current != null && current.GetType() != typeof(SynchronizationContext))
				{
					_scheduler = current;
				}
				else
				{
					TaskScheduler current2 = TaskScheduler.Current;
					if (current2 != TaskScheduler.Default)
					{
						_scheduler = current2;
					}
				}
			}
			base.UserToken = state;
			Action<object> action = Interlocked.CompareExchange(ref _continuation, continuation, null);
			if ((object)action == s_completedSentinel)
			{
				bool requiresExecutionContextFlow = _executionContext != null;
				_executionContext = null;
				base.UserToken = null;
				InvokeContinuation(continuation, state, forceAsync: true, requiresExecutionContextFlow);
			}
			else if (action != null)
			{
				ThrowMultipleContinuationsException();
			}
		}

		private void InvokeContinuation(Action<object> continuation, object state, bool forceAsync, bool requiresExecutionContextFlow)
		{
			object scheduler = _scheduler;
			_scheduler = null;
			if (scheduler != null)
			{
				if (scheduler is SynchronizationContext synchronizationContext)
				{
					synchronizationContext.Post(delegate(object s)
					{
						(Action<object>, object) tuple = ((Action<object>, object))s;
						tuple.Item1(tuple.Item2);
					}, (continuation, state));
				}
				else
				{
					Task.Factory.StartNew(continuation, state, CancellationToken.None, TaskCreationOptions.DenyChildAttach, (TaskScheduler)scheduler);
				}
			}
			else if (forceAsync)
			{
				if (requiresExecutionContextFlow)
				{
					ThreadPool.QueueUserWorkItem(continuation, state, preferLocal: true);
				}
				else
				{
					ThreadPool.UnsafeQueueUserWorkItem(continuation, state, preferLocal: true);
				}
			}
			else
			{
				continuation(state);
			}
		}

		int IValueTaskSource<int>.GetResult(short token)
		{
			if (token != _token)
			{
				ThrowIncorrectTokenException();
			}
			SocketError socketError = base.SocketError;
			int bytesTransferred = base.BytesTransferred;
			CancellationToken cancellationToken = _cancellationToken;
			Release();
			if (socketError != 0)
			{
				ThrowException(socketError, cancellationToken);
			}
			return bytesTransferred;
		}

		void IValueTaskSource.GetResult(short token)
		{
			if (token != _token)
			{
				ThrowIncorrectTokenException();
			}
			SocketError socketError = base.SocketError;
			CancellationToken cancellationToken = _cancellationToken;
			Release();
			if (socketError != 0)
			{
				ThrowException(socketError, cancellationToken);
			}
		}

		Socket IValueTaskSource<Socket>.GetResult(short token)
		{
			if (token != _token)
			{
				ThrowIncorrectTokenException();
			}
			SocketError socketError = base.SocketError;
			Socket acceptSocket = base.AcceptSocket;
			CancellationToken cancellationToken = _cancellationToken;
			Release();
			if (socketError != 0)
			{
				ThrowException(socketError, cancellationToken);
			}
			return acceptSocket;
		}

		SocketReceiveFromResult IValueTaskSource<SocketReceiveFromResult>.GetResult(short token)
		{
			if (token != _token)
			{
				ThrowIncorrectTokenException();
			}
			SocketError socketError = base.SocketError;
			int bytesTransferred = base.BytesTransferred;
			EndPoint remoteEndPoint = base.RemoteEndPoint;
			CancellationToken cancellationToken = _cancellationToken;
			Release();
			if (socketError != 0)
			{
				ThrowException(socketError, cancellationToken);
			}
			SocketReceiveFromResult result = default(SocketReceiveFromResult);
			result.ReceivedBytes = bytesTransferred;
			result.RemoteEndPoint = remoteEndPoint;
			return result;
		}

		SocketReceiveMessageFromResult IValueTaskSource<SocketReceiveMessageFromResult>.GetResult(short token)
		{
			if (token != _token)
			{
				ThrowIncorrectTokenException();
			}
			SocketError socketError = base.SocketError;
			int bytesTransferred = base.BytesTransferred;
			EndPoint remoteEndPoint = base.RemoteEndPoint;
			SocketFlags socketFlags = base.SocketFlags;
			IPPacketInformation receiveMessageFromPacketInfo = base.ReceiveMessageFromPacketInfo;
			CancellationToken cancellationToken = _cancellationToken;
			Release();
			if (socketError != 0)
			{
				ThrowException(socketError, cancellationToken);
			}
			SocketReceiveMessageFromResult result = default(SocketReceiveMessageFromResult);
			result.ReceivedBytes = bytesTransferred;
			result.RemoteEndPoint = remoteEndPoint;
			result.SocketFlags = socketFlags;
			result.PacketInformation = receiveMessageFromPacketInfo;
			return result;
		}

		private void ThrowIncorrectTokenException()
		{
			throw new InvalidOperationException(System.SR.InvalidOperation_IncorrectToken);
		}

		private void ThrowMultipleContinuationsException()
		{
			throw new InvalidOperationException(System.SR.InvalidOperation_MultipleContinuations);
		}

		private void ThrowException(SocketError error, CancellationToken cancellationToken)
		{
			if (error == SocketError.OperationAborted || error == SocketError.ConnectionAborted)
			{
				cancellationToken.ThrowIfCancellationRequested();
			}
			throw CreateException(error, forAsyncThrow: false);
		}

		private Exception CreateException(SocketError error, bool forAsyncThrow = true)
		{
			Exception ex = (Exception)(object)new SocketException((int)error);
			if (forAsyncThrow)
			{
				ex = ExceptionDispatchInfo.SetCurrentStackTrace(ex);
			}
			if (!WrapExceptionsForNetworkStream)
			{
				return ex;
			}
			return new IOException(System.SR.Format(_isReadForCaching ? System.SR.net_io_readfailure : System.SR.net_io_writefailure, ex.Message), ex);
		}
	}

	private sealed class CachedSerializedEndPoint
	{
		public readonly IPEndPoint IPEndPoint;

		public readonly System.Net.Internals.SocketAddress SocketAddress;

		public CachedSerializedEndPoint(IPAddress address)
		{
			IPEndPoint = new IPEndPoint(address, 0);
			SocketAddress = IPEndPointExtensions.Serialize(IPEndPoint);
		}
	}

	private static readonly IPAddress s_IPAddressAnyMapToIPv6 = IPAddress.Any.MapToIPv6();

	private SafeSocketHandle _handle;

	internal EndPoint _rightEndPoint;

	internal EndPoint _remoteEndPoint;

	private EndPoint _localEndPoint;

	private bool _isConnected;

	private bool _isDisconnected;

	private bool _willBlock = true;

	private bool _willBlockInternal = true;

	private bool _isListening;

	private bool _nonBlockingConnectInProgress;

	private EndPoint _pendingConnectRightEndPoint;

	private AddressFamily _addressFamily;

	private SocketType _socketType;

	private ProtocolType _protocolType;

	private bool _receivingPacketInformation;

	private int _closeTimeout = -1;

	private int _disposed;

	private AwaitableSocketAsyncEventArgs _singleBufferReceiveEventArgs;

	private AwaitableSocketAsyncEventArgs _singleBufferSendEventArgs;

	private TaskSocketAsyncEventArgs<int> _multiBufferReceiveEventArgs;

	private TaskSocketAsyncEventArgs<int> _multiBufferSendEventArgs;

	private static CachedSerializedEndPoint s_cachedAnyEndPoint;

	private static CachedSerializedEndPoint s_cachedAnyV6EndPoint;

	private static CachedSerializedEndPoint s_cachedMappedAnyV6EndPoint;

	private DynamicWinsockMethods _dynamicWinsockMethods;

	[Obsolete("SupportsIPv4 has been deprecated. Use OSSupportsIPv4 instead.")]
	public static bool SupportsIPv4 => OSSupportsIPv4;

	[Obsolete("SupportsIPv6 has been deprecated. Use OSSupportsIPv6 instead.")]
	public static bool SupportsIPv6 => OSSupportsIPv6;

	public static bool OSSupportsIPv4 => System.Net.SocketProtocolSupportPal.OSSupportsIPv4;

	public static bool OSSupportsIPv6 => System.Net.SocketProtocolSupportPal.OSSupportsIPv6;

	public static bool OSSupportsUnixDomainSockets => System.Net.SocketProtocolSupportPal.OSSupportsUnixDomainSockets;

	public int Available
	{
		get
		{
			ThrowIfDisposed();
			int available;
			SocketError available2 = SocketPal.GetAvailable(_handle, out available);
			if (System.Net.NetEventSource.Log.IsEnabled())
			{
				System.Net.NetEventSource.Info(this, $"GetAvailable returns errorCode:{available2}", "Available");
			}
			if (available2 != 0)
			{
				UpdateStatusAfterSocketErrorAndThrowException(available2, "Available");
			}
			return available;
		}
	}

	public unsafe EndPoint? LocalEndPoint
	{
		get
		{
			ThrowIfDisposed();
			if (_nonBlockingConnectInProgress && Poll(0, SelectMode.SelectWrite))
			{
				_nonBlockingConnectInProgress = false;
				SetToConnected();
			}
			if (_rightEndPoint == null)
			{
				return null;
			}
			if (_localEndPoint == null)
			{
				System.Net.Internals.SocketAddress socketAddress = IPEndPointExtensions.Serialize(_rightEndPoint);
				fixed (byte* buffer = socketAddress.Buffer)
				{
					fixed (int* nameLen = &socketAddress.InternalSize)
					{
						SocketError sockName = SocketPal.GetSockName(_handle, buffer, nameLen);
						if (sockName != 0)
						{
							UpdateStatusAfterSocketErrorAndThrowException(sockName, "LocalEndPoint");
						}
					}
				}
				_localEndPoint = _rightEndPoint.Create(socketAddress);
			}
			return _localEndPoint;
		}
	}

	public EndPoint? RemoteEndPoint
	{
		get
		{
			ThrowIfDisposed();
			if (_remoteEndPoint == null)
			{
				if (_nonBlockingConnectInProgress && Poll(0, SelectMode.SelectWrite))
				{
					_nonBlockingConnectInProgress = false;
					SetToConnected();
				}
				if (_rightEndPoint == null || !_isConnected)
				{
					return null;
				}
				System.Net.Internals.SocketAddress socketAddress = ((_addressFamily == AddressFamily.InterNetwork || _addressFamily == AddressFamily.InterNetworkV6) ? IPEndPointExtensions.Serialize(_rightEndPoint) : new System.Net.Internals.SocketAddress(_addressFamily, SocketPal.MaximumAddressSize));
				SocketError peerName = SocketPal.GetPeerName(_handle, socketAddress.Buffer, ref socketAddress.InternalSize);
				if (peerName != 0)
				{
					UpdateStatusAfterSocketErrorAndThrowException(peerName, "RemoteEndPoint");
				}
				try
				{
					_remoteEndPoint = _rightEndPoint.Create(socketAddress);
				}
				catch
				{
				}
			}
			return _remoteEndPoint;
		}
	}

	public IntPtr Handle => SafeHandle.DangerousGetHandle();

	public SafeSocketHandle SafeHandle
	{
		get
		{
			_handle.SetExposed();
			return _handle;
		}
	}

	internal SafeSocketHandle InternalSafeHandle => _handle;

	public bool Blocking
	{
		get
		{
			return _willBlock;
		}
		set
		{
			ThrowIfDisposed();
			if (System.Net.NetEventSource.Log.IsEnabled())
			{
				System.Net.NetEventSource.Info(this, $"value:{value} willBlock:{_willBlock} willBlockInternal:{_willBlockInternal}", "Blocking");
			}
			bool current;
			SocketError socketError = InternalSetBlocking(value, out current);
			if (socketError != 0)
			{
				UpdateStatusAfterSocketErrorAndThrowException(socketError, "Blocking");
			}
			_willBlock = current;
		}
	}

	[Obsolete("UseOnlyOverlappedIO has been deprecated and is not supported.")]
	[EditorBrowsable(EditorBrowsableState.Never)]
	public bool UseOnlyOverlappedIO
	{
		get
		{
			return false;
		}
		set
		{
		}
	}

	public bool Connected
	{
		get
		{
			if (System.Net.NetEventSource.Log.IsEnabled())
			{
				System.Net.NetEventSource.Info(this, $"_isConnected:{_isConnected}", "Connected");
			}
			if (_nonBlockingConnectInProgress && Poll(0, SelectMode.SelectWrite))
			{
				_nonBlockingConnectInProgress = false;
				SetToConnected();
			}
			return _isConnected;
		}
	}

	public AddressFamily AddressFamily => _addressFamily;

	public SocketType SocketType => _socketType;

	public ProtocolType ProtocolType => _protocolType;

	public bool IsBound => _rightEndPoint != null;

	public bool ExclusiveAddressUse
	{
		get
		{
			if ((int)GetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ExclusiveAddressUse) == 0)
			{
				return false;
			}
			return true;
		}
		set
		{
			if (IsBound)
			{
				throw new InvalidOperationException(System.SR.net_sockets_mustnotbebound);
			}
			SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ExclusiveAddressUse, value ? 1 : 0);
		}
	}

	public int ReceiveBufferSize
	{
		get
		{
			return (int)GetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReceiveBuffer);
		}
		set
		{
			if (value < 0)
			{
				throw new ArgumentOutOfRangeException("value");
			}
			SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReceiveBuffer, value);
		}
	}

	public int SendBufferSize
	{
		get
		{
			return (int)GetSocketOption(SocketOptionLevel.Socket, SocketOptionName.SendBuffer);
		}
		set
		{
			if (value < 0)
			{
				throw new ArgumentOutOfRangeException("value");
			}
			SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.SendBuffer, value);
		}
	}

	public int ReceiveTimeout
	{
		get
		{
			return (int)GetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReceiveTimeout);
		}
		set
		{
			if (value < -1)
			{
				throw new ArgumentOutOfRangeException("value");
			}
			if (value == -1)
			{
				value = 0;
			}
			SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReceiveTimeout, value);
		}
	}

	public int SendTimeout
	{
		get
		{
			return (int)GetSocketOption(SocketOptionLevel.Socket, SocketOptionName.SendTimeout);
		}
		set
		{
			if (value < -1)
			{
				throw new ArgumentOutOfRangeException("value");
			}
			if (value == -1)
			{
				value = 0;
			}
			SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.SendTimeout, value);
		}
	}

	public LingerOption? LingerState
	{
		get
		{
			return (LingerOption)GetSocketOption(SocketOptionLevel.Socket, SocketOptionName.Linger);
		}
		[param: DisallowNull]
		set
		{
			SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.Linger, value);
		}
	}

	public bool NoDelay
	{
		get
		{
			if ((int)GetSocketOption(SocketOptionLevel.Tcp, SocketOptionName.Debug) == 0)
			{
				return false;
			}
			return true;
		}
		set
		{
			SetSocketOption(SocketOptionLevel.Tcp, SocketOptionName.Debug, value ? 1 : 0);
		}
	}

	public short Ttl
	{
		get
		{
			if (_addressFamily == AddressFamily.InterNetwork)
			{
				return (short)(int)GetSocketOption(SocketOptionLevel.IP, SocketOptionName.ReuseAddress);
			}
			if (_addressFamily == AddressFamily.InterNetworkV6)
			{
				return (short)(int)GetSocketOption(SocketOptionLevel.IPv6, SocketOptionName.ReuseAddress);
			}
			throw new NotSupportedException(System.SR.net_invalidversion);
		}
		set
		{
			if (value < 0 || value > 255)
			{
				throw new ArgumentOutOfRangeException("value");
			}
			if (_addressFamily == AddressFamily.InterNetwork)
			{
				SetSocketOption(SocketOptionLevel.IP, SocketOptionName.ReuseAddress, value);
				return;
			}
			if (_addressFamily == AddressFamily.InterNetworkV6)
			{
				SetSocketOption(SocketOptionLevel.IPv6, SocketOptionName.ReuseAddress, value);
				return;
			}
			throw new NotSupportedException(System.SR.net_invalidversion);
		}
	}

	public bool DontFragment
	{
		get
		{
			if (_addressFamily == AddressFamily.InterNetwork)
			{
				if ((int)GetSocketOption(SocketOptionLevel.IP, SocketOptionName.DontFragment) == 0)
				{
					return false;
				}
				return true;
			}
			throw new NotSupportedException(System.SR.net_invalidversion);
		}
		set
		{
			if (_addressFamily == AddressFamily.InterNetwork)
			{
				SetSocketOption(SocketOptionLevel.IP, SocketOptionName.DontFragment, value ? 1 : 0);
				return;
			}
			throw new NotSupportedException(System.SR.net_invalidversion);
		}
	}

	public bool MulticastLoopback
	{
		get
		{
			if (_addressFamily == AddressFamily.InterNetwork)
			{
				if ((int)GetSocketOption(SocketOptionLevel.IP, SocketOptionName.MulticastLoopback) == 0)
				{
					return false;
				}
				return true;
			}
			if (_addressFamily == AddressFamily.InterNetworkV6)
			{
				if ((int)GetSocketOption(SocketOptionLevel.IPv6, SocketOptionName.MulticastLoopback) == 0)
				{
					return false;
				}
				return true;
			}
			throw new NotSupportedException(System.SR.net_invalidversion);
		}
		set
		{
			if (_addressFamily == AddressFamily.InterNetwork)
			{
				SetSocketOption(SocketOptionLevel.IP, SocketOptionName.MulticastLoopback, value ? 1 : 0);
				return;
			}
			if (_addressFamily == AddressFamily.InterNetworkV6)
			{
				SetSocketOption(SocketOptionLevel.IPv6, SocketOptionName.MulticastLoopback, value ? 1 : 0);
				return;
			}
			throw new NotSupportedException(System.SR.net_invalidversion);
		}
	}

	public bool EnableBroadcast
	{
		get
		{
			if ((int)GetSocketOption(SocketOptionLevel.Socket, SocketOptionName.Broadcast) == 0)
			{
				return false;
			}
			return true;
		}
		set
		{
			SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.Broadcast, value ? 1 : 0);
		}
	}

	public bool DualMode
	{
		get
		{
			if (AddressFamily != AddressFamily.InterNetworkV6)
			{
				return false;
			}
			return (int)GetSocketOption(SocketOptionLevel.IPv6, SocketOptionName.IPv6Only) == 0;
		}
		set
		{
			if (AddressFamily != AddressFamily.InterNetworkV6)
			{
				throw new NotSupportedException(System.SR.net_invalidversion);
			}
			SetSocketOption(SocketOptionLevel.IPv6, SocketOptionName.IPv6Only, (!value) ? 1 : 0);
		}
	}

	private bool IsDualMode
	{
		get
		{
			if (AddressFamily == AddressFamily.InterNetworkV6)
			{
				return DualMode;
			}
			return false;
		}
	}

	internal bool Disposed => _disposed != 0;

	private bool IsConnectionOriented => _socketType == SocketType.Stream;

	public Socket(SocketType socketType, ProtocolType protocolType)
		: this(OSSupportsIPv6 ? AddressFamily.InterNetworkV6 : AddressFamily.InterNetwork, socketType, protocolType)
	{
		if (OSSupportsIPv6)
		{
			DualMode = true;
		}
	}

	public Socket(AddressFamily addressFamily, SocketType socketType, ProtocolType protocolType)
	{
		if (System.Net.NetEventSource.Log.IsEnabled())
		{
			System.Net.NetEventSource.Info(this, addressFamily, ".ctor");
		}
		SocketError socketError = SocketPal.CreateSocket(addressFamily, socketType, protocolType, out _handle);
		if (socketError != 0)
		{
			throw new SocketException((int)socketError);
		}
		_addressFamily = addressFamily;
		_socketType = socketType;
		_protocolType = protocolType;
	}

	public Socket(SafeSocketHandle handle)
		: this(ValidateHandle(handle), loadPropertiesFromHandle: true)
	{
	}

	private unsafe Socket(SafeSocketHandle handle, bool loadPropertiesFromHandle)
	{
		_handle = handle;
		_addressFamily = AddressFamily.Unknown;
		_socketType = SocketType.Unknown;
		_protocolType = ProtocolType.Unknown;
		if (!loadPropertiesFromHandle)
		{
			return;
		}
		try
		{
			LoadSocketTypeFromHandle(handle, out _addressFamily, out _socketType, out _protocolType, out _willBlockInternal, out _isListening, out var isSocket);
			if (!isSocket)
			{
				return;
			}
			Span<byte> span = stackalloc byte[SocketPal.MaximumAddressSize];
			int nameLen = span.Length;
			fixed (byte* buffer = span)
			{
				if (SocketPal.GetSockName(handle, buffer, &nameLen) != 0)
				{
					return;
				}
			}
			System.Net.Internals.SocketAddress socketAddress = null;
			switch (_addressFamily)
			{
			case AddressFamily.InterNetwork:
				_rightEndPoint = new IPEndPoint(new IPAddress((long)System.Net.SocketAddressPal.GetIPv4Address(span.Slice(0, nameLen)) & 0xFFFFFFFFL), System.Net.SocketAddressPal.GetPort(span));
				break;
			case AddressFamily.InterNetworkV6:
			{
				Span<byte> span2 = stackalloc byte[16];
				System.Net.SocketAddressPal.GetIPv6Address(span.Slice(0, nameLen), span2, out var scope);
				_rightEndPoint = new IPEndPoint(new IPAddress(span2, scope), System.Net.SocketAddressPal.GetPort(span));
				break;
			}
			case AddressFamily.Unix:
				socketAddress = new System.Net.Internals.SocketAddress(_addressFamily, span.Slice(0, nameLen));
				_rightEndPoint = new UnixDomainSocketEndPoint(IPEndPointExtensions.GetNetSocketAddress(socketAddress));
				break;
			}
			if (_rightEndPoint == null)
			{
				return;
			}
			try
			{
				nameLen = span.Length;
				switch (SocketPal.GetPeerName(handle, span, ref nameLen))
				{
				case SocketError.Success:
					switch (_addressFamily)
					{
					case AddressFamily.InterNetwork:
						_remoteEndPoint = new IPEndPoint(new IPAddress((long)System.Net.SocketAddressPal.GetIPv4Address(span.Slice(0, nameLen)) & 0xFFFFFFFFL), System.Net.SocketAddressPal.GetPort(span));
						break;
					case AddressFamily.InterNetworkV6:
					{
						Span<byte> span3 = stackalloc byte[16];
						System.Net.SocketAddressPal.GetIPv6Address(span.Slice(0, nameLen), span3, out var scope2);
						_remoteEndPoint = new IPEndPoint(new IPAddress(span3, scope2), System.Net.SocketAddressPal.GetPort(span));
						break;
					}
					case AddressFamily.Unix:
						socketAddress = new System.Net.Internals.SocketAddress(_addressFamily, span.Slice(0, nameLen));
						_remoteEndPoint = new UnixDomainSocketEndPoint(IPEndPointExtensions.GetNetSocketAddress(socketAddress));
						break;
					}
					_isConnected = true;
					break;
				case SocketError.InvalidArgument:
					_isConnected = true;
					break;
				}
			}
			catch
			{
			}
		}
		catch
		{
			_handle = null;
			GC.SuppressFinalize(this);
			throw;
		}
	}

	private static SafeSocketHandle ValidateHandle(SafeSocketHandle handle)
	{
		if (handle != null)
		{
			if (!handle.IsInvalid)
			{
				return handle;
			}
			throw new ArgumentException(System.SR.Arg_InvalidHandle, "handle");
		}
		throw new ArgumentNullException("handle");
	}

	internal bool CanTryAddressFamily(AddressFamily family)
	{
		if (family != _addressFamily)
		{
			if (family == AddressFamily.InterNetwork)
			{
				return IsDualMode;
			}
			return false;
		}
		return true;
	}

	public void Bind(EndPoint localEP)
	{
		if (System.Net.NetEventSource.Log.IsEnabled())
		{
			System.Net.NetEventSource.Info(this, localEP, "Bind");
		}
		ThrowIfDisposed();
		if (localEP == null)
		{
			throw new ArgumentNullException("localEP");
		}
		if (System.Net.NetEventSource.Log.IsEnabled())
		{
			System.Net.NetEventSource.Info(this, $"localEP:{localEP}", "Bind");
		}
		System.Net.Internals.SocketAddress socketAddress = Serialize(ref localEP);
		DoBind(localEP, socketAddress);
	}

	private void DoBind(EndPoint endPointSnapshot, System.Net.Internals.SocketAddress socketAddress)
	{
		IPEndPoint iPEndPoint = endPointSnapshot as IPEndPoint;
		if (!OSSupportsIPv4 && iPEndPoint != null && iPEndPoint.Address.IsIPv4MappedToIPv6)
		{
			UpdateStatusAfterSocketErrorAndThrowException(SocketError.InvalidArgument, "DoBind");
		}
		SocketError socketError = SocketPal.Bind(_handle, _protocolType, socketAddress.Buffer, socketAddress.Size);
		if (socketError != 0)
		{
			UpdateStatusAfterSocketErrorAndThrowException(socketError, "DoBind");
		}
		_rightEndPoint = ((endPointSnapshot is UnixDomainSocketEndPoint unixDomainSocketEndPoint) ? unixDomainSocketEndPoint.CreateBoundEndPoint() : endPointSnapshot);
	}

	public void Connect(EndPoint remoteEP)
	{
		ThrowIfDisposed();
		if (remoteEP == null)
		{
			throw new ArgumentNullException("remoteEP");
		}
		if (_isDisconnected)
		{
			throw new InvalidOperationException(System.SR.net_sockets_disconnectedConnect);
		}
		if (_isListening)
		{
			throw new InvalidOperationException(System.SR.net_sockets_mustnotlisten);
		}
		if (_isConnected)
		{
			throw new SocketException(10056);
		}
		ValidateBlockingMode();
		if (System.Net.NetEventSource.Log.IsEnabled())
		{
			System.Net.NetEventSource.Info(this, $"DST:{remoteEP}", "Connect");
		}
		if (remoteEP is DnsEndPoint dnsEndPoint)
		{
			if (dnsEndPoint.AddressFamily != 0 && !CanTryAddressFamily(dnsEndPoint.AddressFamily))
			{
				throw new NotSupportedException(System.SR.net_invalidversion);
			}
			Connect(dnsEndPoint.Host, dnsEndPoint.Port);
		}
		else
		{
			System.Net.Internals.SocketAddress socketAddress = Serialize(ref remoteEP);
			_pendingConnectRightEndPoint = remoteEP;
			_nonBlockingConnectInProgress = !Blocking;
			DoConnect(remoteEP, socketAddress);
		}
	}

	public void Connect(IPAddress address, int port)
	{
		ThrowIfDisposed();
		if (address == null)
		{
			throw new ArgumentNullException("address");
		}
		if (!System.Net.TcpValidationHelpers.ValidatePortNumber(port))
		{
			throw new ArgumentOutOfRangeException("port");
		}
		if (_isConnected)
		{
			throw new SocketException(10056);
		}
		if (!CanTryAddressFamily(address.AddressFamily))
		{
			throw new NotSupportedException(System.SR.net_invalidversion);
		}
		IPEndPoint remoteEP = new IPEndPoint(address, port);
		Connect(remoteEP);
	}

	public void Connect(string host, int port)
	{
		ThrowIfDisposed();
		if (host == null)
		{
			throw new ArgumentNullException("host");
		}
		if (!System.Net.TcpValidationHelpers.ValidatePortNumber(port))
		{
			throw new ArgumentOutOfRangeException("port");
		}
		if (_addressFamily != AddressFamily.InterNetwork && _addressFamily != AddressFamily.InterNetworkV6)
		{
			throw new NotSupportedException(System.SR.net_invalidversion);
		}
		if (IPAddress.TryParse(host, out IPAddress address))
		{
			Connect(address, port);
			return;
		}
		IPAddress[] hostAddresses = Dns.GetHostAddresses(host);
		Connect(hostAddresses, port);
	}

	public void Connect(IPAddress[] addresses, int port)
	{
		ThrowIfDisposed();
		if (addresses == null)
		{
			throw new ArgumentNullException("addresses");
		}
		if (addresses.Length == 0)
		{
			throw new ArgumentException(System.SR.net_sockets_invalid_ipaddress_length, "addresses");
		}
		if (!System.Net.TcpValidationHelpers.ValidatePortNumber(port))
		{
			throw new ArgumentOutOfRangeException("port");
		}
		if (_addressFamily != AddressFamily.InterNetwork && _addressFamily != AddressFamily.InterNetworkV6)
		{
			throw new NotSupportedException(System.SR.net_invalidversion);
		}
		if (_isConnected)
		{
			throw new SocketException(10056);
		}
		ExceptionDispatchInfo exceptionDispatchInfo = null;
		foreach (IPAddress iPAddress in addresses)
		{
			if (CanTryAddressFamily(iPAddress.AddressFamily))
			{
				try
				{
					Connect(new IPEndPoint(iPAddress, port));
					exceptionDispatchInfo = null;
				}
				catch (Exception ex) when (!ExceptionCheck.IsFatal(ex))
				{
					exceptionDispatchInfo = ExceptionDispatchInfo.Capture(ex);
					continue;
				}
				break;
			}
		}
		exceptionDispatchInfo?.Throw();
		if (!Connected)
		{
			throw new ArgumentException(System.SR.net_invalidAddressList, "addresses");
		}
	}

	public void Close()
	{
		if (System.Net.NetEventSource.Log.IsEnabled())
		{
			System.Net.NetEventSource.Info(this, $"timeout = {_closeTimeout}", "Close");
		}
		Dispose();
	}

	public void Close(int timeout)
	{
		if (timeout < -1)
		{
			throw new ArgumentOutOfRangeException("timeout");
		}
		_closeTimeout = timeout;
		if (System.Net.NetEventSource.Log.IsEnabled())
		{
			System.Net.NetEventSource.Info(this, $"timeout = {_closeTimeout}", "Close");
		}
		Dispose();
	}

	public void Listen()
	{
		Listen(int.MaxValue);
	}

	public void Listen(int backlog)
	{
		if (System.Net.NetEventSource.Log.IsEnabled())
		{
			System.Net.NetEventSource.Info(this, backlog, "Listen");
		}
		ThrowIfDisposed();
		SocketError socketError = SocketPal.Listen(_handle, backlog);
		if (socketError != 0)
		{
			UpdateStatusAfterSocketErrorAndThrowException(socketError, "Listen");
		}
		_isListening = true;
	}

	public Socket Accept()
	{
		ThrowIfDisposed();
		if (_rightEndPoint == null)
		{
			throw new InvalidOperationException(System.SR.net_sockets_mustbind);
		}
		if (!_isListening)
		{
			throw new InvalidOperationException(System.SR.net_sockets_mustlisten);
		}
		if (_isDisconnected)
		{
			throw new InvalidOperationException(System.SR.net_sockets_disconnectedAccept);
		}
		ValidateBlockingMode();
		if (System.Net.NetEventSource.Log.IsEnabled())
		{
			System.Net.NetEventSource.Info(this, $"SRC:{LocalEndPoint}", "Accept");
		}
		System.Net.Internals.SocketAddress socketAddress = ((_addressFamily == AddressFamily.InterNetwork || _addressFamily == AddressFamily.InterNetworkV6) ? IPEndPointExtensions.Serialize(_rightEndPoint) : new System.Net.Internals.SocketAddress(_addressFamily, SocketPal.MaximumAddressSize));
		if (SocketsTelemetry.Log.IsEnabled())
		{
			SocketsTelemetry.Log.AcceptStart(socketAddress);
		}
		SocketError socketError;
		SafeSocketHandle socket;
		try
		{
			socketError = SocketPal.Accept(_handle, socketAddress.Buffer, ref socketAddress.InternalSize, out socket);
		}
		catch (Exception ex)
		{
			if (SocketsTelemetry.Log.IsEnabled())
			{
				SocketsTelemetry.Log.AfterAccept(SocketError.Interrupted, ex.Message);
			}
			throw;
		}
		if (socketError != 0)
		{
			UpdateAcceptSocketErrorForDisposed(ref socketError);
			if (SocketsTelemetry.Log.IsEnabled())
			{
				SocketsTelemetry.Log.AfterAccept(socketError);
			}
			UpdateStatusAfterSocketErrorAndThrowException(socketError, "Accept");
		}
		if (SocketsTelemetry.Log.IsEnabled())
		{
			SocketsTelemetry.Log.AfterAccept(SocketError.Success);
		}
		Socket socket2 = CreateAcceptSocket(socket, _rightEndPoint.Create(socketAddress));
		if (System.Net.NetEventSource.Log.IsEnabled())
		{
			System.Net.NetEventSource.Accepted(socket2, socket2.RemoteEndPoint, socket2.LocalEndPoint);
		}
		return socket2;
	}

	public int Send(byte[] buffer, int size, SocketFlags socketFlags)
	{
		return Send(buffer, 0, size, socketFlags);
	}

	public int Send(byte[] buffer, SocketFlags socketFlags)
	{
		return Send(buffer, 0, (buffer != null) ? buffer.Length : 0, socketFlags);
	}

	public int Send(byte[] buffer)
	{
		return Send(buffer, 0, (buffer != null) ? buffer.Length : 0, SocketFlags.None);
	}

	public int Send(IList<ArraySegment<byte>> buffers)
	{
		return Send(buffers, SocketFlags.None);
	}

	public int Send(IList<ArraySegment<byte>> buffers, SocketFlags socketFlags)
	{
		SocketError errorCode;
		int result = Send(buffers, socketFlags, out errorCode);
		if (errorCode != 0)
		{
			throw new SocketException((int)errorCode);
		}
		return result;
	}

	public int Send(IList<ArraySegment<byte>> buffers, SocketFlags socketFlags, out SocketError errorCode)
	{
		ThrowIfDisposed();
		if (buffers == null)
		{
			throw new ArgumentNullException("buffers");
		}
		if (buffers.Count == 0)
		{
			throw new ArgumentException(System.SR.Format(System.SR.net_sockets_zerolist, "buffers"), "buffers");
		}
		ValidateBlockingMode();
		if (System.Net.NetEventSource.Log.IsEnabled())
		{
			System.Net.NetEventSource.Info(this, $"SRC:{LocalEndPoint} DST:{RemoteEndPoint}", "Send");
		}
		errorCode = SocketPal.Send(_handle, buffers, socketFlags, out var bytesTransferred);
		if (errorCode != 0)
		{
			UpdateSendSocketErrorForDisposed(ref errorCode);
			UpdateStatusAfterSocketError(errorCode);
			if (System.Net.NetEventSource.Log.IsEnabled())
			{
				System.Net.NetEventSource.Error(this, new SocketException((int)errorCode), "Send");
			}
			return 0;
		}
		if (SocketsTelemetry.Log.IsEnabled())
		{
			SocketsTelemetry.Log.BytesSent(bytesTransferred);
			if (SocketType == SocketType.Dgram)
			{
				SocketsTelemetry.Log.DatagramSent();
			}
		}
		return bytesTransferred;
	}

	public int Send(byte[] buffer, int offset, int size, SocketFlags socketFlags)
	{
		SocketError errorCode;
		int result = Send(buffer, offset, size, socketFlags, out errorCode);
		if (errorCode != 0)
		{
			throw new SocketException((int)errorCode);
		}
		return result;
	}

	public int Send(byte[] buffer, int offset, int size, SocketFlags socketFlags, out SocketError errorCode)
	{
		ThrowIfDisposed();
		ValidateBufferArguments(buffer, offset, size);
		errorCode = SocketError.Success;
		ValidateBlockingMode();
		if (System.Net.NetEventSource.Log.IsEnabled())
		{
			System.Net.NetEventSource.Info(this, $"SRC:{LocalEndPoint} DST:{RemoteEndPoint} size:{size}", "Send");
		}
		errorCode = SocketPal.Send(_handle, buffer, offset, size, socketFlags, out var bytesTransferred);
		if (errorCode != 0)
		{
			UpdateSendSocketErrorForDisposed(ref errorCode);
			UpdateStatusAfterSocketError(errorCode);
			if (System.Net.NetEventSource.Log.IsEnabled())
			{
				System.Net.NetEventSource.Error(this, new SocketException((int)errorCode), "Send");
			}
			return 0;
		}
		if (SocketsTelemetry.Log.IsEnabled())
		{
			SocketsTelemetry.Log.BytesSent(bytesTransferred);
			if (SocketType == SocketType.Dgram)
			{
				SocketsTelemetry.Log.DatagramSent();
			}
		}
		if (System.Net.NetEventSource.Log.IsEnabled())
		{
			System.Net.NetEventSource.Info(this, $"Send returns:{bytesTransferred}", "Send");
			System.Net.NetEventSource.DumpBuffer(this, buffer, offset, bytesTransferred, "Send");
		}
		return bytesTransferred;
	}

	public int Send(ReadOnlySpan<byte> buffer)
	{
		return Send(buffer, SocketFlags.None);
	}

	public int Send(ReadOnlySpan<byte> buffer, SocketFlags socketFlags)
	{
		SocketError errorCode;
		int result = Send(buffer, socketFlags, out errorCode);
		if (errorCode != 0)
		{
			throw new SocketException((int)errorCode);
		}
		return result;
	}

	public int Send(ReadOnlySpan<byte> buffer, SocketFlags socketFlags, out SocketError errorCode)
	{
		ThrowIfDisposed();
		ValidateBlockingMode();
		errorCode = SocketPal.Send(_handle, buffer, socketFlags, out var bytesTransferred);
		if (errorCode != 0)
		{
			UpdateSendSocketErrorForDisposed(ref errorCode);
			UpdateStatusAfterSocketError(errorCode);
			if (System.Net.NetEventSource.Log.IsEnabled())
			{
				System.Net.NetEventSource.Error(this, new SocketException((int)errorCode), "Send");
			}
			return 0;
		}
		if (SocketsTelemetry.Log.IsEnabled())
		{
			SocketsTelemetry.Log.BytesSent(bytesTransferred);
			if (SocketType == SocketType.Dgram)
			{
				SocketsTelemetry.Log.DatagramSent();
			}
		}
		return bytesTransferred;
	}

	public void SendFile(string? fileName)
	{
		SendFile(fileName, ReadOnlySpan<byte>.Empty, ReadOnlySpan<byte>.Empty, TransmitFileOptions.UseDefaultWorkerThread);
	}

	public void SendFile(string? fileName, byte[]? preBuffer, byte[]? postBuffer, TransmitFileOptions flags)
	{
		SendFile(fileName, preBuffer.AsSpan(), postBuffer.AsSpan(), flags);
	}

	public void SendFile(string? fileName, ReadOnlySpan<byte> preBuffer, ReadOnlySpan<byte> postBuffer, TransmitFileOptions flags)
	{
		ThrowIfDisposed();
		if (!Connected)
		{
			throw new NotSupportedException(System.SR.net_notconnected);
		}
		ValidateBlockingMode();
		if (System.Net.NetEventSource.Log.IsEnabled())
		{
			System.Net.NetEventSource.Info(this, $"::SendFile() SRC:{LocalEndPoint} DST:{RemoteEndPoint} fileName:{fileName}", "SendFile");
		}
		SendFileInternal(fileName, preBuffer, postBuffer, flags);
	}

	public int SendTo(byte[] buffer, int offset, int size, SocketFlags socketFlags, EndPoint remoteEP)
	{
		ThrowIfDisposed();
		ValidateBufferArguments(buffer, offset, size);
		if (remoteEP == null)
		{
			throw new ArgumentNullException("remoteEP");
		}
		ValidateBlockingMode();
		if (System.Net.NetEventSource.Log.IsEnabled())
		{
			System.Net.NetEventSource.Info(this, $"SRC:{LocalEndPoint} size:{size} remoteEP:{remoteEP}", "SendTo");
		}
		System.Net.Internals.SocketAddress socketAddress = Serialize(ref remoteEP);
		int bytesTransferred;
		SocketError socketError = SocketPal.SendTo(_handle, buffer, offset, size, socketFlags, socketAddress.Buffer, socketAddress.Size, out bytesTransferred);
		if (socketError != 0)
		{
			UpdateSendSocketErrorForDisposed(ref socketError);
			UpdateStatusAfterSocketErrorAndThrowException(socketError, "SendTo");
		}
		else if (SocketsTelemetry.Log.IsEnabled())
		{
			SocketsTelemetry.Log.BytesSent(bytesTransferred);
			if (SocketType == SocketType.Dgram)
			{
				SocketsTelemetry.Log.DatagramSent();
			}
		}
		if (_rightEndPoint == null)
		{
			_rightEndPoint = remoteEP;
		}
		if (System.Net.NetEventSource.Log.IsEnabled())
		{
			System.Net.NetEventSource.DumpBuffer(this, buffer, offset, size, "SendTo");
		}
		return bytesTransferred;
	}

	public int SendTo(byte[] buffer, int size, SocketFlags socketFlags, EndPoint remoteEP)
	{
		return SendTo(buffer, 0, size, socketFlags, remoteEP);
	}

	public int SendTo(byte[] buffer, SocketFlags socketFlags, EndPoint remoteEP)
	{
		return SendTo(buffer, 0, (buffer != null) ? buffer.Length : 0, socketFlags, remoteEP);
	}

	public int SendTo(byte[] buffer, EndPoint remoteEP)
	{
		return SendTo(buffer, 0, (buffer != null) ? buffer.Length : 0, SocketFlags.None, remoteEP);
	}

	public int SendTo(ReadOnlySpan<byte> buffer, EndPoint remoteEP)
	{
		return SendTo(buffer, SocketFlags.None, remoteEP);
	}

	public int SendTo(ReadOnlySpan<byte> buffer, SocketFlags socketFlags, EndPoint remoteEP)
	{
		ThrowIfDisposed();
		if (remoteEP == null)
		{
			throw new ArgumentNullException("remoteEP");
		}
		ValidateBlockingMode();
		System.Net.Internals.SocketAddress socketAddress = Serialize(ref remoteEP);
		int bytesTransferred;
		SocketError socketError = SocketPal.SendTo(_handle, buffer, socketFlags, socketAddress.Buffer, socketAddress.Size, out bytesTransferred);
		if (socketError != 0)
		{
			UpdateSendSocketErrorForDisposed(ref socketError);
			UpdateStatusAfterSocketErrorAndThrowException(socketError, "SendTo");
		}
		else if (SocketsTelemetry.Log.IsEnabled())
		{
			SocketsTelemetry.Log.BytesSent(bytesTransferred);
			if (SocketType == SocketType.Dgram)
			{
				SocketsTelemetry.Log.DatagramSent();
			}
		}
		if (_rightEndPoint == null)
		{
			_rightEndPoint = remoteEP;
		}
		return bytesTransferred;
	}

	public int Receive(byte[] buffer, int size, SocketFlags socketFlags)
	{
		return Receive(buffer, 0, size, socketFlags);
	}

	public int Receive(byte[] buffer, SocketFlags socketFlags)
	{
		return Receive(buffer, 0, (buffer != null) ? buffer.Length : 0, socketFlags);
	}

	public int Receive(byte[] buffer)
	{
		return Receive(buffer, 0, (buffer != null) ? buffer.Length : 0, SocketFlags.None);
	}

	public int Receive(byte[] buffer, int offset, int size, SocketFlags socketFlags)
	{
		SocketError errorCode;
		int result = Receive(buffer, offset, size, socketFlags, out errorCode);
		if (errorCode != 0)
		{
			throw new SocketException((int)errorCode);
		}
		return result;
	}

	public int Receive(byte[] buffer, int offset, int size, SocketFlags socketFlags, out SocketError errorCode)
	{
		ThrowIfDisposed();
		ValidateBufferArguments(buffer, offset, size);
		ValidateBlockingMode();
		if (System.Net.NetEventSource.Log.IsEnabled())
		{
			System.Net.NetEventSource.Info(this, $"SRC:{LocalEndPoint} DST:{RemoteEndPoint} size:{size}", "Receive");
		}
		errorCode = SocketPal.Receive(_handle, buffer, offset, size, socketFlags, out var bytesTransferred);
		UpdateReceiveSocketErrorForDisposed(ref errorCode, bytesTransferred);
		if (errorCode != 0)
		{
			UpdateStatusAfterSocketError(errorCode);
			if (System.Net.NetEventSource.Log.IsEnabled())
			{
				System.Net.NetEventSource.Error(this, new SocketException((int)errorCode), "Receive");
			}
			return 0;
		}
		if (SocketsTelemetry.Log.IsEnabled())
		{
			SocketsTelemetry.Log.BytesReceived(bytesTransferred);
			if (SocketType == SocketType.Dgram)
			{
				SocketsTelemetry.Log.DatagramReceived();
			}
		}
		if (System.Net.NetEventSource.Log.IsEnabled())
		{
			System.Net.NetEventSource.DumpBuffer(this, buffer, offset, bytesTransferred, "Receive");
		}
		return bytesTransferred;
	}

	public int Receive(Span<byte> buffer)
	{
		return Receive(buffer, SocketFlags.None);
	}

	public int Receive(Span<byte> buffer, SocketFlags socketFlags)
	{
		SocketError errorCode;
		int result = Receive(buffer, socketFlags, out errorCode);
		if (errorCode != 0)
		{
			throw new SocketException((int)errorCode);
		}
		return result;
	}

	public int Receive(Span<byte> buffer, SocketFlags socketFlags, out SocketError errorCode)
	{
		ThrowIfDisposed();
		ValidateBlockingMode();
		errorCode = SocketPal.Receive(_handle, buffer, socketFlags, out var bytesTransferred);
		UpdateReceiveSocketErrorForDisposed(ref errorCode, bytesTransferred);
		if (errorCode != 0)
		{
			UpdateStatusAfterSocketError(errorCode);
			if (System.Net.NetEventSource.Log.IsEnabled())
			{
				System.Net.NetEventSource.Error(this, new SocketException((int)errorCode), "Receive");
			}
			return 0;
		}
		if (SocketsTelemetry.Log.IsEnabled())
		{
			SocketsTelemetry.Log.BytesReceived(bytesTransferred);
			if (SocketType == SocketType.Dgram)
			{
				SocketsTelemetry.Log.DatagramReceived();
			}
		}
		return bytesTransferred;
	}

	public int Receive(IList<ArraySegment<byte>> buffers)
	{
		return Receive(buffers, SocketFlags.None);
	}

	public int Receive(IList<ArraySegment<byte>> buffers, SocketFlags socketFlags)
	{
		SocketError errorCode;
		int result = Receive(buffers, socketFlags, out errorCode);
		if (errorCode != 0)
		{
			throw new SocketException((int)errorCode);
		}
		return result;
	}

	public int Receive(IList<ArraySegment<byte>> buffers, SocketFlags socketFlags, out SocketError errorCode)
	{
		ThrowIfDisposed();
		if (buffers == null)
		{
			throw new ArgumentNullException("buffers");
		}
		if (buffers.Count == 0)
		{
			throw new ArgumentException(System.SR.Format(System.SR.net_sockets_zerolist, "buffers"), "buffers");
		}
		ValidateBlockingMode();
		if (System.Net.NetEventSource.Log.IsEnabled())
		{
			System.Net.NetEventSource.Info(this, $"SRC:{LocalEndPoint} DST:{RemoteEndPoint}", "Receive");
		}
		errorCode = SocketPal.Receive(_handle, buffers, socketFlags, out var bytesTransferred);
		UpdateReceiveSocketErrorForDisposed(ref errorCode, bytesTransferred);
		if (errorCode != 0)
		{
			UpdateStatusAfterSocketError(errorCode);
			if (System.Net.NetEventSource.Log.IsEnabled())
			{
				System.Net.NetEventSource.Error(this, new SocketException((int)errorCode), "Receive");
			}
			return 0;
		}
		if (SocketsTelemetry.Log.IsEnabled())
		{
			SocketsTelemetry.Log.BytesReceived(bytesTransferred);
			if (SocketType == SocketType.Dgram)
			{
				SocketsTelemetry.Log.DatagramReceived();
			}
		}
		return bytesTransferred;
	}

	public int ReceiveMessageFrom(byte[] buffer, int offset, int size, ref SocketFlags socketFlags, ref EndPoint remoteEP, out IPPacketInformation ipPacketInformation)
	{
		ThrowIfDisposed();
		ValidateBufferArguments(buffer, offset, size);
		ValidateReceiveFromEndpointAndState(remoteEP, "remoteEP");
		SocketPal.CheckDualModeReceiveSupport(this);
		ValidateBlockingMode();
		EndPoint remoteEP2 = remoteEP;
		System.Net.Internals.SocketAddress socketAddress = Serialize(ref remoteEP2);
		System.Net.Internals.SocketAddress socketAddress2 = IPEndPointExtensions.Serialize(remoteEP2);
		SetReceivingPacketInformation();
		System.Net.Internals.SocketAddress receiveAddress;
		int bytesTransferred;
		SocketError socketError = SocketPal.ReceiveMessageFrom(this, _handle, buffer, offset, size, ref socketFlags, socketAddress, out receiveAddress, out ipPacketInformation, out bytesTransferred);
		UpdateReceiveSocketErrorForDisposed(ref socketError, bytesTransferred);
		if (socketError != 0 && socketError != SocketError.MessageSize)
		{
			UpdateStatusAfterSocketErrorAndThrowException(socketError, "ReceiveMessageFrom");
		}
		else if (SocketsTelemetry.Log.IsEnabled())
		{
			SocketsTelemetry.Log.BytesReceived(bytesTransferred);
			if (socketError == SocketError.Success && SocketType == SocketType.Dgram)
			{
				SocketsTelemetry.Log.DatagramReceived();
			}
		}
		if (!socketAddress2.Equals(receiveAddress))
		{
			try
			{
				remoteEP = remoteEP2.Create(receiveAddress);
			}
			catch
			{
			}
			if (_rightEndPoint == null)
			{
				_rightEndPoint = remoteEP2;
			}
		}
		if (System.Net.NetEventSource.Log.IsEnabled())
		{
			System.Net.NetEventSource.Error(this, socketError, "ReceiveMessageFrom");
		}
		return bytesTransferred;
	}

	public int ReceiveMessageFrom(Span<byte> buffer, ref SocketFlags socketFlags, ref EndPoint remoteEP, out IPPacketInformation ipPacketInformation)
	{
		ThrowIfDisposed();
		if (remoteEP == null)
		{
			throw new ArgumentNullException("remoteEP");
		}
		if (!CanTryAddressFamily(remoteEP.AddressFamily))
		{
			throw new ArgumentException(System.SR.Format(System.SR.net_InvalidEndPointAddressFamily, remoteEP.AddressFamily, _addressFamily), "remoteEP");
		}
		if (_rightEndPoint == null)
		{
			throw new InvalidOperationException(System.SR.net_sockets_mustbind);
		}
		SocketPal.CheckDualModeReceiveSupport(this);
		ValidateBlockingMode();
		EndPoint remoteEP2 = remoteEP;
		System.Net.Internals.SocketAddress socketAddress = Serialize(ref remoteEP2);
		System.Net.Internals.SocketAddress socketAddress2 = IPEndPointExtensions.Serialize(remoteEP2);
		SetReceivingPacketInformation();
		System.Net.Internals.SocketAddress receiveAddress;
		int bytesTransferred;
		SocketError socketError = SocketPal.ReceiveMessageFrom(this, _handle, buffer, ref socketFlags, socketAddress, out receiveAddress, out ipPacketInformation, out bytesTransferred);
		UpdateReceiveSocketErrorForDisposed(ref socketError, bytesTransferred);
		if (socketError != 0 && socketError != SocketError.MessageSize)
		{
			UpdateStatusAfterSocketErrorAndThrowException(socketError, "ReceiveMessageFrom");
		}
		else if (SocketsTelemetry.Log.IsEnabled())
		{
			SocketsTelemetry.Log.BytesReceived(bytesTransferred);
			if (socketError == SocketError.Success && SocketType == SocketType.Dgram)
			{
				SocketsTelemetry.Log.DatagramReceived();
			}
		}
		if (!socketAddress2.Equals(receiveAddress))
		{
			try
			{
				remoteEP = remoteEP2.Create(receiveAddress);
			}
			catch
			{
			}
			if (_rightEndPoint == null)
			{
				_rightEndPoint = remoteEP2;
			}
		}
		if (System.Net.NetEventSource.Log.IsEnabled())
		{
			System.Net.NetEventSource.Error(this, socketError, "ReceiveMessageFrom");
		}
		return bytesTransferred;
	}

	public int ReceiveFrom(byte[] buffer, int offset, int size, SocketFlags socketFlags, ref EndPoint remoteEP)
	{
		ThrowIfDisposed();
		ValidateBufferArguments(buffer, offset, size);
		ValidateReceiveFromEndpointAndState(remoteEP, "remoteEP");
		SocketPal.CheckDualModeReceiveSupport(this);
		ValidateBlockingMode();
		if (System.Net.NetEventSource.Log.IsEnabled())
		{
			System.Net.NetEventSource.Info(this, $"SRC{LocalEndPoint} size:{size} remoteEP:{remoteEP}", "ReceiveFrom");
		}
		EndPoint remoteEP2 = remoteEP;
		System.Net.Internals.SocketAddress socketAddress = Serialize(ref remoteEP2);
		System.Net.Internals.SocketAddress socketAddress2 = IPEndPointExtensions.Serialize(remoteEP2);
		int bytesTransferred;
		SocketError socketError = SocketPal.ReceiveFrom(_handle, buffer, offset, size, socketFlags, socketAddress.Buffer, ref socketAddress.InternalSize, out bytesTransferred);
		UpdateReceiveSocketErrorForDisposed(ref socketError, bytesTransferred);
		SocketException ex = null;
		if (socketError != 0)
		{
			ex = new SocketException((int)socketError);
			UpdateStatusAfterSocketError(ex);
			if (System.Net.NetEventSource.Log.IsEnabled())
			{
				System.Net.NetEventSource.Error(this, ex, "ReceiveFrom");
			}
			if (ex.SocketErrorCode != SocketError.MessageSize)
			{
				throw ex;
			}
		}
		else if (SocketsTelemetry.Log.IsEnabled())
		{
			SocketsTelemetry.Log.BytesReceived(bytesTransferred);
			if (SocketType == SocketType.Dgram)
			{
				SocketsTelemetry.Log.DatagramReceived();
			}
		}
		if (!socketAddress2.Equals(socketAddress))
		{
			try
			{
				remoteEP = remoteEP2.Create(socketAddress);
			}
			catch
			{
			}
			if (_rightEndPoint == null)
			{
				_rightEndPoint = remoteEP2;
			}
		}
		if (ex != null)
		{
			throw ex;
		}
		if (System.Net.NetEventSource.Log.IsEnabled())
		{
			System.Net.NetEventSource.DumpBuffer(this, buffer, offset, size, "ReceiveFrom");
		}
		return bytesTransferred;
	}

	public int ReceiveFrom(byte[] buffer, int size, SocketFlags socketFlags, ref EndPoint remoteEP)
	{
		return ReceiveFrom(buffer, 0, size, socketFlags, ref remoteEP);
	}

	public int ReceiveFrom(byte[] buffer, SocketFlags socketFlags, ref EndPoint remoteEP)
	{
		return ReceiveFrom(buffer, 0, (buffer != null) ? buffer.Length : 0, socketFlags, ref remoteEP);
	}

	public int ReceiveFrom(byte[] buffer, ref EndPoint remoteEP)
	{
		return ReceiveFrom(buffer, 0, (buffer != null) ? buffer.Length : 0, SocketFlags.None, ref remoteEP);
	}

	public int ReceiveFrom(Span<byte> buffer, ref EndPoint remoteEP)
	{
		return ReceiveFrom(buffer, SocketFlags.None, ref remoteEP);
	}

	public int ReceiveFrom(Span<byte> buffer, SocketFlags socketFlags, ref EndPoint remoteEP)
	{
		ThrowIfDisposed();
		ValidateReceiveFromEndpointAndState(remoteEP, "remoteEP");
		SocketPal.CheckDualModeReceiveSupport(this);
		ValidateBlockingMode();
		EndPoint remoteEP2 = remoteEP;
		System.Net.Internals.SocketAddress socketAddress = Serialize(ref remoteEP2);
		System.Net.Internals.SocketAddress socketAddress2 = IPEndPointExtensions.Serialize(remoteEP2);
		int bytesTransferred;
		SocketError socketError = SocketPal.ReceiveFrom(_handle, buffer, socketFlags, socketAddress.Buffer, ref socketAddress.InternalSize, out bytesTransferred);
		UpdateReceiveSocketErrorForDisposed(ref socketError, bytesTransferred);
		SocketException ex = null;
		if (socketError != 0)
		{
			ex = new SocketException((int)socketError);
			UpdateStatusAfterSocketError(ex);
			if (System.Net.NetEventSource.Log.IsEnabled())
			{
				System.Net.NetEventSource.Error(this, ex, "ReceiveFrom");
			}
			if (ex.SocketErrorCode != SocketError.MessageSize)
			{
				throw ex;
			}
		}
		else if (SocketsTelemetry.Log.IsEnabled())
		{
			SocketsTelemetry.Log.BytesReceived(bytesTransferred);
			if (SocketType == SocketType.Dgram)
			{
				SocketsTelemetry.Log.DatagramReceived();
			}
		}
		if (!socketAddress2.Equals(socketAddress))
		{
			try
			{
				remoteEP = remoteEP2.Create(socketAddress);
			}
			catch
			{
			}
			if (_rightEndPoint == null)
			{
				_rightEndPoint = remoteEP2;
			}
		}
		if (ex != null)
		{
			throw ex;
		}
		return bytesTransferred;
	}

	public int IOControl(int ioControlCode, byte[]? optionInValue, byte[]? optionOutValue)
	{
		ThrowIfDisposed();
		int optionLength = 0;
		SocketError socketError = SocketPal.WindowsIoctl(_handle, ioControlCode, optionInValue, optionOutValue, out optionLength);
		if (System.Net.NetEventSource.Log.IsEnabled())
		{
			System.Net.NetEventSource.Info(this, $"WindowsIoctl returns errorCode:{socketError}", "IOControl");
		}
		if (socketError != 0)
		{
			UpdateStatusAfterSocketErrorAndThrowException(socketError, "IOControl");
		}
		return optionLength;
	}

	public int IOControl(IOControlCode ioControlCode, byte[]? optionInValue, byte[]? optionOutValue)
	{
		return IOControl((int)ioControlCode, optionInValue, optionOutValue);
	}

	public void SetSocketOption(SocketOptionLevel optionLevel, SocketOptionName optionName, int optionValue)
	{
		ThrowIfDisposed();
		if (System.Net.NetEventSource.Log.IsEnabled())
		{
			System.Net.NetEventSource.Info(this, $"optionLevel:{optionLevel} optionName:{optionName} optionValue:{optionValue}", "SetSocketOption");
		}
		SetSocketOption(optionLevel, optionName, optionValue, silent: false);
	}

	public void SetSocketOption(SocketOptionLevel optionLevel, SocketOptionName optionName, byte[] optionValue)
	{
		ThrowIfDisposed();
		if (System.Net.NetEventSource.Log.IsEnabled())
		{
			System.Net.NetEventSource.Info(this, $"optionLevel:{optionLevel} optionName:{optionName} optionValue:{optionValue}", "SetSocketOption");
		}
		SocketError socketError = SocketPal.SetSockOpt(_handle, optionLevel, optionName, optionValue);
		if (System.Net.NetEventSource.Log.IsEnabled())
		{
			System.Net.NetEventSource.Info(this, $"SetSockOpt returns errorCode:{socketError}", "SetSocketOption");
		}
		if (socketError != 0)
		{
			UpdateStatusAfterSocketErrorAndThrowException(socketError, "SetSocketOption");
		}
	}

	public void SetSocketOption(SocketOptionLevel optionLevel, SocketOptionName optionName, bool optionValue)
	{
		SetSocketOption(optionLevel, optionName, optionValue ? 1 : 0);
	}

	public void SetSocketOption(SocketOptionLevel optionLevel, SocketOptionName optionName, object optionValue)
	{
		ThrowIfDisposed();
		if (optionValue == null)
		{
			throw new ArgumentNullException("optionValue");
		}
		if (System.Net.NetEventSource.Log.IsEnabled())
		{
			System.Net.NetEventSource.Info(this, $"optionLevel:{optionLevel} optionName:{optionName} optionValue:{optionValue}", "SetSocketOption");
		}
		if (optionLevel == SocketOptionLevel.Socket && optionName == SocketOptionName.Linger)
		{
			if (!(optionValue is LingerOption lingerOption))
			{
				throw new ArgumentException(System.SR.Format(System.SR.net_sockets_invalid_optionValue, "LingerOption"), "optionValue");
			}
			if (lingerOption.LingerTime < 0 || lingerOption.LingerTime > 65535)
			{
				throw new ArgumentException(System.SR.Format(System.SR.ArgumentOutOfRange_Bounds_Lower_Upper_Named, 0, 65535, "optionValue.LingerTime"), "optionValue");
			}
			SetLingerOption(lingerOption);
		}
		else if (optionLevel == SocketOptionLevel.IP && (optionName == SocketOptionName.AddMembership || optionName == SocketOptionName.DropMembership))
		{
			if (!(optionValue is MulticastOption mR))
			{
				throw new ArgumentException(System.SR.Format(System.SR.net_sockets_invalid_optionValue, "MulticastOption"), "optionValue");
			}
			SetMulticastOption(optionName, mR);
		}
		else
		{
			if (optionLevel != SocketOptionLevel.IPv6 || (optionName != SocketOptionName.AddMembership && optionName != SocketOptionName.DropMembership))
			{
				throw new ArgumentException(System.SR.net_sockets_invalid_optionValue_all, "optionValue");
			}
			if (!(optionValue is IPv6MulticastOption mR2))
			{
				throw new ArgumentException(System.SR.Format(System.SR.net_sockets_invalid_optionValue, "IPv6MulticastOption"), "optionValue");
			}
			SetIPv6MulticastOption(optionName, mR2);
		}
	}

	public void SetRawSocketOption(int optionLevel, int optionName, ReadOnlySpan<byte> optionValue)
	{
		ThrowIfDisposed();
		SocketError socketError = SocketPal.SetRawSockOpt(_handle, optionLevel, optionName, optionValue);
		if (System.Net.NetEventSource.Log.IsEnabled())
		{
			System.Net.NetEventSource.Info(this, $"SetSockOpt optionLevel:{optionLevel} optionName:{optionName} returns errorCode:{socketError}", "SetRawSocketOption");
		}
		if (socketError != 0)
		{
			UpdateStatusAfterSocketErrorAndThrowException(socketError, "SetRawSocketOption");
		}
	}

	public object? GetSocketOption(SocketOptionLevel optionLevel, SocketOptionName optionName)
	{
		ThrowIfDisposed();
		if (optionLevel == SocketOptionLevel.Socket && optionName == SocketOptionName.Linger)
		{
			return GetLingerOpt();
		}
		if (optionLevel == SocketOptionLevel.IP && (optionName == SocketOptionName.AddMembership || optionName == SocketOptionName.DropMembership))
		{
			return GetMulticastOpt(optionName);
		}
		if (optionLevel == SocketOptionLevel.IPv6 && (optionName == SocketOptionName.AddMembership || optionName == SocketOptionName.DropMembership))
		{
			return GetIPv6MulticastOpt(optionName);
		}
		int optionValue = 0;
		SocketError sockOpt = SocketPal.GetSockOpt(_handle, optionLevel, optionName, out optionValue);
		if (System.Net.NetEventSource.Log.IsEnabled())
		{
			System.Net.NetEventSource.Info(this, $"GetSockOpt returns errorCode:{sockOpt}", "GetSocketOption");
		}
		if (sockOpt != 0)
		{
			UpdateStatusAfterSocketErrorAndThrowException(sockOpt, "GetSocketOption");
		}
		return optionValue;
	}

	public void GetSocketOption(SocketOptionLevel optionLevel, SocketOptionName optionName, byte[] optionValue)
	{
		ThrowIfDisposed();
		int optionLength = ((optionValue != null) ? optionValue.Length : 0);
		SocketError sockOpt = SocketPal.GetSockOpt(_handle, optionLevel, optionName, optionValue, ref optionLength);
		if (System.Net.NetEventSource.Log.IsEnabled())
		{
			System.Net.NetEventSource.Info(this, $"GetSockOpt returns errorCode:{sockOpt}", "GetSocketOption");
		}
		if (sockOpt != 0)
		{
			UpdateStatusAfterSocketErrorAndThrowException(sockOpt, "GetSocketOption");
		}
	}

	public byte[] GetSocketOption(SocketOptionLevel optionLevel, SocketOptionName optionName, int optionLength)
	{
		ThrowIfDisposed();
		byte[] array = new byte[optionLength];
		int optionLength2 = optionLength;
		SocketError sockOpt = SocketPal.GetSockOpt(_handle, optionLevel, optionName, array, ref optionLength2);
		if (System.Net.NetEventSource.Log.IsEnabled())
		{
			System.Net.NetEventSource.Info(this, $"GetSockOpt returns errorCode:{sockOpt}", "GetSocketOption");
		}
		if (sockOpt != 0)
		{
			UpdateStatusAfterSocketErrorAndThrowException(sockOpt, "GetSocketOption");
		}
		if (optionLength != optionLength2)
		{
			byte[] array2 = new byte[optionLength2];
			Buffer.BlockCopy(array, 0, array2, 0, optionLength2);
			array = array2;
		}
		return array;
	}

	public int GetRawSocketOption(int optionLevel, int optionName, Span<byte> optionValue)
	{
		ThrowIfDisposed();
		int optionLength = optionValue.Length;
		SocketError rawSockOpt = SocketPal.GetRawSockOpt(_handle, optionLevel, optionName, optionValue, ref optionLength);
		if (System.Net.NetEventSource.Log.IsEnabled())
		{
			System.Net.NetEventSource.Info(this, $"GetRawSockOpt optionLevel:{optionLevel} optionName:{optionName} returned errorCode:{rawSockOpt}", "GetRawSocketOption");
		}
		if (rawSockOpt != 0)
		{
			UpdateStatusAfterSocketErrorAndThrowException(rawSockOpt, "GetRawSocketOption");
		}
		return optionLength;
	}

	[SupportedOSPlatform("windows")]
	public void SetIPProtectionLevel(IPProtectionLevel level)
	{
		if (level == IPProtectionLevel.Unspecified)
		{
			throw new ArgumentException(System.SR.net_sockets_invalid_optionValue_all, "level");
		}
		if (_addressFamily == AddressFamily.InterNetworkV6)
		{
			SocketPal.SetIPProtectionLevel(this, SocketOptionLevel.IPv6, (int)level);
			return;
		}
		if (_addressFamily == AddressFamily.InterNetwork)
		{
			SocketPal.SetIPProtectionLevel(this, SocketOptionLevel.IP, (int)level);
			return;
		}
		throw new NotSupportedException(System.SR.net_invalidversion);
	}

	public bool Poll(int microSeconds, SelectMode mode)
	{
		ThrowIfDisposed();
		bool status;
		SocketError socketError = SocketPal.Poll(_handle, microSeconds, mode, out status);
		if (System.Net.NetEventSource.Log.IsEnabled())
		{
			System.Net.NetEventSource.Info(this, $"Poll returns socketCount:{(int)socketError}", "Poll");
		}
		if (socketError != 0)
		{
			UpdateStatusAfterSocketErrorAndThrowException(socketError, "Poll");
		}
		return status;
	}

	public static void Select(IList? checkRead, IList? checkWrite, IList? checkError, int microSeconds)
	{
		if ((checkRead == null || checkRead.Count == 0) && (checkWrite == null || checkWrite.Count == 0) && (checkError == null || checkError.Count == 0))
		{
			throw new ArgumentNullException(null, System.SR.net_sockets_empty_select);
		}
		if (checkRead != null && checkRead.Count > 65536)
		{
			throw new ArgumentOutOfRangeException("checkRead", System.SR.Format(System.SR.net_sockets_toolarge_select, "checkRead", 65536.ToString()));
		}
		if (checkWrite != null && checkWrite.Count > 65536)
		{
			throw new ArgumentOutOfRangeException("checkWrite", System.SR.Format(System.SR.net_sockets_toolarge_select, "checkWrite", 65536.ToString()));
		}
		if (checkError != null && checkError.Count > 65536)
		{
			throw new ArgumentOutOfRangeException("checkError", System.SR.Format(System.SR.net_sockets_toolarge_select, "checkError", 65536.ToString()));
		}
		SocketError socketError = SocketPal.Select(checkRead, checkWrite, checkError, microSeconds);
		if (socketError != 0)
		{
			throw new SocketException((int)socketError);
		}
	}

	public IAsyncResult BeginConnect(EndPoint remoteEP, AsyncCallback? callback, object? state)
	{
		return System.Threading.Tasks.TaskToApm.Begin(ConnectAsync(remoteEP), callback, state);
	}

	public IAsyncResult BeginConnect(string host, int port, AsyncCallback? requestCallback, object? state)
	{
		return System.Threading.Tasks.TaskToApm.Begin(ConnectAsync(host, port), requestCallback, state);
	}

	public IAsyncResult BeginConnect(IPAddress address, int port, AsyncCallback? requestCallback, object? state)
	{
		return System.Threading.Tasks.TaskToApm.Begin(ConnectAsync(address, port), requestCallback, state);
	}

	public IAsyncResult BeginConnect(IPAddress[] addresses, int port, AsyncCallback? requestCallback, object? state)
	{
		return System.Threading.Tasks.TaskToApm.Begin(ConnectAsync(addresses, port), requestCallback, state);
	}

	public void EndConnect(IAsyncResult asyncResult)
	{
		ThrowIfDisposed();
		System.Threading.Tasks.TaskToApm.End(asyncResult);
	}

	public IAsyncResult BeginDisconnect(bool reuseSocket, AsyncCallback? callback, object? state)
	{
		return System.Threading.Tasks.TaskToApm.Begin(DisconnectAsync(reuseSocket).AsTask(), callback, state);
	}

	public void Disconnect(bool reuseSocket)
	{
		ThrowIfDisposed();
		SocketError socketError = SocketError.Success;
		socketError = SocketPal.Disconnect(this, _handle, reuseSocket);
		if (System.Net.NetEventSource.Log.IsEnabled())
		{
			System.Net.NetEventSource.Info(this, $"UnsafeNclNativeMethods.OSSOCK.DisConnectEx returns:{socketError}", "Disconnect");
		}
		if (socketError != 0)
		{
			UpdateStatusAfterSocketErrorAndThrowException(socketError, "Disconnect");
		}
		SetToDisconnected();
		_remoteEndPoint = null;
		_localEndPoint = null;
	}

	public void EndDisconnect(IAsyncResult asyncResult)
	{
		ThrowIfDisposed();
		System.Threading.Tasks.TaskToApm.End(asyncResult);
	}

	public IAsyncResult BeginSend(byte[] buffer, int offset, int size, SocketFlags socketFlags, AsyncCallback? callback, object? state)
	{
		ThrowIfDisposed();
		ValidateBufferArguments(buffer, offset, size);
		return System.Threading.Tasks.TaskToApm.Begin(SendAsync(new ReadOnlyMemory<byte>(buffer, offset, size), socketFlags, default(CancellationToken)).AsTask(), callback, state);
	}

	public IAsyncResult? BeginSend(byte[] buffer, int offset, int size, SocketFlags socketFlags, out SocketError errorCode, AsyncCallback? callback, object? state)
	{
		ThrowIfDisposed();
		ValidateBufferArguments(buffer, offset, size);
		Task<int> task = SendAsync(new ReadOnlyMemory<byte>(buffer, offset, size), socketFlags, default(CancellationToken)).AsTask();
		if (task.IsFaulted || task.IsCanceled)
		{
			errorCode = GetSocketErrorFromFaultedTask(task);
			return null;
		}
		errorCode = SocketError.Success;
		return System.Threading.Tasks.TaskToApm.Begin(task, callback, state);
	}

	public IAsyncResult BeginSend(IList<ArraySegment<byte>> buffers, SocketFlags socketFlags, AsyncCallback? callback, object? state)
	{
		ThrowIfDisposed();
		return System.Threading.Tasks.TaskToApm.Begin(SendAsync(buffers, socketFlags), callback, state);
	}

	public IAsyncResult? BeginSend(IList<ArraySegment<byte>> buffers, SocketFlags socketFlags, out SocketError errorCode, AsyncCallback? callback, object? state)
	{
		ThrowIfDisposed();
		Task<int> task = SendAsync(buffers, socketFlags);
		if (task.IsFaulted || task.IsCanceled)
		{
			errorCode = GetSocketErrorFromFaultedTask(task);
			return null;
		}
		errorCode = SocketError.Success;
		return System.Threading.Tasks.TaskToApm.Begin(task, callback, state);
	}

	public int EndSend(IAsyncResult asyncResult)
	{
		ThrowIfDisposed();
		return System.Threading.Tasks.TaskToApm.End<int>(asyncResult);
	}

	public int EndSend(IAsyncResult asyncResult, out SocketError errorCode)
	{
		return EndSendReceive(asyncResult, out errorCode);
	}

	public IAsyncResult BeginSendFile(string? fileName, AsyncCallback? callback, object? state)
	{
		return BeginSendFile(fileName, null, null, TransmitFileOptions.UseDefaultWorkerThread, callback, state);
	}

	public IAsyncResult BeginSendFile(string? fileName, byte[]? preBuffer, byte[]? postBuffer, TransmitFileOptions flags, AsyncCallback? callback, object? state)
	{
		ThrowIfDisposed();
		if (!Connected)
		{
			throw new NotSupportedException(System.SR.net_notconnected);
		}
		if (System.Net.NetEventSource.Log.IsEnabled())
		{
			System.Net.NetEventSource.Info(this, $"::DoBeginSendFile() SRC:{LocalEndPoint} DST:{RemoteEndPoint} fileName:{fileName}", "BeginSendFile");
		}
		return System.Threading.Tasks.TaskToApm.Begin(SendFileAsync(fileName, preBuffer, postBuffer, flags).AsTask(), callback, state);
	}

	public void EndSendFile(IAsyncResult asyncResult)
	{
		ThrowIfDisposed();
		if (asyncResult == null)
		{
			throw new ArgumentNullException("asyncResult");
		}
		System.Threading.Tasks.TaskToApm.End(asyncResult);
	}

	public IAsyncResult BeginSendTo(byte[] buffer, int offset, int size, SocketFlags socketFlags, EndPoint remoteEP, AsyncCallback? callback, object? state)
	{
		ThrowIfDisposed();
		ValidateBufferArguments(buffer, offset, size);
		if (remoteEP == null)
		{
			throw new ArgumentNullException("remoteEP");
		}
		Task<int> task = SendToAsync(buffer.AsMemory(offset, size), socketFlags, remoteEP).AsTask();
		return System.Threading.Tasks.TaskToApm.Begin(task, callback, state);
	}

	public int EndSendTo(IAsyncResult asyncResult)
	{
		ThrowIfDisposed();
		return System.Threading.Tasks.TaskToApm.End<int>(asyncResult);
	}

	public IAsyncResult BeginReceive(byte[] buffer, int offset, int size, SocketFlags socketFlags, AsyncCallback? callback, object? state)
	{
		ThrowIfDisposed();
		ValidateBufferArguments(buffer, offset, size);
		return System.Threading.Tasks.TaskToApm.Begin(ReceiveAsync(new ArraySegment<byte>(buffer, offset, size), socketFlags, fromNetworkStream: false, default(CancellationToken)).AsTask(), callback, state);
	}

	public IAsyncResult? BeginReceive(byte[] buffer, int offset, int size, SocketFlags socketFlags, out SocketError errorCode, AsyncCallback? callback, object? state)
	{
		ThrowIfDisposed();
		ValidateBufferArguments(buffer, offset, size);
		Task<int> task = ReceiveAsync(new ArraySegment<byte>(buffer, offset, size), socketFlags, fromNetworkStream: false, default(CancellationToken)).AsTask();
		if (task.IsFaulted || task.IsCanceled)
		{
			errorCode = GetSocketErrorFromFaultedTask(task);
			return null;
		}
		errorCode = SocketError.Success;
		return System.Threading.Tasks.TaskToApm.Begin(task, callback, state);
	}

	public IAsyncResult BeginReceive(IList<ArraySegment<byte>> buffers, SocketFlags socketFlags, AsyncCallback? callback, object? state)
	{
		ThrowIfDisposed();
		return System.Threading.Tasks.TaskToApm.Begin(ReceiveAsync(buffers, socketFlags), callback, state);
	}

	public IAsyncResult? BeginReceive(IList<ArraySegment<byte>> buffers, SocketFlags socketFlags, out SocketError errorCode, AsyncCallback? callback, object? state)
	{
		ThrowIfDisposed();
		Task<int> task = ReceiveAsync(buffers, socketFlags);
		if (task.IsFaulted || task.IsCanceled)
		{
			errorCode = GetSocketErrorFromFaultedTask(task);
			return null;
		}
		errorCode = SocketError.Success;
		return System.Threading.Tasks.TaskToApm.Begin(task, callback, state);
	}

	public int EndReceive(IAsyncResult asyncResult)
	{
		ThrowIfDisposed();
		return System.Threading.Tasks.TaskToApm.End<int>(asyncResult);
	}

	public int EndReceive(IAsyncResult asyncResult, out SocketError errorCode)
	{
		return EndSendReceive(asyncResult, out errorCode);
	}

	private int EndSendReceive(IAsyncResult asyncResult, out SocketError errorCode)
	{
		ThrowIfDisposed();
		if (!(System.Threading.Tasks.TaskToApm.GetTask(asyncResult) is Task<int> task))
		{
			throw new ArgumentException(null, "asyncResult");
		}
		if (!task.IsCompleted)
		{
			((IAsyncResult)task).AsyncWaitHandle.WaitOne();
		}
		if (task.IsCompletedSuccessfully)
		{
			errorCode = SocketError.Success;
			return task.Result;
		}
		errorCode = GetSocketErrorFromFaultedTask(task);
		return 0;
	}

	public IAsyncResult BeginReceiveMessageFrom(byte[] buffer, int offset, int size, SocketFlags socketFlags, ref EndPoint remoteEP, AsyncCallback? callback, object? state)
	{
		if (System.Net.NetEventSource.Log.IsEnabled())
		{
			System.Net.NetEventSource.Info(this, $"size:{size}", "BeginReceiveMessageFrom");
		}
		ThrowIfDisposed();
		ValidateBufferArguments(buffer, offset, size);
		ValidateReceiveFromEndpointAndState(remoteEP, "remoteEP");
		Task<SocketReceiveMessageFromResult> task = ReceiveMessageFromAsync(buffer.AsMemory(offset, size), socketFlags, remoteEP).AsTask();
		if (task.IsCompletedSuccessfully)
		{
			EndPoint remoteEndPoint = task.Result.RemoteEndPoint;
			if (!remoteEP.Equals(remoteEndPoint))
			{
				remoteEP = remoteEndPoint;
			}
		}
		IAsyncResult asyncResult = System.Threading.Tasks.TaskToApm.Begin(task, callback, state);
		if (System.Net.NetEventSource.Log.IsEnabled())
		{
			System.Net.NetEventSource.Info(this, $"size:{size} returning AsyncResult:{asyncResult}", "BeginReceiveMessageFrom");
		}
		return asyncResult;
	}

	public int EndReceiveMessageFrom(IAsyncResult asyncResult, ref SocketFlags socketFlags, ref EndPoint endPoint, out IPPacketInformation ipPacketInformation)
	{
		ThrowIfDisposed();
		if (endPoint == null)
		{
			throw new ArgumentNullException("endPoint");
		}
		if (!CanTryAddressFamily(endPoint.AddressFamily))
		{
			throw new ArgumentException(System.SR.Format(System.SR.net_InvalidEndPointAddressFamily, endPoint.AddressFamily, _addressFamily), "endPoint");
		}
		SocketReceiveMessageFromResult socketReceiveMessageFromResult = System.Threading.Tasks.TaskToApm.End<SocketReceiveMessageFromResult>(asyncResult);
		if (!endPoint.Equals(socketReceiveMessageFromResult.RemoteEndPoint))
		{
			endPoint = socketReceiveMessageFromResult.RemoteEndPoint;
		}
		socketFlags = socketReceiveMessageFromResult.SocketFlags;
		ipPacketInformation = socketReceiveMessageFromResult.PacketInformation;
		return socketReceiveMessageFromResult.ReceivedBytes;
	}

	public IAsyncResult BeginReceiveFrom(byte[] buffer, int offset, int size, SocketFlags socketFlags, ref EndPoint remoteEP, AsyncCallback? callback, object? state)
	{
		ThrowIfDisposed();
		ValidateBufferArguments(buffer, offset, size);
		ValidateReceiveFromEndpointAndState(remoteEP, "remoteEP");
		Task<SocketReceiveFromResult> task = ReceiveFromAsync(buffer.AsMemory(offset, size), socketFlags, remoteEP).AsTask();
		if (task.IsCompletedSuccessfully)
		{
			EndPoint remoteEndPoint = task.Result.RemoteEndPoint;
			if (!remoteEP.Equals(remoteEndPoint))
			{
				remoteEP = remoteEndPoint;
			}
		}
		return System.Threading.Tasks.TaskToApm.Begin(task, callback, state);
	}

	public int EndReceiveFrom(IAsyncResult asyncResult, ref EndPoint endPoint)
	{
		ThrowIfDisposed();
		if (endPoint == null)
		{
			throw new ArgumentNullException("endPoint");
		}
		if (!CanTryAddressFamily(endPoint.AddressFamily))
		{
			throw new ArgumentException(System.SR.Format(System.SR.net_InvalidEndPointAddressFamily, endPoint.AddressFamily, _addressFamily), "endPoint");
		}
		SocketReceiveFromResult socketReceiveFromResult = System.Threading.Tasks.TaskToApm.End<SocketReceiveFromResult>(asyncResult);
		if (!endPoint.Equals(socketReceiveFromResult.RemoteEndPoint))
		{
			endPoint = socketReceiveFromResult.RemoteEndPoint;
		}
		return socketReceiveFromResult.ReceivedBytes;
	}

	public IAsyncResult BeginAccept(AsyncCallback? callback, object? state)
	{
		return System.Threading.Tasks.TaskToApm.Begin(AcceptAsync(), callback, state);
	}

	public Socket EndAccept(IAsyncResult asyncResult)
	{
		ThrowIfDisposed();
		return System.Threading.Tasks.TaskToApm.End<Socket>(asyncResult);
	}

	private async Task<(Socket s, byte[] buffer, int bytesReceived)> AcceptAndReceiveHelperAsync(Socket acceptSocket, int receiveSize)
	{
		if (receiveSize < 0)
		{
			throw new ArgumentOutOfRangeException("receiveSize");
		}
		Socket s = await AcceptAsync(acceptSocket).ConfigureAwait(continueOnCapturedContext: false);
		byte[] buffer;
		int item;
		if (receiveSize == 0)
		{
			buffer = Array.Empty<byte>();
			item = 0;
		}
		else
		{
			buffer = new byte[receiveSize];
			try
			{
				item = await s.ReceiveAsync(buffer, SocketFlags.None).ConfigureAwait(continueOnCapturedContext: false);
			}
			catch
			{
				s.Dispose();
				throw;
			}
		}
		return (s: s, buffer: buffer, bytesReceived: item);
	}

	public IAsyncResult BeginAccept(int receiveSize, AsyncCallback? callback, object? state)
	{
		return BeginAccept(null, receiveSize, callback, state);
	}

	public IAsyncResult BeginAccept(Socket? acceptSocket, int receiveSize, AsyncCallback? callback, object? state)
	{
		return System.Threading.Tasks.TaskToApm.Begin(AcceptAndReceiveHelperAsync(acceptSocket, receiveSize), callback, state);
	}

	public Socket EndAccept(out byte[] buffer, IAsyncResult asyncResult)
	{
		byte[] buffer2;
		int bytesTransferred;
		Socket result = EndAccept(out buffer2, out bytesTransferred, asyncResult);
		buffer = new byte[bytesTransferred];
		Buffer.BlockCopy(buffer2, 0, buffer, 0, bytesTransferred);
		return result;
	}

	public Socket EndAccept(out byte[] buffer, out int bytesTransferred, IAsyncResult asyncResult)
	{
		ThrowIfDisposed();
		Socket result;
		(result, buffer, bytesTransferred) = System.Threading.Tasks.TaskToApm.End<(Socket, byte[], int)>(asyncResult);
		return result;
	}

	public void Shutdown(SocketShutdown how)
	{
		ThrowIfDisposed();
		if (System.Net.NetEventSource.Log.IsEnabled())
		{
			System.Net.NetEventSource.Info(this, $"how:{how}", "Shutdown");
		}
		SocketError socketError = SocketPal.Shutdown(_handle, _isConnected, _isDisconnected, how);
		if (System.Net.NetEventSource.Log.IsEnabled())
		{
			System.Net.NetEventSource.Info(this, $"Shutdown returns errorCode:{socketError}", "Shutdown");
		}
		if (socketError != 0 && socketError != SocketError.NotSocket)
		{
			UpdateStatusAfterSocketErrorAndThrowException(socketError, "Shutdown");
		}
		SetToDisconnected();
		InternalSetBlocking(_willBlockInternal);
	}

	public bool AcceptAsync(SocketAsyncEventArgs e)
	{
		return AcceptAsync(e, CancellationToken.None);
	}

	private bool AcceptAsync(SocketAsyncEventArgs e, CancellationToken cancellationToken)
	{
		ThrowIfDisposed();
		if (e == null)
		{
			throw new ArgumentNullException("e");
		}
		if (e.HasMultipleBuffers)
		{
			throw new ArgumentException(System.SR.net_multibuffernotsupported, "e");
		}
		if (_rightEndPoint == null)
		{
			throw new InvalidOperationException(System.SR.net_sockets_mustbind);
		}
		if (!_isListening)
		{
			throw new InvalidOperationException(System.SR.net_sockets_mustlisten);
		}
		e.AcceptSocket = GetOrCreateAcceptSocket(e.AcceptSocket, checkDisconnected: true, "AcceptSocket", out var handle);
		if (SocketsTelemetry.Log.IsEnabled())
		{
			SocketsTelemetry.Log.AcceptStart(_rightEndPoint);
		}
		e.StartOperationCommon(this, SocketAsyncOperation.Accept);
		e.StartOperationAccept();
		SocketError socketError;
		try
		{
			socketError = e.DoOperationAccept(this, _handle, handle, cancellationToken);
		}
		catch (Exception ex)
		{
			if (SocketsTelemetry.Log.IsEnabled())
			{
				SocketsTelemetry.Log.AfterAccept(SocketError.Interrupted, ex.Message);
			}
			e.Complete();
			throw;
		}
		return socketError == SocketError.IOPending;
	}

	public bool ConnectAsync(SocketAsyncEventArgs e)
	{
		return ConnectAsync(e, userSocket: true, saeaCancelable: true);
	}

	internal bool ConnectAsync(SocketAsyncEventArgs e, bool userSocket, bool saeaCancelable)
	{
		ThrowIfDisposed();
		if (e == null)
		{
			throw new ArgumentNullException("e");
		}
		if (e.HasMultipleBuffers)
		{
			throw new ArgumentException(System.SR.net_multibuffernotsupported, "BufferList");
		}
		if (e.RemoteEndPoint == null)
		{
			throw new ArgumentNullException("remoteEP");
		}
		if (_isListening)
		{
			throw new InvalidOperationException(System.SR.net_sockets_mustnotlisten);
		}
		if (_isConnected)
		{
			throw new SocketException(10056);
		}
		EndPoint remoteEP = e.RemoteEndPoint;
		if (remoteEP is DnsEndPoint dnsEndPoint)
		{
			if (System.Net.NetEventSource.Log.IsEnabled())
			{
				System.Net.NetEventSource.ConnectedAsyncDns(this);
			}
			if (dnsEndPoint.AddressFamily != 0 && !CanTryAddressFamily(dnsEndPoint.AddressFamily))
			{
				throw new NotSupportedException(System.SR.net_invalidversion);
			}
			e.StartOperationCommon(this, SocketAsyncOperation.Connect);
			e.StartOperationConnect(saeaCancelable, userSocket);
			try
			{
				return e.DnsConnectAsync(dnsEndPoint, (SocketType)0, ProtocolType.IP);
			}
			catch
			{
				e.Complete();
				throw;
			}
		}
		if (!CanTryAddressFamily(e.RemoteEndPoint.AddressFamily))
		{
			throw new NotSupportedException(System.SR.net_invalidversion);
		}
		e._socketAddress = Serialize(ref remoteEP);
		_pendingConnectRightEndPoint = remoteEP;
		_nonBlockingConnectInProgress = false;
		WildcardBindForConnectIfNecessary(remoteEP.AddressFamily);
		if (SocketsTelemetry.Log.IsEnabled())
		{
			SocketsTelemetry.Log.ConnectStart(e._socketAddress);
		}
		e.StartOperationCommon(this, SocketAsyncOperation.Connect);
		e.StartOperationConnect(saeaMultiConnectCancelable: false, userSocket);
		try
		{
			SocketError socketError = ((_socketType == SocketType.Stream && remoteEP.AddressFamily != AddressFamily.Unix) ? e.DoOperationConnectEx(this, _handle) : e.DoOperationConnect(this, _handle));
			return socketError == SocketError.IOPending;
		}
		catch (Exception ex)
		{
			if (SocketsTelemetry.Log.IsEnabled())
			{
				SocketsTelemetry.Log.AfterConnect(SocketError.NotSocket, ex.Message);
			}
			_localEndPoint = null;
			e.Complete();
			throw;
		}
	}

	public static bool ConnectAsync(SocketType socketType, ProtocolType protocolType, SocketAsyncEventArgs e)
	{
		if (e == null)
		{
			throw new ArgumentNullException("e");
		}
		if (e.HasMultipleBuffers)
		{
			throw new ArgumentException(System.SR.net_multibuffernotsupported, "e");
		}
		if (e.RemoteEndPoint == null)
		{
			throw new ArgumentException(System.SR.Format(System.SR.InvalidNullArgument, "e.RemoteEndPoint"), "e");
		}
		EndPoint remoteEndPoint = e.RemoteEndPoint;
		if (remoteEndPoint is DnsEndPoint dnsEndPoint)
		{
			Socket socket = ((dnsEndPoint.AddressFamily != 0) ? new Socket(dnsEndPoint.AddressFamily, socketType, protocolType) : null);
			e.StartOperationCommon(socket, SocketAsyncOperation.Connect);
			e.StartOperationConnect(saeaMultiConnectCancelable: true, userSocket: false);
			try
			{
				return e.DnsConnectAsync(dnsEndPoint, socketType, protocolType);
			}
			catch
			{
				e.Complete();
				throw;
			}
		}
		Socket socket2 = new Socket(remoteEndPoint.AddressFamily, socketType, protocolType);
		return socket2.ConnectAsync(e, userSocket: false, saeaCancelable: true);
	}

	private void WildcardBindForConnectIfNecessary(AddressFamily addressFamily)
	{
		if (_rightEndPoint == null)
		{
			CachedSerializedEndPoint cachedSerializedEndPoint;
			switch (addressFamily)
			{
			default:
				return;
			case AddressFamily.InterNetwork:
				cachedSerializedEndPoint = (IsDualMode ? (s_cachedMappedAnyV6EndPoint ?? (s_cachedMappedAnyV6EndPoint = new CachedSerializedEndPoint(s_IPAddressAnyMapToIPv6))) : (s_cachedAnyEndPoint ?? (s_cachedAnyEndPoint = new CachedSerializedEndPoint(IPAddress.Any))));
				break;
			case AddressFamily.InterNetworkV6:
				cachedSerializedEndPoint = s_cachedAnyV6EndPoint ?? (s_cachedAnyV6EndPoint = new CachedSerializedEndPoint(IPAddress.IPv6Any));
				break;
			}
			if (System.Net.NetEventSource.Log.IsEnabled())
			{
				System.Net.NetEventSource.Info(this, cachedSerializedEndPoint.IPEndPoint, "WildcardBindForConnectIfNecessary");
			}
			if (_socketType == SocketType.Stream && _protocolType == ProtocolType.Tcp)
			{
				EnableReuseUnicastPort();
			}
			DoBind(cachedSerializedEndPoint.IPEndPoint, cachedSerializedEndPoint.SocketAddress);
		}
	}

	public static void CancelConnectAsync(SocketAsyncEventArgs e)
	{
		if (e == null)
		{
			throw new ArgumentNullException("e");
		}
		e.CancelConnectAsync();
	}

	public bool DisconnectAsync(SocketAsyncEventArgs e)
	{
		return DisconnectAsync(e, default(CancellationToken));
	}

	private bool DisconnectAsync(SocketAsyncEventArgs e, CancellationToken cancellationToken)
	{
		ThrowIfDisposed();
		if (e == null)
		{
			throw new ArgumentNullException("e");
		}
		e.StartOperationCommon(this, SocketAsyncOperation.Disconnect);
		SocketError socketError = SocketError.Success;
		try
		{
			socketError = e.DoOperationDisconnect(this, _handle, cancellationToken);
		}
		catch
		{
			e.Complete();
			throw;
		}
		return socketError == SocketError.IOPending;
	}

	public bool ReceiveAsync(SocketAsyncEventArgs e)
	{
		return ReceiveAsync(e, default(CancellationToken));
	}

	private bool ReceiveAsync(SocketAsyncEventArgs e, CancellationToken cancellationToken)
	{
		ThrowIfDisposed();
		if (e == null)
		{
			throw new ArgumentNullException("e");
		}
		e.StartOperationCommon(this, SocketAsyncOperation.Receive);
		SocketError socketError;
		try
		{
			socketError = e.DoOperationReceive(_handle, cancellationToken);
		}
		catch
		{
			e.Complete();
			throw;
		}
		return socketError == SocketError.IOPending;
	}

	public bool ReceiveFromAsync(SocketAsyncEventArgs e)
	{
		return ReceiveFromAsync(e, default(CancellationToken));
	}

	private bool ReceiveFromAsync(SocketAsyncEventArgs e, CancellationToken cancellationToken)
	{
		ThrowIfDisposed();
		if (e == null)
		{
			throw new ArgumentNullException("e");
		}
		if (e.RemoteEndPoint == null)
		{
			throw new ArgumentException(System.SR.Format(System.SR.InvalidNullArgument, "e.RemoteEndPoint"), "e");
		}
		if (!CanTryAddressFamily(e.RemoteEndPoint.AddressFamily))
		{
			throw new ArgumentException(System.SR.Format(System.SR.net_InvalidEndPointAddressFamily, e.RemoteEndPoint.AddressFamily, _addressFamily), "e");
		}
		SocketPal.CheckDualModeReceiveSupport(this);
		EndPoint remoteEP = e.RemoteEndPoint;
		e._socketAddress = Serialize(ref remoteEP);
		e.RemoteEndPoint = remoteEP;
		e.StartOperationCommon(this, SocketAsyncOperation.ReceiveFrom);
		SocketError socketError;
		try
		{
			socketError = e.DoOperationReceiveFrom(_handle, cancellationToken);
		}
		catch
		{
			e.Complete();
			throw;
		}
		return socketError == SocketError.IOPending;
	}

	public bool ReceiveMessageFromAsync(SocketAsyncEventArgs e)
	{
		return ReceiveMessageFromAsync(e, default(CancellationToken));
	}

	private bool ReceiveMessageFromAsync(SocketAsyncEventArgs e, CancellationToken cancellationToken)
	{
		ThrowIfDisposed();
		if (e == null)
		{
			throw new ArgumentNullException("e");
		}
		if (e.RemoteEndPoint == null)
		{
			throw new ArgumentException(System.SR.Format(System.SR.InvalidNullArgument, "e.RemoteEndPoint"), "e");
		}
		if (!CanTryAddressFamily(e.RemoteEndPoint.AddressFamily))
		{
			throw new ArgumentException(System.SR.Format(System.SR.net_InvalidEndPointAddressFamily, e.RemoteEndPoint.AddressFamily, _addressFamily), "e");
		}
		SocketPal.CheckDualModeReceiveSupport(this);
		EndPoint remoteEP = e.RemoteEndPoint;
		e._socketAddress = Serialize(ref remoteEP);
		e.RemoteEndPoint = remoteEP;
		SetReceivingPacketInformation();
		e.StartOperationCommon(this, SocketAsyncOperation.ReceiveMessageFrom);
		SocketError socketError;
		try
		{
			socketError = e.DoOperationReceiveMessageFrom(this, _handle, cancellationToken);
		}
		catch
		{
			e.Complete();
			throw;
		}
		return socketError == SocketError.IOPending;
	}

	public bool SendAsync(SocketAsyncEventArgs e)
	{
		return SendAsync(e, default(CancellationToken));
	}

	private bool SendAsync(SocketAsyncEventArgs e, CancellationToken cancellationToken)
	{
		ThrowIfDisposed();
		if (e == null)
		{
			throw new ArgumentNullException("e");
		}
		e.StartOperationCommon(this, SocketAsyncOperation.Send);
		SocketError socketError;
		try
		{
			socketError = e.DoOperationSend(_handle, cancellationToken);
		}
		catch
		{
			e.Complete();
			throw;
		}
		return socketError == SocketError.IOPending;
	}

	public bool SendPacketsAsync(SocketAsyncEventArgs e)
	{
		return SendPacketsAsync(e, default(CancellationToken));
	}

	private bool SendPacketsAsync(SocketAsyncEventArgs e, CancellationToken cancellationToken)
	{
		ThrowIfDisposed();
		if (e == null)
		{
			throw new ArgumentNullException("e");
		}
		if (e.SendPacketsElements == null)
		{
			throw new ArgumentException(System.SR.Format(System.SR.InvalidNullArgument, "e.SendPacketsElements"), "e");
		}
		if (!Connected)
		{
			throw new NotSupportedException(System.SR.net_notconnected);
		}
		e.StartOperationCommon(this, SocketAsyncOperation.SendPackets);
		SocketError socketError;
		try
		{
			socketError = e.DoOperationSendPackets(this, _handle, cancellationToken);
		}
		catch (Exception)
		{
			e.Complete();
			throw;
		}
		return socketError == SocketError.IOPending;
	}

	public bool SendToAsync(SocketAsyncEventArgs e)
	{
		return SendToAsync(e, default(CancellationToken));
	}

	private bool SendToAsync(SocketAsyncEventArgs e, CancellationToken cancellationToken)
	{
		ThrowIfDisposed();
		if (e == null)
		{
			throw new ArgumentNullException("e");
		}
		if (e.RemoteEndPoint == null)
		{
			throw new ArgumentException(System.SR.Format(System.SR.InvalidNullArgument, "e.RemoteEndPoint"), "e");
		}
		EndPoint remoteEP = e.RemoteEndPoint;
		e._socketAddress = Serialize(ref remoteEP);
		e.StartOperationCommon(this, SocketAsyncOperation.SendTo);
		EndPoint rightEndPoint = _rightEndPoint;
		if (_rightEndPoint == null)
		{
			_rightEndPoint = remoteEP;
		}
		SocketError socketError;
		try
		{
			socketError = e.DoOperationSendTo(_handle, cancellationToken);
		}
		catch
		{
			_rightEndPoint = rightEndPoint;
			_localEndPoint = null;
			e.Complete();
			throw;
		}
		if (!CheckErrorAndUpdateStatus(socketError))
		{
			_rightEndPoint = rightEndPoint;
			_localEndPoint = null;
		}
		return socketError == SocketError.IOPending;
	}

	internal static void GetIPProtocolInformation(AddressFamily addressFamily, System.Net.Internals.SocketAddress socketAddress, out bool isIPv4, out bool isIPv6)
	{
		bool flag = socketAddress.Family == AddressFamily.InterNetworkV6 && socketAddress.GetIPAddress().IsIPv4MappedToIPv6;
		isIPv4 = addressFamily == AddressFamily.InterNetwork || flag;
		isIPv6 = addressFamily == AddressFamily.InterNetworkV6;
	}

	internal static int GetAddressSize(EndPoint endPoint)
	{
		return endPoint.AddressFamily switch
		{
			AddressFamily.InterNetworkV6 => 28, 
			AddressFamily.InterNetwork => 16, 
			_ => endPoint.Serialize().Size, 
		};
	}

	private System.Net.Internals.SocketAddress Serialize(ref EndPoint remoteEP)
	{
		if (remoteEP is IPEndPoint iPEndPoint)
		{
			IPAddress address = iPEndPoint.Address;
			if (address.AddressFamily == AddressFamily.InterNetwork && IsDualMode)
			{
				address = address.MapToIPv6();
				remoteEP = new IPEndPoint(address, iPEndPoint.Port);
			}
		}
		else if (remoteEP is DnsEndPoint)
		{
			throw new ArgumentException(System.SR.Format(System.SR.net_sockets_invalid_dnsendpoint, "remoteEP"), "remoteEP");
		}
		return IPEndPointExtensions.Serialize(remoteEP);
	}

	private void DoConnect(EndPoint endPointSnapshot, System.Net.Internals.SocketAddress socketAddress)
	{
		if (SocketsTelemetry.Log.IsEnabled())
		{
			SocketsTelemetry.Log.ConnectStart(socketAddress);
		}
		SocketError socketError;
		try
		{
			socketError = SocketPal.Connect(_handle, socketAddress.Buffer, socketAddress.Size);
		}
		catch (Exception ex)
		{
			if (SocketsTelemetry.Log.IsEnabled())
			{
				SocketsTelemetry.Log.AfterConnect(SocketError.NotSocket, ex.Message);
			}
			throw;
		}
		if (socketError != 0)
		{
			UpdateConnectSocketErrorForDisposed(ref socketError);
			SocketException ex2 = System.Net.Internals.SocketExceptionFactory.CreateSocketException((int)socketError, endPointSnapshot);
			UpdateStatusAfterSocketError(ex2);
			if (System.Net.NetEventSource.Log.IsEnabled())
			{
				System.Net.NetEventSource.Error(this, ex2, "DoConnect");
			}
			if (SocketsTelemetry.Log.IsEnabled())
			{
				SocketsTelemetry.Log.AfterConnect(socketError);
			}
			throw ex2;
		}
		if (SocketsTelemetry.Log.IsEnabled())
		{
			SocketsTelemetry.Log.AfterConnect(SocketError.Success);
		}
		if (System.Net.NetEventSource.Log.IsEnabled())
		{
			System.Net.NetEventSource.Info(this, $"connection to:{endPointSnapshot}", "DoConnect");
		}
		_pendingConnectRightEndPoint = endPointSnapshot;
		_nonBlockingConnectInProgress = false;
		SetToConnected();
		if (System.Net.NetEventSource.Log.IsEnabled())
		{
			System.Net.NetEventSource.Connected(this, LocalEndPoint, RemoteEndPoint);
		}
	}

	protected virtual void Dispose(bool disposing)
	{
		if (System.Net.NetEventSource.Log.IsEnabled())
		{
			try
			{
				System.Net.NetEventSource.Info(this, $"disposing:{disposing} Disposed:{Disposed}", "Dispose");
			}
			catch (Exception exception) when (!ExceptionCheck.IsFatal(exception))
			{
			}
		}
		if (Interlocked.CompareExchange(ref _disposed, 1, 0) == 1)
		{
			return;
		}
		SetToDisconnected();
		SafeSocketHandle handle = _handle;
		if (handle != null && handle.OwnsHandle)
		{
			if (!disposing)
			{
				if (System.Net.NetEventSource.Log.IsEnabled())
				{
					System.Net.NetEventSource.Info(this, "Calling _handle.Dispose()", "Dispose");
				}
				handle.Dispose();
			}
			else
			{
				try
				{
					int closeTimeout = _closeTimeout;
					if (closeTimeout == 0)
					{
						if (System.Net.NetEventSource.Log.IsEnabled())
						{
							System.Net.NetEventSource.Info(this, "Calling _handle.CloseAsIs()", "Dispose");
						}
						handle.CloseAsIs(abortive: true);
					}
					else
					{
						if (!_willBlock || !_willBlockInternal)
						{
							bool willBlock;
							SocketError socketError = SocketPal.SetBlocking(handle, shouldBlock: false, out willBlock);
							if (System.Net.NetEventSource.Log.IsEnabled())
							{
								System.Net.NetEventSource.Info(this, $"handle:{handle} ioctlsocket(FIONBIO):{socketError}", "Dispose");
							}
						}
						if (closeTimeout < 0)
						{
							if (System.Net.NetEventSource.Log.IsEnabled())
							{
								System.Net.NetEventSource.Info(this, "Calling _handle.CloseAsIs()", "Dispose");
							}
							handle.CloseAsIs(abortive: false);
						}
						else
						{
							SocketError socketError = SocketPal.Shutdown(handle, _isConnected, _isDisconnected, SocketShutdown.Send);
							if (System.Net.NetEventSource.Log.IsEnabled())
							{
								System.Net.NetEventSource.Info(this, $"handle:{handle} shutdown():{socketError}", "Dispose");
							}
							socketError = SocketPal.SetSockOpt(handle, SocketOptionLevel.Socket, SocketOptionName.ReceiveTimeout, closeTimeout);
							if (System.Net.NetEventSource.Log.IsEnabled())
							{
								System.Net.NetEventSource.Info(this, $"handle:{handle} setsockopt():{socketError}", "Dispose");
							}
							if (socketError != 0)
							{
								handle.CloseAsIs(abortive: true);
							}
							else
							{
								socketError = SocketPal.Receive(handle, Array.Empty<byte>(), 0, 0, SocketFlags.None, out var _);
								if (System.Net.NetEventSource.Log.IsEnabled())
								{
									System.Net.NetEventSource.Info(this, $"handle:{handle} recv():{socketError}", "Dispose");
								}
								if (socketError != 0)
								{
									handle.CloseAsIs(abortive: true);
								}
								else
								{
									int available = 0;
									socketError = SocketPal.GetAvailable(handle, out available);
									if (System.Net.NetEventSource.Log.IsEnabled())
									{
										System.Net.NetEventSource.Info(this, $"handle:{handle} ioctlsocket(FIONREAD):{socketError}", "Dispose");
									}
									if (socketError != 0 || available != 0)
									{
										handle.CloseAsIs(abortive: true);
									}
									else
									{
										handle.CloseAsIs(abortive: false);
									}
								}
							}
						}
					}
				}
				catch (ObjectDisposedException)
				{
				}
			}
			if (_rightEndPoint is UnixDomainSocketEndPoint { BoundFileName: not null } unixDomainSocketEndPoint)
			{
				try
				{
					File.Delete(unixDomainSocketEndPoint.BoundFileName);
				}
				catch
				{
				}
			}
		}
		DisposeCachedTaskSocketAsyncEventArgs();
	}

	public void Dispose()
	{
		if (System.Net.NetEventSource.Log.IsEnabled())
		{
			System.Net.NetEventSource.Info(this, $"timeout = {_closeTimeout}", "Dispose");
		}
		Dispose(disposing: true);
		GC.SuppressFinalize(this);
	}

	~Socket()
	{
		Dispose(disposing: false);
	}

	internal void InternalShutdown(SocketShutdown how)
	{
		if (Disposed || _handle.IsInvalid)
		{
			return;
		}
		try
		{
			SocketPal.Shutdown(_handle, _isConnected, _isDisconnected, how);
		}
		catch (ObjectDisposedException)
		{
		}
	}

	internal void SetReceivingPacketInformation()
	{
		if (!_receivingPacketInformation)
		{
			IPAddress iPAddress = ((_rightEndPoint is IPEndPoint iPEndPoint) ? iPEndPoint.Address : null);
			if (_addressFamily == AddressFamily.InterNetwork)
			{
				SetSocketOption(SocketOptionLevel.IP, SocketOptionName.PacketInformation, optionValue: true);
			}
			if (iPAddress != null && IsDualMode && (iPAddress.IsIPv4MappedToIPv6 || iPAddress.Equals(IPAddress.IPv6Any)))
			{
				SocketPal.SetReceivingDualModeIPv4PacketInformation(this);
			}
			if (_addressFamily == AddressFamily.InterNetworkV6 && (iPAddress == null || !iPAddress.IsIPv4MappedToIPv6))
			{
				SetSocketOption(SocketOptionLevel.IPv6, SocketOptionName.PacketInformation, optionValue: true);
			}
			_receivingPacketInformation = true;
		}
	}

	internal void SetSocketOption(SocketOptionLevel optionLevel, SocketOptionName optionName, int optionValue, bool silent)
	{
		if (silent && (Disposed || _handle.IsInvalid))
		{
			if (System.Net.NetEventSource.Log.IsEnabled())
			{
				System.Net.NetEventSource.Info(this, "skipping the call", "SetSocketOption");
			}
			return;
		}
		SocketError socketError = SocketError.Success;
		try
		{
			socketError = SocketPal.SetSockOpt(_handle, optionLevel, optionName, optionValue);
			if (System.Net.NetEventSource.Log.IsEnabled())
			{
				System.Net.NetEventSource.Info(this, $"SetSockOpt returns errorCode:{socketError}", "SetSocketOption");
			}
		}
		catch
		{
			if (silent && _handle.IsInvalid)
			{
				return;
			}
			throw;
		}
		if (optionName == SocketOptionName.PacketInformation && optionValue == 0 && socketError == SocketError.Success)
		{
			_receivingPacketInformation = false;
		}
		if (!silent && socketError != 0)
		{
			UpdateStatusAfterSocketErrorAndThrowException(socketError, "SetSocketOption");
		}
	}

	private void SetMulticastOption(SocketOptionName optionName, MulticastOption MR)
	{
		SocketError socketError = SocketPal.SetMulticastOption(_handle, optionName, MR);
		if (System.Net.NetEventSource.Log.IsEnabled())
		{
			System.Net.NetEventSource.Info(this, $"SetMulticastOption returns errorCode:{socketError}", "SetMulticastOption");
		}
		if (socketError != 0)
		{
			UpdateStatusAfterSocketErrorAndThrowException(socketError, "SetMulticastOption");
		}
	}

	private void SetIPv6MulticastOption(SocketOptionName optionName, IPv6MulticastOption MR)
	{
		SocketError socketError = SocketPal.SetIPv6MulticastOption(_handle, optionName, MR);
		if (System.Net.NetEventSource.Log.IsEnabled())
		{
			System.Net.NetEventSource.Info(this, $"SetIPv6MulticastOption returns errorCode:{socketError}", "SetIPv6MulticastOption");
		}
		if (socketError != 0)
		{
			UpdateStatusAfterSocketErrorAndThrowException(socketError, "SetIPv6MulticastOption");
		}
	}

	private void SetLingerOption(LingerOption lref)
	{
		SocketError socketError = SocketPal.SetLingerOption(_handle, lref);
		if (System.Net.NetEventSource.Log.IsEnabled())
		{
			System.Net.NetEventSource.Info(this, $"SetLingerOption returns errorCode:{socketError}", "SetLingerOption");
		}
		if (socketError != 0)
		{
			UpdateStatusAfterSocketErrorAndThrowException(socketError, "SetLingerOption");
		}
	}

	private LingerOption GetLingerOpt()
	{
		LingerOption optionValue;
		SocketError lingerOption = SocketPal.GetLingerOption(_handle, out optionValue);
		if (System.Net.NetEventSource.Log.IsEnabled())
		{
			System.Net.NetEventSource.Info(this, $"GetLingerOption returns errorCode:{lingerOption}", "GetLingerOpt");
		}
		if (lingerOption != 0)
		{
			UpdateStatusAfterSocketErrorAndThrowException(lingerOption, "GetLingerOpt");
		}
		return optionValue;
	}

	private MulticastOption GetMulticastOpt(SocketOptionName optionName)
	{
		MulticastOption optionValue;
		SocketError multicastOption = SocketPal.GetMulticastOption(_handle, optionName, out optionValue);
		if (System.Net.NetEventSource.Log.IsEnabled())
		{
			System.Net.NetEventSource.Info(this, $"GetMulticastOption returns errorCode:{multicastOption}", "GetMulticastOpt");
		}
		if (multicastOption != 0)
		{
			UpdateStatusAfterSocketErrorAndThrowException(multicastOption, "GetMulticastOpt");
		}
		return optionValue;
	}

	private IPv6MulticastOption GetIPv6MulticastOpt(SocketOptionName optionName)
	{
		IPv6MulticastOption optionValue;
		SocketError iPv6MulticastOption = SocketPal.GetIPv6MulticastOption(_handle, optionName, out optionValue);
		if (System.Net.NetEventSource.Log.IsEnabled())
		{
			System.Net.NetEventSource.Info(this, $"GetIPv6MulticastOption returns errorCode:{iPv6MulticastOption}", "GetIPv6MulticastOpt");
		}
		if (iPv6MulticastOption != 0)
		{
			UpdateStatusAfterSocketErrorAndThrowException(iPv6MulticastOption, "GetIPv6MulticastOpt");
		}
		return optionValue;
	}

	private SocketError InternalSetBlocking(bool desired, out bool current)
	{
		if (Disposed)
		{
			current = _willBlock;
			return SocketError.Success;
		}
		bool willBlock = false;
		SocketError socketError;
		try
		{
			socketError = SocketPal.SetBlocking(_handle, desired, out willBlock);
		}
		catch (ObjectDisposedException)
		{
			socketError = SocketError.NotSocket;
		}
		if (System.Net.NetEventSource.Log.IsEnabled())
		{
			System.Net.NetEventSource.Info(this, $"SetBlocking returns errorCode:{socketError}", "InternalSetBlocking");
		}
		if (socketError == SocketError.Success)
		{
			_willBlockInternal = willBlock;
		}
		if (System.Net.NetEventSource.Log.IsEnabled())
		{
			System.Net.NetEventSource.Info(this, $"errorCode:{socketError} willBlock:{_willBlock} willBlockInternal:{_willBlockInternal}", "InternalSetBlocking");
		}
		current = _willBlockInternal;
		return socketError;
	}

	internal void InternalSetBlocking(bool desired)
	{
		InternalSetBlocking(desired, out var _);
	}

	internal Socket CreateAcceptSocket(SafeSocketHandle fd, EndPoint remoteEP)
	{
		Socket socket = new Socket(fd, loadPropertiesFromHandle: false);
		return UpdateAcceptSocket(socket, remoteEP);
	}

	internal Socket UpdateAcceptSocket(Socket socket, EndPoint remoteEP)
	{
		socket._addressFamily = _addressFamily;
		socket._socketType = _socketType;
		socket._protocolType = _protocolType;
		socket._remoteEndPoint = remoteEP;
		if (_rightEndPoint is UnixDomainSocketEndPoint { BoundFileName: not null } unixDomainSocketEndPoint)
		{
			socket._rightEndPoint = unixDomainSocketEndPoint.CreateUnboundEndPoint();
		}
		else
		{
			socket._rightEndPoint = _rightEndPoint;
		}
		socket._localEndPoint = ((!IsWildcardEndPoint(_localEndPoint)) ? _localEndPoint : null);
		socket.SetToConnected();
		socket._willBlock = _willBlock;
		socket.InternalSetBlocking(_willBlock);
		return socket;
	}

	internal void SetToConnected()
	{
		if (!_isConnected)
		{
			_isConnected = true;
			_isDisconnected = false;
			if (_rightEndPoint == null)
			{
				_rightEndPoint = _pendingConnectRightEndPoint;
			}
			_pendingConnectRightEndPoint = null;
			UpdateLocalEndPointOnConnect();
			if (System.Net.NetEventSource.Log.IsEnabled())
			{
				System.Net.NetEventSource.Info(this, "now connected", "SetToConnected");
			}
		}
	}

	private void UpdateLocalEndPointOnConnect()
	{
		if (IsWildcardEndPoint(_localEndPoint))
		{
			_localEndPoint = null;
		}
	}

	private bool IsWildcardEndPoint(EndPoint endPoint)
	{
		if (endPoint == null)
		{
			return false;
		}
		if (endPoint is IPEndPoint iPEndPoint)
		{
			IPAddress address = iPEndPoint.Address;
			if (!IPAddress.Any.Equals(address) && !IPAddress.IPv6Any.Equals(address))
			{
				return s_IPAddressAnyMapToIPv6.Equals(address);
			}
			return true;
		}
		return false;
	}

	internal void SetToDisconnected()
	{
		if (_isConnected)
		{
			_isConnected = false;
			_isDisconnected = true;
			if (!Disposed && System.Net.NetEventSource.Log.IsEnabled())
			{
				System.Net.NetEventSource.Info(this, "!Disposed", "SetToDisconnected");
			}
		}
	}

	private void UpdateStatusAfterSocketErrorAndThrowException(SocketError error, [CallerMemberName] string callerName = null)
	{
		SocketException ex = new SocketException((int)error);
		UpdateStatusAfterSocketError(ex);
		if (System.Net.NetEventSource.Log.IsEnabled())
		{
			System.Net.NetEventSource.Error(this, ex, callerName);
		}
		throw ex;
	}

	internal void UpdateStatusAfterSocketError(SocketException socketException)
	{
		UpdateStatusAfterSocketError(socketException.SocketErrorCode);
	}

	internal void UpdateStatusAfterSocketError(SocketError errorCode)
	{
		if (System.Net.NetEventSource.Log.IsEnabled())
		{
			System.Net.NetEventSource.Error(this, $"errorCode:{errorCode}", "UpdateStatusAfterSocketError");
		}
		if (_isConnected && (_handle.IsInvalid || (errorCode != SocketError.WouldBlock && errorCode != SocketError.IOPending && errorCode != SocketError.NoBufferSpaceAvailable && errorCode != SocketError.TimedOut)))
		{
			if (System.Net.NetEventSource.Log.IsEnabled())
			{
				System.Net.NetEventSource.Info(this, "Invalidating socket.", "UpdateStatusAfterSocketError");
			}
			SetToDisconnected();
		}
	}

	private bool CheckErrorAndUpdateStatus(SocketError errorCode)
	{
		if (errorCode == SocketError.Success || errorCode == SocketError.IOPending)
		{
			return true;
		}
		UpdateStatusAfterSocketError(errorCode);
		return false;
	}

	private void ValidateReceiveFromEndpointAndState(EndPoint remoteEndPoint, string remoteEndPointArgumentName)
	{
		if (remoteEndPoint == null)
		{
			throw new ArgumentNullException(remoteEndPointArgumentName);
		}
		if (!CanTryAddressFamily(remoteEndPoint.AddressFamily))
		{
			throw new ArgumentException(System.SR.Format(System.SR.net_InvalidEndPointAddressFamily, remoteEndPoint.AddressFamily, _addressFamily), remoteEndPointArgumentName);
		}
		if (_rightEndPoint == null)
		{
			throw new InvalidOperationException(System.SR.net_sockets_mustbind);
		}
	}

	private void ValidateBlockingMode()
	{
		if (_willBlock && !_willBlockInternal)
		{
			throw new InvalidOperationException(System.SR.net_invasync);
		}
	}

	private static SafeFileHandle OpenFileHandle(string name)
	{
		if (!string.IsNullOrEmpty(name))
		{
			return File.OpenHandle(name, FileMode.Open, FileAccess.Read, FileShare.Read, FileOptions.None, 0L);
		}
		return null;
	}

	private void UpdateReceiveSocketErrorForDisposed(ref SocketError socketError, int bytesTransferred)
	{
		if (bytesTransferred == 0 && Disposed)
		{
			socketError = (IsConnectionOriented ? SocketError.ConnectionAborted : SocketError.Interrupted);
		}
	}

	private void UpdateSendSocketErrorForDisposed(ref SocketError socketError)
	{
		if (Disposed)
		{
			socketError = (IsConnectionOriented ? SocketError.ConnectionAborted : SocketError.Interrupted);
		}
	}

	private void UpdateConnectSocketErrorForDisposed(ref SocketError socketError)
	{
		if (Disposed)
		{
			socketError = SocketError.NotSocket;
		}
	}

	private void UpdateAcceptSocketErrorForDisposed(ref SocketError socketError)
	{
		if (Disposed)
		{
			socketError = SocketError.Interrupted;
		}
	}

	private void ThrowIfDisposed()
	{
		if (Disposed)
		{
			ThrowObjectDisposedException();
		}
	}

	[DoesNotReturn]
	private void ThrowObjectDisposedException()
	{
		throw new ObjectDisposedException(GetType().FullName);
	}

	internal static void SocketListDangerousReleaseRefs(IList socketList, ref int refsAdded)
	{
		if (socketList == null)
		{
			return;
		}
		for (int i = 0; i < socketList.Count; i++)
		{
			if (refsAdded <= 0)
			{
				break;
			}
			Socket socket = (Socket)socketList[i];
			socket.InternalSafeHandle.DangerousRelease();
			refsAdded--;
		}
	}

	private static SocketError GetSocketErrorFromFaultedTask(Task t)
	{
		if (t.IsCanceled)
		{
			return SocketError.OperationAborted;
		}
		Exception innerException = t.Exception.InnerException;
		if (!(innerException is SocketException { SocketErrorCode: var socketErrorCode }))
		{
			if (!(innerException is ObjectDisposedException))
			{
				if (innerException is OperationCanceledException)
				{
					return SocketError.OperationAborted;
				}
				return SocketError.SocketError;
			}
			return SocketError.OperationAborted;
		}
		return socketErrorCode;
	}

	public Task<Socket> AcceptAsync()
	{
		return AcceptAsync((Socket?)null, CancellationToken.None).AsTask();
	}

	public ValueTask<Socket> AcceptAsync(CancellationToken cancellationToken)
	{
		return AcceptAsync((Socket?)null, cancellationToken);
	}

	public Task<Socket> AcceptAsync(Socket? acceptSocket)
	{
		return AcceptAsync(acceptSocket, CancellationToken.None).AsTask();
	}

	public ValueTask<Socket> AcceptAsync(Socket? acceptSocket, CancellationToken cancellationToken)
	{
		if (cancellationToken.IsCancellationRequested)
		{
			return ValueTask.FromCanceled<Socket>(cancellationToken);
		}
		AwaitableSocketAsyncEventArgs awaitableSocketAsyncEventArgs = Interlocked.Exchange(ref _singleBufferSendEventArgs, null) ?? new AwaitableSocketAsyncEventArgs(this, isReceiveForCaching: false);
		awaitableSocketAsyncEventArgs.SetBuffer(null, 0, 0);
		awaitableSocketAsyncEventArgs.AcceptSocket = acceptSocket;
		awaitableSocketAsyncEventArgs.WrapExceptionsForNetworkStream = false;
		return awaitableSocketAsyncEventArgs.AcceptAsync(this, cancellationToken);
	}

	public Task ConnectAsync(EndPoint remoteEP)
	{
		return ConnectAsync(remoteEP, default(CancellationToken)).AsTask();
	}

	public ValueTask ConnectAsync(EndPoint remoteEP, CancellationToken cancellationToken)
	{
		if (cancellationToken.IsCancellationRequested)
		{
			return ValueTask.FromCanceled(cancellationToken);
		}
		AwaitableSocketAsyncEventArgs awaitableSocketAsyncEventArgs = Interlocked.Exchange(ref _singleBufferReceiveEventArgs, null) ?? new AwaitableSocketAsyncEventArgs(this, isReceiveForCaching: true);
		awaitableSocketAsyncEventArgs.RemoteEndPoint = remoteEP;
		ValueTask valueTask = awaitableSocketAsyncEventArgs.ConnectAsync(this);
		if (valueTask.IsCompleted || !cancellationToken.CanBeCanceled)
		{
			return valueTask;
		}
		return WaitForConnectWithCancellation(awaitableSocketAsyncEventArgs, valueTask, cancellationToken);
		static async ValueTask WaitForConnectWithCancellation(AwaitableSocketAsyncEventArgs saea, ValueTask connectTask, CancellationToken cancellationToken)
		{
			try
			{
				using (cancellationToken.UnsafeRegister(delegate(object o)
				{
					CancelConnectAsync((SocketAsyncEventArgs)o);
				}, saea))
				{
					await connectTask.ConfigureAwait(continueOnCapturedContext: false);
				}
			}
			catch (SocketException ex) when (ex.SocketErrorCode == SocketError.OperationAborted)
			{
				cancellationToken.ThrowIfCancellationRequested();
				throw;
			}
		}
	}

	public Task ConnectAsync(IPAddress address, int port)
	{
		return ConnectAsync(new IPEndPoint(address, port));
	}

	public ValueTask ConnectAsync(IPAddress address, int port, CancellationToken cancellationToken)
	{
		return ConnectAsync(new IPEndPoint(address, port), cancellationToken);
	}

	public Task ConnectAsync(IPAddress[] addresses, int port)
	{
		return ConnectAsync(addresses, port, CancellationToken.None).AsTask();
	}

	public ValueTask ConnectAsync(IPAddress[] addresses, int port, CancellationToken cancellationToken)
	{
		ThrowIfDisposed();
		if (addresses == null)
		{
			throw new ArgumentNullException("addresses");
		}
		if (addresses.Length == 0)
		{
			throw new ArgumentException(System.SR.net_invalidAddressList, "addresses");
		}
		if (!System.Net.TcpValidationHelpers.ValidatePortNumber(port))
		{
			throw new ArgumentOutOfRangeException("port");
		}
		if (_isListening)
		{
			throw new InvalidOperationException(System.SR.net_sockets_mustnotlisten);
		}
		if (_isConnected)
		{
			throw new SocketException(10056);
		}
		return Core(addresses, port, cancellationToken);
		async ValueTask Core(IPAddress[] addresses, int port, CancellationToken cancellationToken)
		{
			Exception source = null;
			IPEndPoint endPoint = null;
			foreach (IPAddress address in addresses)
			{
				try
				{
					if (endPoint == null)
					{
						endPoint = new IPEndPoint(address, port);
					}
					else
					{
						endPoint.Address = address;
					}
					await ConnectAsync(endPoint, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
					return;
				}
				catch (Exception ex) when (!(ex is OperationCanceledException))
				{
					source = ex;
				}
			}
			ExceptionDispatchInfo.Throw(source);
		}
	}

	public Task ConnectAsync(string host, int port)
	{
		return ConnectAsync(host, port, default(CancellationToken)).AsTask();
	}

	public ValueTask ConnectAsync(string host, int port, CancellationToken cancellationToken)
	{
		if (host == null)
		{
			throw new ArgumentNullException("host");
		}
		IPAddress address;
		EndPoint remoteEP = (IPAddress.TryParse(host, out address) ? ((EndPoint)new IPEndPoint(address, port)) : ((EndPoint)new DnsEndPoint(host, port)));
		return ConnectAsync(remoteEP, cancellationToken);
	}

	public ValueTask DisconnectAsync(bool reuseSocket, CancellationToken cancellationToken = default(CancellationToken))
	{
		if (cancellationToken.IsCancellationRequested)
		{
			return ValueTask.FromCanceled(cancellationToken);
		}
		AwaitableSocketAsyncEventArgs awaitableSocketAsyncEventArgs = Interlocked.Exchange(ref _singleBufferSendEventArgs, null) ?? new AwaitableSocketAsyncEventArgs(this, isReceiveForCaching: false);
		awaitableSocketAsyncEventArgs.DisconnectReuseSocket = reuseSocket;
		awaitableSocketAsyncEventArgs.WrapExceptionsForNetworkStream = false;
		return awaitableSocketAsyncEventArgs.DisconnectAsync(this, cancellationToken);
	}

	public Task<int> ReceiveAsync(ArraySegment<byte> buffer, SocketFlags socketFlags)
	{
		return ReceiveAsync(buffer, socketFlags, fromNetworkStream: false);
	}

	internal Task<int> ReceiveAsync(ArraySegment<byte> buffer, SocketFlags socketFlags, bool fromNetworkStream)
	{
		ValidateBuffer(buffer);
		return ReceiveAsync(buffer, socketFlags, fromNetworkStream, default(CancellationToken)).AsTask();
	}

	public ValueTask<int> ReceiveAsync(Memory<byte> buffer, SocketFlags socketFlags, CancellationToken cancellationToken)
	{
		return ReceiveAsync(buffer, socketFlags, fromNetworkStream: false, cancellationToken);
	}

	internal ValueTask<int> ReceiveAsync(Memory<byte> buffer, SocketFlags socketFlags, bool fromNetworkStream, CancellationToken cancellationToken)
	{
		if (cancellationToken.IsCancellationRequested)
		{
			return ValueTask.FromCanceled<int>(cancellationToken);
		}
		AwaitableSocketAsyncEventArgs awaitableSocketAsyncEventArgs = Interlocked.Exchange(ref _singleBufferReceiveEventArgs, null) ?? new AwaitableSocketAsyncEventArgs(this, isReceiveForCaching: true);
		awaitableSocketAsyncEventArgs.SetBuffer(buffer);
		awaitableSocketAsyncEventArgs.SocketFlags = socketFlags;
		awaitableSocketAsyncEventArgs.WrapExceptionsForNetworkStream = fromNetworkStream;
		return awaitableSocketAsyncEventArgs.ReceiveAsync(this, cancellationToken);
	}

	public Task<int> ReceiveAsync(IList<ArraySegment<byte>> buffers, SocketFlags socketFlags)
	{
		ValidateBuffersList(buffers);
		TaskSocketAsyncEventArgs<int> taskSocketAsyncEventArgs = Interlocked.Exchange(ref _multiBufferReceiveEventArgs, null);
		if (taskSocketAsyncEventArgs == null)
		{
			taskSocketAsyncEventArgs = new TaskSocketAsyncEventArgs<int>();
			taskSocketAsyncEventArgs.Completed += delegate(object s, SocketAsyncEventArgs e)
			{
				CompleteSendReceive((Socket)s, (TaskSocketAsyncEventArgs<int>)e, isReceive: true);
			};
		}
		taskSocketAsyncEventArgs.BufferList = buffers;
		taskSocketAsyncEventArgs.SocketFlags = socketFlags;
		return GetTaskForSendReceive(ReceiveAsync(taskSocketAsyncEventArgs), taskSocketAsyncEventArgs, fromNetworkStream: false, isReceive: true);
	}

	public Task<SocketReceiveFromResult> ReceiveFromAsync(ArraySegment<byte> buffer, SocketFlags socketFlags, EndPoint remoteEndPoint)
	{
		ValidateBuffer(buffer);
		return ReceiveFromAsync(buffer, socketFlags, remoteEndPoint, default(CancellationToken)).AsTask();
	}

	public ValueTask<SocketReceiveFromResult> ReceiveFromAsync(Memory<byte> buffer, SocketFlags socketFlags, EndPoint remoteEndPoint, CancellationToken cancellationToken = default(CancellationToken))
	{
		ValidateReceiveFromEndpointAndState(remoteEndPoint, "remoteEndPoint");
		if (cancellationToken.IsCancellationRequested)
		{
			return ValueTask.FromCanceled<SocketReceiveFromResult>(cancellationToken);
		}
		AwaitableSocketAsyncEventArgs awaitableSocketAsyncEventArgs = Interlocked.Exchange(ref _singleBufferReceiveEventArgs, null) ?? new AwaitableSocketAsyncEventArgs(this, isReceiveForCaching: true);
		awaitableSocketAsyncEventArgs.SetBuffer(buffer);
		awaitableSocketAsyncEventArgs.SocketFlags = socketFlags;
		awaitableSocketAsyncEventArgs.RemoteEndPoint = remoteEndPoint;
		awaitableSocketAsyncEventArgs.WrapExceptionsForNetworkStream = false;
		return awaitableSocketAsyncEventArgs.ReceiveFromAsync(this, cancellationToken);
	}

	public Task<SocketReceiveMessageFromResult> ReceiveMessageFromAsync(ArraySegment<byte> buffer, SocketFlags socketFlags, EndPoint remoteEndPoint)
	{
		ValidateBuffer(buffer);
		return ReceiveMessageFromAsync(buffer, socketFlags, remoteEndPoint, default(CancellationToken)).AsTask();
	}

	public ValueTask<SocketReceiveMessageFromResult> ReceiveMessageFromAsync(Memory<byte> buffer, SocketFlags socketFlags, EndPoint remoteEndPoint, CancellationToken cancellationToken = default(CancellationToken))
	{
		ValidateReceiveFromEndpointAndState(remoteEndPoint, "remoteEndPoint");
		if (cancellationToken.IsCancellationRequested)
		{
			return ValueTask.FromCanceled<SocketReceiveMessageFromResult>(cancellationToken);
		}
		AwaitableSocketAsyncEventArgs awaitableSocketAsyncEventArgs = Interlocked.Exchange(ref _singleBufferReceiveEventArgs, null) ?? new AwaitableSocketAsyncEventArgs(this, isReceiveForCaching: true);
		awaitableSocketAsyncEventArgs.SetBuffer(buffer);
		awaitableSocketAsyncEventArgs.SocketFlags = socketFlags;
		awaitableSocketAsyncEventArgs.RemoteEndPoint = remoteEndPoint;
		awaitableSocketAsyncEventArgs.WrapExceptionsForNetworkStream = false;
		return awaitableSocketAsyncEventArgs.ReceiveMessageFromAsync(this, cancellationToken);
	}

	public Task<int> SendAsync(ArraySegment<byte> buffer, SocketFlags socketFlags)
	{
		ValidateBuffer(buffer);
		return SendAsync(buffer, socketFlags, default(CancellationToken)).AsTask();
	}

	public ValueTask<int> SendAsync(ReadOnlyMemory<byte> buffer, SocketFlags socketFlags, CancellationToken cancellationToken)
	{
		if (cancellationToken.IsCancellationRequested)
		{
			return ValueTask.FromCanceled<int>(cancellationToken);
		}
		AwaitableSocketAsyncEventArgs awaitableSocketAsyncEventArgs = Interlocked.Exchange(ref _singleBufferSendEventArgs, null) ?? new AwaitableSocketAsyncEventArgs(this, isReceiveForCaching: false);
		awaitableSocketAsyncEventArgs.SetBuffer(MemoryMarshal.AsMemory(buffer));
		awaitableSocketAsyncEventArgs.SocketFlags = socketFlags;
		awaitableSocketAsyncEventArgs.WrapExceptionsForNetworkStream = false;
		return awaitableSocketAsyncEventArgs.SendAsync(this, cancellationToken);
	}

	internal ValueTask SendAsyncForNetworkStream(ReadOnlyMemory<byte> buffer, SocketFlags socketFlags, CancellationToken cancellationToken)
	{
		if (cancellationToken.IsCancellationRequested)
		{
			return ValueTask.FromCanceled(cancellationToken);
		}
		AwaitableSocketAsyncEventArgs awaitableSocketAsyncEventArgs = Interlocked.Exchange(ref _singleBufferSendEventArgs, null) ?? new AwaitableSocketAsyncEventArgs(this, isReceiveForCaching: false);
		awaitableSocketAsyncEventArgs.SetBuffer(MemoryMarshal.AsMemory(buffer));
		awaitableSocketAsyncEventArgs.SocketFlags = socketFlags;
		awaitableSocketAsyncEventArgs.WrapExceptionsForNetworkStream = true;
		return awaitableSocketAsyncEventArgs.SendAsyncForNetworkStream(this, cancellationToken);
	}

	public Task<int> SendAsync(IList<ArraySegment<byte>> buffers, SocketFlags socketFlags)
	{
		ValidateBuffersList(buffers);
		TaskSocketAsyncEventArgs<int> taskSocketAsyncEventArgs = Interlocked.Exchange(ref _multiBufferSendEventArgs, null);
		if (taskSocketAsyncEventArgs == null)
		{
			taskSocketAsyncEventArgs = new TaskSocketAsyncEventArgs<int>();
			taskSocketAsyncEventArgs.Completed += delegate(object s, SocketAsyncEventArgs e)
			{
				CompleteSendReceive((Socket)s, (TaskSocketAsyncEventArgs<int>)e, isReceive: false);
			};
		}
		taskSocketAsyncEventArgs.BufferList = buffers;
		taskSocketAsyncEventArgs.SocketFlags = socketFlags;
		return GetTaskForSendReceive(SendAsync(taskSocketAsyncEventArgs), taskSocketAsyncEventArgs, fromNetworkStream: false, isReceive: false);
	}

	public Task<int> SendToAsync(ArraySegment<byte> buffer, SocketFlags socketFlags, EndPoint remoteEP)
	{
		ValidateBuffer(buffer);
		return SendToAsync(buffer, socketFlags, remoteEP, default(CancellationToken)).AsTask();
	}

	public ValueTask<int> SendToAsync(ReadOnlyMemory<byte> buffer, SocketFlags socketFlags, EndPoint remoteEP, CancellationToken cancellationToken = default(CancellationToken))
	{
		if (remoteEP == null)
		{
			throw new ArgumentNullException("remoteEP");
		}
		if (cancellationToken.IsCancellationRequested)
		{
			return ValueTask.FromCanceled<int>(cancellationToken);
		}
		AwaitableSocketAsyncEventArgs awaitableSocketAsyncEventArgs = Interlocked.Exchange(ref _singleBufferSendEventArgs, null) ?? new AwaitableSocketAsyncEventArgs(this, isReceiveForCaching: false);
		awaitableSocketAsyncEventArgs.SetBuffer(MemoryMarshal.AsMemory(buffer));
		awaitableSocketAsyncEventArgs.SocketFlags = socketFlags;
		awaitableSocketAsyncEventArgs.RemoteEndPoint = remoteEP;
		awaitableSocketAsyncEventArgs.WrapExceptionsForNetworkStream = false;
		return awaitableSocketAsyncEventArgs.SendToAsync(this, cancellationToken);
	}

	public ValueTask SendFileAsync(string? fileName, CancellationToken cancellationToken = default(CancellationToken))
	{
		return SendFileAsync(fileName, default(ReadOnlyMemory<byte>), default(ReadOnlyMemory<byte>), TransmitFileOptions.UseDefaultWorkerThread, cancellationToken);
	}

	public ValueTask SendFileAsync(string? fileName, ReadOnlyMemory<byte> preBuffer, ReadOnlyMemory<byte> postBuffer, TransmitFileOptions flags, CancellationToken cancellationToken = default(CancellationToken))
	{
		if (cancellationToken.IsCancellationRequested)
		{
			return ValueTask.FromCanceled(cancellationToken);
		}
		if (!IsConnectionOriented)
		{
			SocketException exception = new SocketException(10057);
			return ValueTask.FromException((Exception)(object)exception);
		}
		int num = 0;
		if (fileName != null)
		{
			num++;
		}
		if (!preBuffer.IsEmpty)
		{
			num++;
		}
		if (!postBuffer.IsEmpty)
		{
			num++;
		}
		AwaitableSocketAsyncEventArgs awaitableSocketAsyncEventArgs = Interlocked.Exchange(ref _singleBufferSendEventArgs, null) ?? new AwaitableSocketAsyncEventArgs(this, isReceiveForCaching: false);
		SendPacketsElement[]? sendPacketsElements = awaitableSocketAsyncEventArgs.SendPacketsElements;
		SendPacketsElement[] array = ((sendPacketsElements != null && sendPacketsElements.Length == num) ? awaitableSocketAsyncEventArgs.SendPacketsElements : new SendPacketsElement[num]);
		int num2 = 0;
		if (!preBuffer.IsEmpty)
		{
			array[num2++] = new SendPacketsElement(preBuffer, num2 == num);
		}
		if (fileName != null)
		{
			array[num2++] = new SendPacketsElement(fileName, 0, 0, num2 == num);
		}
		if (!postBuffer.IsEmpty)
		{
			array[num2++] = new SendPacketsElement(postBuffer, num2 == num);
		}
		awaitableSocketAsyncEventArgs.SendPacketsFlags = flags;
		awaitableSocketAsyncEventArgs.SendPacketsElements = array;
		awaitableSocketAsyncEventArgs.WrapExceptionsForNetworkStream = false;
		return awaitableSocketAsyncEventArgs.SendPacketsAsync(this, cancellationToken);
	}

	private static void ValidateBufferArguments(byte[] buffer, int offset, int size)
	{
		if (buffer == null)
		{
			throw new ArgumentNullException("buffer");
		}
		if ((uint)offset > (uint)buffer.Length)
		{
			throw new ArgumentOutOfRangeException("offset");
		}
		if ((uint)size > (uint)(buffer.Length - offset))
		{
			throw new ArgumentOutOfRangeException("size");
		}
	}

	private static void ValidateBuffer(ArraySegment<byte> buffer)
	{
		if (buffer.Array == null)
		{
			throw new ArgumentNullException("Array");
		}
		if ((uint)buffer.Offset > (uint)buffer.Array.Length)
		{
			throw new ArgumentOutOfRangeException("Offset");
		}
		if ((uint)buffer.Count > (uint)(buffer.Array.Length - buffer.Offset))
		{
			throw new ArgumentOutOfRangeException("Count");
		}
	}

	private static void ValidateBuffersList(IList<ArraySegment<byte>> buffers)
	{
		if (buffers == null)
		{
			throw new ArgumentNullException("buffers");
		}
		if (buffers.Count == 0)
		{
			throw new ArgumentException(System.SR.Format(System.SR.net_sockets_zerolist, "buffers"), "buffers");
		}
	}

	private Task<int> GetTaskForSendReceive(bool pending, TaskSocketAsyncEventArgs<int> saea, bool fromNetworkStream, bool isReceive)
	{
		Task<int> result;
		if (pending)
		{
			result = saea.GetCompletionResponsibility(out var responsibleForReturningToPool).Task;
			if (responsibleForReturningToPool)
			{
				ReturnSocketAsyncEventArgs(saea, isReceive);
			}
		}
		else
		{
			result = ((saea.SocketError != 0) ? Task.FromException<int>(GetException(saea.SocketError, fromNetworkStream)) : Task.FromResult((!(fromNetworkStream && !isReceive)) ? saea.BytesTransferred : 0));
			ReturnSocketAsyncEventArgs(saea, isReceive);
		}
		return result;
	}

	private static void CompleteSendReceive(Socket s, TaskSocketAsyncEventArgs<int> saea, bool isReceive)
	{
		SocketError socketError = saea.SocketError;
		int bytesTransferred = saea.BytesTransferred;
		bool wrapExceptionsInIOExceptions = saea._wrapExceptionsInIOExceptions;
		bool responsibleForReturningToPool;
		AsyncTaskMethodBuilder<int> completionResponsibility = saea.GetCompletionResponsibility(out responsibleForReturningToPool);
		if (responsibleForReturningToPool)
		{
			s.ReturnSocketAsyncEventArgs(saea, isReceive);
		}
		if (socketError == SocketError.Success)
		{
			completionResponsibility.SetResult(bytesTransferred);
		}
		else
		{
			completionResponsibility.SetException(GetException(socketError, wrapExceptionsInIOExceptions));
		}
	}

	private static Exception GetException(SocketError error, bool wrapExceptionsInIOExceptions = false)
	{
		Exception ex = ExceptionDispatchInfo.SetCurrentStackTrace((Exception)(object)new SocketException((int)error));
		if (!wrapExceptionsInIOExceptions)
		{
			return ex;
		}
		return new IOException(System.SR.Format(System.SR.net_io_readwritefailure, ex.Message), ex);
	}

	private void ReturnSocketAsyncEventArgs(TaskSocketAsyncEventArgs<int> saea, bool isReceive)
	{
		saea._accessed = false;
		saea._builder = default(AsyncTaskMethodBuilder<int>);
		saea._wrapExceptionsInIOExceptions = false;
		if (Interlocked.CompareExchange(ref isReceive ? ref _multiBufferReceiveEventArgs : ref _multiBufferSendEventArgs, saea, null) != null)
		{
			saea.Dispose();
		}
	}

	private void DisposeCachedTaskSocketAsyncEventArgs()
	{
		Interlocked.Exchange(ref _multiBufferReceiveEventArgs, null)?.Dispose();
		Interlocked.Exchange(ref _multiBufferSendEventArgs, null)?.Dispose();
		Interlocked.Exchange(ref _singleBufferReceiveEventArgs, null)?.Dispose();
		Interlocked.Exchange(ref _singleBufferSendEventArgs, null)?.Dispose();
	}

	internal void ReplaceHandleIfNecessaryAfterFailedConnect()
	{
	}

	[SupportedOSPlatform("windows")]
	public unsafe Socket(SocketInformation socketInformation)
	{
		SocketError socketError = SocketPal.CreateSocket(socketInformation, out _handle, ref _addressFamily, ref _socketType, ref _protocolType);
		if (socketError != 0)
		{
			_handle = null;
			if (socketError == SocketError.InvalidArgument)
			{
				throw new ArgumentException(System.SR.net_sockets_invalid_socketinformation, "socketInformation");
			}
			throw new SocketException((int)socketError);
		}
		if (_addressFamily != AddressFamily.InterNetwork && _addressFamily != AddressFamily.InterNetworkV6)
		{
			_handle.Dispose();
			_handle = null;
			throw new NotSupportedException(System.SR.net_invalidversion);
		}
		_isConnected = socketInformation.GetOption(SocketInformationOptions.Connected);
		_willBlock = !socketInformation.GetOption(SocketInformationOptions.NonBlocking);
		InternalSetBlocking(_willBlock);
		_isListening = socketInformation.GetOption(SocketInformationOptions.Listening);
		IPEndPoint iPEndPoint = new IPEndPoint((_addressFamily == AddressFamily.InterNetwork) ? IPAddress.Any : IPAddress.IPv6Any, 0);
		System.Net.Internals.SocketAddress socketAddress = IPEndPointExtensions.Serialize(iPEndPoint);
		fixed (byte* buffer = socketAddress.Buffer)
		{
			fixed (int* nameLen = &socketAddress.InternalSize)
			{
				socketError = SocketPal.GetSockName(_handle, buffer, nameLen);
			}
		}
		switch (socketError)
		{
		case SocketError.Success:
			_rightEndPoint = iPEndPoint.Create(socketAddress);
			break;
		default:
			_handle.Dispose();
			_handle = null;
			throw new SocketException((int)socketError);
		case SocketError.InvalidArgument:
			break;
		}
	}

	private unsafe void LoadSocketTypeFromHandle(SafeSocketHandle handle, out AddressFamily addressFamily, out SocketType socketType, out ProtocolType protocolType, out bool blocking, out bool isListening, out bool isSocket)
	{
		global::Interop.Winsock.EnsureInitialized();
		global::Interop.Winsock.WSAPROTOCOL_INFOW wSAPROTOCOL_INFOW = default(global::Interop.Winsock.WSAPROTOCOL_INFOW);
		int optionLength = sizeof(global::Interop.Winsock.WSAPROTOCOL_INFOW);
		if (global::Interop.Winsock.getsockopt(handle, SocketOptionLevel.Socket, (SocketOptionName)8197, (byte*)(&wSAPROTOCOL_INFOW), ref optionLength) == SocketError.SocketError)
		{
			throw new SocketException((int)SocketPal.GetLastSocketError());
		}
		addressFamily = wSAPROTOCOL_INFOW.iAddressFamily;
		socketType = wSAPROTOCOL_INFOW.iSocketType;
		protocolType = wSAPROTOCOL_INFOW.iProtocol;
		isListening = SocketPal.GetSockOpt(_handle, SocketOptionLevel.Socket, SocketOptionName.AcceptConnection, out var optionValue) == SocketError.Success && optionValue != 0;
		blocking = true;
		isSocket = true;
	}

	[SupportedOSPlatform("windows")]
	public SocketInformation DuplicateAndClose(int targetProcessId)
	{
		ThrowIfDisposed();
		SocketInformation socketInformation;
		SocketError socketError = SocketPal.DuplicateSocket(_handle, targetProcessId, out socketInformation);
		if (socketError != 0)
		{
			throw new SocketException((int)socketError);
		}
		socketInformation.SetOption(SocketInformationOptions.Connected, Connected);
		socketInformation.SetOption(SocketInformationOptions.NonBlocking, !Blocking);
		socketInformation.SetOption(SocketInformationOptions.Listening, _isListening);
		Close(-1);
		return socketInformation;
	}

	private DynamicWinsockMethods GetDynamicWinsockMethods()
	{
		return _dynamicWinsockMethods ?? (_dynamicWinsockMethods = DynamicWinsockMethods.GetMethods(_addressFamily, _socketType, _protocolType));
	}

	internal unsafe bool AcceptEx(SafeSocketHandle listenSocketHandle, SafeSocketHandle acceptSocketHandle, IntPtr buffer, int len, int localAddressLength, int remoteAddressLength, out int bytesReceived, NativeOverlapped* overlapped)
	{
		AcceptExDelegate acceptExDelegate = GetDynamicWinsockMethods().GetAcceptExDelegate(listenSocketHandle);
		return acceptExDelegate(listenSocketHandle, acceptSocketHandle, buffer, len, localAddressLength, remoteAddressLength, out bytesReceived, overlapped);
	}

	internal void GetAcceptExSockaddrs(IntPtr buffer, int receiveDataLength, int localAddressLength, int remoteAddressLength, out IntPtr localSocketAddress, out int localSocketAddressLength, out IntPtr remoteSocketAddress, out int remoteSocketAddressLength)
	{
		GetAcceptExSockaddrsDelegate getAcceptExSockaddrsDelegate = GetDynamicWinsockMethods().GetGetAcceptExSockaddrsDelegate(_handle);
		getAcceptExSockaddrsDelegate(buffer, receiveDataLength, localAddressLength, remoteAddressLength, out localSocketAddress, out localSocketAddressLength, out remoteSocketAddress, out remoteSocketAddressLength);
	}

	internal unsafe bool DisconnectEx(SafeSocketHandle socketHandle, NativeOverlapped* overlapped, int flags, int reserved)
	{
		DisconnectExDelegate disconnectExDelegate = GetDynamicWinsockMethods().GetDisconnectExDelegate(socketHandle);
		return disconnectExDelegate(socketHandle, overlapped, flags, reserved);
	}

	internal unsafe bool DisconnectExBlocking(SafeSocketHandle socketHandle, int flags, int reserved)
	{
		DisconnectExDelegate disconnectExDelegate = GetDynamicWinsockMethods().GetDisconnectExDelegate(socketHandle);
		return disconnectExDelegate(socketHandle, null, flags, reserved);
	}

	private void EnableReuseUnicastPort()
	{
		int optionValue = 1;
		SocketError socketError = global::Interop.Winsock.setsockopt(_handle, SocketOptionLevel.Socket, SocketOptionName.ReuseUnicastPort, ref optionValue, 4);
		if (System.Net.NetEventSource.Log.IsEnabled() && socketError != 0)
		{
			socketError = SocketPal.GetLastSocketError();
			System.Net.NetEventSource.Info($"Enabling SO_REUSE_UNICASTPORT failed with error code: {socketError}", null, "EnableReuseUnicastPort");
		}
	}

	internal unsafe bool ConnectEx(SafeSocketHandle socketHandle, IntPtr socketAddress, int socketAddressSize, IntPtr buffer, int dataLength, out int bytesSent, NativeOverlapped* overlapped)
	{
		ConnectExDelegate connectExDelegate = GetDynamicWinsockMethods().GetConnectExDelegate(socketHandle);
		return connectExDelegate(socketHandle, socketAddress, socketAddressSize, buffer, dataLength, out bytesSent, overlapped);
	}

	internal unsafe SocketError WSARecvMsg(SafeSocketHandle socketHandle, IntPtr msg, out int bytesTransferred, NativeOverlapped* overlapped, IntPtr completionRoutine)
	{
		WSARecvMsgDelegate wSARecvMsgDelegate = GetDynamicWinsockMethods().GetWSARecvMsgDelegate(socketHandle);
		return wSARecvMsgDelegate(socketHandle, msg, out bytesTransferred, overlapped, completionRoutine);
	}

	internal unsafe SocketError WSARecvMsgBlocking(SafeSocketHandle socketHandle, IntPtr msg, out int bytesTransferred)
	{
		WSARecvMsgDelegate wSARecvMsgDelegate = GetDynamicWinsockMethods().GetWSARecvMsgDelegate(_handle);
		return wSARecvMsgDelegate(socketHandle, msg, out bytesTransferred, null, IntPtr.Zero);
	}

	internal unsafe bool TransmitPackets(SafeSocketHandle socketHandle, IntPtr packetArray, int elementCount, int sendSize, NativeOverlapped* overlapped, TransmitFileOptions flags)
	{
		TransmitPacketsDelegate transmitPacketsDelegate = GetDynamicWinsockMethods().GetTransmitPacketsDelegate(socketHandle);
		return transmitPacketsDelegate(socketHandle, packetArray, elementCount, sendSize, overlapped, flags);
	}

	internal static void SocketListToFileDescriptorSet(IList socketList, Span<IntPtr> fileDescriptorSet, ref int refsAdded)
	{
		int count;
		if (socketList == null || (count = socketList.Count) == 0)
		{
			return;
		}
		fileDescriptorSet[0] = (IntPtr)count;
		for (int i = 0; i < count; i++)
		{
			if (!(socketList[i] is Socket socket))
			{
				throw new ArgumentException(System.SR.Format(System.SR.net_sockets_select, socketList[i]?.GetType().FullName, typeof(Socket).FullName), "socketList");
			}
			bool success = false;
			socket.InternalSafeHandle.DangerousAddRef(ref success);
			fileDescriptorSet[i + 1] = socket.InternalSafeHandle.DangerousGetHandle();
			refsAdded++;
		}
	}

	internal static void SelectFileDescriptor(IList socketList, Span<IntPtr> fileDescriptorSet, ref int refsAdded)
	{
		int num;
		if (socketList == null || (num = socketList.Count) == 0)
		{
			return;
		}
		int num2 = (int)fileDescriptorSet[0];
		if (num2 == 0)
		{
			SocketListDangerousReleaseRefs(socketList, ref refsAdded);
			socketList.Clear();
			return;
		}
		lock (socketList)
		{
			for (int i = 0; i < num; i++)
			{
				Socket socket = socketList[i] as Socket;
				int j;
				for (j = 0; j < num2 && !(fileDescriptorSet[j + 1] == socket._handle.DangerousGetHandle()); j++)
				{
				}
				if (j == num2)
				{
					socket.InternalSafeHandle.DangerousRelease();
					refsAdded--;
					socketList.RemoveAt(i--);
					num--;
				}
			}
		}
	}

	private Socket GetOrCreateAcceptSocket(Socket acceptSocket, bool checkDisconnected, string propertyName, out SafeSocketHandle handle)
	{
		if (acceptSocket == null)
		{
			acceptSocket = new Socket(_addressFamily, _socketType, _protocolType);
		}
		else if (acceptSocket._rightEndPoint != null && (!checkDisconnected || !acceptSocket._isDisconnected))
		{
			throw new InvalidOperationException(System.SR.Format(System.SR.net_sockets_namedmustnotbebound, propertyName));
		}
		handle = acceptSocket._handle;
		return acceptSocket;
	}

	private void SendFileInternal(string fileName, ReadOnlySpan<byte> preBuffer, ReadOnlySpan<byte> postBuffer, TransmitFileOptions flags)
	{
		SocketError socketError;
		using (SafeFileHandle fileHandle = OpenFileHandle(fileName))
		{
			socketError = SocketPal.SendFile(_handle, fileHandle, preBuffer, postBuffer, flags);
		}
		if (socketError != 0)
		{
			UpdateSendSocketErrorForDisposed(ref socketError);
			UpdateStatusAfterSocketErrorAndThrowException(socketError, "SendFileInternal");
		}
		if ((flags & (TransmitFileOptions.Disconnect | TransmitFileOptions.ReuseSocket)) != 0)
		{
			SetToDisconnected();
			_remoteEndPoint = null;
		}
	}

	internal ThreadPoolBoundHandle GetOrAllocateThreadPoolBoundHandle()
	{
		return _handle.GetThreadPoolBoundHandle() ?? GetOrAllocateThreadPoolBoundHandleSlow();
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	internal ThreadPoolBoundHandle GetOrAllocateThreadPoolBoundHandleSlow()
	{
		bool trySkipCompletionPortOnSuccess = !CompletionPortHelper.PlatformHasUdpIssue || _protocolType != ProtocolType.Udp;
		return _handle.GetOrAllocateThreadPoolBoundHandle(trySkipCompletionPortOnSuccess);
	}
}
