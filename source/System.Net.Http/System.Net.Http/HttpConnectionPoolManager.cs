using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Net.NetworkInformation;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Threading.Tasks;

namespace System.Net.Http;

internal sealed class HttpConnectionPoolManager : IDisposable
{
	private sealed class NetworkChangeCleanup : IDisposable
	{
		private readonly NetworkAddressChangedEventHandler _handler;

		public NetworkChangeCleanup(NetworkAddressChangedEventHandler handler)
		{
			_handler = handler;
		}

		~NetworkChangeCleanup()
		{
			NetworkChange.NetworkAddressChanged -= _handler;
		}

		public void Dispose()
		{
			NetworkChange.NetworkAddressChanged -= _handler;
			GC.SuppressFinalize(this);
		}
	}

	internal readonly struct HttpConnectionKey : IEquatable<HttpConnectionKey>
	{
		public readonly HttpConnectionKind Kind;

		public readonly string Host;

		public readonly int Port;

		public readonly string SslHostName;

		public readonly Uri ProxyUri;

		public readonly string Identity;

		public HttpConnectionKey(HttpConnectionKind kind, string host, int port, string sslHostName, Uri proxyUri, string identity)
		{
			Kind = kind;
			Host = host;
			Port = port;
			SslHostName = sslHostName;
			ProxyUri = proxyUri;
			Identity = identity;
		}

		public override int GetHashCode()
		{
			if (!(SslHostName == Host))
			{
				return HashCode.Combine(Kind, Host, Port, SslHostName, ProxyUri, Identity);
			}
			return HashCode.Combine(Kind, Host, Port, ProxyUri, Identity);
		}

		public override bool Equals([NotNullWhen(true)] object obj)
		{
			if (obj is HttpConnectionKey other)
			{
				return Equals(other);
			}
			return false;
		}

		public bool Equals(HttpConnectionKey other)
		{
			if (Kind == other.Kind && Host == other.Host && Port == other.Port && ProxyUri == other.ProxyUri && SslHostName == other.SslHostName)
			{
				return Identity == other.Identity;
			}
			return false;
		}
	}

	private readonly TimeSpan _cleanPoolTimeout;

	private readonly ConcurrentDictionary<HttpConnectionKey, HttpConnectionPool> _pools;

	private readonly Timer _cleaningTimer;

	private readonly Timer _heartBeatTimer;

	private readonly HttpConnectionSettings _settings;

	private readonly IWebProxy _proxy;

	private readonly ICredentials _proxyCredentials;

	private NetworkChangeCleanup _networkChangeCleanup;

	private bool _timerIsRunning;

	private object SyncObj => _pools;

	public HttpConnectionSettings Settings => _settings;

	public ICredentials ProxyCredentials => _proxyCredentials;

	public HttpConnectionPoolManager(HttpConnectionSettings settings)
	{
		_settings = settings;
		_pools = new ConcurrentDictionary<HttpConnectionKey, HttpConnectionPool>();
		if (settings._maxConnectionsPerServer != int.MaxValue || (!(settings._pooledConnectionIdleTimeout == TimeSpan.Zero) && !(settings._pooledConnectionLifetime == TimeSpan.Zero)))
		{
			if (settings._pooledConnectionIdleTimeout == Timeout.InfiniteTimeSpan)
			{
				_cleanPoolTimeout = TimeSpan.FromSeconds(30.0);
			}
			else
			{
				TimeSpan timeSpan = settings._pooledConnectionIdleTimeout / 4.0;
				_cleanPoolTimeout = ((timeSpan.TotalSeconds >= 1.0) ? timeSpan : TimeSpan.FromSeconds(1.0));
			}
			bool flag = false;
			try
			{
				if (!ExecutionContext.IsFlowSuppressed())
				{
					ExecutionContext.SuppressFlow();
					flag = true;
				}
				WeakReference<HttpConnectionPoolManager> state2 = new WeakReference<HttpConnectionPoolManager>(this);
				_cleaningTimer = new Timer(delegate(object s)
				{
					WeakReference<HttpConnectionPoolManager> weakReference2 = (WeakReference<HttpConnectionPoolManager>)s;
					if (weakReference2.TryGetTarget(out var target2))
					{
						target2.RemoveStalePools();
					}
				}, state2, -1, -1);
				if (_settings._keepAlivePingDelay != Timeout.InfiniteTimeSpan)
				{
					long num = (long)Math.Max(1000.0, Math.Min(_settings._keepAlivePingDelay.TotalMilliseconds, _settings._keepAlivePingTimeout.TotalMilliseconds) / 4.0);
					_heartBeatTimer = new Timer(delegate(object state)
					{
						WeakReference<HttpConnectionPoolManager> weakReference = (WeakReference<HttpConnectionPoolManager>)state;
						if (weakReference.TryGetTarget(out var target))
						{
							target.HeartBeat();
						}
					}, state2, num, num);
				}
			}
			finally
			{
				if (flag)
				{
					ExecutionContext.RestoreFlow();
				}
			}
		}
		if (settings._useProxy)
		{
			_proxy = settings._proxy ?? HttpClient.DefaultProxy;
			if (_proxy != null)
			{
				_proxyCredentials = _proxy.Credentials ?? settings._defaultProxyCredentials;
			}
		}
	}

