using System.Diagnostics;
using System.Runtime.Versioning;

namespace System.Collections.Concurrent;

internal sealed class BlockingCollectionDebugView<T>
{
	private readonly BlockingCollection<T> _blockingCollection;

	[UnsupportedOSPlatform("browser")]
	[DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
	public T[] Items => _blockingCollection.ToArray();

	public BlockingCollectionDebugView(BlockingCollection<T> collection)
	{
		if (collection == null)
		{
			throw new ArgumentNullException("collection");
		}
		_blockingCollection = collection;
	}
}
