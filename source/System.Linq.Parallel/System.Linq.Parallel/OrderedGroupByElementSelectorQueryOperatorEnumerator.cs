using System.Collections.Generic;
using System.Threading;

namespace System.Linq.Parallel;

internal sealed class OrderedGroupByElementSelectorQueryOperatorEnumerator<TSource, TGroupKey, TElement, TOrderKey> : OrderedGroupByQueryOperatorEnumerator<TSource, TGroupKey, TElement, TOrderKey>
{
	private readonly Func<TSource, TElement> _elementSelector;

	internal OrderedGroupByElementSelectorQueryOperatorEnumerator(QueryOperatorEnumerator<Pair<TSource, TGroupKey>, TOrderKey> source, Func<TSource, TGroupKey> keySelector, Func<TSource, TElement> elementSelector, IEqualityComparer<TGroupKey> keyComparer, IComparer<TOrderKey> orderComparer, CancellationToken cancellationToken)
		: base(source, keySelector, keyComparer, orderComparer, cancellationToken)
	{
		_elementSelector = elementSelector;
	}

	protected override HashLookup<Wrapper<TGroupKey>, GroupKeyData> BuildHashLookup()
	{
		HashLookup<Wrapper<TGroupKey>, GroupKeyData> hashLookup = new HashLookup<Wrapper<TGroupKey>, GroupKeyData>(new WrapperEqualityComparer<TGroupKey>(_keyComparer));
		Pair<TSource, TGroupKey> currentElement = default(Pair<TSource, TGroupKey>);
		TOrderKey currentKey = default(TOrderKey);
		int num = 0;
		while (_source.MoveNext(ref currentElement, ref currentKey))
		{
			if ((num++ & 0x3F) == 0)
			{
				_cancellationToken.ThrowIfCancellationRequested();
			}
			Wrapper<TGroupKey> key = new Wrapper<TGroupKey>(currentElement.Second);
			GroupKeyData value = null;
			if (hashLookup.TryGetValue(key, ref value))
			{
				if (_orderComparer.Compare(currentKey, value._orderKey) < 0)
				{
					value._orderKey = currentKey;
				}
			}
			else
			{
				value = new GroupKeyData(currentKey, key.Value, _orderComparer);
				hashLookup.Add(key, value);
			}
			value._grouping.Add(_elementSelector(currentElement.First), currentKey);
		}
		for (int i = 0; i < hashLookup.Count; i++)
		{
			hashLookup[i].Value._grouping.DoneAdding();
		}
		return hashLookup;
	}
}
