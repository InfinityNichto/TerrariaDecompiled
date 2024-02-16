using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace System.Net.Http;

internal sealed class RedirectHandler : HttpMessageHandlerStage
{
	private readonly HttpMessageHandlerStage _initialInnerHandler;

	private readonly HttpMessageHandlerStage _redirectInnerHandler;

	private readonly int _maxAutomaticRedirections;

	public RedirectHandler(int maxAutomaticRedirections, HttpMessageHandlerStage initialInnerHandler, HttpMessageHandlerStage redirectInnerHandler)
	{
		_maxAutomaticRedirections = maxAutomaticRedirections;
		_initialInnerHandler = initialInnerHandler;
		_redirectInnerHandler = redirectInnerHandler;
	}

	internal override async ValueTask<HttpResponseMessage> SendAsync(HttpRequestMessage request, bool async, CancellationToken cancellationToken)
	{
		HttpResponseMessage httpResponseMessage = await _initialInnerHandler.SendAsync(request, async, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
		uint redirectCount = 0u;
		Uri uriForRedirect;
		while ((uriForRedirect = GetUriForRedirect(request.RequestUri, httpResponseMessage)) != null)
		{
			redirectCount++;
			if (redirectCount > _maxAutomaticRedirections)
			{
				if (System.Net.NetEventSource.Log.IsEnabled())
				{
					TraceError($"Exceeded max number of redirects. Redirect from {request.RequestUri} to {uriForRedirect} blocked.", request.GetHashCode(), "SendAsync");
				}
				break;
			}
			httpResponseMessage.Dispose();
			request.Headers.Authorization = null;
			if (System.Net.NetEventSource.Log.IsEnabled())
			{
				Trace($"Redirecting from {request.RequestUri} to {uriForRedirect} in response to status code {httpResponseMessage.StatusCode} '{httpResponseMessage.StatusCode}'.", request.GetHashCode(), "SendAsync");
			}
			request.RequestUri = uriForRedirect;
			if (RequestRequiresForceGet(httpResponseMessage.StatusCode, request.Method))
			{
				if (System.Net.NetEventSource.Log.IsEnabled())
				{
					Trace($"Modified request from {request.Method} to {HttpMethod.Get} in response to status code {httpResponseMessage.StatusCode} '{httpResponseMessage.StatusCode}'.", request.GetHashCode(), "SendAsync");
				}
				request.Method = HttpMethod.Get;
				request.Content = null;
				if (request.Headers.TransferEncodingChunked == true)
				{
					request.Headers.TransferEncodingChunked = false;
				}
			}
			request.MarkAsRedirected();
			httpResponseMessage = await _redirectInnerHandler.SendAsync(request, async, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
		}
		return httpResponseMessage;
	}

	private Uri GetUriForRedirect(Uri requestUri, HttpResponseMessage response)
	{
		HttpStatusCode statusCode = response.StatusCode;
		if ((uint)(statusCode - 300) > 3u && (uint)(statusCode - 307) > 1u)
		{
			return null;
		}
		Uri uri = response.Headers.Location;
		if (uri == null)
		{
			return null;
		}
		if (!uri.IsAbsoluteUri)
		{
			uri = new Uri(requestUri, uri);
		}
		string fragment = requestUri.Fragment;
		if (!string.IsNullOrEmpty(fragment))
		{
			string fragment2 = uri.Fragment;
			if (string.IsNullOrEmpty(fragment2))
			{
				uri = new UriBuilder(uri)
				{
					Fragment = fragment
				}.Uri;
			}
		}
		if (HttpUtilities.IsSupportedSecureScheme(requestUri.Scheme) && !HttpUtilities.IsSupportedSecureScheme(uri.Scheme))
		{
			if (System.Net.NetEventSource.Log.IsEnabled())
			{
				TraceError($"Insecure https to http redirect from '{requestUri}' to '{uri}' blocked.", response.RequestMessage.GetHashCode(), "GetUriForRedirect");
			}
			return null;
		}
		return uri;
	}

	private static bool RequestRequiresForceGet(HttpStatusCode statusCode, HttpMethod requestMethod)
	{
		switch (statusCode)
		{
		case HttpStatusCode.MultipleChoices:
		case HttpStatusCode.MovedPermanently:
		case HttpStatusCode.Found:
			return requestMethod == HttpMethod.Post;
		case HttpStatusCode.SeeOther:
			if (requestMethod != HttpMethod.Get)
			{
				return requestMethod != HttpMethod.Head;
			}
			return false;
		default:
			return false;
		}
	}

	protected override void Dispose(bool disposing)
	{
		if (disposing)
		{
			_initialInnerHandler.Dispose();
			_redirectInnerHandler.Dispose();
		}
		base.Dispose(disposing);
	}

	internal void Trace(string message, int requestId, [CallerMemberName] string memberName = null)
	{
		System.Net.NetEventSource.Log.HandlerMessage(0, 0, requestId, memberName, ToString() + ": " + message);
	}

	internal void TraceError(string message, int requestId, [CallerMemberName] string memberName = null)
	{
		System.Net.NetEventSource.Log.HandlerMessageError(0, 0, requestId, memberName, ToString() + ": " + message);
	}
}
