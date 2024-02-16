using System.Collections.Generic;

namespace System.Runtime.InteropServices;

public static class CollectionsMarshal
{
	public static Span<T> AsSpan<T>(List<T>? list)
	{
		if (list != null)
		{
			return new Span<T>(list._items, 0, list._size);
		}
		return default(Span<T>);
	}

	public static ref TValue GetValueRefOrNullRef<TKey, TValue>(Dictionary<TKey, TValue> dictionary, TKey key) where TKey : notnull
	{
		return ref dictionary.FindValue(key);
	}

	public static ref TValue? GetValueRefOrAddDefault<TKey, TValue>(Dictionary<TKey, TValue> dictionary, TKey key, out bool exists) where TKey : notnull
	{
		return ref Dictionary<TKey, TValue>.CollectionsMarshalHelper.GetValueRefOrAddDefault(dictionary, key, out exists);
	}
}
