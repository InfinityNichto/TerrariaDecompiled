namespace System.Threading.Tasks.Dataflow.Internal;

internal sealed class BoundingStateWithPostponedAndTask<TInput> : BoundingStateWithPostponed<TInput>
{
	internal Task TaskForInputProcessing;

	internal BoundingStateWithPostponedAndTask(int boundedCapacity)
		: base(boundedCapacity)
	{
	}
}
