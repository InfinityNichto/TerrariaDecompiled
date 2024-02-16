using System.Net.Quic.Implementations.MsQuic.Internal;
using System.Net.Security;
using System.Net.Sockets;
using System.Runtime.ExceptionServices;
using System.Runtime.InteropServices;
using System.Security.Authentication;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace System.Net.Quic.Implementations.MsQuic;

internal sealed class MsQuicConnection : QuicConnectionProvider
{
	internal sealed class State
	{
		public SafeMsQuicConnectionHandle Handle;

		public string TraceId;

		public GCHandle StateGCHandle;

		public MsQuicConnection Connection;

		public MsQuicListener.State ListenerState;

		public TaskCompletionSource<uint> ConnectTcs;

		public readonly TaskCompletionSource<uint> ShutdownTcs = new TaskCompletionSource<uint>(TaskCreationOptions.RunContinuationsAsynchronously);

		public TaskCompletionSource NewUnidirectionalStreamsAvailable;

		public TaskCompletionSource NewBidirectionalStreamsAvailable;

		public bool Connected;

		public long AbortErrorCode = -1L;

		public int StreamCount;

		private bool _closing;

		public X509Certificate RemoteCertificate;

		public bool RemoteCertificateRequired;

		public X509RevocationMode RevocationMode = X509RevocationMode.Offline;

		public RemoteCertificateValidationCallback RemoteCertificateValidationCallback;

		public bool IsServer;

		public string TargetHost;

		public readonly Channel<MsQuicStream> AcceptQueue = Channel.CreateUnbounded<MsQuicStream>(new UnboundedChannelOptions
		{
			SingleWriter = true
		});

		public void RemoveStream(MsQuicStream stream)
		{
			bool flag;
			lock (this)
			{
				StreamCount--;
				flag = _closing && StreamCount == 0;
			}
			if (flag)
			{
				if (System.Net.NetEventSource.Log.IsEnabled())
				{
					System.Net.NetEventSource.Info(this, $"{TraceId} releasing handle after last stream.", "RemoveStream");
				}
				Handle?.Dispose();
			}
		}

		public bool TryQueueNewStream(SafeMsQuicStreamHandle streamHandle, QUIC_STREAM_OPEN_FLAGS flags)
		{
			MsQuicStream msQuicStream = new MsQuicStream(this, streamHandle, flags);
			if (AcceptQueue.Writer.TryWrite(msQuicStream))
			{
				return true;
			}
			msQuicStream.Dispose();
			return false;
		}

		public bool TryAddStream(MsQuicStream stream)
		{
			lock (this)
			{
				if (_closing)
				{
					return false;
				}
				StreamCount++;
				return true;
			}
		}

		public void SetClosing()
		{
			lock (this)
			{
				_closing = true;
			}
		}
	}

	private static readonly Oid s_clientAuthOid = new Oid("1.3.6.1.5.5.7.3.2", "1.3.6.1.5.5.7.3.2");

	private static readonly Oid s_serverAuthOid = new Oid("1.3.6.1.5.5.7.3.1", "1.3.6.1.5.5.7.3.1");

	private static readonly MsQuicNativeMethods.ConnectionCallbackDelegate s_connectionDelegate = NativeCallbackHandler;

	private SafeMsQuicConfigurationHandle _configuration;

	private readonly State _state = new State();

	private int _disposed;

	private IPEndPoint _localEndPoint;

	private readonly EndPoint _remoteEndPoint;

	private SslApplicationProtocol _negotiatedAlpnProtocol;

	internal override IPEndPoint LocalEndPoint => _localEndPoint;

	internal override EndPoint RemoteEndPoint => _remoteEndPoint;

	internal override X509Certificate RemoteCertificate => _state.RemoteCertificate;

	internal override SslApplicationProtocol NegotiatedApplicationProtocol => _negotiatedAlpnProtocol;

	internal override bool Connected => _state.Connected;

	internal string TraceId()
	{
		return _state.TraceId;
	}

