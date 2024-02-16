using System.Collections.Generic;
using System.Threading;

namespace System.Linq.Parallel;

internal sealed class ScanQueryOperator<TElement> : QueryOperator<TElement>
{
	private sealed class ScanEnumerableQueryOperatorResults : QueryResults<TElement>
	{
		private readonly IEnumerable<TElement> _data;

		private QuerySettings _settings;

		internal ScanEnumerableQueryOperatorResults(IEnumerable<TElement> data, QuerySettings settings)
		{
			_data = data;
			_settings = settings;
		}

		internal override void GivePartitionedStream(IPartitionedStreamRecipient<TElement> recipient)
		{
			PartitionedStream<TElement, int> partitionedStream = ExchangeUtilities.PartitionDataSource(_data, _settings.DegreeOfParallelism.Value, useStriping: false);
			recipient.Receive(partitionedStream);
		}
	}

	private readonly IEnumerable<TElement> _data;

	public IEnumerable<TElement> Data => _data;

	internal override OrdinalIndexState OrdinalIndexState
	{
		get
		{
			if (!(_data is IList<TElement>))
			{
				return OrdinalIndexState.Correct;
			}
			return OrdinalIndexState.Indexable;
		}
	}

	internal override bool LimitsParallelism => false;

	internal ScanQueryOperator(IEnumerable<TElement> data)
		: base(isOrdered: false, QuerySettings.Empty)
	{
		if (data is ParallelEnumerableWrapper<TElement> parallelEnumerableWrapper)
		{
			data = parallelEnumerableWrapper.WrappedEnumerable;
		}
		_data = data;
	}

	internal override QueryResults<TElement> Open(QuerySettings settings, bool preferStriping)
	{
		if (_data is IList<TElement> source)
		{
			return new ListQueryResults<TElement>(source, settings.DegreeOfParallelism.GetValueOrDefault(), preferStriping);
		}
		return new ScanEnumerableQueryOperatorResults(_data, settings);
	}

	internal override IEnumerator<TElement> GetEnumerator(ParallelMergeOptions? mergeOptions, bool suppressOrderPreservation)
	{
		return _data.GetEnumerator();
	}

	internal override IEnumerable<TElement> AsSequentialQuery(CancellationToken token)
	{
		return _data;
	}
}
