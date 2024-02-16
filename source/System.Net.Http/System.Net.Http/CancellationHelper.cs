using System.Threading;
using System.Threading.Tasks;

namespace System.Net.Http;

internal static class CancellationHelper
{
	private static readonly string s_cancellationMessage = new OperationCanceledException().Message;

	internal static bool ShouldWrapInOperationCanceledException(Exception exception, CancellationToken cancellationToken)
	{
		if (!(exception is OperationCanceledException))
		{
			return cancellationToken.IsCancellationRequested;
		}
		return false;
	}

	internal static Exception CreateOperationCanceledException(Exception innerException, CancellationToken cancellationToken)
	{
		return new TaskCanceledException(s_cancellationMessage, innerException, cancellationToken);
	}

	private static void ThrowOperationCanceledException(Exception innerException, CancellationToken cancellationToken)
	{
		throw CreateOperationCanceledException(innerException, cancellationToken);
	}

	internal static void ThrowIfCancellationRequested(CancellationToken cancellationToken)
	{
		if (cancellationToken.IsCancellationRequested)
		{
			ThrowOperationCanceledException(null, cancellationToken);
		}
	}
}
