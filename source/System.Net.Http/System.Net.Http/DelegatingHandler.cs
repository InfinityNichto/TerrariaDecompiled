using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;

namespace System.Net.Http;

public abstract class DelegatingHandler : HttpMessageHandler
{
	private HttpMessageHandler _innerHandler;

	private volatile bool _operationStarted;

	private volatile bool _disposed;

	public HttpMessageHandler? InnerHandler
	{
		get
		{
			return _innerHandler;
		}
		[param: DisallowNull]
		set
		{
			if (value == null)
			{
				throw new ArgumentNullException("value");
			}
			CheckDisposedOrStarted();
			if (System.Net.NetEventSource.Log.IsEnabled())
			{
				System.Net.NetEventSource.Associate(this, value, "InnerHandler");
			}
			_innerHandler = value;
		}
	}

	protected DelegatingHandler()
	{
	}

	protected DelegatingHandler(HttpMessageHandler innerHandler)
	{
		InnerHandler = innerHandler;
	}

	protected internal override HttpResponseMessage Send(HttpRequestMessage request, CancellationToken cancellationToken)
	{
		if (request == null)
		{
			throw new ArgumentNullException("request", System.SR.net_http_handler_norequest);
		}
		SetOperationStarted();
		return _innerHandler.Send(request, cancellationToken);
	}

	protected internal override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
	{
		if (request == null)
		{
			throw new ArgumentNullException("request", System.SR.net_http_handler_norequest);
		}
		SetOperationStarted();
		return _innerHandler.SendAsync(request, cancellationToken);
	}

	protected override void Dispose(bool disposing)
	{
		if (disposing && !_disposed)
		{
			_disposed = true;
			if (_innerHandler != null)
			{
				_innerHandler.Dispose();
			}
		}
		base.Dispose(disposing);
	}

	private void CheckDisposed()
	{
		if (_disposed)
		{
			throw new ObjectDisposedException(GetType().ToString());
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

	private void SetOperationStarted()
	{
		CheckDisposed();
		if (_innerHandler == null)
		{
			throw new InvalidOperationException(System.SR.net_http_handler_not_assigned);
		}
		if (!_operationStarted)
		{
			_operationStarted = true;
		}
	}
}
