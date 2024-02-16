using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace System.Net.WebSockets;

internal abstract class WebSocketBase : WebSocket, IDisposable
{
	private abstract class WebSocketOperation
	{
		public class ReceiveOperation : WebSocketOperation
		{
			private int _receiveState;

			private bool _pongReceived;

			private bool _receiveCompleted;

			protected override WebSocketProtocolComponent.ActionQueue ActionQueue => WebSocketProtocolComponent.ActionQueue.Receive;

			protected override int BufferCount => 1;

			public ReceiveOperation(WebSocketBase webSocket)
				: base(webSocket)
			{
			}

			protected override void Initialize(ArraySegment<byte>? buffer, CancellationToken cancellationToken)
			{
				_pongReceived = false;
				_receiveCompleted = false;
				_webSocket.ThrowIfDisposed();
				switch (Interlocked.CompareExchange(ref _webSocket._receiveState, 1, 0))
				{
				case 0:
					_receiveState = 1;
					break;
				case 2:
				{
					if (!_webSocket._internalBuffer.ReceiveFromBufferedPayload(buffer.Value, out var receiveResult))
					{
						_webSocket.UpdateReceiveState(0, 2);
					}
					base.ReceiveResult = receiveResult;
					_receiveCompleted = true;
					break;
				}
				case 1:
					break;
				}
			}

			protected override void Cleanup()
			{
			}

			protected override bool ShouldContinue(CancellationToken cancellationToken)
			{
				cancellationToken.ThrowIfCancellationRequested();
				if (_receiveCompleted)
				{
					return false;
				}
				_webSocket.ThrowIfDisposed();
				_webSocket.ThrowIfPendingException();
				WebSocketProtocolComponent.WebSocketReceive(_webSocket);
				return true;
			}

			protected override bool ProcessAction_NoAction()
			{
				if (_pongReceived)
				{
					_receiveCompleted = false;
					_pongReceived = false;
					return false;
				}
				_receiveCompleted = true;
				if (base.ReceiveResult.MessageType == WebSocketMessageType.Close)
				{
					return true;
				}
				return false;
			}

			protected override void ProcessAction_IndicateReceiveComplete(ArraySegment<byte>? buffer, WebSocketProtocolComponent.BufferType bufferType, WebSocketProtocolComponent.Action action, global::Interop.WebSocket.Buffer[] dataBuffers, uint dataBufferCount, IntPtr actionContext)
			{
				int num = 0;
				_pongReceived = false;
				if (bufferType == WebSocketProtocolComponent.BufferType.PingPong)
				{
					_pongReceived = true;
					WebSocketProtocolComponent.WebSocketCompleteAction(_webSocket, actionContext, num);
					return;
				}
				WebSocketReceiveResult receiveResult;
				try
				{
					WebSocketMessageType messageType = GetMessageType(bufferType);
					int newReceiveState = 0;
					if (bufferType == WebSocketProtocolComponent.BufferType.Close)
					{
						ArraySegment<byte> empty = ArraySegment<byte>.Empty;
						_webSocket._internalBuffer.ConvertCloseBuffer(action, dataBuffers[0], out var closeStatus, out var reason);
						receiveResult = new WebSocketReceiveResult(num, messageType, endOfMessage: true, closeStatus, reason);
					}
					else
					{
						ArraySegment<byte> empty = _webSocket._internalBuffer.ConvertNativeBuffer(action, dataBuffers[0], bufferType);
						bool endOfMessage = bufferType == WebSocketProtocolComponent.BufferType.BinaryMessage || bufferType == WebSocketProtocolComponent.BufferType.UTF8Message || bufferType == WebSocketProtocolComponent.BufferType.Close;
						if (empty.Count > buffer.Value.Count)
						{
							_webSocket._internalBuffer.BufferPayload(empty, buffer.Value.Count, messageType, endOfMessage);
							newReceiveState = 2;
							endOfMessage = false;
						}
						num = Math.Min(empty.Count, buffer.Value.Count);
						if (num > 0)
						{
							Buffer.BlockCopy(empty.Array, empty.Offset, buffer.Value.Array, buffer.Value.Offset, num);
						}
						receiveResult = new WebSocketReceiveResult(num, messageType, endOfMessage);
					}
					_webSocket.UpdateReceiveState(newReceiveState, _receiveState);
				}
				finally
				{
					WebSocketProtocolComponent.WebSocketCompleteAction(_webSocket, actionContext, num);
				}
				base.ReceiveResult = receiveResult;
			}
		}

		public class SendOperation : WebSocketOperation
		{
			protected bool _BufferHasBeenPinned;

			protected override WebSocketProtocolComponent.ActionQueue ActionQueue => WebSocketProtocolComponent.ActionQueue.Send;

			protected override int BufferCount => 2;

			internal WebSocketProtocolComponent.BufferType BufferType { get; set; }

			public SendOperation(WebSocketBase webSocket)
				: base(webSocket)
			{
			}

			protected virtual global::Interop.WebSocket.Buffer? CreateBuffer(ArraySegment<byte>? buffer)
			{
				if (!buffer.HasValue)
				{
					return null;
				}
				global::Interop.WebSocket.Buffer value = default(global::Interop.WebSocket.Buffer);
				_webSocket._internalBuffer.PinSendBuffer(buffer.Value, out _BufferHasBeenPinned);
				value.Data.BufferData = _webSocket._internalBuffer.ConvertPinnedSendPayloadToNative(buffer.Value);
				value.Data.BufferLength = (uint)buffer.Value.Count;
				return value;
			}

			protected override bool ProcessAction_NoAction()
			{
				return false;
			}

			protected override void Cleanup()
			{
				if (_BufferHasBeenPinned)
				{
					_BufferHasBeenPinned = false;
					_webSocket._internalBuffer.ReleasePinnedSendBuffer();
				}
			}

			protected override void Initialize(ArraySegment<byte>? buffer, CancellationToken cancellationToken)
			{
				_webSocket.ThrowIfDisposed();
				_webSocket.ThrowIfPendingException();
				global::Interop.WebSocket.Buffer? buffer2 = CreateBuffer(buffer);
				if (buffer2.HasValue)
				{
					WebSocketProtocolComponent.WebSocketSend(_webSocket, BufferType, buffer2.Value);
				}
				else
				{
					WebSocketProtocolComponent.WebSocketSendWithoutBody(_webSocket, BufferType);
				}
			}

			protected override bool ShouldContinue(CancellationToken cancellationToken)
			{
				if (base.AsyncOperationCompleted)
				{
					return false;
				}
				cancellationToken.ThrowIfCancellationRequested();
				return true;
			}
		}

		public class CloseOutputOperation : SendOperation
		{
			internal WebSocketCloseStatus CloseStatus { get; set; }

			internal string CloseReason { get; set; }

			public CloseOutputOperation(WebSocketBase webSocket)
				: base(webSocket)
			{
				base.BufferType = WebSocketProtocolComponent.BufferType.Close;
			}

