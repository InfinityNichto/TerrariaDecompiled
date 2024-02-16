using System.Collections.Generic;
using System.Collections.Specialized;
using System.Globalization;
using System.IO;
using System.Net.WebSockets;
using System.Runtime.InteropServices;
using System.Security;
using System.Security.Authentication.ExtendedProtection;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;

namespace System.Net;

public sealed class HttpListenerRequest
{
	private static class Helpers
	{
		private sealed class UrlDecoder
		{
			private readonly int _bufferSize;

			private int _numChars;

			private readonly char[] _charBuffer;

			private int _numBytes;

			private byte[] _byteBuffer;

			private readonly Encoding _encoding;

			private void FlushBytes()
			{
				if (_numBytes > 0)
				{
					_numChars += _encoding.GetChars(_byteBuffer, 0, _numBytes, _charBuffer, _numChars);
					_numBytes = 0;
				}
			}

			internal UrlDecoder(int bufferSize, Encoding encoding)
			{
				_bufferSize = bufferSize;
				_encoding = encoding;
				_charBuffer = new char[bufferSize];
			}

			internal void AddChar(char ch)
			{
				if (_numBytes > 0)
				{
					FlushBytes();
				}
				_charBuffer[_numChars++] = ch;
			}

			internal void AddByte(byte b)
			{
				if (_byteBuffer == null)
				{
					_byteBuffer = new byte[_bufferSize];
				}
				_byteBuffer[_numBytes++] = b;
			}

			internal string GetString()
			{
				if (_numBytes > 0)
				{
					FlushBytes();
				}
				if (_numChars > 0)
				{
					return new string(_charBuffer, 0, _numChars);
				}
				return string.Empty;
			}
		}

		internal static string GetCharSetValueFromHeader(string headerValue)
		{
			if (headerValue == null)
			{
				return null;
			}
			int length = headerValue.Length;
			int length2 = "charset".Length;
			int i;
			for (i = 1; i < length; i += length2)
			{
				i = CultureInfo.InvariantCulture.CompareInfo.IndexOf(headerValue, "charset", i, CompareOptions.IgnoreCase);
				if (i < 0 || i + length2 >= length)
				{
					break;
				}
				char c = headerValue[i - 1];
				char c2 = headerValue[i + length2];
				if ((c == ';' || c == ',' || char.IsWhiteSpace(c)) && (c2 == '=' || char.IsWhiteSpace(c2)))
				{
					break;
				}
			}
			if (i < 0 || i >= length)
			{
				return null;
			}
			for (i += length2; i < length && char.IsWhiteSpace(headerValue[i]); i++)
			{
			}
			if (i >= length || headerValue[i] != '=')
			{
				return null;
			}
			for (i++; i < length && char.IsWhiteSpace(headerValue[i]); i++)
			{
			}
			if (i >= length)
			{
				return null;
			}
			string text = null;
			int num;
			if (i < length && headerValue[i] == '"')
			{
				if (i == length - 1)
				{
					return null;
				}
				num = headerValue.IndexOf('"', i + 1);
				if (num < 0 || num == i + 1)
				{
					return null;
				}
				return headerValue.AsSpan(i + 1, num - i - 1).Trim().ToString();
			}
			for (num = i; num < length && headerValue[num] != ';'; num++)
			{
			}
			if (num == i)
			{
				return null;
			}
			return headerValue.AsSpan(i, num - i).Trim().ToString();
		}

		internal static string[] ParseMultivalueHeader(string s)
		{
			if (s == null)
			{
				return null;
			}
			int length = s.Length;
			List<string> list = new List<string>();
			int num = 0;
			while (num < length)
			{
				int num2 = s.IndexOf(',', num);
				if (num2 < 0)
				{
					num2 = length;
				}
				list.Add(s.Substring(num, num2 - num));
				num = num2 + 1;
				if (num < length && s[num] == ' ')
				{
					num++;
				}
			}
			int count = list.Count;
			string[] array;
			if (count == 0)
			{
				array = new string[1] { string.Empty };
			}
			else
			{
				array = new string[count];
				list.CopyTo(0, array, 0, count);
			}
			return array;
		}

