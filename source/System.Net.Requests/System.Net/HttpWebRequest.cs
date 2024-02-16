using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Net.Cache;
using System.Net.Http;
using System.Net.Security;
using System.Net.Sockets;
using System.Runtime.Serialization;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;

namespace System.Net;

public class HttpWebRequest : WebRequest, ISerializable
{
	[Flags]
	private enum Booleans : uint
	{
		AllowAutoRedirect = 1u,
		AllowWriteStreamBuffering = 2u,
		ExpectContinue = 4u,
		ProxySet = 0x10u,
		UnsafeAuthenticatedConnectionSharing = 0x40u,
		IsVersionHttp10 = 0x80u,
		SendChunked = 0x100u,
		EnableDecompression = 0x200u,
		IsTunnelRequest = 0x400u,
		IsWebSocketRequest = 0x800u,
		Default = 7u
	}

	private sealed class HttpClientParameters
	{
		public readonly bool Async;

		public readonly DecompressionMethods AutomaticDecompression;

		public readonly bool AllowAutoRedirect;

		public readonly int MaximumAutomaticRedirections;

		public readonly int MaximumResponseHeadersLength;

		public readonly bool PreAuthenticate;

		public readonly int ReadWriteTimeout;

		public readonly TimeSpan Timeout;

		public readonly SecurityProtocolType SslProtocols;

		public readonly bool CheckCertificateRevocationList;

		public readonly ICredentials Credentials;

		public readonly IWebProxy Proxy;

		public readonly RemoteCertificateValidationCallback ServerCertificateValidationCallback;

		public readonly X509CertificateCollection ClientCertificates;

		public readonly CookieContainer CookieContainer;

		public HttpClientParameters(HttpWebRequest webRequest, bool async)
		{
			Async = async;
			AutomaticDecompression = webRequest.AutomaticDecompression;
			AllowAutoRedirect = webRequest.AllowAutoRedirect;
			MaximumAutomaticRedirections = webRequest.MaximumAutomaticRedirections;
			MaximumResponseHeadersLength = webRequest.MaximumResponseHeadersLength;
			PreAuthenticate = webRequest.PreAuthenticate;
			ReadWriteTimeout = webRequest.ReadWriteTimeout;
			Timeout = ((webRequest.Timeout == -1) ? System.Threading.Timeout.InfiniteTimeSpan : TimeSpan.FromMilliseconds(webRequest.Timeout));
			SslProtocols = ServicePointManager.SecurityProtocol;
			CheckCertificateRevocationList = ServicePointManager.CheckCertificateRevocationList;
			Credentials = webRequest._credentials;
			Proxy = webRequest._proxy;
			ServerCertificateValidationCallback = webRequest.ServerCertificateValidationCallback ?? ServicePointManager.ServerCertificateValidationCallback;
			ClientCertificates = webRequest._clientCertificates;
			CookieContainer = webRequest._cookieContainer;
		}

		public bool Matches(HttpClientParameters requestParameters)
		{
			if (Async == requestParameters.Async && AutomaticDecompression == requestParameters.AutomaticDecompression && AllowAutoRedirect == requestParameters.AllowAutoRedirect && MaximumAutomaticRedirections == requestParameters.MaximumAutomaticRedirections && MaximumResponseHeadersLength == requestParameters.MaximumResponseHeadersLength && PreAuthenticate == requestParameters.PreAuthenticate && ReadWriteTimeout == requestParameters.ReadWriteTimeout && Timeout == requestParameters.Timeout && SslProtocols == requestParameters.SslProtocols && CheckCertificateRevocationList == requestParameters.CheckCertificateRevocationList && Credentials == requestParameters.Credentials && Proxy == requestParameters.Proxy && (object)ServerCertificateValidationCallback == requestParameters.ServerCertificateValidationCallback && ClientCertificates == requestParameters.ClientCertificates)
			{
				return CookieContainer == requestParameters.CookieContainer;
			}
			return false;
		}

		public bool AreParametersAcceptableForCaching()
		{
			if (Credentials == null && Proxy == WebRequest.DefaultWebProxy && ServerCertificateValidationCallback == null && ClientCertificates == null)
			{
				return CookieContainer == null;
			}
			return false;
		}
	}

	private WebHeaderCollection _webHeaderCollection = new WebHeaderCollection();

	private readonly Uri _requestUri;

	private string _originVerb = HttpMethod.Get.Method;

	private int _continueTimeout = 350;

	private bool _allowReadStreamBuffering;

	private CookieContainer _cookieContainer;

	private ICredentials _credentials;

