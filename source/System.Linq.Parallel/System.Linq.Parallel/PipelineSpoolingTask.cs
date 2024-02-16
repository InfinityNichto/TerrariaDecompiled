using System.Threading;

namespace System.Linq.Parallel;

internal sealed class PipelineSpoolingTask<TInputOutput, TIgnoreKey> : SpoolingTaskBase
{
	private readonly QueryOperatorEnumerator<TInputOutput, TIgnoreKey> _source;

	private readonly AsynchronousChannel<TInputOutput> _destination;

	internal PipelineSpoolingTask(int taskIndex, QueryTaskGroupState groupState, QueryOperatorEnumerator<TInputOutput, TIgnoreKey> source, AsynchronousChannel<TInputOutput> destination)
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
		AsynchronousChannel<TInputOutput> destination = _destination;
		CancellationToken mergedCancellationToken = _groupState.CancellationState.MergedCancellationToken;
		while (source.MoveNext(ref currentElement, ref currentKey) && !mergedCancellationToken.IsCancellationRequested)
		{
			destination.Enqueue(currentElement);
		}
		destination.FlushBuffers();
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