		private static string UrlDecodeStringFromStringInternal(string s, Encoding e)
		{
			int length = s.Length;
			UrlDecoder urlDecoder = new UrlDecoder(length, e);
			for (int i = 0; i < length; i++)
			{
				char c = s[i];
				switch (c)
				{
				case '+':
					c = ' ';
					break;
				case '%':
					if (i >= length - 2)
					{
						break;
					}
					if (s[i + 1] == 'u' && i < length - 5)
					{
						int num = System.HexConverter.FromChar(s[i + 2]);
						int num2 = System.HexConverter.FromChar(s[i + 3]);
						int num3 = System.HexConverter.FromChar(s[i + 4]);
						int num4 = System.HexConverter.FromChar(s[i + 5]);
						if ((num | num2 | num3 | num4) != 255)
						{
							c = (char)((num << 12) | (num2 << 8) | (num3 << 4) | num4);
							i += 5;
							urlDecoder.AddChar(c);
							continue;
						}
					}
					else
					{
						int num5 = System.HexConverter.FromChar(s[i + 1]);
						int num6 = System.HexConverter.FromChar(s[i + 2]);
						if ((num5 | num6) != 255)
						{
							byte b = (byte)((num5 << 4) | num6);
							i += 2;
							urlDecoder.AddByte(b);
							continue;
						}
					}
					break;
				}
				if ((c & 0xFF80) == 0)
				{
					urlDecoder.AddByte((byte)c);
				}
				else
				{
					urlDecoder.AddChar(c);
				}
			}
			return urlDecoder.GetString();
		}

		internal static void FillFromString(NameValueCollection nvc, string s, bool urlencoded, Encoding encoding)
		{
			int length = s.Length;
			for (int i = ((length > 0 && s[0] == '?') ? 1 : 0); i < length; i++)
			{
				int num = i;
				int num2 = -1;
				for (; i < length; i++)
				{
					switch (s[i])
					{
					case '=':
						if (num2 < 0)
						{
							num2 = i;
						}
						continue;
					default:
						continue;
					case '&':
						break;
					}
					break;
				}
				string text = null;
				string text2 = null;
				if (num2 >= 0)
				{
					text = s.Substring(num, num2 - num);
					text2 = s.Substring(num2 + 1, i - num2 - 1);
				}
				else
				{
					text2 = s.Substring(num, i - num);
				}
				if (urlencoded)
				{
					nvc.Add((text == null) ? null : UrlDecodeStringFromStringInternal(text, encoding), UrlDecodeStringFromStringInternal(text2, encoding));
				}
				else
				{
					nvc.Add(text, text2);
				}
				if (i == length - 1 && s[i] == '&')
				{
					nvc.Add(null, "");
				}
			}
		}
	}

	private enum SslStatus : byte
	{
		Insecure,
		NoClientCert,
		ClientCert
	}

	private CookieCollection _cookies;

	private bool? _keepAlive;

	private string _rawUrl;

	private Uri _requestUri;

	private Version _version;

	private readonly ulong _requestId;

	internal ulong _connectionId;

	private readonly SslStatus _sslStatus;

	private readonly string _cookedUrlHost;

	private readonly string _cookedUrlPath;

	private readonly string _cookedUrlQuery;

	private long _contentLength;

	private Stream _requestStream;

	private string _httpMethod;

	private WebHeaderCollection _webHeaders;

	private IPEndPoint _localEndPoint;

	private IPEndPoint _remoteEndPoint;

	private BoundaryType _boundaryType;

	private int _clientCertificateError;

	private RequestContextBase _memoryBlob;

	private readonly HttpListenerContext _httpContext;

	private bool _isDisposed;

	private string _serviceName;

	public string[]? AcceptTypes => Helpers.ParseMultivalueHeader(Headers["Accept"]);

	public string[]? UserLanguages => Helpers.ParseMultivalueHeader(Headers["Accept-Language"]);