	private IWebProxy _proxy = WebRequest.DefaultWebProxy;

	private Task<HttpResponseMessage> _sendRequestTask;

	private static int _defaultMaxResponseHeadersLength = 64;

	private int _beginGetRequestStreamCalled;

	private int _beginGetResponseCalled;

	private int _endGetRequestStreamCalled;

	private int _endGetResponseCalled;

	private int _maximumAllowedRedirections = 50;

	private int _maximumResponseHeadersLen = _defaultMaxResponseHeadersLength;

	private ServicePoint _servicePoint;

	private int _timeout = 100000;

	private int _readWriteTimeout = 300000;

	private HttpContinueDelegate _continueDelegate;

	private bool _hostHasPort;

	private Uri _hostUri;

	private RequestStream _requestStream;

	private TaskCompletionSource<Stream> _requestStreamOperation;

	private TaskCompletionSource<WebResponse> _responseOperation;

	private AsyncCallback _requestStreamCallback;

	private AsyncCallback _responseCallback;

	private int _abortCalled;

	private CancellationTokenSource _sendRequestCts;

	private X509CertificateCollection _clientCertificates;

	private Booleans _booleans = Booleans.Default;

	private bool _pipelined = true;

	private bool _preAuthenticate;

	private DecompressionMethods _automaticDecompression;

	private static readonly object s_syncRoot = new object();

	private static volatile HttpClient s_cachedHttpClient;

	private static HttpClientParameters s_cachedHttpClientParameters;

	private static readonly string[] s_wellKnownContentHeaders = new string[10] { "Content-Disposition", "Content-Encoding", "Content-Language", "Content-Length", "Content-Location", "Content-MD5", "Content-Range", "Content-Type", "Expires", "Last-Modified" };

	public string? Accept
	{
		get
		{
			return _webHeaderCollection["Accept"];
		}
		set
		{
			SetSpecialHeaders("Accept", value);
		}
	}

	public virtual bool AllowReadStreamBuffering
	{
		get
		{
			return _allowReadStreamBuffering;
		}
		set
		{
			_allowReadStreamBuffering = value;
		}
	}

	public int MaximumResponseHeadersLength
	{
		get
		{
			return _maximumResponseHeadersLen;
		}
		set
		{
			if (RequestSubmitted)
			{
				throw new InvalidOperationException(System.SR.net_reqsubmitted);
			}
			if (value < 0 && value != -1)
			{
				throw new ArgumentOutOfRangeException("value", System.SR.net_toosmall);
			}
			_maximumResponseHeadersLen = value;
		}
	}

	public int MaximumAutomaticRedirections
	{
		get
		{
			return _maximumAllowedRedirections;
		}
		set
		{
			if (value <= 0)
			{
				throw new ArgumentException(System.SR.net_toosmall, "value");
			}
			_maximumAllowedRedirections = value;
		}
	}

	public override string? ContentType
	{
		get
		{
			return _webHeaderCollection["Content-Type"];
		}
		set
		{
			SetSpecialHeaders("Content-Type", value);
		}
	}

	public int ContinueTimeout
	{
		get
		{
			return _continueTimeout;
		}
		set
		{
			if (RequestSubmitted)
			{
				throw new InvalidOperationException(System.SR.net_reqsubmitted);
			}
			if (value < 0 && value != -1)
			{
				throw new ArgumentOutOfRangeException("value", System.SR.net_io_timeout_use_ge_zero);
			}
			_continueTimeout = value;
		}
	}

	public override int Timeout
	{
		get
		{
			return _timeout;
		}
		set
		{
			if (value < 0 && value != -1)
			{
				throw new ArgumentOutOfRangeException("value", System.SR.net_io_timeout_use_ge_zero);
			}
			_timeout = value;
		}
	}

	public override long ContentLength
	{
		get
		{
			long.TryParse(_webHeaderCollection["Content-Length"], out var result);
			return result;
		}
		set
		{
			if (RequestSubmitted)
			{
				throw new InvalidOperationException(System.SR.net_writestarted);
			}
			if (value < 0)
			{
				throw new ArgumentOutOfRangeException("value", System.SR.net_clsmall);
			}
			SetSpecialHeaders("Content-Length", value.ToString());
		}
	}

	public Uri Address => _requestUri;

	public string? UserAgent
	{
		get
		{
			return _webHeaderCollection["User-Agent"];
		}
		set
		{
			SetSpecialHeaders("User-Agent", value);
		}
	}

