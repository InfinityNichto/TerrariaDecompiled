namespace System.Threading.Tasks;

internal sealed class ParallelLoopState64 : ParallelLoopState
{
	private readonly ParallelLoopStateFlags64 _sharedParallelStateFlags;

	private long _currentIteration;

	internal long CurrentIteration
	{
		get
		{
			return _currentIteration;
		}
		set
		{
			_currentIteration = value;
		}
	}

	internal override bool InternalShouldExitCurrentIteration => _sharedParallelStateFlags.ShouldExitLoop(CurrentIteration);

	internal override long? InternalLowestBreakIteration => _sharedParallelStateFlags.NullableLowestBreakIteration;

	internal ParallelLoopState64(ParallelLoopStateFlags64 sharedParallelStateFlags)
		: base(sharedParallelStateFlags)
	{
		_sharedParallelStateFlags = sharedParallelStateFlags;
	}

	internal override void InternalBreak()
	{
		ParallelLoopState.Break(CurrentIteration, _sharedParallelStateFlags);
	}
}