	public CookieCollection Cookies
	{
		get
		{
			if (_cookies == null)
			{
				string text = Headers["Cookie"];
				if (!string.IsNullOrEmpty(text))
				{
					_cookies = ParseCookies(RequestUri, text);
				}
				if (_cookies == null)
				{
					_cookies = new CookieCollection();
				}
			}
			return _cookies;
		}
	}

	public Encoding ContentEncoding
	{
		get
		{
			if (UserAgent != null && CultureInfo.InvariantCulture.CompareInfo.IsPrefix(UserAgent, "UP"))
			{
				string text = Headers["x-up-devcap-post-charset"];
				if (text != null && text.Length > 0)
				{
					try
					{
						return Encoding.GetEncoding(text);
					}
					catch (ArgumentException)
					{
					}
				}
			}
			if (HasEntityBody && ContentType != null)
			{
				string charSetValueFromHeader = Helpers.GetCharSetValueFromHeader(ContentType);
				if (charSetValueFromHeader != null)
				{
					try
					{
						return Encoding.GetEncoding(charSetValueFromHeader);
					}
					catch (ArgumentException)
					{
					}
				}
			}
			return Encoding.Default;
		}
	}

	public string? ContentType => Headers["Content-Type"];

	public bool IsLocal => LocalEndPoint.Address.Equals(RemoteEndPoint.Address);

	public bool IsWebSocketRequest
	{
		get
		{
			if (!SupportsWebSockets)
			{
				return false;
			}
			bool flag = false;
			if (string.IsNullOrEmpty(Headers["Connection"]) || string.IsNullOrEmpty(Headers["Upgrade"]))
			{
				return false;
			}
			string[] values = Headers.GetValues("Connection");
			foreach (string a in values)
			{
				if (string.Equals(a, "Upgrade", StringComparison.OrdinalIgnoreCase))
				{
					flag = true;
					break;
				}
			}
			if (!flag)
			{
				return false;
			}
			string[] values2 = Headers.GetValues("Upgrade");
			foreach (string a2 in values2)
			{
				if (string.Equals(a2, "websocket", StringComparison.OrdinalIgnoreCase))
				{
					return true;
				}
			}
			return false;
		}
	}

	public bool KeepAlive
	{
		get
		{
			if (!_keepAlive.HasValue)
			{
				string text = Headers["Proxy-Connection"];
				if (string.IsNullOrEmpty(text))
				{
					text = Headers["Connection"];
				}
				if (string.IsNullOrEmpty(text))
				{
					if (ProtocolVersion >= HttpVersion.Version11)
					{
						_keepAlive = true;
					}
					else
					{
						text = Headers["Keep-Alive"];
						_keepAlive = !string.IsNullOrEmpty(text);
					}
				}
				else
				{
					text = text.ToLowerInvariant();
					_keepAlive = text.IndexOf("close", StringComparison.OrdinalIgnoreCase) < 0 || text.Contains("keep-alive", StringComparison.OrdinalIgnoreCase);
				}
			}
			if (System.Net.NetEventSource.Log.IsEnabled())
			{
				bool? keepAlive = _keepAlive;
				System.Net.NetEventSource.Info(this, "_keepAlive=" + keepAlive, "KeepAlive");
			}
			return _keepAlive.Value;
		}
	}

	public NameValueCollection QueryString
	{
		get
		{
			NameValueCollection nameValueCollection = new NameValueCollection();
			Helpers.FillFromString(nameValueCollection, Url.Query, urlencoded: true, ContentEncoding);
			return nameValueCollection;
		}
	}

	public string? RawUrl => _rawUrl;

	private string RequestScheme
	{
		get
		{
			if (!IsSecureConnection)
			{
				return "http";
			}
			return "https";
		}
	}

	public string UserAgent => Headers["User-Agent"];

	public string UserHostAddress => LocalEndPoint.ToString();

	public string UserHostName => Headers["Host"];

	public Uri? UrlReferrer
	{
		get
		{
			string text = Headers["Referer"];
			if (text == null)
			{
				return null;
			}
			if (!Uri.TryCreate(text, UriKind.RelativeOrAbsolute, out Uri result))
			{
				return null;
			}
			return result;
		}
	}

