using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Runtime.Versioning;
using Internal.Runtime.CompilerServices;

namespace System;

[Serializable]
[TypeForwardedFrom("mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
public readonly struct Single : IComparable, IConvertible, ISpanFormattable, IFormattable, IComparable<float>, IEquatable<float>, IBinaryFloatingPoint<float>, IBinaryNumber<float>, IBitwiseOperators<float, float, float>, INumber<float>, IAdditionOperators<float, float, float>, IAdditiveIdentity<float, float>, IComparisonOperators<float, float>, IEqualityOperators<float, float>, IDecrementOperators<float>, IDivisionOperators<float, float, float>, IIncrementOperators<float>, IModulusOperators<float, float, float>, IMultiplicativeIdentity<float, float>, IMultiplyOperators<float, float, float>, ISpanParseable<float>, IParseable<float>, ISubtractionOperators<float, float, float>, IUnaryNegationOperators<float, float>, IUnaryPlusOperators<float, float>, IFloatingPoint<float>, ISignedNumber<float>, IMinMaxValue<float>
{
	private readonly float m_value;

	public const float MinValue = -3.4028235E+38f;

	public const float Epsilon = 1E-45f;

	public const float MaxValue = 3.4028235E+38f;

	public const float PositiveInfinity = 1f / 0f;

	public const float NegativeInfinity = -1f / 0f;

	public const float NaN = 0f / 0f;

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static float IAdditiveIdentity<float, float>.AdditiveIdentity => 0f;

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static float IFloatingPoint<float>.E => (float)Math.E;

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static float IFloatingPoint<float>.Epsilon => float.Epsilon;

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static float IFloatingPoint<float>.NaN => float.NaN;

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static float IFloatingPoint<float>.NegativeInfinity => float.NegativeInfinity;

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static float IFloatingPoint<float>.NegativeZero => -0f;

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static float IFloatingPoint<float>.Pi => (float)Math.PI;

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static float IFloatingPoint<float>.PositiveInfinity => float.PositiveInfinity;

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static float IFloatingPoint<float>.Tau => (float)Math.PI * 2f;

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static float IMinMaxValue<float>.MinValue => float.MinValue;

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static float IMinMaxValue<float>.MaxValue => float.MaxValue;

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static float IMultiplicativeIdentity<float, float>.MultiplicativeIdentity => 1f;

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static float INumber<float>.One => 1f;

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static float INumber<float>.Zero => 0f;

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static float ISignedNumber<float>.NegativeOne => -1f;

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[NonVersionable]
	public static bool IsFinite(float f)
	{
		int num = BitConverter.SingleToInt32Bits(f);
		return (num & 0x7FFFFFFF) < 2139095040;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[NonVersionable]
	public static bool IsInfinity(float f)
	{
		int num = BitConverter.SingleToInt32Bits(f);
		return (num & 0x7FFFFFFF) == 2139095040;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[NonVersionable]
	public static bool IsNaN(float f)
	{
		return f != f;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[NonVersionable]
	public static bool IsNegative(float f)
	{
		return BitConverter.SingleToInt32Bits(f) < 0;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[NonVersionable]
	public static bool IsNegativeInfinity(float f)
	{
		return f == float.NegativeInfinity;
	}

	[NonVersionable]
	public static bool IsNormal(float f)
	{
		int num = BitConverter.SingleToInt32Bits(f);
		num &= 0x7FFFFFFF;
		if (num < 2139095040 && num != 0)
		{
			return (num & 0x7F800000) != 0;
		}
		return false;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[NonVersionable]
	public static bool IsPositiveInfinity(float f)
	{
		return f == float.PositiveInfinity;
	}

	[NonVersionable]
	public static bool IsSubnormal(float f)
	{
		int num = BitConverter.SingleToInt32Bits(f);
		num &= 0x7FFFFFFF;
		if (num < 2139095040 && num != 0)
		{
			return (num & 0x7F800000) == 0;
		}
		return false;
	}

	internal static int ExtractExponentFromBits(uint bits)
	{
		return (int)((bits >> 23) & 0xFF);
	}

	internal static uint ExtractSignificandFromBits(uint bits)
	{
		return bits & 0x7FFFFFu;
	}

	public int CompareTo(object? value)
	{
		if (value == null)
		{
			return 1;
		}
		if (value is float num)
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
		throw new ArgumentException(SR.Arg_MustBeSingle);
	}

	public int CompareTo(float value)
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

	[NonVersionable]
	public static bool operator ==(float left, float right)
	{
		return left == right;
	}

	[NonVersionable]
	public static bool operator !=(float left, float right)
	{
		return left != right;
	}

	[NonVersionable]
	public static bool operator <(float left, float right)
	{
		return left < right;
	}

	[NonVersionable]
	public static bool operator >(float left, float right)
	{
		return left > right;
	}

	[NonVersionable]
	public static bool operator <=(float left, float right)
	{
		return left <= right;
	}

	[NonVersionable]
	public static bool operator >=(float left, float right)
	{
		return left >= right;
	}

	public override bool Equals([NotNullWhen(true)] object? obj)
	{
		if (!(obj is float num))
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

	public bool Equals(float obj)
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
		int num = Unsafe.As<float, int>(ref Unsafe.AsRef(in m_value));
		if (((num - 1) & 0x7FFFFFFF) >= 2139095040)
		{
			num &= 0x7F800000;
		}
		return num;
	}

	public override string ToString()
	{
		return Number.FormatSingle(this, null, NumberFormatInfo.CurrentInfo);
	}

	public string ToString(IFormatProvider? provider)
	{
		return Number.FormatSingle(this, null, NumberFormatInfo.GetInstance(provider));
	}

	public string ToString(string? format)
	{
		return Number.FormatSingle(this, format, NumberFormatInfo.CurrentInfo);
	}

	public string ToString(string? format, IFormatProvider? provider)
	{
		return Number.FormatSingle(this, format, NumberFormatInfo.GetInstance(provider));
	}

	public bool TryFormat(Span<char> destination, out int charsWritten, ReadOnlySpan<char> format = default(ReadOnlySpan<char>), IFormatProvider? provider = null)
	{
		return Number.TryFormatSingle(this, format, NumberFormatInfo.GetInstance(provider), destination, out charsWritten);
	}

	public static float Parse(string s)
	{
		if (s == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.s);
		}
		return Number.ParseSingle(s, NumberStyles.Float | NumberStyles.AllowThousands, NumberFormatInfo.CurrentInfo);
	}

	public static float Parse(string s, NumberStyles style)
	{
		NumberFormatInfo.ValidateParseStyleFloatingPoint(style);
		if (s == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.s);
		}
		return Number.ParseSingle(s, style, NumberFormatInfo.CurrentInfo);
	}

	public static float Parse(string s, IFormatProvider? provider)
	{
		if (s == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.s);
		}
		return Number.ParseSingle(s, NumberStyles.Float | NumberStyles.AllowThousands, NumberFormatInfo.GetInstance(provider));
	}

	public static float Parse(string s, NumberStyles style, IFormatProvider? provider)
	{
		NumberFormatInfo.ValidateParseStyleFloatingPoint(style);
		if (s == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.s);
		}
		return Number.ParseSingle(s, style, NumberFormatInfo.GetInstance(provider));
	}

	public static float Parse(ReadOnlySpan<char> s, NumberStyles style = NumberStyles.Float | NumberStyles.AllowThousands, IFormatProvider? provider = null)
	{
		NumberFormatInfo.ValidateParseStyleFloatingPoint(style);
		return Number.ParseSingle(s, style, NumberFormatInfo.GetInstance(provider));
	}

	public static bool TryParse([NotNullWhen(true)] string? s, out float result)
	{
		if (s == null)
		{
			result = 0f;
			return false;
		}
		return TryParse((ReadOnlySpan<char>)s, NumberStyles.Float | NumberStyles.AllowThousands, NumberFormatInfo.CurrentInfo, out result);
	}

	public static bool TryParse(ReadOnlySpan<char> s, out float result)
	{
		return TryParse(s, NumberStyles.Float | NumberStyles.AllowThousands, NumberFormatInfo.CurrentInfo, out result);
	}

	public static bool TryParse([NotNullWhen(true)] string? s, NumberStyles style, IFormatProvider? provider, out float result)
	{
		NumberFormatInfo.ValidateParseStyleFloatingPoint(style);
		if (s == null)
		{
			result = 0f;
			return false;
		}
		return TryParse((ReadOnlySpan<char>)s, style, NumberFormatInfo.GetInstance(provider), out result);
	}

	public static bool TryParse(ReadOnlySpan<char> s, NumberStyles style, IFormatProvider? provider, out float result)
	{
		NumberFormatInfo.ValidateParseStyleFloatingPoint(style);
		return TryParse(s, style, NumberFormatInfo.GetInstance(provider), out result);
	}

	private static bool TryParse(ReadOnlySpan<char> s, NumberStyles style, NumberFormatInfo info, out float result)
	{
		return Number.TryParseSingle(s, style, info, out result);
	}

	public TypeCode GetTypeCode()
	{
		return TypeCode.Single;
	}

	bool IConvertible.ToBoolean(IFormatProvider provider)
	{
		return Convert.ToBoolean(this);
	}

	char IConvertible.ToChar(IFormatProvider provider)
	{
		throw new InvalidCastException(SR.Format(SR.InvalidCast_FromTo, "Single", "Char"));
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
		return this;
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
		throw new InvalidCastException(SR.Format(SR.InvalidCast_FromTo, "Single", "DateTime"));
	}

	object IConvertible.ToType(Type type, IFormatProvider provider)
	{
		return Convert.DefaultToType(this, type, provider);
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static float IAdditionOperators<float, float, float>.operator +(float left, float right)
	{
		return left + right;
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static bool IBinaryNumber<float>.IsPow2(float value)
	{
		uint num = BitConverter.SingleToUInt32Bits(value);
		uint num2 = (num >> 23) & 0xFFu;
		uint num3 = num & 0x7FFFFFu;
		if (value > 0f && num2 != 0 && num2 != 255)
		{
			return num3 == 0;
		}
		return false;
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static float IBinaryNumber<float>.Log2(float value)
	{
		return MathF.Log2(value);
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static float IBitwiseOperators<float, float, float>.operator &(float left, float right)
	{
		uint value = BitConverter.SingleToUInt32Bits(left) & BitConverter.SingleToUInt32Bits(right);
		return BitConverter.UInt32BitsToSingle(value);
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static float IBitwiseOperators<float, float, float>.operator |(float left, float right)
	{
		uint value = BitConverter.SingleToUInt32Bits(left) | BitConverter.SingleToUInt32Bits(right);
		return BitConverter.UInt32BitsToSingle(value);
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static float IBitwiseOperators<float, float, float>.operator ^(float left, float right)
	{
		uint value = BitConverter.SingleToUInt32Bits(left) ^ BitConverter.SingleToUInt32Bits(right);
		return BitConverter.UInt32BitsToSingle(value);
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static float IBitwiseOperators<float, float, float>.operator ~(float value)
	{
		uint value2 = ~BitConverter.SingleToUInt32Bits(value);
		return BitConverter.UInt32BitsToSingle(value2);
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static bool IComparisonOperators<float, float>.operator <(float left, float right)
	{
		return left < right;
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static bool IComparisonOperators<float, float>.operator <=(float left, float right)
	{
		return left <= right;
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static bool IComparisonOperators<float, float>.operator >(float left, float right)
	{
		return left > right;
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static bool IComparisonOperators<float, float>.operator >=(float left, float right)
	{
		return left >= right;
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static float IDecrementOperators<float>.operator --(float value)
	{
		return value -= 1f;
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static float IDivisionOperators<float, float, float>.operator /(float left, float right)
	{
		return left / right;
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static bool IEqualityOperators<float, float>.operator ==(float left, float right)
	{
		return left == right;
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static bool IEqualityOperators<float, float>.operator !=(float left, float right)
	{
		return left != right;
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static float IFloatingPoint<float>.Acos(float x)
	{
		return MathF.Acos(x);
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static float IFloatingPoint<float>.Acosh(float x)
	{
		return MathF.Acosh(x);
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static float IFloatingPoint<float>.Asin(float x)
	{
		return MathF.Asin(x);
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static float IFloatingPoint<float>.Asinh(float x)
	{
		return MathF.Asinh(x);
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static float IFloatingPoint<float>.Atan(float x)
	{
		return MathF.Atan(x);
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static float IFloatingPoint<float>.Atan2(float y, float x)
	{
		return MathF.Atan2(y, x);
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static float IFloatingPoint<float>.Atanh(float x)
	{
		return MathF.Atanh(x);
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static float IFloatingPoint<float>.BitIncrement(float x)
	{
		return MathF.BitIncrement(x);
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static float IFloatingPoint<float>.BitDecrement(float x)
	{
		return MathF.BitDecrement(x);
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static float IFloatingPoint<float>.Cbrt(float x)
	{
		return MathF.Cbrt(x);
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static float IFloatingPoint<float>.Ceiling(float x)
	{
		return MathF.Ceiling(x);
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static float IFloatingPoint<float>.CopySign(float x, float y)
	{
		return MathF.CopySign(x, y);
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static float IFloatingPoint<float>.Cos(float x)
	{
		return MathF.Cos(x);
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static float IFloatingPoint<float>.Cosh(float x)
	{
		return MathF.Cosh(x);
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static float IFloatingPoint<float>.Exp(float x)
	{
		return MathF.Exp(x);
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static float IFloatingPoint<float>.Floor(float x)
	{
		return MathF.Floor(x);
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static float IFloatingPoint<float>.FusedMultiplyAdd(float left, float right, float addend)
	{
		return MathF.FusedMultiplyAdd(left, right, addend);
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static float IFloatingPoint<float>.IEEERemainder(float left, float right)
	{
		return MathF.IEEERemainder(left, right);
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static TInteger IFloatingPoint<float>.ILogB<TInteger>(float x)
	{
		return TInteger.Create(MathF.ILogB(x));
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static float IFloatingPoint<float>.Log(float x)
	{
		return MathF.Log(x);
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static float IFloatingPoint<float>.Log(float x, float newBase)
	{
		return MathF.Log(x, newBase);
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static float IFloatingPoint<float>.Log2(float x)
	{
		return MathF.Log2(x);
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static float IFloatingPoint<float>.Log10(float x)
	{
		return MathF.Log10(x);
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static float IFloatingPoint<float>.MaxMagnitude(float x, float y)
	{
		return MathF.MaxMagnitude(x, y);
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static float IFloatingPoint<float>.MinMagnitude(float x, float y)
	{
		return MathF.MinMagnitude(x, y);
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static float IFloatingPoint<float>.Pow(float x, float y)
	{
		return MathF.Pow(x, y);
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static float IFloatingPoint<float>.Round(float x)
	{
		return MathF.Round(x);
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static float IFloatingPoint<float>.Round<TInteger>(float x, TInteger digits)
	{
		return MathF.Round(x, int.Create(digits));
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static float IFloatingPoint<float>.Round(float x, MidpointRounding mode)
	{
		return MathF.Round(x, mode);
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static float IFloatingPoint<float>.Round<TInteger>(float x, TInteger digits, MidpointRounding mode)
	{
		return MathF.Round(x, int.Create(digits), mode);
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static float IFloatingPoint<float>.ScaleB<TInteger>(float x, TInteger n)
	{
		return MathF.ScaleB(x, int.Create(n));
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static float IFloatingPoint<float>.Sin(float x)
	{
		return MathF.Sin(x);
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static float IFloatingPoint<float>.Sinh(float x)
	{
		return MathF.Sinh(x);
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static float IFloatingPoint<float>.Sqrt(float x)
	{
		return MathF.Sqrt(x);
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static float IFloatingPoint<float>.Tan(float x)
	{
		return MathF.Tan(x);
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static float IFloatingPoint<float>.Tanh(float x)
	{
		return MathF.Tanh(x);
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static float IFloatingPoint<float>.Truncate(float x)
	{
		return MathF.Truncate(x);
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static bool IFloatingPoint<float>.IsFinite(float x)
	{
		return IsFinite(x);
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static bool IFloatingPoint<float>.IsInfinity(float x)
	{
		return IsInfinity(x);
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static bool IFloatingPoint<float>.IsNaN(float x)
	{
		return IsNaN(x);
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static bool IFloatingPoint<float>.IsNegative(float x)
	{
		return IsNegative(x);
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static bool IFloatingPoint<float>.IsNegativeInfinity(float x)
	{
		return IsNegativeInfinity(x);
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static bool IFloatingPoint<float>.IsNormal(float x)
	{
		return IsNormal(x);
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static bool IFloatingPoint<float>.IsPositiveInfinity(float x)
	{
		return IsPositiveInfinity(x);
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static bool IFloatingPoint<float>.IsSubnormal(float x)
	{
		return IsSubnormal(x);
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static float IIncrementOperators<float>.operator ++(float value)
	{
		return value += 1f;
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static float IModulusOperators<float, float, float>.operator %(float left, float right)
	{
		return left % right;
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static float IMultiplyOperators<float, float, float>.operator *(float left, float right)
	{
		return left * right;
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static float INumber<float>.Abs(float value)
	{
		return MathF.Abs(value);
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static float INumber<float>.Clamp(float value, float min, float max)
	{
		return Math.Clamp(value, min, max);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static float INumber<float>.Create<TOther>(TOther value)
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
			return (float)(decimal)(object)value;
		}
		if (typeof(TOther) == typeof(double))
		{
			return (float)(double)(object)value;
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
		return 0f;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static float INumber<float>.CreateSaturating<TOther>(TOther value)
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
			return (float)(decimal)(object)value;
		}
		if (typeof(TOther) == typeof(double))
		{
			return (float)(double)(object)value;
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
		return 0f;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static float INumber<float>.CreateTruncating<TOther>(TOther value)
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
			return (float)(decimal)(object)value;
		}
		if (typeof(TOther) == typeof(double))
		{
			return (float)(double)(object)value;
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
		return 0f;
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static (float Quotient, float Remainder) INumber<float>.DivRem(float left, float right)
	{
		return (Quotient: left / right, Remainder: left % right);
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static float INumber<float>.Max(float x, float y)
	{
		return MathF.Max(x, y);
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static float INumber<float>.Min(float x, float y)
	{
		return MathF.Min(x, y);
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static float INumber<float>.Parse(string s, NumberStyles style, IFormatProvider provider)
	{
		return Parse(s, style, provider);
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static float INumber<float>.Parse(ReadOnlySpan<char> s, NumberStyles style, IFormatProvider provider)
	{
		return Parse(s, style, provider);
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static float INumber<float>.Sign(float value)
	{
		return MathF.Sign(value);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static bool INumber<float>.TryCreate<TOther>(TOther value, out float result)
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
			result = (float)(decimal)(object)value;
			return true;
		}
		if (typeof(TOther) == typeof(double))
		{
			result = (float)(double)(object)value;
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
		result = 0f;
		return false;
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static bool INumber<float>.TryParse([NotNullWhen(true)] string s, NumberStyles style, IFormatProvider provider, out float result)
	{
		return TryParse(s, style, provider, out result);
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static bool INumber<float>.TryParse(ReadOnlySpan<char> s, NumberStyles style, IFormatProvider provider, out float result)
	{
		return TryParse(s, style, provider, out result);
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static float IParseable<float>.Parse(string s, IFormatProvider provider)
	{
		return Parse(s, provider);
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static bool IParseable<float>.TryParse([NotNullWhen(true)] string s, IFormatProvider provider, out float result)
	{
		return TryParse(s, NumberStyles.Float | NumberStyles.AllowThousands, provider, out result);
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static float ISpanParseable<float>.Parse(ReadOnlySpan<char> s, IFormatProvider provider)
	{
		return Parse(s, NumberStyles.Float | NumberStyles.AllowThousands, provider);
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static bool ISpanParseable<float>.TryParse(ReadOnlySpan<char> s, IFormatProvider provider, out float result)
	{
		return TryParse(s, NumberStyles.Float | NumberStyles.AllowThousands, provider, out result);
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static float ISubtractionOperators<float, float, float>.operator -(float left, float right)
	{
		return left - right;
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static float IUnaryNegationOperators<float, float>.operator -(float value)
	{
		return 0f - value;
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static float IUnaryPlusOperators<float, float>.operator +(float value)
	{
		return value;
	}
}
