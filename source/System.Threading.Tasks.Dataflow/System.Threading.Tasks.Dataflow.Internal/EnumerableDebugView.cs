using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace System.Threading.Tasks.Dataflow.Internal;

internal sealed class EnumerableDebugView<TKey, TValue>
{
	private readonly IEnumerable<KeyValuePair<TKey, TValue>> _enumerable;

	[DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
	public KeyValuePair<TKey, TValue>[] Items => _enumerable.ToArray();

	public EnumerableDebugView(IEnumerable<KeyValuePair<TKey, TValue>> enumerable)
	{
		_enumerable = enumerable;
	}
}