	public Uri? Url => RequestUri;

	public Version ProtocolVersion => _version;

	internal ListenerClientCertState ClientCertState { get; set; }

	internal X509Certificate2? ClientCertificate { get; set; }

	public int ClientCertificateError
	{
		get
		{
			if (ClientCertState == ListenerClientCertState.NotInitialized)
			{
				throw new InvalidOperationException(System.SR.Format(System.SR.net_listener_mustcall, "GetClientCertificate()/BeginGetClientCertificate()"));
			}
			if (ClientCertState == ListenerClientCertState.InProgress)
			{
				throw new InvalidOperationException(System.SR.Format(System.SR.net_listener_mustcompletecall, "GetClientCertificate()/BeginGetClientCertificate()"));
			}
			return GetClientCertificateErrorCore();
		}
	}

	internal HttpListenerContext HttpListenerContext => _httpContext;

	internal IntPtr RequestBuffer
	{
		get
		{
			CheckDisposed();
			return _memoryBlob.RequestBuffer;
		}
	}

	internal IntPtr OriginalBlobAddress
	{
		get
		{
			CheckDisposed();
			return _memoryBlob.OriginalBlobAddress;
		}
	}

	internal ulong RequestId => _requestId;

	public unsafe Guid RequestTraceIdentifier
	{
		get
		{
			Guid result = default(Guid);
			*(ulong*)(8 + (byte*)(&result)) = RequestId;
			return result;
		}
	}

	public long ContentLength64
	{
		get
		{
			if (_boundaryType == BoundaryType.None)
			{
				string text = Headers["Transfer-Encoding"];
				if (text != null && text.Equals("chunked", StringComparison.OrdinalIgnoreCase))
				{
					_boundaryType = BoundaryType.Chunked;
					_contentLength = -1L;
				}
				else
				{
					_contentLength = 0L;
					_boundaryType = BoundaryType.ContentLength;
					string text2 = Headers["Content-Length"];
					if (text2 != null && !long.TryParse(text2, NumberStyles.None, CultureInfo.InvariantCulture.NumberFormat, out _contentLength))
					{
						_contentLength = 0L;
						_boundaryType = BoundaryType.Invalid;
					}
				}
			}
			if (System.Net.NetEventSource.Log.IsEnabled())
			{
				System.Net.NetEventSource.Info(this, $"_contentLength:{_contentLength} _boundaryType:{_boundaryType}", "ContentLength64");
			}
			return _contentLength;
		}
	}

	public NameValueCollection Headers
	{
		get
		{
			if (_webHeaders == null)
			{
				_webHeaders = global::Interop.HttpApi.GetHeaders(RequestBuffer, OriginalBlobAddress);
			}
			if (System.Net.NetEventSource.Log.IsEnabled())
			{
				System.Net.NetEventSource.Info(this, $"webHeaders:{_webHeaders}", "Headers");
			}
			return _webHeaders;
		}
	}

	public string HttpMethod
	{
		get
		{
			if (_httpMethod == null)
			{
				_httpMethod = global::Interop.HttpApi.GetVerb(RequestBuffer, OriginalBlobAddress);
			}
			if (System.Net.NetEventSource.Log.IsEnabled())
			{
				System.Net.NetEventSource.Info(this, $"_httpMethod:{_httpMethod}", "HttpMethod");
			}
			return _httpMethod;
		}
	}

	public Stream InputStream
	{
		get
		{
			if (_requestStream == null)
			{
				_requestStream = (HasEntityBody ? new HttpRequestStream(HttpListenerContext) : Stream.Null);
			}
			return _requestStream;
		}
	}

	public bool IsAuthenticated
	{
		get
		{
			IPrincipal user = HttpListenerContext.User;
			if (user != null && user.Identity != null)
			{
				return user.Identity.IsAuthenticated;
			}
			return false;
		}
	}

	public bool IsSecureConnection => _sslStatus != SslStatus.Insecure;

	public string? ServiceName
	{
		get
		{
			return _serviceName;
		}
		internal set
		{
			_serviceName = value;
		}
	}

