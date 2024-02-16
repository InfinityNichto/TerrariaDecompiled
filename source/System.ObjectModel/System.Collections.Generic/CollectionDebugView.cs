using System.Diagnostics;

namespace System.Collections.Generic;

internal sealed class CollectionDebugView<T>
{
	private readonly ICollection<T> _collection;

	[DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
	public T[] Items
	{
		get
		{
			T[] array = new T[_collection.Count];
			_collection.CopyTo(array, 0);
			return array;
		}
	}

	public CollectionDebugView(ICollection<T> collection)
	{
		_collection = collection ?? throw new ArgumentNullException("collection");
	}
}
