using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Runtime.CompilerServices;

namespace System;

[Serializable]
[TypeForwardedFrom("mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
public abstract class StringComparer : IComparer, IEqualityComparer, IComparer<string?>, IEqualityComparer<string?>
{
	public static StringComparer InvariantCulture => CultureAwareComparer.InvariantCaseSensitiveInstance;

	public static StringComparer InvariantCultureIgnoreCase => CultureAwareComparer.InvariantIgnoreCaseInstance;

	public static StringComparer CurrentCulture => new CultureAwareComparer(CultureInfo.CurrentCulture, CompareOptions.None);

	public static StringComparer CurrentCultureIgnoreCase => new CultureAwareComparer(CultureInfo.CurrentCulture, CompareOptions.IgnoreCase);

	public static StringComparer Ordinal => OrdinalCaseSensitiveComparer.Instance;

	public static StringComparer OrdinalIgnoreCase => OrdinalIgnoreCaseComparer.Instance;

	public static StringComparer FromComparison(StringComparison comparisonType)
	{
		return comparisonType switch
		{
			StringComparison.CurrentCulture => CurrentCulture, 
			StringComparison.CurrentCultureIgnoreCase => CurrentCultureIgnoreCase, 
			StringComparison.InvariantCulture => InvariantCulture, 
			StringComparison.InvariantCultureIgnoreCase => InvariantCultureIgnoreCase, 
			StringComparison.Ordinal => Ordinal, 
			StringComparison.OrdinalIgnoreCase => OrdinalIgnoreCase, 
			_ => throw new ArgumentException(SR.NotSupported_StringComparison, "comparisonType"), 
		};
	}

	public static StringComparer Create(CultureInfo culture, bool ignoreCase)
	{
		if (culture == null)
		{
			throw new ArgumentNullException("culture");
		}
		return new CultureAwareComparer(culture, ignoreCase ? CompareOptions.IgnoreCase : CompareOptions.None);
	}

	public static StringComparer Create(CultureInfo culture, CompareOptions options)
	{
		if (culture == null)
		{
			throw new ArgumentNullException("culture");
		}
		return new CultureAwareComparer(culture, options);
	}

	public static bool IsWellKnownOrdinalComparer(IEqualityComparer<string?>? comparer, out bool ignoreCase)
	{
		if (comparer is IInternalStringEqualityComparer internalStringEqualityComparer)
		{
			comparer = internalStringEqualityComparer.GetUnderlyingEqualityComparer();
		}
		if (!(comparer is StringComparer stringComparer))
		{
			if (comparer is GenericEqualityComparer<string>)
			{
				ignoreCase = false;
				return true;
			}
			ignoreCase = false;
			return false;
		}
		return stringComparer.IsWellKnownOrdinalComparerCore(out ignoreCase);
	}

	private protected virtual bool IsWellKnownOrdinalComparerCore(out bool ignoreCase)
	{
		ignoreCase = false;
		return false;
	}

	public static bool IsWellKnownCultureAwareComparer(IEqualityComparer<string?>? comparer, [NotNullWhen(true)] out CompareInfo? compareInfo, out CompareOptions compareOptions)
	{
		if (comparer is IInternalStringEqualityComparer internalStringEqualityComparer)
		{
			comparer = internalStringEqualityComparer.GetUnderlyingEqualityComparer();
		}
		if (comparer is StringComparer stringComparer)
		{
			return stringComparer.IsWellKnownCultureAwareComparerCore(out compareInfo, out compareOptions);
		}
		compareInfo = null;
		compareOptions = CompareOptions.None;
		return false;
	}

	private protected virtual bool IsWellKnownCultureAwareComparerCore([NotNullWhen(true)] out CompareInfo compareInfo, out CompareOptions compareOptions)
	{
		compareInfo = null;
		compareOptions = CompareOptions.None;
		return false;
	}

	public int Compare(object? x, object? y)
	{
		if (x == y)
		{
			return 0;
		}
		if (x == null)
		{
			return -1;
		}
		if (y == null)
		{
			return 1;
		}
		if (x is string x2 && y is string y2)
		{
			return Compare(x2, y2);
		}
		if (x is IComparable comparable)
		{
			return comparable.CompareTo(y);
		}
		throw new ArgumentException(SR.Argument_ImplementIComparable);
	}

	public new bool Equals(object? x, object? y)
	{
		if (x == y)
		{
			return true;
		}
		if (x == null || y == null)
		{
			return false;
		}
		if (x is string x2 && y is string y2)
		{
			return Equals(x2, y2);
		}
		return x.Equals(y);
	}

	public int GetHashCode(object obj)
	{
		if (obj == null)
		{
			throw new ArgumentNullException("obj");
		}
		if (obj is string obj2)
		{
			return GetHashCode(obj2);
		}
		return obj.GetHashCode();
	}

	public abstract int Compare(string? x, string? y);

	public abstract bool Equals(string? x, string? y);

	public abstract int GetHashCode(string obj);
}
