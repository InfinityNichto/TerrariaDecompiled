using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading;

namespace System.Linq.Parallel;

internal sealed class ConcatQueryOperator<TSource> : BinaryQueryOperator<TSource, TSource, TSource>
{
	private sealed class ConcatQueryOperatorEnumerator<TLeftKey, TRightKey> : QueryOperatorEnumerator<TSource, ConcatKey<TLeftKey, TRightKey>>
	{
		private readonly QueryOperatorEnumerator<TSource, TLeftKey> _firstSource;

		private readonly QueryOperatorEnumerator<TSource, TRightKey> _secondSource;

		private bool _begunSecond;

		internal ConcatQueryOperatorEnumerator(QueryOperatorEnumerator<TSource, TLeftKey> firstSource, QueryOperatorEnumerator<TSource, TRightKey> secondSource)
		{
			_firstSource = firstSource;
			_secondSource = secondSource;
		}

		internal override bool MoveNext([MaybeNullWhen(false)][AllowNull] ref TSource currentElement, ref ConcatKey<TLeftKey, TRightKey> currentKey)
		{
			if (!_begunSecond)
			{
				TLeftKey currentKey2 = default(TLeftKey);
				if (_firstSource.MoveNext(ref currentElement, ref currentKey2))
				{
					currentKey = ConcatKey<TLeftKey, TRightKey>.MakeLeft(currentKey2);
					return true;
				}
				_begunSecond = true;
			}
			TRightKey currentKey3 = default(TRightKey);
			if (_secondSource.MoveNext(ref currentElement, ref currentKey3))
			{
				currentKey = ConcatKey<TLeftKey, TRightKey>.MakeRight(currentKey3);
				return true;
			}
			return false;
		}

		protected override void Dispose(bool disposing)
		{
			_firstSource.Dispose();
			_secondSource.Dispose();
		}
	}

	private sealed class ConcatQueryOperatorResults : BinaryQueryOperatorResults
	{
		private readonly int _leftChildCount;

		private readonly int _rightChildCount;

		internal override bool IsIndexible => true;

		internal override int ElementsCount => _leftChildCount + _rightChildCount;

		public static QueryResults<TSource> NewResults(QueryResults<TSource> leftChildQueryResults, QueryResults<TSource> rightChildQueryResults, ConcatQueryOperator<TSource> op, QuerySettings settings, bool preferStriping)
		{
			if (leftChildQueryResults.IsIndexible && rightChildQueryResults.IsIndexible)
			{
				return new ConcatQueryOperatorResults(leftChildQueryResults, rightChildQueryResults, op, settings, preferStriping);
			}
			return new BinaryQueryOperatorResults(leftChildQueryResults, rightChildQueryResults, op, settings, preferStriping);
		}

		private ConcatQueryOperatorResults(QueryResults<TSource> leftChildQueryResults, QueryResults<TSource> rightChildQueryResults, ConcatQueryOperator<TSource> concatOp, QuerySettings settings, bool preferStriping)
			: base(leftChildQueryResults, rightChildQueryResults, (BinaryQueryOperator<TSource, TSource, TSource>)concatOp, settings, preferStriping)
		{
			_leftChildCount = leftChildQueryResults.ElementsCount;
			_rightChildCount = rightChildQueryResults.ElementsCount;
		}

		internal override TSource GetElement(int index)
		{
			if (index < _leftChildCount)
			{
				return _leftChildQueryResults.GetElement(index);
			}
			return _rightChildQueryResults.GetElement(index - _leftChildCount);
		}
	}

	private readonly bool _prematureMergeLeft;

	private readonly bool _prematureMergeRight;

	internal override bool LimitsParallelism => false;

