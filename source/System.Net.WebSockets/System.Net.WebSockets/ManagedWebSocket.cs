using System.Buffers;
using System.Diagnostics;
using System.IO;
using System.Net.WebSockets.Compression;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace System.Net.WebSockets;

internal sealed class ManagedWebSocket : WebSocket
{
	private sealed class Utf8MessageState
	{
		internal bool SequenceInProgress;

		internal int AdditionalBytesExpected;

		internal int ExpectedValueMin;

		internal int CurrentDecodeBits;
	}

	private enum MessageOpcode : byte
	{
		Continuation = 0,
		Text = 1,
		Binary = 2,
		Close = 8,
		Ping = 9,
		Pong = 10
	}

	[StructLayout(LayoutKind.Auto)]
	private struct MessageHeader
	{
		internal MessageOpcode Opcode;

		internal bool Fin;

		internal long PayloadLength;

		internal bool Compressed;

		internal int Mask;

		internal bool Processed { get; set; }

		internal bool EndOfMessage
		{
			get
			{
				if (Fin && Processed)
				{
					return PayloadLength == 0;
				}
				return false;
			}
		}
	}

	[StructLayout(LayoutKind.Auto)]
	[CompilerGenerated]
	private struct _003CReceiveAsyncPrivate_003Ed__63<TResult> : IAsyncStateMachine
	{
		public int _003C_003E1__state;

		public PoolingAsyncValueTaskMethodBuilder<TResult> _003C_003Et__builder;

		public CancellationToken cancellationToken;

		public ManagedWebSocket _003C_003E4__this;

		public Memory<byte> payloadBuffer;

		private CancellationTokenRegistration _003Cregistration_003E5__2;

		private ConfiguredTaskAwaitable.ConfiguredTaskAwaiter _003C_003Eu__1;

		private MessageHeader _003Cheader_003E5__3;

		private int _003CtotalBytesReceived_003E5__4;

		private ConfiguredValueTaskAwaitable.ConfiguredValueTaskAwaiter _003C_003Eu__2;

		private int _003Climit_003E5__5;

		private ConfiguredValueTaskAwaitable<int>.ConfiguredValueTaskAwaiter _003C_003Eu__3;

