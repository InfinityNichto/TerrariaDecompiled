using System.Collections.Generic;

namespace System.Collections.Immutable;

internal sealed class ImmutableDictionaryDebuggerProxy<TKey, TValue> : ImmutableEnumerableDebuggerProxy<KeyValuePair<TKey, TValue>> where TKey : notnull
{
	public ImmutableDictionaryDebuggerProxy(IImmutableDictionary<TKey, TValue> dictionary)
		: base((IEnumerable<KeyValuePair<TKey, TValue>>)dictionary)
	{
	}
}
