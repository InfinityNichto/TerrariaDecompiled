using System.Collections.Generic;
using System.Globalization;
using System.Net.Security;
using System.Runtime.Versioning;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;

namespace System.Net.Http;

public class HttpClientHandler : HttpMessageHandler
{
	private readonly SocketsHttpHandler _underlyingHandler;

	private ClientCertificateOption _clientCertificateOptions;

	private volatile bool _disposed;

	private static Func<HttpRequestMessage, X509Certificate2, X509Chain, SslPolicyErrors, bool> s_dangerousAcceptAnyServerCertificateValidator;

	private HttpMessageHandler Handler => _underlyingHandler;

	public virtual bool SupportsAutomaticDecompression => true;

	public virtual bool SupportsProxy => true;

	public virtual bool SupportsRedirectConfiguration => true;

	[UnsupportedOSPlatform("browser")]
	public bool UseCookies
	{
		get
		{
			return _underlyingHandler.UseCookies;
		}
		set
		{
			_underlyingHandler.UseCookies = value;
		}
	}

	[UnsupportedOSPlatform("browser")]
	public CookieContainer CookieContainer
	{
		get
		{
			return _underlyingHandler.CookieContainer;
		}
		set
		{
			if (value == null)
			{
				throw new ArgumentNullException("value");
			}
			_underlyingHandler.CookieContainer = value;
		}
	}

	[UnsupportedOSPlatform("browser")]
	public DecompressionMethods AutomaticDecompression
	{
		get
		{
			return _underlyingHandler.AutomaticDecompression;
		}
		set
		{
			_underlyingHandler.AutomaticDecompression = value;
		}
	}

	[UnsupportedOSPlatform("browser")]
	public bool UseProxy
	{
		get
		{
			return _underlyingHandler.UseProxy;
		}
		set
		{
			_underlyingHandler.UseProxy = value;
		}
	}

	[UnsupportedOSPlatform("browser")]
	[UnsupportedOSPlatform("ios")]
	[UnsupportedOSPlatform("tvos")]
	public IWebProxy? Proxy
	{
		get
		{
			return _underlyingHandler.Proxy;
		}
		set
		{
			_underlyingHandler.Proxy = value;
		}
	}

	[UnsupportedOSPlatform("browser")]
	public ICredentials? DefaultProxyCredentials
	{
		get
		{
			return _underlyingHandler.DefaultProxyCredentials;
		}
		set
		{
			_underlyingHandler.DefaultProxyCredentials = value;
		}
	}

	[UnsupportedOSPlatform("browser")]
	public bool PreAuthenticate
	{
		get
		{
			return _underlyingHandler.PreAuthenticate;
		}
		set
		{
			_underlyingHandler.PreAuthenticate = value;
		}
	}

	[UnsupportedOSPlatform("browser")]
	public bool UseDefaultCredentials
	{
		get
		{
			return _underlyingHandler.Credentials == CredentialCache.DefaultCredentials;
		}
		set
		{
			if (value)
			{
				_underlyingHandler.Credentials = CredentialCache.DefaultCredentials;
			}
			else if (_underlyingHandler.Credentials == CredentialCache.DefaultCredentials)
			{
				_underlyingHandler.Credentials = null;
			}
		}
	}

	[UnsupportedOSPlatform("browser")]
	public ICredentials? Credentials
	{
		get
		{
			return _underlyingHandler.Credentials;
		}
		set
		{
			_underlyingHandler.Credentials = value;
		}
	}

	public bool AllowAutoRedirect
	{
		get
		{
			return _underlyingHandler.AllowAutoRedirect;
		}
		set
		{
			_underlyingHandler.AllowAutoRedirect = value;
		}
	}

	[UnsupportedOSPlatform("browser")]
	public int MaxAutomaticRedirections
	{
		get
		{
			return _underlyingHandler.MaxAutomaticRedirections;
		}
		set
		{
			_underlyingHandler.MaxAutomaticRedirections = value;
		}
	}

	[UnsupportedOSPlatform("browser")]
	public int MaxConnectionsPerServer
	{
		get
		{
			return _underlyingHandler.MaxConnectionsPerServer;
		}
		set
		{
			_underlyingHandler.MaxConnectionsPerServer = value;
		}
	}

	public long MaxRequestContentBufferSize
	{
		get
		{
			return 0L;
		}
		set
		{
			if (value < 0)
			{
				throw new ArgumentOutOfRangeException("value");
			}
			if (value > int.MaxValue)
			{
				throw new ArgumentOutOfRangeException("value", value, System.SR.Format(CultureInfo.InvariantCulture, System.SR.net_http_content_buffersize_limit, int.MaxValue));
			}
			CheckDisposed();
		}
	}

	[UnsupportedOSPlatform("browser")]
	public int MaxResponseHeadersLength
	{
		get
		{
			return _underlyingHandler.MaxResponseHeadersLength;
		}
		set
		{
			_underlyingHandler.MaxResponseHeadersLength = value;
		}
	}

