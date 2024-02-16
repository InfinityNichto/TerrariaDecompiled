using System.Collections.Generic;
using System.Diagnostics;

namespace System.Collections.Concurrent;

internal sealed class IDictionaryDebugView<TKey, TValue>
{
	private readonly IDictionary<TKey, TValue> _dictionary;

	[DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
	public KeyValuePair<TKey, TValue>[] Items
	{
		get
		{
			KeyValuePair<TKey, TValue>[] array = new KeyValuePair<TKey, TValue>[_dictionary.Count];
			_dictionary.CopyTo(array, 0);
			return array;
		}
	}

	public IDictionaryDebugView(IDictionary<TKey, TValue> dictionary)
	{
		if (dictionary == null)
		{
			System.ThrowHelper.ThrowArgumentNullException("dictionary");
		}
		_dictionary = dictionary;
	}
}
