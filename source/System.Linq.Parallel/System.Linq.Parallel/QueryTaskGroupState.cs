using System.Threading;
using System.Threading.Tasks;

namespace System.Linq.Parallel;

internal sealed class QueryTaskGroupState
{
	private Task _rootTask;

	private int _alreadyEnded;

	private readonly CancellationState _cancellationState;

	private readonly int _queryId;

	internal bool IsAlreadyEnded => _alreadyEnded == 1;

	internal CancellationState CancellationState => _cancellationState;

	internal int QueryId => _queryId;

	internal QueryTaskGroupState(CancellationState cancellationState, int queryId)
	{
		_cancellationState = cancellationState;
		_queryId = queryId;
	}

	internal void QueryBegin(Task rootTask)
	{
		_rootTask = rootTask;
	}

	internal void QueryEnd(bool userInitiatedDispose)
	{
		if (Interlocked.Exchange(ref _alreadyEnded, 1) != 0)
		{
			return;
		}
		try
		{
			_rootTask.Wait();
		}
		catch (AggregateException ex)
		{
			AggregateException ex2 = ex.Flatten();
			bool flag = true;
			for (int i = 0; i < ex2.InnerExceptions.Count; i++)
			{
				if (!(ex2.InnerExceptions[i] is OperationCanceledException { CancellationToken: { IsCancellationRequested: not false } } ex3) || ex3.CancellationToken != _cancellationState.ExternalCancellationToken)
				{
					flag = false;
					break;
				}
			}
			if (!flag || ex2.InnerExceptions.Count == 0)
			{
				throw ex2;
			}
		}
		finally
		{
			((IDisposable)_rootTask)?.Dispose();
		}
		if (_cancellationState.MergedCancellationToken.IsCancellationRequested)
		{
			if (!_cancellationState.TopLevelDisposedFlag.Value)
			{
				CancellationState.ThrowWithStandardMessageIfCanceled(_cancellationState.ExternalCancellationToken);
			}
			if (!userInitiatedDispose)
			{
				throw new ObjectDisposedException("enumerator", System.SR.PLINQ_DisposeRequested);
			}
		}
	}
}
