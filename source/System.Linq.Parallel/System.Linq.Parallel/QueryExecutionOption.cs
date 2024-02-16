using System.Collections.Generic;
using System.Threading;

namespace System.Linq.Parallel;

internal sealed class QueryExecutionOption<TSource> : QueryOperator<TSource>
{
	private readonly QueryOperator<TSource> _child;

	private readonly OrdinalIndexState _indexState;

	internal override OrdinalIndexState OrdinalIndexState => _indexState;

	internal override bool LimitsParallelism => _child.LimitsParallelism;

	internal QueryExecutionOption(QueryOperator<TSource> source, QuerySettings settings)
		: base(source.OutputOrdered, settings.Merge(source.SpecifiedQuerySettings))
	{
		_child = source;
		_indexState = _child.OrdinalIndexState;
	}

	internal override QueryResults<TSource> Open(QuerySettings settings, bool preferStriping)
	{
		return _child.Open(settings, preferStriping);
	}

	internal override IEnumerable<TSource> AsSequentialQuery(CancellationToken token)
	{
		return _child.AsSequentialQuery(token);
	}
}
