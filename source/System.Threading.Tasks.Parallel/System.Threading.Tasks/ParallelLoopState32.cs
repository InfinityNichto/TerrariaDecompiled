namespace System.Threading.Tasks;

internal sealed class ParallelLoopState32 : ParallelLoopState
{
	private readonly ParallelLoopStateFlags32 _sharedParallelStateFlags;

	private int _currentIteration;

	internal int CurrentIteration
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

	internal ParallelLoopState32(ParallelLoopStateFlags32 sharedParallelStateFlags)
		: base(sharedParallelStateFlags)
	{
		_sharedParallelStateFlags = sharedParallelStateFlags;
	}

	internal override void InternalBreak()
	{
		ParallelLoopState.Break(CurrentIteration, _sharedParallelStateFlags);
	}
}
