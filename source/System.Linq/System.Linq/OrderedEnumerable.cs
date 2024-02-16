using System.Collections;
using System.Collections.Generic;

namespace System.Linq;

internal abstract class OrderedEnumerable<TElement> : IPartition<TElement>, IIListProvider<TElement>, IEnumerable<TElement>, IEnumerable, IOrderedEnumerable<TElement>
{
	internal IEnumerable<TElement> _source;

	public TElement[] ToArray()
	{
		Buffer<TElement> buffer = new Buffer<TElement>(_source);
		int count = buffer._count;
		if (count == 0)
		{
			return buffer._items;
		}
		TElement[] array = new TElement[count];
		int[] array2 = SortedMap(buffer);
		for (int i = 0; i != array.Length; i++)
		{
			array[i] = buffer._items[array2[i]];
		}
		return array;
	}

	public List<TElement> ToList()
	{
		Buffer<TElement> buffer = new Buffer<TElement>(_source);
		int count = buffer._count;
		List<TElement> list = new List<TElement>(count);
		if (count > 0)
		{
			int[] array = SortedMap(buffer);
			for (int i = 0; i != count; i++)
			{
				list.Add(buffer._items[array[i]]);
			}
		}
		return list;
	}

	public int GetCount(bool onlyIfCheap)
	{
		if (_source is IIListProvider<TElement> iIListProvider)
		{
			return iIListProvider.GetCount(onlyIfCheap);
		}
		if (onlyIfCheap && !(_source is ICollection<TElement>) && !(_source is ICollection))
		{
			return -1;
		}
		return _source.Count();
	}

	internal TElement[] ToArray(int minIdx, int maxIdx)
	{
		Buffer<TElement> buffer = new Buffer<TElement>(_source);
		int count = buffer._count;
		if (count <= minIdx)
		{
			return Array.Empty<TElement>();
		}
		if (count <= maxIdx)
		{
			maxIdx = count - 1;
		}
		if (minIdx == maxIdx)
		{
			return new TElement[1] { GetEnumerableSorter().ElementAt(buffer._items, count, minIdx) };
		}
		int[] array = SortedMap(buffer, minIdx, maxIdx);
		TElement[] array2 = new TElement[maxIdx - minIdx + 1];
		int num = 0;
		while (minIdx <= maxIdx)
		{
			array2[num] = buffer._items[array[minIdx]];
			num++;
			minIdx++;
		}
		return array2;
	}

	internal List<TElement> ToList(int minIdx, int maxIdx)
	{
		Buffer<TElement> buffer = new Buffer<TElement>(_source);
		int count = buffer._count;
		if (count <= minIdx)
		{
			return new List<TElement>();
		}
		if (count <= maxIdx)
		{
			maxIdx = count - 1;
		}
		if (minIdx == maxIdx)
		{
			return new List<TElement>(1) { GetEnumerableSorter().ElementAt(buffer._items, count, minIdx) };
		}
		int[] array = SortedMap(buffer, minIdx, maxIdx);
		List<TElement> list = new List<TElement>(maxIdx - minIdx + 1);
		while (minIdx <= maxIdx)
		{
			list.Add(buffer._items[array[minIdx]]);
			minIdx++;
		}
		return list;
	}

	internal int GetCount(int minIdx, int maxIdx, bool onlyIfCheap)
	{
		int count = GetCount(onlyIfCheap);
		if (count <= 0)
		{
			return count;
		}
		if (count <= minIdx)
		{
			return 0;
		}
		return ((count <= maxIdx) ? count : (maxIdx + 1)) - minIdx;
	}

	public IPartition<TElement> Skip(int count)
	{
		return new OrderedPartition<TElement>(this, count, int.MaxValue);
	}

	public IPartition<TElement> Take(int count)
	{
		return new OrderedPartition<TElement>(this, 0, count - 1);
	}

	public TElement TryGetElementAt(int index, out bool found)
	{
		if (index == 0)
		{
			return TryGetFirst(out found);
		}
		if (index > 0)
		{
			Buffer<TElement> buffer = new Buffer<TElement>(_source);
			int count = buffer._count;
			if (index < count)
			{
				found = true;
				return GetEnumerableSorter().ElementAt(buffer._items, count, index);
			}
		}
		found = false;
		return default(TElement);
	}

	public TElement TryGetFirst(out bool found)
	{
		CachingComparer<TElement> comparer = GetComparer();
		using IEnumerator<TElement> enumerator = _source.GetEnumerator();
		if (!enumerator.MoveNext())
		{
			found = false;
			return default(TElement);
		}
		TElement val = enumerator.Current;
		comparer.SetElement(val);
		while (enumerator.MoveNext())
		{
			TElement current = enumerator.Current;
			if (comparer.Compare(current, cacheLower: true) < 0)
			{
				val = current;
			}
		}
		found = true;
		return val;
	}

	public TElement TryGetLast(out bool found)
	{
		using IEnumerator<TElement> enumerator = _source.GetEnumerator();
		if (!enumerator.MoveNext())
		{
			found = false;
			return default(TElement);
		}
		CachingComparer<TElement> comparer = GetComparer();
		TElement val = enumerator.Current;
		comparer.SetElement(val);
		while (enumerator.MoveNext())
		{
			TElement current = enumerator.Current;
			if (comparer.Compare(current, cacheLower: false) >= 0)
			{
				val = current;
			}
		}
		found = true;
		return val;
	}

	public TElement TryGetLast(int minIdx, int maxIdx, out bool found)
	{
		Buffer<TElement> buffer = new Buffer<TElement>(_source);
		int count = buffer._count;
		if (minIdx >= count)
		{
			found = false;
			return default(TElement);
		}
		found = true;
		if (maxIdx >= count - 1)
		{
			return Last(buffer);
		}
		return GetEnumerableSorter().ElementAt(buffer._items, count, maxIdx);
	}

