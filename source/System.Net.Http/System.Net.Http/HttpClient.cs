using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Net.Http.Headers;
using System.Runtime.Versioning;
using System.Threading;
using System.Threading.Tasks;

namespace System.Net.Http;

public class HttpClient : HttpMessageInvoker
{
	private static IWebProxy s_defaultProxy;

	private static readonly TimeSpan s_defaultTimeout = TimeSpan.FromSeconds(100.0);

	private static readonly TimeSpan s_maxTimeout = TimeSpan.FromMilliseconds(2147483647.0);

	private static readonly TimeSpan s_infiniteTimeout = System.Threading.Timeout.InfiniteTimeSpan;

	private volatile bool _operationStarted;

	private volatile bool _disposed;

	private CancellationTokenSource _pendingRequestsCts;

	private HttpRequestHeaders _defaultRequestHeaders;

	private Version _defaultRequestVersion = HttpRequestMessage.DefaultRequestVersion;

	private HttpVersionPolicy _defaultVersionPolicy = HttpRequestMessage.DefaultVersionPolicy;

	private Uri _baseAddress;

	private TimeSpan _timeout;

	private int _maxResponseContentBufferSize;

	public static IWebProxy DefaultProxy
	{
		get
		{
			return LazyInitializer.EnsureInitialized(ref s_defaultProxy, () => SystemProxyInfo.Proxy);
		}
		set
		{
			s_defaultProxy = value ?? throw new ArgumentNullException("value");
		}
	}

	public HttpRequestHeaders DefaultRequestHeaders => _defaultRequestHeaders ?? (_defaultRequestHeaders = new HttpRequestHeaders());

	public Version DefaultRequestVersion
	{
		get
		{
			return _defaultRequestVersion;
		}
		set
		{
			CheckDisposedOrStarted();
			_defaultRequestVersion = value ?? throw new ArgumentNullException("value");
		}
	}

	public HttpVersionPolicy DefaultVersionPolicy
	{
		get
		{
			return _defaultVersionPolicy;
		}
		set
		{
			CheckDisposedOrStarted();
			_defaultVersionPolicy = value;
		}
	}

	public Uri? BaseAddress
	{
		get
		{
			return _baseAddress;
		}
		set
		{
			if ((object)value != null && !value.IsAbsoluteUri)
			{
				throw new ArgumentException(System.SR.net_http_client_absolute_baseaddress_required, "value");
			}
			CheckDisposedOrStarted();
			if (System.Net.NetEventSource.Log.IsEnabled())
			{
				System.Net.NetEventSource.UriBaseAddress(this, value);
			}
			_baseAddress = value;
		}
	}

	public TimeSpan Timeout
	{
		get
		{
			return _timeout;
		}
		set
		{
			if (value != s_infiniteTimeout && (value <= TimeSpan.Zero || value > s_maxTimeout))
			{
				throw new ArgumentOutOfRangeException("value");
			}
			CheckDisposedOrStarted();
			_timeout = value;
		}
	}

	public long MaxResponseContentBufferSize
	{
		get
		{
			return _maxResponseContentBufferSize;
		}
		set
		{
			if (value <= 0)
			{
				throw new ArgumentOutOfRangeException("value");
			}
			if (value > int.MaxValue)
			{
				throw new ArgumentOutOfRangeException("value", value, System.SR.Format(CultureInfo.InvariantCulture, System.SR.net_http_content_buffersize_limit, int.MaxValue));
			}
			CheckDisposedOrStarted();
			_maxResponseContentBufferSize = (int)value;
		}
	}

	public HttpClient()
		: this(new HttpClientHandler())
	{
	}

	public HttpClient(HttpMessageHandler handler)
		: this(handler, disposeHandler: true)
	{
	}

	public HttpClient(HttpMessageHandler handler, bool disposeHandler)
		: base(handler, disposeHandler)
	{
		_timeout = s_defaultTimeout;
		_maxResponseContentBufferSize = int.MaxValue;
		_pendingRequestsCts = new CancellationTokenSource();
	}