	internal ConcatQueryOperator(ParallelQuery<TSource> firstChild, ParallelQuery<TSource> secondChild)
		: base(firstChild, secondChild)
	{
		_outputOrdered = base.LeftChild.OutputOrdered || base.RightChild.OutputOrdered;
		_prematureMergeLeft = base.LeftChild.OrdinalIndexState.IsWorseThan(OrdinalIndexState.Increasing);
		_prematureMergeRight = base.RightChild.OrdinalIndexState.IsWorseThan(OrdinalIndexState.Increasing);
		if (base.LeftChild.OrdinalIndexState == OrdinalIndexState.Indexable && base.RightChild.OrdinalIndexState == OrdinalIndexState.Indexable)
		{
			SetOrdinalIndex(OrdinalIndexState.Indexable);
		}
		else
		{
			SetOrdinalIndex(OrdinalIndexState.Increasing.Worse(base.LeftChild.OrdinalIndexState.Worse(base.RightChild.OrdinalIndexState)));
		}
	}

	internal override QueryResults<TSource> Open(QuerySettings settings, bool preferStriping)
	{
		QueryResults<TSource> leftChildQueryResults = base.LeftChild.Open(settings, preferStriping);
		QueryResults<TSource> rightChildQueryResults = base.RightChild.Open(settings, preferStriping);
		return ConcatQueryOperatorResults.NewResults(leftChildQueryResults, rightChildQueryResults, this, settings, preferStriping);
	}

	public override void WrapPartitionedStream<TLeftKey, TRightKey>(PartitionedStream<TSource, TLeftKey> leftStream, PartitionedStream<TSource, TRightKey> rightStream, IPartitionedStreamRecipient<TSource> outputRecipient, bool preferStriping, QuerySettings settings)
	{
		if (_prematureMergeLeft)
		{
			ListQueryResults<TSource> listQueryResults = QueryOperator<TSource>.ExecuteAndCollectResults(leftStream, leftStream.PartitionCount, base.LeftChild.OutputOrdered, preferStriping, settings);
			PartitionedStream<TSource, int> partitionedStream = listQueryResults.GetPartitionedStream();
			WrapHelper(partitionedStream, rightStream, outputRecipient, settings, preferStriping);
		}
		else
		{
			WrapHelper(leftStream, rightStream, outputRecipient, settings, preferStriping);
		}
	}

	private void WrapHelper<TLeftKey, TRightKey>(PartitionedStream<TSource, TLeftKey> leftStreamInc, PartitionedStream<TSource, TRightKey> rightStream, IPartitionedStreamRecipient<TSource> outputRecipient, QuerySettings settings, bool preferStriping)
	{
		if (_prematureMergeRight)
		{
			ListQueryResults<TSource> listQueryResults = QueryOperator<TSource>.ExecuteAndCollectResults(rightStream, leftStreamInc.PartitionCount, base.LeftChild.OutputOrdered, preferStriping, settings);
			PartitionedStream<TSource, int> partitionedStream = listQueryResults.GetPartitionedStream();
			WrapHelper2(leftStreamInc, partitionedStream, outputRecipient);
		}
		else
		{
			WrapHelper2(leftStreamInc, rightStream, outputRecipient);
		}
	}

	private void WrapHelper2<TLeftKey, TRightKey>(PartitionedStream<TSource, TLeftKey> leftStreamInc, PartitionedStream<TSource, TRightKey> rightStreamInc, IPartitionedStreamRecipient<TSource> outputRecipient)
	{
		int partitionCount = leftStreamInc.PartitionCount;
		IComparer<ConcatKey<TLeftKey, TRightKey>> keyComparer = ConcatKey<TLeftKey, TRightKey>.MakeComparer(leftStreamInc.KeyComparer, rightStreamInc.KeyComparer);
		PartitionedStream<TSource, ConcatKey<TLeftKey, TRightKey>> partitionedStream = new PartitionedStream<TSource, ConcatKey<TLeftKey, TRightKey>>(partitionCount, keyComparer, OrdinalIndexState);
		for (int i = 0; i < partitionCount; i++)
		{
			partitionedStream[i] = new ConcatQueryOperatorEnumerator<TLeftKey, TRightKey>(leftStreamInc[i], rightStreamInc[i]);
		}
		outputRecipient.Receive(partitionedStream);
	}

	internal override IEnumerable<TSource> AsSequentialQuery(CancellationToken token)
	{
		return base.LeftChild.AsSequentialQuery(token).Concat(base.RightChild.AsSequentialQuery(token));
	}
}
