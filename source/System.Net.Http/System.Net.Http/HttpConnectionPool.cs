using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Net.Http.Headers;
using System.Net.Http.HPack;
using System.Net.Http.QPack;
using System.Net.Quic;
using System.Net.Security;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;
using System.Runtime.Versioning;
using System.Security.Authentication;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Internal;

namespace System.Net.Http;

internal sealed class HttpConnectionPool : IDisposable
{
	private struct RequestQueue<T>
	{
		private struct QueueItem
		{
			public HttpRequestMessage Request;

			public TaskCompletionSourceWithCancellation<T> Waiter;
		}

		private Queue<QueueItem> _queue;

		public bool IsEmpty => Count == 0;

		public int Count => _queue?.Count ?? 0;

		public TaskCompletionSourceWithCancellation<T> EnqueueRequest(HttpRequestMessage request)
		{
			if (_queue == null)
			{
				_queue = new Queue<QueueItem>();
			}
			TaskCompletionSourceWithCancellation<T> taskCompletionSourceWithCancellation = new TaskCompletionSourceWithCancellation<T>();
			_queue.Enqueue(new QueueItem
			{
				Request = request,
				Waiter = taskCompletionSourceWithCancellation
			});
			return taskCompletionSourceWithCancellation;
		}

		public bool TryFailNextRequest(Exception e)
		{
			if (_queue != null)
			{
				QueueItem result;
				while (_queue.TryDequeue(out result))
				{
					if (result.Waiter.TrySetException(e))
					{
						return true;
					}
				}
			}
			return false;
		}

		public bool TryDequeueNextRequest(T connection)
		{
			if (_queue != null)
			{
				QueueItem result;
				while (_queue.TryDequeue(out result))
				{
					if (result.Waiter.TrySetResult(connection))
					{
						return true;
					}
				}
			}
			return false;
		}

		public bool TryPeekNextRequest([NotNullWhen(true)] out HttpRequestMessage request)
		{
			if (_queue != null && _queue.TryPeek(out var result))
			{
				request = result.Request;
				return true;
			}
			request = null;
			return false;
		}
	}

	private static readonly bool s_isWindows7Or2008R2 = GetIsWindows7Or2008R2();

	private readonly HttpConnectionPoolManager _poolManager;

	private readonly HttpConnectionKind _kind;

	private readonly Uri _proxyUri;

	private readonly HttpAuthority _originAuthority;

	private volatile HttpAuthority _http3Authority;

	private Timer _authorityExpireTimer;

	private bool _persistAuthority;

	private volatile HashSet<HttpAuthority> _altSvcBlocklist;

	private CancellationTokenSource _altSvcBlocklistTimerCancellation;

	private volatile bool _altSvcEnabled = true;

	private readonly List<HttpConnection> _availableHttp11Connections = new List<HttpConnection>();

	private readonly int _maxHttp11Connections;

	private int _associatedHttp11ConnectionCount;

	private int _pendingHttp11ConnectionCount;

	private RequestQueue<HttpConnection> _http11RequestQueue;

	private List<Http2Connection> _availableHttp2Connections;

	private int _associatedHttp2ConnectionCount;

	private bool _pendingHttp2Connection;

	private RequestQueue<Http2Connection> _http2RequestQueue;

	private bool _http2Enabled;

	private byte[] _http2AltSvcOriginUri;

	internal readonly byte[] _http2EncodedAuthorityHostHeader;

	private readonly bool _http3Enabled;

	private Http3Connection _http3Connection;

	private SemaphoreSlim _http3ConnectionCreateLock;

	internal readonly byte[] _http3EncodedAuthorityHostHeader;

	private readonly byte[] _hostHeaderValueBytes;

	private readonly SslClientAuthenticationOptions _sslOptionsHttp11;

	private readonly SslClientAuthenticationOptions _sslOptionsHttp2;

	private readonly SslClientAuthenticationOptions _sslOptionsHttp2Only;

	private readonly SslClientAuthenticationOptions _sslOptionsHttp3;

	private bool _usedSinceLastCleanup = true;

	private bool _disposed;

	private static readonly List<SslApplicationProtocol> s_http3ApplicationProtocols = new List<SslApplicationProtocol> { SslApplicationProtocol.Http3 };

	private static readonly List<SslApplicationProtocol> s_http2ApplicationProtocols = new List<SslApplicationProtocol>
	{
		SslApplicationProtocol.Http2,
		SslApplicationProtocol.Http11
	};

	private static readonly List<SslApplicationProtocol> s_http2OnlyApplicationProtocols = new List<SslApplicationProtocol> { SslApplicationProtocol.Http2 };

	public HttpConnectionSettings Settings => _poolManager.Settings;

	public HttpConnectionKind Kind => _kind;

	public bool IsSecure
	{
		get
		{
			if (_kind != HttpConnectionKind.Https && _kind != HttpConnectionKind.SslProxyTunnel)
			{
				return _kind == HttpConnectionKind.SslSocksTunnel;
			}
			return true;
		}
	}

	public Uri ProxyUri => _proxyUri;

	public ICredentials ProxyCredentials => _poolManager.ProxyCredentials;

	public byte[] HostHeaderValueBytes => _hostHeaderValueBytes;

	public CredentialCache PreAuthCredentials { get; }

	public byte[] Http2AltSvcOriginUri
	{
		get
		{
			if (_http2AltSvcOriginUri == null)
			{
				StringBuilder stringBuilder = new StringBuilder();
				stringBuilder.Append(IsSecure ? "https://" : "http://").Append(_originAuthority.IdnHost);
				if (_originAuthority.Port != (IsSecure ? 443 : 80))
				{
					StringBuilder stringBuilder2 = stringBuilder;
					IFormatProvider invariantCulture = CultureInfo.InvariantCulture;
					StringBuilder.AppendInterpolatedStringHandler handler = new StringBuilder.AppendInterpolatedStringHandler(1, 1, stringBuilder2, invariantCulture);
					handler.AppendLiteral(":");
					handler.AppendFormatted(_originAuthority.Port);
					stringBuilder2.Append(invariantCulture, ref handler);
				}
				_http2AltSvcOriginUri = Encoding.ASCII.GetBytes(stringBuilder.ToString());
			}
			return _http2AltSvcOriginUri;
		}
	}

	private bool EnableMultipleHttp2Connections => _poolManager.Settings.EnableMultipleHttp2Connections;

	private object SyncObj => _availableHttp11Connections;

	private bool DoProxyAuth
	{
		get
		{
			if (_kind != HttpConnectionKind.Proxy)
			{
				return _kind == HttpConnectionKind.ProxyConnect;
			}
			return true;
		}
	}