	public Task<string> GetStringAsync(string? requestUri)
	{
		return GetStringAsync(CreateUri(requestUri));
	}

	public Task<string> GetStringAsync(Uri? requestUri)
	{
		return GetStringAsync(requestUri, CancellationToken.None);
	}

	public Task<string> GetStringAsync(string? requestUri, CancellationToken cancellationToken)
	{
		return GetStringAsync(CreateUri(requestUri), cancellationToken);
	}

	public Task<string> GetStringAsync(Uri? requestUri, CancellationToken cancellationToken)
	{
		HttpRequestMessage request = CreateRequestMessage(HttpMethod.Get, requestUri);
		CheckRequestBeforeSend(request);
		return GetStringAsyncCore(request, cancellationToken);
	}

	private async Task<string> GetStringAsyncCore(HttpRequestMessage request, CancellationToken cancellationToken)
	{
		bool telemetryStarted = StartSend(request);
		bool responseContentTelemetryStarted = false;
		(CancellationTokenSource, bool, CancellationTokenSource) tuple = PrepareCancellationTokenSource(cancellationToken);
		CancellationTokenSource cts = tuple.Item1;
		bool disposeCts = tuple.Item2;
		CancellationTokenSource pendingRequestsCts = tuple.Item3;
		HttpResponseMessage response = null;
		try
		{
			response = await base.SendAsync(request, cts.Token).ConfigureAwait(continueOnCapturedContext: false);
			ThrowForNullResponse(response);
			response.EnsureSuccessStatusCode();
			HttpContent c = response.Content;
			if (HttpTelemetry.Log.IsEnabled() && telemetryStarted)
			{
				HttpTelemetry.Log.ResponseContentStart();
				responseContentTelemetryStarted = true;
			}
			Stream stream = c.TryReadAsStream();
			Stream stream2 = stream;
			if (stream2 == null)
			{
				stream2 = await c.ReadAsStreamAsync(cts.Token).ConfigureAwait(continueOnCapturedContext: false);
			}
			using Stream responseStream = stream2;
			using HttpContent.LimitArrayPoolWriteStream buffer = new HttpContent.LimitArrayPoolWriteStream(_maxResponseContentBufferSize, (int)c.Headers.ContentLength.GetValueOrDefault());
			_ = 2;
			try
			{
				await responseStream.CopyToAsync(buffer, cts.Token).ConfigureAwait(continueOnCapturedContext: false);
			}
			catch (Exception e) when (HttpContent.StreamCopyExceptionNeedsWrapping(e))
			{
				throw HttpContent.WrapStreamCopyException(e);
			}
			if (buffer.Length > 0)
			{
				return HttpContent.ReadBufferAsString(buffer.GetBuffer(), c.Headers);
			}
			return string.Empty;
		}
		catch (Exception e2)
		{
			HandleFailure(e2, telemetryStarted, response, cts, cancellationToken, pendingRequestsCts);
			throw;
		}
		finally
		{
			FinishSend(cts, disposeCts, telemetryStarted, responseContentTelemetryStarted);
		}
	}

	public Task<byte[]> GetByteArrayAsync(string? requestUri)
	{
		return GetByteArrayAsync(CreateUri(requestUri));
	}

	public Task<byte[]> GetByteArrayAsync(Uri? requestUri)
	{
		return GetByteArrayAsync(requestUri, CancellationToken.None);
	}

	public Task<byte[]> GetByteArrayAsync(string? requestUri, CancellationToken cancellationToken)
	{
		return GetByteArrayAsync(CreateUri(requestUri), cancellationToken);
	}

	public Task<byte[]> GetByteArrayAsync(Uri? requestUri, CancellationToken cancellationToken)
	{
		HttpRequestMessage request = CreateRequestMessage(HttpMethod.Get, requestUri);
		CheckRequestBeforeSend(request);
		return GetByteArrayAsyncCore(request, cancellationToken);
	}

