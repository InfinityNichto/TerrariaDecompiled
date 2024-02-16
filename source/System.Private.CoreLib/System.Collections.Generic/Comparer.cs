using System.Runtime.CompilerServices;

namespace System.Collections.Generic;

[Serializable]
[TypeDependency("System.Collections.Generic.ObjectComparer`1")]
[TypeForwardedFrom("mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
public abstract class Comparer<T> : IComparer, IComparer<T>
{
	public static Comparer<T> Default
	{
		[Intrinsic]
		get;
	} = (Comparer<T>)ComparerHelpers.CreateDefaultComparer(typeof(T));


	public static Comparer<T> Create(Comparison<T> comparison)
	{
		if (comparison == null)
		{
			throw new ArgumentNullException("comparison");
		}
		return new ComparisonComparer<T>(comparison);
	}

	public abstract int Compare(T? x, T? y);

	int IComparer.Compare(object x, object y)
	{
		if (x == null)
		{
			if (y != null)
			{
				return -1;
			}
			return 0;
		}
		if (y == null)
		{
			return 1;
		}
		if (x is T && y is T)
		{
			return Compare((T)x, (T)y);
		}
		ThrowHelper.ThrowArgumentException(ExceptionResource.Argument_InvalidArgumentForComparison);
		return 0;
	}
}
