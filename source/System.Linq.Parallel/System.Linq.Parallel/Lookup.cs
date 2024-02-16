using System.Collections;
using System.Collections.Generic;

namespace System.Linq.Parallel;

internal sealed class Lookup<TKey, TElement> : ILookup<TKey, TElement>, IEnumerable<IGrouping<TKey, TElement>>, IEnumerable
{
	private readonly IDictionary<TKey, IGrouping<TKey, TElement>> _dict;

	private readonly IEqualityComparer<TKey> _comparer;

	private IGrouping<TKey, TElement> _defaultKeyGrouping;

	public int Count
	{
		get
		{
			int num = _dict.Count;
			if (_defaultKeyGrouping != null)
			{
				num++;
			}
			return num;
		}
	}

	public IEnumerable<TElement> this[TKey key]
	{
		get
		{
			if (_comparer.Equals(key, default(TKey)))
			{
				if (_defaultKeyGrouping != null)
				{
					return _defaultKeyGrouping;
				}
				return Enumerable.Empty<TElement>();
			}
			if (_dict.TryGetValue(key, out var value))
			{
				return value;
			}
			return Enumerable.Empty<TElement>();
		}
	}

	internal Lookup(IEqualityComparer<TKey> comparer)
	{
		_comparer = comparer;
		_dict = new Dictionary<TKey, IGrouping<TKey, TElement>>(_comparer);
	}

	public bool Contains(TKey key)
	{
		if (_comparer.Equals(key, default(TKey)))
		{
			return _defaultKeyGrouping != null;
		}
		return _dict.ContainsKey(key);
	}

	internal void Add(IGrouping<TKey, TElement> grouping)
	{
		if (_comparer.Equals(grouping.Key, default(TKey)))
		{
			_defaultKeyGrouping = grouping;
		}
		else
		{
			_dict.Add(grouping.Key, grouping);
		}
	}

	public IEnumerator<IGrouping<TKey, TElement>> GetEnumerator()
	{
		foreach (IGrouping<TKey, TElement> value in _dict.Values)
		{
			yield return value;
		}
		if (_defaultKeyGrouping != null)
		{
			yield return _defaultKeyGrouping;
		}
	}

	IEnumerator IEnumerable.GetEnumerator()
	{
		return ((IEnumerable<IGrouping<TKey, TElement>>)this).GetEnumerator();
	}
}
