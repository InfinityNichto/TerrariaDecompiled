using System.Threading;

namespace System.Linq.Parallel;

internal sealed class CancellationState
{
	internal CancellationTokenSource InternalCancellationTokenSource;

	internal CancellationToken ExternalCancellationToken;

	internal CancellationTokenSource MergedCancellationTokenSource;

	internal Shared<bool> TopLevelDisposedFlag;

	internal CancellationToken MergedCancellationToken
	{
		get
		{
			if (MergedCancellationTokenSource != null)
			{
				return MergedCancellationTokenSource.Token;
			}
			return new CancellationToken(canceled: false);
		}
	}

	internal CancellationState(CancellationToken externalCancellationToken)
	{
		ExternalCancellationToken = externalCancellationToken;
		TopLevelDisposedFlag = new Shared<bool>(value: false);
	}

	internal static void ThrowWithStandardMessageIfCanceled(CancellationToken externalCancellationToken)
	{
		if (externalCancellationToken.IsCancellationRequested)
		{
			string pLINQ_ExternalCancellationRequested = System.SR.PLINQ_ExternalCancellationRequested;
			throw new OperationCanceledException(pLINQ_ExternalCancellationRequested, externalCancellationToken);
		}
	}
}
