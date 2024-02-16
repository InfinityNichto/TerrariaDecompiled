namespace System.Net.Http;

public class HttpRequestException : Exception
{
	internal RequestRetryType AllowRetry { get; }

	public HttpStatusCode? StatusCode { get; }

	public HttpRequestException()
		: this(null, null)
	{
	}

	public HttpRequestException(string? message)
		: this(message, null)
	{
	}

	public HttpRequestException(string? message, Exception? inner)
		: base(message, inner)
	{
		if (inner != null)
		{
			base.HResult = inner.HResult;
		}
	}

	public HttpRequestException(string? message, Exception? inner, HttpStatusCode? statusCode)
		: this(message, inner)
	{
		StatusCode = statusCode;
	}

	internal HttpRequestException(string message, Exception inner, RequestRetryType allowRetry)
		: this(message, inner)
	{
		AllowRetry = allowRetry;
	}
}
