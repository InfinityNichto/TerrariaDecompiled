using System.Collections;
using System.Collections.Generic;

namespace System.Linq;

internal sealed class GroupedResultEnumerable<TSource, TKey, TElement, TResult> : IIListProvider<TResult>, IEnumerable<TResult>, IEnumerable
{
	private readonly IEnumerable<TSource> _source;

	private readonly Func<TSource, TKey> _keySelector;

	private readonly Func<TSource, TElement> _elementSelector;

	private readonly IEqualityComparer<TKey> _comparer;

	private readonly Func<TKey, IEnumerable<TElement>, TResult> _resultSelector;

	public TResult[] ToArray()
	{
		return Lookup<TKey, TElement>.Create(_source, _keySelector, _elementSelector, _comparer).ToArray(_resultSelector);
	}

	public List<TResult> ToList()
	{
		return Lookup<TKey, TElement>.Create(_source, _keySelector, _elementSelector, _comparer).ToList(_resultSelector);
	}

	public int GetCount(bool onlyIfCheap)
	{
		if (!onlyIfCheap)
		{
			return Lookup<TKey, TElement>.Create(_source, _keySelector, _elementSelector, _comparer).Count;
		}
		return -1;
	}

	public GroupedResultEnumerable(IEnumerable<TSource> source, Func<TSource, TKey> keySelector, Func<TSource, TElement> elementSelector, Func<TKey, IEnumerable<TElement>, TResult> resultSelector, IEqualityComparer<TKey> comparer)
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
		if (resultSelector == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.resultSelector);
		}
		_source = source;
		_keySelector = keySelector;
		_elementSelector = elementSelector;
		_comparer = comparer;
		_resultSelector = resultSelector;
	}

	public IEnumerator<TResult> GetEnumerator()
	{
		Lookup<TKey, TElement> lookup = Lookup<TKey, TElement>.Create(_source, _keySelector, _elementSelector, _comparer);
		return lookup.ApplyResultSelector(_resultSelector).GetEnumerator();
	}

	IEnumerator IEnumerable.GetEnumerator()
	{
		return GetEnumerator();
	}
}
internal sealed class GroupedResultEnumerable<TSource, TKey, TResult> : IIListProvider<TResult>, IEnumerable<TResult>, IEnumerable
{
	private readonly IEnumerable<TSource> _source;

	private readonly Func<TSource, TKey> _keySelector;

	private readonly IEqualityComparer<TKey> _comparer;

	private readonly Func<TKey, IEnumerable<TSource>, TResult> _resultSelector;

	public TResult[] ToArray()
	{
		return Lookup<TKey, TSource>.Create(_source, _keySelector, _comparer).ToArray(_resultSelector);
	}

	public List<TResult> ToList()
	{
		return Lookup<TKey, TSource>.Create(_source, _keySelector, _comparer).ToList(_resultSelector);
	}

	public int GetCount(bool onlyIfCheap)
	{
		if (!onlyIfCheap)
		{
			return Lookup<TKey, TSource>.Create(_source, _keySelector, _comparer).Count;
		}
		return -1;
	}

	public GroupedResultEnumerable(IEnumerable<TSource> source, Func<TSource, TKey> keySelector, Func<TKey, IEnumerable<TSource>, TResult> resultSelector, IEqualityComparer<TKey> comparer)
	{
		if (source == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.source);
		}
		if (keySelector == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.keySelector);
		}
		if (resultSelector == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.resultSelector);
		}
		_source = source;
		_keySelector = keySelector;
		_resultSelector = resultSelector;
		_comparer = comparer;
	}

	public IEnumerator<TResult> GetEnumerator()
	{
		Lookup<TKey, TSource> lookup = Lookup<TKey, TSource>.Create(_source, _keySelector, _comparer);
		return lookup.ApplyResultSelector(_resultSelector).GetEnumerator();
	}

	IEnumerator IEnumerable.GetEnumerator()
	{
		return GetEnumerator();
	}
}
