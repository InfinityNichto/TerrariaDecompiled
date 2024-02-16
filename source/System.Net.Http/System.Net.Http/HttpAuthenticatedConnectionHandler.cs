using System.Threading;
using System.Threading.Tasks;

namespace System.Net.Http;

internal sealed class HttpAuthenticatedConnectionHandler : HttpMessageHandlerStage
{
	private readonly HttpConnectionPoolManager _poolManager;

	public HttpAuthenticatedConnectionHandler(HttpConnectionPoolManager poolManager)
	{
		_poolManager = poolManager;
	}

	internal override ValueTask<HttpResponseMessage> SendAsync(HttpRequestMessage request, bool async, CancellationToken cancellationToken)
	{
		return _poolManager.SendAsync(request, async, doRequestAuth: true, cancellationToken);
	}

	protected override void Dispose(bool disposing)
	{
		if (disposing)
		{
			_poolManager.Dispose();
		}
		base.Dispose(disposing);
	}
}