			protected override global::Interop.WebSocket.Buffer? CreateBuffer(ArraySegment<byte>? buffer)
			{
				_webSocket.ThrowIfDisposed();
				_webSocket.ThrowIfPendingException();
				if (CloseStatus == WebSocketCloseStatus.Empty)
				{
					return null;
				}
				global::Interop.WebSocket.Buffer value = default(global::Interop.WebSocket.Buffer);
				if (CloseReason != null)
				{
					byte[] bytes = Encoding.UTF8.GetBytes(CloseReason);
					ArraySegment<byte> payload = new ArraySegment<byte>(bytes, 0, Math.Min(123, bytes.Length));
					_webSocket._internalBuffer.PinSendBuffer(payload, out _BufferHasBeenPinned);
					value.CloseStatus.ReasonData = _webSocket._internalBuffer.ConvertPinnedSendPayloadToNative(payload);
					value.CloseStatus.ReasonLength = (uint)payload.Count;
				}
				value.CloseStatus.CloseStatus = (ushort)CloseStatus;
				return value;
			}
		}

		private readonly WebSocketBase _webSocket;

		protected bool AsyncOperationCompleted { get; set; }

		public WebSocketReceiveResult ReceiveResult { get; protected set; }

		protected abstract int BufferCount { get; }

		protected abstract WebSocketProtocolComponent.ActionQueue ActionQueue { get; }

		internal WebSocketOperation(WebSocketBase webSocket)
		{
			_webSocket = webSocket;
			AsyncOperationCompleted = false;
		}

		protected abstract void Initialize(ArraySegment<byte>? buffer, CancellationToken cancellationToken);

		protected abstract bool ShouldContinue(CancellationToken cancellationToken);

		protected abstract bool ProcessAction_NoAction();

		protected virtual void ProcessAction_IndicateReceiveComplete(ArraySegment<byte>? buffer, WebSocketProtocolComponent.BufferType bufferType, WebSocketProtocolComponent.Action action, global::Interop.WebSocket.Buffer[] dataBuffers, uint dataBufferCount, IntPtr actionContext)
		{
			throw new NotImplementedException();
		}

		protected abstract void Cleanup();

		internal async Task<WebSocketReceiveResult> Process(ArraySegment<byte>? buffer, CancellationToken cancellationToken)
		{
			bool sessionHandleLockTaken = false;
			AsyncOperationCompleted = false;
			ReceiveResult = null;
			try
			{
				Monitor.Enter(_webSocket.SessionHandle, ref sessionHandleLockTaken);
				_webSocket.ThrowIfPendingException();
				Initialize(buffer, cancellationToken);
				while (ShouldContinue(cancellationToken))
				{
					bool completed = false;
					while (!completed)
					{
						global::Interop.WebSocket.Buffer[] array = new global::Interop.WebSocket.Buffer[BufferCount];
						uint dataBufferCount = (uint)BufferCount;
						_webSocket.ThrowIfDisposed();
						WebSocketProtocolComponent.WebSocketGetAction(_webSocket, ActionQueue, array, ref dataBufferCount, out var action, out var bufferType, out var actionContext);
						switch (action)
						{
						case WebSocketProtocolComponent.Action.NoAction:
							if (ProcessAction_NoAction())
							{
								bool thisLockTaken = false;
								try
								{
									if (_webSocket.StartOnCloseReceived(ref thisLockTaken))
									{
										ReleaseLock(_webSocket.SessionHandle, ref sessionHandleLockTaken);
										bool flag;
										try
										{
											flag = await _webSocket.StartOnCloseCompleted(thisLockTaken, sessionHandleLockTaken, cancellationToken).SuppressContextFlow();
										}
										catch (Exception)
										{
											_webSocket.ResetFlagAndTakeLock(_webSocket._thisLock, ref thisLockTaken);
											throw;
										}
										if (flag)
										{
											_webSocket.ResetFlagAndTakeLock(_webSocket._thisLock, ref thisLockTaken);
											_webSocket.FinishOnCloseCompleted();
										}
									}
									_webSocket.FinishOnCloseReceived(ReceiveResult.CloseStatus.Value, ReceiveResult.CloseStatusDescription);
								}
								finally
								{
									if (thisLockTaken)
									{
										ReleaseLock(_webSocket._thisLock, ref thisLockTaken);
									}
								}
							}
							completed = true;
							break;
						case WebSocketProtocolComponent.Action.IndicateReceiveComplete:
							ProcessAction_IndicateReceiveComplete(buffer, bufferType, action, array, dataBufferCount, actionContext);
							break;
						case WebSocketProtocolComponent.Action.ReceiveFromNetwork:
						{
							int count = 0;
							try
							{
								ArraySegment<byte> arraySegment = _webSocket._internalBuffer.ConvertNativeBuffer(action, array[0], bufferType);
								ReleaseLock(_webSocket.SessionHandle, ref sessionHandleLockTaken);
								HttpWebSocket.ThrowIfConnectionAborted(_webSocket._innerStream, read: true);
								try
								{
									Task<int> task = _webSocket._innerStream.ReadAsync(arraySegment.Array, arraySegment.Offset, arraySegment.Count, cancellationToken);
									count = await task.SuppressContextFlow();
									_webSocket._keepAliveTracker.OnDataReceived();
								}
								catch (ObjectDisposedException innerException)
								{
									throw new WebSocketException(WebSocketError.ConnectionClosedPrematurely, innerException);
								}
								catch (NotSupportedException innerException2)
								{
									throw new WebSocketException(WebSocketError.ConnectionClosedPrematurely, innerException2);
								}
								Monitor.Enter(_webSocket.SessionHandle, ref sessionHandleLockTaken);
								_webSocket.ThrowIfPendingException();
								if (count == 0)
								{
									throw new WebSocketException(WebSocketError.ConnectionClosedPrematurely);
								}
							}
							finally
							{
								WebSocketProtocolComponent.WebSocketCompleteAction(_webSocket, actionContext, count);
							}
							break;
						}
						case WebSocketProtocolComponent.Action.IndicateSendComplete:
							WebSocketProtocolComponent.WebSocketCompleteAction(_webSocket, actionContext, 0);
							AsyncOperationCompleted = true;
							ReleaseLock(_webSocket.SessionHandle, ref sessionHandleLockTaken);
							await _webSocket._innerStream.FlushAsync(cancellationToken).SuppressContextFlow();
							Monitor.Enter(_webSocket.SessionHandle, ref sessionHandleLockTaken);
							break;
						case WebSocketProtocolComponent.Action.SendToNetwork:
						{
							int bytesSent = 0;
							try
							{
								if ((_webSocket.State != WebSocketState.CloseSent || (bufferType != WebSocketProtocolComponent.BufferType.PingPong && bufferType != WebSocketProtocolComponent.BufferType.UnsolicitedPong)) && dataBufferCount != 0)
								{
									List<ArraySegment<byte>> list = new List<ArraySegment<byte>>((int)dataBufferCount);
									int sendBufferSize2 = 0;
									ArraySegment<byte> item = _webSocket._internalBuffer.ConvertNativeBuffer(action, array[0], bufferType);
									list.Add(item);
									sendBufferSize2 += item.Count;
									if (dataBufferCount == 2)
									{
										ArraySegment<byte> item2 = ((!_webSocket._internalBuffer.IsPinnedSendPayloadBuffer(array[1], bufferType)) ? _webSocket._internalBuffer.ConvertNativeBuffer(action, array[1], bufferType) : _webSocket._internalBuffer.ConvertPinnedSendPayloadFromNative(array[1], bufferType));
										list.Add(item2);
										sendBufferSize2 += item2.Count;
									}
									ReleaseLock(_webSocket.SessionHandle, ref sessionHandleLockTaken);
									HttpWebSocket.ThrowIfConnectionAborted(_webSocket._innerStream, read: false);
									await _webSocket.SendFrameAsync(list, cancellationToken).SuppressContextFlow();
									Monitor.Enter(_webSocket.SessionHandle, ref sessionHandleLockTaken);
									_webSocket.ThrowIfPendingException();
									bytesSent += sendBufferSize2;
									_webSocket._keepAliveTracker.OnDataSent();
								}
							}
							finally
							{
								WebSocketProtocolComponent.WebSocketCompleteAction(_webSocket, actionContext, bytesSent);
							}
							break;
						}
						default:
							throw new InvalidOperationException();
						}
					}
					ReleaseLock(_webSocket.SessionHandle, ref sessionHandleLockTaken);
					Monitor.Enter(_webSocket.SessionHandle, ref sessionHandleLockTaken);
				}
			}
			finally
			{
				Cleanup();
				ReleaseLock(_webSocket.SessionHandle, ref sessionHandleLockTaken);
			}
			return ReceiveResult;
		}
	}

