using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace System.Collections.Generic;

[Serializable]
[TypeForwardedFrom("mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
public sealed class NullableComparer<T> : Comparer<T?> where T : struct, IComparable<T>
{
	public override int Compare(T? x, T? y)
	{
		if (x.HasValue)
		{
			if (y.HasValue)
			{
				return x.value.CompareTo(y.value);
			}
			return 1;
		}
		if (y.HasValue)
		{
			return -1;
		}
		return 0;
	}

	public override bool Equals([NotNullWhen(true)] object? obj)
	{
		if (obj != null)
		{
			return GetType() == obj.GetType();
		}
		return false;
	}

	public override int GetHashCode()
	{
		return GetType().GetHashCode();
	}
}