	public MsQuicConnection(IPEndPoint localEndPoint, IPEndPoint remoteEndPoint, MsQuicListener.State listenerState, SafeMsQuicConnectionHandle handle, bool remoteCertificateRequired = false, X509RevocationMode revocationMode = X509RevocationMode.Offline, RemoteCertificateValidationCallback remoteCertificateValidationCallback = null, ServerCertificateSelectionCallback serverCertificateSelectionCallback = null)
	{
		_state.Handle = handle;
		_state.StateGCHandle = GCHandle.Alloc(_state);
		_state.RemoteCertificateRequired = remoteCertificateRequired;
		_state.RevocationMode = revocationMode;
		_state.RemoteCertificateValidationCallback = remoteCertificateValidationCallback;
		_state.IsServer = true;
		_localEndPoint = localEndPoint;
		_remoteEndPoint = remoteEndPoint;
		try
		{
			MsQuicApi.Api.SetCallbackHandlerDelegate(_state.Handle, s_connectionDelegate, GCHandle.ToIntPtr(_state.StateGCHandle));
		}
		catch
		{
			_state.StateGCHandle.Free();
			throw;
		}
		_state.ListenerState = listenerState;
		_state.TraceId = MsQuicTraceHelper.GetTraceId(_state.Handle);
		if (System.Net.NetEventSource.Log.IsEnabled())
		{
			System.Net.NetEventSource.Info(_state, $"{TraceId()} Inbound connection created", ".ctor");
		}
	}

	public MsQuicConnection(QuicClientConnectionOptions options)
	{
		if (options.RemoteEndPoint == null)
		{
			throw new ArgumentNullException("RemoteEndPoint");
		}
		_remoteEndPoint = options.RemoteEndPoint;
		_configuration = SafeMsQuicConfigurationHandle.Create(options);
		_state.RemoteCertificateRequired = true;
		if (options.ClientAuthenticationOptions != null)
		{
			_state.RevocationMode = options.ClientAuthenticationOptions.CertificateRevocationCheckMode;
			_state.RemoteCertificateValidationCallback = options.ClientAuthenticationOptions.RemoteCertificateValidationCallback;
			_state.TargetHost = options.ClientAuthenticationOptions.TargetHost;
		}
		_state.StateGCHandle = GCHandle.Alloc(_state);
		try
		{
			uint status = MsQuicApi.Api.ConnectionOpenDelegate(MsQuicApi.Api.Registration, s_connectionDelegate, GCHandle.ToIntPtr(_state.StateGCHandle), out _state.Handle);
			QuicExceptionHelpers.ThrowIfFailed(status, "Could not open the connection.");
		}
		catch
		{
			_state.StateGCHandle.Free();
			throw;
		}
		_state.TraceId = MsQuicTraceHelper.GetTraceId(_state.Handle);
		if (System.Net.NetEventSource.Log.IsEnabled())
		{
			System.Net.NetEventSource.Info(_state, $"{TraceId()} Outbound connection created", ".ctor");
		}
	}

	private static uint HandleEventConnected(State state, ref MsQuicNativeMethods.ConnectionEvent connectionEvent)
	{
		if (state.Connected)
		{
			return 0u;
		}
		if (state.IsServer)
		{
			state.Connected = true;
			MsQuicListener.State listenerState = state.ListenerState;
			state.ListenerState = null;
			if (listenerState != null && listenerState.PendingConnections.TryRemove(state.Handle.DangerousGetHandle(), out var value))
			{
				if (listenerState.AcceptConnectionQueue.Writer.TryWrite(value))
				{
					return 0u;
				}
				value.Dispose();
			}
			return 2151743490u;
		}
		MsQuicNativeMethods.SOCKADDR_INET inetAddress = MsQuicParameterHelpers.GetINetParam(MsQuicApi.Api, state.Handle, QUIC_PARAM_LEVEL.CONNECTION, 1u);
		state.Connection._localEndPoint = MsQuicAddressHelpers.INetToIPEndPoint(ref inetAddress);
		state.Connection.SetNegotiatedAlpn(connectionEvent.Data.Connected.NegotiatedAlpn, connectionEvent.Data.Connected.NegotiatedAlpnLength);
		state.Connection = null;
		state.Connected = true;
		state.ConnectTcs.SetResult(0u);
		state.ConnectTcs = null;
		return 0u;
	}