	public HttpConnectionPool(HttpConnectionPoolManager poolManager, HttpConnectionKind kind, string host, int port, string sslHostName, Uri proxyUri)
	{
		_poolManager = poolManager;
		_kind = kind;
		_proxyUri = proxyUri;
		_maxHttp11Connections = Settings._maxConnectionsPerServer;
		if (host != null)
		{
			_originAuthority = new HttpAuthority(host, port);
		}
		_http2Enabled = _poolManager.Settings._maxHttpVersion >= HttpVersion.Version20;
		if (IsHttp3Supported())
		{
			_http3Enabled = _poolManager.Settings._maxHttpVersion >= HttpVersion.Version30 && (_poolManager.Settings._quicImplementationProvider ?? QuicImplementationProviders.Default).IsSupported;
		}
		switch (kind)
		{
		case HttpConnectionKind.Http:
			_http3Enabled = false;
			break;
		case HttpConnectionKind.Proxy:
			_http2Enabled = false;
			_http3Enabled = false;
			break;
		case HttpConnectionKind.ProxyTunnel:
			_http2Enabled = false;
			_http3Enabled = false;
			break;
		case HttpConnectionKind.SslProxyTunnel:
			_http3Enabled = false;
			break;
		case HttpConnectionKind.ProxyConnect:
			_maxHttp11Connections = int.MaxValue;
			_http2Enabled = false;
			_http3Enabled = false;
			break;
		case HttpConnectionKind.SocksTunnel:
		case HttpConnectionKind.SslSocksTunnel:
			_http3Enabled = false;
			break;
		}
		if (!_http3Enabled)
		{
			_altSvcEnabled = false;
		}
		string text = null;
		if (_originAuthority != null)
		{
			text = ((_originAuthority.Port != ((sslHostName == null) ? 80 : 443)) ? $"{_originAuthority.IdnHost}:{_originAuthority.Port}" : _originAuthority.IdnHost);
			_hostHeaderValueBytes = Encoding.ASCII.GetBytes(text);
			if (sslHostName == null)
			{
				_http2EncodedAuthorityHostHeader = HPackEncoder.EncodeLiteralHeaderFieldWithoutIndexingToAllocatedArray(1, text);
				_http3EncodedAuthorityHostHeader = QPackEncoder.EncodeLiteralHeaderFieldWithStaticNameReferenceToArray(0, text);
			}
		}
		if (sslHostName != null)
		{
			_sslOptionsHttp11 = ConstructSslOptions(poolManager, sslHostName);
			_sslOptionsHttp11.ApplicationProtocols = null;
			if (_http2Enabled)
			{
				_sslOptionsHttp2 = ConstructSslOptions(poolManager, sslHostName);
				_sslOptionsHttp2.ApplicationProtocols = s_http2ApplicationProtocols;
				_sslOptionsHttp2Only = ConstructSslOptions(poolManager, sslHostName);
				_sslOptionsHttp2Only.ApplicationProtocols = s_http2OnlyApplicationProtocols;
				_http2EncodedAuthorityHostHeader = HPackEncoder.EncodeLiteralHeaderFieldWithoutIndexingToAllocatedArray(1, text);
				_http3EncodedAuthorityHostHeader = QPackEncoder.EncodeLiteralHeaderFieldWithStaticNameReferenceToArray(0, text);
			}
			if (IsHttp3Supported() && _http3Enabled)
			{
				_sslOptionsHttp3 = ConstructSslOptions(poolManager, sslHostName);
				_sslOptionsHttp3.ApplicationProtocols = s_http3ApplicationProtocols;
			}
		}
		if (_poolManager.Settings._preAuthenticate)
		{
			PreAuthCredentials = new CredentialCache();
		}
		if (System.Net.NetEventSource.Log.IsEnabled())
		{
			Trace($"{this}", ".ctor");
		}
	}

	[SupportedOSPlatformGuard("linux")]
	[SupportedOSPlatformGuard("macOS")]
	[SupportedOSPlatformGuard("Windows")]
	internal static bool IsHttp3Supported()
	{
		if ((!OperatingSystem.IsLinux() || OperatingSystem.IsAndroid()) && !OperatingSystem.IsWindows())
		{
			return OperatingSystem.IsMacOS();
		}
		return true;
	}

	private static SslClientAuthenticationOptions ConstructSslOptions(HttpConnectionPoolManager poolManager, string sslHostName)
	{
		SslClientAuthenticationOptions sslClientAuthenticationOptions = poolManager.Settings._sslOptions?.ShallowClone() ?? new SslClientAuthenticationOptions();
		sslClientAuthenticationOptions.TargetHost = sslHostName;
		if (s_isWindows7Or2008R2 && sslClientAuthenticationOptions.EnabledSslProtocols == SslProtocols.None)
		{
			if (System.Net.NetEventSource.Log.IsEnabled())
			{
				System.Net.NetEventSource.Info(poolManager, $"Win7OrWin2K8R2 platform, Changing default TLS protocols to {SslProtocols.Tls | SslProtocols.Tls11 | SslProtocols.Tls12 | SslProtocols.Tls13}", "ConstructSslOptions");
			}
			sslClientAuthenticationOptions.EnabledSslProtocols = SslProtocols.Tls | SslProtocols.Tls11 | SslProtocols.Tls12 | SslProtocols.Tls13;
		}
		return sslClientAuthenticationOptions;
	}

	[DoesNotReturn]
	private static void ThrowGetVersionException(HttpRequestMessage request, int desiredVersion)
	{
		throw new HttpRequestException(System.SR.Format(System.SR.net_http_requested_version_cannot_establish, request.Version, request.VersionPolicy, desiredVersion));
	}

	private bool CheckExpirationOnGet(HttpConnectionBase connection)
	{
		TimeSpan pooledConnectionLifetime = _poolManager.Settings._pooledConnectionLifetime;
		if (pooledConnectionLifetime != Timeout.InfiniteTimeSpan)
		{
			return (double)connection.GetLifetimeTicks(Environment.TickCount64) > pooledConnectionLifetime.TotalMilliseconds;
		}
		return false;
	}

	private static Exception CreateConnectTimeoutException(OperationCanceledException oce)
	{
		TimeoutException innerException = new TimeoutException(System.SR.net_http_connect_timedout, oce.InnerException);
		Exception ex = CancellationHelper.CreateOperationCanceledException(innerException, oce.CancellationToken);
		ExceptionDispatchInfo.SetCurrentStackTrace(ex);
		return ex;
	}

	private async Task AddHttp11ConnectionAsync(HttpRequestMessage request)
	{
		if (System.Net.NetEventSource.Log.IsEnabled())
		{
			Trace("Creating new HTTP/1.1 connection for pool.", "AddHttp11ConnectionAsync");
		}
		HttpConnection connection;
		using (CancellationTokenSource cts = GetConnectTimeoutCancellationTokenSource())
		{
			try
			{
				connection = await CreateHttp11ConnectionAsync(request, async: true, cts.Token).ConfigureAwait(continueOnCapturedContext: false);
			}
			catch (OperationCanceledException ex) when (ex.CancellationToken == cts.Token)
			{
				HandleHttp11ConnectionFailure(CreateConnectTimeoutException(ex));
				return;
			}
			catch (Exception e)
			{
				HandleHttp11ConnectionFailure(e);
				return;
			}
		}
		ReturnHttp11Connection(connection, isNewConnection: true);
	}

	private void CheckForHttp11ConnectionInjection()
	{
		if (_http11RequestQueue.TryPeekNextRequest(out var request) && _availableHttp11Connections.Count == 0 && _http11RequestQueue.Count > _pendingHttp11ConnectionCount && _associatedHttp11ConnectionCount < _maxHttp11Connections)
		{
			_associatedHttp11ConnectionCount++;
			_pendingHttp11ConnectionCount++;
			Task.Run(() => AddHttp11ConnectionAsync(request));
		}
	}

