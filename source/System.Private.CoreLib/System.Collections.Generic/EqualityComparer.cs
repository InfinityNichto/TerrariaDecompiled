using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace System.Collections.Generic;

[Serializable]
[TypeDependency("System.Collections.Generic.ObjectEqualityComparer`1")]
[TypeForwardedFrom("mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
public abstract class EqualityComparer<T> : IEqualityComparer, IEqualityComparer<T>
{
	public static EqualityComparer<T> Default
	{
		[Intrinsic]
		get;
	} = (EqualityComparer<T>)ComparerHelpers.CreateDefaultEqualityComparer(typeof(T));


	public abstract bool Equals(T? x, T? y);

	public abstract int GetHashCode([DisallowNull] T obj);

	int IEqualityComparer.GetHashCode(object obj)
	{
		if (obj == null)
		{
			return 0;
		}
		if (obj is T)
		{
			return GetHashCode((T)obj);
		}
		ThrowHelper.ThrowArgumentException(ExceptionResource.Argument_InvalidArgumentForComparison);
		return 0;
	}

	bool IEqualityComparer.Equals(object x, object y)
	{
		if (x == y)
		{
			return true;
		}
		if (x == null || y == null)
		{
			return false;
		}
		if (x is T && y is T)
		{
			return Equals((T)x, (T)y);
		}
		ThrowHelper.ThrowArgumentException(ExceptionResource.Argument_InvalidArgumentForComparison);
		return false;
	}

	internal virtual int IndexOf(T[] array, T value, int startIndex, int count)
	{
		int num = startIndex + count;
		for (int i = startIndex; i < num; i++)
		{
			if (Equals(array[i], value))
			{
				return i;
			}
		}
		return -1;
	}

	internal virtual int LastIndexOf(T[] array, T value, int startIndex, int count)
	{
		int num = startIndex - count + 1;
		for (int num2 = startIndex; num2 >= num; num2--)
		{
			if (Equals(array[num2], value))
			{
				return num2;
			}
		}
		return -1;
	}
}