	public TransportContext TransportContext => new HttpListenerRequestContext(this);

	public bool HasEntityBody
	{
		get
		{
			if ((ContentLength64 <= 0 || _boundaryType != 0) && _boundaryType != BoundaryType.Chunked)
			{
				return _boundaryType == BoundaryType.Multipart;
			}
			return true;
		}
	}

	public IPEndPoint RemoteEndPoint
	{
		get
		{
			if (_remoteEndPoint == null)
			{
				_remoteEndPoint = global::Interop.HttpApi.GetRemoteEndPoint(RequestBuffer, OriginalBlobAddress);
			}
			if (System.Net.NetEventSource.Log.IsEnabled())
			{
				System.Net.NetEventSource.Info(this, "_remoteEndPoint" + _remoteEndPoint, "RemoteEndPoint");
			}
			return _remoteEndPoint;
		}
	}

	public IPEndPoint LocalEndPoint
	{
		get
		{
			if (_localEndPoint == null)
			{
				_localEndPoint = global::Interop.HttpApi.GetLocalEndPoint(RequestBuffer, OriginalBlobAddress);
			}
			if (System.Net.NetEventSource.Log.IsEnabled())
			{
				System.Net.NetEventSource.Info(this, $"_localEndPoint={_localEndPoint}", "LocalEndPoint");
			}
			return _localEndPoint;
		}
	}

	private Uri RequestUri
	{
		get
		{
			if (_requestUri == null)
			{
				_requestUri = HttpListenerRequestUriBuilder.GetRequestUri(_rawUrl, RequestScheme, _cookedUrlHost, _cookedUrlPath, _cookedUrlQuery);
			}
			if (System.Net.NetEventSource.Log.IsEnabled())
			{
				System.Net.NetEventSource.Info(this, $"_requestUri:{_requestUri}", "RequestUri");
			}
			return _requestUri;
		}
	}

	private bool SupportsWebSockets => WebSocketProtocolComponent.IsSupported;

	private CookieCollection ParseCookies(Uri uri, string setCookieHeader)
	{
		if (System.Net.NetEventSource.Log.IsEnabled())
		{
			System.Net.NetEventSource.Info(this, "uri:" + uri?.ToString() + " setCookieHeader:" + setCookieHeader, "ParseCookies");
		}
		CookieCollection cookieCollection = new CookieCollection();
		System.Net.CookieParser cookieParser = new System.Net.CookieParser(setCookieHeader);
		while (true)
		{
			Cookie server = cookieParser.GetServer();
			if (server == null)
			{
				break;
			}
			if (System.Net.NetEventSource.Log.IsEnabled())
			{
				System.Net.NetEventSource.Info(this, "CookieParser returned cookie: " + server.ToString(), "ParseCookies");
			}
			if (server.Name.Length != 0)
			{
				cookieCollection.InternalAdd(server, isStrict: true);
			}
		}
		return cookieCollection;
	}

	public X509Certificate2? GetClientCertificate()
	{
		if (ClientCertState == ListenerClientCertState.InProgress)
		{
			throw new InvalidOperationException(System.SR.Format(System.SR.net_listener_callinprogress, "GetClientCertificate()/BeginGetClientCertificate()"));
		}
		ClientCertState = ListenerClientCertState.InProgress;
		GetClientCertificateCore();
		ClientCertState = ListenerClientCertState.Completed;
		if (System.Net.NetEventSource.Log.IsEnabled())
		{
			System.Net.NetEventSource.Info(this, $"_clientCertificate:{ClientCertificate}", "GetClientCertificate");
		}
		return ClientCertificate;
	}

	public IAsyncResult BeginGetClientCertificate(AsyncCallback? requestCallback, object? state)
	{
		if (System.Net.NetEventSource.Log.IsEnabled())
		{
			System.Net.NetEventSource.Info(this, null, "BeginGetClientCertificate");
		}
		if (ClientCertState == ListenerClientCertState.InProgress)
		{
			throw new InvalidOperationException(System.SR.Format(System.SR.net_listener_callinprogress, "GetClientCertificate()/BeginGetClientCertificate()"));
		}
		ClientCertState = ListenerClientCertState.InProgress;
		return BeginGetClientCertificateCore(requestCallback, state);
	}

