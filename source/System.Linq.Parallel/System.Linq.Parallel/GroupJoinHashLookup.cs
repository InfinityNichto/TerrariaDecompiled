using System.Collections.Generic;

namespace System.Linq.Parallel;

internal abstract class GroupJoinHashLookup<THashKey, TElement, TBaseElement, TOrderKey> : HashJoinHashLookup<THashKey, IEnumerable<TElement>, TOrderKey>
{
	private readonly HashLookup<THashKey, TBaseElement> _base;

	protected abstract TOrderKey EmptyValueKey { get; }

	internal GroupJoinHashLookup(HashLookup<THashKey, TBaseElement> baseLookup)
	{
		_base = baseLookup;
	}

	public override bool TryGetValue(THashKey key, ref HashLookupValueList<IEnumerable<TElement>, TOrderKey> value)
	{
		Pair<IEnumerable<TElement>, TOrderKey> valueList = GetValueList(key);
		value = new HashLookupValueList<IEnumerable<TElement>, TOrderKey>(valueList.First, valueList.Second);
		return true;
	}

	private Pair<IEnumerable<TElement>, TOrderKey> GetValueList(THashKey key)
	{
		TBaseElement value = default(TBaseElement);
		if (_base.TryGetValue(key, ref value))
		{
			return CreateValuePair(value);
		}
		return new Pair<IEnumerable<TElement>, TOrderKey>(ParallelEnumerable.Empty<TElement>(), EmptyValueKey);
	}

	protected abstract Pair<IEnumerable<TElement>, TOrderKey> CreateValuePair(TBaseElement baseValue);
}
