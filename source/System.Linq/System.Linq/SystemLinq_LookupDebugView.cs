using System.Diagnostics;

namespace System.Linq;

internal sealed class SystemLinq_LookupDebugView<TKey, TElement>
{
	private readonly Lookup<TKey, TElement> _lookup;

	private IGrouping<TKey, TElement>[] _cachedGroupings;

	[DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
	public IGrouping<TKey, TElement>[] Groupings => _cachedGroupings ?? (_cachedGroupings = _lookup.ToArray());

	public SystemLinq_LookupDebugView(Lookup<TKey, TElement> lookup)
	{
		_lookup = lookup;
	}
}
