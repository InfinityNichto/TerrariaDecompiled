using System.Collections;
using System.Collections.Generic;

namespace System.Linq.Parallel;

internal abstract class MergeEnumerator<TInputOutput> : IEnumerator<TInputOutput>, IEnumerator, IDisposable
{
	protected QueryTaskGroupState _taskGroupState;

	public abstract TInputOutput Current { get; }

	object IEnumerator.Current => ((IEnumerator<TInputOutput>)this).Current;

	protected MergeEnumerator(QueryTaskGroupState taskGroupState)
	{
		_taskGroupState = taskGroupState;
	}

	public abstract bool MoveNext();

	public virtual void Reset()
	{
	}

	public virtual void Dispose()
	{
		if (!_taskGroupState.IsAlreadyEnded)
		{
			_taskGroupState.QueryEnd(userInitiatedDispose: true);
		}
	}
}