	private async Task<byte[]> GetByteArrayAsyncCore(HttpRequestMessage request, CancellationToken cancellationToken)
	{
		bool telemetryStarted = StartSend(request);
		bool responseContentTelemetryStarted = false;
		(CancellationTokenSource, bool, CancellationTokenSource) tuple = PrepareCancellationTokenSource(cancellationToken);
		CancellationTokenSource cts = tuple.Item1;
		bool disposeCts = tuple.Item2;
		CancellationTokenSource pendingRequestsCts = tuple.Item3;
		HttpResponseMessage response = null;
		try
		{
			response = await base.SendAsync(request, cts.Token).ConfigureAwait(continueOnCapturedContext: false);
			ThrowForNullResponse(response);
			response.EnsureSuccessStatusCode();
			HttpContent content = response.Content;
			if (HttpTelemetry.Log.IsEnabled() && telemetryStarted)
			{
				HttpTelemetry.Log.ResponseContentStart();
				responseContentTelemetryStarted = true;
			}
			long? contentLength = content.Headers.ContentLength;
			using Stream buffer = (contentLength.HasValue ? ((Stream)new HttpContent.LimitMemoryStream(_maxResponseContentBufferSize, (int)contentLength.GetValueOrDefault())) : ((Stream)new HttpContent.LimitArrayPoolWriteStream(_maxResponseContentBufferSize)));
			Stream stream = content.TryReadAsStream();
			Stream stream2 = stream;
			if (stream2 == null)
			{
				stream2 = await content.ReadAsStreamAsync(cts.Token).ConfigureAwait(continueOnCapturedContext: false);
			}
			using Stream responseStream = stream2;
			_ = 2;
			try
			{
				await responseStream.CopyToAsync(buffer, cts.Token).ConfigureAwait(continueOnCapturedContext: false);
			}
			catch (Exception e) when (HttpContent.StreamCopyExceptionNeedsWrapping(e))
			{
				throw HttpContent.WrapStreamCopyException(e);
			}
			return (buffer.Length == 0L) ? Array.Empty<byte>() : ((buffer is HttpContent.LimitMemoryStream limitMemoryStream) ? limitMemoryStream.GetSizedBuffer() : ((HttpContent.LimitArrayPoolWriteStream)buffer).ToArray());
		}
		catch (Exception e2)
		{
			HandleFailure(e2, telemetryStarted, response, cts, cancellationToken, pendingRequestsCts);
			throw;
		}
		finally
		{
			FinishSend(cts, disposeCts, telemetryStarted, responseContentTelemetryStarted);
		}
	}

	public Task<Stream> GetStreamAsync(string? requestUri)
	{
		return GetStreamAsync(CreateUri(requestUri));
	}

	public Task<Stream> GetStreamAsync(string? requestUri, CancellationToken cancellationToken)
	{
		return GetStreamAsync(CreateUri(requestUri), cancellationToken);
	}

	public Task<Stream> GetStreamAsync(Uri? requestUri)
	{
		return GetStreamAsync(requestUri, CancellationToken.None);
	}

	public Task<Stream> GetStreamAsync(Uri? requestUri, CancellationToken cancellationToken)
	{
		HttpRequestMessage request = CreateRequestMessage(HttpMethod.Get, requestUri);
		CheckRequestBeforeSend(request);
		return GetStreamAsyncCore(request, cancellationToken);
	}

	private async Task<Stream> GetStreamAsyncCore(HttpRequestMessage request, CancellationToken cancellationToken)
	{
		bool telemetryStarted = StartSend(request);
		(CancellationTokenSource, bool, CancellationTokenSource) tuple = PrepareCancellationTokenSource(cancellationToken);
		CancellationTokenSource cts = tuple.Item1;
		bool disposeCts = tuple.Item2;
		CancellationTokenSource pendingRequestsCts = tuple.Item3;
		HttpResponseMessage response = null;
		try
		{
			response = await base.SendAsync(request, cts.Token).ConfigureAwait(continueOnCapturedContext: false);
			ThrowForNullResponse(response);
			response.EnsureSuccessStatusCode();
			HttpContent content = response.Content;
			Stream stream = content.TryReadAsStream();
			Stream stream2 = stream;
			if (stream2 == null)
			{
				stream2 = await content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
			}
			return stream2;
		}
		catch (Exception e)
		{
			HandleFailure(e, telemetryStarted, response, cts, cancellationToken, pendingRequestsCts);
			throw;
		}
		finally
		{
			FinishSend(cts, disposeCts, telemetryStarted, responseContentTelemetryStarted: false);
		}
	}