	public string Host
	{
		get
		{
			Uri uri = _hostUri ?? Address;
			if ((!(_hostUri == null) && _hostHasPort) || !Address.IsDefaultPort)
			{
				return uri.Host + ":" + uri.Port;
			}
			return uri.Host;
		}
		set
		{
			if (RequestSubmitted)
			{
				throw new InvalidOperationException(System.SR.net_writestarted);
			}
			if (value == null)
			{
				throw new ArgumentNullException("value");
			}
			if (value.Contains('/') || !TryGetHostUri(value, out var hostUri))
			{
				throw new ArgumentException(System.SR.net_invalid_host, "value");
			}
			_hostUri = hostUri;
			if (!_hostUri.IsDefaultPort)
			{
				_hostHasPort = true;
				return;
			}
			if (!value.Contains(':'))
			{
				_hostHasPort = false;
				return;
			}
			int num = value.IndexOf(']');
			_hostHasPort = num == -1 || value.LastIndexOf(':') > num;
		}
	}

	public bool Pipelined
	{
		get
		{
			return _pipelined;
		}
		set
		{
			_pipelined = value;
		}
	}

	public string? Referer
	{
		get
		{
			return _webHeaderCollection["Referer"];
		}
		set
		{
			SetSpecialHeaders("Referer", value);
		}
	}

	public string? MediaType { get; set; }

	public string? TransferEncoding
	{
		get
		{
			return _webHeaderCollection["Transfer-Encoding"];
		}
		set
		{
			if (string.IsNullOrWhiteSpace(value))
			{
				_webHeaderCollection.Remove("Transfer-Encoding");
				return;
			}
			if (value.IndexOf("chunked", StringComparison.OrdinalIgnoreCase) != -1)
			{
				throw new ArgumentException(System.SR.net_nochunked, "value");
			}
			if (!SendChunked)
			{
				throw new InvalidOperationException(System.SR.net_needchunked);
			}
			string value2 = System.Net.HttpValidationHelpers.CheckBadHeaderValueChars(value);
			_webHeaderCollection["Transfer-Encoding"] = value2;
		}
	}

	public bool KeepAlive { get; set; } = true;


	public bool UnsafeAuthenticatedConnectionSharing
	{
		get
		{
			return (_booleans & Booleans.UnsafeAuthenticatedConnectionSharing) != 0;
		}
		set
		{
			if (value)
			{
				_booleans |= Booleans.UnsafeAuthenticatedConnectionSharing;
			}
			else
			{
				_booleans &= ~Booleans.UnsafeAuthenticatedConnectionSharing;
			}
		}
	}

	public DecompressionMethods AutomaticDecompression
	{
		get
		{
			return _automaticDecompression;
		}
		set
		{
			if (RequestSubmitted)
			{
				throw new InvalidOperationException(System.SR.net_writestarted);
			}
			_automaticDecompression = value;
		}
	}

	public virtual bool AllowWriteStreamBuffering
	{
		get
		{
			return (_booleans & Booleans.AllowWriteStreamBuffering) != 0;
		}
		set
		{
			if (value)
			{
				_booleans |= Booleans.AllowWriteStreamBuffering;
			}
			else
			{
				_booleans &= ~Booleans.AllowWriteStreamBuffering;
			}
		}
	}

	public virtual bool AllowAutoRedirect
	{
		get
		{
			return (_booleans & Booleans.AllowAutoRedirect) != 0;
		}
		set
		{
			if (value)
			{
				_booleans |= Booleans.AllowAutoRedirect;
			}
			else
			{
				_booleans &= ~Booleans.AllowAutoRedirect;
			}
		}
	}

	public override string? ConnectionGroupName { get; set; }

	public override bool PreAuthenticate
	{
		get
		{
			return _preAuthenticate;
		}
		set
		{
			_preAuthenticate = value;
		}
	}

	public string? Connection
	{
		get
		{
			return _webHeaderCollection["Connection"];
		}
		set
		{
			if (string.IsNullOrWhiteSpace(value))
			{
				_webHeaderCollection.Remove("Connection");
				return;
			}
			bool flag = value.IndexOf("keep-alive", StringComparison.OrdinalIgnoreCase) != -1;
			bool flag2 = value.IndexOf("close", StringComparison.OrdinalIgnoreCase) != -1;
			if (flag || flag2)
			{
				throw new ArgumentException(System.SR.net_connarg, "value");
			}
			string value2 = System.Net.HttpValidationHelpers.CheckBadHeaderValueChars(value);
			_webHeaderCollection["Connection"] = value2;
		}
	}

