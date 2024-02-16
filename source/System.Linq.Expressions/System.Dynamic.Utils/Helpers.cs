using System.Collections.Generic;

namespace System.Dynamic.Utils;

internal static class Helpers
{
	internal static T CommonNode<T>(T first, T second, Func<T, T> parent) where T : class
	{
		EqualityComparer<T> @default = EqualityComparer<T>.Default;
		if (@default.Equals(first, second))
		{
			return first;
		}
		HashSet<T> hashSet = new HashSet<T>(@default);
		for (T val = first; val != null; val = parent(val))
		{
			hashSet.Add(val);
		}
		for (T val2 = second; val2 != null; val2 = parent(val2))
		{
			if (hashSet.Contains(val2))
			{
				return val2;
			}
		}
		return null;
	}

	internal static void IncrementCount<T>(T key, Dictionary<T, int> dict)
	{
		dict.TryGetValue(key, out var value);
		dict[key] = value + 1;
	}
}
