using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading;

namespace System.Linq.Parallel;

internal abstract class OrderedGroupByQueryOperatorEnumerator<TSource, TGroupKey, TElement, TOrderKey> : QueryOperatorEnumerator<IGrouping<TGroupKey, TElement>, TOrderKey>
{
	private sealed class Mutables
	{
		internal HashLookup<Wrapper<TGroupKey>, GroupKeyData> _hashLookup;

		internal int _hashLookupIndex;
	}

	protected class GroupKeyData
	{
		internal TOrderKey _orderKey;

		internal OrderedGroupByGrouping<TGroupKey, TOrderKey, TElement> _grouping;

		internal GroupKeyData(TOrderKey orderKey, TGroupKey hashKey, IComparer<TOrderKey> orderComparer)
		{
			_orderKey = orderKey;
			_grouping = new OrderedGroupByGrouping<TGroupKey, TOrderKey, TElement>(hashKey, orderComparer);
		}
	}

	protected readonly QueryOperatorEnumerator<Pair<TSource, TGroupKey>, TOrderKey> _source;

	private readonly Func<TSource, TGroupKey> _keySelector;

	protected readonly IEqualityComparer<TGroupKey> _keyComparer;

	protected readonly IComparer<TOrderKey> _orderComparer;

	protected readonly CancellationToken _cancellationToken;

	private Mutables _mutables;

	protected OrderedGroupByQueryOperatorEnumerator(QueryOperatorEnumerator<Pair<TSource, TGroupKey>, TOrderKey> source, Func<TSource, TGroupKey> keySelector, IEqualityComparer<TGroupKey> keyComparer, IComparer<TOrderKey> orderComparer, CancellationToken cancellationToken)
	{
		_source = source;
		_keySelector = keySelector;
		_keyComparer = keyComparer;
		_orderComparer = orderComparer;
		_cancellationToken = cancellationToken;
	}

	internal override bool MoveNext([MaybeNullWhen(false)][AllowNull] ref IGrouping<TGroupKey, TElement> currentElement, [AllowNull] ref TOrderKey currentKey)
	{
		Mutables mutables = _mutables;
		if (mutables == null)
		{
			mutables = (_mutables = new Mutables());
			mutables._hashLookup = BuildHashLookup();
			mutables._hashLookupIndex = -1;
		}
		if (++mutables._hashLookupIndex < mutables._hashLookup.Count)
		{
			GroupKeyData value = mutables._hashLookup[mutables._hashLookupIndex].Value;
			currentElement = value._grouping;
			currentKey = value._orderKey;
			return true;
		}
		return false;
	}

	protected abstract HashLookup<Wrapper<TGroupKey>, GroupKeyData> BuildHashLookup();

	protected override void Dispose(bool disposing)
	{
		_source.Dispose();
	}
}
