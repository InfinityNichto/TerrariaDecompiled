using System.Diagnostics;

namespace System.Collections.Immutable;

internal sealed class ImmutableListBuilderDebuggerProxy<T>
{
	private readonly ImmutableList<T>.Builder _list;

	private T[] _cachedContents;

	[DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
	public T[] Contents
	{
		get
		{
			if (_cachedContents == null)
			{
				_cachedContents = _list.ToArray(_list.Count);
			}
			return _cachedContents;
		}
	}

	public ImmutableListBuilderDebuggerProxy(ImmutableList<T>.Builder builder)
	{
		Requires.NotNull(builder, "builder");
		_list = builder;
	}
}
