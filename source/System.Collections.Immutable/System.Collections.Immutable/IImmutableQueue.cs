using System.Collections.Generic;

namespace System.Collections.Immutable;

public interface IImmutableQueue<T> : IEnumerable<T>, IEnumerable
{
	bool IsEmpty { get; }

	IImmutableQueue<T> Clear();

	T Peek();

	IImmutableQueue<T> Enqueue(T value);

	IImmutableQueue<T> Dequeue();
}
