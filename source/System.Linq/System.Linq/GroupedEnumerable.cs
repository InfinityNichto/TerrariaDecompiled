using System.Collections;
using System.Collections.Generic;

namespace System.Linq;

internal sealed class GroupedEnumerable<TSource, TKey, TElement> : IIListProvider<IGrouping<TKey, TElement>>, IEnumerable<IGrouping<TKey, TElement>>, IEnumerable
{
	private readonly IEnumerable<TSource> _source;

	private readonly Func<TSource, TKey> _keySelector;

	private readonly Func<TSource, TElement> _elementSelector;

	private readonly IEqualityComparer<TKey> _comparer;

	public IGrouping<TKey, TElement>[] ToArray()
	{
		IIListProvider<IGrouping<TKey, TElement>> iIListProvider = Lookup<TKey, TElement>.Create(_source, _keySelector, _elementSelector, _comparer);
		return iIListProvider.ToArray();
	}

	public List<IGrouping<TKey, TElement>> ToList()
	{
		IIListProvider<IGrouping<TKey, TElement>> iIListProvider = Lookup<TKey, TElement>.Create(_source, _keySelector, _elementSelector, _comparer);
		return iIListProvider.ToList();
	}

	public int GetCount(bool onlyIfCheap)
	{
		if (!onlyIfCheap)
		{
			return Lookup<TKey, TElement>.Create(_source, _keySelector, _elementSelector, _comparer).Count;
		}
		return -1;
	}

	public GroupedEnumerable(IEnumerable<TSource> source, Func<TSource, TKey> keySelector, Func<TSource, TElement> elementSelector, IEqualityComparer<TKey> comparer)
	{
		if (source == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.source);
		}
		if (keySelector == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.keySelector);
		}
		if (elementSelector == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.elementSelector);
		}
		_source = source;
		_keySelector = keySelector;
		_elementSelector = elementSelector;
		_comparer = comparer;
	}

	public IEnumerator<IGrouping<TKey, TElement>> GetEnumerator()
	{
		return Lookup<TKey, TElement>.Create(_source, _keySelector, _elementSelector, _comparer).GetEnumerator();
	}

	IEnumerator IEnumerable.GetEnumerator()
	{
		return GetEnumerator();
	}
}
internal sealed class GroupedEnumerable<TSource, TKey> : IIListProvider<IGrouping<TKey, TSource>>, IEnumerable<IGrouping<TKey, TSource>>, IEnumerable
{
	private readonly IEnumerable<TSource> _source;

	private readonly Func<TSource, TKey> _keySelector;

	private readonly IEqualityComparer<TKey> _comparer;

	public IGrouping<TKey, TSource>[] ToArray()
	{
		IIListProvider<IGrouping<TKey, TSource>> iIListProvider = Lookup<TKey, TSource>.Create(_source, _keySelector, _comparer);
		return iIListProvider.ToArray();
	}

	public List<IGrouping<TKey, TSource>> ToList()
	{
		IIListProvider<IGrouping<TKey, TSource>> iIListProvider = Lookup<TKey, TSource>.Create(_source, _keySelector, _comparer);
		return iIListProvider.ToList();
	}

	public int GetCount(bool onlyIfCheap)
	{
		if (!onlyIfCheap)
		{
			return Lookup<TKey, TSource>.Create(_source, _keySelector, _comparer).Count;
		}
		return -1;
	}

	public GroupedEnumerable(IEnumerable<TSource> source, Func<TSource, TKey> keySelector, IEqualityComparer<TKey> comparer)
	{
		if (source == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.source);
		}
		if (keySelector == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.keySelector);
		}
		_source = source;
		_keySelector = keySelector;
		_comparer = comparer;
	}

	public IEnumerator<IGrouping<TKey, TSource>> GetEnumerator()
	{
		return Lookup<TKey, TSource>.Create(_source, _keySelector, _comparer).GetEnumerator();
	}

	IEnumerator IEnumerable.GetEnumerator()
	{
		return GetEnumerator();
	}
}