	public string? Expect
	{
		get
		{
			return _webHeaderCollection["Expect"];
		}
		set
		{
			if (string.IsNullOrWhiteSpace(value))
			{
				_webHeaderCollection.Remove("Expect");
				return;
			}
			if (value.IndexOf("100-continue", StringComparison.OrdinalIgnoreCase) != -1)
			{
				throw new ArgumentException(System.SR.net_no100, "value");
			}
			string value2 = System.Net.HttpValidationHelpers.CheckBadHeaderValueChars(value);
			_webHeaderCollection["Expect"] = value2;
		}
	}

	public static int DefaultMaximumResponseHeadersLength
	{
		get
		{
			return _defaultMaxResponseHeadersLength;
		}
		set
		{
			_defaultMaxResponseHeadersLength = value;
		}
	}

	public static int DefaultMaximumErrorResponseLength { get; set; }

	public new static RequestCachePolicy? DefaultCachePolicy { get; set; } = new RequestCachePolicy(RequestCacheLevel.BypassCache);


	public DateTime IfModifiedSince
	{
		get
		{
			return GetDateHeaderHelper("If-Modified-Since");
		}
		set
		{
			SetDateHeaderHelper("If-Modified-Since", value);
		}
	}

	public DateTime Date
	{
		get
		{
			return GetDateHeaderHelper("Date");
		}
		set
		{
			SetDateHeaderHelper("Date", value);
		}
	}

	public bool SendChunked
	{
		get
		{
			return (_booleans & Booleans.SendChunked) != 0;
		}
		set
		{
			if (RequestSubmitted)
			{
				throw new InvalidOperationException(System.SR.net_writestarted);
			}
			if (value)
			{
				_booleans |= Booleans.SendChunked;
			}
			else
			{
				_booleans &= ~Booleans.SendChunked;
			}
		}
	}

	public HttpContinueDelegate? ContinueDelegate
	{
		get
		{
			return _continueDelegate;
		}
		set
		{
			_continueDelegate = value;
		}
	}

	public ServicePoint ServicePoint => _servicePoint ?? (_servicePoint = ServicePointManager.FindServicePoint(Address, Proxy));

	public RemoteCertificateValidationCallback? ServerCertificateValidationCallback { get; set; }

	public X509CertificateCollection ClientCertificates
	{
		get
		{
			return _clientCertificates ?? (_clientCertificates = new X509CertificateCollection());
		}
		set
		{
			_clientCertificates = value ?? throw new ArgumentNullException("value");
		}
	}

	public Version ProtocolVersion
	{
		get
		{
			if (!IsVersionHttp10)
			{
				return HttpVersion.Version11;
			}
			return HttpVersion.Version10;
		}
		set
		{
			if (value.Equals(HttpVersion.Version11))
			{
				IsVersionHttp10 = false;
				return;
			}
			if (value.Equals(HttpVersion.Version10))
			{
				IsVersionHttp10 = true;
				return;
			}
			throw new ArgumentException(System.SR.net_wrongversion, "value");
		}
	}

	public int ReadWriteTimeout
	{
		get
		{
			return _readWriteTimeout;
		}
		set
		{
			if (RequestSubmitted)
			{
				throw new InvalidOperationException(System.SR.net_reqsubmitted);
			}
			if (value <= 0 && value != -1)
			{
				throw new ArgumentOutOfRangeException("value", System.SR.net_io_timeout_use_gt_zero);
			}
			_readWriteTimeout = value;
		}
	}

	public virtual CookieContainer? CookieContainer
	{
		get
		{
			return _cookieContainer;
		}
		set
		{
			_cookieContainer = value;
		}
	}

	public override ICredentials? Credentials
	{
		get
		{
			return _credentials;
		}
		set
		{
			_credentials = value;
		}
	}

	public virtual bool HaveResponse
	{
		get
		{
			if (_sendRequestTask != null)
			{
				return _sendRequestTask.IsCompletedSuccessfully;
			}
			return false;
		}
	}

	public override WebHeaderCollection Headers
	{
		get
		{
			return _webHeaderCollection;
		}
		set
		{
			if (RequestSubmitted)
			{
				throw new InvalidOperationException(System.SR.net_reqsubmitted);
			}
			WebHeaderCollection webHeaderCollection = new WebHeaderCollection();
			string[] allKeys = value.AllKeys;
			foreach (string name in allKeys)
			{
				webHeaderCollection[name] = value[name];
			}
			_webHeaderCollection = webHeaderCollection;
		}
	}

	public override string Method
	{
		get
		{
			return _originVerb;
		}
		set
		{
			if (string.IsNullOrEmpty(value))
			{
				throw new ArgumentException(System.SR.net_badmethod, "value");
			}
			if (System.Net.HttpValidationHelpers.IsInvalidMethodOrHeaderString(value))
			{
				throw new ArgumentException(System.SR.net_badmethod, "value");
			}
			_originVerb = value;
		}
	}

