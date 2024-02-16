namespace System.Collections.Generic;

internal static class EnumerableHelpers
{
	internal static T[] ToArray<T>(IEnumerable<T> source, out int length)
	{
		if (source is ICollection<T> { Count: var count } collection)
		{
			if (count != 0)
			{
				T[] array = new T[count];
				collection.CopyTo(array, 0);
				length = count;
				return array;
			}
		}
		else
		{
			using IEnumerator<T> enumerator = source.GetEnumerator();
			if (enumerator.MoveNext())
			{
				T[] array2 = new T[4]
				{
					enumerator.Current,
					default(T),
					default(T),
					default(T)
				};
				int num = 1;
				while (enumerator.MoveNext())
				{
					if (num == array2.Length)
					{
						int num2 = num << 1;
						if ((uint)num2 > Array.MaxLength)
						{
							num2 = ((Array.MaxLength <= num) ? (num + 1) : Array.MaxLength);
						}
						Array.Resize(ref array2, num2);
					}
					array2[num++] = enumerator.Current;
				}
				length = num;
				return array2;
			}
		}
		length = 0;
		return Array.Empty<T>();
	}

	internal static void Copy<T>(IEnumerable<T> source, T[] array, int arrayIndex, int count)
	{
		if (source is ICollection<T> collection)
		{
			collection.CopyTo(array, arrayIndex);
		}
		else
		{
			IterativeCopy(source, array, arrayIndex, count);
		}
	}

	internal static void IterativeCopy<T>(IEnumerable<T> source, T[] array, int arrayIndex, int count)
	{
		int num = arrayIndex + count;
		foreach (T item in source)
		{
			array[arrayIndex++] = item;
		}
	}

	internal static T[] ToArray<T>(IEnumerable<T> source)
	{
		if (source is ICollection<T> { Count: var count } collection)
		{
			if (count == 0)
			{
				return Array.Empty<T>();
			}
			T[] array = new T[count];
			collection.CopyTo(array, 0);
			return array;
		}
		LargeArrayBuilder<T> largeArrayBuilder = new LargeArrayBuilder<T>(initialize: true);
		largeArrayBuilder.AddRange(source);
		return largeArrayBuilder.ToArray();
	}
}
