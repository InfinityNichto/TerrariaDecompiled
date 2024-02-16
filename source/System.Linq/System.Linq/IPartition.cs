using System.Collections;
using System.Collections.Generic;

namespace System.Linq;

internal interface IPartition<TElement> : IIListProvider<TElement>, IEnumerable<TElement>, IEnumerable
{
	IPartition<TElement> Skip(int count);

	IPartition<TElement> Take(int count);

	TElement TryGetElementAt(int index, out bool found);

	TElement TryGetFirst(out bool found);

	TElement TryGetLast(out bool found);
}
