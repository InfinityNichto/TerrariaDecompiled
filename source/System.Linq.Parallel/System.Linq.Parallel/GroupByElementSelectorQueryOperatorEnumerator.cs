using System.Collections.Generic;
using System.Threading;

namespace System.Linq.Parallel;

internal sealed class GroupByElementSelectorQueryOperatorEnumerator<TSource, TGroupKey, TElement, TOrderKey> : GroupByQueryOperatorEnumerator<TSource, TGroupKey, TElement, TOrderKey>
{
	private readonly Func<TSource, TElement> _elementSelector;

	internal GroupByElementSelectorQueryOperatorEnumerator(QueryOperatorEnumerator<Pair<TSource, TGroupKey>, TOrderKey> source, IEqualityComparer<TGroupKey> keyComparer, Func<TSource, TElement> elementSelector, CancellationToken cancellationToken)
		: base(source, keyComparer, cancellationToken)
	{
		_elementSelector = elementSelector;
	}

	protected override HashLookup<Wrapper<TGroupKey>, ListChunk<TElement>> BuildHashLookup()
	{
		HashLookup<Wrapper<TGroupKey>, ListChunk<TElement>> hashLookup = new HashLookup<Wrapper<TGroupKey>, ListChunk<TElement>>(new WrapperEqualityComparer<TGroupKey>(_keyComparer));
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
			ListChunk<TElement> value = null;
			if (!hashLookup.TryGetValue(key, ref value))
			{
				value = new ListChunk<TElement>(2);
				hashLookup.Add(key, value);
			}
			value.Add(_elementSelector(currentElement.First));
		}
		return hashLookup;
	}
}
