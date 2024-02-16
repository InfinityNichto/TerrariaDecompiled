using System.Collections.Generic;
using System.Threading;

namespace System.Linq.Parallel;

internal sealed class ZipQueryOperator<TLeftInput, TRightInput, TOutput> : QueryOperator<TOutput>
{
	internal sealed class ZipQueryOperatorResults : QueryResults<TOutput>
	{
		private readonly QueryResults<TLeftInput> _leftChildResults;

		private readonly QueryResults<TRightInput> _rightChildResults;

		private readonly Func<TLeftInput, TRightInput, TOutput> _resultSelector;

		private readonly int _count;

		private readonly int _partitionCount;

		private readonly bool _preferStriping;

		internal override int ElementsCount => _count;

		internal override bool IsIndexible => true;

		internal ZipQueryOperatorResults(QueryResults<TLeftInput> leftChildResults, QueryResults<TRightInput> rightChildResults, Func<TLeftInput, TRightInput, TOutput> resultSelector, int partitionCount, bool preferStriping)
		{
			_leftChildResults = leftChildResults;
			_rightChildResults = rightChildResults;
			_resultSelector = resultSelector;
			_partitionCount = partitionCount;
			_preferStriping = preferStriping;
			_count = Math.Min(_leftChildResults.Count, _rightChildResults.Count);
		}

		internal override TOutput GetElement(int index)
		{
			return _resultSelector(_leftChildResults.GetElement(index), _rightChildResults.GetElement(index));
		}

		internal override void GivePartitionedStream(IPartitionedStreamRecipient<TOutput> recipient)
		{
			PartitionedStream<TOutput, int> partitionedStream = ExchangeUtilities.PartitionDataSource(this, _partitionCount, _preferStriping);
			recipient.Receive(partitionedStream);
		}
	}

	private readonly Func<TLeftInput, TRightInput, TOutput> _resultSelector;

	private readonly QueryOperator<TLeftInput> _leftChild;

	private readonly QueryOperator<TRightInput> _rightChild;

	private readonly bool _prematureMergeLeft;

	private readonly bool _prematureMergeRight;

	private readonly bool _limitsParallelism;

	internal override OrdinalIndexState OrdinalIndexState => OrdinalIndexState.Indexable;

	internal override bool LimitsParallelism => _limitsParallelism;

	internal ZipQueryOperator(ParallelQuery<TLeftInput> leftChildSource, ParallelQuery<TRightInput> rightChildSource, Func<TLeftInput, TRightInput, TOutput> resultSelector)
		: this(QueryOperator<TLeftInput>.AsQueryOperator(leftChildSource), QueryOperator<TRightInput>.AsQueryOperator(rightChildSource), resultSelector)
	{
	}

	private ZipQueryOperator(QueryOperator<TLeftInput> left, QueryOperator<TRightInput> right, Func<TLeftInput, TRightInput, TOutput> resultSelector)
		: base(left.SpecifiedQuerySettings.Merge(right.SpecifiedQuerySettings))
	{
		_leftChild = left;
		_rightChild = right;
		_resultSelector = resultSelector;
		_outputOrdered = _leftChild.OutputOrdered || _rightChild.OutputOrdered;
		OrdinalIndexState ordinalIndexState = _leftChild.OrdinalIndexState;
		OrdinalIndexState ordinalIndexState2 = _rightChild.OrdinalIndexState;
		_prematureMergeLeft = ordinalIndexState != OrdinalIndexState.Indexable;
		_prematureMergeRight = ordinalIndexState2 != OrdinalIndexState.Indexable;
		_limitsParallelism = (_prematureMergeLeft && ordinalIndexState != OrdinalIndexState.Shuffled) || (_prematureMergeRight && ordinalIndexState2 != OrdinalIndexState.Shuffled);
	}

	internal override QueryResults<TOutput> Open(QuerySettings settings, bool preferStriping)
	{
		QueryResults<TLeftInput> queryResults = _leftChild.Open(settings, preferStriping);
		QueryResults<TRightInput> queryResults2 = _rightChild.Open(settings, preferStriping);
		int value = settings.DegreeOfParallelism.Value;
		if (_prematureMergeLeft)
		{
			PartitionedStreamMerger<TLeftInput> partitionedStreamMerger = new PartitionedStreamMerger<TLeftInput>(forEffectMerge: false, ParallelMergeOptions.FullyBuffered, settings.TaskScheduler, _leftChild.OutputOrdered, settings.CancellationState, settings.QueryId);
			queryResults.GivePartitionedStream(partitionedStreamMerger);
			queryResults = new ListQueryResults<TLeftInput>(partitionedStreamMerger.MergeExecutor.GetResultsAsArray(), value, preferStriping);
		}
		if (_prematureMergeRight)
		{
			PartitionedStreamMerger<TRightInput> partitionedStreamMerger2 = new PartitionedStreamMerger<TRightInput>(forEffectMerge: false, ParallelMergeOptions.FullyBuffered, settings.TaskScheduler, _rightChild.OutputOrdered, settings.CancellationState, settings.QueryId);
			queryResults2.GivePartitionedStream(partitionedStreamMerger2);
			queryResults2 = new ListQueryResults<TRightInput>(partitionedStreamMerger2.MergeExecutor.GetResultsAsArray(), value, preferStriping);
		}
		return new ZipQueryOperatorResults(queryResults, queryResults2, _resultSelector, value, preferStriping);
	}

	internal override IEnumerable<TOutput> AsSequentialQuery(CancellationToken token)
	{
		using IEnumerator<TLeftInput> leftEnumerator = _leftChild.AsSequentialQuery(token).GetEnumerator();
		using IEnumerator<TRightInput> rightEnumerator = _rightChild.AsSequentialQuery(token).GetEnumerator();
		while (leftEnumerator.MoveNext() && rightEnumerator.MoveNext())
		{
			yield return _resultSelector(leftEnumerator.Current, rightEnumerator.Current);
		}
	}
}