		private void MoveNext()
		{
			int num = _003C_003E1__state;
			ManagedWebSocket managedWebSocket = _003C_003E4__this;
			TResult receiveResult;
			try
			{
				if ((uint)num > 7u)
				{
					_003Cregistration_003E5__2 = cancellationToken.Register(delegate(object s)
					{
						((ManagedWebSocket)s).Abort();
					}, managedWebSocket);
				}
				try
				{
					ConfiguredTaskAwaitable.ConfiguredTaskAwaiter awaiter;
					if (num != 0)
					{
						if ((uint)(num - 1) <= 6u)
						{
							goto IL_00be;
						}
						awaiter = managedWebSocket._receiveMutex.EnterAsync(cancellationToken).ConfigureAwait(continueOnCapturedContext: false).GetAwaiter();
						if (!awaiter.IsCompleted)
						{
							num = (_003C_003E1__state = 0);
							_003C_003Eu__1 = awaiter;
							_003C_003Et__builder.AwaitUnsafeOnCompleted(ref awaiter, ref this);
							return;
						}
					}
					else
					{
						awaiter = _003C_003Eu__1;
						_003C_003Eu__1 = default(ConfiguredTaskAwaitable.ConfiguredTaskAwaiter);
						num = (_003C_003E1__state = -1);
					}
					awaiter.GetResult();
					goto IL_00be;
					IL_00be:
					try
					{
						ConfiguredValueTaskAwaitable.ConfiguredValueTaskAwaiter awaiter8;
						ConfiguredValueTaskAwaitable.ConfiguredValueTaskAwaiter awaiter7;
						ConfiguredValueTaskAwaitable.ConfiguredValueTaskAwaiter awaiter6;
						ConfiguredValueTaskAwaitable.ConfiguredValueTaskAwaiter awaiter5;
						ConfiguredValueTaskAwaitable.ConfiguredValueTaskAwaiter awaiter4;
						ConfiguredValueTaskAwaitable<int>.ConfiguredValueTaskAwaiter awaiter3;
						ConfiguredValueTaskAwaitable.ConfiguredValueTaskAwaiter awaiter2;
						Span<byte> span;
						int result;
						int num2;
						long num4;
						string text;
						switch (num)
						{
						default:
							_003Cheader_003E5__3 = managedWebSocket._lastReceiveHeader;
							if (_003Cheader_003E5__3.Processed)
							{
								if (managedWebSocket._receiveBufferCount < (managedWebSocket._isServer ? 14 : 10))
								{
									if (managedWebSocket._receiveBufferCount < 2)
									{
										awaiter8 = managedWebSocket.EnsureBufferContainsAsync(2, cancellationToken).ConfigureAwait(continueOnCapturedContext: false).GetAwaiter();
										if (!awaiter8.IsCompleted)
										{
											num = (_003C_003E1__state = 1);
											_003C_003Eu__2 = awaiter8;
											_003C_003Et__builder.AwaitUnsafeOnCompleted(ref awaiter8, ref this);
											return;
										}
										goto IL_018d;
									}
									goto IL_0194;
								}
								goto IL_0264;
							}
							goto IL_0323;
						case 1:
							awaiter8 = _003C_003Eu__2;
							_003C_003Eu__2 = default(ConfiguredValueTaskAwaitable.ConfiguredValueTaskAwaiter);
							num = (_003C_003E1__state = -1);
							goto IL_018d;
						case 2:
							awaiter7 = _003C_003Eu__2;
							_003C_003Eu__2 = default(ConfiguredValueTaskAwaitable.ConfiguredValueTaskAwaiter);
							num = (_003C_003E1__state = -1);
							goto IL_025d;
						case 3:
							awaiter6 = _003C_003Eu__2;
							_003C_003Eu__2 = default(ConfiguredValueTaskAwaitable.ConfiguredValueTaskAwaiter);
							num = (_003C_003E1__state = -1);
							goto IL_02e4;
						case 4:
							awaiter5 = _003C_003Eu__2;
							_003C_003Eu__2 = default(ConfiguredValueTaskAwaitable.ConfiguredValueTaskAwaiter);
							num = (_003C_003E1__state = -1);
							goto IL_03b2;
						case 5:
							awaiter4 = _003C_003Eu__2;
							_003C_003Eu__2 = default(ConfiguredValueTaskAwaitable.ConfiguredValueTaskAwaiter);
							num = (_003C_003E1__state = -1);
							goto IL_0440;
						case 6:
							awaiter3 = _003C_003Eu__3;
							_003C_003Eu__3 = default(ConfiguredValueTaskAwaitable<int>.ConfiguredValueTaskAwaiter);
							num = (_003C_003E1__state = -1);
							goto IL_06b5;
						case 7:
							{
								awaiter2 = _003C_003Eu__2;
								_003C_003Eu__2 = default(ConfiguredValueTaskAwaitable.ConfiguredValueTaskAwaiter);
								num = (_003C_003E1__state = -1);
								goto IL_08c1;
							}
							IL_06f0:
							if (managedWebSocket._isServer)
							{
								Span<byte> toMask;
								if (!_003Cheader_003E5__3.Compressed)
								{
									span = payloadBuffer.Span;
									toMask = span.Slice(0, _003CtotalBytesReceived_003E5__4);
								}
								else
								{
									span = managedWebSocket._inflater.Span;
									toMask = span.Slice(0, _003CtotalBytesReceived_003E5__4);
								}
								managedWebSocket._receivedMaskOffsetOffset = ApplyMask(toMask, _003Cheader_003E5__3.Mask, managedWebSocket._receivedMaskOffsetOffset);
							}
							_003Cheader_003E5__3.PayloadLength -= _003CtotalBytesReceived_003E5__4;
							if (_003Cheader_003E5__3.Compressed)
							{
								managedWebSocket._inflater.AddBytes(_003CtotalBytesReceived_003E5__4, _003Cheader_003E5__3.Fin && _003Cheader_003E5__3.PayloadLength == 0);
							}
							goto IL_07ac;
							IL_06b5:
							result = awaiter3.GetResult();
							num2 = result;
							if (num2 <= 0)
							{
								managedWebSocket.ThrowIfEOFUnexpected(throwOnPrematureClosure: true);
								goto IL_06f0;
							}
							_003CtotalBytesReceived_003E5__4 += num2;
							goto IL_06df;
							IL_0323:
							if (_003Cheader_003E5__3.Opcode == MessageOpcode.Ping || _003Cheader_003E5__3.Opcode == MessageOpcode.Pong)
							{
								awaiter5 = managedWebSocket.HandleReceivedPingPongAsync(_003Cheader_003E5__3, cancellationToken).ConfigureAwait(continueOnCapturedContext: false).GetAwaiter();
								if (!awaiter5.IsCompleted)
								{
									num = (_003C_003E1__state = 4);
									_003C_003Eu__2 = awaiter5;
									_003C_003Et__builder.AwaitUnsafeOnCompleted(ref awaiter5, ref this);
									return;
								}
								goto IL_03b2;
							}
							if (_003Cheader_003E5__3.Opcode == MessageOpcode.Close)
							{
								awaiter4 = managedWebSocket.HandleReceivedCloseAsync(_003Cheader_003E5__3, cancellationToken).ConfigureAwait(continueOnCapturedContext: false).GetAwaiter();
								if (!awaiter4.IsCompleted)
								{
									num = (_003C_003E1__state = 5);
									_003C_003Eu__2 = awaiter4;
									_003C_003Et__builder.AwaitUnsafeOnCompleted(ref awaiter4, ref this);
									return;
								}
								goto IL_0440;
							}
							if (_003Cheader_003E5__3.Opcode == MessageOpcode.Continuation)
							{
								_003Cheader_003E5__3.Opcode = managedWebSocket._lastReceiveHeader.Opcode;
								_003Cheader_003E5__3.Compressed = managedWebSocket._lastReceiveHeader.Compressed;
							}
							if (!_003Cheader_003E5__3.Processed && payloadBuffer.Length != 0)
							{
								_003CtotalBytesReceived_003E5__4 = 0;
								if (_003Cheader_003E5__3.PayloadLength > 0)
								{
									if (_003Cheader_003E5__3.Compressed)
									{
										managedWebSocket._inflater.Prepare(_003Cheader_003E5__3.PayloadLength, payloadBuffer.Length);
									}
									int length;
									if (!_003Cheader_003E5__3.Compressed)
									{
										length = payloadBuffer.Length;
									}
									else
									{
										span = managedWebSocket._inflater.Span;
										length = span.Length;
									}
									_003Climit_003E5__5 = (int)Math.Min(length, _003Cheader_003E5__3.PayloadLength);
									if (managedWebSocket._receiveBufferCount > 0)
									{
										int num3 = Math.Min(_003Climit_003E5__5, managedWebSocket._receiveBufferCount);
										span = managedWebSocket._receiveBuffer.Span;
										span = span.Slice(managedWebSocket._receiveBufferOffset, num3);
										span.CopyTo(_003Cheader_003E5__3.Compressed ? managedWebSocket._inflater.Span : payloadBuffer.Span);
										managedWebSocket.ConsumeFromBuffer(num3);
										_003CtotalBytesReceived_003E5__4 += num3;
									}
									goto IL_06df;
								}
								goto IL_07ac;
							}
							managedWebSocket._lastReceiveHeader = _003Cheader_003E5__3;
							receiveResult = managedWebSocket.GetReceiveResult<TResult>(0, (_003Cheader_003E5__3.Opcode != MessageOpcode.Text) ? WebSocketMessageType.Binary : WebSocketMessageType.Text, _003Cheader_003E5__3.EndOfMessage);
							goto end_IL_00be;
							IL_07ac:
							if (_003Cheader_003E5__3.Compressed)
							{
								_003Cheader_003E5__3.Processed = managedWebSocket._inflater.Inflate(payloadBuffer.Span, out _003CtotalBytesReceived_003E5__4) && _003Cheader_003E5__3.PayloadLength == 0;
							}
							else
							{
								_003Cheader_003E5__3.Processed = _003Cheader_003E5__3.PayloadLength == 0;
							}
							if (_003Cheader_003E5__3.Opcode != MessageOpcode.Text)
							{
								break;
							}
							span = payloadBuffer.Span;
							if (TryValidateUtf8(span.Slice(0, _003CtotalBytesReceived_003E5__4), _003Cheader_003E5__3.EndOfMessage, managedWebSocket._utf8TextState))
							{
								break;
							}
							awaiter2 = managedWebSocket.CloseWithReceiveErrorAndThrowAsync(WebSocketCloseStatus.InvalidPayloadData, WebSocketError.Faulted).ConfigureAwait(continueOnCapturedContext: false).GetAwaiter();
							if (!awaiter2.IsCompleted)
							{
								num = (_003C_003E1__state = 7);
								_003C_003Eu__2 = awaiter2;
								_003C_003Et__builder.AwaitUnsafeOnCompleted(ref awaiter2, ref this);
								return;
							}
							goto IL_08c1;
							IL_06df:
							if (_003CtotalBytesReceived_003E5__4 < _003Climit_003E5__5)
							{
								awaiter3 = managedWebSocket._stream.ReadAsync(_003Cheader_003E5__3.Compressed ? managedWebSocket._inflater.Memory.Slice(_003CtotalBytesReceived_003E5__4, _003Climit_003E5__5 - _003CtotalBytesReceived_003E5__4) : payloadBuffer.Slice(_003CtotalBytesReceived_003E5__4, _003Climit_003E5__5 - _003CtotalBytesReceived_003E5__4), cancellationToken).ConfigureAwait(continueOnCapturedContext: false).GetAwaiter();
								if (!awaiter3.IsCompleted)
								{
									num = (_003C_003E1__state = 6);
									_003C_003Eu__3 = awaiter3;
									_003C_003Et__builder.AwaitUnsafeOnCompleted(ref awaiter3, ref this);
									return;
								}
								goto IL_06b5;
							}
							goto IL_06f0;
							IL_03b2:
							awaiter5.GetResult();
							goto default;
							IL_025d:
							awaiter7.GetResult();
							goto IL_0264;
							IL_018d:
							awaiter8.GetResult();
							goto IL_0194;
							IL_0194:
							span = managedWebSocket._receiveBuffer.Span;
							num4 = span[managedWebSocket._receiveBufferOffset + 1] & 0x7F;
							if (managedWebSocket._isServer || num4 > 125)
							{
								int minimumRequiredBytes = 2 + (managedWebSocket._isServer ? 4 : 0) + ((num4 > 125) ? ((num4 == 126) ? 2 : 8) : 0);
								awaiter7 = managedWebSocket.EnsureBufferContainsAsync(minimumRequiredBytes, cancellationToken).ConfigureAwait(continueOnCapturedContext: false).GetAwaiter();
								if (!awaiter7.IsCompleted)
								{
									num = (_003C_003E1__state = 2);
									_003C_003Eu__2 = awaiter7;
									_003C_003Et__builder.AwaitUnsafeOnCompleted(ref awaiter7, ref this);
									return;
								}
								goto IL_025d;
							}
							goto IL_0264;
							IL_08c1:
							awaiter2.GetResult();
							break;
							IL_0264:
							text = managedWebSocket.TryParseMessageHeaderFromReceiveBuffer(out _003Cheader_003E5__3);
							if (text != null)
							{
								awaiter6 = managedWebSocket.CloseWithReceiveErrorAndThrowAsync(WebSocketCloseStatus.ProtocolError, WebSocketError.Faulted, text).ConfigureAwait(continueOnCapturedContext: false).GetAwaiter();
								if (!awaiter6.IsCompleted)
								{
									num = (_003C_003E1__state = 3);
									_003C_003Eu__2 = awaiter6;
									_003C_003Et__builder.AwaitUnsafeOnCompleted(ref awaiter6, ref this);
									return;
								}
								goto IL_02e4;
							}
							goto IL_02eb;
							IL_02e4:
							awaiter6.GetResult();
							goto IL_02eb;
							IL_02eb:
							managedWebSocket._receivedMaskOffsetOffset = 0;
							if (_003Cheader_003E5__3.PayloadLength == 0L && _003Cheader_003E5__3.Compressed)
							{
								managedWebSocket._inflater.AddBytes(0, _003Cheader_003E5__3.Fin);
							}
							goto IL_0323;
							IL_0440:
							awaiter4.GetResult();
							receiveResult = managedWebSocket.GetReceiveResult<TResult>(0, WebSocketMessageType.Close, endOfMessage: true);
							goto end_IL_00be;
						}
						managedWebSocket._lastReceiveHeader = _003Cheader_003E5__3;
						receiveResult = managedWebSocket.GetReceiveResult<TResult>(_003CtotalBytesReceived_003E5__4, (_003Cheader_003E5__3.Opcode != MessageOpcode.Text) ? WebSocketMessageType.Binary : WebSocketMessageType.Text, _003Cheader_003E5__3.EndOfMessage);
						end_IL_00be:;
					}
					finally
					{
						if (num < 0)
						{
							managedWebSocket._receiveMutex.Exit();
						}
					}
				}
				catch (Exception ex) when (!(ex is OperationCanceledException))
				{
					if (managedWebSocket._state == WebSocketState.Aborted)
					{
						throw new OperationCanceledException("Aborted", ex);
					}
					managedWebSocket.OnAborted();
					if (ex is WebSocketException)
					{
						throw;
					}
					throw new WebSocketException(WebSocketError.ConnectionClosedPrematurely, ex);
				}
				finally
				{
					if (num < 0)
					{
						_003Cregistration_003E5__2.Dispose();
					}
				}
			}
			catch (Exception exception)
			{
				_003C_003E1__state = -2;
				_003Cregistration_003E5__2 = default(CancellationTokenRegistration);
				_003C_003Et__builder.SetException(exception);
				return;
			}
			_003C_003E1__state = -2;
			_003Cregistration_003E5__2 = default(CancellationTokenRegistration);
			_003C_003Et__builder.SetResult(receiveResult);
		}

