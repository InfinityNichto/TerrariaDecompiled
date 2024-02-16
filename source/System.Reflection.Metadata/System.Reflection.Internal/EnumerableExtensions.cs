using System.Collections.Generic;
using System.Collections.Immutable;

namespace System.Reflection.Internal;

internal static class EnumerableExtensions
{
	public static T? FirstOrDefault<T>(this ImmutableArray<T> collection, Func<T, bool> predicate)
	{
		ImmutableArray<T>.Enumerator enumerator = collection.GetEnumerator();
		while (enumerator.MoveNext())
		{
			T current = enumerator.Current;
			if (predicate(current))
			{
				return current;
			}
		}
		return default(T);
	}

	public static IEnumerable<TResult> Select<TSource, TResult>(this IEnumerable<TSource> source, Func<TSource, TResult> selector)
	{
		foreach (TSource item in source)
		{
			yield return selector(item);
		}
	}

	public static T Last<T>(this ImmutableArray<T>.Builder source)
	{
		return source[source.Count - 1];
	}

	public static IEnumerable<T> OrderBy<T>(this List<T> source, Comparison<T> comparison)
	{
		Comparison<T> comparison2 = comparison;
		List<T> source2 = source;
		int[] array = new int[source2.Count];
		for (int i = 0; i < array.Length; i++)
		{
			array[i] = i;
		}
		Array.Sort(array, delegate(int left, int right)
		{
			if (left == right)
			{
				return 0;
			}
			int num = comparison2(source2[left], source2[right]);
			return (num == 0) ? (left - right) : num;
		});
		int[] array2 = array;
		foreach (int index in array2)
		{
			yield return source2[index];
		}
	}
}
