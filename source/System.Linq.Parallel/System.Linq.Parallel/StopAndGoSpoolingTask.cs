using System.Threading;

namespace System.Linq.Parallel;

internal sealed class StopAndGoSpoolingTask<TInputOutput, TIgnoreKey> : SpoolingTaskBase
{
	private readonly QueryOperatorEnumerator<TInputOutput, TIgnoreKey> _source;

	private readonly SynchronousChannel<TInputOutput> _destination;

	internal StopAndGoSpoolingTask(int taskIndex, QueryTaskGroupState groupState, QueryOperatorEnumerator<TInputOutput, TIgnoreKey> source, SynchronousChannel<TInputOutput> destination)
		: base(taskIndex, groupState)
	{
		_source = source;
		_destination = destination;
	}

	protected override void SpoolingWork()
	{
		TInputOutput currentElement = default(TInputOutput);
		TIgnoreKey currentKey = default(TIgnoreKey);
		QueryOperatorEnumerator<TInputOutput, TIgnoreKey> source = _source;
		SynchronousChannel<TInputOutput> destination = _destination;
		CancellationToken mergedCancellationToken = _groupState.CancellationState.MergedCancellationToken;
		destination.Init();
		while (source.MoveNext(ref currentElement, ref currentKey) && !mergedCancellationToken.IsCancellationRequested)
		{
			destination.Enqueue(currentElement);
		}
	}

	protected override void SpoolingFinally()
	{
		base.SpoolingFinally();
		if (_destination != null)
		{
			_destination.SetDone();
		}
		_source.Dispose();
	}
}
