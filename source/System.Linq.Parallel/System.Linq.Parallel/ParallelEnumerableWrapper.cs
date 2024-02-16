using System.Collections;
using System.Collections.Generic;

namespace System.Linq.Parallel;

internal sealed class ParallelEnumerableWrapper : ParallelQuery<object>
{
	private readonly IEnumerable _source;

	internal ParallelEnumerableWrapper(IEnumerable source)
		: base(QuerySettings.Empty)
	{
		_source = source;
	}

	internal override IEnumerator GetEnumeratorUntyped()
	{
		return _source.GetEnumerator();
	}

	public override IEnumerator<object> GetEnumerator()
	{
		return new EnumerableWrapperWeakToStrong(_source).GetEnumerator();
	}
}
internal sealed class ParallelEnumerableWrapper<T> : ParallelQuery<T>
{
	private readonly IEnumerable<T> _wrappedEnumerable;

	internal IEnumerable<T> WrappedEnumerable => _wrappedEnumerable;

	internal ParallelEnumerableWrapper(IEnumerable<T> wrappedEnumerable)
		: base(QuerySettings.Empty)
	{
		_wrappedEnumerable = wrappedEnumerable;
	}

	public override IEnumerator<T> GetEnumerator()
	{
		return _wrappedEnumerable.GetEnumerator();
	}
}
