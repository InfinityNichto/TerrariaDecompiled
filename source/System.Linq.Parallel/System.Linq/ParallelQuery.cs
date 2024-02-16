using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Parallel;

namespace System.Linq;

public class ParallelQuery : IEnumerable
{
	private QuerySettings _specifiedSettings;

	internal QuerySettings SpecifiedQuerySettings => _specifiedSettings;

	internal ParallelQuery(QuerySettings specifiedSettings)
	{
		_specifiedSettings = specifiedSettings;
	}

	[ExcludeFromCodeCoverage(Justification = "The derived class must override this method")]
	internal virtual ParallelQuery<TCastTo> Cast<TCastTo>()
	{
		throw new NotSupportedException();
	}

	[ExcludeFromCodeCoverage(Justification = "The derived class must override this method")]
	internal virtual ParallelQuery<TCastTo> OfType<TCastTo>()
	{
		throw new NotSupportedException();
	}

	[ExcludeFromCodeCoverage(Justification = "The derived class must override this method")]
	internal virtual IEnumerator GetEnumeratorUntyped()
	{
		throw new NotSupportedException();
	}

	IEnumerator IEnumerable.GetEnumerator()
	{
		return GetEnumeratorUntyped();
	}
}
public class ParallelQuery<TSource> : ParallelQuery, IEnumerable<TSource>, IEnumerable
{
	internal ParallelQuery(QuerySettings settings)
		: base(settings)
	{
	}

	internal sealed override ParallelQuery<TCastTo> Cast<TCastTo>()
	{
		return this.Select((TSource elem) => (TCastTo)(object)elem);
	}

	internal sealed override ParallelQuery<TCastTo> OfType<TCastTo>()
	{
		return from elem in this
			where elem is TCastTo
			select (TCastTo)(object)elem;
	}

	internal override IEnumerator GetEnumeratorUntyped()
	{
		return ((IEnumerable<TSource>)this).GetEnumerator();
	}

	public virtual IEnumerator<TSource> GetEnumerator()
	{
		throw new NotSupportedException();
	}
}
