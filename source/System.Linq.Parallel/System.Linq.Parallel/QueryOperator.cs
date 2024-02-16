using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace System.Linq.Parallel;

internal abstract class QueryOperator<TOutput> : ParallelQuery<TOutput>
{
	protected bool _outputOrdered;

	internal bool OutputOrdered => _outputOrdered;

	internal abstract bool LimitsParallelism { get; }

	internal abstract OrdinalIndexState OrdinalIndexState { get; }

	internal QueryOperator(QuerySettings settings)
		: this(isOrdered: false, settings)
	{
	}

	internal QueryOperator(bool isOrdered, QuerySettings settings)
		: base(settings)
	{
		_outputOrdered = isOrdered;
	}

	internal abstract QueryResults<TOutput> Open(QuerySettings settings, bool preferStriping);

	public override IEnumerator<TOutput> GetEnumerator()
	{
		return GetEnumerator(null, suppressOrderPreservation: false);
	}

	public IEnumerator<TOutput> GetEnumerator(ParallelMergeOptions? mergeOptions)
	{
		return GetEnumerator(mergeOptions, suppressOrderPreservation: false);
	}

	internal virtual IEnumerator<TOutput> GetEnumerator(ParallelMergeOptions? mergeOptions, bool suppressOrderPreservation)
	{
		return new QueryOpeningEnumerator<TOutput>(this, mergeOptions, suppressOrderPreservation);
	}

	internal IEnumerator<TOutput> GetOpenedEnumerator(ParallelMergeOptions? mergeOptions, bool suppressOrder, bool forEffect, QuerySettings querySettings)
	{
		if (querySettings.ExecutionMode.Value == ParallelExecutionMode.Default && LimitsParallelism)
		{
			IEnumerable<TOutput> source = AsSequentialQuery(querySettings.CancellationState.ExternalCancellationToken);
			return ExceptionAggregator.WrapEnumerable(source, querySettings.CancellationState).GetEnumerator();
		}
		QueryResults<TOutput> queryResults = GetQueryResults(querySettings);
		if (!mergeOptions.HasValue)
		{
			mergeOptions = querySettings.MergeOptions;
		}
		if (querySettings.CancellationState.MergedCancellationToken.IsCancellationRequested)
		{
			querySettings.CancellationState.ExternalCancellationToken.ThrowIfCancellationRequested();
			throw new OperationCanceledException();
		}
		bool outputOrdered = OutputOrdered && !suppressOrder;
		PartitionedStreamMerger<TOutput> partitionedStreamMerger = new PartitionedStreamMerger<TOutput>(forEffect, mergeOptions.GetValueOrDefault(), querySettings.TaskScheduler, outputOrdered, querySettings.CancellationState, querySettings.QueryId);
		queryResults.GivePartitionedStream(partitionedStreamMerger);
		if (forEffect)
		{
			return null;
		}
		return partitionedStreamMerger.MergeExecutor.GetEnumerator();
	}

	private QueryResults<TOutput> GetQueryResults(QuerySettings querySettings)
	{
		return Open(querySettings, preferStriping: false);
	}

	internal TOutput[] ExecuteAndGetResultsAsArray()
	{
		QuerySettings querySettings = base.SpecifiedQuerySettings.WithPerExecutionSettings().WithDefaults();
		QueryLifecycle.LogicalQueryExecutionBegin(querySettings.QueryId);
		try
		{
			if (querySettings.ExecutionMode.Value == ParallelExecutionMode.Default && LimitsParallelism)
			{
				IEnumerable<TOutput> source = AsSequentialQuery(querySettings.CancellationState.ExternalCancellationToken);
				IEnumerable<TOutput> source2 = CancellableEnumerable.Wrap(source, querySettings.CancellationState.ExternalCancellationToken);
				return ExceptionAggregator.WrapEnumerable(source2, querySettings.CancellationState).ToArray();
			}
			QueryResults<TOutput> queryResults = GetQueryResults(querySettings);
			if (querySettings.CancellationState.MergedCancellationToken.IsCancellationRequested)
			{
				querySettings.CancellationState.ExternalCancellationToken.ThrowIfCancellationRequested();
				throw new OperationCanceledException();
			}
			if (queryResults.IsIndexible && OutputOrdered)
			{
				ArrayMergeHelper<TOutput> arrayMergeHelper = new ArrayMergeHelper<TOutput>(base.SpecifiedQuerySettings, queryResults);
				arrayMergeHelper.Execute();
				TOutput[] resultsAsArray = arrayMergeHelper.GetResultsAsArray();
				querySettings.CleanStateAtQueryEnd();
				return resultsAsArray;
			}
			PartitionedStreamMerger<TOutput> partitionedStreamMerger = new PartitionedStreamMerger<TOutput>(forEffectMerge: false, ParallelMergeOptions.FullyBuffered, querySettings.TaskScheduler, OutputOrdered, querySettings.CancellationState, querySettings.QueryId);
			queryResults.GivePartitionedStream(partitionedStreamMerger);
			TOutput[] resultsAsArray2 = partitionedStreamMerger.MergeExecutor.GetResultsAsArray();
			querySettings.CleanStateAtQueryEnd();
			return resultsAsArray2;
		}
		finally
		{
			QueryLifecycle.LogicalQueryExecutionEnd(querySettings.QueryId);
		}
	}

	internal abstract IEnumerable<TOutput> AsSequentialQuery(CancellationToken token);

	internal static ListQueryResults<TOutput> ExecuteAndCollectResults<TKey>(PartitionedStream<TOutput, TKey> openedChild, int partitionCount, bool outputOrdered, bool useStriping, QuerySettings settings)
	{
		TaskScheduler taskScheduler = settings.TaskScheduler;
		MergeExecutor<TOutput> mergeExecutor = MergeExecutor<TOutput>.Execute(openedChild, ignoreOutput: false, ParallelMergeOptions.FullyBuffered, taskScheduler, outputOrdered, settings.CancellationState, settings.QueryId);
		return new ListQueryResults<TOutput>(mergeExecutor.GetResultsAsArray(), partitionCount, useStriping);
	}

	internal static QueryOperator<TOutput> AsQueryOperator(IEnumerable<TOutput> source)
	{
		QueryOperator<TOutput> queryOperator = source as QueryOperator<TOutput>;
		if (queryOperator == null)
		{
			queryOperator = ((!(source is OrderedParallelQuery<TOutput> orderedParallelQuery)) ? new ScanQueryOperator<TOutput>(source) : orderedParallelQuery.SortOperator);
		}
		return queryOperator;
	}
}