	public ClientCertificateOption ClientCertificateOptions
	{
		get
		{
			return _clientCertificateOptions;
		}
		set
		{
			switch (value)
			{
			case ClientCertificateOption.Manual:
				ThrowForModifiedManagedSslOptionsIfStarted();
				_clientCertificateOptions = value;
				_underlyingHandler.SslOptions.LocalCertificateSelectionCallback = (object sender, string targetHost, X509CertificateCollection localCertificates, X509Certificate remoteCertificate, string[] acceptableIssuers) => CertificateHelper.GetEligibleClientCertificate(ClientCertificates);
				break;
			case ClientCertificateOption.Automatic:
				ThrowForModifiedManagedSslOptionsIfStarted();
				_clientCertificateOptions = value;
				_underlyingHandler.SslOptions.LocalCertificateSelectionCallback = (object sender, string targetHost, X509CertificateCollection localCertificates, X509Certificate remoteCertificate, string[] acceptableIssuers) => CertificateHelper.GetEligibleClientCertificate();
				break;
			default:
				throw new ArgumentOutOfRangeException("value");
			}
		}
	}

	[UnsupportedOSPlatform("browser")]
	public X509CertificateCollection ClientCertificates
	{
		get
		{
			if (ClientCertificateOptions != 0)
			{
				throw new InvalidOperationException(System.SR.Format(System.SR.net_http_invalid_enable_first, "ClientCertificateOptions", "Manual"));
			}
			return _underlyingHandler.SslOptions.ClientCertificates ?? (_underlyingHandler.SslOptions.ClientCertificates = new X509CertificateCollection());
		}
	}

	[UnsupportedOSPlatform("browser")]
	public Func<HttpRequestMessage, X509Certificate2?, X509Chain?, SslPolicyErrors, bool>? ServerCertificateCustomValidationCallback
	{
		get
		{
			return (_underlyingHandler.SslOptions.RemoteCertificateValidationCallback?.Target as ConnectHelper.CertificateCallbackMapper)?.FromHttpClientHandler;
		}
		set
		{
			ThrowForModifiedManagedSslOptionsIfStarted();
			_underlyingHandler.SslOptions.RemoteCertificateValidationCallback = ((value != null) ? new ConnectHelper.CertificateCallbackMapper(value).ForSocketsHttpHandler : null);
		}
	}

	[UnsupportedOSPlatform("browser")]
	public bool CheckCertificateRevocationList
	{
		get
		{
			return _underlyingHandler.SslOptions.CertificateRevocationCheckMode == X509RevocationMode.Online;
		}
		set
		{
			ThrowForModifiedManagedSslOptionsIfStarted();
			_underlyingHandler.SslOptions.CertificateRevocationCheckMode = (value ? X509RevocationMode.Online : X509RevocationMode.NoCheck);
		}
	}

	[UnsupportedOSPlatform("browser")]
	public SslProtocols SslProtocols
	{
		get
		{
			return _underlyingHandler.SslOptions.EnabledSslProtocols;
		}
		set
		{
			ThrowForModifiedManagedSslOptionsIfStarted();
			_underlyingHandler.SslOptions.EnabledSslProtocols = value;
		}
	}

	public IDictionary<string, object?> Properties => _underlyingHandler.Properties;

	[UnsupportedOSPlatform("browser")]
	public static Func<HttpRequestMessage, X509Certificate2?, X509Chain?, SslPolicyErrors, bool> DangerousAcceptAnyServerCertificateValidator => Volatile.Read(ref s_dangerousAcceptAnyServerCertificateValidator) ?? Interlocked.CompareExchange(ref s_dangerousAcceptAnyServerCertificateValidator, (HttpRequestMessage _003Cp0_003E, X509Certificate2 _003Cp1_003E, X509Chain _003Cp2_003E, SslPolicyErrors _003Cp3_003E) => true, null) ?? s_dangerousAcceptAnyServerCertificateValidator;

	public HttpClientHandler()
	{
		_underlyingHandler = new SocketsHttpHandler();
		ClientCertificateOptions = ClientCertificateOption.Manual;
	}

	protected override void Dispose(bool disposing)
	{
		if (disposing && !_disposed)
		{
			_disposed = true;
			_underlyingHandler.Dispose();
		}
		base.Dispose(disposing);
	}

	[UnsupportedOSPlatform("browser")]
	protected internal override HttpResponseMessage Send(HttpRequestMessage request, CancellationToken cancellationToken)
	{
		return Handler.Send(request, cancellationToken);
	}

	protected internal override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
	{
		return Handler.SendAsync(request, cancellationToken);
	}

	private void ThrowForModifiedManagedSslOptionsIfStarted()
	{
		_underlyingHandler.SslOptions = _underlyingHandler.SslOptions;
	}

	private void CheckDisposed()
	{
		if (_disposed)
		{
			throw new ObjectDisposedException(GetType().ToString());
		}
	}
}