	public Task<X509Certificate2?> GetClientCertificateAsync()
	{
		return Task.Factory.FromAsync((AsyncCallback callback, object state) => ((HttpListenerRequest)state).BeginGetClientCertificate(callback, state), (IAsyncResult iar) => ((HttpListenerRequest)iar.AsyncState).EndGetClientCertificate(iar), this);
	}

	internal unsafe HttpListenerRequest(HttpListenerContext httpContext, RequestContextBase memoryBlob)
	{
		if (System.Net.NetEventSource.Log.IsEnabled())
		{
			System.Net.NetEventSource.Info(this, $"httpContext:${httpContext} memoryBlob {(IntPtr)memoryBlob.RequestBlob}", ".ctor");
			System.Net.NetEventSource.Associate(this, httpContext, ".ctor");
		}
		_httpContext = httpContext;
		_memoryBlob = memoryBlob;
		_boundaryType = BoundaryType.None;
		_requestId = memoryBlob.RequestBlob->RequestId;
		_connectionId = memoryBlob.RequestBlob->ConnectionId;
		_sslStatus = ((memoryBlob.RequestBlob->pSslInfo != null) ? ((memoryBlob.RequestBlob->pSslInfo->SslClientCertNegotiated == 0) ? SslStatus.NoClientCert : SslStatus.ClientCert) : SslStatus.Insecure);
		if (memoryBlob.RequestBlob->pRawUrl != null && memoryBlob.RequestBlob->RawUrlLength > 0)
		{
			_rawUrl = Marshal.PtrToStringAnsi((IntPtr)memoryBlob.RequestBlob->pRawUrl, memoryBlob.RequestBlob->RawUrlLength);
		}
		global::Interop.HttpApi.HTTP_COOKED_URL cookedUrl = memoryBlob.RequestBlob->CookedUrl;
		if (cookedUrl.pHost != null && cookedUrl.HostLength > 0)
		{
			_cookedUrlHost = Marshal.PtrToStringUni((IntPtr)cookedUrl.pHost, cookedUrl.HostLength / 2);
		}
		if (cookedUrl.pAbsPath != null && cookedUrl.AbsPathLength > 0)
		{
			_cookedUrlPath = Marshal.PtrToStringUni((IntPtr)cookedUrl.pAbsPath, cookedUrl.AbsPathLength / 2);
		}
		if (cookedUrl.pQueryString != null && cookedUrl.QueryStringLength > 0)
		{
			_cookedUrlQuery = Marshal.PtrToStringUni((IntPtr)cookedUrl.pQueryString, cookedUrl.QueryStringLength / 2);
		}
		_version = new Version(memoryBlob.RequestBlob->Version.MajorVersion, memoryBlob.RequestBlob->Version.MinorVersion);
		if (System.Net.NetEventSource.Log.IsEnabled())
		{
			System.Net.NetEventSource.Info(this, $"RequestId:{RequestId} ConnectionId:{_connectionId} RawConnectionId:{memoryBlob.RequestBlob->RawConnectionId} UrlContext:{memoryBlob.RequestBlob->UrlContext} RawUrl:{_rawUrl} Version:{_version} Secure:{_sslStatus}", ".ctor");
			System.Net.NetEventSource.Info(this, $"httpContext:${httpContext} RequestUri:{RequestUri} Content-Length:{ContentLength64} HTTP Method:{HttpMethod}", ".ctor");
		}
		if (System.Net.NetEventSource.Log.IsEnabled())
		{
			StringBuilder stringBuilder = new StringBuilder("HttpListenerRequest Headers:\n");
			for (int i = 0; i < Headers.Count; i++)
			{
				stringBuilder.Append('\t');
				stringBuilder.Append(Headers.GetKey(i));
				stringBuilder.Append(" : ");
				stringBuilder.Append(Headers.Get(i));
				stringBuilder.Append('\n');
			}
			System.Net.NetEventSource.Info(this, stringBuilder.ToString(), ".ctor");
		}
	}

