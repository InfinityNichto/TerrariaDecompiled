using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;

namespace System.Linq;

[DebuggerDisplay("Count = {Count}")]
[DebuggerTypeProxy(typeof(SystemLinq_LookupDebugView<, >))]
public class Lookup<TKey, TElement> : IIListProvider<IGrouping<TKey, TElement>>, IEnumerable<IGrouping<TKey, TElement>>, IEnumerable, ILookup<TKey, TElement>
{
	private readonly IEqualityComparer<TKey> _comparer;

	private Grouping<TKey, TElement>[] _groupings;

	private Grouping<TKey, TElement> _lastGrouping;

	private int _count;

	public int Count => _count;

	public IEnumerable<TElement> this[TKey key]
	{
		get
		{
			Grouping<TKey, TElement> grouping = GetGrouping(key, create: false);
			IEnumerable<TElement> enumerable = grouping;
			return enumerable ?? Enumerable.Empty<TElement>();
		}
	}

	IGrouping<TKey, TElement>[] IIListProvider<IGrouping<TKey, TElement>>.ToArray()
	{
		IGrouping<TKey, TElement>[] array = new IGrouping<TKey, TElement>[_count];
		int num = 0;
		Grouping<TKey, TElement> grouping = _lastGrouping;
		if (grouping != null)
		{
			do
			{
				grouping = (Grouping<TKey, TElement>)(array[num] = grouping._next);
				num++;
			}
			while (grouping != _lastGrouping);
		}
		return array;
	}

	internal TResult[] ToArray<TResult>(Func<TKey, IEnumerable<TElement>, TResult> resultSelector)
	{
		TResult[] array = new TResult[_count];
		int num = 0;
		Grouping<TKey, TElement> grouping = _lastGrouping;
		if (grouping != null)
		{
			do
			{
				grouping = grouping._next;
				grouping.Trim();
				array[num] = resultSelector(grouping._key, grouping._elements);
				num++;
			}
			while (grouping != _lastGrouping);
		}
		return array;
	}

	List<IGrouping<TKey, TElement>> IIListProvider<IGrouping<TKey, TElement>>.ToList()
	{
		List<IGrouping<TKey, TElement>> list = new List<IGrouping<TKey, TElement>>(_count);
		Grouping<TKey, TElement> grouping = _lastGrouping;
		if (grouping != null)
		{
			do
			{
				grouping = grouping._next;
				list.Add(grouping);
			}
			while (grouping != _lastGrouping);
		}
		return list;
	}

	int IIListProvider<IGrouping<TKey, TElement>>.GetCount(bool onlyIfCheap)
	{
		return _count;
	}

	internal static Lookup<TKey, TElement> Create<TSource>(IEnumerable<TSource> source, Func<TSource, TKey> keySelector, Func<TSource, TElement> elementSelector, IEqualityComparer<TKey> comparer)
	{
		Lookup<TKey, TElement> lookup = new Lookup<TKey, TElement>(comparer);
		foreach (TSource item in source)
		{
			lookup.GetGrouping(keySelector(item), create: true).Add(elementSelector(item));
		}
		return lookup;
	}

	internal static Lookup<TKey, TElement> Create(IEnumerable<TElement> source, Func<TElement, TKey> keySelector, IEqualityComparer<TKey> comparer)
	{
		Lookup<TKey, TElement> lookup = new Lookup<TKey, TElement>(comparer);
		foreach (TElement item in source)
		{
			lookup.GetGrouping(keySelector(item), create: true).Add(item);
		}
		return lookup;
	}

	internal static Lookup<TKey, TElement> CreateForJoin(IEnumerable<TElement> source, Func<TElement, TKey> keySelector, IEqualityComparer<TKey> comparer)
	{
		Lookup<TKey, TElement> lookup = new Lookup<TKey, TElement>(comparer);
		foreach (TElement item in source)
		{
			TKey val = keySelector(item);
			if (val != null)
			{
				lookup.GetGrouping(val, create: true).Add(item);
			}
		}
		return lookup;
	}

	private Lookup(IEqualityComparer<TKey> comparer)
	{
		_comparer = comparer ?? EqualityComparer<TKey>.Default;
		_groupings = new Grouping<TKey, TElement>[7];
	}

	public bool Contains(TKey key)
	{
		return GetGrouping(key, create: false) != null;
	}

	public IEnumerator<IGrouping<TKey, TElement>> GetEnumerator()
	{
		Grouping<TKey, TElement> g = _lastGrouping;
		if (g != null)
		{
			do
			{
				g = g._next;
				yield return g;
			}
			while (g != _lastGrouping);
		}
	}

	internal List<TResult> ToList<TResult>(Func<TKey, IEnumerable<TElement>, TResult> resultSelector)
	{
		List<TResult> list = new List<TResult>(_count);
		Grouping<TKey, TElement> grouping = _lastGrouping;
		if (grouping != null)
		{
			do
			{
				grouping = grouping._next;
				grouping.Trim();
				list.Add(resultSelector(grouping._key, grouping._elements));
			}
			while (grouping != _lastGrouping);
		}
		return list;
	}

	public IEnumerable<TResult> ApplyResultSelector<TResult>(Func<TKey, IEnumerable<TElement>, TResult> resultSelector)
	{
		Grouping<TKey, TElement> g = _lastGrouping;
		if (g != null)
		{
			do
			{
				g = g._next;
				g.Trim();
				yield return resultSelector(g._key, g._elements);
			}
			while (g != _lastGrouping);
		}
	}

	IEnumerator IEnumerable.GetEnumerator()
	{
		return GetEnumerator();
	}

	private int InternalGetHashCode(TKey key)
	{
		if (key != null)
		{
			return _comparer.GetHashCode(key) & 0x7FFFFFFF;
		}
		return 0;
	}

	internal Grouping<TKey, TElement> GetGrouping(TKey key, bool create)
	{
		int num = InternalGetHashCode(key);
		for (Grouping<TKey, TElement> grouping = _groupings[num % _groupings.Length]; grouping != null; grouping = grouping._hashNext)
		{
			if (grouping._hashCode == num && _comparer.Equals(grouping._key, key))
			{
				return grouping;
			}
		}
		if (create)
		{
			if (_count == _groupings.Length)
			{
				Resize();
			}
			int num2 = num % _groupings.Length;
			Grouping<TKey, TElement> grouping2 = new Grouping<TKey, TElement>(key, num);
			grouping2._hashNext = _groupings[num2];
			_groupings[num2] = grouping2;
			if (_lastGrouping == null)
			{
				grouping2._next = grouping2;
			}
			else
			{
				grouping2._next = _lastGrouping._next;
				_lastGrouping._next = grouping2;
			}
			_lastGrouping = grouping2;
			_count++;
			return grouping2;
		}
		return null;
	}

	private void Resize()
	{
		int num = checked(_count * 2 + 1);
		Grouping<TKey, TElement>[] array = new Grouping<TKey, TElement>[num];
		Grouping<TKey, TElement> grouping = _lastGrouping;
		do
		{
			grouping = grouping._next;
			int num2 = grouping._hashCode % num;
			grouping._hashNext = array[num2];
			array[num2] = grouping;
		}
		while (grouping != _lastGrouping);
		_groupings = array;
	}
}
