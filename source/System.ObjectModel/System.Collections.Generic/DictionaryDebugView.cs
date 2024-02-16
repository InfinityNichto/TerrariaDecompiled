using System.Diagnostics;

namespace System.Collections.Generic;

internal sealed class DictionaryDebugView<K, V>
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

	public DictionaryDebugView(IDictionary<K, V> dictionary)
	{
		_dict = dictionary ?? throw new ArgumentNullException("dictionary");
	}
}