	public void StartMonitoringNetworkChanges()
	{
		if (_networkChangeCleanup != null)
		{
			return;
		}
		WeakReference<ConcurrentDictionary<HttpConnectionKey, HttpConnectionPool>> poolsRef = new WeakReference<ConcurrentDictionary<HttpConnectionKey, HttpConnectionPool>>(_pools);
		NetworkAddressChangedEventHandler networkAddressChangedEventHandler = delegate
		{
			if (poolsRef.TryGetTarget(out var target))
			{
				foreach (HttpConnectionPool value in target.Values)
				{
					value.OnNetworkChanged();
				}
			}
		};
		NetworkChangeCleanup networkChangeCleanup = new NetworkChangeCleanup(networkAddressChangedEventHandler);
		if (Interlocked.CompareExchange(ref _networkChangeCleanup, networkChangeCleanup, null) != null)
		{
			GC.SuppressFinalize(networkChangeCleanup);
			return;
		}
		if (!ExecutionContext.IsFlowSuppressed())
		{
			using (ExecutionContext.SuppressFlow())
			{
				NetworkChange.NetworkAddressChanged += networkAddressChangedEventHandler;
				return;
			}
		}
		NetworkChange.NetworkAddressChanged += networkAddressChangedEventHandler;
	}

	private static string ParseHostNameFromHeader(string hostHeader)
	{
		int num = hostHeader.IndexOf(':');
		if (num >= 0)
		{
			int num2 = hostHeader.IndexOf(']');
			if (num2 == -1)
			{
				return hostHeader.Substring(0, num);
			}
			num = hostHeader.LastIndexOf(':');
			if (num > num2)
			{
				return hostHeader.Substring(0, num);
			}
		}
		return hostHeader;
	}

	private HttpConnectionKey GetConnectionKey(HttpRequestMessage request, Uri proxyUri, bool isProxyConnect)
	{
		Uri requestUri = request.RequestUri;
		if (isProxyConnect)
		{
			return new HttpConnectionKey(HttpConnectionKind.ProxyConnect, requestUri.IdnHost, requestUri.Port, null, proxyUri, GetIdentityIfDefaultCredentialsUsed(_settings._defaultCredentialsUsedForProxy));
		}
		string text = null;
		if (HttpUtilities.IsSupportedSecureScheme(requestUri.Scheme))
		{
			string host = request.Headers.Host;
			text = ((host == null) ? requestUri.IdnHost : ParseHostNameFromHeader(host));
		}
		string identityIfDefaultCredentialsUsed = GetIdentityIfDefaultCredentialsUsed((proxyUri != null) ? _settings._defaultCredentialsUsedForProxy : _settings._defaultCredentialsUsedForServer);
		if (proxyUri != null)
		{
			if (HttpUtilities.IsSocksScheme(proxyUri.Scheme))
			{
				if (text != null)
				{
					return new HttpConnectionKey(HttpConnectionKind.SslSocksTunnel, requestUri.IdnHost, requestUri.Port, text, proxyUri, identityIfDefaultCredentialsUsed);
				}
				return new HttpConnectionKey(HttpConnectionKind.SocksTunnel, requestUri.IdnHost, requestUri.Port, null, proxyUri, identityIfDefaultCredentialsUsed);
			}
			if (text == null)
			{
				if (HttpUtilities.IsNonSecureWebSocketScheme(requestUri.Scheme))
				{
					return new HttpConnectionKey(HttpConnectionKind.ProxyTunnel, requestUri.IdnHost, requestUri.Port, null, proxyUri, identityIfDefaultCredentialsUsed);
				}
				return new HttpConnectionKey(HttpConnectionKind.Proxy, null, 0, null, proxyUri, identityIfDefaultCredentialsUsed);
			}
			return new HttpConnectionKey(HttpConnectionKind.SslProxyTunnel, requestUri.IdnHost, requestUri.Port, text, proxyUri, identityIfDefaultCredentialsUsed);
		}
		if (text != null)
		{
			return new HttpConnectionKey(HttpConnectionKind.Https, requestUri.IdnHost, requestUri.Port, text, null, identityIfDefaultCredentialsUsed);
		}
		return new HttpConnectionKey(HttpConnectionKind.Http, requestUri.IdnHost, requestUri.Port, null, null, identityIfDefaultCredentialsUsed);
	}

