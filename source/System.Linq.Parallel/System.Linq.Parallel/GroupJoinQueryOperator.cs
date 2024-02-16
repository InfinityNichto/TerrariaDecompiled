using System.Collections.Generic;
using System.Threading;

namespace System.Linq.Parallel;

internal sealed class GroupJoinQueryOperator<TLeftInput, TRightInput, TKey, TOutput> : BinaryQueryOperator<TLeftInput, TRightInput, TOutput>
{
	private readonly Func<TLeftInput, TKey> _leftKeySelector;

	private readonly Func<TRightInput, TKey> _rightKeySelector;

	private readonly Func<TLeftInput, IEnumerable<TRightInput>, TOutput> _resultSelector;

	private readonly IEqualityComparer<TKey> _keyComparer;

	internal override bool LimitsParallelism => false;

	internal GroupJoinQueryOperator(ParallelQuery<TLeftInput> left, ParallelQuery<TRightInput> right, Func<TLeftInput, TKey> leftKeySelector, Func<TRightInput, TKey> rightKeySelector, Func<TLeftInput, IEnumerable<TRightInput>, TOutput> resultSelector, IEqualityComparer<TKey> keyComparer)
		: base(left, right)
	{
		_leftKeySelector = leftKeySelector;
		_rightKeySelector = rightKeySelector;
		_resultSelector = resultSelector;
		_keyComparer = keyComparer;
		_outputOrdered = base.LeftChild.OutputOrdered;
		SetOrdinalIndex(OrdinalIndexState.Shuffled);
	}

	internal override QueryResults<TOutput> Open(QuerySettings settings, bool preferStriping)
	{
		QueryResults<TLeftInput> leftChildQueryResults = base.LeftChild.Open(settings, preferStriping: false);
		QueryResults<TRightInput> rightChildQueryResults = base.RightChild.Open(settings, preferStriping: false);
		return new BinaryQueryOperatorResults(leftChildQueryResults, rightChildQueryResults, this, settings, preferStriping: false);
	}

	public override void WrapPartitionedStream<TLeftKey, TRightKey>(PartitionedStream<TLeftInput, TLeftKey> leftStream, PartitionedStream<TRightInput, TRightKey> rightStream, IPartitionedStreamRecipient<TOutput> outputRecipient, bool preferStriping, QuerySettings settings)
	{
		int partitionCount = leftStream.PartitionCount;
		if (base.LeftChild.OutputOrdered)
		{
			WrapPartitionedStreamHelper(ExchangeUtilities.HashRepartitionOrdered(leftStream, _leftKeySelector, _keyComparer, null, settings.CancellationState.MergedCancellationToken), rightStream, outputRecipient, partitionCount, settings.CancellationState.MergedCancellationToken);
		}
		else
		{
			WrapPartitionedStreamHelper(ExchangeUtilities.HashRepartition(leftStream, _leftKeySelector, _keyComparer, null, settings.CancellationState.MergedCancellationToken), rightStream, outputRecipient, partitionCount, settings.CancellationState.MergedCancellationToken);
		}
	}

	private void WrapPartitionedStreamHelper<TLeftKey, TRightKey>(PartitionedStream<Pair<TLeftInput, TKey>, TLeftKey> leftHashStream, PartitionedStream<TRightInput, TRightKey> rightPartitionedStream, IPartitionedStreamRecipient<TOutput> outputRecipient, int partitionCount, CancellationToken cancellationToken)
	{
		if (base.RightChild.OutputOrdered)
		{
			PartitionedStream<Pair<TRightInput, TKey>, TRightKey> partitionedStream = ExchangeUtilities.HashRepartitionOrdered(rightPartitionedStream, _rightKeySelector, _keyComparer, null, cancellationToken);
			HashLookupBuilder<IEnumerable<TRightInput>, Pair<bool, TRightKey>, TKey>[] array = new HashLookupBuilder<IEnumerable<TRightInput>, Pair<bool, TRightKey>, TKey>[partitionCount];
			for (int i = 0; i < partitionCount; i++)
			{
				array[i] = new OrderedGroupJoinHashLookupBuilder<TRightInput, TRightKey, TKey>(partitionedStream[i], _keyComparer, partitionedStream.KeyComparer);
			}
			WrapPartitionedStreamHelper(leftHashStream, array, CreateComparer(rightPartitionedStream.KeyComparer), outputRecipient, partitionCount, cancellationToken);
		}
		else
		{
			PartitionedStream<Pair<TRightInput, TKey>, int> partitionedStream2 = ExchangeUtilities.HashRepartition(rightPartitionedStream, _rightKeySelector, _keyComparer, null, cancellationToken);
			HashLookupBuilder<IEnumerable<TRightInput>, int, TKey>[] array2 = new HashLookupBuilder<IEnumerable<TRightInput>, int, TKey>[partitionCount];
			for (int j = 0; j < partitionCount; j++)
			{
				array2[j] = new GroupJoinHashLookupBuilder<TRightInput, int, TKey>(partitionedStream2[j], _keyComparer);
			}
			WrapPartitionedStreamHelper(leftHashStream, array2, null, outputRecipient, partitionCount, cancellationToken);
		}
	}

