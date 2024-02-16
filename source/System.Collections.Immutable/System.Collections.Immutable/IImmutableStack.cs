using System.Collections.Generic;

namespace System.Collections.Immutable;

public interface IImmutableStack<T> : IEnumerable<T>, IEnumerable
{
	bool IsEmpty { get; }

	IImmutableStack<T> Clear();

	IImmutableStack<T> Push(T value);

	IImmutableStack<T> Pop();

	T Peek();
}
