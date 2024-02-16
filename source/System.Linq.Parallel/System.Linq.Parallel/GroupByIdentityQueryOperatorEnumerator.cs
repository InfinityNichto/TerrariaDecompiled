using System.Collections.Generic;
using System.Threading;

namespace System.Linq.Parallel;

internal sealed class GroupByIdentityQueryOperatorEnumerator<TSource, TGroupKey, TOrderKey> : GroupByQueryOperatorEnumerator<TSource, TGroupKey, TSource, TOrderKey>
{
	internal GroupByIdentityQueryOperatorEnumerator(QueryOperatorEnumerator<Pair<TSource, TGroupKey>, TOrderKey> source, IEqualityComparer<TGroupKey> keyComparer, CancellationToken cancellationToken)
		: base(source, keyComparer, cancellationToken)
	{
	}

	protected override HashLookup<Wrapper<TGroupKey>, ListChunk<TSource>> BuildHashLookup()
	{
		HashLookup<Wrapper<TGroupKey>, ListChunk<TSource>> hashLookup = new HashLookup<Wrapper<TGroupKey>, ListChunk<TSource>>(new WrapperEqualityComparer<TGroupKey>(_keyComparer));
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
			ListChunk<TSource> value = null;
			if (!hashLookup.TryGetValue(key, ref value))
			{
				value = new ListChunk<TSource>(2);
				hashLookup.Add(key, value);
			}
			value.Add(currentElement.First);
		}
		return hashLookup;
	}
}
