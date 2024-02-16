using System.Collections.Generic;
using System.Diagnostics;

namespace System.Text.RegularExpressions;

internal sealed class CollectionDebuggerProxy<T>
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

	public CollectionDebuggerProxy(ICollection<T> collection)
	{
		_collection = collection ?? throw new ArgumentNullException("collection");
	}
}
