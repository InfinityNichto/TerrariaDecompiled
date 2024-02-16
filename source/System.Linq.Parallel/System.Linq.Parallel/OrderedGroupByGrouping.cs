using System.Collections;
using System.Collections.Generic;

namespace System.Linq.Parallel;

internal sealed class OrderedGroupByGrouping<TGroupKey, TOrderKey, TElement> : IGrouping<TGroupKey, TElement>, IEnumerable<TElement>, IEnumerable
{
	private readonly TGroupKey _groupKey;

	private ListChunk<Pair<TOrderKey, TElement>> _values;

	private TElement[] _sortedValues;

	private readonly IComparer<TOrderKey> _orderComparer;

	TGroupKey IGrouping<TGroupKey, TElement>.Key => _groupKey;

	internal OrderedGroupByGrouping(TGroupKey groupKey, IComparer<TOrderKey> orderComparer)
	{
		_groupKey = groupKey;
		_values = new ListChunk<Pair<TOrderKey, TElement>>(2);
		_orderComparer = orderComparer;
	}

	IEnumerator<TElement> IEnumerable<TElement>.GetEnumerator()
	{
		return ((IEnumerable<TElement>)_sortedValues).GetEnumerator();
	}

	IEnumerator IEnumerable.GetEnumerator()
	{
		return ((IEnumerable<TElement>)this).GetEnumerator();
	}

	internal void Add(TElement value, TOrderKey orderKey)
	{
		_values.Add(new Pair<TOrderKey, TElement>(orderKey, value));
	}

	internal void DoneAdding()
	{
		int num = _values.Count;
		ListChunk<Pair<TOrderKey, TElement>> listChunk = _values;
		while ((listChunk = listChunk.Next) != null)
		{
			num += listChunk.Count;
		}
		TElement[] array = new TElement[num];
		TOrderKey[] array2 = new TOrderKey[num];
		int num2 = 0;
		foreach (Pair<TOrderKey, TElement> value in _values)
		{
			array2[num2] = value.First;
			array[num2] = value.Second;
			num2++;
		}
		Array.Sort(array2, array, _orderComparer);
		_sortedValues = array;
	}
}
