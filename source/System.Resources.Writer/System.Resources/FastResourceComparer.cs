using System.Collections.Generic;

namespace System.Resources;

internal sealed class FastResourceComparer : IComparer<string>, IEqualityComparer<string>
{
	internal static readonly System.Resources.FastResourceComparer Default = new System.Resources.FastResourceComparer();

	public int GetHashCode(string key)
	{
		return HashFunction(key);
	}

	internal static int HashFunction(string key)
	{
		uint num = 5381u;
		for (int i = 0; i < key.Length; i++)
		{
			num = ((num << 5) + num) ^ key[i];
		}
		return (int)num;
	}

	public int Compare(string a, string b)
	{
		return string.CompareOrdinal(a, b);
	}

	public bool Equals(string a, string b)
	{
		return string.Equals(a, b);
	}
}
