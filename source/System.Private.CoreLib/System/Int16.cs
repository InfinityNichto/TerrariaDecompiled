using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.Versioning;

namespace System;

[Serializable]
[TypeForwardedFrom("mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
public readonly struct Int16 : IComparable, IConvertible, ISpanFormattable, IFormattable, IComparable<short>, IEquatable<short>, IBinaryInteger<short>, IBinaryNumber<short>, IBitwiseOperators<short, short, short>, INumber<short>, IAdditionOperators<short, short, short>, IAdditiveIdentity<short, short>, IComparisonOperators<short, short>, IEqualityOperators<short, short>, IDecrementOperators<short>, IDivisionOperators<short, short, short>, IIncrementOperators<short>, IModulusOperators<short, short, short>, IMultiplicativeIdentity<short, short>, IMultiplyOperators<short, short, short>, ISpanParseable<short>, IParseable<short>, ISubtractionOperators<short, short, short>, IUnaryNegationOperators<short, short>, IUnaryPlusOperators<short, short>, IShiftOperators<short, short>, IMinMaxValue<short>, ISignedNumber<short>
{
	private readonly short m_value;

	public const short MaxValue = 32767;

	public const short MinValue = -32768;

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static short IAdditiveIdentity<short, short>.AdditiveIdentity => 0;

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static short IMinMaxValue<short>.MinValue => short.MinValue;

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static short IMinMaxValue<short>.MaxValue => short.MaxValue;

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static short IMultiplicativeIdentity<short, short>.MultiplicativeIdentity => 1;

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static short INumber<short>.One => 1;

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static short INumber<short>.Zero => 0;

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static short ISignedNumber<short>.NegativeOne => -1;

	public int CompareTo(object? value)
	{
		if (value == null)
		{
			return 1;
		}
		if (value is short)
		{
			return this - (short)value;
		}
		throw new ArgumentException(SR.Arg_MustBeInt16);
	}

	public int CompareTo(short value)
	{
		return this - value;
	}

	public override bool Equals([NotNullWhen(true)] object? obj)
	{
		if (!(obj is short))
		{
			return false;
		}
		return this == (short)obj;
	}

	[NonVersionable]
	public bool Equals(short obj)
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

	public string ToString(IFormatProvider? provider)
	{
		return Number.FormatInt32(this, 0, null, provider);
	}

	public string ToString(string? format)
	{
		return ToString(format, null);
	}

	public string ToString(string? format, IFormatProvider? provider)
	{
		return Number.FormatInt32(this, 65535, format, provider);
	}

	public bool TryFormat(Span<char> destination, out int charsWritten, ReadOnlySpan<char> format = default(ReadOnlySpan<char>), IFormatProvider? provider = null)
	{
		return Number.TryFormatInt32(this, 65535, format, provider, destination, out charsWritten);
	}

	public static short Parse(string s)
	{
		if (s == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.s);
		}
		return Parse((ReadOnlySpan<char>)s, NumberStyles.Integer, NumberFormatInfo.CurrentInfo);
	}

	public static short Parse(string s, NumberStyles style)
	{
		NumberFormatInfo.ValidateParseStyleInteger(style);
		if (s == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.s);
		}
		return Parse((ReadOnlySpan<char>)s, style, NumberFormatInfo.CurrentInfo);
	}

	public static short Parse(string s, IFormatProvider? provider)
	{
		if (s == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.s);
		}
		return Parse((ReadOnlySpan<char>)s, NumberStyles.Integer, NumberFormatInfo.GetInstance(provider));
	}

	public static short Parse(string s, NumberStyles style, IFormatProvider? provider)
	{
		NumberFormatInfo.ValidateParseStyleInteger(style);
		if (s == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.s);
		}
		return Parse((ReadOnlySpan<char>)s, style, NumberFormatInfo.GetInstance(provider));
	}

	public static short Parse(ReadOnlySpan<char> s, NumberStyles style = NumberStyles.Integer, IFormatProvider? provider = null)
	{
		NumberFormatInfo.ValidateParseStyleInteger(style);
		return Parse(s, style, NumberFormatInfo.GetInstance(provider));
	}

	private static short Parse(ReadOnlySpan<char> s, NumberStyles style, NumberFormatInfo info)
	{
		int result;
		Number.ParsingStatus parsingStatus = Number.TryParseInt32(s, style, info, out result);
		if (parsingStatus != 0)
		{
			Number.ThrowOverflowOrFormatException(parsingStatus, TypeCode.Int16);
		}
		if ((uint)(result - -32768 - ((int)(style & NumberStyles.AllowHexSpecifier) << 6)) > 65535u)
		{
			Number.ThrowOverflowException(TypeCode.Int16);
		}
		return (short)result;
	}

	public static bool TryParse([NotNullWhen(true)] string? s, out short result)
	{
		if (s == null)
		{
			result = 0;
			return false;
		}
		return TryParse((ReadOnlySpan<char>)s, NumberStyles.Integer, NumberFormatInfo.CurrentInfo, out result);
	}

	public static bool TryParse(ReadOnlySpan<char> s, out short result)
	{
		return TryParse(s, NumberStyles.Integer, NumberFormatInfo.CurrentInfo, out result);
	}

	public static bool TryParse([NotNullWhen(true)] string? s, NumberStyles style, IFormatProvider? provider, out short result)
	{
		NumberFormatInfo.ValidateParseStyleInteger(style);
		if (s == null)
		{
			result = 0;
			return false;
		}
		return TryParse((ReadOnlySpan<char>)s, style, NumberFormatInfo.GetInstance(provider), out result);
	}

	public static bool TryParse(ReadOnlySpan<char> s, NumberStyles style, IFormatProvider? provider, out short result)
	{
		NumberFormatInfo.ValidateParseStyleInteger(style);
		return TryParse(s, style, NumberFormatInfo.GetInstance(provider), out result);
	}

	private static bool TryParse(ReadOnlySpan<char> s, NumberStyles style, NumberFormatInfo info, out short result)
	{
		if (Number.TryParseInt32(s, style, info, out var result2) != 0 || (uint)(result2 - -32768 - ((int)(style & NumberStyles.AllowHexSpecifier) << 6)) > 65535u)
		{
			result = 0;
			return false;
		}
		result = (short)result2;
		return true;
	}

	public TypeCode GetTypeCode()
	{
		return TypeCode.Int16;
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
		return this;
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
		throw new InvalidCastException(SR.Format(SR.InvalidCast_FromTo, "Int16", "DateTime"));
	}

	object IConvertible.ToType(Type type, IFormatProvider provider)
	{
		return Convert.DefaultToType(this, type, provider);
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static short IAdditionOperators<short, short, short>.operator +(short left, short right)
	{
		return (short)(left + right);
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static short IBinaryInteger<short>.LeadingZeroCount(short value)
	{
		return (short)(BitOperations.LeadingZeroCount((ushort)value) - 16);
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static short IBinaryInteger<short>.PopCount(short value)
	{
		return (short)BitOperations.PopCount((ushort)value);
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static short IBinaryInteger<short>.RotateLeft(short value, int rotateAmount)
	{
		return (short)((value << (rotateAmount & 0xF)) | ((ushort)value >> ((16 - rotateAmount) & 0xF)));
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static short IBinaryInteger<short>.RotateRight(short value, int rotateAmount)
	{
		return (short)(((ushort)value >> (rotateAmount & 0xF)) | (value << ((16 - rotateAmount) & 0xF)));
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static short IBinaryInteger<short>.TrailingZeroCount(short value)
	{
		return (byte)(BitOperations.TrailingZeroCount(value << 16) - 16);
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static bool IBinaryNumber<short>.IsPow2(short value)
	{
		return BitOperations.IsPow2(value);
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static short IBinaryNumber<short>.Log2(short value)
	{
		if (value < 0)
		{
			ThrowHelper.ThrowValueArgumentOutOfRange_NeedNonNegNumException();
		}
		return (short)BitOperations.Log2((ushort)value);
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static short IBitwiseOperators<short, short, short>.operator &(short left, short right)
	{
		return (short)(left & right);
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static short IBitwiseOperators<short, short, short>.operator |(short left, short right)
	{
		return (short)(left | right);
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static short IBitwiseOperators<short, short, short>.operator ^(short left, short right)
	{
		return (short)(left ^ right);
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static short IBitwiseOperators<short, short, short>.operator ~(short value)
	{
		return (short)(~value);
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static bool IComparisonOperators<short, short>.operator <(short left, short right)
	{
		return left < right;
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static bool IComparisonOperators<short, short>.operator <=(short left, short right)
	{
		return left <= right;
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static bool IComparisonOperators<short, short>.operator >(short left, short right)
	{
		return left > right;
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static bool IComparisonOperators<short, short>.operator >=(short left, short right)
	{
		return left >= right;
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static short IDecrementOperators<short>.operator --(short value)
	{
		return --value;
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static short IDivisionOperators<short, short, short>.operator /(short left, short right)
	{
		return (short)(left / right);
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static bool IEqualityOperators<short, short>.operator ==(short left, short right)
	{
		return left == right;
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static bool IEqualityOperators<short, short>.operator !=(short left, short right)
	{
		return left != right;
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static short IIncrementOperators<short>.operator ++(short value)
	{
		return ++value;
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static short IModulusOperators<short, short, short>.operator %(short left, short right)
	{
		return (short)(left % right);
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static short IMultiplyOperators<short, short, short>.operator *(short left, short right)
	{
		return (short)(left * right);
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static short INumber<short>.Abs(short value)
	{
		return Math.Abs(value);
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static short INumber<short>.Clamp(short value, short min, short max)
	{
		return Math.Clamp(value, min, max);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static short INumber<short>.Create<TOther>(TOther value)
	{
		if (typeof(TOther) == typeof(byte))
		{
			return (byte)(object)value;
		}
		checked
		{
			if (typeof(TOther) == typeof(char))
			{
				return (short)(char)(object)value;
			}
			if (typeof(TOther) == typeof(decimal))
			{
				return (short)(decimal)(object)value;
			}
			if (typeof(TOther) == typeof(double))
			{
				return (short)(double)(object)value;
			}
			if (typeof(TOther) == typeof(short))
			{
				return (short)(object)value;
			}
			if (typeof(TOther) == typeof(int))
			{
				return (short)(int)(object)value;
			}
			if (typeof(TOther) == typeof(long))
			{
				return (short)(long)(object)value;
			}
			if (typeof(TOther) == typeof(IntPtr))
			{
				return (short)(nint)(IntPtr)(object)value;
			}
			if (typeof(TOther) == typeof(sbyte))
			{
				return (sbyte)(object)value;
			}
			if (typeof(TOther) == typeof(float))
			{
				return (short)(float)(object)value;
			}
			if (typeof(TOther) == typeof(ushort))
			{
				return (short)(ushort)(object)value;
			}
			if (typeof(TOther) == typeof(uint))
			{
				return (short)(uint)(object)value;
			}
			if (typeof(TOther) == typeof(ulong))
			{
				return (short)(ulong)(object)value;
			}
			if (typeof(TOther) == typeof(UIntPtr))
			{
				return (short)(nuint)(UIntPtr)(object)value;
			}
			ThrowHelper.ThrowNotSupportedException();
			return 0;
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static short INumber<short>.CreateSaturating<TOther>(TOther value)
	{
		if (typeof(TOther) == typeof(byte))
		{
			return (byte)(object)value;
		}
		if (typeof(TOther) == typeof(char))
		{
			char c = (char)(object)value;
			if (c <= '翿')
			{
				return (short)c;
			}
			return short.MaxValue;
		}
		if (typeof(TOther) == typeof(decimal))
		{
			decimal num = (decimal)(object)value;
			if (!(num > 32767m))
			{
				if (!(num < -32768m))
				{
					return (short)num;
				}
				return short.MinValue;
			}
			return short.MaxValue;
		}
		if (typeof(TOther) == typeof(double))
		{
			double num2 = (double)(object)value;
			if (!(num2 > 32767.0))
			{
				if (!(num2 < -32768.0))
				{
					return (short)num2;
				}
				return short.MinValue;
			}
			return short.MaxValue;
		}
		if (typeof(TOther) == typeof(short))
		{
			return (short)(object)value;
		}
		if (typeof(TOther) == typeof(int))
		{
			int num3 = (int)(object)value;
			if (num3 <= 32767)
			{
				if (num3 >= -32768)
				{
					return (short)num3;
				}
				return short.MinValue;
			}
			return short.MaxValue;
		}
		if (typeof(TOther) == typeof(long))
		{
			long num4 = (long)(object)value;
			if (num4 <= 32767)
			{
				if (num4 >= -32768)
				{
					return (short)num4;
				}
				return short.MinValue;
			}
			return short.MaxValue;
		}
		if (typeof(TOther) == typeof(IntPtr))
		{
			IntPtr intPtr = (IntPtr)(object)value;
			if ((nint)intPtr <= 32767)
			{
				if ((nint)intPtr >= -32768)
				{
					return (short)(nint)intPtr;
				}
				return short.MinValue;
			}
			return short.MaxValue;
		}
		if (typeof(TOther) == typeof(sbyte))
		{
			return (sbyte)(object)value;
		}
		if (typeof(TOther) == typeof(float))
		{
			float num5 = (float)(object)value;
			if (!(num5 > 32767f))
			{
				if (!(num5 < -32768f))
				{
					return (short)num5;
				}
				return short.MinValue;
			}
			return short.MaxValue;
		}
		if (typeof(TOther) == typeof(ushort))
		{
			ushort num6 = (ushort)(object)value;
			if (num6 <= 32767)
			{
				return (short)num6;
			}
			return short.MaxValue;
		}
		if (typeof(TOther) == typeof(uint))
		{
			uint num7 = (uint)(object)value;
			if ((long)num7 <= 32767L)
			{
				return (short)num7;
			}
			return short.MaxValue;
		}
		if (typeof(TOther) == typeof(ulong))
		{
			ulong num8 = (ulong)(object)value;
			if (num8 <= 32767)
			{
				return (short)num8;
			}
			return short.MaxValue;
		}
		if (typeof(TOther) == typeof(UIntPtr))
		{
			UIntPtr uIntPtr = (UIntPtr)(object)value;
			if ((nuint)uIntPtr <= 32767)
			{
				return (short)(nuint)uIntPtr;
			}
			return short.MaxValue;
		}
		ThrowHelper.ThrowNotSupportedException();
		return 0;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static short INumber<short>.CreateTruncating<TOther>(TOther value)
	{
		if (typeof(TOther) == typeof(byte))
		{
			return (byte)(object)value;
		}
		if (typeof(TOther) == typeof(char))
		{
			return (short)(char)(object)value;
		}
		if (typeof(TOther) == typeof(decimal))
		{
			return (short)(decimal)(object)value;
		}
		if (typeof(TOther) == typeof(double))
		{
			return (short)(double)(object)value;
		}
		if (typeof(TOther) == typeof(short))
		{
			return (short)(object)value;
		}
		if (typeof(TOther) == typeof(int))
		{
			return (short)(int)(object)value;
		}
		if (typeof(TOther) == typeof(long))
		{
			return (short)(long)(object)value;
		}
		if (typeof(TOther) == typeof(IntPtr))
		{
			return (short)(nint)(IntPtr)(object)value;
		}
		if (typeof(TOther) == typeof(sbyte))
		{
			return (sbyte)(object)value;
		}
		if (typeof(TOther) == typeof(float))
		{
			return (short)(float)(object)value;
		}
		if (typeof(TOther) == typeof(ushort))
		{
			return (short)(ushort)(object)value;
		}
		if (typeof(TOther) == typeof(uint))
		{
			return (short)(uint)(object)value;
		}
		if (typeof(TOther) == typeof(ulong))
		{
			return (short)(ulong)(object)value;
		}
		if (typeof(TOther) == typeof(UIntPtr))
		{
			return (short)(nuint)(UIntPtr)(object)value;
		}
		ThrowHelper.ThrowNotSupportedException();
		return 0;
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static (short Quotient, short Remainder) INumber<short>.DivRem(short left, short right)
	{
		return Math.DivRem(left, right);
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static short INumber<short>.Max(short x, short y)
	{
		return Math.Max(x, y);
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static short INumber<short>.Min(short x, short y)
	{
		return Math.Min(x, y);
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static short INumber<short>.Parse(string s, NumberStyles style, IFormatProvider provider)
	{
		return Parse(s, style, provider);
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static short INumber<short>.Parse(ReadOnlySpan<char> s, NumberStyles style, IFormatProvider provider)
	{
		return Parse(s, style, provider);
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static short INumber<short>.Sign(short value)
	{
		return (short)Math.Sign(value);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static bool INumber<short>.TryCreate<TOther>(TOther value, out short result)
	{
		if (typeof(TOther) == typeof(byte))
		{
			result = (byte)(object)value;
			return true;
		}
		if (typeof(TOther) == typeof(char))
		{
			char c = (char)(object)value;
			if (c > '翿')
			{
				result = 0;
				return false;
			}
			result = (short)c;
			return true;
		}
		if (typeof(TOther) == typeof(decimal))
		{
			decimal num = (decimal)(object)value;
			if (num < -32768m || num > 32767m)
			{
				result = 0;
				return false;
			}
			result = (short)num;
			return true;
		}
		if (typeof(TOther) == typeof(double))
		{
			double num2 = (double)(object)value;
			if (num2 < -32768.0 || num2 > 32767.0)
			{
				result = 0;
				return false;
			}
			result = (short)num2;
			return true;
		}
		if (typeof(TOther) == typeof(short))
		{
			result = (short)(object)value;
			return true;
		}
		if (typeof(TOther) == typeof(int))
		{
			int num3 = (int)(object)value;
			if (num3 < -32768 || num3 > 32767)
			{
				result = 0;
				return false;
			}
			result = (short)num3;
			return true;
		}
		if (typeof(TOther) == typeof(long))
		{
			long num4 = (long)(object)value;
			if (num4 < -32768 || num4 > 32767)
			{
				result = 0;
				return false;
			}
			result = (short)num4;
			return true;
		}
		if (typeof(TOther) == typeof(IntPtr))
		{
			IntPtr intPtr = (IntPtr)(object)value;
			if ((nint)intPtr < -32768 || (nint)intPtr > 32767)
			{
				result = 0;
				return false;
			}
			result = (short)(nint)intPtr;
			return true;
		}
		if (typeof(TOther) == typeof(sbyte))
		{
			result = (sbyte)(object)value;
			return true;
		}
		if (typeof(TOther) == typeof(float))
		{
			float num5 = (float)(object)value;
			if (num5 < -32768f || num5 > 32767f)
			{
				result = 0;
				return false;
			}
			result = (short)num5;
			return true;
		}
		if (typeof(TOther) == typeof(ushort))
		{
			ushort num6 = (ushort)(object)value;
			if (num6 > 32767)
			{
				result = 0;
				return false;
			}
			result = (short)num6;
			return true;
		}
		if (typeof(TOther) == typeof(uint))
		{
			uint num7 = (uint)(object)value;
			if ((long)num7 > 32767L)
			{
				result = 0;
				return false;
			}
			result = (short)num7;
			return true;
		}
		if (typeof(TOther) == typeof(ulong))
		{
			ulong num8 = (ulong)(object)value;
			if (num8 > 32767)
			{
				result = 0;
				return false;
			}
			result = (short)num8;
			return true;
		}
		if (typeof(TOther) == typeof(UIntPtr))
		{
			UIntPtr uIntPtr = (UIntPtr)(object)value;
			if ((nuint)uIntPtr > 32767)
			{
				result = 0;
				return false;
			}
			result = (short)(nuint)uIntPtr;
			return true;
		}
		ThrowHelper.ThrowNotSupportedException();
		result = 0;
		return false;
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static bool INumber<short>.TryParse([NotNullWhen(true)] string s, NumberStyles style, IFormatProvider provider, out short result)
	{
		return TryParse(s, style, provider, out result);
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static bool INumber<short>.TryParse(ReadOnlySpan<char> s, NumberStyles style, IFormatProvider provider, out short result)
	{
		return TryParse(s, style, provider, out result);
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static short IParseable<short>.Parse(string s, IFormatProvider provider)
	{
		return Parse(s, provider);
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static bool IParseable<short>.TryParse([NotNullWhen(true)] string s, IFormatProvider provider, out short result)
	{
		return TryParse(s, NumberStyles.Integer, provider, out result);
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static short IShiftOperators<short, short>.operator <<(short value, int shiftAmount)
	{
		return (short)(value << shiftAmount);
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static short IShiftOperators<short, short>.operator >>(short value, int shiftAmount)
	{
		return (short)(value >> shiftAmount);
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static short ISpanParseable<short>.Parse(ReadOnlySpan<char> s, IFormatProvider provider)
	{
		return Parse(s, NumberStyles.Integer, provider);
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static bool ISpanParseable<short>.TryParse(ReadOnlySpan<char> s, IFormatProvider provider, out short result)
	{
		return TryParse(s, NumberStyles.Integer, provider, out result);
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static short ISubtractionOperators<short, short, short>.operator -(short left, short right)
	{
		return (short)(left - right);
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static short IUnaryNegationOperators<short, short>.operator -(short value)
	{
		return (short)(-value);
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static short IUnaryPlusOperators<short, short>.operator +(short value)
	{
		return value;
	}
}
