using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace System.Linq;

[DebuggerDisplay("Count = 0")]
internal sealed class EmptyPartition<TElement> : IPartition<TElement>, IIListProvider<TElement>, IEnumerable<TElement>, IEnumerable, IEnumerator<TElement>, IEnumerator, IDisposable
{
	public static readonly IPartition<TElement> Instance = new EmptyPartition<TElement>();

	[ExcludeFromCodeCoverage(Justification = "Shouldn't be called, and as undefined can return or throw anything anyway")]
	public TElement Current => default(TElement);

	[ExcludeFromCodeCoverage(Justification = "Shouldn't be called, and as undefined can return or throw anything anyway")]
	object IEnumerator.Current => null;

	private EmptyPartition()
	{
	}

	public IEnumerator<TElement> GetEnumerator()
	{
		return this;
	}

	IEnumerator IEnumerable.GetEnumerator()
	{
		return this;
	}

	public bool MoveNext()
	{
		return false;
	}

	void IEnumerator.Reset()
	{
	}

	void IDisposable.Dispose()
	{
	}

	public IPartition<TElement> Skip(int count)
	{
		return this;
	}

	public IPartition<TElement> Take(int count)
	{
		return this;
	}

	public TElement TryGetElementAt(int index, out bool found)
	{
		found = false;
		return default(TElement);
	}

	public TElement TryGetFirst(out bool found)
	{
		found = false;
		return default(TElement);
	}

	public TElement TryGetLast(out bool found)
	{
		found = false;
		return default(TElement);
	}

	public TElement[] ToArray()
	{
		return Array.Empty<TElement>();
	}

	public List<TElement> ToList()
	{
		return new List<TElement>();
	}

	public int GetCount(bool onlyIfCheap)
	{
		return 0;
	}
}