	private static uint HandleEventShutdownInitiatedByTransport(State state, ref MsQuicNativeMethods.ConnectionEvent connectionEvent)
	{
		if (!state.Connected && state.ConnectTcs != null)
		{
			state.Connection = null;
			uint status = connectionEvent.Data.ShutdownInitiatedByTransport.Status;
			Exception currentStackTrace = QuicExceptionHelpers.CreateExceptionForHResult(status, "Connection has been shutdown by transport.");
			state.ConnectTcs.SetException(ExceptionDispatchInfo.SetCurrentStackTrace(currentStackTrace));
			state.ConnectTcs = null;
		}
		state.AbortErrorCode = 0L;
		state.AcceptQueue.Writer.TryComplete();
		return 0u;
	}

	private static uint HandleEventShutdownInitiatedByPeer(State state, ref MsQuicNativeMethods.ConnectionEvent connectionEvent)
	{
		state.AbortErrorCode = connectionEvent.Data.ShutdownInitiatedByPeer.ErrorCode;
		state.AcceptQueue.Writer.TryComplete();
		return 0u;
	}

	private static uint HandleEventShutdownComplete(State state, ref MsQuicNativeMethods.ConnectionEvent connectionEvent)
	{
		state.StateGCHandle.Free();
		if (state.ListenerState != null)
		{
			if (state.ListenerState.PendingConnections.TryRemove(state.Handle.DangerousGetHandle(), out var value))
			{
				value.Dispose();
			}
			state.ListenerState = null;
		}
		state.Connection = null;
		state.ShutdownTcs.SetResult(0u);
		state.AcceptQueue.Writer.TryComplete();
		TaskCompletionSource taskCompletionSource = null;
		TaskCompletionSource taskCompletionSource2 = null;
		lock (state)
		{
			taskCompletionSource = state.NewUnidirectionalStreamsAvailable;
			taskCompletionSource2 = state.NewBidirectionalStreamsAvailable;
			state.NewUnidirectionalStreamsAvailable = null;
			state.NewBidirectionalStreamsAvailable = null;
		}
		taskCompletionSource?.SetException(ExceptionDispatchInfo.SetCurrentStackTrace(new QuicOperationAbortedException()));
		taskCompletionSource2?.SetException(ExceptionDispatchInfo.SetCurrentStackTrace(new QuicOperationAbortedException()));
		return 0u;
	}

	private static uint HandleEventNewStream(State state, ref MsQuicNativeMethods.ConnectionEvent connectionEvent)
	{
		SafeMsQuicStreamHandle safeMsQuicStreamHandle = new SafeMsQuicStreamHandle(connectionEvent.Data.PeerStreamStarted.Stream);
		if (!state.TryQueueNewStream(safeMsQuicStreamHandle, connectionEvent.Data.PeerStreamStarted.Flags))
		{
			safeMsQuicStreamHandle.Dispose();
		}
		return 0u;
	}

	private static uint HandleEventStreamsAvailable(State state, ref MsQuicNativeMethods.ConnectionEvent connectionEvent)
	{
		TaskCompletionSource taskCompletionSource = null;
		TaskCompletionSource taskCompletionSource2 = null;
		lock (state)
		{
			if (connectionEvent.Data.StreamsAvailable.UniDirectionalCount > 0)
			{
				taskCompletionSource = state.NewUnidirectionalStreamsAvailable;
				state.NewUnidirectionalStreamsAvailable = null;
			}
			if (connectionEvent.Data.StreamsAvailable.BiDirectionalCount > 0)
			{
				taskCompletionSource2 = state.NewBidirectionalStreamsAvailable;
				state.NewBidirectionalStreamsAvailable = null;
			}
		}
		taskCompletionSource?.SetResult();
		taskCompletionSource2?.SetResult();
		return 0u;
	}

