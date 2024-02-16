namespace System.Linq.Parallel;

internal sealed class ForAllSpoolingTask<TInputOutput, TIgnoreKey> : SpoolingTaskBase
{
	private readonly QueryOperatorEnumerator<TInputOutput, TIgnoreKey> _source;

	internal ForAllSpoolingTask(int taskIndex, QueryTaskGroupState groupState, QueryOperatorEnumerator<TInputOutput, TIgnoreKey> source)
		: base(taskIndex, groupState)
	{
		_source = source;
	}

	protected override void SpoolingWork()
	{
		TInputOutput currentElement = default(TInputOutput);
		TIgnoreKey currentKey = default(TIgnoreKey);
		while (_source.MoveNext(ref currentElement, ref currentKey))
		{
		}
	}

	protected override void SpoolingFinally()
	{
		base.SpoolingFinally();
		_source.Dispose();
	}
}
