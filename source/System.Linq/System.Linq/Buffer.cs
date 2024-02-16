using System.Collections.Generic;

namespace System.Linq;

internal readonly struct Buffer<TElement>
{
	internal readonly TElement[] _items;

	internal readonly int _count;

	internal Buffer(IEnumerable<TElement> source)
	{
		if (source is IIListProvider<TElement> iIListProvider)
		{
			_count = (_items = iIListProvider.ToArray()).Length;
		}
		else
		{
			_items = System.Collections.Generic.EnumerableHelpers.ToArray(source, out _count);
		}
	}
}