	private unsafe static uint HandleEventPeerCertificateReceived(State state, ref MsQuicNativeMethods.ConnectionEvent connectionEvent)
	{
		SslPolicyErrors sslPolicyErrors = SslPolicyErrors.None;
		X509Chain x509Chain = null;
		X509Certificate2 x509Certificate = null;
		X509Certificate2Collection x509Certificate2Collection = null;
		try
		{
			if (connectionEvent.Data.PeerCertificateReceived.PlatformCertificateHandle != IntPtr.Zero)
			{
				if (OperatingSystem.IsWindows())
				{
					x509Certificate = new X509Certificate2(connectionEvent.Data.PeerCertificateReceived.PlatformCertificateHandle);
				}
				else
				{
					ReadOnlySpan<MsQuicNativeMethods.QuicBuffer> readOnlySpan = new ReadOnlySpan<MsQuicNativeMethods.QuicBuffer>((void*)connectionEvent.Data.PeerCertificateReceived.PlatformCertificateHandle, sizeof(MsQuicNativeMethods.QuicBuffer));
					x509Certificate = new X509Certificate2(new ReadOnlySpan<byte>(readOnlySpan[0].Buffer, (int)readOnlySpan[0].Length));
					if (connectionEvent.Data.PeerCertificateReceived.PlatformCertificateChainHandle != IntPtr.Zero)
					{
						readOnlySpan = new ReadOnlySpan<MsQuicNativeMethods.QuicBuffer>((void*)connectionEvent.Data.PeerCertificateReceived.PlatformCertificateChainHandle, sizeof(MsQuicNativeMethods.QuicBuffer));
						if (readOnlySpan[0].Length != 0 && readOnlySpan[0].Buffer != null)
						{
							x509Certificate2Collection = new X509Certificate2Collection();
							x509Certificate2Collection.Import(new ReadOnlySpan<byte>(readOnlySpan[0].Buffer, (int)readOnlySpan[0].Length));
						}
					}
				}
			}
			if (x509Certificate == null)
			{
				if (System.Net.NetEventSource.Log.IsEnabled() && state.RemoteCertificateRequired)
				{
					System.Net.NetEventSource.Error(state, $"{state.TraceId} Remote certificate required, but no remote certificate received", "HandleEventPeerCertificateReceived");
				}
				sslPolicyErrors |= SslPolicyErrors.RemoteCertificateNotAvailable;
			}
			else
			{
				x509Chain = new X509Chain();
				x509Chain.ChainPolicy.RevocationMode = state.RevocationMode;
				x509Chain.ChainPolicy.RevocationFlag = X509RevocationFlag.ExcludeRoot;
				x509Chain.ChainPolicy.ApplicationPolicy.Add(state.IsServer ? s_clientAuthOid : s_serverAuthOid);
				if (x509Certificate2Collection != null && x509Certificate2Collection.Count > 1)
				{
					x509Chain.ChainPolicy.ExtraStore.AddRange(x509Certificate2Collection);
				}
				sslPolicyErrors |= System.Net.CertificateValidation.BuildChainAndVerifyProperties(x509Chain, x509Certificate, checkCertName: true, state.IsServer, state.TargetHost);
			}
			if (!state.RemoteCertificateRequired)
			{
				sslPolicyErrors &= ~SslPolicyErrors.RemoteCertificateNotAvailable;
			}
			state.RemoteCertificate = x509Certificate;
			if (state.RemoteCertificateValidationCallback != null)
			{
				bool success = state.RemoteCertificateValidationCallback(state, x509Certificate, x509Chain, sslPolicyErrors);
				state.RemoteCertificateValidationCallback = (object _, X509Certificate _, X509Chain _, SslPolicyErrors _) => success;
				if (!success && System.Net.NetEventSource.Log.IsEnabled())
				{
					System.Net.NetEventSource.Error(state, $"{state.TraceId} Remote certificate rejected by verification callback", "HandleEventPeerCertificateReceived");
				}
				if (!success)
				{
					if (state.IsServer)
					{
						return 2151743490u;
					}
					throw new AuthenticationException(System.SR.net_quic_cert_custom_validation);
				}
				return 0u;
			}
			if (System.Net.NetEventSource.Log.IsEnabled())
			{
				System.Net.NetEventSource.Info(state, $"{state.TraceId} Certificate validation for '${x509Certificate?.Subject}' finished with ${sslPolicyErrors}", "HandleEventPeerCertificateReceived");
			}
			if (sslPolicyErrors != 0)
			{
				if (state.IsServer)
				{
					return 2151743488u;
				}
				throw new AuthenticationException(System.SR.Format(System.SR.net_quic_cert_chain_validation, sslPolicyErrors));
			}
			return 0u;
		}
		catch (Exception ex)
		{
			if (System.Net.NetEventSource.Log.IsEnabled())
			{
				System.Net.NetEventSource.Error(state, $"{state.TraceId} Certificate validation failed ${ex.Message}", "HandleEventPeerCertificateReceived");
			}
			throw;
		}
	}