	public Task<HttpResponseMessage> GetAsync(string? requestUri)
	{
		return GetAsync(CreateUri(requestUri));
	}

	public Task<HttpResponseMessage> GetAsync(Uri? requestUri)
	{
		return GetAsync(requestUri, HttpCompletionOption.ResponseContentRead);
	}

	public Task<HttpResponseMessage> GetAsync(string? requestUri, HttpCompletionOption completionOption)
	{
		return GetAsync(CreateUri(requestUri), completionOption);
	}

	public Task<HttpResponseMessage> GetAsync(Uri? requestUri, HttpCompletionOption completionOption)
	{
		return GetAsync(requestUri, completionOption, CancellationToken.None);
	}

	public Task<HttpResponseMessage> GetAsync(string? requestUri, CancellationToken cancellationToken)
	{
		return GetAsync(CreateUri(requestUri), cancellationToken);
	}

	public Task<HttpResponseMessage> GetAsync(Uri? requestUri, CancellationToken cancellationToken)
	{
		return GetAsync(requestUri, HttpCompletionOption.ResponseContentRead, cancellationToken);
	}

	public Task<HttpResponseMessage> GetAsync(string? requestUri, HttpCompletionOption completionOption, CancellationToken cancellationToken)
	{
		return GetAsync(CreateUri(requestUri), completionOption, cancellationToken);
	}

	public Task<HttpResponseMessage> GetAsync(Uri? requestUri, HttpCompletionOption completionOption, CancellationToken cancellationToken)
	{
		return SendAsync(CreateRequestMessage(HttpMethod.Get, requestUri), completionOption, cancellationToken);
	}

	public Task<HttpResponseMessage> PostAsync(string? requestUri, HttpContent? content)
	{
		return PostAsync(CreateUri(requestUri), content);
	}

	public Task<HttpResponseMessage> PostAsync(Uri? requestUri, HttpContent? content)
	{
		return PostAsync(requestUri, content, CancellationToken.None);
	}

	public Task<HttpResponseMessage> PostAsync(string? requestUri, HttpContent? content, CancellationToken cancellationToken)
	{
		return PostAsync(CreateUri(requestUri), content, cancellationToken);
	}

	public Task<HttpResponseMessage> PostAsync(Uri? requestUri, HttpContent? content, CancellationToken cancellationToken)
	{
		HttpRequestMessage httpRequestMessage = CreateRequestMessage(HttpMethod.Post, requestUri);
		httpRequestMessage.Content = content;
		return SendAsync(httpRequestMessage, cancellationToken);
	}

	public Task<HttpResponseMessage> PutAsync(string? requestUri, HttpContent? content)
	{
		return PutAsync(CreateUri(requestUri), content);
	}

	public Task<HttpResponseMessage> PutAsync(Uri? requestUri, HttpContent? content)
	{
		return PutAsync(requestUri, content, CancellationToken.None);
	}

	public Task<HttpResponseMessage> PutAsync(string? requestUri, HttpContent? content, CancellationToken cancellationToken)
	{
		return PutAsync(CreateUri(requestUri), content, cancellationToken);
	}

	public Task<HttpResponseMessage> PutAsync(Uri? requestUri, HttpContent? content, CancellationToken cancellationToken)
	{
		HttpRequestMessage httpRequestMessage = CreateRequestMessage(HttpMethod.Put, requestUri);
		httpRequestMessage.Content = content;
		return SendAsync(httpRequestMessage, cancellationToken);
	}

	public Task<HttpResponseMessage> PatchAsync(string? requestUri, HttpContent? content)
	{
		return PatchAsync(CreateUri(requestUri), content);
	}

