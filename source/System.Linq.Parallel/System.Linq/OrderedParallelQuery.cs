using System.Collections.Generic;
using System.Linq.Parallel;

namespace System.Linq;

public class OrderedParallelQuery<TSource> : ParallelQuery<TSource>
{
	private readonly QueryOperator<TSource> _sortOp;

	internal QueryOperator<TSource> SortOperator => _sortOp;

	internal IOrderedEnumerable<TSource> OrderedEnumerable => (IOrderedEnumerable<TSource>)_sortOp;

	internal OrderedParallelQuery(QueryOperator<TSource> sortOp)
		: base(sortOp.SpecifiedQuerySettings)
	{
		_sortOp = sortOp;
	}

	public override IEnumerator<TSource> GetEnumerator()
	{
		return _sortOp.GetEnumerator();
	}
}