	public override Uri RequestUri => _requestUri;

	public virtual bool SupportsCookieContainer => true;

	public override bool UseDefaultCredentials
	{
		get
		{
			return _credentials == CredentialCache.DefaultCredentials;
		}
		set
		{
			if (RequestSubmitted)
			{
				throw new InvalidOperationException(System.SR.net_writestarted);
			}
			_credentials = (value ? CredentialCache.DefaultCredentials : null);
		}
	}

	public override IWebProxy? Proxy
	{
		get
		{
			return _proxy;
		}
		set
		{
			if (RequestSubmitted)
			{
				throw new InvalidOperationException(System.SR.net_reqsubmitted);
			}
			_proxy = value;
		}
	}

	private bool IsVersionHttp10
	{
		get
		{
			return (_booleans & Booleans.IsVersionHttp10) != 0;
		}
		set
		{
			if (value)
			{
				_booleans |= Booleans.IsVersionHttp10;
			}
			else
			{
				_booleans &= ~Booleans.IsVersionHttp10;
			}
		}
	}

	private bool RequestSubmitted => _sendRequestTask != null;

	[Obsolete("WebRequest, HttpWebRequest, ServicePoint, and WebClient are obsolete. Use HttpClient instead.", DiagnosticId = "SYSLIB0014", UrlFormat = "https://aka.ms/dotnet-warnings/{0}")]
	protected HttpWebRequest(SerializationInfo serializationInfo, StreamingContext streamingContext)
		: base(serializationInfo, streamingContext)
	{
		throw new PlatformNotSupportedException();
	}

	void ISerializable.GetObjectData(SerializationInfo serializationInfo, StreamingContext streamingContext)
	{
		throw new PlatformNotSupportedException();
	}

	protected override void GetObjectData(SerializationInfo serializationInfo, StreamingContext streamingContext)
	{
		throw new PlatformNotSupportedException();
	}

	internal HttpWebRequest(Uri uri)
	{
		_requestUri = uri;
	}

	private void SetSpecialHeaders(string HeaderName, string value)
	{
		_webHeaderCollection.Remove(HeaderName);
		if (!string.IsNullOrEmpty(value))
		{
			_webHeaderCollection[HeaderName] = value;
		}
	}

	public override void Abort()
	{
		if (Interlocked.Exchange(ref _abortCalled, 1) != 0)
		{
			return;
		}
		if (_responseOperation != null)
		{
			if (_responseOperation.TrySetCanceled() && _responseCallback != null)
			{
				_responseCallback(_responseOperation.Task);
			}
			_sendRequestCts.Cancel();
		}
		else if (_requestStreamOperation != null && _requestStreamOperation.TrySetCanceled() && _requestStreamCallback != null)
		{
			_requestStreamCallback(_requestStreamOperation.Task);
		}
	}

	public override WebResponse GetResponse()
	{
		try
		{
			_sendRequestCts = new CancellationTokenSource();
			return SendRequest(async: false).GetAwaiter().GetResult();
		}
		catch (Exception exception)
		{
			throw WebException.CreateCompatibleException(exception);
		}
	}

	public override Stream GetRequestStream()
	{
		return InternalGetRequestStream().Result;
	}

	private Task<Stream> InternalGetRequestStream()
	{
		CheckAbort();
		if (string.Equals(HttpMethod.Get.Method, _originVerb, StringComparison.OrdinalIgnoreCase) || string.Equals(HttpMethod.Head.Method, _originVerb, StringComparison.OrdinalIgnoreCase) || string.Equals("CONNECT", _originVerb, StringComparison.OrdinalIgnoreCase))
		{
			throw new ProtocolViolationException(System.SR.net_nouploadonget);
		}
		if (RequestSubmitted)
		{
			throw new InvalidOperationException(System.SR.net_reqsubmitted);
		}
		_requestStream = new RequestStream();
		return Task.FromResult((Stream)_requestStream);
	}

	public Stream EndGetRequestStream(IAsyncResult asyncResult, out TransportContext? context)
	{
		context = null;
		return EndGetRequestStream(asyncResult);
	}

	public Stream GetRequestStream(out TransportContext? context)
	{
		context = null;
		return GetRequestStream();
	}

