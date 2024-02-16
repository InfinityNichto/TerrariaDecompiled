using System.Collections.Generic;

namespace System.Linq;

internal sealed class CachingComparerWithChild<TElement, TKey> : CachingComparer<TElement, TKey>
{
	private readonly CachingComparer<TElement> _child;

	public CachingComparerWithChild(Func<TElement, TKey> keySelector, IComparer<TKey> comparer, bool descending, CachingComparer<TElement> child)
		: base(keySelector, comparer, descending)
	{
		_child = child;
	}

	internal override int Compare(TElement element, bool cacheLower)
	{
		TKey val = _keySelector(element);
		int num = (_descending ? _comparer.Compare(_lastKey, val) : _comparer.Compare(val, _lastKey));
		if (num == 0)
		{
			return _child.Compare(element, cacheLower);
		}
		if (cacheLower == num < 0)
		{
			_lastKey = val;
			_child.SetElement(element);
		}
		return num;
	}

	internal override void SetElement(TElement element)
	{
		base.SetElement(element);
		_child.SetElement(element);
	}
}
