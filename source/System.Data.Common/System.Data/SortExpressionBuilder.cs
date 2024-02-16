using System.Collections.Generic;

namespace System.Data;

internal sealed class SortExpressionBuilder<T> : IComparer<List<object>>
{
	private readonly LinkedList<Func<T, object>> _selectors = new LinkedList<Func<T, object>>();

	private readonly LinkedList<Comparison<object>> _comparers = new LinkedList<Comparison<object>>();

	private LinkedListNode<Func<T, object>> _currentSelector;

	private LinkedListNode<Comparison<object>> _currentComparer;

	internal int Count => _selectors.Count;

	internal void Add(Func<T, object> keySelector, Comparison<object> compare, bool isOrderBy)
	{
		if (isOrderBy)
		{
			_currentSelector = _selectors.AddFirst(keySelector);
			_currentComparer = _comparers.AddFirst(compare);
		}
		else
		{
			_currentSelector = _selectors.AddAfter(_currentSelector, keySelector);
			_currentComparer = _comparers.AddAfter(_currentComparer, compare);
		}
	}

	public List<object> Select(T row)
	{
		List<object> list = new List<object>();
		foreach (Func<T, object> selector in _selectors)
		{
			list.Add(selector(row));
		}
		return list;
	}

	public int Compare(List<object> a, List<object> b)
	{
		int num = 0;
		foreach (Comparison<object> comparer in _comparers)
		{
			int num2 = comparer(a[num], b[num]);
			if (num2 != 0)
			{
				return num2;
			}
			num++;
		}
		return 0;
	}

	internal SortExpressionBuilder<T> Clone()
	{
		SortExpressionBuilder<T> sortExpressionBuilder = new SortExpressionBuilder<T>();
		foreach (Func<T, object> selector in _selectors)
		{
			if (selector == _currentSelector.Value)
			{
				sortExpressionBuilder._currentSelector = sortExpressionBuilder._selectors.AddLast(selector);
			}
			else
			{
				sortExpressionBuilder._selectors.AddLast(selector);
			}
		}
		foreach (Comparison<object> comparer in _comparers)
		{
			if (comparer == _currentComparer.Value)
			{
				sortExpressionBuilder._currentComparer = sortExpressionBuilder._comparers.AddLast(comparer);
			}
			else
			{
				sortExpressionBuilder._comparers.AddLast(comparer);
			}
		}
		return sortExpressionBuilder;
	}

	internal SortExpressionBuilder<TResult> CloneCast<TResult>()
	{
		SortExpressionBuilder<TResult> sortExpressionBuilder = new SortExpressionBuilder<TResult>();
		foreach (Func<T, object> selector in _selectors)
		{
			if (selector == _currentSelector.Value)
			{
				sortExpressionBuilder._currentSelector = sortExpressionBuilder._selectors.AddLast((TResult r) => selector((T)(object)r));
			}
			else
			{
				sortExpressionBuilder._selectors.AddLast((TResult r) => selector((T)(object)r));
			}
		}
		foreach (Comparison<object> comparer in _comparers)
		{
			if (comparer == _currentComparer.Value)
			{
				sortExpressionBuilder._currentComparer = sortExpressionBuilder._comparers.AddLast(comparer);
			}
			else
			{
				sortExpressionBuilder._comparers.AddLast(comparer);
			}
		}
		return sortExpressionBuilder;
	}
}