	private async ValueTask<HttpConnection> GetHttp11ConnectionAsync(HttpRequestMessage request, bool async, CancellationToken cancellationToken)
	{
		TaskCompletionSourceWithCancellation<HttpConnection> taskCompletionSourceWithCancellation;
		while (true)
		{
			HttpConnection httpConnection = null;
			lock (SyncObj)
			{
				_usedSinceLastCleanup = true;
				int count = _availableHttp11Connections.Count;
				if (count > 0)
				{
					httpConnection = _availableHttp11Connections[count - 1];
					_availableHttp11Connections.RemoveAt(count - 1);
					goto IL_0095;
				}
				taskCompletionSourceWithCancellation = _http11RequestQueue.EnqueueRequest(request);
				CheckForHttp11ConnectionInjection();
			}
			break;
			IL_0095:
			if (CheckExpirationOnGet(httpConnection))
			{
				if (System.Net.NetEventSource.Log.IsEnabled())
				{
					httpConnection.Trace("Found expired HTTP/1.1 connection in pool.", "GetHttp11ConnectionAsync");
				}
				httpConnection.Dispose();
				continue;
			}
			if (!httpConnection.PrepareForReuse(async))
			{
				if (System.Net.NetEventSource.Log.IsEnabled())
				{
					httpConnection.Trace("Found invalid HTTP/1.1 connection in pool.", "GetHttp11ConnectionAsync");
				}
				httpConnection.Dispose();
				continue;
			}
			if (System.Net.NetEventSource.Log.IsEnabled())
			{
				httpConnection.Trace("Found usable HTTP/1.1 connection in pool.", "GetHttp11ConnectionAsync");
			}
			return httpConnection;
		}
		if (System.Net.NetEventSource.Log.IsEnabled())
		{
			Trace("No available HTTP/1.1 connections; request queued.", "GetHttp11ConnectionAsync");
		}
		Microsoft.Extensions.Internal.ValueStopwatch stopwatch = Microsoft.Extensions.Internal.ValueStopwatch.StartNew();
		try
		{
			return await taskCompletionSourceWithCancellation.WaitWithCancellationAsync(async, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
		}
		finally
		{
			if (HttpTelemetry.Log.IsEnabled())
			{
				HttpTelemetry.Log.Http11RequestLeftQueue(stopwatch.GetElapsedTime().TotalMilliseconds);
			}
		}
	}

	private async Task HandleHttp11Downgrade(HttpRequestMessage request, Socket socket, Stream stream, TransportContext transportContext, CancellationToken cancellationToken)
	{
		if (System.Net.NetEventSource.Log.IsEnabled())
		{
			Trace("Server does not support HTTP2; disabling HTTP2 use and proceeding with HTTP/1.1 connection", "HandleHttp11Downgrade");
		}
		bool flag = true;
		lock (SyncObj)
		{
			_http2Enabled = false;
			_associatedHttp2ConnectionCount--;
			_pendingHttp2Connection = false;
			while (_http2RequestQueue.TryDequeueNextRequest(null))
			{
			}
			if (_associatedHttp11ConnectionCount < _maxHttp11Connections)
			{
				_associatedHttp11ConnectionCount++;
				_pendingHttp11ConnectionCount++;
			}
			else
			{
				flag = false;
			}
		}
		if (!flag)
		{
			if (System.Net.NetEventSource.Log.IsEnabled())
			{
				Trace("Discarding downgraded HTTP/1.1 connection because HTTP/1.1 connection limit is exceeded", "HandleHttp11Downgrade");
			}
			stream.Dispose();
		}
		HttpConnection connection;
		try
		{
			connection = await ConstructHttp11ConnectionAsync(async: true, socket, stream, transportContext, request, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
		}
		catch (OperationCanceledException ex) when (ex.CancellationToken == cancellationToken)
		{
			HandleHttp11ConnectionFailure(CreateConnectTimeoutException(ex));
			return;
		}
		catch (Exception e)
		{
			HandleHttp11ConnectionFailure(e);
			return;
		}
		ReturnHttp11Connection(connection, isNewConnection: true);
	}

	private async Task AddHttp2ConnectionAsync(HttpRequestMessage request)
	{
		if (System.Net.NetEventSource.Log.IsEnabled())
		{
			Trace("Creating new HTTP/2 connection for pool.", "AddHttp2ConnectionAsync");
		}
		Http2Connection connection;
		using (CancellationTokenSource cts = GetConnectTimeoutCancellationTokenSource())
		{
			_ = 3;
			try
			{
				var (socket, stream, transportContext) = await ConnectAsync(request, async: true, cts.Token).ConfigureAwait(continueOnCapturedContext: false);
				if (IsSecure)
				{
					SslStream sslStream = (SslStream)stream;
					if (!(sslStream.NegotiatedApplicationProtocol == SslApplicationProtocol.Http2))
					{
						await HandleHttp11Downgrade(request, socket, stream, transportContext, cts.Token).ConfigureAwait(continueOnCapturedContext: false);
						return;
					}
					if (sslStream.SslProtocol < SslProtocols.Tls12)
					{
						stream.Dispose();
						throw new HttpRequestException(System.SR.Format(System.SR.net_ssl_http2_requires_tls12, sslStream.SslProtocol));
					}
					connection = await ConstructHttp2ConnectionAsync(stream, request, cts.Token).ConfigureAwait(continueOnCapturedContext: false);
				}
				else
				{
					connection = await ConstructHttp2ConnectionAsync(stream, request, cts.Token).ConfigureAwait(continueOnCapturedContext: false);
				}
			}
			catch (OperationCanceledException ex) when (ex.CancellationToken == cts.Token)
			{
				HandleHttp2ConnectionFailure(CreateConnectTimeoutException(ex));
				return;
			}
			catch (Exception e)
			{
				HandleHttp2ConnectionFailure(e);
				return;
			}
		}
		ValueTask valueTask = connection.WaitForShutdownAsync();
		ReturnHttp2Connection(connection, isNewConnection: true);
		await valueTask.ConfigureAwait(continueOnCapturedContext: false);
		InvalidateHttp2Connection(connection);
	}

	private void CheckForHttp2ConnectionInjection()
	{
		if (!_http2RequestQueue.TryPeekNextRequest(out var request))
		{
			return;
		}
		List<Http2Connection> availableHttp2Connections = _availableHttp2Connections;
		if ((availableHttp2Connections == null || availableHttp2Connections.Count == 0) && !_pendingHttp2Connection && (_associatedHttp2ConnectionCount == 0 || EnableMultipleHttp2Connections))
		{
			_associatedHttp2ConnectionCount++;
			_pendingHttp2Connection = true;
			Task.Run(() => AddHttp2ConnectionAsync(request));
		}
	}

	private async ValueTask<Http2Connection> GetHttp2ConnectionAsync(HttpRequestMessage request, bool async, CancellationToken cancellationToken)
	{
		TaskCompletionSourceWithCancellation<Http2Connection> taskCompletionSourceWithCancellation;
		while (true)
		{
			Http2Connection http2Connection;
			lock (SyncObj)
			{
				_usedSinceLastCleanup = true;
				if (!_http2Enabled)
				{
					return null;
				}
				int num = _availableHttp2Connections?.Count ?? 0;
				if (num > 0)
				{
					http2Connection = _availableHttp2Connections[num - 1];
					goto IL_0099;
				}
				taskCompletionSourceWithCancellation = _http2RequestQueue.EnqueueRequest(request);
				CheckForHttp2ConnectionInjection();
			}
			break;
			IL_0099:
			if (CheckExpirationOnGet(http2Connection))
			{
				if (System.Net.NetEventSource.Log.IsEnabled())
				{
					http2Connection.Trace("Found expired HTTP/2 connection in pool.", "GetHttp2ConnectionAsync");
				}
				InvalidateHttp2Connection(http2Connection);
				continue;
			}
			if (!http2Connection.TryReserveStream())
			{
				if (System.Net.NetEventSource.Log.IsEnabled())
				{
					http2Connection.Trace("Found HTTP/2 connection in pool without available streams.", "GetHttp2ConnectionAsync");
				}
				bool flag = false;
				lock (SyncObj)
				{
					int num2 = _availableHttp2Connections.IndexOf(http2Connection);
					if (num2 != -1)
					{
						flag = true;
						_availableHttp2Connections.RemoveAt(num2);
					}
				}
				if (flag)
				{
					DisableHttp2Connection(http2Connection);
				}
				continue;
			}
			if (System.Net.NetEventSource.Log.IsEnabled())
			{
				http2Connection.Trace("Found usable HTTP/2 connection in pool.", "GetHttp2ConnectionAsync");
			}
			return http2Connection;
		}
		if (System.Net.NetEventSource.Log.IsEnabled())
		{
			Trace("No available HTTP/2 connections; request queued.", "GetHttp2ConnectionAsync");
		}
		Microsoft.Extensions.Internal.ValueStopwatch stopwatch = Microsoft.Extensions.Internal.ValueStopwatch.StartNew();
		try
		{
			return await taskCompletionSourceWithCancellation.WaitWithCancellationAsync(async, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
		}
		finally
		{
			if (HttpTelemetry.Log.IsEnabled())
			{
				HttpTelemetry.Log.Http20RequestLeftQueue(stopwatch.GetElapsedTime().TotalMilliseconds);
			}
		}
	}

	[SupportedOSPlatform("windows")]
	[SupportedOSPlatform("linux")]
	[SupportedOSPlatform("macos")]
	private async ValueTask<Http3Connection> GetHttp3ConnectionAsync(HttpRequestMessage request, HttpAuthority authority, CancellationToken cancellationToken)
	{
		Http3Connection http3Connection = Volatile.Read(ref _http3Connection);
		if (http3Connection != null)
		{
			if (!CheckExpirationOnGet(http3Connection) && http3Connection.Authority == authority)
			{
				if (System.Net.NetEventSource.Log.IsEnabled())
				{
					Trace("Using existing HTTP3 connection.", "GetHttp3ConnectionAsync");
				}
				_usedSinceLastCleanup = true;
				return http3Connection;
			}
			if (System.Net.NetEventSource.Log.IsEnabled())
			{
				http3Connection.Trace("Found expired HTTP3 connection.", "GetHttp3ConnectionAsync");
			}
			http3Connection.Dispose();
			InvalidateHttp3Connection(http3Connection);
		}
		if (_http3ConnectionCreateLock == null)
		{
			lock (SyncObj)
			{
				if (_http3ConnectionCreateLock == null)
				{
					_http3ConnectionCreateLock = new SemaphoreSlim(1);
				}
			}
		}
		await _http3ConnectionCreateLock.WaitAsync(cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
		try
		{
			if (_http3Connection != null)
			{
				if (System.Net.NetEventSource.Log.IsEnabled())
				{
					Trace("Using existing HTTP3 connection.", "GetHttp3ConnectionAsync");
				}
				return _http3Connection;
			}
			if (System.Net.NetEventSource.Log.IsEnabled())
			{
				Trace("Attempting new HTTP3 connection.", "GetHttp3ConnectionAsync");
			}
			QuicConnection connection;
			try
			{
				connection = await ConnectHelper.ConnectQuicAsync(request, Settings._quicImplementationProvider ?? QuicImplementationProviders.Default, new DnsEndPoint(authority.IdnHost, authority.Port), _sslOptionsHttp3, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
			}
			catch
			{
				BlocklistAuthority(authority);
				throw;
			}
			http3Connection = (_http3Connection = new Http3Connection(this, _originAuthority, authority, connection));
			if (System.Net.NetEventSource.Log.IsEnabled())
			{
				Trace("New HTTP3 connection established.", "GetHttp3ConnectionAsync");
			}
			return http3Connection;
		}
		finally
		{
			_http3ConnectionCreateLock.Release();
		}
	}

	[SupportedOSPlatform("windows")]
	[SupportedOSPlatform("linux")]
	[SupportedOSPlatform("macos")]
	private async ValueTask<HttpResponseMessage> TrySendUsingHttp3Async(HttpRequestMessage request, bool async, CancellationToken cancellationToken)
	{
		HttpResponseMessage httpResponseMessage;
		while (true)
		{
			HttpAuthority httpAuthority = _http3Authority;
			if (request.Version.Major >= 3 && request.VersionPolicy != 0 && httpAuthority == null)
			{
				httpAuthority = _originAuthority;
			}
			if (httpAuthority == null)
			{
				return null;
			}
			if (IsAltSvcBlocked(httpAuthority))
			{
				ThrowGetVersionException(request, 3);
			}
			Http3Connection connection = await GetHttp3ConnectionAsync(request, httpAuthority, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
			httpResponseMessage = await connection.SendAsync(request, async, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
			if (httpResponseMessage.StatusCode != HttpStatusCode.MisdirectedRequest || connection.Authority == _originAuthority)
			{
				break;
			}
			httpResponseMessage.Dispose();
			BlocklistAuthority(connection.Authority);
		}
		return httpResponseMessage;
	}

	private void ProcessAltSvc(HttpResponseMessage response)
	{
		if (_altSvcEnabled && response.Headers.TryGetValues(KnownHeaders.AltSvc.Descriptor, out var values))
		{
			HandleAltSvc(values, response.Headers.Age);
		}
	}

	public async ValueTask<HttpResponseMessage> SendWithVersionDetectionAndRetryAsync(HttpRequestMessage request, bool async, bool doRequestAuth, CancellationToken cancellationToken)
	{
		int retryCount = 0;
		while (true)
		{
			try
			{
				HttpResponseMessage response = null;
				if (IsHttp3Supported() && _http3Enabled && (request.Version.Major >= 3 || (request.VersionPolicy == HttpVersionPolicy.RequestVersionOrHigher && IsSecure)))
				{
					response = await TrySendUsingHttp3Async(request, async, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
				}
				if (response == null)
				{
					if (request.Version.Major >= 3 && request.VersionPolicy != 0)
					{
						ThrowGetVersionException(request, 3);
					}
					if (_http2Enabled && (request.Version.Major >= 2 || (request.VersionPolicy == HttpVersionPolicy.RequestVersionOrHigher && IsSecure)) && (request.VersionPolicy != 0 || IsSecure))
					{
						Http2Connection http2Connection = await GetHttp2ConnectionAsync(request, async, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
						if (http2Connection != null)
						{
							response = await http2Connection.SendAsync(request, async, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
						}
					}
					if (response == null)
					{
						if (request.Version.Major >= 2 && request.VersionPolicy != 0)
						{
							ThrowGetVersionException(request, 2);
						}
						HttpConnection connection = await GetHttp11ConnectionAsync(request, async, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
						connection.Acquire();
						try
						{
							response = await SendWithNtConnectionAuthAsync(connection, request, async, doRequestAuth, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
						}
						finally
						{
							connection.Release();
						}
					}
				}
				ProcessAltSvc(response);
				return response;
			}
			catch (HttpRequestException ex) when (ex.AllowRetry == RequestRetryType.RetryOnConnectionFailure)
			{
				if (retryCount == 3)
				{
					if (System.Net.NetEventSource.Log.IsEnabled())
					{
						Trace($"MaxConnectionFailureRetries limit of {3} hit. Retryable request will not be retried. Exception: {ex}", "SendWithVersionDetectionAndRetryAsync");
					}
					throw;
				}
				retryCount++;
				if (System.Net.NetEventSource.Log.IsEnabled())
				{
					Trace($"Retry attempt {retryCount} after connection failure. Connection exception: {ex}", "SendWithVersionDetectionAndRetryAsync");
				}
			}
			catch (HttpRequestException ex2) when (ex2.AllowRetry == RequestRetryType.RetryOnLowerHttpVersion)
			{
				if (request.VersionPolicy != 0)
				{
					throw new HttpRequestException(System.SR.Format(System.SR.net_http_requested_version_server_refused, request.Version, request.VersionPolicy), ex2);
				}
				if (System.Net.NetEventSource.Log.IsEnabled())
				{
					Trace($"Retrying request because server requested version fallback: {ex2}", "SendWithVersionDetectionAndRetryAsync");
				}
				request.Version = HttpVersion.Version11;
			}
			catch (HttpRequestException ex3) when (ex3.AllowRetry == RequestRetryType.RetryOnStreamLimitReached)
			{
				if (System.Net.NetEventSource.Log.IsEnabled())
				{
					Trace($"Retrying request on another HTTP/2 connection after active streams limit is reached on existing one: {ex3}", "SendWithVersionDetectionAndRetryAsync");
				}
			}
		}
	}

	internal void HandleAltSvc(IEnumerable<string> altSvcHeaderValues, TimeSpan? responseAge)
	{
		HttpAuthority httpAuthority = null;
		TimeSpan dueTime = default(TimeSpan);
		bool flag = false;
		foreach (string altSvcHeaderValue2 in altSvcHeaderValues)
		{
			int index = 0;
			if (!AltSvcHeaderParser.Parser.TryParseValue(altSvcHeaderValue2, null, ref index, out var parsedValue))
			{
				continue;
			}
			AltSvcHeaderValue altSvcHeaderValue = (AltSvcHeaderValue)parsedValue;
			if (altSvcHeaderValue == AltSvcHeaderValue.Clear)
			{
				ExpireAltSvcAuthority();
				_authorityExpireTimer.Change(-1, -1);
				break;
			}
			if (httpAuthority != null || altSvcHeaderValue == null || !(altSvcHeaderValue.AlpnProtocolName == "h3"))
			{
				continue;
			}
			HttpAuthority httpAuthority2 = new HttpAuthority(altSvcHeaderValue.Host ?? _originAuthority.IdnHost, altSvcHeaderValue.Port);
			if (!IsAltSvcBlocked(httpAuthority2))
			{
				TimeSpan maxAge = altSvcHeaderValue.MaxAge;
				if (responseAge.HasValue)
				{
					maxAge -= responseAge.GetValueOrDefault();
				}
				if (maxAge > TimeSpan.Zero)
				{
					httpAuthority = httpAuthority2;
					dueTime = maxAge;
					flag = altSvcHeaderValue.Persist;
				}
			}
		}
		if (httpAuthority == null || httpAuthority.Equals(_http3Authority))
		{
			return;
		}
		if (dueTime.Ticks > 25920000000000L)
		{
			dueTime = TimeSpan.FromTicks(25920000000000L);
		}
		lock (SyncObj)
		{
			if (_authorityExpireTimer == null)
			{
				WeakReference<HttpConnectionPool> state = new WeakReference<HttpConnectionPool>(this);
				bool flag2 = false;
				try
				{
					if (!ExecutionContext.IsFlowSuppressed())
					{
						ExecutionContext.SuppressFlow();
						flag2 = true;
					}
					_authorityExpireTimer = new Timer(delegate(object o)
					{
						WeakReference<HttpConnectionPool> weakReference = (WeakReference<HttpConnectionPool>)o;
						if (weakReference.TryGetTarget(out var target))
						{
							target.ExpireAltSvcAuthority();
						}
					}, state, dueTime, Timeout.InfiniteTimeSpan);
				}
				finally
				{
					if (flag2)
					{
						ExecutionContext.RestoreFlow();
					}
				}
			}
			else
			{
				_authorityExpireTimer.Change(dueTime, Timeout.InfiniteTimeSpan);
			}
			_http3Authority = httpAuthority;
			_persistAuthority = flag;
		}
		if (!flag)
		{
			_poolManager.StartMonitoringNetworkChanges();
		}
	}

	private void ExpireAltSvcAuthority()
	{
		_http3Authority = null;
	}

	private bool IsAltSvcBlocked(HttpAuthority authority)
	{
		if (_altSvcBlocklist != null)
		{
			lock (_altSvcBlocklist)
			{
				return _altSvcBlocklist.Contains(authority);
			}
		}
		return false;
	}

	internal void BlocklistAuthority(HttpAuthority badAuthority)
	{
		HashSet<HttpAuthority> altSvcBlocklist = _altSvcBlocklist;
		if (altSvcBlocklist == null)
		{
			lock (SyncObj)
			{
				altSvcBlocklist = _altSvcBlocklist;
				if (altSvcBlocklist == null)
				{
					altSvcBlocklist = new HashSet<HttpAuthority>();
					_altSvcBlocklistTimerCancellation = new CancellationTokenSource();
					_altSvcBlocklist = altSvcBlocklist;
				}
			}
		}
		bool flag = false;
		bool flag2;
		lock (altSvcBlocklist)
		{
			flag2 = altSvcBlocklist.Add(badAuthority);
			if (flag2 && altSvcBlocklist.Count >= 8 && _altSvcEnabled)
			{
				_altSvcEnabled = false;
				flag = true;
			}
		}
		lock (SyncObj)
		{
			if (_http3Authority == badAuthority)
			{
				ExpireAltSvcAuthority();
				_authorityExpireTimer.Change(-1, -1);
			}
		}
		if (flag2)
		{
			Task.Delay(600000).ContinueWith(delegate
			{
				lock (altSvcBlocklist)
				{
					altSvcBlocklist.Remove(badAuthority);
				}
			}, _altSvcBlocklistTimerCancellation.Token, TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);
		}
		if (flag)
		{
			Task.Delay(600000).ContinueWith(delegate
			{
				_altSvcEnabled = true;
			}, _altSvcBlocklistTimerCancellation.Token, TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);
		}
	}

	public void OnNetworkChanged()
	{
		lock (SyncObj)
		{
			if (_http3Authority != null && !_persistAuthority)
			{
				ExpireAltSvcAuthority();
				_authorityExpireTimer.Change(-1, -1);
			}
		}
	}

	public Task<HttpResponseMessage> SendWithNtConnectionAuthAsync(HttpConnection connection, HttpRequestMessage request, bool async, bool doRequestAuth, CancellationToken cancellationToken)
	{
		if (doRequestAuth && Settings._credentials != null)
		{
			return AuthenticationHelper.SendWithNtConnectionAuthAsync(request, async, Settings._credentials, connection, this, cancellationToken);
		}
		return SendWithNtProxyAuthAsync(connection, request, async, cancellationToken);
	}

	public Task<HttpResponseMessage> SendWithNtProxyAuthAsync(HttpConnection connection, HttpRequestMessage request, bool async, CancellationToken cancellationToken)
	{
		if (DoProxyAuth && ProxyCredentials != null)
		{
			return AuthenticationHelper.SendWithNtProxyAuthAsync(request, ProxyUri, async, ProxyCredentials, connection, this, cancellationToken);
		}
		return connection.SendAsync(request, async, cancellationToken);
	}

	public ValueTask<HttpResponseMessage> SendWithProxyAuthAsync(HttpRequestMessage request, bool async, bool doRequestAuth, CancellationToken cancellationToken)
	{
		if (DoProxyAuth && ProxyCredentials != null)
		{
			return AuthenticationHelper.SendWithProxyAuthAsync(request, _proxyUri, async, ProxyCredentials, doRequestAuth, this, cancellationToken);
		}
		return SendWithVersionDetectionAndRetryAsync(request, async, doRequestAuth, cancellationToken);
	}

	public ValueTask<HttpResponseMessage> SendAsync(HttpRequestMessage request, bool async, bool doRequestAuth, CancellationToken cancellationToken)
	{
		if (doRequestAuth && Settings._credentials != null)
		{
			return AuthenticationHelper.SendWithRequestAuthAsync(request, async, Settings._credentials, Settings._preAuthenticate, this, cancellationToken);
		}
		return SendWithProxyAuthAsync(request, async, doRequestAuth, cancellationToken);
	}

	private CancellationTokenSource GetConnectTimeoutCancellationTokenSource()
	{
		return new CancellationTokenSource(Settings._connectTimeout);
	}

	private async ValueTask<(Socket, Stream, TransportContext)> ConnectAsync(HttpRequestMessage request, bool async, CancellationToken cancellationToken)
	{
		Stream stream = null;
		Socket socket = null;
		switch (_kind)
		{
		case HttpConnectionKind.Http:
		case HttpConnectionKind.Https:
		case HttpConnectionKind.ProxyConnect:
			(socket, stream) = await ConnectToTcpHostAsync(_originAuthority.IdnHost, _originAuthority.Port, request, async, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
			break;
		case HttpConnectionKind.Proxy:
			(socket, stream) = await ConnectToTcpHostAsync(_proxyUri.IdnHost, _proxyUri.Port, request, async, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
			break;
		case HttpConnectionKind.ProxyTunnel:
		case HttpConnectionKind.SslProxyTunnel:
			stream = await EstablishProxyTunnelAsync(async, request.HasHeaders ? request.Headers : null, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
			break;
		case HttpConnectionKind.SocksTunnel:
		case HttpConnectionKind.SslSocksTunnel:
			(socket, stream) = await EstablishSocksTunnel(request, async, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
			break;
		}
		if (socket == null && stream is NetworkStream networkStream)
		{
			socket = networkStream.Socket;
		}
		TransportContext item = null;
		if (IsSecure)
		{
			SslStream sslStream = await ConnectHelper.EstablishSslConnectionAsync(GetSslOptionsForRequest(request), request, async, stream, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
			item = sslStream.TransportContext;
			stream = sslStream;
		}
		return (socket, stream, item);
	}

	private async ValueTask<(Socket, Stream)> ConnectToTcpHostAsync(string host, int port, HttpRequestMessage initialRequest, bool async, CancellationToken cancellationToken)
	{
		cancellationToken.ThrowIfCancellationRequested();
		DnsEndPoint endPoint = new DnsEndPoint(host, port);
		Socket socket = null;
		try
		{
			Stream item;
			if (Settings._connectCallback != null)
			{
				ValueTask<Stream> valueTask = Settings._connectCallback(new SocketsHttpConnectionContext(endPoint, initialRequest), cancellationToken);
				if (!async && !valueTask.IsCompleted)
				{
					Trace("ConnectCallback completing asynchronously for a synchronous request.", "ConnectToTcpHostAsync");
				}
				item = (await valueTask.ConfigureAwait(continueOnCapturedContext: false)) ?? throw new HttpRequestException(System.SR.net_http_null_from_connect_callback);
			}
			else
			{
				socket = new Socket(SocketType.Stream, ProtocolType.Tcp)
				{
					NoDelay = true
				};
				if (async)
				{
					await socket.ConnectAsync(endPoint, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
				}
				else
				{
					using (cancellationToken.UnsafeRegister(delegate(object s)
					{
						((Socket)s).Dispose();
					}, socket))
					{
						socket.Connect(endPoint);
					}
				}
				item = new NetworkStream(socket, ownsSocket: true);
			}
			return (socket, item);
		}
		catch (Exception ex)
		{
			socket?.Dispose();
			throw (ex is OperationCanceledException ex2 && ex2.CancellationToken == cancellationToken) ? CancellationHelper.CreateOperationCanceledException(null, cancellationToken) : ConnectHelper.CreateWrappedException(ex, endPoint.Host, endPoint.Port, cancellationToken);
		}
	}

	internal async ValueTask<HttpConnection> CreateHttp11ConnectionAsync(HttpRequestMessage request, bool async, CancellationToken cancellationToken)
	{
		var (socket, stream, transportContext) = await ConnectAsync(request, async, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
		return await ConstructHttp11ConnectionAsync(async, socket, stream, transportContext, request, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
	}

	private SslClientAuthenticationOptions GetSslOptionsForRequest(HttpRequestMessage request)
	{
		if (_http2Enabled)
		{
			if (request.Version.Major >= 2 && request.VersionPolicy != 0)
			{
				return _sslOptionsHttp2Only;
			}
			if (request.Version.Major >= 2 || request.VersionPolicy == HttpVersionPolicy.RequestVersionOrHigher)
			{
				return _sslOptionsHttp2;
			}
		}
		return _sslOptionsHttp11;
	}

	private async ValueTask<Stream> ApplyPlaintextFilterAsync(bool async, Stream stream, Version httpVersion, HttpRequestMessage request, CancellationToken cancellationToken)
	{
		if (Settings._plaintextStreamFilter == null)
		{
			return stream;
		}
		Stream stream2;
		try
		{
			ValueTask<Stream> valueTask = Settings._plaintextStreamFilter(new SocketsHttpPlaintextStreamFilterContext(stream, httpVersion, request), cancellationToken);
			if (!async && !valueTask.IsCompleted)
			{
				Trace("PlaintextStreamFilter completing asynchronously for a synchronous request.", "ApplyPlaintextFilterAsync");
			}
			stream2 = await valueTask.ConfigureAwait(continueOnCapturedContext: false);
		}
		catch (OperationCanceledException ex) when (ex.CancellationToken == cancellationToken)
		{
			stream.Dispose();
			throw;
		}
		catch (Exception inner)
		{
			stream.Dispose();
			throw new HttpRequestException(System.SR.net_http_exception_during_plaintext_filter, inner);
		}
		if (stream2 == null)
		{
			stream.Dispose();
			throw new HttpRequestException(System.SR.net_http_null_from_plaintext_filter);
		}
		return stream2;
	}

	private async ValueTask<HttpConnection> ConstructHttp11ConnectionAsync(bool async, Socket socket, Stream stream, TransportContext transportContext, HttpRequestMessage request, CancellationToken cancellationToken)
	{
		Stream stream2 = await ApplyPlaintextFilterAsync(async, stream, HttpVersion.Version11, request, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
		if (stream2 != stream)
		{
			socket = null;
		}
		return new HttpConnection(this, socket, stream2, transportContext);
	}

	private async ValueTask<Http2Connection> ConstructHttp2ConnectionAsync(Stream stream, HttpRequestMessage request, CancellationToken cancellationToken)
	{
		stream = await ApplyPlaintextFilterAsync(async: true, stream, HttpVersion.Version20, request, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
		Http2Connection http2Connection = new Http2Connection(this, stream);
		try
		{
			await http2Connection.SetupAsync().ConfigureAwait(continueOnCapturedContext: false);
			return http2Connection;
		}
		catch (Exception inner)
		{
			throw new HttpRequestException(System.SR.net_http_client_execution_error, inner);
		}
	}

	private async ValueTask<Stream> EstablishProxyTunnelAsync(bool async, HttpRequestHeaders headers, CancellationToken cancellationToken)
	{
		HttpRequestMessage httpRequestMessage = new HttpRequestMessage(HttpMethod.Connect, _proxyUri);
		httpRequestMessage.Headers.Host = $"{_originAuthority.IdnHost}:{_originAuthority.Port}";
		if (headers != null && headers.TryGetValues("User-Agent", out IEnumerable<string> values))
		{
			httpRequestMessage.Headers.TryAddWithoutValidation("User-Agent", values);
		}
		HttpResponseMessage httpResponseMessage = await _poolManager.SendProxyConnectAsync(httpRequestMessage, _proxyUri, async, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
		if (httpResponseMessage.StatusCode != HttpStatusCode.OK)
		{
			httpResponseMessage.Dispose();
			throw new HttpRequestException(System.SR.Format(System.SR.net_http_proxy_tunnel_returned_failure_status_code, _proxyUri, (int)httpResponseMessage.StatusCode));
		}
		try
		{
			return httpResponseMessage.Content.ReadAsStream(cancellationToken);
		}
		catch
		{
			httpResponseMessage.Dispose();
			throw;
		}
	}

	private async ValueTask<(Socket socket, Stream stream)> EstablishSocksTunnel(HttpRequestMessage request, bool async, CancellationToken cancellationToken)
	{
		var (socket, stream) = await ConnectToTcpHostAsync(_proxyUri.IdnHost, _proxyUri.Port, request, async, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
		try
		{
			await SocksHelper.EstablishSocksTunnelAsync(stream, _originAuthority.IdnHost, _originAuthority.Port, _proxyUri, ProxyCredentials, async, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
		}
		catch (Exception ex) when (!(ex is OperationCanceledException))
		{
			throw new HttpRequestException(System.SR.net_http_request_aborted, ex);
		}
		return (socket, stream);
	}

	private void HandleHttp11ConnectionFailure(Exception e)
	{
		if (System.Net.NetEventSource.Log.IsEnabled())
		{
			Trace("HTTP/1.1 connection failed", "HandleHttp11ConnectionFailure");
		}
		lock (SyncObj)
		{
			_associatedHttp11ConnectionCount--;
			_pendingHttp11ConnectionCount--;
			_http11RequestQueue.TryFailNextRequest(e);
			CheckForHttp11ConnectionInjection();
		}
	}

	private void HandleHttp2ConnectionFailure(Exception e)
	{
		if (System.Net.NetEventSource.Log.IsEnabled())
		{
			Trace("HTTP2 connection failed", "HandleHttp2ConnectionFailure");
		}
		lock (SyncObj)
		{
			_associatedHttp2ConnectionCount--;
			_pendingHttp2Connection = false;
			_http2RequestQueue.TryFailNextRequest(e);
			CheckForHttp2ConnectionInjection();
		}
	}

	public void InvalidateHttp11Connection(HttpConnection connection, bool disposing = true)
	{
		lock (SyncObj)
		{
			_associatedHttp11ConnectionCount--;
			CheckForHttp11ConnectionInjection();
		}
	}

	public void InvalidateHttp2Connection(Http2Connection connection)
	{
		if (System.Net.NetEventSource.Log.IsEnabled())
		{
			connection.Trace("", "InvalidateHttp2Connection");
		}
		bool flag = false;
		lock (SyncObj)
		{
			if (_availableHttp2Connections != null)
			{
				int num = _availableHttp2Connections.IndexOf(connection);
				if (num != -1)
				{
					flag = true;
					_availableHttp2Connections.RemoveAt(num);
					_associatedHttp2ConnectionCount--;
				}
			}
			CheckForHttp2ConnectionInjection();
		}
		if (flag)
		{
			connection.Dispose();
		}
	}

	private bool CheckExpirationOnReturn(HttpConnectionBase connection)
	{
		TimeSpan pooledConnectionLifetime = _poolManager.Settings._pooledConnectionLifetime;
		if (pooledConnectionLifetime != Timeout.InfiniteTimeSpan)
		{
			if (!(pooledConnectionLifetime == TimeSpan.Zero))
			{
				return (double)connection.GetLifetimeTicks(Environment.TickCount64) > pooledConnectionLifetime.TotalMilliseconds;
			}
			return true;
		}
		return false;
	}

	public void ReturnHttp11Connection(HttpConnection connection, bool isNewConnection = false)
	{
		if (System.Net.NetEventSource.Log.IsEnabled())
		{
			connection.Trace($"{"isNewConnection"}={isNewConnection}", "ReturnHttp11Connection");
		}
		if (!isNewConnection && CheckExpirationOnReturn(connection))
		{
			if (System.Net.NetEventSource.Log.IsEnabled())
			{
				connection.Trace("Disposing HTTP/1.1 connection return to pool. Connection lifetime expired.", "ReturnHttp11Connection");
			}
			connection.Dispose();
			return;
		}
		lock (SyncObj)
		{
			if (isNewConnection)
			{
				_pendingHttp11ConnectionCount--;
			}
			if (_http11RequestQueue.TryDequeueNextRequest(connection))
			{
				if (System.Net.NetEventSource.Log.IsEnabled())
				{
					connection.Trace("Dequeued waiting HTTP/1.1 request.", "ReturnHttp11Connection");
				}
				return;
			}
			if (!_disposed)
			{
				_availableHttp11Connections.Add(connection);
				if (System.Net.NetEventSource.Log.IsEnabled())
				{
					connection.Trace("Put connection in pool.", "ReturnHttp11Connection");
				}
				return;
			}
			if (System.Net.NetEventSource.Log.IsEnabled())
			{
				connection.Trace("Disposing connection returned to pool. Pool was disposed.", "ReturnHttp11Connection");
			}
		}
		connection.Dispose();
	}

	public void ReturnHttp2Connection(Http2Connection connection, bool isNewConnection)
	{
		if (System.Net.NetEventSource.Log.IsEnabled())
		{
			connection.Trace($"{"isNewConnection"}={isNewConnection}", "ReturnHttp2Connection");
		}
		if (!isNewConnection && CheckExpirationOnReturn(connection))
		{
			lock (SyncObj)
			{
				_associatedHttp2ConnectionCount--;
			}
			if (System.Net.NetEventSource.Log.IsEnabled())
			{
				connection.Trace("Disposing HTTP/2 connection return to pool. Connection lifetime expired.", "ReturnHttp2Connection");
			}
			connection.Dispose();
			return;
		}
		bool flag = true;
		bool flag2 = false;
		lock (SyncObj)
		{
			if (isNewConnection)
			{
				_pendingHttp2Connection = false;
			}
			while (!_http2RequestQueue.IsEmpty)
			{
				if (!connection.TryReserveStream())
				{
					flag = false;
					if (isNewConnection)
					{
						HttpRequestException ex = new HttpRequestException(System.SR.net_http_http2_connection_not_established);
						ExceptionDispatchInfo.SetCurrentStackTrace(ex);
						_http2RequestQueue.TryFailNextRequest(ex);
					}
					break;
				}
				isNewConnection = false;
				if (!_http2RequestQueue.TryDequeueNextRequest(connection))
				{
					connection.ReleaseStream();
					break;
				}
				if (System.Net.NetEventSource.Log.IsEnabled())
				{
					connection.Trace("Dequeued waiting HTTP/2 request.", "ReturnHttp2Connection");
				}
			}
			CheckForHttp2ConnectionInjection();
			if (_disposed)
			{
				_associatedHttp2ConnectionCount--;
				flag2 = true;
			}
			else if (flag)
			{
				if (_availableHttp2Connections == null)
				{
					_availableHttp2Connections = new List<Http2Connection>();
				}
				_availableHttp2Connections.Add(connection);
				if (System.Net.NetEventSource.Log.IsEnabled())
				{
					connection.Trace("Put HTTP/2 connection in pool.", "ReturnHttp2Connection");
				}
				return;
			}
		}
		if (flag2)
		{
			if (System.Net.NetEventSource.Log.IsEnabled())
			{
				connection.Trace("Disposing HTTP/2 connection returned to pool. Pool was disposed.", "ReturnHttp2Connection");
			}
			connection.Dispose();
		}
		else
		{
			DisableHttp2Connection(connection);
		}
	}

	private void DisableHttp2Connection(Http2Connection connection)
	{
		if (System.Net.NetEventSource.Log.IsEnabled())
		{
			connection.Trace("", "DisableHttp2Connection");
		}
		Task.Run(async delegate
		{
			bool flag = await connection.WaitForAvailableStreamsAsync().ConfigureAwait(continueOnCapturedContext: false);
			if (System.Net.NetEventSource.Log.IsEnabled())
			{
				connection.Trace($"WaitForAvailableStreamsAsync completed, {"usable"}={flag}", "DisableHttp2Connection");
			}
			if (flag)
			{
				ReturnHttp2Connection(connection, isNewConnection: false);
			}
			else
			{
				lock (SyncObj)
				{
					_associatedHttp2ConnectionCount--;
					CheckForHttp2ConnectionInjection();
				}
				if (System.Net.NetEventSource.Log.IsEnabled())
				{
					connection.Trace("HTTP2 connection no longer usable", "DisableHttp2Connection");
				}
				connection.Dispose();
			}
		});
	}

	public void InvalidateHttp3Connection(Http3Connection connection)
	{
		lock (SyncObj)
		{
			if (_http3Connection == connection)
			{
				_http3Connection = null;
			}
		}
	}

	public void Dispose()
	{
		List<HttpConnectionBase> list = null;
		lock (SyncObj)
		{
			if (!_disposed)
			{
				if (System.Net.NetEventSource.Log.IsEnabled())
				{
					Trace("Disposing pool.", "Dispose");
				}
				_disposed = true;
				list = new List<HttpConnectionBase>(_availableHttp11Connections.Count + (_availableHttp2Connections?.Count ?? 0));
				list.AddRange(_availableHttp11Connections);
				if (_availableHttp2Connections != null)
				{
					list.AddRange(_availableHttp2Connections);
				}
				_availableHttp11Connections.Clear();
				_associatedHttp2ConnectionCount -= _availableHttp2Connections?.Count ?? 0;
				_availableHttp2Connections?.Clear();
				if (_http3Connection != null)
				{
					list.Add(_http3Connection);
					_http3Connection = null;
				}
				if (_authorityExpireTimer != null)
				{
					_authorityExpireTimer.Dispose();
					_authorityExpireTimer = null;
				}
				if (_altSvcBlocklistTimerCancellation != null)
				{
					_altSvcBlocklistTimerCancellation.Cancel();
					_altSvcBlocklistTimerCancellation.Dispose();
					_altSvcBlocklistTimerCancellation = null;
				}
			}
		}
		list?.ForEach(delegate(HttpConnectionBase c)
		{
			c.Dispose();
		});
	}

	public bool CleanCacheAndDisposeIfUnused()
	{
		TimeSpan pooledConnectionLifetime2 = _poolManager.Settings._pooledConnectionLifetime;
		TimeSpan pooledConnectionIdleTimeout2 = _poolManager.Settings._pooledConnectionIdleTimeout;
		List<HttpConnectionBase> toDispose2 = null;
		lock (SyncObj)
		{
			if (!_usedSinceLastCleanup && _associatedHttp11ConnectionCount == 0 && _associatedHttp2ConnectionCount == 0)
			{
				_disposed = true;
				return true;
			}
			_usedSinceLastCleanup = false;
			long tickCount = Environment.TickCount64;
			ScavengeConnectionList<HttpConnection>(_availableHttp11Connections, ref toDispose2, tickCount, pooledConnectionLifetime2, pooledConnectionIdleTimeout2);
			if (_availableHttp2Connections != null)
			{
				int num = ScavengeConnectionList<Http2Connection>(_availableHttp2Connections, ref toDispose2, tickCount, pooledConnectionLifetime2, pooledConnectionIdleTimeout2);
				_associatedHttp2ConnectionCount -= num;
			}
		}
		toDispose2?.ForEach(delegate(HttpConnectionBase c)
		{
			c.Dispose();
		});
		return false;
		static bool IsUsableConnection(HttpConnectionBase connection, long nowTicks, TimeSpan pooledConnectionLifetime, TimeSpan pooledConnectionIdleTimeout)
		{
			if (pooledConnectionIdleTimeout != Timeout.InfiniteTimeSpan)
			{
				long idleTicks = connection.GetIdleTicks(nowTicks);
				if ((double)idleTicks > pooledConnectionIdleTimeout.TotalMilliseconds)
				{
					if (System.Net.NetEventSource.Log.IsEnabled())
					{
						connection.Trace($"Scavenging connection. Idle {TimeSpan.FromMilliseconds(idleTicks)} > {pooledConnectionIdleTimeout}.", "CleanCacheAndDisposeIfUnused");
					}
					return false;
				}
			}
			if (pooledConnectionLifetime != Timeout.InfiniteTimeSpan)
			{
				long lifetimeTicks = connection.GetLifetimeTicks(nowTicks);
				if ((double)lifetimeTicks > pooledConnectionLifetime.TotalMilliseconds)
				{
					if (System.Net.NetEventSource.Log.IsEnabled())
					{
						connection.Trace($"Scavenging connection. Lifetime {TimeSpan.FromMilliseconds(lifetimeTicks)} > {pooledConnectionLifetime}.", "CleanCacheAndDisposeIfUnused");
					}
					return false;
				}
			}
			if (!connection.CheckUsabilityOnScavenge())
			{
				if (System.Net.NetEventSource.Log.IsEnabled())
				{
					connection.Trace("Scavenging connection. Unexpected data or EOF received.", "CleanCacheAndDisposeIfUnused");
				}
				return false;
			}
			return true;
		}
		static int ScavengeConnectionList<T>(List<T> list, ref List<HttpConnectionBase> toDispose, long nowTicks, TimeSpan pooledConnectionLifetime, TimeSpan pooledConnectionIdleTimeout) where T : HttpConnectionBase
		{
			int i;
			for (i = 0; i < list.Count && IsUsableConnection(list[i], nowTicks, pooledConnectionLifetime, pooledConnectionIdleTimeout); i++)
			{
			}
			int num2 = 0;
			if (i < list.Count)
			{
				if (toDispose == null)
				{
					toDispose = new List<HttpConnectionBase> { list[i] };
				}
				int j = i + 1;
				while (j < list.Count)
				{
					for (; j < list.Count && !IsUsableConnection(list[j], nowTicks, pooledConnectionLifetime, pooledConnectionIdleTimeout); j++)
					{
						toDispose.Add(list[j]);
					}
					if (j < list.Count)
					{
						list[i++] = list[j++];
					}
				}
				num2 = list.Count - i;
				list.RemoveRange(i, num2);
			}
			return num2;
		}
	}

	private static bool GetIsWindows7Or2008R2()
	{
		OperatingSystem oSVersion = Environment.OSVersion;
		if (oSVersion.Platform == PlatformID.Win32NT)
		{
			Version version = oSVersion.Version;
			if (version.Major == 6)
			{
				return version.Minor == 1;
			}
			return false;
		}
		return false;
	}

	internal void HeartBeat()
	{
		Http2Connection[] array;
		lock (SyncObj)
		{
			array = _availableHttp2Connections?.ToArray();
		}
		if (array != null)
		{
			Http2Connection[] array2 = array;
			foreach (Http2Connection http2Connection in array2)
			{
				http2Connection.HeartBeat();
			}
		}
	}

	public override string ToString()
	{
		return "HttpConnectionPool " + ((!(_proxyUri == null)) ? ((_sslOptionsHttp11 == null) ? $"Proxy {_proxyUri}" : ($"https://{_originAuthority}/ tunnelled via Proxy {_proxyUri}" + ((_sslOptionsHttp11.TargetHost != _originAuthority.IdnHost) ? (", SSL TargetHost=" + _sslOptionsHttp11.TargetHost) : null))) : ((_sslOptionsHttp11 == null) ? $"http://{_originAuthority}" : ($"https://{_originAuthority}" + ((_sslOptionsHttp11.TargetHost != _originAuthority.IdnHost) ? (", SSL TargetHost=" + _sslOptionsHttp11.TargetHost) : null))));
	}

	private void Trace(string message, [CallerMemberName] string memberName = null)
	{
		System.Net.NetEventSource.Log.HandlerMessage(GetHashCode(), 0, 0, memberName, message);
	}
}