	public override IAsyncResult BeginGetRequestStream(AsyncCallback? callback, object? state)
	{
		CheckAbort();
		if (Interlocked.Exchange(ref _beginGetRequestStreamCalled, 1) != 0)
		{
			throw new InvalidOperationException(System.SR.net_repcall);
		}
		_requestStreamCallback = callback;
		_requestStreamOperation = InternalGetRequestStream().ToApm(callback, state);
		return _requestStreamOperation.Task;
	}

	public override Stream EndGetRequestStream(IAsyncResult asyncResult)
	{
		CheckAbort();
		if (asyncResult == null || !(asyncResult is Task<Stream>))
		{
			throw new ArgumentException(System.SR.net_io_invalidasyncresult, "asyncResult");
		}
		if (Interlocked.Exchange(ref _endGetRequestStreamCalled, 1) != 0)
		{
			throw new InvalidOperationException(System.SR.Format(System.SR.net_io_invalidendcall, "EndGetRequestStream"));
		}
		try
		{
			return ((Task<Stream>)asyncResult).GetAwaiter().GetResult();
		}
		catch (Exception exception)
		{
			throw WebException.CreateCompatibleException(exception);
		}
	}

	private async Task<WebResponse> SendRequest(bool async)
	{
		if (RequestSubmitted)
		{
			throw new InvalidOperationException(System.SR.net_reqsubmitted);
		}
		HttpRequestMessage httpRequestMessage = new HttpRequestMessage(new HttpMethod(_originVerb), _requestUri);
		bool disposeRequired = false;
		HttpClient client = null;
		try
		{
			client = GetCachedOrCreateHttpClient(async, out disposeRequired);
			if (_requestStream != null)
			{
				ArraySegment<byte> buffer = _requestStream.GetBuffer();
				httpRequestMessage.Content = new ByteArrayContent(buffer.Array, buffer.Offset, buffer.Count);
			}
			if (_hostUri != null)
			{
				httpRequestMessage.Headers.Host = Host;
			}
			foreach (string item in _webHeaderCollection)
			{
				if (IsWellKnownContentHeader(item))
				{
					if (httpRequestMessage.Content == null)
					{
						httpRequestMessage.Content = new ByteArrayContent(Array.Empty<byte>());
					}
					httpRequestMessage.Content.Headers.TryAddWithoutValidation(item, _webHeaderCollection[item]);
				}
				else
				{
					httpRequestMessage.Headers.TryAddWithoutValidation(item, _webHeaderCollection[item]);
				}
			}
			httpRequestMessage.Headers.TransferEncodingChunked = SendChunked;
			if (KeepAlive)
			{
				httpRequestMessage.Headers.Connection.Add("Keep-Alive");
			}
			else
			{
				httpRequestMessage.Headers.ConnectionClose = true;
			}
			httpRequestMessage.Version = ProtocolVersion;
			_sendRequestTask = (Task<HttpResponseMessage>)(async ? ((Task)client.SendAsync(httpRequestMessage, (!_allowReadStreamBuffering) ? HttpCompletionOption.ResponseHeadersRead : HttpCompletionOption.ResponseContentRead, _sendRequestCts.Token)) : ((Task)Task.FromResult(client.Send(httpRequestMessage, (!_allowReadStreamBuffering) ? HttpCompletionOption.ResponseHeadersRead : HttpCompletionOption.ResponseContentRead, _sendRequestCts.Token))));
			HttpWebResponse httpWebResponse = new HttpWebResponse(await _sendRequestTask.ConfigureAwait(continueOnCapturedContext: false), _requestUri, _cookieContainer);
			int num = (AllowAutoRedirect ? 299 : 399);
			if ((int)httpWebResponse.StatusCode > num || httpWebResponse.StatusCode < HttpStatusCode.OK)
			{
				throw new WebException(System.SR.Format(System.SR.net_servererror, (int)httpWebResponse.StatusCode, httpWebResponse.StatusDescription), null, WebExceptionStatus.ProtocolError, httpWebResponse);
			}
			return httpWebResponse;
		}
		finally
		{
			if (disposeRequired)
			{
				client?.Dispose();
			}
		}
	}

	public override IAsyncResult BeginGetResponse(AsyncCallback? callback, object? state)
	{
		CheckAbort();
		if (Interlocked.Exchange(ref _beginGetResponseCalled, 1) != 0)
		{
			throw new InvalidOperationException(System.SR.net_repcall);
		}
		_sendRequestCts = new CancellationTokenSource();
		_responseCallback = callback;
		_responseOperation = SendRequest(async: true).ToApm(callback, state);
		return _responseOperation.Task;
	}