	internal void DetachBlob(RequestContextBase memoryBlob)
	{
		if (memoryBlob != null && memoryBlob == _memoryBlob)
		{
			_memoryBlob = null;
		}
	}

	internal void ReleasePins()
	{
		_memoryBlob.ReleasePins();
	}

	private int GetClientCertificateErrorCore()
	{
		if (System.Net.NetEventSource.Log.IsEnabled())
		{
			System.Net.NetEventSource.Info(this, $"ClientCertificateError:{_clientCertificateError}", "GetClientCertificateErrorCore");
		}
		return _clientCertificateError;
	}

	internal void SetClientCertificateError(int clientCertificateError)
	{
		_clientCertificateError = clientCertificateError;
	}

	public X509Certificate2? EndGetClientCertificate(IAsyncResult asyncResult)
	{
		X509Certificate2 x509Certificate = null;
		if (asyncResult == null)
		{
			throw new ArgumentNullException("asyncResult");
		}
		if (!(asyncResult is ListenerClientCertAsyncResult listenerClientCertAsyncResult) || listenerClientCertAsyncResult.AsyncObject != this)
		{
			throw new ArgumentException(System.SR.net_io_invalidasyncresult, "asyncResult");
		}
		if (listenerClientCertAsyncResult.EndCalled)
		{
			throw new InvalidOperationException(System.SR.Format(System.SR.net_io_invalidendcall, "EndGetClientCertificate"));
		}
		listenerClientCertAsyncResult.EndCalled = true;
		x509Certificate = listenerClientCertAsyncResult.InternalWaitForCompletion() as X509Certificate2;
		if (System.Net.NetEventSource.Log.IsEnabled())
		{
			System.Net.NetEventSource.Info(this, $"_clientCertificate:{ClientCertificate}", "EndGetClientCertificate");
		}
		return x509Certificate;
	}

	internal void Close()
	{
		RequestContextBase memoryBlob = _memoryBlob;
		if (memoryBlob != null)
		{
			memoryBlob.Close();
			_memoryBlob = null;
		}
		_isDisposed = true;
	}

	private unsafe ListenerClientCertAsyncResult BeginGetClientCertificateCore(AsyncCallback requestCallback, object state)
	{
		ListenerClientCertAsyncResult listenerClientCertAsyncResult = null;
		if (_sslStatus != 0)
		{
			uint num = 1500u;
			listenerClientCertAsyncResult = new ListenerClientCertAsyncResult(HttpListenerContext.RequestQueueBoundHandle, this, state, requestCallback, num);
			try
			{
				while (true)
				{
					if (System.Net.NetEventSource.Log.IsEnabled())
					{
						System.Net.NetEventSource.Info(this, "Calling Interop.HttpApi.HttpReceiveClientCertificate size:" + num, "BeginGetClientCertificateCore");
					}
					uint num2 = 0u;
					uint num3 = global::Interop.HttpApi.HttpReceiveClientCertificate(HttpListenerContext.RequestQueueHandle, _connectionId, 0u, listenerClientCertAsyncResult.RequestBlob, num, &num2, listenerClientCertAsyncResult.NativeOverlapped);
					if (System.Net.NetEventSource.Log.IsEnabled())
					{
						System.Net.NetEventSource.Info(this, "Call to Interop.HttpApi.HttpReceiveClientCertificate returned:" + num3 + " bytesReceived:" + num2, "BeginGetClientCertificateCore");
					}
					switch (num3)
					{
					case 234u:
						break;
					default:
						throw new HttpListenerException((int)num3);
					case 0u:
					case 997u:
						if (num3 == 0 && HttpListener.SkipIOCPCallbackOnSuccess)
						{
							listenerClientCertAsyncResult.IOCompleted(num3, num2);
						}
						goto end_IL_0028;
					}
					global::Interop.HttpApi.HTTP_SSL_CLIENT_CERT_INFO* requestBlob = listenerClientCertAsyncResult.RequestBlob;
					num = num2 + requestBlob->CertEncodedSize;
					listenerClientCertAsyncResult.Reset(num);
					continue;
					end_IL_0028:
					break;
				}
			}
			catch
			{
				listenerClientCertAsyncResult?.InternalCleanup();
				throw;
			}
		}
		else
		{
			listenerClientCertAsyncResult = new ListenerClientCertAsyncResult(HttpListenerContext.RequestQueueBoundHandle, this, state, requestCallback, 0u);
			listenerClientCertAsyncResult.InvokeCallback();
		}
		return listenerClientCertAsyncResult;
	}

