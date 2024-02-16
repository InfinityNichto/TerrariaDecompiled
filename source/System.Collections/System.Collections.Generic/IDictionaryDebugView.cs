using System.Diagnostics;

namespace System.Collections.Generic;

internal sealed class IDictionaryDebugView<K, V>
{
	private readonly IDictionary<K, V> _dict;

	[DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
	public KeyValuePair<K, V>[] Items
	{
		get
		{
			KeyValuePair<K, V>[] array = new KeyValuePair<K, V>[_dict.Count];
			_dict.CopyTo(array, 0);
			return array;
		}
	}

	public IDictionaryDebugView(IDictionary<K, V> dictionary)
	{
		if (dictionary == null)
		{
			throw new ArgumentNullException("dictionary");
		}
		_dict = dictionary;
	}
}