	public override WebResponse EndGetResponse(IAsyncResult asyncResult)
	{
		CheckAbort();
		if (asyncResult == null || !(asyncResult is Task<WebResponse>))
		{
			throw new ArgumentException(System.SR.net_io_invalidasyncresult, "asyncResult");
		}
		if (Interlocked.Exchange(ref _endGetResponseCalled, 1) != 0)
		{
			throw new InvalidOperationException(System.SR.Format(System.SR.net_io_invalidendcall, "EndGetResponse"));
		}
		try
		{
			return ((Task<WebResponse>)asyncResult).GetAwaiter().GetResult();
		}
		catch (Exception exception)
		{
			throw WebException.CreateCompatibleException(exception);
		}
	}

	public void AddRange(int from, int to)
	{
		AddRange("bytes", (long)from, (long)to);
	}

	public void AddRange(long from, long to)
	{
		AddRange("bytes", from, to);
	}

	public void AddRange(int range)
	{
		AddRange("bytes", (long)range);
	}

	public void AddRange(long range)
	{
		AddRange("bytes", range);
	}

	public void AddRange(string rangeSpecifier, int from, int to)
	{
		AddRange(rangeSpecifier, (long)from, (long)to);
	}

	public void AddRange(string rangeSpecifier, long from, long to)
	{
		if (rangeSpecifier == null)
		{
			throw new ArgumentNullException("rangeSpecifier");
		}
		if (from < 0 || to < 0)
		{
			throw new ArgumentOutOfRangeException((from < 0) ? "from" : "to", System.SR.net_rangetoosmall);
		}
		if (from > to)
		{
			throw new ArgumentOutOfRangeException("from", System.SR.net_fromto);
		}
		if (!System.Net.HttpValidationHelpers.IsValidToken(rangeSpecifier))
		{
			throw new ArgumentException(System.SR.net_nottoken, "rangeSpecifier");
		}
		if (!AddRange(rangeSpecifier, from.ToString(NumberFormatInfo.InvariantInfo), to.ToString(NumberFormatInfo.InvariantInfo)))
		{
			throw new InvalidOperationException(System.SR.net_rangetype);
		}
	}

	public void AddRange(string rangeSpecifier, int range)
	{
		AddRange(rangeSpecifier, (long)range);
	}

	public void AddRange(string rangeSpecifier, long range)
	{
		if (rangeSpecifier == null)
		{
			throw new ArgumentNullException("rangeSpecifier");
		}
		if (!System.Net.HttpValidationHelpers.IsValidToken(rangeSpecifier))
		{
			throw new ArgumentException(System.SR.net_nottoken, "rangeSpecifier");
		}
		if (!AddRange(rangeSpecifier, range.ToString(NumberFormatInfo.InvariantInfo), (range >= 0) ? "" : null))
		{
			throw new InvalidOperationException(System.SR.net_rangetype);
		}
	}

	private bool AddRange(string rangeSpecifier, string from, string to)
	{
		string text = _webHeaderCollection["Range"];
		if (text == null || text.Length == 0)
		{
			text = rangeSpecifier + "=";
		}
		else
		{
			if (!string.Equals(text.Substring(0, text.IndexOf('=')), rangeSpecifier, StringComparison.OrdinalIgnoreCase))
			{
				return false;
			}
			text = string.Empty;
		}
		text += from.ToString();
		if (to != null)
		{
			text = text + "-" + to;
		}
		_webHeaderCollection["Range"] = text;
		return true;
	}

	private void CheckAbort()
	{
		if (Volatile.Read(ref _abortCalled) == 1)
		{
			throw new WebException(System.SR.net_reqaborted, WebExceptionStatus.RequestCanceled);
		}
	}

	private bool IsWellKnownContentHeader(string header)
	{
		string[] array = s_wellKnownContentHeaders;
		foreach (string b in array)
		{
			if (string.Equals(header, b, StringComparison.OrdinalIgnoreCase))
			{
				return true;
			}
		}
		return false;
	}

	private DateTime GetDateHeaderHelper(string headerName)
	{
		string text = _webHeaderCollection[headerName];
		if (text == null)
		{
			return DateTime.MinValue;
		}
		if (System.Net.HttpDateParser.TryParse(text, out var result))
		{
			return result.LocalDateTime;
		}
		throw new ProtocolViolationException(System.SR.net_baddate);
	}

	private void SetDateHeaderHelper(string headerName, DateTime dateTime)
	{
		if (dateTime == DateTime.MinValue)
		{
			SetSpecialHeaders(headerName, null);
		}
		else
		{
			SetSpecialHeaders(headerName, System.Net.HttpDateParser.DateToString(dateTime.ToUniversalTime()));
		}
	}

