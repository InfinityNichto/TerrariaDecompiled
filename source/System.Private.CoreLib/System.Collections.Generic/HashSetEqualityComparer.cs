using System.Diagnostics.CodeAnalysis;

namespace System.Collections.Generic;

internal sealed class HashSetEqualityComparer<T> : IEqualityComparer<HashSet<T>>
{
	public bool Equals(HashSet<T> x, HashSet<T> y)
	{
		if (x == y)
		{
			return true;
		}
		if (x == null || y == null)
		{
			return false;
		}
		EqualityComparer<T> @default = EqualityComparer<T>.Default;
		if (HashSet<T>.EqualityComparersAreEqual(x, y))
		{
			if (x.Count == y.Count)
			{
				return y.IsSubsetOfHashSetWithSameComparer(x);
			}
			return false;
		}
		foreach (T item in y)
		{
			bool flag = false;
			foreach (T item2 in x)
			{
				if (@default.Equals(item, item2))
				{
					flag = true;
					break;
				}
			}
			if (!flag)
			{
				return false;
			}
		}
		return true;
	}

	public int GetHashCode(HashSet<T> obj)
	{
		int num = 0;
		if (obj != null)
		{
			foreach (T item in obj)
			{
				if (item != null)
				{
					num ^= item.GetHashCode();
				}
			}
		}
		return num;
	}

	public override bool Equals([NotNullWhen(true)] object obj)
	{
		return obj is HashSetEqualityComparer<T>;
	}

	public override int GetHashCode()
	{
		return EqualityComparer<T>.Default.GetHashCode();
	}
}
