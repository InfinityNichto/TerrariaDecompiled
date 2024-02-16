using System.Collections.Generic;
using System.Threading;

namespace System.Linq.Parallel;

internal sealed class OrderingQueryOperator<TSource> : QueryOperator<TSource>
{
	private readonly QueryOperator<TSource> _child;

	private readonly OrdinalIndexState _ordinalIndexState;

	internal override bool LimitsParallelism => _child.LimitsParallelism;

	internal override OrdinalIndexState OrdinalIndexState => _ordinalIndexState;

	public OrderingQueryOperator(QueryOperator<TSource> child, bool orderOn)
		: base(orderOn, child.SpecifiedQuerySettings)
	{
		_child = child;
		_ordinalIndexState = _child.OrdinalIndexState;
	}

	internal override QueryResults<TSource> Open(QuerySettings settings, bool preferStriping)
	{
		return _child.Open(settings, preferStriping);
	}

	internal override IEnumerator<TSource> GetEnumerator(ParallelMergeOptions? mergeOptions, bool suppressOrderPreservation)
	{
		if (_child is ScanQueryOperator<TSource> scanQueryOperator)
		{
			return scanQueryOperator.Data.GetEnumerator();
		}
		return base.GetEnumerator(mergeOptions, suppressOrderPreservation);
	}

	internal override IEnumerable<TSource> AsSequentialQuery(CancellationToken token)
	{
		return _child.AsSequentialQuery(token);
	}
}
