using System.Collections.Generic;

namespace System.Collections.Immutable;

public interface IImmutableList<T> : IReadOnlyList<T>, IEnumerable<T>, IEnumerable, IReadOnlyCollection<T>
{
	IImmutableList<T> Clear();

	int IndexOf(T item, int index, int count, IEqualityComparer<T>? equalityComparer);

	int LastIndexOf(T item, int index, int count, IEqualityComparer<T>? equalityComparer);

	IImmutableList<T> Add(T value);

	IImmutableList<T> AddRange(IEnumerable<T> items);

	IImmutableList<T> Insert(int index, T element);

	IImmutableList<T> InsertRange(int index, IEnumerable<T> items);

	IImmutableList<T> Remove(T value, IEqualityComparer<T>? equalityComparer);

	IImmutableList<T> RemoveAll(Predicate<T> match);

	IImmutableList<T> RemoveRange(IEnumerable<T> items, IEqualityComparer<T>? equalityComparer);

	IImmutableList<T> RemoveRange(int index, int count);

	IImmutableList<T> RemoveAt(int index);

	IImmutableList<T> SetItem(int index, T value);

	IImmutableList<T> Replace(T oldValue, T newValue, IEqualityComparer<T>? equalityComparer);
}
