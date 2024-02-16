using System.Collections.Generic;
using System.Diagnostics;

namespace System.Collections.Immutable;

internal sealed class ImmutableDictionaryBuilderDebuggerProxy<TKey, TValue> where TKey : notnull
{
	private readonly ImmutableDictionary<TKey, TValue>.Builder _map;

	private KeyValuePair<TKey, TValue>[] _contents;

	[DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
	public KeyValuePair<TKey, TValue>[] Contents
	{
		get
		{
			if (_contents == null)
			{
				_contents = _map.ToArray(_map.Count);
			}
			return _contents;
		}
	}

	public ImmutableDictionaryBuilderDebuggerProxy(ImmutableDictionary<TKey, TValue>.Builder map)
	{
		Requires.NotNull(map, "map");
		_map = map;
	}
}
