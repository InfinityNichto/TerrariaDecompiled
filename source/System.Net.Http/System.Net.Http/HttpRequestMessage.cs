using System.Collections.Generic;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;

namespace System.Net.Http;

public class HttpRequestMessage : IDisposable
{
	private int _sendStatus;

	private HttpMethod _method;

	private Uri _requestUri;

	private HttpRequestHeaders _headers;

	private Version _version;

	private HttpVersionPolicy _versionPolicy;

	private HttpContent _content;

	private bool _disposed;

	private HttpRequestOptions _options;

	internal static Version DefaultRequestVersion => HttpVersion.Version11;

	internal static HttpVersionPolicy DefaultVersionPolicy => HttpVersionPolicy.RequestVersionOrLower;

	public Version Version
	{
		get
		{
			return _version;
		}
		set
		{
			if (value == null)
			{
				throw new ArgumentNullException("value");
			}
			CheckDisposed();
			_version = value;
		}
	}

	public HttpVersionPolicy VersionPolicy
	{
		get
		{
			return _versionPolicy;
		}
		set
		{
			CheckDisposed();
			_versionPolicy = value;
		}
	}

	public HttpContent? Content
	{
		get
		{
			return _content;
		}
		set
		{
			CheckDisposed();
			if (System.Net.NetEventSource.Log.IsEnabled())
			{
				if (value == null)
				{
					System.Net.NetEventSource.ContentNull(this);
				}
				else
				{
					System.Net.NetEventSource.Associate(this, value, "Content");
				}
			}
			_content = value;
		}
	}

	public HttpMethod Method
	{
		get
		{
			return _method;
		}
		set
		{
			if (value == null)
			{
				throw new ArgumentNullException("value");
			}
			CheckDisposed();
			_method = value;
		}
	}

	public Uri? RequestUri
	{
		get
		{
			return _requestUri;
		}
		set
		{
			CheckDisposed();
			_requestUri = value;
		}
	}

	public HttpRequestHeaders Headers => _headers ?? (_headers = new HttpRequestHeaders());

	internal bool HasHeaders => _headers != null;

	[Obsolete("HttpRequestMessage.Properties has been deprecated. Use Options instead.")]
	public IDictionary<string, object?> Properties => Options;

	public HttpRequestOptions Options => _options ?? (_options = new HttpRequestOptions());

	public HttpRequestMessage()
		: this(HttpMethod.Get, (Uri?)null)
	{
	}

	public HttpRequestMessage(HttpMethod method, Uri? requestUri)
	{
		_method = method ?? throw new ArgumentNullException("method");
		_requestUri = requestUri;
		_version = DefaultRequestVersion;
		_versionPolicy = DefaultVersionPolicy;
	}

	public HttpRequestMessage(HttpMethod method, string? requestUri)
		: this(method, string.IsNullOrEmpty(requestUri) ? null : new Uri(requestUri, UriKind.RelativeOrAbsolute))
	{
	}

	public override string ToString()
	{
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.Append("Method: ");
		stringBuilder.Append(_method);
		stringBuilder.Append(", RequestUri: '");
		stringBuilder.Append((_requestUri == null) ? "<null>" : _requestUri.ToString());
		stringBuilder.Append("', Version: ");
		stringBuilder.Append(_version);
		stringBuilder.Append(", Content: ");
		stringBuilder.Append((_content == null) ? "<null>" : _content.GetType().ToString());
		stringBuilder.AppendLine(", Headers:");
		HeaderUtilities.DumpHeaders(stringBuilder, _headers, _content?.Headers);
		return stringBuilder.ToString();
	}

	internal bool MarkAsSent()
	{
		return Interlocked.CompareExchange(ref _sendStatus, 1, 0) == 0;
	}

	internal bool WasSentByHttpClient()
	{
		return (_sendStatus & 1) != 0;
	}

	internal void MarkAsRedirected()
	{
		_sendStatus |= 2;
	}

	internal bool WasRedirected()
	{
		return (_sendStatus & 2) != 0;
	}

	protected virtual void Dispose(bool disposing)
	{
		if (disposing && !_disposed)
		{
			_disposed = true;
			if (_content != null)
			{
				_content.Dispose();
			}
		}
	}

	public void Dispose()
	{
		Dispose(disposing: true);
		GC.SuppressFinalize(this);
	}

	private void CheckDisposed()
	{
		if (_disposed)
		{
			throw new ObjectDisposedException(GetType().ToString());
		}
	}
}