	private unsafe void GetClientCertificateCore()
	{
		if (System.Net.NetEventSource.Log.IsEnabled())
		{
			System.Net.NetEventSource.Info(this, null, "GetClientCertificateCore");
		}
		if (_sslStatus == SslStatus.Insecure)
		{
			return;
		}
		uint num = 1500u;
		while (true)
		{
			byte[] array = new byte[checked((int)num)];
			fixed (byte* ptr = &array[0])
			{
				global::Interop.HttpApi.HTTP_SSL_CLIENT_CERT_INFO* ptr2 = (global::Interop.HttpApi.HTTP_SSL_CLIENT_CERT_INFO*)ptr;
				if (System.Net.NetEventSource.Log.IsEnabled())
				{
					System.Net.NetEventSource.Info(this, "Calling Interop.HttpApi.HttpReceiveClientCertificate size:" + num, "GetClientCertificateCore");
				}
				uint num2 = 0u;
				uint num3 = global::Interop.HttpApi.HttpReceiveClientCertificate(HttpListenerContext.RequestQueueHandle, _connectionId, 0u, ptr2, num, &num2, null);
				if (System.Net.NetEventSource.Log.IsEnabled())
				{
					System.Net.NetEventSource.Info(this, "Call to Interop.HttpApi.HttpReceiveClientCertificate returned:" + num3 + " bytesReceived:" + num2, "GetClientCertificateCore");
				}
				switch (num3)
				{
				case 234u:
					num = num2 + ptr2->CertEncodedSize;
					break;
				case 0u:
					if (ptr2 == null)
					{
						return;
					}
					if (System.Net.NetEventSource.Log.IsEnabled())
					{
						System.Net.NetEventSource.Info(this, $"pClientCertInfo:{(IntPtr)ptr2} pClientCertInfo->CertFlags: {ptr2->CertFlags} pClientCertInfo->CertEncodedSize: {ptr2->CertEncodedSize} pClientCertInfo->pCertEncoded: {(IntPtr)ptr2->pCertEncoded} pClientCertInfo->Token: {(IntPtr)ptr2->Token} pClientCertInfo->CertDeniedByMapper: {ptr2->CertDeniedByMapper}", "GetClientCertificateCore");
					}
					if (ptr2->pCertEncoded != null)
					{
						try
						{
							byte[] array2 = new byte[ptr2->CertEncodedSize];
							Marshal.Copy((IntPtr)ptr2->pCertEncoded, array2, 0, array2.Length);
							ClientCertificate = new X509Certificate2(array2);
						}
						catch (CryptographicException ex)
						{
							if (System.Net.NetEventSource.Log.IsEnabled())
							{
								System.Net.NetEventSource.Info(this, $"CryptographicException={ex}", "GetClientCertificateCore");
							}
						}
						catch (SecurityException ex2)
						{
							if (System.Net.NetEventSource.Log.IsEnabled())
							{
								System.Net.NetEventSource.Info(this, $"SecurityException={ex2}", "GetClientCertificateCore");
							}
						}
					}
					_clientCertificateError = (int)ptr2->CertFlags;
					return;
				default:
					return;
				}
			}
		}
	}

	internal ChannelBinding GetChannelBinding()
	{
		return HttpListener.GetChannelBindingFromTls(HttpListenerContext.ListenerSession, _connectionId);
	}

	internal void CheckDisposed()
	{
		if (_isDisposed)
		{
			throw new ObjectDisposedException(GetType().FullName);
		}
	}
}
