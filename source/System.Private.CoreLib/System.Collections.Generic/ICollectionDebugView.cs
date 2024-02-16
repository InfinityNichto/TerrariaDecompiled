using System.Diagnostics;

namespace System.Collections.Generic;

internal sealed class ICollectionDebugView<T>
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

	public ICollectionDebugView(ICollection<T> collection)
	{
		if (collection == null)
		{
			throw new ArgumentNullException("collection");
		}
		_collection = collection;
	}
}