	private TElement Last(Buffer<TElement> buffer)
	{
		CachingComparer<TElement> comparer = GetComparer();
		TElement[] items = buffer._items;
		int count = buffer._count;
		TElement val = items[0];
		comparer.SetElement(val);
		for (int i = 1; i != count; i++)
		{
			TElement val2 = items[i];
			if (comparer.Compare(val2, cacheLower: false) >= 0)
			{
				val = val2;
			}
		}
		return val;
	}

	protected OrderedEnumerable(IEnumerable<TElement> source)
	{
		_source = source;
	}

	private int[] SortedMap(Buffer<TElement> buffer)
	{
		return GetEnumerableSorter().Sort(buffer._items, buffer._count);
	}

	private int[] SortedMap(Buffer<TElement> buffer, int minIdx, int maxIdx)
	{
		return GetEnumerableSorter().Sort(buffer._items, buffer._count, minIdx, maxIdx);
	}

	public IEnumerator<TElement> GetEnumerator()
	{
		Buffer<TElement> buffer = new Buffer<TElement>(_source);
		if (buffer._count > 0)
		{
			int[] map = SortedMap(buffer);
			for (int i = 0; i < buffer._count; i++)
			{
				yield return buffer._items[map[i]];
			}
		}
	}

	internal IEnumerator<TElement> GetEnumerator(int minIdx, int maxIdx)
	{
		Buffer<TElement> buffer = new Buffer<TElement>(_source);
		int count = buffer._count;
		if (count <= minIdx)
		{
			yield break;
		}
		if (count <= maxIdx)
		{
			maxIdx = count - 1;
		}
		if (minIdx == maxIdx)
		{
			yield return GetEnumerableSorter().ElementAt(buffer._items, count, minIdx);
			yield break;
		}
		int[] map = SortedMap(buffer, minIdx, maxIdx);
		while (minIdx <= maxIdx)
		{
			yield return buffer._items[map[minIdx]];
			int num = minIdx + 1;
			minIdx = num;
		}
	}

	private EnumerableSorter<TElement> GetEnumerableSorter()
	{
		return GetEnumerableSorter(null);
	}

	internal abstract EnumerableSorter<TElement> GetEnumerableSorter(EnumerableSorter<TElement> next);

	private CachingComparer<TElement> GetComparer()
	{
		return GetComparer(null);
	}

	internal abstract CachingComparer<TElement> GetComparer(CachingComparer<TElement> childComparer);

	IEnumerator IEnumerable.GetEnumerator()
	{
		return GetEnumerator();
	}

	IOrderedEnumerable<TElement> IOrderedEnumerable<TElement>.CreateOrderedEnumerable<TKey>(Func<TElement, TKey> keySelector, IComparer<TKey> comparer, bool descending)
	{
		return new OrderedEnumerable<TElement, TKey>(_source, keySelector, comparer, descending, this);
	}

	public TElement TryGetLast(Func<TElement, bool> predicate, out bool found)
	{
		CachingComparer<TElement> comparer = GetComparer();
		using IEnumerator<TElement> enumerator = _source.GetEnumerator();
		while (enumerator.MoveNext())
		{
			TElement val = enumerator.Current;
			if (!predicate(val))
			{
				continue;
			}
			comparer.SetElement(val);
			while (enumerator.MoveNext())
			{
				TElement current = enumerator.Current;
				if (predicate(current) && comparer.Compare(current, cacheLower: false) >= 0)
				{
					val = current;
				}
			}
			found = true;
			return val;
		}
		found = false;
		return default(TElement);
	}
}
internal sealed class OrderedEnumerable<TElement, TKey> : OrderedEnumerable<TElement>
{
	private readonly OrderedEnumerable<TElement> _parent;

	private readonly Func<TElement, TKey> _keySelector;

	private readonly IComparer<TKey> _comparer;

	private readonly bool _descending;

	internal OrderedEnumerable(IEnumerable<TElement> source, Func<TElement, TKey> keySelector, IComparer<TKey> comparer, bool descending, OrderedEnumerable<TElement> parent)
		: base(source)
	{
		if (source == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.source);
		}
		if (keySelector == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.keySelector);
		}
		_parent = parent;
		_keySelector = keySelector;
		_comparer = comparer ?? Comparer<TKey>.Default;
		_descending = descending;
	}

	internal override EnumerableSorter<TElement> GetEnumerableSorter(EnumerableSorter<TElement> next)
	{
		IComparer<TKey> comparer = _comparer;
		if (typeof(TKey) == typeof(string) && comparer == Comparer<string>.Default)
		{
			comparer = (IComparer<TKey>)StringComparer.CurrentCulture;
		}
		EnumerableSorter<TElement> enumerableSorter = new EnumerableSorter<TElement, TKey>(_keySelector, comparer, _descending, next);
		if (_parent != null)
		{
			enumerableSorter = _parent.GetEnumerableSorter(enumerableSorter);
		}
		return enumerableSorter;
	}

	internal override CachingComparer<TElement> GetComparer(CachingComparer<TElement> childComparer)
	{
		CachingComparer<TElement> cachingComparer = ((childComparer == null) ? new CachingComparer<TElement, TKey>(_keySelector, _comparer, _descending) : new CachingComparerWithChild<TElement, TKey>(_keySelector, _comparer, _descending, childComparer));
		if (_parent == null)
		{
			return cachingComparer;
		}
		return _parent.GetComparer(cachingComparer);
	}
}
