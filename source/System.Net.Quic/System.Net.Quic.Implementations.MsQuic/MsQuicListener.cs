using System.Buffers;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net.Quic.Implementations.MsQuic.Internal;
using System.Net.Security;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace System.Net.Quic.Implementations.MsQuic;

internal sealed class MsQuicListener : QuicListenerProvider, IDisposable
{
	internal sealed class State
	{
		public SafeMsQuicListenerHandle Handle;

		public string TraceId;

		public readonly SafeMsQuicConfigurationHandle ConnectionConfiguration;

		public readonly Channel<MsQuicConnection> AcceptConnectionQueue;

		public readonly ConcurrentDictionary<IntPtr, MsQuicConnection> PendingConnections;

		public QuicOptions ConnectionOptions = new QuicOptions();

		public SslServerAuthenticationOptions AuthenticationOptions = new SslServerAuthenticationOptions();

		public State(QuicListenerOptions options)
		{
			ConnectionOptions.IdleTimeout = options.IdleTimeout;
			ConnectionOptions.MaxBidirectionalStreams = options.MaxBidirectionalStreams;
			ConnectionOptions.MaxUnidirectionalStreams = options.MaxUnidirectionalStreams;
			bool flag = false;
			if (options.ServerAuthenticationOptions != null)
			{
				AuthenticationOptions.ClientCertificateRequired = options.ServerAuthenticationOptions.ClientCertificateRequired;
				AuthenticationOptions.CertificateRevocationCheckMode = options.ServerAuthenticationOptions.CertificateRevocationCheckMode;
				AuthenticationOptions.RemoteCertificateValidationCallback = options.ServerAuthenticationOptions.RemoteCertificateValidationCallback;
				AuthenticationOptions.ServerCertificateSelectionCallback = options.ServerAuthenticationOptions.ServerCertificateSelectionCallback;
				AuthenticationOptions.ApplicationProtocols = options.ServerAuthenticationOptions.ApplicationProtocols;
				if (options.ServerAuthenticationOptions.ServerCertificate == null && options.ServerAuthenticationOptions.ServerCertificateContext == null && options.ServerAuthenticationOptions.ServerCertificateSelectionCallback != null)
				{
					flag = true;
				}
			}
			if (!flag)
			{
				ConnectionConfiguration = SafeMsQuicConfigurationHandle.Create(options, options.ServerAuthenticationOptions);
			}
			PendingConnections = new ConcurrentDictionary<IntPtr, MsQuicConnection>();
			AcceptConnectionQueue = Channel.CreateBounded<MsQuicConnection>(new BoundedChannelOptions(options.ListenBacklog)
			{
				SingleReader = true,
				SingleWriter = true
			});
		}
	}

	private static readonly MsQuicNativeMethods.ListenerCallbackDelegate s_listenerDelegate = NativeCallbackHandler;

	private readonly State _state;

	private GCHandle _stateHandle;

	private volatile bool _disposed;

	private readonly IPEndPoint _listenEndPoint;

	internal override IPEndPoint ListenEndPoint => new IPEndPoint(_listenEndPoint.Address, _listenEndPoint.Port);

	internal MsQuicListener(QuicListenerOptions options)
	{
		if (options.ListenEndPoint == null)
		{
			throw new ArgumentNullException("ListenEndPoint");
		}
		_state = new State(options);
		_stateHandle = GCHandle.Alloc(_state);
		try
		{
			uint status = MsQuicApi.Api.ListenerOpenDelegate(MsQuicApi.Api.Registration, s_listenerDelegate, GCHandle.ToIntPtr(_stateHandle), out _state.Handle);
			QuicExceptionHelpers.ThrowIfFailed(status, "ListenerOpen failed.");
		}
		catch
		{
			_stateHandle.Free();
			throw;
		}
		_state.TraceId = MsQuicTraceHelper.GetTraceId(_state.Handle);
		if (System.Net.NetEventSource.Log.IsEnabled())
		{
			System.Net.NetEventSource.Info(_state, $"{_state.TraceId} Listener created", ".ctor");
		}
		_listenEndPoint = Start(options);
		if (System.Net.NetEventSource.Log.IsEnabled())
		{
			System.Net.NetEventSource.Info(_state, $"{_state.TraceId} Listener started", ".ctor");
		}
	}