		void IAsyncStateMachine.MoveNext()
		{
			//ILSpy generated this explicit interface implementation from .override directive in MoveNext
			this.MoveNext();
		}

		[DebuggerHidden]
		private void SetStateMachine(IAsyncStateMachine stateMachine)
		{
			_003C_003Et__builder.SetStateMachine(stateMachine);
		}

		void IAsyncStateMachine.SetStateMachine(IAsyncStateMachine stateMachine)
		{
			//ILSpy generated this explicit interface implementation from .override directive in SetStateMachine
			this.SetStateMachine(stateMachine);
		}
	}

	[StructLayout(LayoutKind.Auto)]
	[CompilerGenerated]
	private struct _003CEnsureBufferContainsAsync_003Ed__74 : IAsyncStateMachine
	{
		public int _003C_003E1__state;

		public PoolingAsyncValueTaskMethodBuilder _003C_003Et__builder;

		public ManagedWebSocket _003C_003E4__this;

		public int minimumRequiredBytes;

		public CancellationToken cancellationToken;

		public bool throwOnPrematureClosure;

		private ConfiguredValueTaskAwaitable<int>.ConfiguredValueTaskAwaiter _003C_003Eu__1;

		private void MoveNext()
		{
			int num = _003C_003E1__state;
			ManagedWebSocket managedWebSocket = _003C_003E4__this;
			try
			{
				ConfiguredValueTaskAwaitable<int>.ConfiguredValueTaskAwaiter awaiter;
				if (num == 0)
				{
					awaiter = _003C_003Eu__1;
					_003C_003Eu__1 = default(ConfiguredValueTaskAwaitable<int>.ConfiguredValueTaskAwaiter);
					num = (_003C_003E1__state = -1);
					goto IL_00ff;
				}
				if (managedWebSocket._receiveBufferCount < minimumRequiredBytes)
				{
					if (managedWebSocket._receiveBufferCount > 0)
					{
						Span<byte> span = managedWebSocket._receiveBuffer.Span;
						span = span.Slice(managedWebSocket._receiveBufferOffset, managedWebSocket._receiveBufferCount);
						span.CopyTo(managedWebSocket._receiveBuffer.Span);
					}
					managedWebSocket._receiveBufferOffset = 0;
					goto IL_012b;
				}
				goto end_IL_000e;
				IL_00ff:
				int result = awaiter.GetResult();
				int num2 = result;
				if (num2 > 0)
				{
					managedWebSocket._receiveBufferCount += num2;
					goto IL_012b;
				}
				managedWebSocket.ThrowIfEOFUnexpected(throwOnPrematureClosure);
				goto end_IL_000e;
				IL_012b:
				if (managedWebSocket._receiveBufferCount < minimumRequiredBytes)
				{
					awaiter = managedWebSocket._stream.ReadAsync(managedWebSocket._receiveBuffer.Slice(managedWebSocket._receiveBufferCount, managedWebSocket._receiveBuffer.Length - managedWebSocket._receiveBufferCount), cancellationToken).ConfigureAwait(continueOnCapturedContext: false).GetAwaiter();
					if (!awaiter.IsCompleted)
					{
						num = (_003C_003E1__state = 0);
						_003C_003Eu__1 = awaiter;
						_003C_003Et__builder.AwaitUnsafeOnCompleted(ref awaiter, ref this);
						return;
					}
					goto IL_00ff;
				}
				end_IL_000e:;
			}
			catch (Exception exception)
			{
				_003C_003E1__state = -2;
				_003C_003Et__builder.SetException(exception);
				return;
			}
			_003C_003E1__state = -2;
			_003C_003Et__builder.SetResult();
		}

