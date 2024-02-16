using System.Collections;
using System.Collections.Generic;

namespace System.Linq;

internal interface IIListProvider<TElement> : IEnumerable<TElement>, IEnumerable
{
	TElement[] ToArray();

	List<TElement> ToList();

	int GetCount(bool onlyIfCheap);
}