	private abstract class KeepAliveTracker : IDisposable
	{
		private sealed class DisabledKeepAliveTracker : KeepAliveTracker
		{
			public override void OnDataReceived()
			{
			}

			public override void OnDataSent()
			{
			}

			public override void ResetTimer()
			{
			}

			public override void StartTimer(WebSocketBase webSocket)
			{
			}

			public override bool ShouldSendKeepAlive()
			{
				return false;
			}

			public override void Dispose()
			{
			}
		}

		private sealed class DefaultKeepAliveTracker : KeepAliveTracker
		{
			private static readonly TimerCallback s_KeepAliveTimerElapsedCallback = OnKeepAlive;

			private readonly TimeSpan _keepAliveInterval;

			private readonly Stopwatch _lastSendActivity;

			private readonly Stopwatch _lastReceiveActivity;

			private Timer _keepAliveTimer;

			public DefaultKeepAliveTracker(TimeSpan keepAliveInterval)
			{
				_keepAliveInterval = keepAliveInterval;
				_lastSendActivity = new Stopwatch();
				_lastReceiveActivity = new Stopwatch();
			}

			public override void OnDataReceived()
			{
				_lastReceiveActivity.Restart();
			}

			public override void OnDataSent()
			{
				_lastSendActivity.Restart();
			}

			public override void ResetTimer()
			{
				ResetTimer((int)_keepAliveInterval.TotalMilliseconds);
			}

			public override void StartTimer(WebSocketBase webSocket)
			{
				int dueTime = (int)_keepAliveInterval.TotalMilliseconds;
				_keepAliveTimer = new Timer(s_KeepAliveTimerElapsedCallback, webSocket, -1, -1);
				_keepAliveTimer.Change(dueTime, -1);
			}

			public override bool ShouldSendKeepAlive()
			{
				TimeSpan idleTime = GetIdleTime();
				if (idleTime >= _keepAliveInterval)
				{
					return true;
				}
				ResetTimer((int)(_keepAliveInterval - idleTime).TotalMilliseconds);
				return false;
			}

			public override void Dispose()
			{
				_keepAliveTimer.Dispose();
			}

			private void ResetTimer(int dueInMilliseconds)
			{
				_keepAliveTimer.Change(dueInMilliseconds, -1);
			}

			private TimeSpan GetIdleTime()
			{
				TimeSpan timeElapsed = GetTimeElapsed(_lastSendActivity);
				TimeSpan timeElapsed2 = GetTimeElapsed(_lastReceiveActivity);
				if (timeElapsed2 < timeElapsed)
				{
					return timeElapsed2;
				}
				return timeElapsed;
			}

			private TimeSpan GetTimeElapsed(Stopwatch watch)
			{
				if (watch.IsRunning)
				{
					return watch.Elapsed;
				}
				return _keepAliveInterval;
			}
		}

		public abstract void OnDataReceived();

		public abstract void OnDataSent();

		public abstract void Dispose();

		public abstract void StartTimer(WebSocketBase webSocket);

		public abstract void ResetTimer();

		public abstract bool ShouldSendKeepAlive();

		public static KeepAliveTracker Create(TimeSpan keepAliveInterval)
		{
			if ((int)keepAliveInterval.TotalMilliseconds > 0)
			{
				return new DefaultKeepAliveTracker(keepAliveInterval);
			}
			return new DisabledKeepAliveTracker();
		}
	}

	private sealed class OutstandingOperationHelper : IDisposable
	{
		private volatile int _operationsOutstanding;

		private volatile CancellationTokenSource _cancellationTokenSource;

		private volatile bool _isDisposed;

		private readonly object _thisLock = new object();

		public bool TryStartOperation(CancellationToken userCancellationToken, out CancellationToken linkedCancellationToken)
		{
			linkedCancellationToken = CancellationToken.None;
			ThrowIfDisposed();
			lock (_thisLock)
			{
				if (++_operationsOutstanding == 1)
				{
					linkedCancellationToken = CreateLinkedCancellationToken(userCancellationToken);
					return true;
				}
				return false;
			}
		}

		public void CompleteOperation(bool ownsCancellationTokenSource)
		{
			if (_isDisposed)
			{
				return;
			}
			CancellationTokenSource cancellationTokenSource = null;
			lock (_thisLock)
			{
				_operationsOutstanding--;
				if (ownsCancellationTokenSource)
				{
					cancellationTokenSource = _cancellationTokenSource;
					_cancellationTokenSource = null;
				}
			}
			cancellationTokenSource?.Dispose();
		}

		private CancellationToken CreateLinkedCancellationToken(CancellationToken cancellationToken)
		{
			return (_cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken)).Token;
		}

		public void CancelIO()
		{
			CancellationTokenSource cancellationTokenSource = null;
			lock (_thisLock)
			{
				if (_operationsOutstanding == 0)
				{
					return;
				}
				cancellationTokenSource = _cancellationTokenSource;
			}
			if (cancellationTokenSource != null)
			{
				try
				{
					cancellationTokenSource.Cancel();
				}
				catch (ObjectDisposedException)
				{
				}
			}
		}

		public void Dispose()
		{
			if (_isDisposed)
			{
				return;
			}
			CancellationTokenSource cancellationTokenSource = null;
			lock (_thisLock)
			{
				if (_isDisposed)
				{
					return;
				}
				_isDisposed = true;
				cancellationTokenSource = _cancellationTokenSource;
				_cancellationTokenSource = null;
			}
			cancellationTokenSource?.Dispose();
		}