		void IAsyncStateMachine.MoveNext()
		{
			//ILSpy generated this explicit interface implementation from .override directive in MoveNext
			this.MoveNext();
		}

		[DebuggerHidden]
		private void SetStateMachine(IAsyncStateMachine stateMachine)
		{
			_003C_003Et__builder.SetStateMachine(stateMachine);
		}

		void IAsyncStateMachine.SetStateMachine(IAsyncStateMachine stateMachine)
		{
			//ILSpy generated this explicit interface implementation from .override directive in SetStateMachine
			this.SetStateMachine(stateMachine);
		}
	}

	private static readonly UTF8Encoding s_textEncoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true);

	private static readonly WebSocketState[] s_validSendStates = new WebSocketState[2]
	{
		WebSocketState.Open,
		WebSocketState.CloseReceived
	};

	private static readonly WebSocketState[] s_validReceiveStates = new WebSocketState[2]
	{
		WebSocketState.Open,
		WebSocketState.CloseSent
	};

	private static readonly WebSocketState[] s_validCloseOutputStates = new WebSocketState[2]
	{
		WebSocketState.Open,
		WebSocketState.CloseReceived
	};

	private static readonly WebSocketState[] s_validCloseStates = new WebSocketState[3]
	{
		WebSocketState.Open,
		WebSocketState.CloseReceived,
		WebSocketState.CloseSent
	};

	private readonly Stream _stream;

	private readonly bool _isServer;

	private readonly string _subprotocol;

	private readonly Timer _keepAliveTimer;

	private readonly Memory<byte> _receiveBuffer;

	private readonly Utf8MessageState _utf8TextState = new Utf8MessageState();

	private readonly AsyncMutex _sendMutex = new AsyncMutex();

	private readonly AsyncMutex _receiveMutex = new AsyncMutex();

	private WebSocketState _state = WebSocketState.Open;

	private bool _disposed;

	private bool _sentCloseFrame;

	private bool _receivedCloseFrame;

	private WebSocketCloseStatus? _closeStatus;

	private string _closeStatusDescription;

	private MessageHeader _lastReceiveHeader = new MessageHeader
	{
		Opcode = MessageOpcode.Text,
		Fin = true,
		Processed = true
	};

	private int _receiveBufferOffset;

	private int _receiveBufferCount;

	private int _receivedMaskOffsetOffset;

	private byte[] _sendBuffer;

	private bool _lastSendWasFragment;

	private bool _lastSendHadDisableCompression;

	private readonly WebSocketInflater _inflater;

	private readonly WebSocketDeflater _deflater;

	private object StateUpdateLock => _sendMutex;

	public override WebSocketCloseStatus? CloseStatus => _closeStatus;

	public override string CloseStatusDescription => _closeStatusDescription;

	public override WebSocketState State => _state;

	public override string SubProtocol => _subprotocol;

	internal ManagedWebSocket(Stream stream, bool isServer, string subprotocol, TimeSpan keepAliveInterval)
	{
		_stream = stream;
		_isServer = isServer;
		_subprotocol = subprotocol;
		_receiveBuffer = new byte[125];
		if (!(keepAliveInterval > TimeSpan.Zero))
		{
			return;
		}
		_keepAliveTimer = new Timer(delegate(object s)
		{
			WeakReference<ManagedWebSocket> weakReference = (WeakReference<ManagedWebSocket>)s;
			if (weakReference.TryGetTarget(out var target))
			{
				target.SendKeepAliveFrameAsync();
			}
		}, new WeakReference<ManagedWebSocket>(this), keepAliveInterval, keepAliveInterval);
	}

	internal ManagedWebSocket(Stream stream, WebSocketCreationOptions options)
		: this(stream, options.IsServer, options.SubProtocol, options.KeepAliveInterval)
	{
		WebSocketDeflateOptions dangerousDeflateOptions = options.DangerousDeflateOptions;
		if (dangerousDeflateOptions != null)
		{
			if (options.IsServer)
			{
				_inflater = new WebSocketInflater(dangerousDeflateOptions.ClientMaxWindowBits, dangerousDeflateOptions.ClientContextTakeover);
				_deflater = new WebSocketDeflater(dangerousDeflateOptions.ServerMaxWindowBits, dangerousDeflateOptions.ServerContextTakeover);
			}
			else
			{
				_inflater = new WebSocketInflater(dangerousDeflateOptions.ServerMaxWindowBits, dangerousDeflateOptions.ServerContextTakeover);
				_deflater = new WebSocketDeflater(dangerousDeflateOptions.ClientMaxWindowBits, dangerousDeflateOptions.ClientContextTakeover);
			}
		}
	}

	public override void Dispose()
	{
		lock (StateUpdateLock)
		{
			DisposeCore();
		}
	}

	private void DisposeCore()
	{
		if (!_disposed)
		{
			_disposed = true;
			_keepAliveTimer?.Dispose();
			_stream.Dispose();
			_inflater?.Dispose();
			_deflater?.Dispose();
			if (_state < WebSocketState.Aborted)
			{
				_state = WebSocketState.Closed;
			}
		}
	}

	public override Task SendAsync(ArraySegment<byte> buffer, WebSocketMessageType messageType, bool endOfMessage, CancellationToken cancellationToken)
	{
		if (messageType != 0 && messageType != WebSocketMessageType.Binary)
		{
			throw new ArgumentException(System.SR.Format(System.SR.net_WebSockets_Argument_InvalidMessageType, "Close", "SendAsync", "Binary", "Text", "CloseOutputAsync"), "messageType");
		}
		WebSocketValidate.ValidateArraySegment(buffer, "buffer");
		return SendAsync(buffer, messageType, endOfMessage ? WebSocketMessageFlags.EndOfMessage : WebSocketMessageFlags.None, cancellationToken).AsTask();
	}

	public override ValueTask SendAsync(ReadOnlyMemory<byte> buffer, WebSocketMessageType messageType, bool endOfMessage, CancellationToken cancellationToken)
	{
		return SendAsync(buffer, messageType, endOfMessage ? WebSocketMessageFlags.EndOfMessage : WebSocketMessageFlags.None, cancellationToken);
	}

	public override ValueTask SendAsync(ReadOnlyMemory<byte> buffer, WebSocketMessageType messageType, WebSocketMessageFlags messageFlags, CancellationToken cancellationToken)
	{
		if (messageType != 0 && messageType != WebSocketMessageType.Binary)
		{
			throw new ArgumentException(System.SR.Format(System.SR.net_WebSockets_Argument_InvalidMessageType, "Close", "SendAsync", "Binary", "Text", "CloseOutputAsync"), "messageType");
		}
		try
		{
			WebSocketValidate.ThrowIfInvalidState(_state, _disposed, s_validSendStates);
		}
		catch (Exception exception)
		{
			return new ValueTask(Task.FromException(exception));
		}
		bool flag = messageFlags.HasFlag(WebSocketMessageFlags.EndOfMessage);
		bool flag2 = messageFlags.HasFlag(WebSocketMessageFlags.DisableCompression);
		MessageOpcode opcode;
		if (_lastSendWasFragment)
		{
			if (_lastSendHadDisableCompression != flag2)
			{
				throw new ArgumentException(System.SR.net_WebSockets_Argument_MessageFlagsHasDifferentCompressionOptions, "messageFlags");
			}
			opcode = MessageOpcode.Continuation;
		}
		else
		{
			opcode = ((messageType != WebSocketMessageType.Binary) ? MessageOpcode.Text : MessageOpcode.Binary);
		}
		ValueTask result = SendFrameAsync(opcode, flag, flag2, buffer, cancellationToken);
		_lastSendWasFragment = !flag;
		_lastSendHadDisableCompression = flag2;
		return result;
	}

	public override Task<WebSocketReceiveResult> ReceiveAsync(ArraySegment<byte> buffer, CancellationToken cancellationToken)
	{
		WebSocketValidate.ValidateArraySegment(buffer, "buffer");
		try
		{
			WebSocketValidate.ThrowIfInvalidState(_state, _disposed, s_validReceiveStates);
			return ReceiveAsyncPrivate<WebSocketReceiveResult>(buffer, cancellationToken).AsTask();
		}
		catch (Exception exception)
		{
			return Task.FromException<WebSocketReceiveResult>(exception);
		}
	}

	public override ValueTask<ValueWebSocketReceiveResult> ReceiveAsync(Memory<byte> buffer, CancellationToken cancellationToken)
	{
		try
		{
			WebSocketValidate.ThrowIfInvalidState(_state, _disposed, s_validReceiveStates);
			return ReceiveAsyncPrivate<ValueWebSocketReceiveResult>(buffer, cancellationToken);
		}
		catch (Exception exception)
		{
			return ValueTask.FromException<ValueWebSocketReceiveResult>(exception);
		}
	}

	public override Task CloseAsync(WebSocketCloseStatus closeStatus, string statusDescription, CancellationToken cancellationToken)
	{
		WebSocketValidate.ValidateCloseStatus(closeStatus, statusDescription);
		try
		{
			WebSocketValidate.ThrowIfInvalidState(_state, _disposed, s_validCloseStates);
		}
		catch (Exception exception)
		{
			return Task.FromException(exception);
		}
		return CloseAsyncPrivate(closeStatus, statusDescription, cancellationToken);
	}

	public override Task CloseOutputAsync(WebSocketCloseStatus closeStatus, string statusDescription, CancellationToken cancellationToken)
	{
		WebSocketValidate.ValidateCloseStatus(closeStatus, statusDescription);
		return CloseOutputAsyncCore(closeStatus, statusDescription, cancellationToken);
	}

	private async Task CloseOutputAsyncCore(WebSocketCloseStatus closeStatus, string statusDescription, CancellationToken cancellationToken)
	{
		WebSocketValidate.ThrowIfInvalidState(_state, _disposed, s_validCloseOutputStates);
		await SendCloseFrameAsync(closeStatus, statusDescription, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
		lock (StateUpdateLock)
		{
			if (_receivedCloseFrame)
			{
				DisposeCore();
			}
		}
	}

	public override void Abort()
	{
		OnAborted();
		Dispose();
	}

	private void OnAborted()
	{
		lock (StateUpdateLock)
		{
			WebSocketState state = _state;
			if (state != WebSocketState.Closed && state != WebSocketState.Aborted)
			{
				_state = ((state != 0 && state != WebSocketState.Connecting) ? WebSocketState.Aborted : WebSocketState.Closed);
			}
		}
	}

	private ValueTask SendFrameAsync(MessageOpcode opcode, bool endOfMessage, bool disableCompression, ReadOnlyMemory<byte> payloadBuffer, CancellationToken cancellationToken)
	{
		Task task = _sendMutex.EnterAsync(cancellationToken);
		if (!cancellationToken.CanBeCanceled && task.IsCompletedSuccessfully)
		{
			return SendFrameLockAcquiredNonCancelableAsync(opcode, endOfMessage, disableCompression, payloadBuffer);
		}
		return SendFrameFallbackAsync(opcode, endOfMessage, disableCompression, payloadBuffer, task, cancellationToken);
	}

	private ValueTask SendFrameLockAcquiredNonCancelableAsync(MessageOpcode opcode, bool endOfMessage, bool disableCompression, ReadOnlyMemory<byte> payloadBuffer)
	{
		ValueTask valueTask = default(ValueTask);
		bool flag = true;
		try
		{
			int length = WriteFrameToSendBuffer(opcode, endOfMessage, disableCompression, payloadBuffer.Span);
			valueTask = _stream.WriteAsync(new ReadOnlyMemory<byte>(_sendBuffer, 0, length));
			if (valueTask.IsCompleted)
			{
				return valueTask;
			}
			flag = false;
		}
		catch (Exception ex)
		{
			return new ValueTask(Task.FromException((ex is OperationCanceledException) ? ex : ((_state == WebSocketState.Aborted) ? CreateOperationCanceledException(ex) : new WebSocketException(WebSocketError.ConnectionClosedPrematurely, ex))));
		}
		finally
		{
			if (flag)
			{
				ReleaseSendBuffer();
				_sendMutex.Exit();
			}
		}
		return WaitForWriteTaskAsync(valueTask);
	}

	private async ValueTask WaitForWriteTaskAsync(ValueTask writeTask)
	{
		try
		{
			await writeTask.ConfigureAwait(continueOnCapturedContext: false);
		}
		catch (Exception ex) when (!(ex is OperationCanceledException))
		{
			throw (_state == WebSocketState.Aborted) ? CreateOperationCanceledException(ex) : new WebSocketException(WebSocketError.ConnectionClosedPrematurely, ex);
		}
		finally
		{
			ReleaseSendBuffer();
			_sendMutex.Exit();
		}
	}

	private async ValueTask SendFrameFallbackAsync(MessageOpcode opcode, bool endOfMessage, bool disableCompression, ReadOnlyMemory<byte> payloadBuffer, Task lockTask, CancellationToken cancellationToken)
	{
		await lockTask.ConfigureAwait(continueOnCapturedContext: false);
		try
		{
			int length = WriteFrameToSendBuffer(opcode, endOfMessage, disableCompression, payloadBuffer.Span);
			using (cancellationToken.Register(delegate(object s)
			{
				((ManagedWebSocket)s).Abort();
			}, this))
			{
				await _stream.WriteAsync(new ReadOnlyMemory<byte>(_sendBuffer, 0, length), cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
			}
		}
		catch (Exception ex) when (!(ex is OperationCanceledException))
		{
			throw (_state == WebSocketState.Aborted) ? CreateOperationCanceledException(ex, cancellationToken) : new WebSocketException(WebSocketError.ConnectionClosedPrematurely, ex);
		}
		finally
		{
			ReleaseSendBuffer();
			_sendMutex.Exit();
		}
	}

	private int WriteFrameToSendBuffer(MessageOpcode opcode, bool endOfMessage, bool disableCompression, ReadOnlySpan<byte> payloadBuffer)
	{
		if (_deflater != null && !disableCompression)
		{
			payloadBuffer = _deflater.Deflate(payloadBuffer, endOfMessage);
		}
		int length = payloadBuffer.Length;
		AllocateSendBuffer(length + 14);
		int? num = null;
		int num2;
		if (_isServer)
		{
			num2 = WriteHeader(opcode, _sendBuffer, payloadBuffer, endOfMessage, useMask: false, _deflater != null && !disableCompression);
		}
		else
		{
			num = WriteHeader(opcode, _sendBuffer, payloadBuffer, endOfMessage, useMask: true, _deflater != null && !disableCompression);
			num2 = num.GetValueOrDefault() + 4;
		}
		if (payloadBuffer.Length > 0)
		{
			payloadBuffer.CopyTo(new Span<byte>(_sendBuffer, num2, length));
			_deflater?.ReleaseBuffer();
			if (num.HasValue)
			{
				ApplyMask(new Span<byte>(_sendBuffer, num2, length), _sendBuffer, num.Value, 0);
			}
		}
		return num2 + length;
	}

	private void SendKeepAliveFrameAsync()
	{
		ValueTask valueTask = SendFrameAsync(MessageOpcode.Pong, endOfMessage: true, disableCompression: true, ReadOnlyMemory<byte>.Empty, CancellationToken.None);
		if (valueTask.IsCompletedSuccessfully)
		{
			valueTask.GetAwaiter().GetResult();
			return;
		}
		valueTask.AsTask().ContinueWith(delegate(Task p)
		{
			_ = p.Exception;
		}, CancellationToken.None, TaskContinuationOptions.OnlyOnFaulted | TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);
	}

	private static int WriteHeader(MessageOpcode opcode, byte[] sendBuffer, ReadOnlySpan<byte> payload, bool endOfMessage, bool useMask, bool compressed)
	{
		sendBuffer[0] = (byte)opcode;
		if (endOfMessage)
		{
			sendBuffer[0] |= 128;
		}
		if (compressed && opcode != 0)
		{
			sendBuffer[0] |= 64;
		}
		int num;
		if (payload.Length <= 125)
		{
			sendBuffer[1] = (byte)payload.Length;
			num = 2;
		}
		else if (payload.Length <= 65535)
		{
			sendBuffer[1] = 126;
			sendBuffer[2] = (byte)(payload.Length / 256);
			sendBuffer[3] = (byte)payload.Length;
			num = 4;
		}
		else
		{
			sendBuffer[1] = 127;
			int num2 = payload.Length;
			for (int num3 = 9; num3 >= 2; num3--)
			{
				sendBuffer[num3] = (byte)num2;
				num2 /= 256;
			}
			num = 10;
		}
		if (useMask)
		{
			sendBuffer[1] |= 128;
			WriteRandomMask(sendBuffer, num);
		}
		return num;
	}

	private static void WriteRandomMask(byte[] buffer, int offset)
	{
		RandomNumberGenerator.Fill(buffer.AsSpan(offset, 4));
	}

	[AsyncStateMachine(typeof(_003CReceiveAsyncPrivate_003Ed__63<>))]
	[AsyncMethodBuilder(typeof(PoolingAsyncValueTaskMethodBuilder<>))]
	private ValueTask<TResult> ReceiveAsyncPrivate<TResult>(Memory<byte> payloadBuffer, CancellationToken cancellationToken)
	{
		Unsafe.SkipInit(out _003CReceiveAsyncPrivate_003Ed__63<TResult> stateMachine);
		stateMachine._003C_003Et__builder = PoolingAsyncValueTaskMethodBuilder<TResult>.Create();
		stateMachine._003C_003E4__this = this;
		stateMachine.payloadBuffer = payloadBuffer;
		stateMachine.cancellationToken = cancellationToken;
		stateMachine._003C_003E1__state = -1;
		stateMachine._003C_003Et__builder.Start(ref stateMachine);
		return stateMachine._003C_003Et__builder.Task;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private TResult GetReceiveResult<TResult>(int count, WebSocketMessageType messageType, bool endOfMessage)
	{
		if (typeof(TResult) == typeof(ValueWebSocketReceiveResult))
		{
			return (TResult)(object)new ValueWebSocketReceiveResult(count, messageType, endOfMessage);
		}
		return (TResult)(object)new WebSocketReceiveResult(count, messageType, endOfMessage, _closeStatus, _closeStatusDescription);
	}

	private async ValueTask HandleReceivedCloseAsync(MessageHeader header, CancellationToken cancellationToken)
	{
		lock (StateUpdateLock)
		{
			_receivedCloseFrame = true;
			if (_sentCloseFrame && _state < WebSocketState.Closed)
			{
				_state = WebSocketState.Closed;
			}
			else if (_state < WebSocketState.CloseReceived)
			{
				_state = WebSocketState.CloseReceived;
			}
		}
		WebSocketCloseStatus closeStatus = WebSocketCloseStatus.NormalClosure;
		string closeStatusDescription = string.Empty;
		if (header.PayloadLength == 1)
		{
			await CloseWithReceiveErrorAndThrowAsync(WebSocketCloseStatus.ProtocolError, WebSocketError.Faulted).ConfigureAwait(continueOnCapturedContext: false);
		}
		else if (header.PayloadLength >= 2)
		{
			if (_receiveBufferCount < header.PayloadLength)
			{
				await EnsureBufferContainsAsync((int)header.PayloadLength, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
			}
			if (_isServer)
			{
				ApplyMask(_receiveBuffer.Span.Slice(_receiveBufferOffset, (int)header.PayloadLength), header.Mask, 0);
			}
			closeStatus = (WebSocketCloseStatus)((_receiveBuffer.Span[_receiveBufferOffset] << 8) | _receiveBuffer.Span[_receiveBufferOffset + 1]);
			if (!IsValidCloseStatus(closeStatus))
			{
				await CloseWithReceiveErrorAndThrowAsync(WebSocketCloseStatus.ProtocolError, WebSocketError.Faulted).ConfigureAwait(continueOnCapturedContext: false);
			}
			if (header.PayloadLength > 2)
			{
				try
				{
					closeStatusDescription = s_textEncoding.GetString(_receiveBuffer.Span.Slice(_receiveBufferOffset + 2, (int)header.PayloadLength - 2));
				}
				catch (DecoderFallbackException innerException)
				{
					await CloseWithReceiveErrorAndThrowAsync(WebSocketCloseStatus.ProtocolError, WebSocketError.Faulted, null, innerException).ConfigureAwait(continueOnCapturedContext: false);
				}
			}
			ConsumeFromBuffer((int)header.PayloadLength);
		}
		_closeStatus = closeStatus;
		_closeStatusDescription = closeStatusDescription;
		if (!_isServer && _sentCloseFrame)
		{
			await WaitForServerToCloseConnectionAsync(cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
		}
	}

	private async ValueTask WaitForServerToCloseConnectionAsync(CancellationToken cancellationToken)
	{
		ValueTask<int> valueTask = _stream.ReadAsync(_receiveBuffer, cancellationToken);
		if (valueTask.IsCompletedSuccessfully)
		{
			valueTask.GetAwaiter().GetResult();
			return;
		}
		try
		{
			await valueTask.AsTask().WaitAsync(TimeSpan.FromMilliseconds(1000.0)).ConfigureAwait(continueOnCapturedContext: false);
		}
		catch
		{
			Abort();
		}
	}

	private async ValueTask HandleReceivedPingPongAsync(MessageHeader header, CancellationToken cancellationToken)
	{
		if (header.PayloadLength > 0 && _receiveBufferCount < header.PayloadLength)
		{
			await EnsureBufferContainsAsync((int)header.PayloadLength, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
		}
		if (header.Opcode == MessageOpcode.Ping)
		{
			if (_isServer)
			{
				ApplyMask(_receiveBuffer.Span.Slice(_receiveBufferOffset, (int)header.PayloadLength), header.Mask, 0);
			}
			await SendFrameAsync(MessageOpcode.Pong, endOfMessage: true, disableCompression: true, _receiveBuffer.Slice(_receiveBufferOffset, (int)header.PayloadLength), cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
		}
		if (header.PayloadLength > 0)
		{
			ConsumeFromBuffer((int)header.PayloadLength);
		}
	}

	private static bool IsValidCloseStatus(WebSocketCloseStatus closeStatus)
	{
		if (closeStatus < WebSocketCloseStatus.NormalClosure || closeStatus >= (WebSocketCloseStatus)5000)
		{
			return false;
		}
		if (closeStatus >= (WebSocketCloseStatus)3000)
		{
			return true;
		}
		if ((uint)(closeStatus - 1000) <= 3u || (uint)(closeStatus - 1007) <= 4u)
		{
			return true;
		}
		return false;
	}

	private async ValueTask CloseWithReceiveErrorAndThrowAsync(WebSocketCloseStatus closeStatus, WebSocketError error, string errorMessage = null, Exception innerException = null)
	{
		if (!_sentCloseFrame)
		{
			await CloseOutputAsync(closeStatus, string.Empty, default(CancellationToken)).ConfigureAwait(continueOnCapturedContext: false);
		}
		_receiveBufferCount = 0;
		throw (errorMessage != null) ? new WebSocketException(error, errorMessage, innerException) : new WebSocketException(error, innerException);
	}

	private string TryParseMessageHeaderFromReceiveBuffer(out MessageHeader resultHeader)
	{
		MessageHeader messageHeader = default(MessageHeader);
		Span<byte> span = _receiveBuffer.Span;
		messageHeader.Fin = (span[_receiveBufferOffset] & 0x80) != 0;
		bool flag = (span[_receiveBufferOffset] & 0x30) != 0;
		messageHeader.Opcode = (MessageOpcode)(span[_receiveBufferOffset] & 0xFu);
		messageHeader.Compressed = (span[_receiveBufferOffset] & 0x40) != 0;
		bool flag2 = (span[_receiveBufferOffset + 1] & 0x80) != 0;
		messageHeader.PayloadLength = span[_receiveBufferOffset + 1] & 0x7F;
		ConsumeFromBuffer(2);
		if (messageHeader.PayloadLength == 126)
		{
			messageHeader.PayloadLength = (span[_receiveBufferOffset] << 8) | span[_receiveBufferOffset + 1];
			ConsumeFromBuffer(2);
		}
		else if (messageHeader.PayloadLength == 127)
		{
			messageHeader.PayloadLength = 0L;
			for (int i = 0; i < 8; i++)
			{
				messageHeader.PayloadLength = (messageHeader.PayloadLength << 8) | span[_receiveBufferOffset + i];
			}
			ConsumeFromBuffer(8);
		}
		if (flag)
		{
			resultHeader = default(MessageHeader);
			return System.SR.net_Websockets_ReservedBitsSet;
		}
		if (messageHeader.PayloadLength < 0)
		{
			resultHeader = default(MessageHeader);
			return System.SR.net_Websockets_InvalidPayloadLength;
		}
		if (messageHeader.Compressed && _inflater == null)
		{
			resultHeader = default(MessageHeader);
			return System.SR.net_Websockets_PerMessageCompressedFlagWhenNotEnabled;
		}
		if (flag2)
		{
			if (!_isServer)
			{
				resultHeader = default(MessageHeader);
				return System.SR.net_Websockets_ClientReceivedMaskedFrame;
			}
			messageHeader.Mask = CombineMaskBytes(span, _receiveBufferOffset);
			ConsumeFromBuffer(4);
		}
		switch (messageHeader.Opcode)
		{
		case MessageOpcode.Continuation:
			if (_lastReceiveHeader.Fin)
			{
				resultHeader = default(MessageHeader);
				return System.SR.net_Websockets_ContinuationFromFinalFrame;
			}
			if (messageHeader.Compressed)
			{
				resultHeader = default(MessageHeader);
				return System.SR.net_Websockets_PerMessageCompressedFlagInContinuation;
			}
			messageHeader.Compressed = _lastReceiveHeader.Compressed;
			break;
		case MessageOpcode.Text:
		case MessageOpcode.Binary:
			if (!_lastReceiveHeader.Fin)
			{
				resultHeader = default(MessageHeader);
				return System.SR.net_Websockets_NonContinuationAfterNonFinalFrame;
			}
			break;
		case MessageOpcode.Close:
		case MessageOpcode.Ping:
		case MessageOpcode.Pong:
			if (messageHeader.PayloadLength > 125 || !messageHeader.Fin)
			{
				resultHeader = default(MessageHeader);
				return System.SR.net_Websockets_InvalidControlMessage;
			}
			break;
		default:
			resultHeader = default(MessageHeader);
			return System.SR.Format(System.SR.net_Websockets_UnknownOpcode, messageHeader.Opcode);
		}
		messageHeader.Processed = messageHeader.PayloadLength == 0L && !messageHeader.Compressed;
		resultHeader = messageHeader;
		return null;
	}

	private async Task CloseAsyncPrivate(WebSocketCloseStatus closeStatus, string statusDescription, CancellationToken cancellationToken)
	{
		if (!_sentCloseFrame)
		{
			await SendCloseFrameAsync(closeStatus, statusDescription, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
		}
		if (State == WebSocketState.CloseSent)
		{
			byte[] closeBuffer = ArrayPool<byte>.Shared.Rent(139);
			try
			{
				while (!_receivedCloseFrame)
				{
					ValueTask<ValueWebSocketReceiveResult> receiveTask = default(ValueTask<ValueWebSocketReceiveResult>);
					try
					{
						await _receiveMutex.EnterAsync(cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
						try
						{
							if (!_receivedCloseFrame)
							{
								receiveTask = ReceiveAsyncPrivate<ValueWebSocketReceiveResult>(closeBuffer, cancellationToken);
							}
						}
						finally
						{
							_receiveMutex.Exit();
						}
					}
					catch (OperationCanceledException)
					{
						Abort();
						throw;
					}
					await receiveTask.ConfigureAwait(continueOnCapturedContext: false);
				}
			}
			finally
			{
				ArrayPool<byte>.Shared.Return(closeBuffer);
			}
		}
		lock (StateUpdateLock)
		{
			DisposeCore();
		}
	}

	private async ValueTask SendCloseFrameAsync(WebSocketCloseStatus closeStatus, string closeStatusDescription, CancellationToken cancellationToken)
	{
		byte[] buffer = null;
		try
		{
			int num = 2;
			if (string.IsNullOrEmpty(closeStatusDescription))
			{
				buffer = ArrayPool<byte>.Shared.Rent(num);
			}
			else
			{
				num += s_textEncoding.GetByteCount(closeStatusDescription);
				buffer = ArrayPool<byte>.Shared.Rent(num);
				s_textEncoding.GetBytes(closeStatusDescription, 0, closeStatusDescription.Length, buffer, 2);
			}
			ushort num2 = (ushort)closeStatus;
			buffer[0] = (byte)(num2 >> 8);
			buffer[1] = (byte)(num2 & 0xFFu);
			await SendFrameAsync(MessageOpcode.Close, endOfMessage: true, disableCompression: true, new Memory<byte>(buffer, 0, num), cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
		}
		finally
		{
			if (buffer != null)
			{
				ArrayPool<byte>.Shared.Return(buffer);
			}
		}
		lock (StateUpdateLock)
		{
			_sentCloseFrame = true;
			if (_receivedCloseFrame && _state < WebSocketState.Closed)
			{
				_state = WebSocketState.Closed;
			}
			else if (_state < WebSocketState.CloseSent)
			{
				_state = WebSocketState.CloseSent;
			}
		}
		if (!_isServer && _receivedCloseFrame)
		{
			await WaitForServerToCloseConnectionAsync(cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
		}
	}

	private void ConsumeFromBuffer(int count)
	{
		_receiveBufferCount -= count;
		_receiveBufferOffset += count;
	}

	[AsyncStateMachine(typeof(_003CEnsureBufferContainsAsync_003Ed__74))]
	[AsyncMethodBuilder(typeof(PoolingAsyncValueTaskMethodBuilder))]
	private ValueTask EnsureBufferContainsAsync(int minimumRequiredBytes, CancellationToken cancellationToken, bool throwOnPrematureClosure = true)
	{
		Unsafe.SkipInit(out _003CEnsureBufferContainsAsync_003Ed__74 stateMachine);
		stateMachine._003C_003Et__builder = PoolingAsyncValueTaskMethodBuilder.Create();
		stateMachine._003C_003E4__this = this;
		stateMachine.minimumRequiredBytes = minimumRequiredBytes;
		stateMachine.cancellationToken = cancellationToken;
		stateMachine.throwOnPrematureClosure = throwOnPrematureClosure;
		stateMachine._003C_003E1__state = -1;
		stateMachine._003C_003Et__builder.Start(ref stateMachine);
		return stateMachine._003C_003Et__builder.Task;
	}

	private void ThrowIfEOFUnexpected(bool throwOnPrematureClosure)
	{
		if (_disposed)
		{
			throw new ObjectDisposedException("WebSocket");
		}
		if (throwOnPrematureClosure)
		{
			throw new WebSocketException(WebSocketError.ConnectionClosedPrematurely);
		}
	}

	private void AllocateSendBuffer(int minLength)
	{
		_sendBuffer = ArrayPool<byte>.Shared.Rent(minLength);
	}

	private void ReleaseSendBuffer()
	{
		byte[] sendBuffer = _sendBuffer;
		if (sendBuffer != null)
		{
			_sendBuffer = null;
			ArrayPool<byte>.Shared.Return(sendBuffer);
		}
	}

	private static int CombineMaskBytes(Span<byte> buffer, int maskOffset)
	{
		return BitConverter.ToInt32(buffer.Slice(maskOffset));
	}

	private static int ApplyMask(Span<byte> toMask, byte[] mask, int maskOffset, int maskOffsetIndex)
	{
		return ApplyMask(toMask, CombineMaskBytes(mask, maskOffset), maskOffsetIndex);
	}

	private unsafe static int ApplyMask(Span<byte> toMask, int mask, int maskIndex)
	{
		fixed (byte* ptr = &MemoryMarshal.GetReference(toMask))
		{
			byte* ptr2 = ptr;
			byte* ptr3 = ptr + toMask.Length;
			byte* ptr4 = (byte*)(&mask);
			if (ptr3 - ptr2 >= 4)
			{
				while ((ulong)ptr2 % 4uL != 0L)
				{
					byte* intPtr = ptr2++;
					*intPtr ^= ptr4[maskIndex];
					maskIndex = (maskIndex + 1) & 3;
				}
				int num = (int)((!BitConverter.IsLittleEndian) ? BitOperations.RotateLeft((uint)mask, maskIndex * 8) : BitOperations.RotateRight((uint)mask, maskIndex * 8));
				if (Vector.IsHardwareAccelerated && Vector<byte>.Count % 4 == 0 && ptr3 - ptr2 >= Vector<byte>.Count)
				{
					for (; (ulong)ptr2 % (ulong)(uint)Vector<byte>.Count != 0L; ptr2 += 4)
					{
						*(int*)ptr2 ^= num;
					}
					if (ptr3 - ptr2 >= Vector<byte>.Count)
					{
						Vector<byte> vector = Vector.AsVectorByte(new Vector<int>(num));
						do
						{
							*(Vector<byte>*)ptr2 ^= vector;
							ptr2 += Vector<byte>.Count;
						}
						while (ptr3 - ptr2 >= Vector<byte>.Count);
					}
				}
				for (; ptr3 - ptr2 >= 4; ptr2 += 4)
				{
					*(int*)ptr2 ^= num;
				}
			}
			while (ptr2 != ptr3)
			{
				byte* intPtr2 = ptr2++;
				*intPtr2 ^= ptr4[maskIndex];
				maskIndex = (maskIndex + 1) & 3;
			}
		}
		return maskIndex;
	}

	private static Exception CreateOperationCanceledException(Exception innerException, CancellationToken cancellationToken = default(CancellationToken))
	{
		return new OperationCanceledException(new OperationCanceledException().Message, innerException, cancellationToken);
	}

	private static bool TryValidateUtf8(Span<byte> span, bool endOfMessage, Utf8MessageState state)
	{
		int num = 0;
		while (num < span.Length)
		{
			if (!state.SequenceInProgress)
			{
				state.SequenceInProgress = true;
				byte b = span[num];
				num++;
				if ((b & 0x80) == 0)
				{
					state.AdditionalBytesExpected = 0;
					state.CurrentDecodeBits = b & 0x7F;
					state.ExpectedValueMin = 0;
				}
				else
				{
					if ((b & 0xC0) == 128)
					{
						return false;
					}
					if ((b & 0xE0) == 192)
					{
						state.AdditionalBytesExpected = 1;
						state.CurrentDecodeBits = b & 0x1F;
						state.ExpectedValueMin = 128;
					}
					else if ((b & 0xF0) == 224)
					{
						state.AdditionalBytesExpected = 2;
						state.CurrentDecodeBits = b & 0xF;
						state.ExpectedValueMin = 2048;
					}
					else
					{
						if ((b & 0xF8) != 240)
						{
							return false;
						}
						state.AdditionalBytesExpected = 3;
						state.CurrentDecodeBits = b & 7;
						state.ExpectedValueMin = 65536;
					}
				}
			}
			while (state.AdditionalBytesExpected > 0 && num < span.Length)
			{
				byte b2 = span[num];
				if ((b2 & 0xC0) != 128)
				{
					return false;
				}
				num++;
				state.AdditionalBytesExpected--;
				state.CurrentDecodeBits = (state.CurrentDecodeBits << 6) | (b2 & 0x3F);
				if (state.AdditionalBytesExpected == 1 && state.CurrentDecodeBits >= 864 && state.CurrentDecodeBits <= 895)
				{
					return false;
				}
				if (state.AdditionalBytesExpected == 2 && state.CurrentDecodeBits >= 272)
				{
					return false;
				}
			}
			if (state.AdditionalBytesExpected == 0)
			{
				state.SequenceInProgress = false;
				if (state.CurrentDecodeBits < state.ExpectedValueMin)
				{
					return false;
				}
			}
		}
		if (endOfMessage && state.SequenceInProgress)
		{
			return false;
		}
		return true;
	}
}