	internal override async ValueTask<QuicStreamProvider> AcceptStreamAsync(CancellationToken cancellationToken = default(CancellationToken))
	{
		ThrowIfDisposed();
		try
		{
			return await _state.AcceptQueue.Reader.ReadAsync(cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
		}
		catch (ChannelClosedException)
		{
			throw ThrowHelper.GetConnectionAbortedException(_state.AbortErrorCode);
		}
	}

	internal override ValueTask WaitForAvailableUnidirectionalStreamsAsync(CancellationToken cancellationToken = default(CancellationToken))
	{
		TaskCompletionSource newUnidirectionalStreamsAvailable = _state.NewUnidirectionalStreamsAvailable;
		if (newUnidirectionalStreamsAvailable == null)
		{
			int remoteAvailableUnidirectionalStreamCount = GetRemoteAvailableUnidirectionalStreamCount();
			lock (_state)
			{
				if (_state.NewUnidirectionalStreamsAvailable == null)
				{
					if (_state.ShutdownTcs.Task.IsCompleted)
					{
						throw new QuicOperationAbortedException();
					}
					if (remoteAvailableUnidirectionalStreamCount > 0)
					{
						return ValueTask.CompletedTask;
					}
					_state.NewUnidirectionalStreamsAvailable = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
				}
				newUnidirectionalStreamsAvailable = _state.NewUnidirectionalStreamsAvailable;
			}
		}
		return new ValueTask(newUnidirectionalStreamsAvailable.Task.WaitAsync(cancellationToken));
	}

	internal override ValueTask WaitForAvailableBidirectionalStreamsAsync(CancellationToken cancellationToken = default(CancellationToken))
	{
		TaskCompletionSource newBidirectionalStreamsAvailable = _state.NewBidirectionalStreamsAvailable;
		if (newBidirectionalStreamsAvailable == null)
		{
			int remoteAvailableBidirectionalStreamCount = GetRemoteAvailableBidirectionalStreamCount();
			lock (_state)
			{
				if (_state.NewBidirectionalStreamsAvailable == null)
				{
					if (_state.ShutdownTcs.Task.IsCompleted)
					{
						throw new QuicOperationAbortedException();
					}
					if (remoteAvailableBidirectionalStreamCount > 0)
					{
						return ValueTask.CompletedTask;
					}
					_state.NewBidirectionalStreamsAvailable = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
				}
				newBidirectionalStreamsAvailable = _state.NewBidirectionalStreamsAvailable;
			}
		}
		return new ValueTask(newBidirectionalStreamsAvailable.Task.WaitAsync(cancellationToken));
	}

	internal override QuicStreamProvider OpenUnidirectionalStream()
	{
		ThrowIfDisposed();
		if (!Connected)
		{
			throw new InvalidOperationException(System.SR.net_quic_not_connected);
		}
		return new MsQuicStream(_state, QUIC_STREAM_OPEN_FLAGS.UNIDIRECTIONAL);
	}

	internal override QuicStreamProvider OpenBidirectionalStream()
	{
		ThrowIfDisposed();
		if (!Connected)
		{
			throw new InvalidOperationException(System.SR.net_quic_not_connected);
		}
		return new MsQuicStream(_state, QUIC_STREAM_OPEN_FLAGS.NONE);
	}

	internal override int GetRemoteAvailableUnidirectionalStreamCount()
	{
		return MsQuicParameterHelpers.GetUShortParam(MsQuicApi.Api, _state.Handle, QUIC_PARAM_LEVEL.CONNECTION, 9u);
	}

	internal override int GetRemoteAvailableBidirectionalStreamCount()
	{
		return MsQuicParameterHelpers.GetUShortParam(MsQuicApi.Api, _state.Handle, QUIC_PARAM_LEVEL.CONNECTION, 8u);
	}

	internal unsafe override ValueTask ConnectAsync(CancellationToken cancellationToken = default(CancellationToken))
	{
		ThrowIfDisposed();
		if (_configuration == null)
		{
			throw new InvalidOperationException("ConnectAsync must not be called on a connection obtained from a listener.");
		}
		QUIC_ADDRESS_FAMILY family = _remoteEndPoint.AddressFamily switch
		{
			AddressFamily.Unspecified => QUIC_ADDRESS_FAMILY.UNSPEC, 
			AddressFamily.InterNetwork => QUIC_ADDRESS_FAMILY.INET, 
			AddressFamily.InterNetworkV6 => QUIC_ADDRESS_FAMILY.INET6, 
			_ => throw new ArgumentException(System.SR.Format(System.SR.net_quic_unsupported_address_family, _remoteEndPoint.AddressFamily)), 
		};
		_state.Connection = this;
		string serverName;
		int port;
		if (_remoteEndPoint is IPEndPoint)
		{
			MsQuicNativeMethods.SOCKADDR_INET sOCKADDR_INET = MsQuicAddressHelpers.IPEndPointToINet((IPEndPoint)_remoteEndPoint);
			uint status = MsQuicApi.Api.SetParamDelegate(_state.Handle, QUIC_PARAM_LEVEL.CONNECTION, 2u, (uint)sizeof(MsQuicNativeMethods.SOCKADDR_INET), (byte*)(&sOCKADDR_INET));
			QuicExceptionHelpers.ThrowIfFailed(status, "Failed to connect to peer.");
			serverName = _state.TargetHost ?? ((IPEndPoint)_remoteEndPoint).Address.ToString();
			port = ((IPEndPoint)_remoteEndPoint).Port;
		}
		else
		{
			if (!(_remoteEndPoint is DnsEndPoint))
			{
				throw new ArgumentException($"Unsupported remote endpoint type '{_remoteEndPoint.GetType()}'.");
			}
			port = ((DnsEndPoint)_remoteEndPoint).Port;
			string host = ((DnsEndPoint)_remoteEndPoint).Host;
			if (!string.IsNullOrEmpty(_state.TargetHost) && !host.Equals(_state.TargetHost, StringComparison.InvariantCultureIgnoreCase) && IPAddress.TryParse(host, out IPAddress address))
			{
				MsQuicNativeMethods.SOCKADDR_INET sOCKADDR_INET2 = MsQuicAddressHelpers.IPEndPointToINet(new IPEndPoint(address, port));
				uint status = MsQuicApi.Api.SetParamDelegate(_state.Handle, QUIC_PARAM_LEVEL.CONNECTION, 2u, (uint)sizeof(MsQuicNativeMethods.SOCKADDR_INET), (byte*)(&sOCKADDR_INET2));
				QuicExceptionHelpers.ThrowIfFailed(status, "Failed to connect to peer.");
				serverName = _state.TargetHost;
			}
			else
			{
				serverName = host;
			}
		}
		TaskCompletionSource<uint> taskCompletionSource = (_state.ConnectTcs = new TaskCompletionSource<uint>(TaskCreationOptions.RunContinuationsAsynchronously));
		try
		{
			uint status = MsQuicApi.Api.ConnectionStartDelegate(_state.Handle, _configuration, family, serverName, (ushort)port);
			QuicExceptionHelpers.ThrowIfFailed(status, "Failed to connect to peer.");
			_configuration.Dispose();
			_configuration = null;
		}
		catch
		{
			_state.StateGCHandle.Free();
			_state.Connection = null;
			throw;
		}
		return new ValueTask(taskCompletionSource.Task);
	}

	private ValueTask ShutdownAsync(QUIC_CONNECTION_SHUTDOWN_FLAGS Flags, long ErrorCode)
	{
		_state.Connection = this;
		try
		{
			MsQuicApi.Api.ConnectionShutdownDelegate(_state.Handle, Flags, ErrorCode);
		}
		catch
		{
			_state.Connection = null;
			throw;
		}
		return new ValueTask(_state.ShutdownTcs.Task);
	}

	internal void SetNegotiatedAlpn(IntPtr alpn, int alpnLength)
	{
		if (alpn != IntPtr.Zero && alpnLength != 0)
		{
			byte[] array = new byte[alpnLength];
			Marshal.Copy(alpn, array, 0, alpnLength);
			_negotiatedAlpnProtocol = new SslApplicationProtocol(array);
		}
	}

	private static uint NativeCallbackHandler(IntPtr connection, IntPtr context, ref MsQuicNativeMethods.ConnectionEvent connectionEvent)
	{
		State state = (State)GCHandle.FromIntPtr(context).Target;
		if (System.Net.NetEventSource.Log.IsEnabled())
		{
			System.Net.NetEventSource.Info(state, $"{state.TraceId} Connection received event {connectionEvent.Type}", "NativeCallbackHandler");
		}
		try
		{
			return connectionEvent.Type switch
			{
				QUIC_CONNECTION_EVENT_TYPE.CONNECTED => HandleEventConnected(state, ref connectionEvent), 
				QUIC_CONNECTION_EVENT_TYPE.SHUTDOWN_INITIATED_BY_TRANSPORT => HandleEventShutdownInitiatedByTransport(state, ref connectionEvent), 
				QUIC_CONNECTION_EVENT_TYPE.SHUTDOWN_INITIATED_BY_PEER => HandleEventShutdownInitiatedByPeer(state, ref connectionEvent), 
				QUIC_CONNECTION_EVENT_TYPE.SHUTDOWN_COMPLETE => HandleEventShutdownComplete(state, ref connectionEvent), 
				QUIC_CONNECTION_EVENT_TYPE.PEER_STREAM_STARTED => HandleEventNewStream(state, ref connectionEvent), 
				QUIC_CONNECTION_EVENT_TYPE.STREAMS_AVAILABLE => HandleEventStreamsAvailable(state, ref connectionEvent), 
				QUIC_CONNECTION_EVENT_TYPE.PEER_CERTIFICATE_RECEIVED => HandleEventPeerCertificateReceived(state, ref connectionEvent), 
				_ => 0u, 
			};
		}
		catch (Exception ex)
		{
			if (System.Net.NetEventSource.Log.IsEnabled())
			{
				System.Net.NetEventSource.Error(state, $"{state.TraceId} Exception occurred during handling {connectionEvent.Type} connection callback: {ex}", "NativeCallbackHandler");
			}
			if (state.ConnectTcs != null)
			{
				state.ConnectTcs.TrySetException(ex);
				state.Connection = null;
				state.ConnectTcs = null;
			}
			return 2151743491u;
		}
	}

	public override void Dispose()
	{
		Dispose(disposing: true);
		GC.SuppressFinalize(this);
	}

	~MsQuicConnection()
	{
		Dispose(disposing: false);
	}

	private async Task FlushAcceptQueue()
	{
		_state.AcceptQueue.Writer.TryComplete();
		await foreach (MsQuicStream item in _state.AcceptQueue.Reader.ReadAllAsync().ConfigureAwait(continueOnCapturedContext: false))
		{
			if (item.CanRead)
			{
				item.AbortRead(4294967295L);
			}
			if (item.CanWrite)
			{
				item.AbortWrite(4294967295L);
			}
			item.Dispose();
		}
	}

	private void Dispose(bool disposing)
	{
		if (Interlocked.Exchange(ref _disposed, 1) != 0)
		{
			return;
		}
		if (System.Net.NetEventSource.Log.IsEnabled())
		{
			System.Net.NetEventSource.Info(_state, $"{TraceId()} Connection disposing {disposing}", "Dispose");
		}
		if (_state.Handle != null && !_state.Handle.IsInvalid && !_state.Handle.IsClosed)
		{
			MsQuicApi.Api.ConnectionShutdownDelegate(_state.Handle, QUIC_CONNECTION_SHUTDOWN_FLAGS.SILENT, 0L);
		}
		bool flag = false;
		lock (_state)
		{
			_state.Connection = null;
			if (_state.StreamCount == 0)
			{
				flag = true;
			}
			else
			{
				_state.SetClosing();
			}
		}
		FlushAcceptQueue().GetAwaiter().GetResult();
		_configuration?.Dispose();
		if (flag)
		{
			if (System.Net.NetEventSource.Log.IsEnabled())
			{
				System.Net.NetEventSource.Info(_state, $"{TraceId()} Connection releasing handle", "Dispose");
			}
			_state.Handle?.Dispose();
		}
	}

	internal override ValueTask CloseAsync(long errorCode, CancellationToken cancellationToken = default(CancellationToken))
	{
		if (_disposed == 1)
		{
			return default(ValueTask);
		}
		return ShutdownAsync(QUIC_CONNECTION_SHUTDOWN_FLAGS.NONE, errorCode);
	}

	private void ThrowIfDisposed()
	{
		if (_disposed == 1)
		{
			throw new ObjectDisposedException("MsQuicStream");
		}
	}
}
