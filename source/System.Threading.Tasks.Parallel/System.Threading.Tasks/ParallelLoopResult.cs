namespace System.Threading.Tasks;

public struct ParallelLoopResult
{
	internal bool _completed;

	internal long? _lowestBreakIteration;

	public bool IsCompleted => _completed;

	public long? LowestBreakIteration => _lowestBreakIteration;
}
