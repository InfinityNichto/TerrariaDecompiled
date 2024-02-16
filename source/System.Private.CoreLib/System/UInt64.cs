using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.Versioning;

namespace System;

[Serializable]
[CLSCompliant(false)]
[TypeForwardedFrom("mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
public readonly struct UInt64 : IComparable, IConvertible, ISpanFormattable, IFormattable, IComparable<ulong>, IEquatable<ulong>, IBinaryInteger<ulong>, IBinaryNumber<ulong>, IBitwiseOperators<ulong, ulong, ulong>, INumber<ulong>, IAdditionOperators<ulong, ulong, ulong>, IAdditiveIdentity<ulong, ulong>, IComparisonOperators<ulong, ulong>, IEqualityOperators<ulong, ulong>, IDecrementOperators<ulong>, IDivisionOperators<ulong, ulong, ulong>, IIncrementOperators<ulong>, IModulusOperators<ulong, ulong, ulong>, IMultiplicativeIdentity<ulong, ulong>, IMultiplyOperators<ulong, ulong, ulong>, ISpanParseable<ulong>, IParseable<ulong>, ISubtractionOperators<ulong, ulong, ulong>, IUnaryNegationOperators<ulong, ulong>, IUnaryPlusOperators<ulong, ulong>, IShiftOperators<ulong, ulong>, IMinMaxValue<ulong>, IUnsignedNumber<ulong>
{
	private readonly ulong m_value;

	public const ulong MaxValue = 18446744073709551615uL;

	public const ulong MinValue = 0uL;

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static ulong IAdditiveIdentity<ulong, ulong>.AdditiveIdentity => 0uL;

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static ulong IMinMaxValue<ulong>.MinValue => 0uL;

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static ulong IMinMaxValue<ulong>.MaxValue => ulong.MaxValue;

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static ulong IMultiplicativeIdentity<ulong, ulong>.MultiplicativeIdentity => 1uL;

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static ulong INumber<ulong>.One => 1uL;

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static ulong INumber<ulong>.Zero => 0uL;

	public int CompareTo(object? value)
	{
		if (value == null)
		{
			return 1;
		}
		if (value is ulong num)
		{
			if (this < num)
			{
				return -1;
			}
			if (this > num)
			{
				return 1;
			}
			return 0;
		}
		throw new ArgumentException(SR.Arg_MustBeUInt64);
	}

	public int CompareTo(ulong value)
	{
		if (this < value)
		{
			return -1;
		}
		if (this > value)
		{
			return 1;
		}
		return 0;
	}

	public override bool Equals([NotNullWhen(true)] object? obj)
	{
		if (!(obj is ulong))
		{
			return false;
		}
		return this == (ulong)obj;
	}

	[NonVersionable]
	public bool Equals(ulong obj)
	{
		return this == obj;
	}

	public override int GetHashCode()
	{
		return (int)this ^ (int)(this >> 32);
	}

	public override string ToString()
	{
		return Number.UInt64ToDecStr(this, -1);
	}

	public string ToString(IFormatProvider? provider)
	{
		return Number.UInt64ToDecStr(this, -1);
	}

	public string ToString(string? format)
	{
		return Number.FormatUInt64(this, format, null);
	}

	public string ToString(string? format, IFormatProvider? provider)
	{
		return Number.FormatUInt64(this, format, provider);
	}

	public bool TryFormat(Span<char> destination, out int charsWritten, ReadOnlySpan<char> format = default(ReadOnlySpan<char>), IFormatProvider? provider = null)
	{
		return Number.TryFormatUInt64(this, format, provider, destination, out charsWritten);
	}

	public static ulong Parse(string s)
	{
		if (s == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.s);
		}
		return Number.ParseUInt64(s, NumberStyles.Integer, NumberFormatInfo.CurrentInfo);
	}

	public static ulong Parse(string s, NumberStyles style)
	{
		NumberFormatInfo.ValidateParseStyleInteger(style);
		if (s == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.s);
		}
		return Number.ParseUInt64(s, style, NumberFormatInfo.CurrentInfo);
	}

	public static ulong Parse(string s, IFormatProvider? provider)
	{
		if (s == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.s);
		}
		return Number.ParseUInt64(s, NumberStyles.Integer, NumberFormatInfo.GetInstance(provider));
	}

	public static ulong Parse(string s, NumberStyles style, IFormatProvider? provider)
	{
		NumberFormatInfo.ValidateParseStyleInteger(style);
		if (s == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.s);
		}
		return Number.ParseUInt64(s, style, NumberFormatInfo.GetInstance(provider));
	}

	public static ulong Parse(ReadOnlySpan<char> s, NumberStyles style = NumberStyles.Integer, IFormatProvider? provider = null)
	{
		NumberFormatInfo.ValidateParseStyleInteger(style);
		return Number.ParseUInt64(s, style, NumberFormatInfo.GetInstance(provider));
	}

	public static bool TryParse([NotNullWhen(true)] string? s, out ulong result)
	{
		if (s == null)
		{
			result = 0uL;
			return false;
		}
		return Number.TryParseUInt64IntegerStyle(s, NumberStyles.Integer, NumberFormatInfo.CurrentInfo, out result) == Number.ParsingStatus.OK;
	}

	public static bool TryParse(ReadOnlySpan<char> s, out ulong result)
	{
		return Number.TryParseUInt64IntegerStyle(s, NumberStyles.Integer, NumberFormatInfo.CurrentInfo, out result) == Number.ParsingStatus.OK;
	}

	public static bool TryParse([NotNullWhen(true)] string? s, NumberStyles style, IFormatProvider? provider, out ulong result)
	{
		NumberFormatInfo.ValidateParseStyleInteger(style);
		if (s == null)
		{
			result = 0uL;
			return false;
		}
		return Number.TryParseUInt64(s, style, NumberFormatInfo.GetInstance(provider), out result) == Number.ParsingStatus.OK;
	}

	public static bool TryParse(ReadOnlySpan<char> s, NumberStyles style, IFormatProvider? provider, out ulong result)
	{
		NumberFormatInfo.ValidateParseStyleInteger(style);
		return Number.TryParseUInt64(s, style, NumberFormatInfo.GetInstance(provider), out result) == Number.ParsingStatus.OK;
	}

	public TypeCode GetTypeCode()
	{
		return TypeCode.UInt64;
	}

	bool IConvertible.ToBoolean(IFormatProvider provider)
	{
		return Convert.ToBoolean(this);
	}

	char IConvertible.ToChar(IFormatProvider provider)
	{
		return Convert.ToChar(this);
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
		return this;
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
		throw new InvalidCastException(SR.Format(SR.InvalidCast_FromTo, "UInt64", "DateTime"));
	}

	object IConvertible.ToType(Type type, IFormatProvider provider)
	{
		return Convert.DefaultToType(this, type, provider);
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static ulong IAdditionOperators<ulong, ulong, ulong>.operator +(ulong left, ulong right)
	{
		return left + right;
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static ulong IBinaryInteger<ulong>.LeadingZeroCount(ulong value)
	{
		return (ulong)BitOperations.LeadingZeroCount(value);
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static ulong IBinaryInteger<ulong>.PopCount(ulong value)
	{
		return (ulong)BitOperations.PopCount(value);
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static ulong IBinaryInteger<ulong>.RotateLeft(ulong value, int rotateAmount)
	{
		return BitOperations.RotateLeft(value, rotateAmount);
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static ulong IBinaryInteger<ulong>.RotateRight(ulong value, int rotateAmount)
	{
		return BitOperations.RotateRight(value, rotateAmount);
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static ulong IBinaryInteger<ulong>.TrailingZeroCount(ulong value)
	{
		return (ulong)BitOperations.TrailingZeroCount(value);
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static bool IBinaryNumber<ulong>.IsPow2(ulong value)
	{
		return BitOperations.IsPow2(value);
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static ulong IBinaryNumber<ulong>.Log2(ulong value)
	{
		return (ulong)BitOperations.Log2(value);
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static ulong IBitwiseOperators<ulong, ulong, ulong>.operator &(ulong left, ulong right)
	{
		return left & right;
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static ulong IBitwiseOperators<ulong, ulong, ulong>.operator |(ulong left, ulong right)
	{
		return left | right;
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static ulong IBitwiseOperators<ulong, ulong, ulong>.operator ^(ulong left, ulong right)
	{
		return left ^ right;
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static ulong IBitwiseOperators<ulong, ulong, ulong>.operator ~(ulong value)
	{
		return ~value;
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static bool IComparisonOperators<ulong, ulong>.operator <(ulong left, ulong right)
	{
		return left < right;
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static bool IComparisonOperators<ulong, ulong>.operator <=(ulong left, ulong right)
	{
		return left <= right;
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static bool IComparisonOperators<ulong, ulong>.operator >(ulong left, ulong right)
	{
		return left > right;
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static bool IComparisonOperators<ulong, ulong>.operator >=(ulong left, ulong right)
	{
		return left >= right;
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static ulong IDecrementOperators<ulong>.operator --(ulong value)
	{
		return --value;
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static ulong IDivisionOperators<ulong, ulong, ulong>.operator /(ulong left, ulong right)
	{
		return left / right;
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static bool IEqualityOperators<ulong, ulong>.operator ==(ulong left, ulong right)
	{
		return left == right;
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static bool IEqualityOperators<ulong, ulong>.operator !=(ulong left, ulong right)
	{
		return left != right;
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static ulong IIncrementOperators<ulong>.operator ++(ulong value)
	{
		return ++value;
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static ulong IModulusOperators<ulong, ulong, ulong>.operator %(ulong left, ulong right)
	{
		return left % right;
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static ulong IMultiplyOperators<ulong, ulong, ulong>.operator *(ulong left, ulong right)
	{
		return left * right;
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static ulong INumber<ulong>.Abs(ulong value)
	{
		return value;
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static ulong INumber<ulong>.Clamp(ulong value, ulong min, ulong max)
	{
		return Math.Clamp(value, min, max);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static ulong INumber<ulong>.Create<TOther>(TOther value)
	{
		if (typeof(TOther) == typeof(byte))
		{
			return (byte)(object)value;
		}
		if (typeof(TOther) == typeof(char))
		{
			return (char)(object)value;
		}
		if (typeof(TOther) == typeof(decimal))
		{
			return (ulong)(decimal)(object)value;
		}
		checked
		{
			if (typeof(TOther) == typeof(double))
			{
				return (ulong)(double)(object)value;
			}
			if (typeof(TOther) == typeof(short))
			{
				return (ulong)(short)(object)value;
			}
			if (typeof(TOther) == typeof(int))
			{
				return (ulong)(int)(object)value;
			}
			if (typeof(TOther) == typeof(long))
			{
				return (ulong)(long)(object)value;
			}
			if (typeof(TOther) == typeof(IntPtr))
			{
				return (ulong)(nint)(IntPtr)(object)value;
			}
			if (typeof(TOther) == typeof(sbyte))
			{
				return (ulong)(sbyte)(object)value;
			}
			if (typeof(TOther) == typeof(float))
			{
				return (ulong)(float)(object)value;
			}
			if (typeof(TOther) == typeof(ushort))
			{
				return (ushort)(object)value;
			}
			if (typeof(TOther) == typeof(uint))
			{
				return (uint)(object)value;
			}
			if (typeof(TOther) == typeof(ulong))
			{
				return (ulong)(object)value;
			}
		}
		if (typeof(TOther) == typeof(UIntPtr))
		{
			return (ulong)(UIntPtr)(object)value;
		}
		ThrowHelper.ThrowNotSupportedException();
		return 0uL;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static ulong INumber<ulong>.CreateSaturating<TOther>(TOther value)
	{
		if (typeof(TOther) == typeof(byte))
		{
			return (byte)(object)value;
		}
		if (typeof(TOther) == typeof(char))
		{
			return (char)(object)value;
		}
		if (typeof(TOther) == typeof(decimal))
		{
			decimal num = (decimal)(object)value;
			if (!(num > 18446744073709551615m))
			{
				if (!(num < 0m))
				{
					return (ulong)num;
				}
				return 0uL;
			}
			return ulong.MaxValue;
		}
		if (typeof(TOther) == typeof(double))
		{
			double num2 = (double)(object)value;
			if (!(num2 > 1.8446744073709552E+19))
			{
				if (!(num2 < 0.0))
				{
					return (ulong)num2;
				}
				return 0uL;
			}
			return ulong.MaxValue;
		}
		if (typeof(TOther) == typeof(short))
		{
			short num3 = (short)(object)value;
			if (num3 >= 0)
			{
				return (ulong)num3;
			}
			return 0uL;
		}
		if (typeof(TOther) == typeof(int))
		{
			int num4 = (int)(object)value;
			if (num4 >= 0)
			{
				return (ulong)num4;
			}
			return 0uL;
		}
		if (typeof(TOther) == typeof(long))
		{
			long num5 = (long)(object)value;
			if (num5 >= 0)
			{
				return (ulong)num5;
			}
			return 0uL;
		}
		if (typeof(TOther) == typeof(IntPtr))
		{
			IntPtr intPtr = (IntPtr)(object)value;
			if ((nint)intPtr >= 0)
			{
				return (ulong)(nint)intPtr;
			}
			return 0uL;
		}
		if (typeof(TOther) == typeof(sbyte))
		{
			sbyte b = (sbyte)(object)value;
			if (b >= 0)
			{
				return (ulong)b;
			}
			return 0uL;
		}
		if (typeof(TOther) == typeof(float))
		{
			float num6 = (float)(object)value;
			if (!(num6 > 1.8446744E+19f))
			{
				if (!(num6 < 0f))
				{
					return (ulong)num6;
				}
				return 0uL;
			}
			return ulong.MaxValue;
		}
		if (typeof(TOther) == typeof(ushort))
		{
			return (ushort)(object)value;
		}
		if (typeof(TOther) == typeof(uint))
		{
			return (uint)(object)value;
		}
		if (typeof(TOther) == typeof(ulong))
		{
			return (ulong)(object)value;
		}
		if (typeof(TOther) == typeof(UIntPtr))
		{
			return (ulong)(UIntPtr)(object)value;
		}
		ThrowHelper.ThrowNotSupportedException();
		return 0uL;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static ulong INumber<ulong>.CreateTruncating<TOther>(TOther value)
	{
		if (typeof(TOther) == typeof(byte))
		{
			return (byte)(object)value;
		}
		if (typeof(TOther) == typeof(char))
		{
			return (char)(object)value;
		}
		if (typeof(TOther) == typeof(decimal))
		{
			return (ulong)(decimal)(object)value;
		}
		if (typeof(TOther) == typeof(double))
		{
			return (ulong)(double)(object)value;
		}
		if (typeof(TOther) == typeof(short))
		{
			return (ulong)(short)(object)value;
		}
		if (typeof(TOther) == typeof(int))
		{
			return (ulong)(int)(object)value;
		}
		if (typeof(TOther) == typeof(long))
		{
			return (ulong)(long)(object)value;
		}
		if (typeof(TOther) == typeof(IntPtr))
		{
			return (ulong)(nint)(IntPtr)(object)value;
		}
		if (typeof(TOther) == typeof(sbyte))
		{
			return (ulong)(sbyte)(object)value;
		}
		if (typeof(TOther) == typeof(float))
		{
			return (ulong)(float)(object)value;
		}
		if (typeof(TOther) == typeof(ushort))
		{
			return (ushort)(object)value;
		}
		if (typeof(TOther) == typeof(uint))
		{
			return (uint)(object)value;
		}
		if (typeof(TOther) == typeof(ulong))
		{
			return (ulong)(object)value;
		}
		if (typeof(TOther) == typeof(UIntPtr))
		{
			return (ulong)(UIntPtr)(object)value;
		}
		ThrowHelper.ThrowNotSupportedException();
		return 0uL;
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static (ulong Quotient, ulong Remainder) INumber<ulong>.DivRem(ulong left, ulong right)
	{
		return Math.DivRem(left, right);
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static ulong INumber<ulong>.Max(ulong x, ulong y)
	{
		return Math.Max(x, y);
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static ulong INumber<ulong>.Min(ulong x, ulong y)
	{
		return Math.Min(x, y);
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static ulong INumber<ulong>.Parse(string s, NumberStyles style, IFormatProvider provider)
	{
		return Parse(s, style, provider);
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static ulong INumber<ulong>.Parse(ReadOnlySpan<char> s, NumberStyles style, IFormatProvider provider)
	{
		return Parse(s, style, provider);
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static ulong INumber<ulong>.Sign(ulong value)
	{
		return (ulong)((value != 0L) ? 1 : 0);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static bool INumber<ulong>.TryCreate<TOther>(TOther value, out ulong result)
	{
		if (typeof(TOther) == typeof(byte))
		{
			result = (byte)(object)value;
			return true;
		}
		if (typeof(TOther) == typeof(char))
		{
			result = (char)(object)value;
			return true;
		}
		if (typeof(TOther) == typeof(decimal))
		{
			decimal num = (decimal)(object)value;
			if (num < 0m || num > 18446744073709551615m)
			{
				result = 0uL;
				return false;
			}
			result = (ulong)num;
			return true;
		}
		if (typeof(TOther) == typeof(double))
		{
			double num2 = (double)(object)value;
			if (num2 < 0.0 || num2 > 1.8446744073709552E+19)
			{
				result = 0uL;
				return false;
			}
			result = (ulong)num2;
			return true;
		}
		if (typeof(TOther) == typeof(short))
		{
			short num3 = (short)(object)value;
			if (num3 < 0)
			{
				result = 0uL;
				return false;
			}
			result = (ulong)num3;
			return true;
		}
		if (typeof(TOther) == typeof(int))
		{
			int num4 = (int)(object)value;
			if (num4 < 0)
			{
				result = 0uL;
				return false;
			}
			result = (ulong)num4;
			return true;
		}
		if (typeof(TOther) == typeof(long))
		{
			long num5 = (long)(object)value;
			if (num5 < 0)
			{
				result = 0uL;
				return false;
			}
			result = (ulong)num5;
			return true;
		}
		if (typeof(TOther) == typeof(IntPtr))
		{
			IntPtr intPtr = (IntPtr)(object)value;
			if ((nint)intPtr < 0)
			{
				result = 0uL;
				return false;
			}
			result = (ulong)(nint)intPtr;
			return true;
		}
		if (typeof(TOther) == typeof(sbyte))
		{
			sbyte b = (sbyte)(object)value;
			if (b < 0)
			{
				result = 0uL;
				return false;
			}
			result = (ulong)b;
			return true;
		}
		if (typeof(TOther) == typeof(float))
		{
			float num6 = (float)(object)value;
			if (num6 < 0f || num6 > 1.8446744E+19f)
			{
				result = 0uL;
				return false;
			}
			result = (ulong)num6;
			return true;
		}
		if (typeof(TOther) == typeof(ushort))
		{
			result = (ushort)(object)value;
			return true;
		}
		if (typeof(TOther) == typeof(uint))
		{
			result = (uint)(object)value;
			return true;
		}
		if (typeof(TOther) == typeof(ulong))
		{
			result = (ulong)(object)value;
			return true;
		}
		if (typeof(TOther) == typeof(UIntPtr))
		{
			result = (ulong)(UIntPtr)(object)value;
			return true;
		}
		ThrowHelper.ThrowNotSupportedException();
		result = 0uL;
		return false;
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static bool INumber<ulong>.TryParse([NotNullWhen(true)] string s, NumberStyles style, IFormatProvider provider, out ulong result)
	{
		return TryParse(s, style, provider, out result);
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static bool INumber<ulong>.TryParse(ReadOnlySpan<char> s, NumberStyles style, IFormatProvider provider, out ulong result)
	{
		return TryParse(s, style, provider, out result);
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static ulong IParseable<ulong>.Parse(string s, IFormatProvider provider)
	{
		return Parse(s, provider);
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static bool IParseable<ulong>.TryParse([NotNullWhen(true)] string s, IFormatProvider provider, out ulong result)
	{
		return TryParse(s, NumberStyles.Integer, provider, out result);
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static ulong IShiftOperators<ulong, ulong>.operator <<(ulong value, int shiftAmount)
	{
		return value << shiftAmount;
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static ulong IShiftOperators<ulong, ulong>.operator >>(ulong value, int shiftAmount)
	{
		return value >> shiftAmount;
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static ulong ISpanParseable<ulong>.Parse(ReadOnlySpan<char> s, IFormatProvider provider)
	{
		return Parse(s, NumberStyles.Integer, provider);
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static bool ISpanParseable<ulong>.TryParse(ReadOnlySpan<char> s, IFormatProvider provider, out ulong result)
	{
		return TryParse(s, NumberStyles.Integer, provider, out result);
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static ulong ISubtractionOperators<ulong, ulong, ulong>.operator -(ulong left, ulong right)
	{
		return left - right;
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static ulong IUnaryNegationOperators<ulong, ulong>.operator -(ulong value)
	{
		return 0 - value;
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static ulong IUnaryPlusOperators<ulong, ulong>.operator +(ulong value)
	{
		return value;
	}
}
