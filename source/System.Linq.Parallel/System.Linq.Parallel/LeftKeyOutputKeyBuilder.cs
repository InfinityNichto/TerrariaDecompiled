namespace System.Linq.Parallel;

internal sealed class LeftKeyOutputKeyBuilder<TLeftKey, TRightKey> : HashJoinOutputKeyBuilder<TLeftKey, TRightKey, TLeftKey>
{
	public override TLeftKey Combine(TLeftKey leftKey, TRightKey rightKey)
	{
		return leftKey;
	}
}
