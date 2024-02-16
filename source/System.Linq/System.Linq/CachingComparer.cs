using System.Collections.Generic;

namespace System.Linq;

internal abstract class CachingComparer<TElement>
{
	internal abstract int Compare(TElement element, bool cacheLower);

	internal abstract void SetElement(TElement element);
}
internal class CachingComparer<TElement, TKey> : CachingComparer<TElement>
{
	protected readonly Func<TElement, TKey> _keySelector;

	protected readonly IComparer<TKey> _comparer;

	protected readonly bool _descending;

	protected TKey _lastKey;

	public CachingComparer(Func<TElement, TKey> keySelector, IComparer<TKey> comparer, bool descending)
	{
		_keySelector = keySelector;
		_comparer = comparer;
		_descending = descending;
	}

	internal override int Compare(TElement element, bool cacheLower)
	{
		TKey val = _keySelector(element);
		int num = (_descending ? _comparer.Compare(_lastKey, val) : _comparer.Compare(val, _lastKey));
		if (cacheLower == num < 0)
		{
			_lastKey = val;
		}
		return num;
	}

	internal override void SetElement(TElement element)
	{
		_lastKey = _keySelector(element);
	}
}