	public ValueTask<HttpResponseMessage> SendAsyncCore(HttpRequestMessage request, Uri proxyUri, bool async, bool doRequestAuth, bool isProxyConnect, CancellationToken cancellationToken)
	{
		HttpConnectionKey connectionKey = GetConnectionKey(request, proxyUri, isProxyConnect);
		HttpConnectionPool value;
		while (!_pools.TryGetValue(connectionKey, out value))
		{
			value = new HttpConnectionPool(this, connectionKey.Kind, connectionKey.Host, connectionKey.Port, connectionKey.SslHostName, connectionKey.ProxyUri);
			if (_cleaningTimer == null)
			{
				break;
			}
			if (!_pools.TryAdd(connectionKey, value))
			{
				continue;
			}
			lock (SyncObj)
			{
				if (!_timerIsRunning)
				{
					SetCleaningTimer(_cleanPoolTimeout);
				}
			}
			break;
		}
		return value.SendAsync(request, async, doRequestAuth, cancellationToken);
	}

	public ValueTask<HttpResponseMessage> SendProxyConnectAsync(HttpRequestMessage request, Uri proxyUri, bool async, CancellationToken cancellationToken)
	{
		return SendAsyncCore(request, proxyUri, async, doRequestAuth: false, isProxyConnect: true, cancellationToken);
	}

	public ValueTask<HttpResponseMessage> SendAsync(HttpRequestMessage request, bool async, bool doRequestAuth, CancellationToken cancellationToken)
	{
		if (_proxy == null)
		{
			return SendAsyncCore(request, null, async, doRequestAuth, isProxyConnect: false, cancellationToken);
		}
		Uri uri = null;
		try
		{
			if (!_proxy.IsBypassed(request.RequestUri))
			{
				if (_proxy is IMultiWebProxy multiWebProxy)
				{
					MultiProxy multiProxy = multiWebProxy.GetMultiProxy(request.RequestUri);
					if (multiProxy.ReadNext(out uri, out var isFinalProxy) && !isFinalProxy)
					{
						return SendAsyncMultiProxy(request, async, doRequestAuth, multiProxy, uri, cancellationToken);
					}
				}
				else
				{
					uri = _proxy.GetProxy(request.RequestUri);
				}
			}
		}
		catch (Exception ex)
		{
			if (System.Net.NetEventSource.Log.IsEnabled())
			{
				System.Net.NetEventSource.Error(this, $"Exception from {_proxy.GetType().Name}.GetProxy({request.RequestUri}): {ex}", "SendAsync");
			}
		}
		if (uri != null && !HttpUtilities.IsSupportedProxyScheme(uri.Scheme))
		{
			throw new NotSupportedException(System.SR.net_http_invalid_proxy_scheme);
		}
		return SendAsyncCore(request, uri, async, doRequestAuth, isProxyConnect: false, cancellationToken);
	}

	private async ValueTask<HttpResponseMessage> SendAsyncMultiProxy(HttpRequestMessage request, bool async, bool doRequestAuth, MultiProxy multiProxy, Uri firstProxy, CancellationToken cancellationToken)
	{
		HttpRequestException source;
		bool isFinalProxy;
		do
		{
			try
			{
				return await SendAsyncCore(request, firstProxy, async, doRequestAuth, isProxyConnect: false, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
			}
			catch (HttpRequestException ex) when (ex.AllowRetry != RequestRetryType.NoRetry)
			{
				source = ex;
			}
		}
		while (multiProxy.ReadNext(out firstProxy, out isFinalProxy));
		ExceptionDispatchInfo.Throw(source);
		return null;
	}

	public void Dispose()
	{
		_cleaningTimer?.Dispose();
		_heartBeatTimer?.Dispose();
		foreach (KeyValuePair<HttpConnectionKey, HttpConnectionPool> pool in _pools)
		{
			pool.Value.Dispose();
		}
		_networkChangeCleanup?.Dispose();
	}

	private void SetCleaningTimer(TimeSpan timeout)
	{
		try
		{
			_cleaningTimer.Change(timeout, timeout);
			_timerIsRunning = timeout != Timeout.InfiniteTimeSpan;
		}
		catch (ObjectDisposedException)
		{
		}
	}

	private void RemoveStalePools()
	{
		foreach (KeyValuePair<HttpConnectionKey, HttpConnectionPool> pool in _pools)
		{
			if (pool.Value.CleanCacheAndDisposeIfUnused())
			{
				_pools.TryRemove(pool.Key, out var _);
			}
		}
		lock (SyncObj)
		{
			if (_pools.IsEmpty)
			{
				SetCleaningTimer(Timeout.InfiniteTimeSpan);
			}
		}
	}

	private void HeartBeat()
	{
		foreach (KeyValuePair<HttpConnectionKey, HttpConnectionPool> pool in _pools)
		{
			pool.Value.HeartBeat();
		}
	}

	private static string GetIdentityIfDefaultCredentialsUsed(bool defaultCredentialsUsed)
	{
		if (!defaultCredentialsUsed)
		{
			return string.Empty;
		}
		return CurrentUserIdentityProvider.GetIdentity();
	}
}