	private void WrapPartitionedStreamHelper<TLeftKey, TRightKey>(PartitionedStream<Pair<TLeftInput, TKey>, TLeftKey> leftHashStream, HashLookupBuilder<IEnumerable<TRightInput>, TRightKey, TKey>[] rightLookupBuilders, IComparer<TRightKey> rightKeyComparer, IPartitionedStreamRecipient<TOutput> outputRecipient, int partitionCount, CancellationToken cancellationToken)
	{
		if (base.RightChild.OutputOrdered && base.LeftChild.OutputOrdered)
		{
			PairOutputKeyBuilder<TLeftKey, TRightKey> outputKeyBuilder = new PairOutputKeyBuilder<TLeftKey, TRightKey>();
			IComparer<Pair<TLeftKey, TRightKey>> outputKeyComparer = new PairComparer<TLeftKey, TRightKey>(leftHashStream.KeyComparer, rightKeyComparer);
			WrapPartitionedStreamHelper(leftHashStream, rightLookupBuilders, outputKeyBuilder, outputKeyComparer, outputRecipient, partitionCount, cancellationToken);
		}
		else
		{
			LeftKeyOutputKeyBuilder<TLeftKey, TRightKey> outputKeyBuilder2 = new LeftKeyOutputKeyBuilder<TLeftKey, TRightKey>();
			WrapPartitionedStreamHelper(leftHashStream, rightLookupBuilders, outputKeyBuilder2, leftHashStream.KeyComparer, outputRecipient, partitionCount, cancellationToken);
		}
	}

	private IComparer<Pair<bool, TRightKey>> CreateComparer<TRightKey>(IComparer<TRightKey> comparer)
	{
		return CreateComparer(Comparer<bool>.Default, comparer);
	}

	private IComparer<Pair<TLeftKey, TRightKey>> CreateComparer<TLeftKey, TRightKey>(IComparer<TLeftKey> leftKeyComparer, IComparer<TRightKey> rightKeyComparer)
	{
		return new PairComparer<TLeftKey, TRightKey>(leftKeyComparer, rightKeyComparer);
	}

	private void WrapPartitionedStreamHelper<TLeftKey, TRightKey, TOutputKey>(PartitionedStream<Pair<TLeftInput, TKey>, TLeftKey> leftHashStream, HashLookupBuilder<IEnumerable<TRightInput>, TRightKey, TKey>[] rightLookupBuilders, HashJoinOutputKeyBuilder<TLeftKey, TRightKey, TOutputKey> outputKeyBuilder, IComparer<TOutputKey> outputKeyComparer, IPartitionedStreamRecipient<TOutput> outputRecipient, int partitionCount, CancellationToken cancellationToken)
	{
		PartitionedStream<TOutput, TOutputKey> partitionedStream = new PartitionedStream<TOutput, TOutputKey>(partitionCount, outputKeyComparer, OrdinalIndexState);
		for (int i = 0; i < partitionCount; i++)
		{
			partitionedStream[i] = new HashJoinQueryOperatorEnumerator<TLeftInput, TLeftKey, IEnumerable<TRightInput>, TRightKey, TKey, TOutput, TOutputKey>(leftHashStream[i], rightLookupBuilders[i], _resultSelector, outputKeyBuilder, cancellationToken);
		}
		outputRecipient.Receive(partitionedStream);
	}

	internal override IEnumerable<TOutput> AsSequentialQuery(CancellationToken token)
	{
		IEnumerable<TLeftInput> outer = CancellableEnumerable.Wrap(base.LeftChild.AsSequentialQuery(token), token);
		IEnumerable<TRightInput> inner = CancellableEnumerable.Wrap(base.RightChild.AsSequentialQuery(token), token);
		return outer.GroupJoin(inner, _leftKeySelector, _rightKeySelector, _resultSelector, _keyComparer);
	}
}
