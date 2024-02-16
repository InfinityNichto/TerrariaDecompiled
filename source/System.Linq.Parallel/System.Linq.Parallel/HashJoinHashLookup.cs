namespace System.Linq.Parallel;

internal abstract class HashJoinHashLookup<THashKey, TElement, TOrderKey>
{
	public abstract bool TryGetValue(THashKey key, ref HashLookupValueList<TElement, TOrderKey> value);
}
