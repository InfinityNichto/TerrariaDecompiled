using System.Diagnostics;

namespace System.Collections.Generic;

internal sealed class DictionaryKeyCollectionDebugView<TKey, TValue>
{
	private readonly ICollection<TKey> _collection;

	[DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
	public TKey[] Items
	{
		get
		{
			TKey[] array = new TKey[_collection.Count];
			_collection.CopyTo(array, 0);
			return array;
		}
	}

	public DictionaryKeyCollectionDebugView(ICollection<TKey> collection)
	{
		if (collection == null)
		{
			throw new ArgumentNullException("collection");
		}
		_collection = collection;
	}
}
