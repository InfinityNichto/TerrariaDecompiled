using System.Collections.Generic;
using System.Threading;

namespace System.Linq.Parallel;

internal sealed class GroupByQueryOperator<TSource, TGroupKey, TElement> : UnaryQueryOperator<TSource, IGrouping<TGroupKey, TElement>>
{
	private readonly Func<TSource, TGroupKey> _keySelector;

	private readonly Func<TSource, TElement> _elementSelector;

	private readonly IEqualityComparer<TGroupKey> _keyComparer;

	internal override bool LimitsParallelism => false;

	internal GroupByQueryOperator(IEnumerable<TSource> child, Func<TSource, TGroupKey> keySelector, Func<TSource, TElement> elementSelector, IEqualityComparer<TGroupKey> keyComparer)
		: base(child)
	{
		_keySelector = keySelector;
		_elementSelector = elementSelector;
		_keyComparer = keyComparer;
		SetOrdinalIndexState(OrdinalIndexState.Shuffled);
	}

	internal override void WrapPartitionedStream<TKey>(PartitionedStream<TSource, TKey> inputStream, IPartitionedStreamRecipient<IGrouping<TGroupKey, TElement>> recipient, bool preferStriping, QuerySettings settings)
	{
		if (base.Child.OutputOrdered)
		{
			WrapPartitionedStreamHelperOrdered(ExchangeUtilities.HashRepartitionOrdered(inputStream, _keySelector, _keyComparer, null, settings.CancellationState.MergedCancellationToken), recipient, settings.CancellationState.MergedCancellationToken);
		}
		else
		{
			WrapPartitionedStreamHelper<TKey, int>(ExchangeUtilities.HashRepartition(inputStream, _keySelector, _keyComparer, null, settings.CancellationState.MergedCancellationToken), recipient, settings.CancellationState.MergedCancellationToken);
		}
	}

	private void WrapPartitionedStreamHelper<TIgnoreKey, TKey>(PartitionedStream<Pair<TSource, TGroupKey>, TKey> hashStream, IPartitionedStreamRecipient<IGrouping<TGroupKey, TElement>> recipient, CancellationToken cancellationToken)
	{
		int partitionCount = hashStream.PartitionCount;
		PartitionedStream<IGrouping<TGroupKey, TElement>, TKey> partitionedStream = new PartitionedStream<IGrouping<TGroupKey, TElement>, TKey>(partitionCount, hashStream.KeyComparer, OrdinalIndexState.Shuffled);
		for (int i = 0; i < partitionCount; i++)
		{
			if (_elementSelector == null)
			{
				GroupByIdentityQueryOperatorEnumerator<TSource, TGroupKey, TKey> groupByIdentityQueryOperatorEnumerator = new GroupByIdentityQueryOperatorEnumerator<TSource, TGroupKey, TKey>(hashStream[i], _keyComparer, cancellationToken);
				partitionedStream[i] = (QueryOperatorEnumerator<IGrouping<TGroupKey, TElement>, TKey>)(object)groupByIdentityQueryOperatorEnumerator;
			}
			else
			{
				partitionedStream[i] = new GroupByElementSelectorQueryOperatorEnumerator<TSource, TGroupKey, TElement, TKey>(hashStream[i], _keyComparer, _elementSelector, cancellationToken);
			}
		}
		recipient.Receive(partitionedStream);
	}

	private void WrapPartitionedStreamHelperOrdered<TKey>(PartitionedStream<Pair<TSource, TGroupKey>, TKey> hashStream, IPartitionedStreamRecipient<IGrouping<TGroupKey, TElement>> recipient, CancellationToken cancellationToken)
	{
		int partitionCount = hashStream.PartitionCount;
		PartitionedStream<IGrouping<TGroupKey, TElement>, TKey> partitionedStream = new PartitionedStream<IGrouping<TGroupKey, TElement>, TKey>(partitionCount, hashStream.KeyComparer, OrdinalIndexState.Shuffled);
		IComparer<TKey> keyComparer = hashStream.KeyComparer;
		for (int i = 0; i < partitionCount; i++)
		{
			if (_elementSelector == null)
			{
				OrderedGroupByIdentityQueryOperatorEnumerator<TSource, TGroupKey, TKey> orderedGroupByIdentityQueryOperatorEnumerator = new OrderedGroupByIdentityQueryOperatorEnumerator<TSource, TGroupKey, TKey>(hashStream[i], _keySelector, _keyComparer, keyComparer, cancellationToken);
				partitionedStream[i] = (QueryOperatorEnumerator<IGrouping<TGroupKey, TElement>, TKey>)(object)orderedGroupByIdentityQueryOperatorEnumerator;
			}
			else
			{
				partitionedStream[i] = new OrderedGroupByElementSelectorQueryOperatorEnumerator<TSource, TGroupKey, TElement, TKey>(hashStream[i], _keySelector, _elementSelector, _keyComparer, keyComparer, cancellationToken);
			}
		}
		recipient.Receive(partitionedStream);
	}

	internal override QueryResults<IGrouping<TGroupKey, TElement>> Open(QuerySettings settings, bool preferStriping)
	{
		QueryResults<TSource> childQueryResults = base.Child.Open(settings, preferStriping: false);
		return new UnaryQueryOperatorResults(childQueryResults, this, settings, preferStriping: false);
	}

	internal override IEnumerable<IGrouping<TGroupKey, TElement>> AsSequentialQuery(CancellationToken token)
	{
		IEnumerable<TSource> source = CancellableEnumerable.Wrap(base.Child.AsSequentialQuery(token), token);
		if (_elementSelector == null)
		{
			return (IEnumerable<IGrouping<TGroupKey, TElement>>)source.GroupBy(_keySelector, _keyComparer);
		}
		return source.GroupBy(_keySelector, _elementSelector, _keyComparer);
	}
}