	public Task<HttpResponseMessage> PatchAsync(Uri? requestUri, HttpContent? content)
	{
		return PatchAsync(requestUri, content, CancellationToken.None);
	}

	public Task<HttpResponseMessage> PatchAsync(string? requestUri, HttpContent? content, CancellationToken cancellationToken)
	{
		return PatchAsync(CreateUri(requestUri), content, cancellationToken);
	}

	public Task<HttpResponseMessage> PatchAsync(Uri? requestUri, HttpContent? content, CancellationToken cancellationToken)
	{
		HttpRequestMessage httpRequestMessage = CreateRequestMessage(HttpMethod.Patch, requestUri);
		httpRequestMessage.Content = content;
		return SendAsync(httpRequestMessage, cancellationToken);
	}

	public Task<HttpResponseMessage> DeleteAsync(string? requestUri)
	{
		return DeleteAsync(CreateUri(requestUri));
	}

	public Task<HttpResponseMessage> DeleteAsync(Uri? requestUri)
	{
		return DeleteAsync(requestUri, CancellationToken.None);
	}

	public Task<HttpResponseMessage> DeleteAsync(string? requestUri, CancellationToken cancellationToken)
	{
		return DeleteAsync(CreateUri(requestUri), cancellationToken);
	}

	public Task<HttpResponseMessage> DeleteAsync(Uri? requestUri, CancellationToken cancellationToken)
	{
		return SendAsync(CreateRequestMessage(HttpMethod.Delete, requestUri), cancellationToken);
	}

	[UnsupportedOSPlatform("browser")]
	public HttpResponseMessage Send(HttpRequestMessage request)
	{
		return Send(request, HttpCompletionOption.ResponseContentRead, default(CancellationToken));
	}

	[UnsupportedOSPlatform("browser")]
	public HttpResponseMessage Send(HttpRequestMessage request, HttpCompletionOption completionOption)
	{
		return Send(request, completionOption, default(CancellationToken));
	}

	[UnsupportedOSPlatform("browser")]
	public override HttpResponseMessage Send(HttpRequestMessage request, CancellationToken cancellationToken)
	{
		return Send(request, HttpCompletionOption.ResponseContentRead, cancellationToken);
	}

	[UnsupportedOSPlatform("browser")]
	public HttpResponseMessage Send(HttpRequestMessage request, HttpCompletionOption completionOption, CancellationToken cancellationToken)
	{
		CheckRequestBeforeSend(request);
		(CancellationTokenSource TokenSource, bool DisposeTokenSource, CancellationTokenSource PendingRequestsCts) tuple = PrepareCancellationTokenSource(cancellationToken);
		CancellationTokenSource item = tuple.TokenSource;
		bool item2 = tuple.DisposeTokenSource;
		CancellationTokenSource item3 = tuple.PendingRequestsCts;
		bool flag = StartSend(request);
		bool responseContentTelemetryStarted = false;
		HttpResponseMessage httpResponseMessage = null;
		try
		{
			httpResponseMessage = base.Send(request, item.Token);
			ThrowForNullResponse(httpResponseMessage);
			if (ShouldBufferResponse(completionOption, request))
			{
				if (HttpTelemetry.Log.IsEnabled() && flag)
				{
					HttpTelemetry.Log.ResponseContentStart();
					responseContentTelemetryStarted = true;
				}
				httpResponseMessage.Content.LoadIntoBuffer(_maxResponseContentBufferSize, item.Token);
			}
			return httpResponseMessage;
		}
		catch (Exception e)
		{
			HandleFailure(e, flag, httpResponseMessage, item, cancellationToken, item3);
			throw;
		}
		finally
		{
			FinishSend(item, item2, flag, responseContentTelemetryStarted);
		}
	}

	public Task<HttpResponseMessage> SendAsync(HttpRequestMessage request)
	{
		return SendAsync(request, HttpCompletionOption.ResponseContentRead, CancellationToken.None);
	}

	public override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
	{
		return SendAsync(request, HttpCompletionOption.ResponseContentRead, cancellationToken);
	}

