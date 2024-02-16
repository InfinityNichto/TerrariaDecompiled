namespace System.Linq.Parallel;

internal sealed class PairOutputKeyBuilder<TLeftKey, TRightKey> : HashJoinOutputKeyBuilder<TLeftKey, TRightKey, Pair<TLeftKey, TRightKey>>
{
	public override Pair<TLeftKey, TRightKey> Combine(TLeftKey leftKey, TRightKey rightKey)
	{
		return new Pair<TLeftKey, TRightKey>(leftKey, rightKey);
	}
}
