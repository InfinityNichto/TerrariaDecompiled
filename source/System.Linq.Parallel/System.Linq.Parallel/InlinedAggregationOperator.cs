using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading;

namespace System.Linq.Parallel;

internal abstract class InlinedAggregationOperator<TSource, TIntermediate, TResult> : UnaryQueryOperator<TSource, TIntermediate>
{
	internal override bool LimitsParallelism => false;

	internal InlinedAggregationOperator(IEnumerable<TSource> child)
		: base(child)
	{
	}

	internal TResult Aggregate()
	{
		Exception singularExceptionToThrow = null;
		TResult result;
		try
		{
			result = InternalAggregate(ref singularExceptionToThrow);
		}
		catch (Exception ex)
		{
			if (!(ex is AggregateException))
			{
				if (ex is OperationCanceledException ex2 && ex2.CancellationToken == base.SpecifiedQuerySettings.CancellationState.ExternalCancellationToken && base.SpecifiedQuerySettings.CancellationState.ExternalCancellationToken.IsCancellationRequested)
				{
					throw;
				}
				throw new AggregateException(ex);
			}
			throw;
		}
		if (singularExceptionToThrow != null)
		{
			throw singularExceptionToThrow;
		}
		return result;
	}

	protected abstract TResult InternalAggregate(ref Exception singularExceptionToThrow);

	internal override QueryResults<TIntermediate> Open(QuerySettings settings, bool preferStriping)
	{
		QueryResults<TSource> childQueryResults = base.Child.Open(settings, preferStriping);
		return new UnaryQueryOperatorResults(childQueryResults, this, settings, preferStriping);
	}

	internal override void WrapPartitionedStream<TKey>(PartitionedStream<TSource, TKey> inputStream, IPartitionedStreamRecipient<TIntermediate> recipient, bool preferStriping, QuerySettings settings)
	{
		int partitionCount = inputStream.PartitionCount;
		PartitionedStream<TIntermediate, int> partitionedStream = new PartitionedStream<TIntermediate, int>(partitionCount, Util.GetDefaultComparer<int>(), OrdinalIndexState.Correct);
		for (int i = 0; i < partitionCount; i++)
		{
			partitionedStream[i] = CreateEnumerator(i, partitionCount, inputStream[i], null, settings.CancellationState.MergedCancellationToken);
		}
		recipient.Receive(partitionedStream);
	}

	protected abstract QueryOperatorEnumerator<TIntermediate, int> CreateEnumerator<TKey>(int index, int count, QueryOperatorEnumerator<TSource, TKey> source, object sharedData, CancellationToken cancellationToken);

	[ExcludeFromCodeCoverage(Justification = "This method should never be called. Associative aggregation can always be parallelized")]
	internal override IEnumerable<TIntermediate> AsSequentialQuery(CancellationToken token)
	{
		throw new NotSupportedException();
	}
}