	public Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, HttpCompletionOption completionOption)
	{
		return SendAsync(request, completionOption, CancellationToken.None);
	}

	public Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, HttpCompletionOption completionOption, CancellationToken cancellationToken)
	{
		CheckRequestBeforeSend(request);
		var (cts2, disposeCts2, pendingRequestsCts2) = PrepareCancellationTokenSource(cancellationToken);
		return Core(request, completionOption, cts2, disposeCts2, pendingRequestsCts2, cancellationToken);
		async Task<HttpResponseMessage> Core(HttpRequestMessage request, HttpCompletionOption completionOption, CancellationTokenSource cts, bool disposeCts, CancellationTokenSource pendingRequestsCts, CancellationToken originalCancellationToken)
		{
			bool telemetryStarted = StartSend(request);
			bool responseContentTelemetryStarted = false;
			HttpResponseMessage response = null;
			try
			{
				response = await base.SendAsync(request, cts.Token).ConfigureAwait(continueOnCapturedContext: false);
				ThrowForNullResponse(response);
				if (ShouldBufferResponse(completionOption, request))
				{
					if (HttpTelemetry.Log.IsEnabled() && telemetryStarted)
					{
						HttpTelemetry.Log.ResponseContentStart();
						responseContentTelemetryStarted = true;
					}
					await response.Content.LoadIntoBufferAsync(_maxResponseContentBufferSize, cts.Token).ConfigureAwait(continueOnCapturedContext: false);
				}
				return response;
			}
			catch (Exception e)
			{
				HandleFailure(e, telemetryStarted, response, cts, originalCancellationToken, pendingRequestsCts);
				throw;
			}
			finally
			{
				FinishSend(cts, disposeCts, telemetryStarted, responseContentTelemetryStarted);
			}
		}
	}

	private void CheckRequestBeforeSend(HttpRequestMessage request)
	{
		if (request == null)
		{
			throw new ArgumentNullException("request", System.SR.net_http_handler_norequest);
		}
		CheckDisposed();
		CheckRequestMessage(request);
		SetOperationStarted();
		PrepareRequestMessage(request);
	}

	private static void ThrowForNullResponse([NotNull] HttpResponseMessage response)
	{
		if (response == null)
		{
			throw new InvalidOperationException(System.SR.net_http_handler_noresponse);
		}
	}

	private static bool ShouldBufferResponse(HttpCompletionOption completionOption, HttpRequestMessage request)
	{
		if (completionOption == HttpCompletionOption.ResponseContentRead)
		{
			return !string.Equals(request.Method.Method, "HEAD", StringComparison.OrdinalIgnoreCase);
		}
		return false;
	}

	private void HandleFailure(Exception e, bool telemetryStarted, HttpResponseMessage response, CancellationTokenSource cts, CancellationToken cancellationToken, CancellationTokenSource pendingRequestsCts)
	{
		HttpMessageInvoker.LogRequestFailed(telemetryStarted);
		response?.Dispose();
		Exception ex = null;
		if (e is OperationCanceledException ex2)
		{
			if (cancellationToken.IsCancellationRequested)
			{
				if (ex2.CancellationToken != cancellationToken)
				{
					e = (ex = new TaskCanceledException(ex2.Message, ex2.InnerException, cancellationToken));
				}
			}
			else if (!pendingRequestsCts.IsCancellationRequested)
			{
				e = (ex = new TaskCanceledException(System.SR.Format(System.SR.net_http_request_timedout, _timeout.TotalSeconds), new TimeoutException(e.Message, e), ex2.CancellationToken));
			}
		}
		else if (e is HttpRequestException && cts.IsCancellationRequested)
		{
			e = (ex = new OperationCanceledException(cancellationToken.IsCancellationRequested ? cancellationToken : cts.Token));
		}
		if (System.Net.NetEventSource.Log.IsEnabled())
		{
			System.Net.NetEventSource.Error(this, e, "HandleFailure");
		}
		if (ex != null)
		{
			throw ex;
		}
	}

	private static bool StartSend(HttpRequestMessage request)
	{
		if (HttpTelemetry.Log.IsEnabled())
		{
			HttpTelemetry.Log.RequestStart(request);
			return true;
		}
		return false;
	}

	private static void FinishSend(CancellationTokenSource cts, bool disposeCts, bool telemetryStarted, bool responseContentTelemetryStarted)
	{
		if (HttpTelemetry.Log.IsEnabled() && telemetryStarted)
		{
			if (responseContentTelemetryStarted)
			{
				HttpTelemetry.Log.ResponseContentStop();
			}
			HttpTelemetry.Log.RequestStop();
		}
		if (disposeCts)
		{
			cts.Dispose();
		}
	}

	public void CancelPendingRequests()
	{
		CheckDisposed();
		CancellationTokenSource cancellationTokenSource = Interlocked.Exchange(ref _pendingRequestsCts, new CancellationTokenSource());
		cancellationTokenSource.Cancel();
		cancellationTokenSource.Dispose();
	}

	protected override void Dispose(bool disposing)
	{
		if (disposing && !_disposed)
		{
			_disposed = true;
			_pendingRequestsCts.Cancel();
			_pendingRequestsCts.Dispose();
		}
		base.Dispose(disposing);
	}

	private void SetOperationStarted()
	{
		if (!_operationStarted)
		{
			_operationStarted = true;
		}
	}

	private void CheckDisposedOrStarted()
	{
		CheckDisposed();
		if (_operationStarted)
		{
			throw new InvalidOperationException(System.SR.net_http_operation_started);
		}
	}

	private void CheckDisposed()
	{
		if (_disposed)
		{
			throw new ObjectDisposedException(GetType().ToString());
		}
	}

	private static void CheckRequestMessage(HttpRequestMessage request)
	{
		if (!request.MarkAsSent())
		{
			throw new InvalidOperationException(System.SR.net_http_client_request_already_sent);
		}
	}

	private void PrepareRequestMessage(HttpRequestMessage request)
	{
		Uri uri = null;
		if (request.RequestUri == null && _baseAddress == null)
		{
			throw new InvalidOperationException(System.SR.net_http_client_invalid_requesturi);
		}
		if (request.RequestUri == null)
		{
			uri = _baseAddress;
		}
		else if (!request.RequestUri.IsAbsoluteUri)
		{
			if (_baseAddress == null)
			{
				throw new InvalidOperationException(System.SR.net_http_client_invalid_requesturi);
			}
			uri = new Uri(_baseAddress, request.RequestUri);
		}
		if (uri != null)
		{
			request.RequestUri = uri;
		}
		if (_defaultRequestHeaders != null)
		{
			request.Headers.AddHeaders(_defaultRequestHeaders);
		}
	}

	private (CancellationTokenSource TokenSource, bool DisposeTokenSource, CancellationTokenSource PendingRequestsCts) PrepareCancellationTokenSource(CancellationToken cancellationToken)
	{
		CancellationTokenSource pendingRequestsCts = _pendingRequestsCts;
		bool flag = _timeout != s_infiniteTimeout;
		if (flag || cancellationToken.CanBeCanceled)
		{
			CancellationTokenSource cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, pendingRequestsCts.Token);
			if (flag)
			{
				cancellationTokenSource.CancelAfter(_timeout);
			}
			return (TokenSource: cancellationTokenSource, DisposeTokenSource: true, PendingRequestsCts: pendingRequestsCts);
		}
		return (TokenSource: pendingRequestsCts, DisposeTokenSource: false, PendingRequestsCts: pendingRequestsCts);
	}

	private Uri CreateUri(string uri)
	{
		if (!string.IsNullOrEmpty(uri))
		{
			return new Uri(uri, UriKind.RelativeOrAbsolute);
		}
		return null;
	}

	private HttpRequestMessage CreateRequestMessage(HttpMethod method, Uri uri)
	{
		return new HttpRequestMessage(method, uri)
		{
			Version = _defaultRequestVersion,
			VersionPolicy = _defaultVersionPolicy
		};
	}
}