	internal override async ValueTask<QuicConnectionProvider> AcceptConnectionAsync(CancellationToken cancellationToken = default(CancellationToken))
	{
		ThrowIfDisposed();
		try
		{
			return await _state.AcceptConnectionQueue.Reader.ReadAsync(cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
		}
		catch (ChannelClosedException)
		{
			throw new QuicOperationAbortedException();
		}
	}

	public override void Dispose()
	{
		Dispose(disposing: true);
		GC.SuppressFinalize(this);
	}

	~MsQuicListener()
	{
		Dispose(disposing: false);
	}

	private void Dispose(bool disposing)
	{
		if (!_disposed)
		{
			Stop();
			_state?.Handle?.Dispose();
			if (_stateHandle.IsAllocated)
			{
				_stateHandle.Free();
			}
			_state?.ConnectionConfiguration?.Dispose();
			_disposed = true;
		}
	}

	private unsafe IPEndPoint Start(QuicListenerOptions options)
	{
		List<SslApplicationProtocol> applicationProtocols = options.ServerAuthenticationOptions.ApplicationProtocols;
		IPEndPoint listenEndPoint = options.ListenEndPoint;
		MsQuicNativeMethods.SOCKADDR_INET localAddress = MsQuicAddressHelpers.IPEndPointToINet(listenEndPoint);
		MemoryHandle[] handles = null;
		MsQuicNativeMethods.QuicBuffer[] buffers = null;
		uint status;
		try
		{
			MsQuicAlpnHelper.Prepare(applicationProtocols, out handles, out buffers);
			status = MsQuicApi.Api.ListenerStartDelegate(_state.Handle, (MsQuicNativeMethods.QuicBuffer*)(void*)Marshal.UnsafeAddrOfPinnedArrayElement(buffers, 0), (uint)applicationProtocols.Count, ref localAddress);
		}
		catch
		{
			_stateHandle.Free();
			throw;
		}
		finally
		{
			MsQuicAlpnHelper.Return(ref handles, ref buffers);
		}
		QuicExceptionHelpers.ThrowIfFailed(status, "ListenerStart failed.");
		MsQuicNativeMethods.SOCKADDR_INET inetAddress = MsQuicParameterHelpers.GetINetParam(MsQuicApi.Api, _state.Handle, QUIC_PARAM_LEVEL.LISTENER, 0u);
		return MsQuicAddressHelpers.INetToIPEndPoint(ref inetAddress);
	}

	private void Stop()
	{
		if (_state != null)
		{
			_state.AcceptConnectionQueue?.Writer.TryComplete();
			if (_state.Handle != null)
			{
				MsQuicApi.Api.ListenerStopDelegate(_state.Handle);
			}
		}
	}

	private unsafe static uint NativeCallbackHandler(IntPtr listener, IntPtr context, ref MsQuicNativeMethods.ListenerEvent evt)
	{
		State state = (State)GCHandle.FromIntPtr(context).Target;
		if (evt.Type != 0)
		{
			return 2151743491u;
		}
		SafeMsQuicConnectionHandle safeMsQuicConnectionHandle = null;
		MsQuicConnection msQuicConnection = null;
		try
		{
			ref MsQuicNativeMethods.NewConnectionInfo reference = ref *evt.Data.NewConnection.Info;
			IPEndPoint localEndPoint = MsQuicAddressHelpers.INetToIPEndPoint(ref *(MsQuicNativeMethods.SOCKADDR_INET*)(void*)reference.LocalAddress);
			IPEndPoint remoteEndPoint = MsQuicAddressHelpers.INetToIPEndPoint(ref *(MsQuicNativeMethods.SOCKADDR_INET*)(void*)reference.RemoteAddress);
			string targetHost = string.Empty;
			if (reference.ServerNameLength > 0 && reference.ServerName != IntPtr.Zero)
			{
				targetHost = Marshal.PtrToStringAnsi(reference.ServerName, reference.ServerNameLength);
			}
			SafeMsQuicConfigurationHandle safeMsQuicConfigurationHandle = state.ConnectionConfiguration;
			if (safeMsQuicConfigurationHandle == null)
			{
				try
				{
					safeMsQuicConfigurationHandle = SafeMsQuicConfigurationHandle.Create(state.ConnectionOptions, state.AuthenticationOptions, targetHost);
				}
				catch (Exception ex)
				{
					if (System.Net.NetEventSource.Log.IsEnabled())
					{
						System.Net.NetEventSource.Error(state, $"[Listener#{state.GetHashCode()}] Exception occurred during creating configuration in connection callback: {ex}", "NativeCallbackHandler");
					}
				}
				if (safeMsQuicConfigurationHandle == null)
				{
					return 2151743491u;
				}
			}
			safeMsQuicConnectionHandle = new SafeMsQuicConnectionHandle(evt.Data.NewConnection.Connection);
			uint status = MsQuicApi.Api.ConnectionSetConfigurationDelegate(safeMsQuicConnectionHandle, safeMsQuicConfigurationHandle);
			if (MsQuicStatusHelper.SuccessfulStatusCode(status))
			{
				msQuicConnection = new MsQuicConnection(localEndPoint, remoteEndPoint, state, safeMsQuicConnectionHandle, state.AuthenticationOptions.ClientCertificateRequired, state.AuthenticationOptions.CertificateRevocationCheckMode, state.AuthenticationOptions.RemoteCertificateValidationCallback);
				msQuicConnection.SetNegotiatedAlpn(reference.NegotiatedAlpn, reference.NegotiatedAlpnLength);
				if (!state.PendingConnections.TryAdd(safeMsQuicConnectionHandle.DangerousGetHandle(), msQuicConnection))
				{
					msQuicConnection.Dispose();
				}
				return 0u;
			}
		}
		catch (Exception ex2)
		{
			if (System.Net.NetEventSource.Log.IsEnabled())
			{
				System.Net.NetEventSource.Error(state, $"[Listener#{state.GetHashCode()}] Exception occurred during handling {evt.Type} connection callback: {ex2}", "NativeCallbackHandler");
			}
		}
		safeMsQuicConnectionHandle?.SetHandleAsInvalid();
		msQuicConnection?.Dispose();
		return 2151743491u;
	}

	private void ThrowIfDisposed()
	{
		if (_disposed)
		{
			throw new ObjectDisposedException("MsQuicStream");
		}
	}
}
