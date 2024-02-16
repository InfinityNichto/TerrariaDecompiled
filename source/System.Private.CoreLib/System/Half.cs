using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.Versioning;

namespace System;

public readonly struct Half : IComparable, ISpanFormattable, IFormattable, IComparable<Half>, IEquatable<Half>, IBinaryFloatingPoint<Half>, IBinaryNumber<Half>, IBitwiseOperators<Half, Half, Half>, INumber<Half>, IAdditionOperators<Half, Half, Half>, IAdditiveIdentity<Half, Half>, IComparisonOperators<Half, Half>, IEqualityOperators<Half, Half>, IDecrementOperators<Half>, IDivisionOperators<Half, Half, Half>, IIncrementOperators<Half>, IModulusOperators<Half, Half, Half>, IMultiplicativeIdentity<Half, Half>, IMultiplyOperators<Half, Half, Half>, ISpanParseable<Half>, IParseable<Half>, ISubtractionOperators<Half, Half, Half>, IUnaryNegationOperators<Half, Half>, IUnaryPlusOperators<Half, Half>, IFloatingPoint<Half>, ISignedNumber<Half>, IMinMaxValue<Half>
{
	private static readonly Half PositiveZero = new Half(0);

	private static readonly Half NegativeZero = new Half(32768);

	private readonly ushort _value;

	public static Half Epsilon => new Half(1);

	public static Half PositiveInfinity => new Half(31744);

	public static Half NegativeInfinity => new Half(64512);

	public static Half NaN => new Half(65024);

	public static Half MinValue => new Half(64511);

	public static Half MaxValue => new Half(31743);

	private sbyte Exponent => (sbyte)((_value & 0x7C00) >> 10);

	private ushort Significand => (ushort)(_value & 0x3FFu);

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static Half IAdditiveIdentity<Half, Half>.AdditiveIdentity => PositiveZero;

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static Half IFloatingPoint<Half>.E => (Half)(float)Math.E;

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static Half IFloatingPoint<Half>.Epsilon => Epsilon;

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static Half IFloatingPoint<Half>.NaN => NaN;

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static Half IFloatingPoint<Half>.NegativeInfinity => NegativeInfinity;

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static Half IFloatingPoint<Half>.NegativeZero => NegativeZero;

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static Half IFloatingPoint<Half>.Pi => (Half)(float)Math.PI;

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static Half IFloatingPoint<Half>.PositiveInfinity => PositiveInfinity;

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static Half IFloatingPoint<Half>.Tau => (Half)((float)Math.PI * 2f);

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static Half IMinMaxValue<Half>.MinValue => MinValue;

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static Half IMinMaxValue<Half>.MaxValue => MaxValue;

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static Half IMultiplicativeIdentity<Half, Half>.MultiplicativeIdentity => (Half)1f;

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static Half INumber<Half>.One => (Half)1f;

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static Half INumber<Half>.Zero => PositiveZero;

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static Half ISignedNumber<Half>.NegativeOne => (Half)(-1f);

	internal Half(ushort value)
	{
		_value = value;
	}

	private Half(bool sign, ushort exp, ushort sig)
	{
		_value = (ushort)(((sign ? 1 : 0) << 15) + (exp << 10) + sig);
	}

	public static bool operator <(Half left, Half right)
	{
		if (IsNaN(left) || IsNaN(right))
		{
			return false;
		}
		bool flag = IsNegative(left);
		if (flag != IsNegative(right))
		{
			if (flag)
			{
				return !AreZero(left, right);
			}
			return false;
		}
		if (left._value != right._value)
		{
			return (left._value < right._value) ^ flag;
		}
		return false;
	}

	public static bool operator >(Half left, Half right)
	{
		return right < left;
	}

	public static bool operator <=(Half left, Half right)
	{
		if (IsNaN(left) || IsNaN(right))
		{
			return false;
		}
		bool flag = IsNegative(left);
		if (flag != IsNegative(right))
		{
			if (!flag)
			{
				return AreZero(left, right);
			}
			return true;
		}
		if (left._value != right._value)
		{
			return (left._value < right._value) ^ flag;
		}
		return true;
	}

	public static bool operator >=(Half left, Half right)
	{
		return right <= left;
	}

	public static bool operator ==(Half left, Half right)
	{
		if (IsNaN(left) || IsNaN(right))
		{
			return false;
		}
		if (left._value != right._value)
		{
			return AreZero(left, right);
		}
		return true;
	}

	public static bool operator !=(Half left, Half right)
	{
		return !(left == right);
	}

	public static bool IsFinite(Half value)
	{
		return StripSign(value) < 31744;
	}

	public static bool IsInfinity(Half value)
	{
		return StripSign(value) == 31744;
	}

	public static bool IsNaN(Half value)
	{
		return StripSign(value) > 31744;
	}

	public static bool IsNegative(Half value)
	{
		return (short)value._value < 0;
	}

	public static bool IsNegativeInfinity(Half value)
	{
		return value._value == 64512;
	}

	public static bool IsNormal(Half value)
	{
		uint num = StripSign(value);
		if (num < 31744 && num != 0)
		{
			return (num & 0x7C00) != 0;
		}
		return false;
	}

	public static bool IsPositiveInfinity(Half value)
	{
		return value._value == 31744;
	}

	public static bool IsSubnormal(Half value)
	{
		uint num = StripSign(value);
		if (num < 31744 && num != 0)
		{
			return (num & 0x7C00) == 0;
		}
		return false;
	}

	public static Half Parse(string s)
	{
		if (s == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.s);
		}
		return Number.ParseHalf(s, NumberStyles.Float | NumberStyles.AllowThousands, NumberFormatInfo.CurrentInfo);
	}

	public static Half Parse(string s, NumberStyles style)
	{
		NumberFormatInfo.ValidateParseStyleFloatingPoint(style);
		if (s == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.s);
		}
		return Number.ParseHalf(s, style, NumberFormatInfo.CurrentInfo);
	}

	public static Half Parse(string s, IFormatProvider? provider)
	{
		if (s == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.s);
		}
		return Number.ParseHalf(s, NumberStyles.Float | NumberStyles.AllowThousands, NumberFormatInfo.GetInstance(provider));
	}

	public static Half Parse(string s, NumberStyles style = NumberStyles.Float | NumberStyles.AllowThousands, IFormatProvider? provider = null)
	{
		NumberFormatInfo.ValidateParseStyleFloatingPoint(style);
		if (s == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.s);
		}
		return Number.ParseHalf(s, style, NumberFormatInfo.GetInstance(provider));
	}

	public static Half Parse(ReadOnlySpan<char> s, NumberStyles style = NumberStyles.Float | NumberStyles.AllowThousands, IFormatProvider? provider = null)
	{
		NumberFormatInfo.ValidateParseStyleFloatingPoint(style);
		return Number.ParseHalf(s, style, NumberFormatInfo.GetInstance(provider));
	}

	public static bool TryParse([NotNullWhen(true)] string? s, out Half result)
	{
		if (s == null)
		{
			result = default(Half);
			return false;
		}
		return TryParse(s, NumberStyles.Float | NumberStyles.AllowThousands, null, out result);
	}

	public static bool TryParse(ReadOnlySpan<char> s, out Half result)
	{
		return TryParse(s, NumberStyles.Float | NumberStyles.AllowThousands, null, out result);
	}

	public static bool TryParse([NotNullWhen(true)] string? s, NumberStyles style, IFormatProvider? provider, out Half result)
	{
		NumberFormatInfo.ValidateParseStyleFloatingPoint(style);
		if (s == null)
		{
			result = default(Half);
			return false;
		}
		return TryParse(s.AsSpan(), style, provider, out result);
	}

	public static bool TryParse(ReadOnlySpan<char> s, NumberStyles style, IFormatProvider? provider, out Half result)
	{
		NumberFormatInfo.ValidateParseStyleFloatingPoint(style);
		return Number.TryParseHalf(s, style, NumberFormatInfo.GetInstance(provider), out result);
	}

	private static bool AreZero(Half left, Half right)
	{
		return (ushort)((left._value | right._value) & -32769) == 0;
	}

	private static bool IsNaNOrZero(Half value)
	{
		return ((value._value - 1) & -32769) >= 31744;
	}

	private static uint StripSign(Half value)
	{
		return (ushort)(value._value & 0xFFFF7FFFu);
	}

	public int CompareTo(object? obj)
	{
		if (!(obj is Half))
		{
			if (obj != null)
			{
				throw new ArgumentException(SR.Arg_MustBeHalf);
			}
			return 1;
		}
		return CompareTo((Half)obj);
	}

	public int CompareTo(Half other)
	{
		if (this < other)
		{
			return -1;
		}
		if (this > other)
		{
			return 1;
		}
		if (this == other)
		{
			return 0;
		}
		if (IsNaN(this))
		{
			if (!IsNaN(other))
			{
				return -1;
			}
			return 0;
		}
		return 1;
	}

	public override bool Equals([NotNullWhen(true)] object? obj)
	{
		if (obj is Half other)
		{
			return Equals(other);
		}
		return false;
	}

	public bool Equals(Half other)
	{
		if (_value != other._value && !AreZero(this, other))
		{
			if (IsNaN(this))
			{
				return IsNaN(other);
			}
			return false;
		}
		return true;
	}

	public override int GetHashCode()
	{
		if (IsNaNOrZero(this))
		{
			return _value & 0x7C00;
		}
		return _value;
	}

	public override string ToString()
	{
		return Number.FormatHalf(this, null, NumberFormatInfo.CurrentInfo);
	}

	public string ToString(string? format)
	{
		return Number.FormatHalf(this, format, NumberFormatInfo.CurrentInfo);
	}

	public string ToString(IFormatProvider? provider)
	{
		return Number.FormatHalf(this, null, NumberFormatInfo.GetInstance(provider));
	}

	public string ToString(string? format, IFormatProvider? provider)
	{
		return Number.FormatHalf(this, format, NumberFormatInfo.GetInstance(provider));
	}

	public bool TryFormat(Span<char> destination, out int charsWritten, ReadOnlySpan<char> format = default(ReadOnlySpan<char>), IFormatProvider? provider = null)
	{
		return Number.TryFormatHalf(this, format, NumberFormatInfo.GetInstance(provider), destination, out charsWritten);
	}

	public static explicit operator Half(float value)
	{
		uint num = BitConverter.SingleToUInt32Bits(value);
		bool flag = (num & 0x80000000u) >> 31 != 0;
		int num2 = (int)(num & 0x7F800000) >> 23;
		uint num3 = num & 0x7FFFFFu;
		if (num2 == 255)
		{
			if (num3 != 0)
			{
				return CreateHalfNaN(flag, (ulong)num3 << 41);
			}
			if (!flag)
			{
				return PositiveInfinity;
			}
			return NegativeInfinity;
		}
		uint num4 = (num3 >> 9) | (((num3 & 0x1FFu) != 0) ? 1u : 0u);
		if (((uint)num2 | num4) == 0)
		{
			return new Half(flag, 0, 0);
		}
		return new Half(RoundPackToHalf(flag, (short)(num2 - 113), (ushort)(num4 | 0x4000u)));
	}

	public static explicit operator Half(double value)
	{
		ulong num = BitConverter.DoubleToUInt64Bits(value);
		bool flag = (num & 0x8000000000000000uL) >> 63 != 0;
		int num2 = (int)((num & 0x7FF0000000000000L) >> 52);
		ulong num3 = num & 0xFFFFFFFFFFFFFuL;
		if (num2 == 2047)
		{
			if (num3 != 0L)
			{
				return CreateHalfNaN(flag, num3 << 12);
			}
			if (!flag)
			{
				return PositiveInfinity;
			}
			return NegativeInfinity;
		}
		uint num4 = (uint)ShiftRightJam(num3, 38);
		if (((uint)num2 | num4) == 0)
		{
			return new Half(flag, 0, 0);
		}
		return new Half(RoundPackToHalf(flag, (short)(num2 - 1009), (ushort)(num4 | 0x4000u)));
	}

	public static explicit operator float(Half value)
	{
		bool flag = IsNegative(value);
		int num = value.Exponent;
		uint num2 = value.Significand;
		switch (num)
		{
		case 31:
			if (num2 != 0)
			{
				return CreateSingleNaN(flag, (ulong)num2 << 54);
			}
			if (!flag)
			{
				return float.PositiveInfinity;
			}
			return float.NegativeInfinity;
		case 0:
		{
			if (num2 == 0)
			{
				return BitConverter.UInt32BitsToSingle(flag ? 2147483648u : 0u);
			}
			(int Exp, uint Sig) tuple = NormSubnormalF16Sig(num2);
			num = tuple.Exp;
			num2 = tuple.Sig;
			num--;
			break;
		}
		}
		return CreateSingle(flag, (byte)(num + 112), num2 << 13);
	}

	public static explicit operator double(Half value)
	{
		bool flag = IsNegative(value);
		int num = value.Exponent;
		uint num2 = value.Significand;
		switch (num)
		{
		case 31:
			if (num2 != 0)
			{
				return CreateDoubleNaN(flag, (ulong)num2 << 54);
			}
			if (!flag)
			{
				return double.PositiveInfinity;
			}
			return double.NegativeInfinity;
		case 0:
		{
			if (num2 == 0)
			{
				return BitConverter.UInt64BitsToDouble(flag ? 9223372036854775808uL : 0);
			}
			(int Exp, uint Sig) tuple = NormSubnormalF16Sig(num2);
			num = tuple.Exp;
			num2 = tuple.Sig;
			num--;
			break;
		}
		}
		return CreateDouble(flag, (ushort)(num + 1008), (ulong)num2 << 42);
	}

	internal static Half Negate(Half value)
	{
		if (!IsNaN(value))
		{
			return new Half((ushort)(value._value ^ 0x8000u));
		}
		return value;
	}

	private static (int Exp, uint Sig) NormSubnormalF16Sig(uint sig)
	{
		int num = BitOperations.LeadingZeroCount(sig) - 16 - 5;
		return (Exp: 1 - num, Sig: sig << num);
	}

	private static Half CreateHalfNaN(bool sign, ulong significand)
	{
		uint num = (uint)((sign ? 1 : 0) << 15);
		uint num2 = (uint)(significand >> 54);
		return BitConverter.UInt16BitsToHalf((ushort)(num | 0x7E00u | num2));
	}

	private static ushort RoundPackToHalf(bool sign, short exp, ushort sig)
	{
		int num = sig & 0xF;
		if ((uint)exp >= 29u)
		{
			if (exp < 0)
			{
				sig = (ushort)ShiftRightJam(sig, -exp);
				exp = 0;
				num = sig & 0xF;
			}
			else if (exp > 29 || sig + 8 >= 32768)
			{
				if (!sign)
				{
					return 31744;
				}
				return 64512;
			}
		}
		sig = (ushort)(sig + 8 >> 4);
		sig &= (ushort)(~((((num ^ 8) == 0) ? 1u : 0u) & 1u));
		if (sig == 0)
		{
			exp = 0;
		}
		return new Half(sign, (ushort)exp, sig)._value;
	}

	private static uint ShiftRightJam(uint i, int dist)
	{
		if (dist >= 31)
		{
			if (i == 0)
			{
				return 0u;
			}
			return 1u;
		}
		return (i >> dist) | ((i << -dist != 0) ? 1u : 0u);
	}

	private static ulong ShiftRightJam(ulong l, int dist)
	{
		if (dist >= 63)
		{
			if (l == 0L)
			{
				return 0uL;
			}
			return 1uL;
		}
		return (l >> dist) | (ulong)((l << -dist != 0L) ? 1 : 0);
	}

	private static float CreateSingleNaN(bool sign, ulong significand)
	{
		uint num = (uint)((sign ? 1 : 0) << 31);
		uint num2 = (uint)(significand >> 41);
		return BitConverter.UInt32BitsToSingle(num | 0x7FC00000u | num2);
	}

	private static double CreateDoubleNaN(bool sign, ulong significand)
	{
		ulong num = (ulong)((long)(sign ? 1 : 0) << 63);
		ulong num2 = significand >> 12;
		return BitConverter.UInt64BitsToDouble(num | 0x7FF8000000000000uL | num2);
	}

	private static float CreateSingle(bool sign, byte exp, uint sig)
	{
		return BitConverter.UInt32BitsToSingle((uint)(((sign ? 1 : 0) << 31) + (exp << 23)) + sig);
	}

	private static double CreateDouble(bool sign, ushort exp, ulong sig)
	{
		return BitConverter.UInt64BitsToDouble((ulong)(((long)(sign ? 1 : 0) << 63) + (long)((ulong)exp << 52)) + sig);
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static Half IAdditionOperators<Half, Half, Half>.operator +(Half left, Half right)
	{
		return (Half)((float)left + (float)right);
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static bool IBinaryNumber<Half>.IsPow2(Half value)
	{
		uint num = BitConverter.HalfToUInt16Bits(value);
		uint num2 = (num >> 10) & 0x1Fu;
		uint num3 = num & 0x3FFu;
		if (value > PositiveZero && num2 != 0 && num2 != 31)
		{
			return num3 == 0;
		}
		return false;
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static Half IBinaryNumber<Half>.Log2(Half value)
	{
		return (Half)MathF.Log2((float)value);
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static Half IBitwiseOperators<Half, Half, Half>.operator &(Half left, Half right)
	{
		ushort value = (ushort)(BitConverter.HalfToUInt16Bits(left) & BitConverter.HalfToUInt16Bits(right));
		return BitConverter.UInt16BitsToHalf(value);
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static Half IBitwiseOperators<Half, Half, Half>.operator |(Half left, Half right)
	{
		ushort value = (ushort)(BitConverter.HalfToUInt16Bits(left) | BitConverter.HalfToUInt16Bits(right));
		return BitConverter.UInt16BitsToHalf(value);
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static Half IBitwiseOperators<Half, Half, Half>.operator ^(Half left, Half right)
	{
		ushort value = (ushort)(BitConverter.HalfToUInt16Bits(left) ^ BitConverter.HalfToUInt16Bits(right));
		return BitConverter.UInt16BitsToHalf(value);
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static Half IBitwiseOperators<Half, Half, Half>.operator ~(Half value)
	{
		ushort value2 = (ushort)(~BitConverter.HalfToUInt16Bits(value));
		return BitConverter.UInt16BitsToHalf(value2);
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static bool IComparisonOperators<Half, Half>.operator <(Half left, Half right)
	{
		return left < right;
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static bool IComparisonOperators<Half, Half>.operator <=(Half left, Half right)
	{
		return left <= right;
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static bool IComparisonOperators<Half, Half>.operator >(Half left, Half right)
	{
		return left > right;
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static bool IComparisonOperators<Half, Half>.operator >=(Half left, Half right)
	{
		return left >= right;
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static Half IDecrementOperators<Half>.operator --(Half value)
	{
		float num = (float)value;
		num -= 1f;
		return (Half)num;
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static bool IEqualityOperators<Half, Half>.operator ==(Half left, Half right)
	{
		return left == right;
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static bool IEqualityOperators<Half, Half>.operator !=(Half left, Half right)
	{
		return left != right;
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static Half IDivisionOperators<Half, Half, Half>.operator /(Half left, Half right)
	{
		return (Half)((float)left / (float)right);
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static Half IFloatingPoint<Half>.Acos(Half x)
	{
		return (Half)MathF.Acos((float)x);
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static Half IFloatingPoint<Half>.Acosh(Half x)
	{
		return (Half)MathF.Acosh((float)x);
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static Half IFloatingPoint<Half>.Asin(Half x)
	{
		return (Half)MathF.Asin((float)x);
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static Half IFloatingPoint<Half>.Asinh(Half x)
	{
		return (Half)MathF.Asinh((float)x);
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static Half IFloatingPoint<Half>.Atan(Half x)
	{
		return (Half)MathF.Atan((float)x);
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static Half IFloatingPoint<Half>.Atan2(Half y, Half x)
	{
		return (Half)MathF.Atan2((float)y, (float)x);
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static Half IFloatingPoint<Half>.Atanh(Half x)
	{
		return (Half)MathF.Atanh((float)x);
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static Half IFloatingPoint<Half>.BitIncrement(Half x)
	{
		ushort num = BitConverter.HalfToUInt16Bits(x);
		if ((num & 0x7C00) >= 31744)
		{
			if (num != 64512)
			{
				return x;
			}
			return MinValue;
		}
		if (num == 32768)
		{
			return Epsilon;
		}
		num += (ushort)((num >= 0) ? 1 : (-1));
		return BitConverter.UInt16BitsToHalf(num);
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static Half IFloatingPoint<Half>.BitDecrement(Half x)
	{
		ushort num = BitConverter.HalfToUInt16Bits(x);
		if ((num & 0x7C00) >= 31744)
		{
			if (num != 31744)
			{
				return x;
			}
			return MaxValue;
		}
		if (num == 0)
		{
			return new Half(32769);
		}
		num += (ushort)((num < 0) ? 1 : (-1));
		return BitConverter.UInt16BitsToHalf(num);
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static Half IFloatingPoint<Half>.Cbrt(Half x)
	{
		return (Half)MathF.Cbrt((float)x);
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static Half IFloatingPoint<Half>.Ceiling(Half x)
	{
		return (Half)MathF.Ceiling((float)x);
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static Half IFloatingPoint<Half>.CopySign(Half x, Half y)
	{
		return (Half)MathF.CopySign((float)x, (float)y);
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static Half IFloatingPoint<Half>.Cos(Half x)
	{
		return (Half)MathF.Cos((float)x);
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static Half IFloatingPoint<Half>.Cosh(Half x)
	{
		return (Half)MathF.Cosh((float)x);
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static Half IFloatingPoint<Half>.Exp(Half x)
	{
		return (Half)MathF.Exp((float)x);
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static Half IFloatingPoint<Half>.Floor(Half x)
	{
		return (Half)MathF.Floor((float)x);
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static Half IFloatingPoint<Half>.FusedMultiplyAdd(Half left, Half right, Half addend)
	{
		return (Half)MathF.FusedMultiplyAdd((float)left, (float)right, (float)addend);
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static Half IFloatingPoint<Half>.IEEERemainder(Half left, Half right)
	{
		return (Half)MathF.IEEERemainder((float)left, (float)right);
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static TInteger IFloatingPoint<Half>.ILogB<TInteger>(Half x)
	{
		return TInteger.Create(MathF.ILogB((float)x));
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static Half IFloatingPoint<Half>.Log(Half x)
	{
		return (Half)MathF.Log((float)x);
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static Half IFloatingPoint<Half>.Log(Half x, Half newBase)
	{
		return (Half)MathF.Log((float)x, (float)newBase);
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static Half IFloatingPoint<Half>.Log2(Half x)
	{
		return (Half)MathF.Log2((float)x);
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static Half IFloatingPoint<Half>.Log10(Half x)
	{
		return (Half)MathF.Log10((float)x);
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static Half IFloatingPoint<Half>.MaxMagnitude(Half x, Half y)
	{
		return (Half)MathF.MaxMagnitude((float)x, (float)y);
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static Half IFloatingPoint<Half>.MinMagnitude(Half x, Half y)
	{
		return (Half)MathF.MinMagnitude((float)x, (float)y);
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static Half IFloatingPoint<Half>.Pow(Half x, Half y)
	{
		return (Half)MathF.Pow((float)x, (float)y);
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static Half IFloatingPoint<Half>.Round(Half x)
	{
		return (Half)MathF.Round((float)x);
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static Half IFloatingPoint<Half>.Round<TInteger>(Half x, TInteger digits)
	{
		return (Half)MathF.Round((float)x, int.Create(digits));
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static Half IFloatingPoint<Half>.Round(Half x, MidpointRounding mode)
	{
		return (Half)MathF.Round((float)x, mode);
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static Half IFloatingPoint<Half>.Round<TInteger>(Half x, TInteger digits, MidpointRounding mode)
	{
		return (Half)MathF.Round((float)x, int.Create(digits), mode);
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static Half IFloatingPoint<Half>.ScaleB<TInteger>(Half x, TInteger n)
	{
		return (Half)MathF.ScaleB((float)x, int.Create(n));
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static Half IFloatingPoint<Half>.Sin(Half x)
	{
		return (Half)MathF.Sin((float)x);
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static Half IFloatingPoint<Half>.Sinh(Half x)
	{
		return (Half)MathF.Sinh((float)x);
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static Half IFloatingPoint<Half>.Sqrt(Half x)
	{
		return (Half)MathF.Sqrt((float)x);
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static Half IFloatingPoint<Half>.Tan(Half x)
	{
		return (Half)MathF.Tan((float)x);
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static Half IFloatingPoint<Half>.Tanh(Half x)
	{
		return (Half)MathF.Tanh((float)x);
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static Half IFloatingPoint<Half>.Truncate(Half x)
	{
		return (Half)MathF.Truncate((float)x);
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static bool IFloatingPoint<Half>.IsFinite(Half x)
	{
		return IsFinite(x);
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static bool IFloatingPoint<Half>.IsInfinity(Half x)
	{
		return IsInfinity(x);
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static bool IFloatingPoint<Half>.IsNaN(Half x)
	{
		return IsNaN(x);
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static bool IFloatingPoint<Half>.IsNegative(Half x)
	{
		return IsNegative(x);
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static bool IFloatingPoint<Half>.IsNegativeInfinity(Half x)
	{
		return IsNegativeInfinity(x);
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static bool IFloatingPoint<Half>.IsNormal(Half x)
	{
		return IsNormal(x);
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static bool IFloatingPoint<Half>.IsPositiveInfinity(Half x)
	{
		return IsPositiveInfinity(x);
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static bool IFloatingPoint<Half>.IsSubnormal(Half x)
	{
		return IsSubnormal(x);
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static Half IIncrementOperators<Half>.operator ++(Half value)
	{
		float num = (float)value;
		num += 1f;
		return (Half)num;
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static Half IModulusOperators<Half, Half, Half>.operator %(Half left, Half right)
	{
		return (Half)((float)left % (float)right);
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static Half IMultiplyOperators<Half, Half, Half>.operator *(Half left, Half right)
	{
		return (Half)((float)left * (float)right);
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static Half INumber<Half>.Abs(Half value)
	{
		return (Half)MathF.Abs((float)value);
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static Half INumber<Half>.Clamp(Half value, Half min, Half max)
	{
		return (Half)Math.Clamp((float)value, (float)min, (float)max);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static Half INumber<Half>.Create<TOther>(TOther value)
	{
		if (typeof(TOther) == typeof(byte))
		{
			return (Half)(int)(byte)(object)value;
		}
		if (typeof(TOther) == typeof(char))
		{
			return (Half)(int)(char)(object)value;
		}
		if (typeof(TOther) == typeof(decimal))
		{
			return (Half)(float)(decimal)(object)value;
		}
		if (typeof(TOther) == typeof(double))
		{
			return (Half)(double)(object)value;
		}
		if (typeof(TOther) == typeof(short))
		{
			return (Half)(short)(object)value;
		}
		if (typeof(TOther) == typeof(int))
		{
			return (Half)(int)(object)value;
		}
		if (typeof(TOther) == typeof(long))
		{
			return (Half)(long)(object)value;
		}
		if (typeof(TOther) == typeof(IntPtr))
		{
			return (Half)(nint)(IntPtr)(object)value;
		}
		if (typeof(TOther) == typeof(sbyte))
		{
			return (Half)(sbyte)(object)value;
		}
		if (typeof(TOther) == typeof(float))
		{
			return (Half)(float)(object)value;
		}
		if (typeof(TOther) == typeof(ushort))
		{
			return (Half)(int)(ushort)(object)value;
		}
		if (typeof(TOther) == typeof(uint))
		{
			return (Half)(uint)(object)value;
		}
		if (typeof(TOther) == typeof(ulong))
		{
			return (Half)(ulong)(object)value;
		}
		if (typeof(TOther) == typeof(UIntPtr))
		{
			return (Half)(nuint)(UIntPtr)(object)value;
		}
		ThrowHelper.ThrowNotSupportedException();
		return default(Half);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static Half INumber<Half>.CreateSaturating<TOther>(TOther value)
	{
		if (typeof(TOther) == typeof(byte))
		{
			return (Half)(int)(byte)(object)value;
		}
		if (typeof(TOther) == typeof(char))
		{
			return (Half)(int)(char)(object)value;
		}
		if (typeof(TOther) == typeof(decimal))
		{
			return (Half)(float)(decimal)(object)value;
		}
		if (typeof(TOther) == typeof(double))
		{
			return (Half)(double)(object)value;
		}
		if (typeof(TOther) == typeof(short))
		{
			return (Half)(short)(object)value;
		}
		if (typeof(TOther) == typeof(int))
		{
			return (Half)(int)(object)value;
		}
		if (typeof(TOther) == typeof(long))
		{
			return (Half)(long)(object)value;
		}
		if (typeof(TOther) == typeof(IntPtr))
		{
			return (Half)(nint)(IntPtr)(object)value;
		}
		if (typeof(TOther) == typeof(sbyte))
		{
			return (Half)(sbyte)(object)value;
		}
		if (typeof(TOther) == typeof(float))
		{
			return (Half)(float)(object)value;
		}
		if (typeof(TOther) == typeof(ushort))
		{
			return (Half)(int)(ushort)(object)value;
		}
		if (typeof(TOther) == typeof(uint))
		{
			return (Half)(uint)(object)value;
		}
		if (typeof(TOther) == typeof(ulong))
		{
			return (Half)(ulong)(object)value;
		}
		if (typeof(TOther) == typeof(UIntPtr))
		{
			return (Half)(nuint)(UIntPtr)(object)value;
		}
		ThrowHelper.ThrowNotSupportedException();
		return default(Half);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static Half INumber<Half>.CreateTruncating<TOther>(TOther value)
	{
		if (typeof(TOther) == typeof(byte))
		{
			return (Half)(int)(byte)(object)value;
		}
		if (typeof(TOther) == typeof(char))
		{
			return (Half)(int)(char)(object)value;
		}
		if (typeof(TOther) == typeof(decimal))
		{
			return (Half)(float)(decimal)(object)value;
		}
		if (typeof(TOther) == typeof(double))
		{
			return (Half)(double)(object)value;
		}
		if (typeof(TOther) == typeof(short))
		{
			return (Half)(short)(object)value;
		}
		if (typeof(TOther) == typeof(int))
		{
			return (Half)(int)(object)value;
		}
		if (typeof(TOther) == typeof(long))
		{
			return (Half)(long)(object)value;
		}
		if (typeof(TOther) == typeof(IntPtr))
		{
			return (Half)(nint)(IntPtr)(object)value;
		}
		if (typeof(TOther) == typeof(sbyte))
		{
			return (Half)(sbyte)(object)value;
		}
		if (typeof(TOther) == typeof(float))
		{
			return (Half)(float)(object)value;
		}
		if (typeof(TOther) == typeof(ushort))
		{
			return (Half)(int)(ushort)(object)value;
		}
		if (typeof(TOther) == typeof(uint))
		{
			return (Half)(uint)(object)value;
		}
		if (typeof(TOther) == typeof(ulong))
		{
			return (Half)(ulong)(object)value;
		}
		if (typeof(TOther) == typeof(UIntPtr))
		{
			return (Half)(nuint)(UIntPtr)(object)value;
		}
		ThrowHelper.ThrowNotSupportedException();
		return default(Half);
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static (Half Quotient, Half Remainder) INumber<Half>.DivRem(Half left, Half right)
	{
		return (Quotient: (Half)((float)left / (float)right), Remainder: (Half)((float)left % (float)right));
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static Half INumber<Half>.Max(Half x, Half y)
	{
		return (Half)MathF.Max((float)x, (float)y);
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static Half INumber<Half>.Min(Half x, Half y)
	{
		return (Half)MathF.Min((float)x, (float)y);
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static Half INumber<Half>.Parse(string s, NumberStyles style, IFormatProvider provider)
	{
		return Parse(s, style, provider);
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static Half INumber<Half>.Parse(ReadOnlySpan<char> s, NumberStyles style, IFormatProvider provider)
	{
		return Parse(s, style, provider);
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static Half INumber<Half>.Sign(Half value)
	{
		return (Half)MathF.Sign((float)value);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static bool INumber<Half>.TryCreate<TOther>(TOther value, out Half result)
	{
		if (typeof(TOther) == typeof(byte))
		{
			result = (Half)(int)(byte)(object)value;
			return true;
		}
		if (typeof(TOther) == typeof(char))
		{
			result = (Half)(int)(char)(object)value;
			return true;
		}
		if (typeof(TOther) == typeof(decimal))
		{
			result = (Half)(float)(decimal)(object)value;
			return true;
		}
		if (typeof(TOther) == typeof(double))
		{
			result = (Half)(double)(object)value;
			return true;
		}
		if (typeof(TOther) == typeof(short))
		{
			result = (Half)(short)(object)value;
			return true;
		}
		if (typeof(TOther) == typeof(int))
		{
			result = (Half)(int)(object)value;
			return true;
		}
		if (typeof(TOther) == typeof(long))
		{
			result = (Half)(long)(object)value;
			return true;
		}
		if (typeof(TOther) == typeof(IntPtr))
		{
			result = (Half)(nint)(IntPtr)(object)value;
			return true;
		}
		if (typeof(TOther) == typeof(sbyte))
		{
			result = (Half)(sbyte)(object)value;
			return true;
		}
		if (typeof(TOther) == typeof(float))
		{
			result = (Half)(float)(object)value;
			return true;
		}
		if (typeof(TOther) == typeof(ushort))
		{
			result = (Half)(int)(ushort)(object)value;
			return true;
		}
		if (typeof(TOther) == typeof(uint))
		{
			result = (Half)(uint)(object)value;
			return true;
		}
		if (typeof(TOther) == typeof(ulong))
		{
			result = (Half)(ulong)(object)value;
			return true;
		}
		if (typeof(TOther) == typeof(UIntPtr))
		{
			result = (Half)(nuint)(UIntPtr)(object)value;
			return true;
		}
		ThrowHelper.ThrowNotSupportedException();
		result = default(Half);
		return false;
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static bool INumber<Half>.TryParse([NotNullWhen(true)] string s, NumberStyles style, IFormatProvider provider, out Half result)
	{
		return TryParse(s, style, provider, out result);
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static bool INumber<Half>.TryParse(ReadOnlySpan<char> s, NumberStyles style, IFormatProvider provider, out Half result)
	{
		return TryParse(s, style, provider, out result);
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static Half IParseable<Half>.Parse(string s, IFormatProvider provider)
	{
		return Parse(s, provider);
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static bool IParseable<Half>.TryParse([NotNullWhen(true)] string s, IFormatProvider provider, out Half result)
	{
		return TryParse(s, NumberStyles.Float | NumberStyles.AllowThousands, provider, out result);
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static Half ISpanParseable<Half>.Parse(ReadOnlySpan<char> s, IFormatProvider provider)
	{
		return Parse(s, NumberStyles.Float | NumberStyles.AllowThousands, provider);
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static bool ISpanParseable<Half>.TryParse(ReadOnlySpan<char> s, IFormatProvider provider, out Half result)
	{
		return TryParse(s, NumberStyles.Float | NumberStyles.AllowThousands, provider, out result);
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static Half ISubtractionOperators<Half, Half, Half>.operator -(Half left, Half right)
	{
		return (Half)((float)left - (float)right);
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static Half IUnaryNegationOperators<Half, Half>.operator -(Half value)
	{
		return (Half)(0f - (float)value);
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static Half IUnaryPlusOperators<Half, Half>.operator +(Half value)
	{
		return value;
	}
}
