using System.Threading;
using System.Threading.Tasks;

namespace System.Net.Http;

internal abstract class HttpMessageHandlerStage : HttpMessageHandler
{
	protected internal sealed override HttpResponseMessage Send(HttpRequestMessage request, CancellationToken cancellationToken)
	{
		ValueTask<HttpResponseMessage> valueTask = SendAsync(request, async: false, cancellationToken);
		if (!valueTask.IsCompleted)
		{
			return valueTask.AsTask().GetAwaiter().GetResult();
		}
		return valueTask.Result;
	}

	protected internal sealed override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
	{
		return SendAsync(request, async: true, cancellationToken).AsTask();
	}

	internal abstract ValueTask<HttpResponseMessage> SendAsync(HttpRequestMessage request, bool async, CancellationToken cancellationToken);
}
