using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net.Quic.Implementations;
using System.Net.Security;
using System.Runtime.Versioning;
using System.Threading;
using System.Threading.Tasks;

namespace System.Net.Http;

internal sealed class HttpConnectionSettings
{
	internal DecompressionMethods _automaticDecompression;

	internal bool _useCookies = true;

	internal CookieContainer _cookieContainer;

	internal bool _useProxy = true;

	internal IWebProxy _proxy;

	internal ICredentials _defaultProxyCredentials;

	internal bool _defaultCredentialsUsedForProxy;

	internal bool _defaultCredentialsUsedForServer;

	internal bool _preAuthenticate;

	internal ICredentials _credentials;

	internal bool _allowAutoRedirect = true;

	internal int _maxAutomaticRedirections = 50;

	internal int _maxConnectionsPerServer = int.MaxValue;

	internal int _maxResponseDrainSize = 1048576;

	internal TimeSpan _maxResponseDrainTime = HttpHandlerDefaults.DefaultResponseDrainTimeout;

	internal int _maxResponseHeadersLength = 64;

	internal TimeSpan _pooledConnectionLifetime = HttpHandlerDefaults.DefaultPooledConnectionLifetime;

	internal TimeSpan _pooledConnectionIdleTimeout = HttpHandlerDefaults.DefaultPooledConnectionIdleTimeout;

	internal TimeSpan _expect100ContinueTimeout = HttpHandlerDefaults.DefaultExpect100ContinueTimeout;

	internal TimeSpan _keepAlivePingTimeout = HttpHandlerDefaults.DefaultKeepAlivePingTimeout;

	internal TimeSpan _keepAlivePingDelay = HttpHandlerDefaults.DefaultKeepAlivePingDelay;

	internal HttpKeepAlivePingPolicy _keepAlivePingPolicy = HttpKeepAlivePingPolicy.Always;

	internal TimeSpan _connectTimeout = HttpHandlerDefaults.DefaultConnectTimeout;

	internal HeaderEncodingSelector<HttpRequestMessage> _requestHeaderEncodingSelector;

	internal HeaderEncodingSelector<HttpRequestMessage> _responseHeaderEncodingSelector;

	internal DistributedContextPropagator _activityHeadersPropagator = DistributedContextPropagator.Current;

	internal Version _maxHttpVersion;

	internal SslClientAuthenticationOptions _sslOptions;

	internal bool _enableMultipleHttp2Connections;

	internal Func<SocketsHttpConnectionContext, CancellationToken, ValueTask<Stream>> _connectCallback;

	internal Func<SocketsHttpPlaintextStreamFilterContext, CancellationToken, ValueTask<Stream>> _plaintextStreamFilter;

	internal QuicImplementationProvider _quicImplementationProvider;

	internal IDictionary<string, object> _properties;

	internal int _initialHttp2StreamWindowSize = 65535;

	private byte[] _http3SettingsFrame;

	public bool EnableMultipleHttp2Connections => _enableMultipleHttp2Connections;

	[SupportedOSPlatform("windows")]
	[SupportedOSPlatform("linux")]
	[SupportedOSPlatform("macos")]
	internal byte[] Http3SettingsFrame => _http3SettingsFrame ?? (_http3SettingsFrame = Http3Connection.BuildSettingsFrame(this));

	public HttpConnectionSettings()
	{
		bool allowHttp = GlobalHttpSettings.SocketsHttpHandler.AllowHttp2;
		bool allowHttp2 = GlobalHttpSettings.SocketsHttpHandler.AllowHttp3;
		_maxHttpVersion = ((allowHttp2 && allowHttp) ? HttpVersion.Version30 : (allowHttp ? HttpVersion.Version20 : HttpVersion.Version11));
	}

	public HttpConnectionSettings CloneAndNormalize()
	{
		if (_useCookies && _cookieContainer == null)
		{
			_cookieContainer = new CookieContainer();
		}
		HttpConnectionSettings httpConnectionSettings = new HttpConnectionSettings
		{
			_allowAutoRedirect = _allowAutoRedirect,
			_automaticDecompression = _automaticDecompression,
			_cookieContainer = _cookieContainer,
			_connectTimeout = _connectTimeout,
			_credentials = _credentials,
			_defaultProxyCredentials = _defaultProxyCredentials,
			_expect100ContinueTimeout = _expect100ContinueTimeout,
			_maxAutomaticRedirections = _maxAutomaticRedirections,
			_maxConnectionsPerServer = _maxConnectionsPerServer,
			_maxHttpVersion = _maxHttpVersion,
			_maxResponseDrainSize = _maxResponseDrainSize,
			_maxResponseDrainTime = _maxResponseDrainTime,
			_maxResponseHeadersLength = _maxResponseHeadersLength,
			_pooledConnectionLifetime = _pooledConnectionLifetime,
			_pooledConnectionIdleTimeout = _pooledConnectionIdleTimeout,
			_preAuthenticate = _preAuthenticate,
			_properties = _properties,
			_proxy = _proxy,
			_sslOptions = _sslOptions?.ShallowClone(),
			_useCookies = _useCookies,
			_useProxy = _useProxy,
			_keepAlivePingTimeout = _keepAlivePingTimeout,
			_keepAlivePingDelay = _keepAlivePingDelay,
			_keepAlivePingPolicy = _keepAlivePingPolicy,
			_requestHeaderEncodingSelector = _requestHeaderEncodingSelector,
			_responseHeaderEncodingSelector = _responseHeaderEncodingSelector,
			_enableMultipleHttp2Connections = _enableMultipleHttp2Connections,
			_connectCallback = _connectCallback,
			_plaintextStreamFilter = _plaintextStreamFilter,
			_initialHttp2StreamWindowSize = _initialHttp2StreamWindowSize,
			_activityHeadersPropagator = _activityHeadersPropagator,
			_defaultCredentialsUsedForProxy = (_proxy != null && (_proxy.Credentials == CredentialCache.DefaultCredentials || _defaultProxyCredentials == CredentialCache.DefaultCredentials)),
			_defaultCredentialsUsedForServer = (_credentials == CredentialCache.DefaultCredentials)
		};
		if (HttpConnectionPool.IsHttp3Supported())
		{
			httpConnectionSettings._quicImplementationProvider = _quicImplementationProvider;
		}
		return httpConnectionSettings;
	}
}
