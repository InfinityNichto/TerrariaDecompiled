using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Runtime.Versioning;
using Internal.Runtime.CompilerServices;

namespace System;

[Serializable]
[TypeForwardedFrom("mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
public readonly struct Double : IComparable, IConvertible, ISpanFormattable, IFormattable, IComparable<double>, IEquatable<double>, IBinaryFloatingPoint<double>, IBinaryNumber<double>, IBitwiseOperators<double, double, double>, INumber<double>, IAdditionOperators<double, double, double>, IAdditiveIdentity<double, double>, IComparisonOperators<double, double>, IEqualityOperators<double, double>, IDecrementOperators<double>, IDivisionOperators<double, double, double>, IIncrementOperators<double>, IModulusOperators<double, double, double>, IMultiplicativeIdentity<double, double>, IMultiplyOperators<double, double, double>, ISpanParseable<double>, IParseable<double>, ISubtractionOperators<double, double, double>, IUnaryNegationOperators<double, double>, IUnaryPlusOperators<double, double>, IFloatingPoint<double>, ISignedNumber<double>, IMinMaxValue<double>
{
	private readonly double m_value;

	public const double MinValue = -1.7976931348623157E+308;

	public const double MaxValue = 1.7976931348623157E+308;

	public const double Epsilon = 5E-324;

	public const double NegativeInfinity = -1.0 / 0.0;

	public const double PositiveInfinity = 1.0 / 0.0;

	public const double NaN = 0.0 / 0.0;

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static double IAdditiveIdentity<double, double>.AdditiveIdentity => 0.0;

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static double IFloatingPoint<double>.E => Math.E;

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static double IFloatingPoint<double>.Epsilon => double.Epsilon;

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static double IFloatingPoint<double>.NaN => double.NaN;

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static double IFloatingPoint<double>.NegativeInfinity => double.NegativeInfinity;

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static double IFloatingPoint<double>.NegativeZero => -0.0;

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static double IFloatingPoint<double>.Pi => Math.PI;

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static double IFloatingPoint<double>.PositiveInfinity => double.PositiveInfinity;

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static double IFloatingPoint<double>.Tau => Math.PI * 2.0;

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static double IMinMaxValue<double>.MinValue => double.MinValue;

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static double IMinMaxValue<double>.MaxValue => double.MaxValue;

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static double IMultiplicativeIdentity<double, double>.MultiplicativeIdentity => 1.0;

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static double INumber<double>.One => 1.0;

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static double INumber<double>.Zero => 0.0;

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static double ISignedNumber<double>.NegativeOne => -1.0;

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[NonVersionable]
	public static bool IsFinite(double d)
	{
		long num = BitConverter.DoubleToInt64Bits(d);
		return (num & 0x7FFFFFFFFFFFFFFFL) < 9218868437227405312L;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[NonVersionable]
	public static bool IsInfinity(double d)
	{
		long num = BitConverter.DoubleToInt64Bits(d);
		return (num & 0x7FFFFFFFFFFFFFFFL) == 9218868437227405312L;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[NonVersionable]
	public static bool IsNaN(double d)
	{
		return d != d;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[NonVersionable]
	public static bool IsNegative(double d)
	{
		return BitConverter.DoubleToInt64Bits(d) < 0;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[NonVersionable]
	public static bool IsNegativeInfinity(double d)
	{
		return d == double.NegativeInfinity;
	}

	[NonVersionable]
	public static bool IsNormal(double d)
	{
		long num = BitConverter.DoubleToInt64Bits(d);
		num &= 0x7FFFFFFFFFFFFFFFL;
		if (num < 9218868437227405312L && num != 0L)
		{
			return (num & 0x7FF0000000000000L) != 0;
		}
		return false;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[NonVersionable]
	public static bool IsPositiveInfinity(double d)
	{
		return d == double.PositiveInfinity;
	}

	[NonVersionable]
	public static bool IsSubnormal(double d)
	{
		long num = BitConverter.DoubleToInt64Bits(d);
		num &= 0x7FFFFFFFFFFFFFFFL;
		if (num < 9218868437227405312L && num != 0L)
		{
			return (num & 0x7FF0000000000000L) == 0;
		}
		return false;
	}

	internal static int ExtractExponentFromBits(ulong bits)
	{
		return (int)(bits >> 52) & 0x7FF;
	}

	internal static ulong ExtractSignificandFromBits(ulong bits)
	{
		return bits & 0xFFFFFFFFFFFFFuL;
	}

	public int CompareTo(object? value)
	{
		if (value == null)
		{
			return 1;
		}
		if (value is double num)
		{
			if (this < num)
			{
				return -1;
			}
			if (this > num)
			{
				return 1;
			}
			if (this == num)
			{
				return 0;
			}
			if (IsNaN(this))
			{
				if (!IsNaN(num))
				{
					return -1;
				}
				return 0;
			}
			return 1;
		}
		throw new ArgumentException(SR.Arg_MustBeDouble);
	}

	public int CompareTo(double value)
	{
		if (this < value)
		{
			return -1;
		}
		if (this > value)
		{
			return 1;
		}
		if (this == value)
		{
			return 0;
		}
		if (IsNaN(this))
		{
			if (!IsNaN(value))
			{
				return -1;
			}
			return 0;
		}
		return 1;
	}

	public override bool Equals([NotNullWhen(true)] object? obj)
	{
		if (!(obj is double num))
		{
			return false;
		}
		if (num == this)
		{
			return true;
		}
		if (IsNaN(num))
		{
			return IsNaN(this);
		}
		return false;
	}

	[NonVersionable]
	public static bool operator ==(double left, double right)
	{
		return left == right;
	}

	[NonVersionable]
	public static bool operator !=(double left, double right)
	{
		return left != right;
	}

	[NonVersionable]
	public static bool operator <(double left, double right)
	{
		return left < right;
	}

	[NonVersionable]
	public static bool operator >(double left, double right)
	{
		return left > right;
	}

	[NonVersionable]
	public static bool operator <=(double left, double right)
	{
		return left <= right;
	}

	[NonVersionable]
	public static bool operator >=(double left, double right)
	{
		return left >= right;
	}

	public bool Equals(double obj)
	{
		if (obj == this)
		{
			return true;
		}
		if (IsNaN(obj))
		{
			return IsNaN(this);
		}
		return false;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public override int GetHashCode()
	{
		long num = Unsafe.As<double, long>(ref Unsafe.AsRef(in m_value));
		if (((num - 1) & 0x7FFFFFFFFFFFFFFFL) >= 9218868437227405312L)
		{
			num &= 0x7FF0000000000000L;
		}
		return (int)num ^ (int)(num >> 32);
	}

	public override string ToString()
	{
		return Number.FormatDouble(this, null, NumberFormatInfo.CurrentInfo);
	}

	public string ToString(string? format)
	{
		return Number.FormatDouble(this, format, NumberFormatInfo.CurrentInfo);
	}

	public string ToString(IFormatProvider? provider)
	{
		return Number.FormatDouble(this, null, NumberFormatInfo.GetInstance(provider));
	}

	public string ToString(string? format, IFormatProvider? provider)
	{
		return Number.FormatDouble(this, format, NumberFormatInfo.GetInstance(provider));
	}

	public bool TryFormat(Span<char> destination, out int charsWritten, ReadOnlySpan<char> format = default(ReadOnlySpan<char>), IFormatProvider? provider = null)
	{
		return Number.TryFormatDouble(this, format, NumberFormatInfo.GetInstance(provider), destination, out charsWritten);
	}

	public static double Parse(string s)
	{
		if (s == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.s);
		}
		return Number.ParseDouble(s, NumberStyles.Float | NumberStyles.AllowThousands, NumberFormatInfo.CurrentInfo);
	}

	public static double Parse(string s, NumberStyles style)
	{
		NumberFormatInfo.ValidateParseStyleFloatingPoint(style);
		if (s == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.s);
		}
		return Number.ParseDouble(s, style, NumberFormatInfo.CurrentInfo);
	}

	public static double Parse(string s, IFormatProvider? provider)
	{
		if (s == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.s);
		}
		return Number.ParseDouble(s, NumberStyles.Float | NumberStyles.AllowThousands, NumberFormatInfo.GetInstance(provider));
	}

	public static double Parse(string s, NumberStyles style, IFormatProvider? provider)
	{
		NumberFormatInfo.ValidateParseStyleFloatingPoint(style);
		if (s == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.s);
		}
		return Number.ParseDouble(s, style, NumberFormatInfo.GetInstance(provider));
	}

	public static double Parse(ReadOnlySpan<char> s, NumberStyles style = NumberStyles.Float | NumberStyles.AllowThousands, IFormatProvider? provider = null)
	{
		NumberFormatInfo.ValidateParseStyleFloatingPoint(style);
		return Number.ParseDouble(s, style, NumberFormatInfo.GetInstance(provider));
	}

	public static bool TryParse([NotNullWhen(true)] string? s, out double result)
	{
		if (s == null)
		{
			result = 0.0;
			return false;
		}
		return TryParse((ReadOnlySpan<char>)s, NumberStyles.Float | NumberStyles.AllowThousands, NumberFormatInfo.CurrentInfo, out result);
	}

	public static bool TryParse(ReadOnlySpan<char> s, out double result)
	{
		return TryParse(s, NumberStyles.Float | NumberStyles.AllowThousands, NumberFormatInfo.CurrentInfo, out result);
	}

	public static bool TryParse([NotNullWhen(true)] string? s, NumberStyles style, IFormatProvider? provider, out double result)
	{
		NumberFormatInfo.ValidateParseStyleFloatingPoint(style);
		if (s == null)
		{
			result = 0.0;
			return false;
		}
		return TryParse((ReadOnlySpan<char>)s, style, NumberFormatInfo.GetInstance(provider), out result);
	}

	public static bool TryParse(ReadOnlySpan<char> s, NumberStyles style, IFormatProvider? provider, out double result)
	{
		NumberFormatInfo.ValidateParseStyleFloatingPoint(style);
		return TryParse(s, style, NumberFormatInfo.GetInstance(provider), out result);
	}

	private static bool TryParse(ReadOnlySpan<char> s, NumberStyles style, NumberFormatInfo info, out double result)
	{
		return Number.TryParseDouble(s, style, info, out result);
	}

	public TypeCode GetTypeCode()
	{
		return TypeCode.Double;
	}

	bool IConvertible.ToBoolean(IFormatProvider provider)
	{
		return Convert.ToBoolean(this);
	}

	char IConvertible.ToChar(IFormatProvider provider)
	{
		throw new InvalidCastException(SR.Format(SR.InvalidCast_FromTo, "Double", "Char"));
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
		return this;
	}

	decimal IConvertible.ToDecimal(IFormatProvider provider)
	{
		return Convert.ToDecimal(this);
	}

	DateTime IConvertible.ToDateTime(IFormatProvider provider)
	{
		throw new InvalidCastException(SR.Format(SR.InvalidCast_FromTo, "Double", "DateTime"));
	}

	object IConvertible.ToType(Type type, IFormatProvider provider)
	{
		return Convert.DefaultToType(this, type, provider);
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static double IAdditionOperators<double, double, double>.operator +(double left, double right)
	{
		return left + right;
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static bool IBinaryNumber<double>.IsPow2(double value)
	{
		ulong num = BitConverter.DoubleToUInt64Bits(value);
		uint num2 = (uint)(int)(num >> 52) & 0x7FFu;
		ulong num3 = num & 0xFFFFFFFFFFFFFuL;
		if (value > 0.0 && num2 != 0 && num2 != 2047)
		{
			return num3 == 0;
		}
		return false;
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static double IBinaryNumber<double>.Log2(double value)
	{
		return Math.Log2(value);
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static double IBitwiseOperators<double, double, double>.operator &(double left, double right)
	{
		ulong value = BitConverter.DoubleToUInt64Bits(left) & BitConverter.DoubleToUInt64Bits(right);
		return BitConverter.UInt64BitsToDouble(value);
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static double IBitwiseOperators<double, double, double>.operator |(double left, double right)
	{
		ulong value = BitConverter.DoubleToUInt64Bits(left) | BitConverter.DoubleToUInt64Bits(right);
		return BitConverter.UInt64BitsToDouble(value);
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static double IBitwiseOperators<double, double, double>.operator ^(double left, double right)
	{
		ulong value = BitConverter.DoubleToUInt64Bits(left) ^ BitConverter.DoubleToUInt64Bits(right);
		return BitConverter.UInt64BitsToDouble(value);
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static double IBitwiseOperators<double, double, double>.operator ~(double value)
	{
		ulong value2 = ~BitConverter.DoubleToUInt64Bits(value);
		return BitConverter.UInt64BitsToDouble(value2);
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static bool IComparisonOperators<double, double>.operator <(double left, double right)
	{
		return left < right;
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static bool IComparisonOperators<double, double>.operator <=(double left, double right)
	{
		return left <= right;
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static bool IComparisonOperators<double, double>.operator >(double left, double right)
	{
		return left > right;
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static bool IComparisonOperators<double, double>.operator >=(double left, double right)
	{
		return left >= right;
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static double IDecrementOperators<double>.operator --(double value)
	{
		return value -= 1.0;
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static double IDivisionOperators<double, double, double>.operator /(double left, double right)
	{
		return left / right;
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static bool IEqualityOperators<double, double>.operator ==(double left, double right)
	{
		return left == right;
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static bool IEqualityOperators<double, double>.operator !=(double left, double right)
	{
		return left != right;
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static double IFloatingPoint<double>.Acos(double x)
	{
		return Math.Acos(x);
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static double IFloatingPoint<double>.Acosh(double x)
	{
		return Math.Acosh(x);
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static double IFloatingPoint<double>.Asin(double x)
	{
		return Math.Asin(x);
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static double IFloatingPoint<double>.Asinh(double x)
	{
		return Math.Asinh(x);
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static double IFloatingPoint<double>.Atan(double x)
	{
		return Math.Atan(x);
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static double IFloatingPoint<double>.Atan2(double y, double x)
	{
		return Math.Atan2(y, x);
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static double IFloatingPoint<double>.Atanh(double x)
	{
		return Math.Atanh(x);
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static double IFloatingPoint<double>.BitIncrement(double x)
	{
		return Math.BitIncrement(x);
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static double IFloatingPoint<double>.BitDecrement(double x)
	{
		return Math.BitDecrement(x);
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static double IFloatingPoint<double>.Cbrt(double x)
	{
		return Math.Cbrt(x);
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static double IFloatingPoint<double>.Ceiling(double x)
	{
		return Math.Ceiling(x);
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static double IFloatingPoint<double>.CopySign(double x, double y)
	{
		return Math.CopySign(x, y);
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static double IFloatingPoint<double>.Cos(double x)
	{
		return Math.Cos(x);
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static double IFloatingPoint<double>.Cosh(double x)
	{
		return Math.Cosh(x);
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static double IFloatingPoint<double>.Exp(double x)
	{
		return Math.Exp(x);
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static double IFloatingPoint<double>.Floor(double x)
	{
		return Math.Floor(x);
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static double IFloatingPoint<double>.FusedMultiplyAdd(double left, double right, double addend)
	{
		return Math.FusedMultiplyAdd(left, right, addend);
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static double IFloatingPoint<double>.IEEERemainder(double left, double right)
	{
		return Math.IEEERemainder(left, right);
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static TInteger IFloatingPoint<double>.ILogB<TInteger>(double x)
	{
		return TInteger.Create(Math.ILogB(x));
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static double IFloatingPoint<double>.Log(double x)
	{
		return Math.Log(x);
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static double IFloatingPoint<double>.Log(double x, double newBase)
	{
		return Math.Log(x, newBase);
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static double IFloatingPoint<double>.Log2(double x)
	{
		return Math.Log2(x);
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static double IFloatingPoint<double>.Log10(double x)
	{
		return Math.Log10(x);
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static double IFloatingPoint<double>.MaxMagnitude(double x, double y)
	{
		return Math.MaxMagnitude(x, y);
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static double IFloatingPoint<double>.MinMagnitude(double x, double y)
	{
		return Math.MinMagnitude(x, y);
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static double IFloatingPoint<double>.Pow(double x, double y)
	{
		return Math.Pow(x, y);
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static double IFloatingPoint<double>.Round(double x)
	{
		return Math.Round(x);
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static double IFloatingPoint<double>.Round<TInteger>(double x, TInteger digits)
	{
		return Math.Round(x, int.Create(digits));
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static double IFloatingPoint<double>.Round(double x, MidpointRounding mode)
	{
		return Math.Round(x, mode);
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static double IFloatingPoint<double>.Round<TInteger>(double x, TInteger digits, MidpointRounding mode)
	{
		return Math.Round(x, int.Create(digits), mode);
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static double IFloatingPoint<double>.ScaleB<TInteger>(double x, TInteger n)
	{
		return Math.ScaleB(x, int.Create(n));
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static double IFloatingPoint<double>.Sin(double x)
	{
		return Math.Sin(x);
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static double IFloatingPoint<double>.Sinh(double x)
	{
		return Math.Sinh(x);
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static double IFloatingPoint<double>.Sqrt(double x)
	{
		return Math.Sqrt(x);
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static double IFloatingPoint<double>.Tan(double x)
	{
		return Math.Tan(x);
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static double IFloatingPoint<double>.Tanh(double x)
	{
		return Math.Tanh(x);
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static double IFloatingPoint<double>.Truncate(double x)
	{
		return Math.Truncate(x);
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static bool IFloatingPoint<double>.IsFinite(double d)
	{
		return IsFinite(d);
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static bool IFloatingPoint<double>.IsInfinity(double d)
	{
		return IsInfinity(d);
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static bool IFloatingPoint<double>.IsNaN(double d)
	{
		return IsNaN(d);
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static bool IFloatingPoint<double>.IsNegative(double d)
	{
		return IsNegative(d);
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static bool IFloatingPoint<double>.IsNegativeInfinity(double d)
	{
		return IsNegativeInfinity(d);
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static bool IFloatingPoint<double>.IsNormal(double d)
	{
		return IsNormal(d);
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static bool IFloatingPoint<double>.IsPositiveInfinity(double d)
	{
		return IsPositiveInfinity(d);
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static bool IFloatingPoint<double>.IsSubnormal(double d)
	{
		return IsSubnormal(d);
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static double IIncrementOperators<double>.operator ++(double value)
	{
		return value += 1.0;
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static double IModulusOperators<double, double, double>.operator %(double left, double right)
	{
		return left % right;
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static double IMultiplyOperators<double, double, double>.operator *(double left, double right)
	{
		return left * right;
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static double INumber<double>.Abs(double value)
	{
		return Math.Abs(value);
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static double INumber<double>.Clamp(double value, double min, double max)
	{
		return Math.Clamp(value, min, max);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static double INumber<double>.Create<TOther>(TOther value)
	{
		if (typeof(TOther) == typeof(byte))
		{
			return (int)(byte)(object)value;
		}
		if (typeof(TOther) == typeof(char))
		{
			return (int)(char)(object)value;
		}
		if (typeof(TOther) == typeof(decimal))
		{
			return (double)(decimal)(object)value;
		}
		if (typeof(TOther) == typeof(double))
		{
			return (double)(object)value;
		}
		if (typeof(TOther) == typeof(short))
		{
			return (short)(object)value;
		}
		if (typeof(TOther) == typeof(int))
		{
			return (int)(object)value;
		}
		if (typeof(TOther) == typeof(long))
		{
			return (long)(object)value;
		}
		if (typeof(TOther) == typeof(IntPtr))
		{
			return (nint)(IntPtr)(object)value;
		}
		if (typeof(TOther) == typeof(sbyte))
		{
			return (sbyte)(object)value;
		}
		if (typeof(TOther) == typeof(float))
		{
			return (float)(object)value;
		}
		if (typeof(TOther) == typeof(ushort))
		{
			return (int)(ushort)(object)value;
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
			return (nint)(nuint)(UIntPtr)(object)value;
		}
		ThrowHelper.ThrowNotSupportedException();
		return 0.0;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static double INumber<double>.CreateSaturating<TOther>(TOther value)
	{
		if (typeof(TOther) == typeof(byte))
		{
			return (int)(byte)(object)value;
		}
		if (typeof(TOther) == typeof(char))
		{
			return (int)(char)(object)value;
		}
		if (typeof(TOther) == typeof(decimal))
		{
			return (double)(decimal)(object)value;
		}
		if (typeof(TOther) == typeof(double))
		{
			return (double)(object)value;
		}
		if (typeof(TOther) == typeof(short))
		{
			return (short)(object)value;
		}
		if (typeof(TOther) == typeof(int))
		{
			return (int)(object)value;
		}
		if (typeof(TOther) == typeof(long))
		{
			return (long)(object)value;
		}
		if (typeof(TOther) == typeof(IntPtr))
		{
			return (nint)(IntPtr)(object)value;
		}
		if (typeof(TOther) == typeof(sbyte))
		{
			return (sbyte)(object)value;
		}
		if (typeof(TOther) == typeof(float))
		{
			return (float)(object)value;
		}
		if (typeof(TOther) == typeof(ushort))
		{
			return (int)(ushort)(object)value;
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
			return (nint)(nuint)(UIntPtr)(object)value;
		}
		ThrowHelper.ThrowNotSupportedException();
		return 0.0;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static double INumber<double>.CreateTruncating<TOther>(TOther value)
	{
		if (typeof(TOther) == typeof(byte))
		{
			return (int)(byte)(object)value;
		}
		if (typeof(TOther) == typeof(char))
		{
			return (int)(char)(object)value;
		}
		if (typeof(TOther) == typeof(decimal))
		{
			return (double)(decimal)(object)value;
		}
		if (typeof(TOther) == typeof(double))
		{
			return (double)(object)value;
		}
		if (typeof(TOther) == typeof(short))
		{
			return (short)(object)value;
		}
		if (typeof(TOther) == typeof(int))
		{
			return (int)(object)value;
		}
		if (typeof(TOther) == typeof(long))
		{
			return (long)(object)value;
		}
		if (typeof(TOther) == typeof(IntPtr))
		{
			return (nint)(IntPtr)(object)value;
		}
		if (typeof(TOther) == typeof(sbyte))
		{
			return (sbyte)(object)value;
		}
		if (typeof(TOther) == typeof(float))
		{
			return (float)(object)value;
		}
		if (typeof(TOther) == typeof(ushort))
		{
			return (int)(ushort)(object)value;
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
			return (nint)(nuint)(UIntPtr)(object)value;
		}
		ThrowHelper.ThrowNotSupportedException();
		return 0.0;
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static (double Quotient, double Remainder) INumber<double>.DivRem(double left, double right)
	{
		return (Quotient: left / right, Remainder: left % right);
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static double INumber<double>.Max(double x, double y)
	{
		return Math.Max(x, y);
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static double INumber<double>.Min(double x, double y)
	{
		return Math.Min(x, y);
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static double INumber<double>.Parse(string s, NumberStyles style, IFormatProvider provider)
	{
		return Parse(s, style, provider);
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static double INumber<double>.Parse(ReadOnlySpan<char> s, NumberStyles style, IFormatProvider provider)
	{
		return Parse(s, style, provider);
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static double INumber<double>.Sign(double value)
	{
		return Math.Sign(value);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static bool INumber<double>.TryCreate<TOther>(TOther value, out double result)
	{
		if (typeof(TOther) == typeof(byte))
		{
			result = (int)(byte)(object)value;
			return true;
		}
		if (typeof(TOther) == typeof(char))
		{
			result = (int)(char)(object)value;
			return true;
		}
		if (typeof(TOther) == typeof(decimal))
		{
			result = (double)(decimal)(object)value;
			return true;
		}
		if (typeof(TOther) == typeof(double))
		{
			result = (double)(object)value;
			return true;
		}
		if (typeof(TOther) == typeof(short))
		{
			result = (short)(object)value;
			return true;
		}
		if (typeof(TOther) == typeof(int))
		{
			result = (int)(object)value;
			return true;
		}
		if (typeof(TOther) == typeof(long))
		{
			result = (long)(object)value;
			return true;
		}
		if (typeof(TOther) == typeof(IntPtr))
		{
			result = (nint)(IntPtr)(object)value;
			return true;
		}
		if (typeof(TOther) == typeof(sbyte))
		{
			result = (sbyte)(object)value;
			return true;
		}
		if (typeof(TOther) == typeof(float))
		{
			result = (float)(object)value;
			return true;
		}
		if (typeof(TOther) == typeof(ushort))
		{
			result = (int)(ushort)(object)value;
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
			result = (nint)(nuint)(UIntPtr)(object)value;
			return true;
		}
		ThrowHelper.ThrowNotSupportedException();
		result = 0.0;
		return false;
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static bool INumber<double>.TryParse([NotNullWhen(true)] string s, NumberStyles style, IFormatProvider provider, out double result)
	{
		return TryParse(s, style, provider, out result);
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static bool INumber<double>.TryParse(ReadOnlySpan<char> s, NumberStyles style, IFormatProvider provider, out double result)
	{
		return TryParse(s, style, provider, out result);
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static double IParseable<double>.Parse(string s, IFormatProvider provider)
	{
		return Parse(s, provider);
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static bool IParseable<double>.TryParse([NotNullWhen(true)] string s, IFormatProvider provider, out double result)
	{
		return TryParse(s, NumberStyles.Float | NumberStyles.AllowThousands, provider, out result);
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static double ISpanParseable<double>.Parse(ReadOnlySpan<char> s, IFormatProvider provider)
	{
		return Parse(s, NumberStyles.Float | NumberStyles.AllowThousands, provider);
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static bool ISpanParseable<double>.TryParse(ReadOnlySpan<char> s, IFormatProvider provider, out double result)
	{
		return TryParse(s, NumberStyles.Float | NumberStyles.AllowThousands, provider, out result);
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static double ISubtractionOperators<double, double, double>.operator -(double left, double right)
	{
		return left - right;
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static double IUnaryNegationOperators<double, double>.operator -(double value)
	{
		return 0.0 - value;
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static double IUnaryPlusOperators<double, double>.operator +(double value)
	{
		return value;
	}
}
