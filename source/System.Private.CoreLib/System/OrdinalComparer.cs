using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Runtime.CompilerServices;

namespace System;

[Serializable]
[TypeForwardedFrom("mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
public class OrdinalComparer : StringComparer
{
	private readonly bool _ignoreCase;

	internal OrdinalComparer(bool ignoreCase)
	{
		_ignoreCase = ignoreCase;
	}

	public override int Compare(string? x, string? y)
	{
		if ((object)x == y)
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
		if (_ignoreCase)
		{
			return string.Compare(x, y, StringComparison.OrdinalIgnoreCase);
		}
		return string.CompareOrdinal(x, y);
	}

	public override bool Equals(string? x, string? y)
	{
		if ((object)x == y)
		{
			return true;
		}
		if (x == null || y == null)
		{
			return false;
		}
		if (_ignoreCase)
		{
			if (x.Length != y.Length)
			{
				return false;
			}
			return System.Globalization.Ordinal.EqualsIgnoreCase(ref x.GetRawStringData(), ref y.GetRawStringData(), x.Length);
		}
		return x.Equals(y);
	}

	public override int GetHashCode(string obj)
	{
		if (obj == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.obj);
		}
		if (_ignoreCase)
		{
			return obj.GetHashCodeOrdinalIgnoreCase();
		}
		return obj.GetHashCode();
	}

	public override bool Equals([NotNullWhen(true)] object? obj)
	{
		if (!(obj is OrdinalComparer ordinalComparer))
		{
			return false;
		}
		return _ignoreCase == ordinalComparer._ignoreCase;
	}

	public override int GetHashCode()
	{
		int hashCode = "OrdinalComparer".GetHashCode();
		if (!_ignoreCase)
		{
			return hashCode;
		}
		return ~hashCode;
	}

	private protected override bool IsWellKnownOrdinalComparerCore(out bool ignoreCase)
	{
		ignoreCase = _ignoreCase;
		return true;
	}
}