		private void ThrowIfDisposed()
		{
			if (_isDisposed)
			{
				throw new ObjectDisposedException(GetType().FullName);
			}
		}
	}

	internal interface IWebSocketStream
	{
		bool SupportsMultipleWrite { get; }

		void SwitchToOpaqueMode(WebSocketBase webSocket);

		void Abort();

		Task MultipleWriteAsync(IList<ArraySegment<byte>> buffers, CancellationToken cancellationToken);

		Task CloseNetworkConnectionAsync(CancellationToken cancellationToken);
	}

	private readonly OutstandingOperationHelper _closeOutstandingOperationHelper;

	private readonly OutstandingOperationHelper _closeOutputOutstandingOperationHelper;

	private readonly OutstandingOperationHelper _receiveOutstandingOperationHelper;

	private readonly OutstandingOperationHelper _sendOutstandingOperationHelper;

	private readonly Stream _innerStream;

	private readonly IWebSocketStream _innerStreamAsWebSocketStream;

	private readonly string _subProtocol;

	private readonly SemaphoreSlim _sendFrameThrottle;

	private readonly object _thisLock;

	private readonly WebSocketBuffer _internalBuffer;

	private readonly KeepAliveTracker _keepAliveTracker;

	private volatile bool _cleanedUp;

	private volatile TaskCompletionSource _closeReceivedTaskCompletionSource;

	private volatile Task _closeOutputTask;

	private volatile bool _isDisposed;

	private volatile Task _closeNetworkConnectionTask;

	private volatile bool _closeAsyncStartedReceive;

	private volatile WebSocketState _state;

	private volatile Task _keepAliveTask;

	private volatile WebSocketOperation.ReceiveOperation _receiveOperation;

	private volatile WebSocketOperation.SendOperation _sendOperation;

	private volatile WebSocketOperation.SendOperation _keepAliveOperation;

	private volatile WebSocketOperation.CloseOutputOperation _closeOutputOperation;

	private WebSocketCloseStatus? _closeStatus;

	private string _closeStatusDescription;

	private int _receiveState;

	private Exception _pendingException;

	public override WebSocketState State => _state;

	public override string SubProtocol => _subProtocol;

	public override WebSocketCloseStatus? CloseStatus => _closeStatus;

	public override string CloseStatusDescription => _closeStatusDescription;

	internal WebSocketBuffer InternalBuffer => _internalBuffer;

	internal abstract SafeHandle SessionHandle { get; }

	protected WebSocketBase(Stream innerStream, string subProtocol, TimeSpan keepAliveInterval, WebSocketBuffer internalBuffer)
	{
		HttpWebSocket.ValidateInnerStream(innerStream);
		HttpWebSocket.ValidateOptions(subProtocol, internalBuffer.ReceiveBufferSize, internalBuffer.SendBufferSize, keepAliveInterval);
		_thisLock = new object();
		_innerStream = innerStream;
		_internalBuffer = internalBuffer;
		if (System.Net.NetEventSource.Log.IsEnabled())
		{
			System.Net.NetEventSource.Associate(this, _innerStream, ".ctor");
			System.Net.NetEventSource.Associate(this, _internalBuffer, ".ctor");
		}
		_closeOutstandingOperationHelper = new OutstandingOperationHelper();
		_closeOutputOutstandingOperationHelper = new OutstandingOperationHelper();
		_receiveOutstandingOperationHelper = new OutstandingOperationHelper();
		_sendOutstandingOperationHelper = new OutstandingOperationHelper();
		_state = WebSocketState.Open;
		_subProtocol = subProtocol;
		_sendFrameThrottle = new SemaphoreSlim(1, 1);
		_closeStatus = null;
		_closeStatusDescription = null;
		_innerStreamAsWebSocketStream = innerStream as IWebSocketStream;
		if (_innerStreamAsWebSocketStream != null)
		{
			_innerStreamAsWebSocketStream.SwitchToOpaqueMode(this);
		}
		_keepAliveTracker = KeepAliveTracker.Create(keepAliveInterval);
	}

	protected void StartKeepAliveTimer()
	{
		_keepAliveTracker.StartTimer(this);
	}

	public override Task<WebSocketReceiveResult> ReceiveAsync(ArraySegment<byte> buffer, CancellationToken cancellationToken)
	{
		System.Net.WebSockets.WebSocketValidate.ValidateArraySegment(buffer, "buffer");
		return ReceiveAsyncCore(buffer, cancellationToken);
	}

	private async Task<WebSocketReceiveResult> ReceiveAsyncCore(ArraySegment<byte> buffer, CancellationToken cancellationToken)
	{
		ThrowIfPendingException();
		ThrowIfDisposed();
		WebSocket.ThrowOnInvalidState(State, WebSocketState.Open, WebSocketState.CloseSent);
		bool ownsCancellationTokenSource = false;
		CancellationToken linkedCancellationToken = CancellationToken.None;
		WebSocketReceiveResult webSocketReceiveResult;
		try
		{
			ownsCancellationTokenSource = _receiveOutstandingOperationHelper.TryStartOperation(cancellationToken, out linkedCancellationToken);
			if (!ownsCancellationTokenSource)
			{
				lock (_thisLock)
				{
					if (_closeAsyncStartedReceive)
					{
						throw new InvalidOperationException(System.SR.Format(System.SR.net_WebSockets_ReceiveAsyncDisallowedAfterCloseAsync, "CloseAsync", "CloseOutputAsync"));
					}
					throw new InvalidOperationException(System.SR.Format(System.SR.net_Websockets_AlreadyOneOutstandingOperation, "ReceiveAsync"));
				}
			}
			EnsureReceiveOperation();
			webSocketReceiveResult = await _receiveOperation.Process(buffer, linkedCancellationToken).SuppressContextFlow();
			if (System.Net.NetEventSource.Log.IsEnabled() && webSocketReceiveResult.Count > 0)
			{
				System.Net.NetEventSource.DumpBuffer(this, buffer.Array, buffer.Offset, webSocketReceiveResult.Count, "ReceiveAsyncCore");
			}
		}
		catch (Exception exception)
		{
			bool isCancellationRequested = linkedCancellationToken.IsCancellationRequested;
			Abort();
			ThrowIfConvertibleException("ReceiveAsync", exception, cancellationToken, isCancellationRequested);
			throw;
		}
		finally
		{
			_receiveOutstandingOperationHelper.CompleteOperation(ownsCancellationTokenSource);
		}
		return webSocketReceiveResult;
	}

	public override Task SendAsync(ArraySegment<byte> buffer, WebSocketMessageType messageType, bool endOfMessage, CancellationToken cancellationToken)
	{
		if (messageType != WebSocketMessageType.Binary && messageType != 0)
		{
			throw new ArgumentException(System.SR.Format(System.SR.net_WebSockets_Argument_InvalidMessageType, messageType, "SendAsync", WebSocketMessageType.Binary, WebSocketMessageType.Text, "CloseOutputAsync"), "messageType");
		}
		System.Net.WebSockets.WebSocketValidate.ValidateArraySegment(buffer, "buffer");
		return SendAsyncCore(buffer, messageType, endOfMessage, cancellationToken);
	}

	private async Task SendAsyncCore(ArraySegment<byte> buffer, WebSocketMessageType messageType, bool endOfMessage, CancellationToken cancellationToken)
	{
		ThrowIfPendingException();
		ThrowIfDisposed();
		WebSocket.ThrowOnInvalidState(State, WebSocketState.Open, WebSocketState.CloseReceived);
		bool ownsCancellationTokenSource = false;
		CancellationToken linkedCancellationToken = CancellationToken.None;
		try
		{
			while (true)
			{
				bool flag;
				ownsCancellationTokenSource = (flag = _sendOutstandingOperationHelper.TryStartOperation(cancellationToken, out linkedCancellationToken));
				if (flag)
				{
					break;
				}
				Task keepAliveTask;
				lock (SessionHandle)
				{
					keepAliveTask = _keepAliveTask;
					if (keepAliveTask == null)
					{
						_sendOutstandingOperationHelper.CompleteOperation(ownsCancellationTokenSource);
						ownsCancellationTokenSource = (flag = _sendOutstandingOperationHelper.TryStartOperation(cancellationToken, out linkedCancellationToken));
						if (flag)
						{
							break;
						}
						throw new InvalidOperationException(System.SR.Format(System.SR.net_Websockets_AlreadyOneOutstandingOperation, "SendAsync"));
					}
				}
				await keepAliveTask.SuppressContextFlow();
				ThrowIfPendingException();
				_sendOutstandingOperationHelper.CompleteOperation(ownsCancellationTokenSource);
			}
			if (System.Net.NetEventSource.Log.IsEnabled() && buffer.Count > 0)
			{
				System.Net.NetEventSource.DumpBuffer(this, buffer.Array, buffer.Offset, buffer.Count, "SendAsyncCore");
			}
			EnsureSendOperation();
			_sendOperation.BufferType = GetBufferType(messageType, endOfMessage);
			await _sendOperation.Process(buffer, linkedCancellationToken).SuppressContextFlow();
		}
		catch (Exception exception)
		{
			bool isCancellationRequested = linkedCancellationToken.IsCancellationRequested;
			Abort();
			ThrowIfConvertibleException("SendAsync", exception, cancellationToken, isCancellationRequested);
			throw;
		}
		finally
		{
			_sendOutstandingOperationHelper.CompleteOperation(ownsCancellationTokenSource);
		}
	}

	private async Task SendFrameAsync(IList<ArraySegment<byte>> sendBuffers, CancellationToken cancellationToken)
	{
		bool sendFrameLockTaken = false;
		try
		{
			await _sendFrameThrottle.WaitAsync(cancellationToken).SuppressContextFlow();
			sendFrameLockTaken = true;
			if (sendBuffers.Count > 1 && _innerStreamAsWebSocketStream != null && _innerStreamAsWebSocketStream.SupportsMultipleWrite)
			{
				await _innerStreamAsWebSocketStream.MultipleWriteAsync(sendBuffers, cancellationToken).SuppressContextFlow();
				return;
			}
			foreach (ArraySegment<byte> sendBuffer in sendBuffers)
			{
				await _innerStream.WriteAsync(sendBuffer.Array, sendBuffer.Offset, sendBuffer.Count, cancellationToken).SuppressContextFlow();
			}
		}
		catch (ObjectDisposedException innerException)
		{
			throw new WebSocketException(WebSocketError.ConnectionClosedPrematurely, innerException);
		}
		catch (NotSupportedException innerException2)
		{
			throw new WebSocketException(WebSocketError.ConnectionClosedPrematurely, innerException2);
		}
		finally
		{
			if (sendFrameLockTaken)
			{
				_sendFrameThrottle.Release();
			}
		}
	}

	public override void Abort()
	{
		bool thisLockTaken = false;
		bool sessionHandleLockTaken = false;
		try
		{
			if (WebSocket.IsStateTerminal(State))
			{
				return;
			}
			TakeLocks(ref thisLockTaken, ref sessionHandleLockTaken);
			if (!WebSocket.IsStateTerminal(State))
			{
				_state = WebSocketState.Aborted;
				if (SessionHandle != null && !SessionHandle.IsClosed && !SessionHandle.IsInvalid)
				{
					WebSocketProtocolComponent.WebSocketAbortHandle(SessionHandle);
				}
				_receiveOutstandingOperationHelper.CancelIO();
				_sendOutstandingOperationHelper.CancelIO();
				_closeOutputOutstandingOperationHelper.CancelIO();
				_closeOutstandingOperationHelper.CancelIO();
				if (_innerStreamAsWebSocketStream != null)
				{
					_innerStreamAsWebSocketStream.Abort();
				}
				CleanUp();
			}
		}
		finally
		{
			ReleaseLocks(ref thisLockTaken, ref sessionHandleLockTaken);
		}
	}

	public override Task CloseOutputAsync(WebSocketCloseStatus closeStatus, string statusDescription, CancellationToken cancellationToken)
	{
		System.Net.WebSockets.WebSocketValidate.ValidateCloseStatus(closeStatus, statusDescription);
		return CloseOutputAsyncCore(closeStatus, statusDescription, cancellationToken);
	}

	private async Task CloseOutputAsyncCore(WebSocketCloseStatus closeStatus, string statusDescription, CancellationToken cancellationToken)
	{
		ThrowIfPendingException();
		if (WebSocket.IsStateTerminal(State))
		{
			return;
		}
		ThrowIfDisposed();
		bool thisLockTaken = false;
		bool sessionHandleLockTaken = false;
		bool needToCompleteSendOperation = false;
		bool ownsCloseOutputCancellationTokenSource = false;
		bool ownsSendCancellationTokenSource = false;
		CancellationToken linkedCancellationToken = CancellationToken.None;
		try
		{
			TakeLocks(ref thisLockTaken, ref sessionHandleLockTaken);
			ThrowIfPendingException();
			ThrowIfDisposed();
			if (WebSocket.IsStateTerminal(State))
			{
				return;
			}
			WebSocket.ThrowOnInvalidState(State, WebSocketState.Open, WebSocketState.CloseReceived);
			ownsCloseOutputCancellationTokenSource = _closeOutputOutstandingOperationHelper.TryStartOperation(cancellationToken, out linkedCancellationToken);
			if (!ownsCloseOutputCancellationTokenSource)
			{
				Task closeOutputTask = _closeOutputTask;
				if (closeOutputTask != null)
				{
					ReleaseLocks(ref thisLockTaken, ref sessionHandleLockTaken);
					await closeOutputTask.SuppressContextFlow();
					TakeLocks(ref thisLockTaken, ref sessionHandleLockTaken);
				}
				return;
			}
			needToCompleteSendOperation = true;
			while (true)
			{
				bool flag;
				ownsSendCancellationTokenSource = (flag = _sendOutstandingOperationHelper.TryStartOperation(cancellationToken, out linkedCancellationToken));
				if (flag)
				{
					break;
				}
				if (_keepAliveTask != null)
				{
					Task keepAliveTask = _keepAliveTask;
					ReleaseLocks(ref thisLockTaken, ref sessionHandleLockTaken);
					await keepAliveTask.SuppressContextFlow();
					TakeLocks(ref thisLockTaken, ref sessionHandleLockTaken);
					ThrowIfPendingException();
					_sendOutstandingOperationHelper.CompleteOperation(ownsSendCancellationTokenSource);
					continue;
				}
				throw new InvalidOperationException(System.SR.Format(System.SR.net_Websockets_AlreadyOneOutstandingOperation, "SendAsync"));
			}
			EnsureCloseOutputOperation();
			_closeOutputOperation.CloseStatus = closeStatus;
			_closeOutputOperation.CloseReason = statusDescription;
			_closeOutputTask = _closeOutputOperation.Process(null, linkedCancellationToken);
			ReleaseLocks(ref thisLockTaken, ref sessionHandleLockTaken);
			await _closeOutputTask.SuppressContextFlow();
			TakeLocks(ref thisLockTaken, ref sessionHandleLockTaken);
			if (OnCloseOutputCompleted())
			{
				bool flag2;
				try
				{
					flag2 = await StartOnCloseCompleted(thisLockTaken, sessionHandleLockTaken, linkedCancellationToken).SuppressContextFlow();
				}
				catch (Exception)
				{
					ResetFlagsAndTakeLocks(ref thisLockTaken, ref sessionHandleLockTaken);
					throw;
				}
				if (flag2)
				{
					ResetFlagsAndTakeLocks(ref thisLockTaken, ref sessionHandleLockTaken);
					FinishOnCloseCompleted();
				}
			}
		}
		catch (Exception exception)
		{
			bool isCancellationRequested = linkedCancellationToken.IsCancellationRequested;
			Abort();
			ThrowIfConvertibleException("CloseOutputAsync", exception, cancellationToken, isCancellationRequested);
			throw;
		}
		finally
		{
			_closeOutputOutstandingOperationHelper.CompleteOperation(ownsCloseOutputCancellationTokenSource);
			if (needToCompleteSendOperation)
			{
				_sendOutstandingOperationHelper.CompleteOperation(ownsSendCancellationTokenSource);
			}
			_closeOutputTask = null;
			ReleaseLocks(ref thisLockTaken, ref sessionHandleLockTaken);
		}
	}

	private bool OnCloseOutputCompleted()
	{
		if (WebSocket.IsStateTerminal(State))
		{
			return false;
		}
		switch (State)
		{
		case WebSocketState.Open:
			_state = WebSocketState.CloseSent;
			return false;
		case WebSocketState.CloseReceived:
			return true;
		default:
			return false;
		}
	}

	private async Task<bool> StartOnCloseCompleted(bool thisLockTakenSnapshot, bool sessionHandleLockTakenSnapshot, CancellationToken cancellationToken)
	{
		if (WebSocket.IsStateTerminal(_state))
		{
			return false;
		}
		_state = WebSocketState.Closed;
		if (_innerStreamAsWebSocketStream != null)
		{
			bool lockTaken = thisLockTakenSnapshot;
			bool sessionHandleLockTaken = sessionHandleLockTakenSnapshot;
			try
			{
				if (_closeNetworkConnectionTask == null)
				{
					_closeNetworkConnectionTask = _innerStreamAsWebSocketStream.CloseNetworkConnectionAsync(cancellationToken);
				}
				if (lockTaken && sessionHandleLockTaken)
				{
					ReleaseLocks(ref lockTaken, ref sessionHandleLockTaken);
				}
				else if (lockTaken)
				{
					ReleaseLock(_thisLock, ref lockTaken);
				}
				await _closeNetworkConnectionTask.SuppressContextFlow();
			}
			catch (Exception ex)
			{
				if (!CanHandleExceptionDuringClose(ex))
				{
					ThrowIfConvertibleException("StartOnCloseCompleted", ex, cancellationToken, cancellationToken.IsCancellationRequested);
					throw;
				}
			}
		}
		return true;
	}

	private void FinishOnCloseCompleted()
	{
		CleanUp();
	}

	public override Task CloseAsync(WebSocketCloseStatus closeStatus, string statusDescription, CancellationToken cancellationToken)
	{
		System.Net.WebSockets.WebSocketValidate.ValidateCloseStatus(closeStatus, statusDescription);
		return CloseAsyncCore(closeStatus, statusDescription, cancellationToken);
	}

	private async Task CloseAsyncCore(WebSocketCloseStatus closeStatus, string statusDescription, CancellationToken cancellationToken)
	{
		ThrowIfPendingException();
		if (WebSocket.IsStateTerminal(State))
		{
			return;
		}
		ThrowIfDisposed();
		bool lockTaken = false;
		Monitor.Enter(_thisLock, ref lockTaken);
		bool ownsCloseCancellationTokenSource = false;
		CancellationToken linkedCancellationToken = CancellationToken.None;
		try
		{
			ThrowIfPendingException();
			if (WebSocket.IsStateTerminal(State))
			{
				return;
			}
			ThrowIfDisposed();
			WebSocket.ThrowOnInvalidState(State, WebSocketState.Open, WebSocketState.CloseReceived, WebSocketState.CloseSent);
			ownsCloseCancellationTokenSource = _closeOutstandingOperationHelper.TryStartOperation(cancellationToken, out linkedCancellationToken);
			Task task;
			if (ownsCloseCancellationTokenSource)
			{
				task = _closeOutputTask;
				if (task == null && State != WebSocketState.CloseSent)
				{
					if (_closeReceivedTaskCompletionSource == null)
					{
						_closeReceivedTaskCompletionSource = new TaskCompletionSource();
					}
					task = CloseOutputAsync(closeStatus, statusDescription, linkedCancellationToken);
				}
			}
			else
			{
				task = _closeReceivedTaskCompletionSource.Task;
			}
			if (task != null)
			{
				ReleaseLock(_thisLock, ref lockTaken);
				try
				{
					await task.SuppressContextFlow();
				}
				catch (Exception ex)
				{
					Monitor.Enter(_thisLock, ref lockTaken);
					if (!CanHandleExceptionDuringClose(ex))
					{
						ThrowIfConvertibleException("CloseOutputAsync", ex, cancellationToken, linkedCancellationToken.IsCancellationRequested);
						throw;
					}
				}
				if (!lockTaken)
				{
					Monitor.Enter(_thisLock, ref lockTaken);
				}
			}
			if (OnCloseOutputCompleted())
			{
				bool flag;
				try
				{
					flag = await StartOnCloseCompleted(lockTaken, sessionHandleLockTakenSnapshot: false, linkedCancellationToken).SuppressContextFlow();
				}
				catch (Exception)
				{
					ResetFlagAndTakeLock(_thisLock, ref lockTaken);
					throw;
				}
				if (flag)
				{
					ResetFlagAndTakeLock(_thisLock, ref lockTaken);
					FinishOnCloseCompleted();
				}
			}
			if (WebSocket.IsStateTerminal(State))
			{
				return;
			}
			linkedCancellationToken = CancellationToken.None;
			bool flag2 = _receiveOutstandingOperationHelper.TryStartOperation(cancellationToken, out linkedCancellationToken);
			if (flag2)
			{
				_closeAsyncStartedReceive = true;
				ArraySegment<byte> closeMessageBuffer = new ArraySegment<byte>(new byte[256]);
				EnsureReceiveOperation();
				Task<WebSocketReceiveResult> task2 = _receiveOperation.Process(closeMessageBuffer, linkedCancellationToken);
				ReleaseLock(_thisLock, ref lockTaken);
				WebSocketReceiveResult receiveResult = null;
				try
				{
					receiveResult = await task2.SuppressContextFlow();
				}
				catch (Exception ex3)
				{
					Monitor.Enter(_thisLock, ref lockTaken);
					if (!CanHandleExceptionDuringClose(ex3))
					{
						ThrowIfConvertibleException("CloseAsync", ex3, cancellationToken, linkedCancellationToken.IsCancellationRequested);
						throw;
					}
				}
				if (receiveResult != null)
				{
					if (System.Net.NetEventSource.Log.IsEnabled() && receiveResult.Count > 0)
					{
						System.Net.NetEventSource.DumpBuffer(this, closeMessageBuffer.Array, closeMessageBuffer.Offset, receiveResult.Count, "CloseAsyncCore");
					}
					if (receiveResult.MessageType != WebSocketMessageType.Close)
					{
						throw new WebSocketException(WebSocketError.InvalidMessageType, System.SR.Format(System.SR.net_WebSockets_InvalidMessageType, "WebSocket.CloseAsync", "WebSocket.CloseOutputAsync", receiveResult.MessageType));
					}
				}
			}
			else
			{
				_receiveOutstandingOperationHelper.CompleteOperation(flag2);
				ReleaseLock(_thisLock, ref lockTaken);
				await _closeReceivedTaskCompletionSource.Task.SuppressContextFlow();
			}
			if (!lockTaken)
			{
				Monitor.Enter(_thisLock, ref lockTaken);
			}
			if (WebSocket.IsStateTerminal(State))
			{
				return;
			}
			bool ownsSendCancellationSource = false;
			try
			{
				ownsSendCancellationSource = _sendOutstandingOperationHelper.TryStartOperation(cancellationToken, out linkedCancellationToken);
				bool flag3;
				try
				{
					flag3 = await StartOnCloseCompleted(lockTaken, sessionHandleLockTakenSnapshot: false, linkedCancellationToken).SuppressContextFlow();
				}
				catch (Exception)
				{
					ResetFlagAndTakeLock(_thisLock, ref lockTaken);
					throw;
				}
				if (flag3)
				{
					ResetFlagAndTakeLock(_thisLock, ref lockTaken);
					FinishOnCloseCompleted();
				}
			}
			finally
			{
				_sendOutstandingOperationHelper.CompleteOperation(ownsSendCancellationSource);
			}
		}
		catch (Exception exception)
		{
			bool isCancellationRequested = linkedCancellationToken.IsCancellationRequested;
			Abort();
			ThrowIfConvertibleException("CloseAsync", exception, cancellationToken, isCancellationRequested);
			throw;
		}
		finally
		{
			_closeOutstandingOperationHelper.CompleteOperation(ownsCloseCancellationTokenSource);
			ReleaseLock(_thisLock, ref lockTaken);
		}
	}

	public override void Dispose()
	{
		if (_isDisposed)
		{
			return;
		}
		bool thisLockTaken = false;
		bool sessionHandleLockTaken = false;
		try
		{
			TakeLocks(ref thisLockTaken, ref sessionHandleLockTaken);
			if (!_isDisposed)
			{
				if (!WebSocket.IsStateTerminal(State))
				{
					Abort();
				}
				else
				{
					CleanUp();
				}
				_isDisposed = true;
			}
		}
		finally
		{
			ReleaseLocks(ref thisLockTaken, ref sessionHandleLockTaken);
		}
	}

	private void ResetFlagAndTakeLock(object lockObject, ref bool thisLockTaken)
	{
		thisLockTaken = false;
		Monitor.Enter(lockObject, ref thisLockTaken);
	}

	private void ResetFlagsAndTakeLocks(ref bool thisLockTaken, ref bool sessionHandleLockTaken)
	{
		thisLockTaken = false;
		sessionHandleLockTaken = false;
		TakeLocks(ref thisLockTaken, ref sessionHandleLockTaken);
	}

	private void TakeLocks(ref bool thisLockTaken, ref bool sessionHandleLockTaken)
	{
		Monitor.Enter(SessionHandle, ref sessionHandleLockTaken);
		Monitor.Enter(_thisLock, ref thisLockTaken);
	}

	private void ReleaseLocks(ref bool thisLockTaken, ref bool sessionHandleLockTaken)
	{
		if (thisLockTaken)
		{
			Monitor.Exit(_thisLock);
			thisLockTaken = false;
		}
		if (sessionHandleLockTaken)
		{
			Monitor.Exit(SessionHandle);
			sessionHandleLockTaken = false;
		}
	}

	private void EnsureReceiveOperation()
	{
		if (_receiveOperation != null)
		{
			return;
		}
		lock (_thisLock)
		{
			if (_receiveOperation == null)
			{
				_receiveOperation = new WebSocketOperation.ReceiveOperation(this);
			}
		}
	}

	private void EnsureSendOperation()
	{
		if (_sendOperation != null)
		{
			return;
		}
		lock (_thisLock)
		{
			if (_sendOperation == null)
			{
				_sendOperation = new WebSocketOperation.SendOperation(this);
			}
		}
	}

	private void EnsureKeepAliveOperation()
	{
		if (_keepAliveOperation != null)
		{
			return;
		}
		lock (_thisLock)
		{
			if (_keepAliveOperation == null)
			{
				WebSocketOperation.SendOperation sendOperation = new WebSocketOperation.SendOperation(this);
				sendOperation.BufferType = WebSocketProtocolComponent.BufferType.UnsolicitedPong;
				_keepAliveOperation = sendOperation;
			}
		}
	}

	private void EnsureCloseOutputOperation()
	{
		if (_closeOutputOperation != null)
		{
			return;
		}
		lock (_thisLock)
		{
			if (_closeOutputOperation == null)
			{
				_closeOutputOperation = new WebSocketOperation.CloseOutputOperation(this);
			}
		}
	}

	private static void ReleaseLock(object lockObject, ref bool lockTaken)
	{
		if (lockTaken)
		{
			Monitor.Exit(lockObject);
			lockTaken = false;
		}
	}

	private static WebSocketProtocolComponent.BufferType GetBufferType(WebSocketMessageType messageType, bool endOfMessage)
	{
		if (messageType == WebSocketMessageType.Text)
		{
			if (endOfMessage)
			{
				return WebSocketProtocolComponent.BufferType.UTF8Message;
			}
			return WebSocketProtocolComponent.BufferType.UTF8Fragment;
		}
		if (endOfMessage)
		{
			return WebSocketProtocolComponent.BufferType.BinaryMessage;
		}
		return WebSocketProtocolComponent.BufferType.BinaryFragment;
	}

	private static WebSocketMessageType GetMessageType(WebSocketProtocolComponent.BufferType bufferType)
	{
		switch (bufferType)
		{
		case WebSocketProtocolComponent.BufferType.Close:
			return WebSocketMessageType.Close;
		case WebSocketProtocolComponent.BufferType.BinaryMessage:
		case WebSocketProtocolComponent.BufferType.BinaryFragment:
			return WebSocketMessageType.Binary;
		case WebSocketProtocolComponent.BufferType.UTF8Message:
		case WebSocketProtocolComponent.BufferType.UTF8Fragment:
			return WebSocketMessageType.Text;
		default:
			throw new WebSocketException(WebSocketError.NativeError, System.SR.Format(System.SR.net_WebSockets_InvalidBufferType, bufferType, WebSocketProtocolComponent.BufferType.Close, WebSocketProtocolComponent.BufferType.BinaryFragment, WebSocketProtocolComponent.BufferType.BinaryMessage, WebSocketProtocolComponent.BufferType.UTF8Fragment, WebSocketProtocolComponent.BufferType.UTF8Message));
		}
	}

	internal void ValidateNativeBuffers(WebSocketProtocolComponent.Action action, WebSocketProtocolComponent.BufferType bufferType, global::Interop.WebSocket.Buffer[] dataBuffers, uint dataBufferCount)
	{
		_internalBuffer.ValidateNativeBuffers(action, bufferType, dataBuffers, dataBufferCount);
	}

	private void ThrowIfAborted(bool aborted, Exception innerException)
	{
		if (aborted)
		{
			throw new WebSocketException(WebSocketError.InvalidState, System.SR.Format(System.SR.net_WebSockets_InvalidState_ClosedOrAborted, GetType().FullName, WebSocketState.Aborted), innerException);
		}
	}

	private bool CanHandleExceptionDuringClose(Exception error)
	{
		if (State != WebSocketState.Closed)
		{
			return false;
		}
		if (!(error is OperationCanceledException) && !(error is WebSocketException) && !(error is SocketException) && !(error is HttpListenerException))
		{
			return error is IOException;
		}
		return true;
	}

	private void ThrowIfConvertibleException(string methodName, Exception exception, CancellationToken cancellationToken, bool aborted)
	{
		if (System.Net.NetEventSource.Log.IsEnabled() && !string.IsNullOrEmpty(methodName))
		{
			System.Net.NetEventSource.Error(this, $"methodName: {methodName}, exception: {exception}", "ThrowIfConvertibleException");
		}
		if (exception is OperationCanceledException)
		{
			if (cancellationToken.IsCancellationRequested || !aborted)
			{
				return;
			}
			ThrowIfAborted(aborted, exception);
		}
		WebSocketException ex2 = exception as WebSocketException;
		if (ex2 != null)
		{
			cancellationToken.ThrowIfCancellationRequested();
			ThrowIfAborted(aborted, ex2);
			return;
		}
		if (exception is SocketException ex3)
		{
			ex2 = new WebSocketException(ex3.NativeErrorCode, ex3);
		}
		if (exception is HttpListenerException ex4)
		{
			ex2 = new WebSocketException(ex4.ErrorCode, ex4);
		}
		if (exception is IOException innerException && exception.InnerException is SocketException ex5)
		{
			ex2 = new WebSocketException(ex5.NativeErrorCode, innerException);
		}
		if (ex2 != null)
		{
			cancellationToken.ThrowIfCancellationRequested();
			ThrowIfAborted(aborted, ex2);
			throw ex2;
		}
		if (!(exception is AggregateException ex6))
		{
			return;
		}
		ReadOnlyCollection<Exception> innerExceptions = ex6.Flatten().InnerExceptions;
		if (innerExceptions.Count == 0)
		{
			return;
		}
		foreach (Exception item in innerExceptions)
		{
			ThrowIfConvertibleException(null, item, cancellationToken, aborted);
		}
	}

	private void CleanUp()
	{
		if (_cleanedUp)
		{
			return;
		}
		_cleanedUp = true;
		if (SessionHandle != null)
		{
			SessionHandle.Dispose();
		}
		if (_internalBuffer != null)
		{
			_internalBuffer.Dispose(State);
		}
		if (_receiveOutstandingOperationHelper != null)
		{
			_receiveOutstandingOperationHelper.Dispose();
		}
		if (_sendOutstandingOperationHelper != null)
		{
			_sendOutstandingOperationHelper.Dispose();
		}
		if (_closeOutputOutstandingOperationHelper != null)
		{
			_closeOutputOutstandingOperationHelper.Dispose();
		}
		if (_closeOutstandingOperationHelper != null)
		{
			_closeOutstandingOperationHelper.Dispose();
		}
		if (_innerStream != null)
		{
			try
			{
				_innerStream.Close();
			}
			catch (ObjectDisposedException)
			{
			}
			catch (IOException)
			{
			}
			catch (SocketException)
			{
			}
			catch (HttpListenerException)
			{
			}
		}
		_keepAliveTracker.Dispose();
	}

	private void OnBackgroundTaskException(Exception exception)
	{
		if (Interlocked.CompareExchange(ref _pendingException, exception, null) == null)
		{
			if (System.Net.NetEventSource.Log.IsEnabled())
			{
				System.Net.NetEventSource.Error(this, exception.ToString(), "OnBackgroundTaskException");
			}
			Abort();
		}
	}

	private void ThrowIfPendingException()
	{
		Exception ex = Interlocked.Exchange(ref _pendingException, null);
		if (ex != null)
		{
			throw new WebSocketException(WebSocketError.Faulted, ex);
		}
	}

	private void ThrowIfDisposed()
	{
		if (_isDisposed)
		{
			throw new ObjectDisposedException(GetType().FullName);
		}
	}

	private void UpdateReceiveState(int newReceiveState, int expectedReceiveState)
	{
		int num = Interlocked.Exchange(ref _receiveState, newReceiveState);
	}

	private bool StartOnCloseReceived(ref bool thisLockTaken)
	{
		ThrowIfDisposed();
		if (WebSocket.IsStateTerminal(State) || State == WebSocketState.CloseReceived)
		{
			return false;
		}
		Monitor.Enter(_thisLock, ref thisLockTaken);
		if (WebSocket.IsStateTerminal(State) || State == WebSocketState.CloseReceived)
		{
			return false;
		}
		if (State == WebSocketState.Open)
		{
			_state = WebSocketState.CloseReceived;
			if (_closeReceivedTaskCompletionSource == null)
			{
				_closeReceivedTaskCompletionSource = new TaskCompletionSource();
			}
			return false;
		}
		return true;
	}

	private void FinishOnCloseReceived(WebSocketCloseStatus closeStatus, string closeStatusDescription)
	{
		_closeReceivedTaskCompletionSource?.TrySetResult();
		_closeStatus = closeStatus;
		_closeStatusDescription = closeStatusDescription;
		if (System.Net.NetEventSource.Log.IsEnabled())
		{
			System.Net.NetEventSource.Info(this, $"closeStatus: {closeStatus}, closeStatusDescription: {closeStatusDescription}, _State: {_state}", "FinishOnCloseReceived");
		}
	}

	private static async void OnKeepAlive(object sender)
	{
		WebSocketBase thisPtr = sender as WebSocketBase;
		bool lockTaken = false;
		CancellationToken linkedCancellationToken = CancellationToken.None;
		try
		{
			Monitor.Enter(thisPtr.SessionHandle, ref lockTaken);
			if (thisPtr._isDisposed || thisPtr._state != WebSocketState.Open || thisPtr._closeOutputTask != null || !thisPtr._keepAliveTracker.ShouldSendKeepAlive())
			{
				return;
			}
			bool ownsCancellationTokenSource = false;
			try
			{
				ownsCancellationTokenSource = thisPtr._sendOutstandingOperationHelper.TryStartOperation(CancellationToken.None, out linkedCancellationToken);
				if (ownsCancellationTokenSource)
				{
					thisPtr.EnsureKeepAliveOperation();
					thisPtr._keepAliveTask = thisPtr._keepAliveOperation.Process(null, linkedCancellationToken);
					ReleaseLock(thisPtr.SessionHandle, ref lockTaken);
					await thisPtr._keepAliveTask.SuppressContextFlow();
				}
			}
			finally
			{
				if (!lockTaken)
				{
					Monitor.Enter(thisPtr.SessionHandle, ref lockTaken);
				}
				thisPtr._sendOutstandingOperationHelper.CompleteOperation(ownsCancellationTokenSource);
				thisPtr._keepAliveTask = null;
			}
			thisPtr._keepAliveTracker.ResetTimer();
		}
		catch (Exception exception)
		{
			try
			{
				thisPtr.ThrowIfConvertibleException("OnKeepAlive", exception, CancellationToken.None, linkedCancellationToken.IsCancellationRequested);
				throw;
			}
			catch (Exception exception2)
			{
				thisPtr.OnBackgroundTaskException(exception2);
			}
		}
		finally
		{
			ReleaseLock(thisPtr.SessionHandle, ref lockTaken);
		}
	}
}
