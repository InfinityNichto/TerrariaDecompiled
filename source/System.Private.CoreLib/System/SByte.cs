using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.Versioning;

namespace System;

[Serializable]
[CLSCompliant(false)]
[TypeForwardedFrom("mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
public readonly struct SByte : IComparable, IConvertible, ISpanFormattable, IFormattable, IComparable<sbyte>, IEquatable<sbyte>, IBinaryInteger<sbyte>, IBinaryNumber<sbyte>, IBitwiseOperators<sbyte, sbyte, sbyte>, INumber<sbyte>, IAdditionOperators<sbyte, sbyte, sbyte>, IAdditiveIdentity<sbyte, sbyte>, IComparisonOperators<sbyte, sbyte>, IEqualityOperators<sbyte, sbyte>, IDecrementOperators<sbyte>, IDivisionOperators<sbyte, sbyte, sbyte>, IIncrementOperators<sbyte>, IModulusOperators<sbyte, sbyte, sbyte>, IMultiplicativeIdentity<sbyte, sbyte>, IMultiplyOperators<sbyte, sbyte, sbyte>, ISpanParseable<sbyte>, IParseable<sbyte>, ISubtractionOperators<sbyte, sbyte, sbyte>, IUnaryNegationOperators<sbyte, sbyte>, IUnaryPlusOperators<sbyte, sbyte>, IShiftOperators<sbyte, sbyte>, IMinMaxValue<sbyte>, ISignedNumber<sbyte>
{
	private readonly sbyte m_value;

	public const sbyte MaxValue = 127;

	public const sbyte MinValue = -128;

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static sbyte IAdditiveIdentity<sbyte, sbyte>.AdditiveIdentity => 0;

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static sbyte IMinMaxValue<sbyte>.MinValue => sbyte.MinValue;

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static sbyte IMinMaxValue<sbyte>.MaxValue => sbyte.MaxValue;

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static sbyte IMultiplicativeIdentity<sbyte, sbyte>.MultiplicativeIdentity => 1;

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static sbyte INumber<sbyte>.One => 1;

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static sbyte INumber<sbyte>.Zero => 0;

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static sbyte ISignedNumber<sbyte>.NegativeOne => -1;

	public int CompareTo(object? obj)
	{
		if (obj == null)
		{
			return 1;
		}
		if (!(obj is sbyte))
		{
			throw new ArgumentException(SR.Arg_MustBeSByte);
		}
		return this - (sbyte)obj;
	}

	public int CompareTo(sbyte value)
	{
		return this - value;
	}

	public override bool Equals([NotNullWhen(true)] object? obj)
	{
		if (!(obj is sbyte))
		{
			return false;
		}
		return this == (sbyte)obj;
	}

	[NonVersionable]
	public bool Equals(sbyte obj)
	{
		return this == obj;
	}

	public override int GetHashCode()
	{
		return this;
	}

	public override string ToString()
	{
		return Number.Int32ToDecStr(this);
	}

	public string ToString(string? format)
	{
		return ToString(format, null);
	}

	public string ToString(IFormatProvider? provider)
	{
		return Number.FormatInt32(this, 0, null, provider);
	}

	public string ToString(string? format, IFormatProvider? provider)
	{
		return Number.FormatInt32(this, 255, format, provider);
	}

	public bool TryFormat(Span<char> destination, out int charsWritten, ReadOnlySpan<char> format = default(ReadOnlySpan<char>), IFormatProvider? provider = null)
	{
		return Number.TryFormatInt32(this, 255, format, provider, destination, out charsWritten);
	}

	public static sbyte Parse(string s)
	{
		if (s == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.s);
		}
		return Parse((ReadOnlySpan<char>)s, NumberStyles.Integer, NumberFormatInfo.CurrentInfo);
	}

	public static sbyte Parse(string s, NumberStyles style)
	{
		NumberFormatInfo.ValidateParseStyleInteger(style);
		if (s == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.s);
		}
		return Parse((ReadOnlySpan<char>)s, style, NumberFormatInfo.CurrentInfo);
	}

	public static sbyte Parse(string s, IFormatProvider? provider)
	{
		if (s == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.s);
		}
		return Parse((ReadOnlySpan<char>)s, NumberStyles.Integer, NumberFormatInfo.GetInstance(provider));
	}

	public static sbyte Parse(string s, NumberStyles style, IFormatProvider? provider)
	{
		NumberFormatInfo.ValidateParseStyleInteger(style);
		if (s == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.s);
		}
		return Parse((ReadOnlySpan<char>)s, style, NumberFormatInfo.GetInstance(provider));
	}

	public static sbyte Parse(ReadOnlySpan<char> s, NumberStyles style = NumberStyles.Integer, IFormatProvider? provider = null)
	{
		NumberFormatInfo.ValidateParseStyleInteger(style);
		return Parse(s, style, NumberFormatInfo.GetInstance(provider));
	}

	private static sbyte Parse(ReadOnlySpan<char> s, NumberStyles style, NumberFormatInfo info)
	{
		int result;
		Number.ParsingStatus parsingStatus = Number.TryParseInt32(s, style, info, out result);
		if (parsingStatus != 0)
		{
			Number.ThrowOverflowOrFormatException(parsingStatus, TypeCode.SByte);
		}
		if ((uint)(result - -128 - ((int)(style & NumberStyles.AllowHexSpecifier) >> 2)) > 255u)
		{
			Number.ThrowOverflowException(TypeCode.SByte);
		}
		return (sbyte)result;
	}

	public static bool TryParse([NotNullWhen(true)] string? s, out sbyte result)
	{
		if (s == null)
		{
			result = 0;
			return false;
		}
		return TryParse((ReadOnlySpan<char>)s, NumberStyles.Integer, NumberFormatInfo.CurrentInfo, out result);
	}

	public static bool TryParse(ReadOnlySpan<char> s, out sbyte result)
	{
		return TryParse(s, NumberStyles.Integer, NumberFormatInfo.CurrentInfo, out result);
	}

	public static bool TryParse([NotNullWhen(true)] string? s, NumberStyles style, IFormatProvider? provider, out sbyte result)
	{
		NumberFormatInfo.ValidateParseStyleInteger(style);
		if (s == null)
		{
			result = 0;
			return false;
		}
		return TryParse((ReadOnlySpan<char>)s, style, NumberFormatInfo.GetInstance(provider), out result);
	}

	public static bool TryParse(ReadOnlySpan<char> s, NumberStyles style, IFormatProvider? provider, out sbyte result)
	{
		NumberFormatInfo.ValidateParseStyleInteger(style);
		return TryParse(s, style, NumberFormatInfo.GetInstance(provider), out result);
	}

	private static bool TryParse(ReadOnlySpan<char> s, NumberStyles style, NumberFormatInfo info, out sbyte result)
	{
		if (Number.TryParseInt32(s, style, info, out var result2) != 0 || (uint)(result2 - -128 - ((int)(style & NumberStyles.AllowHexSpecifier) >> 2)) > 255u)
		{
			result = 0;
			return false;
		}
		result = (sbyte)result2;
		return true;
	}

	public TypeCode GetTypeCode()
	{
		return TypeCode.SByte;
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
		return this;
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
		return this;
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
		throw new InvalidCastException(SR.Format(SR.InvalidCast_FromTo, "SByte", "DateTime"));
	}

	object IConvertible.ToType(Type type, IFormatProvider provider)
	{
		return Convert.DefaultToType(this, type, provider);
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static sbyte IAdditionOperators<sbyte, sbyte, sbyte>.operator +(sbyte left, sbyte right)
	{
		return (sbyte)(left + right);
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static sbyte IBinaryInteger<sbyte>.LeadingZeroCount(sbyte value)
	{
		return (sbyte)(BitOperations.LeadingZeroCount((byte)value) - 24);
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static sbyte IBinaryInteger<sbyte>.PopCount(sbyte value)
	{
		return (sbyte)BitOperations.PopCount((byte)value);
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static sbyte IBinaryInteger<sbyte>.RotateLeft(sbyte value, int rotateAmount)
	{
		return (sbyte)((value << (rotateAmount & 7)) | ((byte)value >> ((8 - rotateAmount) & 7)));
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static sbyte IBinaryInteger<sbyte>.RotateRight(sbyte value, int rotateAmount)
	{
		return (sbyte)(((byte)value >> (rotateAmount & 7)) | (value << ((8 - rotateAmount) & 7)));
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static sbyte IBinaryInteger<sbyte>.TrailingZeroCount(sbyte value)
	{
		return (sbyte)(BitOperations.TrailingZeroCount(value << 24) - 24);
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static bool IBinaryNumber<sbyte>.IsPow2(sbyte value)
	{
		return BitOperations.IsPow2(value);
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static sbyte IBinaryNumber<sbyte>.Log2(sbyte value)
	{
		if (value < 0)
		{
			ThrowHelper.ThrowValueArgumentOutOfRange_NeedNonNegNumException();
		}
		return (sbyte)BitOperations.Log2((byte)value);
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static sbyte IBitwiseOperators<sbyte, sbyte, sbyte>.operator &(sbyte left, sbyte right)
	{
		return (sbyte)(left & right);
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static sbyte IBitwiseOperators<sbyte, sbyte, sbyte>.operator |(sbyte left, sbyte right)
	{
		return (sbyte)(left | right);
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static sbyte IBitwiseOperators<sbyte, sbyte, sbyte>.operator ^(sbyte left, sbyte right)
	{
		return (sbyte)(left ^ right);
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static sbyte IBitwiseOperators<sbyte, sbyte, sbyte>.operator ~(sbyte value)
	{
		return (sbyte)(~value);
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static bool IComparisonOperators<sbyte, sbyte>.operator <(sbyte left, sbyte right)
	{
		return left < right;
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static bool IComparisonOperators<sbyte, sbyte>.operator <=(sbyte left, sbyte right)
	{
		return left <= right;
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static bool IComparisonOperators<sbyte, sbyte>.operator >(sbyte left, sbyte right)
	{
		return left > right;
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static bool IComparisonOperators<sbyte, sbyte>.operator >=(sbyte left, sbyte right)
	{
		return left >= right;
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static sbyte IDecrementOperators<sbyte>.operator --(sbyte value)
	{
		return --value;
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static sbyte IDivisionOperators<sbyte, sbyte, sbyte>.operator /(sbyte left, sbyte right)
	{
		return (sbyte)(left / right);
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static bool IEqualityOperators<sbyte, sbyte>.operator ==(sbyte left, sbyte right)
	{
		return left == right;
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static bool IEqualityOperators<sbyte, sbyte>.operator !=(sbyte left, sbyte right)
	{
		return left != right;
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static sbyte IIncrementOperators<sbyte>.operator ++(sbyte value)
	{
		return ++value;
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static sbyte IModulusOperators<sbyte, sbyte, sbyte>.operator %(sbyte left, sbyte right)
	{
		return (sbyte)(left % right);
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static sbyte IMultiplyOperators<sbyte, sbyte, sbyte>.operator *(sbyte left, sbyte right)
	{
		return (sbyte)(left * right);
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static sbyte INumber<sbyte>.Abs(sbyte value)
	{
		return Math.Abs(value);
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static sbyte INumber<sbyte>.Clamp(sbyte value, sbyte min, sbyte max)
	{
		return Math.Clamp(value, min, max);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static sbyte INumber<sbyte>.Create<TOther>(TOther value)
	{
		checked
		{
			if (typeof(TOther) == typeof(byte))
			{
				return (sbyte)(byte)(object)value;
			}
			if (typeof(TOther) == typeof(char))
			{
				return (sbyte)(char)(object)value;
			}
			if (typeof(TOther) == typeof(decimal))
			{
				return (sbyte)(decimal)(object)value;
			}
			if (typeof(TOther) == typeof(double))
			{
				return (sbyte)(double)(object)value;
			}
			if (typeof(TOther) == typeof(short))
			{
				return (sbyte)(short)(object)value;
			}
			if (typeof(TOther) == typeof(int))
			{
				return (sbyte)(int)(object)value;
			}
			if (typeof(TOther) == typeof(long))
			{
				return (sbyte)(long)(object)value;
			}
			if (typeof(TOther) == typeof(IntPtr))
			{
				return (sbyte)(nint)(IntPtr)(object)value;
			}
			if (typeof(TOther) == typeof(sbyte))
			{
				return (sbyte)(object)value;
			}
			if (typeof(TOther) == typeof(float))
			{
				return (sbyte)(float)(object)value;
			}
			if (typeof(TOther) == typeof(ushort))
			{
				return (sbyte)(ushort)(object)value;
			}
			if (typeof(TOther) == typeof(uint))
			{
				return (sbyte)(uint)(object)value;
			}
			if (typeof(TOther) == typeof(ulong))
			{
				return (sbyte)(ulong)(object)value;
			}
			if (typeof(TOther) == typeof(UIntPtr))
			{
				return (sbyte)(nuint)(UIntPtr)(object)value;
			}
			ThrowHelper.ThrowNotSupportedException();
			return 0;
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static sbyte INumber<sbyte>.CreateSaturating<TOther>(TOther value)
	{
		if (typeof(TOther) == typeof(byte))
		{
			byte b = (byte)(object)value;
			if (b <= 127)
			{
				return (sbyte)b;
			}
			return sbyte.MaxValue;
		}
		if (typeof(TOther) == typeof(char))
		{
			char c = (char)(object)value;
			if (c <= '\u007f')
			{
				return (sbyte)c;
			}
			return sbyte.MaxValue;
		}
		if (typeof(TOther) == typeof(decimal))
		{
			decimal num = (decimal)(object)value;
			if (!(num > 127m))
			{
				if (!(num < -128m))
				{
					return (sbyte)num;
				}
				return sbyte.MinValue;
			}
			return sbyte.MaxValue;
		}
		if (typeof(TOther) == typeof(double))
		{
			double num2 = (double)(object)value;
			if (!(num2 > 127.0))
			{
				if (!(num2 < -128.0))
				{
					return (sbyte)num2;
				}
				return sbyte.MinValue;
			}
			return sbyte.MaxValue;
		}
		if (typeof(TOther) == typeof(short))
		{
			short num3 = (short)(object)value;
			if (num3 <= 127)
			{
				if (num3 >= -128)
				{
					return (sbyte)num3;
				}
				return sbyte.MinValue;
			}
			return sbyte.MaxValue;
		}
		if (typeof(TOther) == typeof(int))
		{
			int num4 = (int)(object)value;
			if (num4 <= 127)
			{
				if (num4 >= -128)
				{
					return (sbyte)num4;
				}
				return sbyte.MinValue;
			}
			return sbyte.MaxValue;
		}
		if (typeof(TOther) == typeof(long))
		{
			long num5 = (long)(object)value;
			if (num5 <= 127)
			{
				if (num5 >= -128)
				{
					return (sbyte)num5;
				}
				return sbyte.MinValue;
			}
			return sbyte.MaxValue;
		}
		if (typeof(TOther) == typeof(IntPtr))
		{
			IntPtr intPtr = (IntPtr)(object)value;
			if ((nint)intPtr <= 127)
			{
				if ((nint)intPtr >= -128)
				{
					return (sbyte)(nint)intPtr;
				}
				return sbyte.MinValue;
			}
			return sbyte.MaxValue;
		}
		if (typeof(TOther) == typeof(sbyte))
		{
			return (sbyte)(object)value;
		}
		if (typeof(TOther) == typeof(float))
		{
			float num6 = (float)(object)value;
			if (!(num6 > 127f))
			{
				if (!(num6 < -128f))
				{
					return (sbyte)num6;
				}
				return sbyte.MinValue;
			}
			return sbyte.MaxValue;
		}
		if (typeof(TOther) == typeof(ushort))
		{
			ushort num7 = (ushort)(object)value;
			if (num7 <= 127)
			{
				return (sbyte)num7;
			}
			return sbyte.MaxValue;
		}
		if (typeof(TOther) == typeof(uint))
		{
			uint num8 = (uint)(object)value;
			if ((long)num8 <= 127L)
			{
				return (sbyte)num8;
			}
			return sbyte.MaxValue;
		}
		if (typeof(TOther) == typeof(ulong))
		{
			ulong num9 = (ulong)(object)value;
			if (num9 <= 127)
			{
				return (sbyte)num9;
			}
			return sbyte.MaxValue;
		}
		if (typeof(TOther) == typeof(UIntPtr))
		{
			UIntPtr uIntPtr = (UIntPtr)(object)value;
			if ((nuint)uIntPtr <= 127)
			{
				return (sbyte)(nuint)uIntPtr;
			}
			return sbyte.MaxValue;
		}
		ThrowHelper.ThrowNotSupportedException();
		return 0;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static sbyte INumber<sbyte>.CreateTruncating<TOther>(TOther value)
	{
		if (typeof(TOther) == typeof(byte))
		{
			return (sbyte)(byte)(object)value;
		}
		if (typeof(TOther) == typeof(char))
		{
			return (sbyte)(char)(object)value;
		}
		if (typeof(TOther) == typeof(decimal))
		{
			return (sbyte)(decimal)(object)value;
		}
		if (typeof(TOther) == typeof(double))
		{
			return (sbyte)(double)(object)value;
		}
		if (typeof(TOther) == typeof(short))
		{
			return (sbyte)(short)(object)value;
		}
		if (typeof(TOther) == typeof(int))
		{
			return (sbyte)(int)(object)value;
		}
		if (typeof(TOther) == typeof(long))
		{
			return (sbyte)(long)(object)value;
		}
		if (typeof(TOther) == typeof(IntPtr))
		{
			return (sbyte)(nint)(IntPtr)(object)value;
		}
		if (typeof(TOther) == typeof(sbyte))
		{
			return (sbyte)(object)value;
		}
		if (typeof(TOther) == typeof(float))
		{
			return (sbyte)(float)(object)value;
		}
		if (typeof(TOther) == typeof(ushort))
		{
			return (sbyte)(ushort)(object)value;
		}
		if (typeof(TOther) == typeof(uint))
		{
			return (sbyte)(uint)(object)value;
		}
		if (typeof(TOther) == typeof(ulong))
		{
			return (sbyte)(ulong)(object)value;
		}
		if (typeof(TOther) == typeof(UIntPtr))
		{
			return (sbyte)(nuint)(UIntPtr)(object)value;
		}
		ThrowHelper.ThrowNotSupportedException();
		return 0;
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static (sbyte Quotient, sbyte Remainder) INumber<sbyte>.DivRem(sbyte left, sbyte right)
	{
		return Math.DivRem(left, right);
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static sbyte INumber<sbyte>.Max(sbyte x, sbyte y)
	{
		return Math.Max(x, y);
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static sbyte INumber<sbyte>.Min(sbyte x, sbyte y)
	{
		return Math.Min(x, y);
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static sbyte INumber<sbyte>.Parse(string s, NumberStyles style, IFormatProvider provider)
	{
		return Parse(s, style, provider);
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static sbyte INumber<sbyte>.Parse(ReadOnlySpan<char> s, NumberStyles style, IFormatProvider provider)
	{
		return Parse(s, style, provider);
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static sbyte INumber<sbyte>.Sign(sbyte value)
	{
		return (sbyte)Math.Sign(value);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static bool INumber<sbyte>.TryCreate<TOther>(TOther value, out sbyte result)
	{
		if (typeof(TOther) == typeof(byte))
		{
			byte b = (byte)(object)value;
			if (b > 127)
			{
				result = 0;
				return false;
			}
			result = (sbyte)b;
			return true;
		}
		if (typeof(TOther) == typeof(char))
		{
			char c = (char)(object)value;
			if (c > '\u007f')
			{
				result = 0;
				return false;
			}
			result = (sbyte)c;
			return true;
		}
		if (typeof(TOther) == typeof(decimal))
		{
			decimal num = (decimal)(object)value;
			if (num < -128m || num > 127m)
			{
				result = 0;
				return false;
			}
			result = (sbyte)num;
			return true;
		}
		if (typeof(TOther) == typeof(double))
		{
			double num2 = (double)(object)value;
			if (num2 < -128.0 || num2 > 127.0)
			{
				result = 0;
				return false;
			}
			result = (sbyte)num2;
			return true;
		}
		if (typeof(TOther) == typeof(short))
		{
			short num3 = (short)(object)value;
			if (num3 < -128 || num3 > 127)
			{
				result = 0;
				return false;
			}
			result = (sbyte)num3;
			return true;
		}
		if (typeof(TOther) == typeof(int))
		{
			int num4 = (int)(object)value;
			if (num4 < -128 || num4 > 127)
			{
				result = 0;
				return false;
			}
			result = (sbyte)num4;
			return true;
		}
		if (typeof(TOther) == typeof(long))
		{
			long num5 = (long)(object)value;
			if (num5 < -128 || num5 > 127)
			{
				result = 0;
				return false;
			}
			result = (sbyte)num5;
			return true;
		}
		if (typeof(TOther) == typeof(IntPtr))
		{
			IntPtr intPtr = (IntPtr)(object)value;
			if ((nint)intPtr < -128 || (nint)intPtr > 127)
			{
				result = 0;
				return false;
			}
			result = (sbyte)(nint)intPtr;
			return true;
		}
		if (typeof(TOther) == typeof(sbyte))
		{
			result = (sbyte)(object)value;
			return true;
		}
		if (typeof(TOther) == typeof(float))
		{
			float num6 = (float)(object)value;
			if (num6 < -128f || num6 > 127f)
			{
				result = 0;
				return false;
			}
			result = (sbyte)num6;
			return true;
		}
		if (typeof(TOther) == typeof(ushort))
		{
			ushort num7 = (ushort)(object)value;
			if (num7 > 127)
			{
				result = 0;
				return false;
			}
			result = (sbyte)num7;
			return true;
		}
		if (typeof(TOther) == typeof(uint))
		{
			uint num8 = (uint)(object)value;
			if ((long)num8 > 127L)
			{
				result = 0;
				return false;
			}
			result = (sbyte)num8;
			return true;
		}
		if (typeof(TOther) == typeof(ulong))
		{
			ulong num9 = (ulong)(object)value;
			if (num9 > 127)
			{
				result = 0;
				return false;
			}
			result = (sbyte)num9;
			return true;
		}
		if (typeof(TOther) == typeof(UIntPtr))
		{
			UIntPtr uIntPtr = (UIntPtr)(object)value;
			if ((nuint)uIntPtr > 127)
			{
				result = 0;
				return false;
			}
			result = (sbyte)(nuint)uIntPtr;
			return true;
		}
		ThrowHelper.ThrowNotSupportedException();
		result = 0;
		return false;
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static bool INumber<sbyte>.TryParse([NotNullWhen(true)] string s, NumberStyles style, IFormatProvider provider, out sbyte result)
	{
		return TryParse(s, style, provider, out result);
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static bool INumber<sbyte>.TryParse(ReadOnlySpan<char> s, NumberStyles style, IFormatProvider provider, out sbyte result)
	{
		return TryParse(s, style, provider, out result);
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static sbyte IParseable<sbyte>.Parse(string s, IFormatProvider provider)
	{
		return Parse(s, provider);
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static bool IParseable<sbyte>.TryParse([NotNullWhen(true)] string s, IFormatProvider provider, out sbyte result)
	{
		return TryParse(s, NumberStyles.Integer, provider, out result);
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static sbyte IShiftOperators<sbyte, sbyte>.operator <<(sbyte value, int shiftAmount)
	{
		return (sbyte)(value << shiftAmount);
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static sbyte IShiftOperators<sbyte, sbyte>.operator >>(sbyte value, int shiftAmount)
	{
		return (sbyte)(value >> shiftAmount);
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static sbyte ISpanParseable<sbyte>.Parse(ReadOnlySpan<char> s, IFormatProvider provider)
	{
		return Parse(s, NumberStyles.Integer, provider);
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static bool ISpanParseable<sbyte>.TryParse(ReadOnlySpan<char> s, IFormatProvider provider, out sbyte result)
	{
		return TryParse(s, NumberStyles.Integer, provider, out result);
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static sbyte ISubtractionOperators<sbyte, sbyte, sbyte>.operator -(sbyte left, sbyte right)
	{
		return (sbyte)(left - right);
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static sbyte IUnaryNegationOperators<sbyte, sbyte>.operator -(sbyte value)
	{
		return (sbyte)(-value);
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static sbyte IUnaryPlusOperators<sbyte, sbyte>.operator +(sbyte value)
	{
		return value;
	}
}
