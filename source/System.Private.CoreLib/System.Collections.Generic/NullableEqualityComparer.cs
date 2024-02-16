using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace System.Collections.Generic;

[Serializable]
[TypeForwardedFrom("mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
public sealed class NullableEqualityComparer<T> : EqualityComparer<T?> where T : struct, IEquatable<T>
{
	internal override int IndexOf(T?[] array, T? value, int startIndex, int count)
	{
		int num = startIndex + count;
		if (!value.HasValue)
		{
			for (int i = startIndex; i < num; i++)
			{
				if (!array[i].HasValue)
				{
					return i;
				}
			}
		}
		else
		{
			for (int j = startIndex; j < num; j++)
			{
				if (array[j].HasValue && array[j].value.Equals(value.value))
				{
					return j;
				}
			}
		}
		return -1;
	}

	internal override int LastIndexOf(T?[] array, T? value, int startIndex, int count)
	{
		int num = startIndex - count + 1;
		if (!value.HasValue)
		{
			for (int num2 = startIndex; num2 >= num; num2--)
			{
				if (!array[num2].HasValue)
				{
					return num2;
				}
			}
		}
		else
		{
			for (int num3 = startIndex; num3 >= num; num3--)
			{
				if (array[num3].HasValue && array[num3].value.Equals(value.value))
				{
					return num3;
				}
			}
		}
		return -1;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public override bool Equals(T? x, T? y)
	{
		if (x.HasValue)
		{
			if (y.HasValue)
			{
				return x.value.Equals(y.value);
			}
			return false;
		}
		if (y.HasValue)
		{
			return false;
		}
		return true;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public override int GetHashCode(T? obj)
	{
		return obj.GetHashCode();
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
