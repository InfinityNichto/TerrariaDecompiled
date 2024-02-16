namespace System.Linq.Parallel;

internal abstract class SpoolingTaskBase : QueryTask
{
	protected SpoolingTaskBase(int taskIndex, QueryTaskGroupState groupState)
		: base(taskIndex, groupState)
	{
	}

	protected override void Work()
	{
		try
		{
			SpoolingWork();
		}
		catch (Exception ex)
		{
			if (!(ex is OperationCanceledException ex2) || !(ex2.CancellationToken == _groupState.CancellationState.MergedCancellationToken) || !_groupState.CancellationState.MergedCancellationToken.IsCancellationRequested)
			{
				_groupState.CancellationState.InternalCancellationTokenSource.Cancel();
				throw;
			}
		}
		finally
		{
			SpoolingFinally();
		}
	}

	protected abstract void SpoolingWork();

	protected virtual void SpoolingFinally()
	{
	}
}
