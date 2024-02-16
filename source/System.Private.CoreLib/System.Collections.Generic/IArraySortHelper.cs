namespace System.Collections.Generic;

internal interface IArraySortHelper<TKey>
{
	void Sort(Span<TKey> keys, IComparer<TKey> comparer);

	int BinarySearch(TKey[] keys, int index, int length, TKey value, IComparer<TKey> comparer);
}
internal interface IArraySortHelper<TKey, TValue>
{
	void Sort(Span<TKey> keys, Span<TValue> values, IComparer<TKey> comparer);
}
