namespace System.Linq.Parallel;

internal abstract class HashJoinOutputKeyBuilder<TLeftKey, TRightKey, TOutputKey>
{
	public abstract TOutputKey Combine(TLeftKey leftKey, TRightKey rightKey);
}
