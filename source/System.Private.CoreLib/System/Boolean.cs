using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.Versioning;

namespace System;

[Serializable]
[TypeForwardedFrom("mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
public readonly struct Boolean : IComparable, IConvertible, IComparable<bool>, IEquatable<bool>
{
	private readonly bool m_value;

	public static readonly string TrueString = "True";

	public static readonly string FalseString = "False";

	public override int GetHashCode()
	{
		if (!this)
		{
			return 0;
		}
		return 1;
	}

	public override string ToString()
	{
		if (!this)
		{
			return "False";
		}
		return "True";
	}

	public string ToString(IFormatProvider? provider)
	{
		return ToString();
	}

	public bool TryFormat(Span<char> destination, out int charsWritten)
	{
		if (this)
		{
			if ((uint)destination.Length > 3u)
			{
				destination[0] = 'T';
				destination[1] = 'r';
				destination[2] = 'u';
				destination[3] = 'e';
				charsWritten = 4;
				return true;
			}
		}
		else if ((uint)destination.Length > 4u)
		{
			destination[0] = 'F';
			destination[1] = 'a';
			destination[2] = 'l';
			destination[3] = 's';
			destination[4] = 'e';
			charsWritten = 5;
			return true;
		}
		charsWritten = 0;
		return false;
	}

	public override bool Equals([NotNullWhen(true)] object? obj)
	{
		if (!(obj is bool))
		{
			return false;
		}
		return this == (bool)obj;
	}

	[NonVersionable]
	public bool Equals(bool obj)
	{
		return this == obj;
	}

	public int CompareTo(object? obj)
	{
		if (obj == null)
		{
			return 1;
		}
		if (!(obj is bool))
		{
			throw new ArgumentException(SR.Arg_MustBeBoolean);
		}
		if (this == (bool)obj)
		{
			return 0;
		}
		if (!this)
		{
			return -1;
		}
		return 1;
	}

	public int CompareTo(bool value)
	{
		if (this == value)
		{
			return 0;
		}
		if (!this)
		{
			return -1;
		}
		return 1;
	}

	internal static bool IsTrueStringIgnoreCase(ReadOnlySpan<char> value)
	{
		if (value.Length == 4 && (value[0] == 't' || value[0] == 'T') && (value[1] == 'r' || value[1] == 'R') && (value[2] == 'u' || value[2] == 'U'))
		{
			if (value[3] != 'e')
			{
				return value[3] == 'E';
			}
			return true;
		}
		return false;
	}

	internal static bool IsFalseStringIgnoreCase(ReadOnlySpan<char> value)
	{
		if (value.Length == 5 && (value[0] == 'f' || value[0] == 'F') && (value[1] == 'a' || value[1] == 'A') && (value[2] == 'l' || value[2] == 'L') && (value[3] == 's' || value[3] == 'S'))
		{
			if (value[4] != 'e')
			{
				return value[4] == 'E';
			}
			return true;
		}
		return false;
	}

	public static bool Parse(string value)
	{
		if (value == null)
		{
			throw new ArgumentNullException("value");
		}
		return Parse(value.AsSpan());
	}

	public static bool Parse(ReadOnlySpan<char> value)
	{
		if (!TryParse(value, out var result))
		{
			throw new FormatException(SR.Format(SR.Format_BadBoolean, new string(value)));
		}
		return result;
	}

	public static bool TryParse([NotNullWhen(true)] string? value, out bool result)
	{
		if (value == null)
		{
			result = false;
			return false;
		}
		return TryParse(value.AsSpan(), out result);
	}

	public static bool TryParse(ReadOnlySpan<char> value, out bool result)
	{
		if (IsTrueStringIgnoreCase(value))
		{
			result = true;
			return true;
		}
		if (IsFalseStringIgnoreCase(value))
		{
			result = false;
			return true;
		}
		value = TrimWhiteSpaceAndNull(value);
		if (IsTrueStringIgnoreCase(value))
		{
			result = true;
			return true;
		}
		if (IsFalseStringIgnoreCase(value))
		{
			result = false;
			return true;
		}
		result = false;
		return false;
	}

	private static ReadOnlySpan<char> TrimWhiteSpaceAndNull(ReadOnlySpan<char> value)
	{
		int i;
		for (i = 0; i < value.Length && (char.IsWhiteSpace(value[i]) || value[i] == '\0'); i++)
		{
		}
		int num = value.Length - 1;
		while (num >= i && (char.IsWhiteSpace(value[num]) || value[num] == '\0'))
		{
			num--;
		}
		return value.Slice(i, num - i + 1);
	}

	public TypeCode GetTypeCode()
	{
		return TypeCode.Boolean;
	}

	bool IConvertible.ToBoolean(IFormatProvider provider)
	{
		return this;
	}

	char IConvertible.ToChar(IFormatProvider provider)
	{
		throw new InvalidCastException(SR.Format(SR.InvalidCast_FromTo, "Boolean", "Char"));
	}

	sbyte IConvertible.ToSByte(IFormatProvider provider)
	{
		return Convert.ToSByte(this);
	}

	byte IConvertible.ToByte(IFormatProvider provider)
	{
		return Convert.ToByte(this);
	}

	short IConvertible.ToInt16(IFormatProvider provider)
	{
		return Convert.ToInt16(this);
	}

	ushort IConvertible.ToUInt16(IFormatProvider provider)
	{
		return Convert.ToUInt16(this);
	}

	int IConvertible.ToInt32(IFormatProvider provider)
	{
		return Convert.ToInt32(this);
	}

	uint IConvertible.ToUInt32(IFormatProvider provider)
	{
		return Convert.ToUInt32(this);
	}

	long IConvertible.ToInt64(IFormatProvider provider)
	{
		return Convert.ToInt64(this);
	}

	ulong IConvertible.ToUInt64(IFormatProvider provider)
	{
		return Convert.ToUInt64(this);
	}

	float IConvertible.ToSingle(IFormatProvider provider)
	{
		return Convert.ToSingle(this);
	}

	double IConvertible.ToDouble(IFormatProvider provider)
	{
		return Convert.ToDouble(this);
	}

	decimal IConvertible.ToDecimal(IFormatProvider provider)
	{
		return Convert.ToDecimal(this);
	}

	DateTime IConvertible.ToDateTime(IFormatProvider provider)
	{
		throw new InvalidCastException(SR.Format(SR.InvalidCast_FromTo, "Boolean", "DateTime"));
	}

	object IConvertible.ToType(Type type, IFormatProvider provider)
	{
		return Convert.DefaultToType(this, type, provider);
	}
}
