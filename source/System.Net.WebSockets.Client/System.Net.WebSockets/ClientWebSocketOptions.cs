using System.Collections.Generic;
using System.Net.Security;
using System.Runtime.Versioning;
using System.Security.Cryptography.X509Certificates;
using System.Threading;

namespace System.Net.WebSockets;

public sealed class ClientWebSocketOptions
{
	private bool _isReadOnly;

	private TimeSpan _keepAliveInterval = WebSocket.DefaultKeepAliveInterval;

	private bool _useDefaultCredentials;

	private ICredentials _credentials;

	private IWebProxy _proxy;

	private CookieContainer _cookies;

	private int _receiveBufferSize = 4096;

	private ArraySegment<byte>? _buffer;

	private RemoteCertificateValidationCallback _remoteCertificateValidationCallback;

	internal X509CertificateCollection _clientCertificates;

	internal WebHeaderCollection _requestHeaders;

	internal List<string> _requestedSubProtocols;

	internal WebHeaderCollection RequestHeaders => _requestHeaders ?? (_requestHeaders = new WebHeaderCollection());

	internal List<string> RequestedSubProtocols => _requestedSubProtocols ?? (_requestedSubProtocols = new List<string>());

	[UnsupportedOSPlatform("browser")]
	public bool UseDefaultCredentials
	{
		get
		{
			return _useDefaultCredentials;
		}
		set
		{
			ThrowIfReadOnly();
			_useDefaultCredentials = value;
		}
	}

	[UnsupportedOSPlatform("browser")]
	public ICredentials? Credentials
	{
		get
		{
			return _credentials;
		}
		set
		{
			ThrowIfReadOnly();
			_credentials = value;
		}
	}

	[UnsupportedOSPlatform("browser")]
	public IWebProxy? Proxy
	{
		get
		{
			return _proxy;
		}
		set
		{
			ThrowIfReadOnly();
			_proxy = value;
		}
	}

	[UnsupportedOSPlatform("browser")]
	public X509CertificateCollection ClientCertificates
	{
		get
		{
			return _clientCertificates ?? (_clientCertificates = new X509CertificateCollection());
		}
		set
		{
			ThrowIfReadOnly();
			_clientCertificates = value ?? throw new ArgumentNullException("value");
		}
	}

	[UnsupportedOSPlatform("browser")]
	public RemoteCertificateValidationCallback? RemoteCertificateValidationCallback
	{
		get
		{
			return _remoteCertificateValidationCallback;
		}
		set
		{
			ThrowIfReadOnly();
			_remoteCertificateValidationCallback = value;
		}
	}

	[UnsupportedOSPlatform("browser")]
	public CookieContainer? Cookies
	{
		get
		{
			return _cookies;
		}
		set
		{
			ThrowIfReadOnly();
			_cookies = value;
		}
	}

	[UnsupportedOSPlatform("browser")]
	public TimeSpan KeepAliveInterval
	{
		get
		{
			return _keepAliveInterval;
		}
		set
		{
			ThrowIfReadOnly();
			if (value != Timeout.InfiniteTimeSpan && value < TimeSpan.Zero)
			{
				throw new ArgumentOutOfRangeException("value", value, System.SR.Format(System.SR.net_WebSockets_ArgumentOutOfRange_TooSmall, Timeout.InfiniteTimeSpan.ToString()));
			}
			_keepAliveInterval = value;
		}
	}

	[UnsupportedOSPlatform("browser")]
	public WebSocketDeflateOptions? DangerousDeflateOptions { get; set; }

	internal ClientWebSocketOptions()
	{
	}

	[UnsupportedOSPlatform("browser")]
	public void SetRequestHeader(string headerName, string? headerValue)
	{
		ThrowIfReadOnly();
		RequestHeaders.Set(headerName, headerValue);
	}

	public void AddSubProtocol(string subProtocol)
	{
		ThrowIfReadOnly();
		System.Net.WebSockets.WebSocketValidate.ValidateSubprotocol(subProtocol);
		List<string> requestedSubProtocols = RequestedSubProtocols;
		foreach (string item in requestedSubProtocols)
		{
			if (string.Equals(item, subProtocol, StringComparison.OrdinalIgnoreCase))
			{
				throw new ArgumentException(System.SR.Format(System.SR.net_WebSockets_NoDuplicateProtocol, subProtocol), "subProtocol");
			}
		}
		requestedSubProtocols.Add(subProtocol);
	}

	[UnsupportedOSPlatform("browser")]
	public void SetBuffer(int receiveBufferSize, int sendBufferSize)
	{
		ThrowIfReadOnly();
		if (receiveBufferSize <= 0)
		{
			throw new ArgumentOutOfRangeException("receiveBufferSize", receiveBufferSize, System.SR.Format(System.SR.net_WebSockets_ArgumentOutOfRange_TooSmall, 1));
		}
		if (sendBufferSize <= 0)
		{
			throw new ArgumentOutOfRangeException("sendBufferSize", sendBufferSize, System.SR.Format(System.SR.net_WebSockets_ArgumentOutOfRange_TooSmall, 1));
		}
		_receiveBufferSize = receiveBufferSize;
		_buffer = null;
	}

	[UnsupportedOSPlatform("browser")]
	public void SetBuffer(int receiveBufferSize, int sendBufferSize, ArraySegment<byte> buffer)
	{
		ThrowIfReadOnly();
		if (receiveBufferSize <= 0)
		{
			throw new ArgumentOutOfRangeException("receiveBufferSize", receiveBufferSize, System.SR.Format(System.SR.net_WebSockets_ArgumentOutOfRange_TooSmall, 1));
		}
		if (sendBufferSize <= 0)
		{
			throw new ArgumentOutOfRangeException("sendBufferSize", sendBufferSize, System.SR.Format(System.SR.net_WebSockets_ArgumentOutOfRange_TooSmall, 1));
		}
		System.Net.WebSockets.WebSocketValidate.ValidateArraySegment(buffer, "buffer");
		if (buffer.Count == 0)
		{
			throw new ArgumentOutOfRangeException("buffer");
		}
		_receiveBufferSize = receiveBufferSize;
		_buffer = buffer;
	}

	internal void SetToReadOnly()
	{
		_isReadOnly = true;
	}

	private void ThrowIfReadOnly()
	{
		if (_isReadOnly)
		{
			throw new InvalidOperationException(System.SR.net_WebSockets_AlreadyStarted);
		}
	}
}
