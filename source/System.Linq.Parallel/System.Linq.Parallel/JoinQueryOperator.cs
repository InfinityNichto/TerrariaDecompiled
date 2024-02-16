using System.Collections.Generic;
using System.Threading;

namespace System.Linq.Parallel;

internal sealed class JoinQueryOperator<TLeftInput, TRightInput, TKey, TOutput> : BinaryQueryOperator<TLeftInput, TRightInput, TOutput>
{
	private readonly Func<TLeftInput, TKey> _leftKeySelector;

	private readonly Func<TRightInput, TKey> _rightKeySelector;

	private readonly Func<TLeftInput, TRightInput, TOutput> _resultSelector;

	private readonly IEqualityComparer<TKey> _keyComparer;

	internal override bool LimitsParallelism => false;

	internal JoinQueryOperator(ParallelQuery<TLeftInput> left, ParallelQuery<TRightInput> right, Func<TLeftInput, TKey> leftKeySelector, Func<TRightInput, TKey> rightKeySelector, Func<TLeftInput, TRightInput, TOutput> resultSelector, IEqualityComparer<TKey> keyComparer)
		: base(left, right)
	{
		_leftKeySelector = leftKeySelector;
		_rightKeySelector = rightKeySelector;
		_resultSelector = resultSelector;
		_keyComparer = keyComparer;
		_outputOrdered = base.LeftChild.OutputOrdered;
		SetOrdinalIndex(OrdinalIndexState.Shuffled);
	}

	public override void WrapPartitionedStream<TLeftKey, TRightKey>(PartitionedStream<TLeftInput, TLeftKey> leftStream, PartitionedStream<TRightInput, TRightKey> rightStream, IPartitionedStreamRecipient<TOutput> outputRecipient, bool preferStriping, QuerySettings settings)
	{
		if (base.LeftChild.OutputOrdered)
		{
			if (base.LeftChild.OrdinalIndexState.IsWorseThan(OrdinalIndexState.Increasing))
			{
				PartitionedStream<TLeftInput, int> partitionedStream = QueryOperator<TLeftInput>.ExecuteAndCollectResults(leftStream, leftStream.PartitionCount, base.OutputOrdered, preferStriping, settings).GetPartitionedStream();
				WrapPartitionedStreamHelper(ExchangeUtilities.HashRepartitionOrdered(partitionedStream, _leftKeySelector, _keyComparer, null, settings.CancellationState.MergedCancellationToken), rightStream, outputRecipient, settings.CancellationState.MergedCancellationToken);
			}
			else
			{
				WrapPartitionedStreamHelper(ExchangeUtilities.HashRepartitionOrdered(leftStream, _leftKeySelector, _keyComparer, null, settings.CancellationState.MergedCancellationToken), rightStream, outputRecipient, settings.CancellationState.MergedCancellationToken);
			}
		}
		else
		{
			WrapPartitionedStreamHelper(ExchangeUtilities.HashRepartition(leftStream, _leftKeySelector, _keyComparer, null, settings.CancellationState.MergedCancellationToken), rightStream, outputRecipient, settings.CancellationState.MergedCancellationToken);
		}
	}

	private void WrapPartitionedStreamHelper<TLeftKey, TRightKey>(PartitionedStream<Pair<TLeftInput, TKey>, TLeftKey> leftHashStream, PartitionedStream<TRightInput, TRightKey> rightPartitionedStream, IPartitionedStreamRecipient<TOutput> outputRecipient, CancellationToken cancellationToken)
	{
		if (base.RightChild.OutputOrdered && base.LeftChild.OutputOrdered)
		{
			PairOutputKeyBuilder<TLeftKey, TRightKey> outputKeyBuilder = new PairOutputKeyBuilder<TLeftKey, TRightKey>();
			IComparer<Pair<TLeftKey, TRightKey>> outputKeyComparer = new PairComparer<TLeftKey, TRightKey>(leftHashStream.KeyComparer, rightPartitionedStream.KeyComparer);
			WrapPartitionedStreamHelper(leftHashStream, ExchangeUtilities.HashRepartitionOrdered(rightPartitionedStream, _rightKeySelector, _keyComparer, null, cancellationToken), outputKeyBuilder, outputKeyComparer, outputRecipient, cancellationToken);
		}
		else
		{
			LeftKeyOutputKeyBuilder<TLeftKey, int> outputKeyBuilder2 = new LeftKeyOutputKeyBuilder<TLeftKey, int>();
			WrapPartitionedStreamHelper(leftHashStream, ExchangeUtilities.HashRepartition(rightPartitionedStream, _rightKeySelector, _keyComparer, null, cancellationToken), outputKeyBuilder2, leftHashStream.KeyComparer, outputRecipient, cancellationToken);
		}
	}

	private void WrapPartitionedStreamHelper<TLeftKey, TRightKey, TOutputKey>(PartitionedStream<Pair<TLeftInput, TKey>, TLeftKey> leftHashStream, PartitionedStream<Pair<TRightInput, TKey>, TRightKey> rightHashStream, HashJoinOutputKeyBuilder<TLeftKey, TRightKey, TOutputKey> outputKeyBuilder, IComparer<TOutputKey> outputKeyComparer, IPartitionedStreamRecipient<TOutput> outputRecipient, CancellationToken cancellationToken)
	{
		int partitionCount = leftHashStream.PartitionCount;
		PartitionedStream<TOutput, TOutputKey> partitionedStream = new PartitionedStream<TOutput, TOutputKey>(partitionCount, outputKeyComparer, OrdinalIndexState);
		for (int i = 0; i < partitionCount; i++)
		{
			JoinHashLookupBuilder<TRightInput, TRightKey, TKey> rightLookupBuilder = new JoinHashLookupBuilder<TRightInput, TRightKey, TKey>(rightHashStream[i], _keyComparer);
			partitionedStream[i] = new HashJoinQueryOperatorEnumerator<TLeftInput, TLeftKey, TRightInput, TRightKey, TKey, TOutput, TOutputKey>(leftHashStream[i], rightLookupBuilder, _resultSelector, outputKeyBuilder, cancellationToken);
		}
		outputRecipient.Receive(partitionedStream);
	}

	internal override QueryResults<TOutput> Open(QuerySettings settings, bool preferStriping)
	{
		QueryResults<TLeftInput> leftChildQueryResults = base.LeftChild.Open(settings, preferStriping: false);
		QueryResults<TRightInput> rightChildQueryResults = base.RightChild.Open(settings, preferStriping: false);
		return new BinaryQueryOperatorResults(leftChildQueryResults, rightChildQueryResults, this, settings, preferStriping: false);
	}

	internal override IEnumerable<TOutput> AsSequentialQuery(CancellationToken token)
	{
		IEnumerable<TLeftInput> outer = CancellableEnumerable.Wrap(base.LeftChild.AsSequentialQuery(token), token);
		IEnumerable<TRightInput> inner = CancellableEnumerable.Wrap(base.RightChild.AsSequentialQuery(token), token);
		return outer.Join(inner, _leftKeySelector, _rightKeySelector, _resultSelector, _keyComparer);
	}
}