	private bool TryGetHostUri(string hostName, [NotNullWhen(true)] out Uri hostUri)
	{
		string uriString = Address.Scheme + "://" + hostName + Address.PathAndQuery;
		return Uri.TryCreate(uriString, UriKind.Absolute, out hostUri);
	}

	private HttpClient GetCachedOrCreateHttpClient(bool async, out bool disposeRequired)
	{
		HttpClientParameters httpClientParameters = new HttpClientParameters(this, async);
		if (httpClientParameters.AreParametersAcceptableForCaching())
		{
			disposeRequired = false;
			if (s_cachedHttpClient == null)
			{
				lock (s_syncRoot)
				{
					if (s_cachedHttpClient == null)
					{
						s_cachedHttpClientParameters = httpClientParameters;
						s_cachedHttpClient = CreateHttpClient(httpClientParameters, null);
						return s_cachedHttpClient;
					}
				}
			}
			if (s_cachedHttpClientParameters.Matches(httpClientParameters))
			{
				return s_cachedHttpClient;
			}
		}
		disposeRequired = true;
		return CreateHttpClient(httpClientParameters, this);
	}

	private static HttpClient CreateHttpClient(HttpClientParameters parameters, HttpWebRequest request)
	{
		HttpClient httpClient = null;
		try
		{
			SocketsHttpHandler socketsHttpHandler = new SocketsHttpHandler();
			httpClient = new HttpClient(socketsHttpHandler);
			socketsHttpHandler.AutomaticDecompression = parameters.AutomaticDecompression;
			socketsHttpHandler.Credentials = parameters.Credentials;
			socketsHttpHandler.AllowAutoRedirect = parameters.AllowAutoRedirect;
			socketsHttpHandler.MaxAutomaticRedirections = parameters.MaximumAutomaticRedirections;
			socketsHttpHandler.MaxResponseHeadersLength = parameters.MaximumResponseHeadersLength;
			socketsHttpHandler.PreAuthenticate = parameters.PreAuthenticate;
			httpClient.Timeout = parameters.Timeout;
			if (parameters.CookieContainer != null)
			{
				socketsHttpHandler.CookieContainer = parameters.CookieContainer;
			}
			else
			{
				socketsHttpHandler.UseCookies = false;
			}
			if (parameters.Proxy == null)
			{
				socketsHttpHandler.UseProxy = false;
			}
			else if (parameters.Proxy != WebRequest.GetSystemWebProxy())
			{
				socketsHttpHandler.Proxy = parameters.Proxy;
			}
			else
			{
				socketsHttpHandler.DefaultProxyCredentials = parameters.Proxy.Credentials;
			}
			if (parameters.ClientCertificates != null)
			{
				socketsHttpHandler.SslOptions.ClientCertificates = new X509CertificateCollection(parameters.ClientCertificates);
			}
			socketsHttpHandler.SslOptions.EnabledSslProtocols = (SslProtocols)parameters.SslProtocols;
			socketsHttpHandler.SslOptions.CertificateRevocationCheckMode = (parameters.CheckCertificateRevocationList ? X509RevocationMode.Online : X509RevocationMode.NoCheck);
			RemoteCertificateValidationCallback rcvc = parameters.ServerCertificateValidationCallback;
			if (rcvc != null)
			{
				socketsHttpHandler.SslOptions.RemoteCertificateValidationCallback = (object message, X509Certificate cert, X509Chain chain, SslPolicyErrors errors) => rcvc(request, cert, chain, errors);
			}
			socketsHttpHandler.ConnectCallback = async delegate(SocketsHttpConnectionContext context, CancellationToken cancellationToken)
			{
				Socket socket = new Socket(SocketType.Stream, ProtocolType.Tcp);
				try
				{
					socket.NoDelay = true;
					if (parameters.Async)
					{
						await socket.ConnectAsync(context.DnsEndPoint, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
					}
					else
					{
						using (cancellationToken.UnsafeRegister(delegate(object s)
						{
							((Socket)s).Dispose();
						}, socket))
						{
							socket.Connect(context.DnsEndPoint);
						}
						cancellationToken.ThrowIfCancellationRequested();
					}
					if (parameters.ReadWriteTimeout > 0)
					{
						int sendTimeout = (socket.ReceiveTimeout = parameters.ReadWriteTimeout);
						socket.SendTimeout = sendTimeout;
					}
				}
				catch
				{
					socket.Dispose();
					throw;
				}
				return new NetworkStream(socket, ownsSocket: true);
			};
			return httpClient;
		}
		catch
		{
			httpClient?.Dispose();
			throw;
		}
	}
}
