using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.Versioning;

namespace System;

[Serializable]
[TypeForwardedFrom("mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
public readonly struct Byte : IComparable, IConvertible, ISpanFormattable, IFormattable, IComparable<byte>, IEquatable<byte>, IBinaryInteger<byte>, IBinaryNumber<byte>, IBitwiseOperators<byte, byte, byte>, INumber<byte>, IAdditionOperators<byte, byte, byte>, IAdditiveIdentity<byte, byte>, IComparisonOperators<byte, byte>, IEqualityOperators<byte, byte>, IDecrementOperators<byte>, IDivisionOperators<byte, byte, byte>, IIncrementOperators<byte>, IModulusOperators<byte, byte, byte>, IMultiplicativeIdentity<byte, byte>, IMultiplyOperators<byte, byte, byte>, ISpanParseable<byte>, IParseable<byte>, ISubtractionOperators<byte, byte, byte>, IUnaryNegationOperators<byte, byte>, IUnaryPlusOperators<byte, byte>, IShiftOperators<byte, byte>, IMinMaxValue<byte>, IUnsignedNumber<byte>
{
	private readonly byte m_value;

	public const byte MaxValue = 255;

	public const byte MinValue = 0;

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static byte IAdditiveIdentity<byte, byte>.AdditiveIdentity => 0;

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static byte IMinMaxValue<byte>.MinValue => 0;

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static byte IMinMaxValue<byte>.MaxValue => byte.MaxValue;

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static byte IMultiplicativeIdentity<byte, byte>.MultiplicativeIdentity => 1;

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static byte INumber<byte>.One => 1;

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static byte INumber<byte>.Zero => 0;

	public int CompareTo(object? value)
	{
		if (value == null)
		{
			return 1;
		}
		if (!(value is byte))
		{
			throw new ArgumentException(SR.Arg_MustBeByte);
		}
		return this - (byte)value;
	}

	public int CompareTo(byte value)
	{
		return this - value;
	}

	public override bool Equals([NotNullWhen(true)] object? obj)
	{
		if (!(obj is byte))
		{
			return false;
		}
		return this == (byte)obj;
	}

	[NonVersionable]
	public bool Equals(byte obj)
	{
		return this == obj;
	}

	public override int GetHashCode()
	{
		return this;
	}

	public static byte Parse(string s)
	{
		if (s == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.s);
		}
		return Parse((ReadOnlySpan<char>)s, NumberStyles.Integer, NumberFormatInfo.CurrentInfo);
	}

	public static byte Parse(string s, NumberStyles style)
	{
		NumberFormatInfo.ValidateParseStyleInteger(style);
		if (s == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.s);
		}
		return Parse((ReadOnlySpan<char>)s, style, NumberFormatInfo.CurrentInfo);
	}

	public static byte Parse(string s, IFormatProvider? provider)
	{
		if (s == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.s);
		}
		return Parse((ReadOnlySpan<char>)s, NumberStyles.Integer, NumberFormatInfo.GetInstance(provider));
	}

	public static byte Parse(string s, NumberStyles style, IFormatProvider? provider)
	{
		NumberFormatInfo.ValidateParseStyleInteger(style);
		if (s == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.s);
		}
		return Parse((ReadOnlySpan<char>)s, style, NumberFormatInfo.GetInstance(provider));
	}

	public static byte Parse(ReadOnlySpan<char> s, NumberStyles style = NumberStyles.Integer, IFormatProvider? provider = null)
	{
		NumberFormatInfo.ValidateParseStyleInteger(style);
		return Parse(s, style, NumberFormatInfo.GetInstance(provider));
	}

	private static byte Parse(ReadOnlySpan<char> s, NumberStyles style, NumberFormatInfo info)
	{
		uint result;
		Number.ParsingStatus parsingStatus = Number.TryParseUInt32(s, style, info, out result);
		if (parsingStatus != 0)
		{
			Number.ThrowOverflowOrFormatException(parsingStatus, TypeCode.Byte);
		}
		if (result > 255)
		{
			Number.ThrowOverflowException(TypeCode.Byte);
		}
		return (byte)result;
	}

	public static bool TryParse([NotNullWhen(true)] string? s, out byte result)
	{
		if (s == null)
		{
			result = 0;
			return false;
		}
		return TryParse((ReadOnlySpan<char>)s, NumberStyles.Integer, NumberFormatInfo.CurrentInfo, out result);
	}

	public static bool TryParse(ReadOnlySpan<char> s, out byte result)
	{
		return TryParse(s, NumberStyles.Integer, NumberFormatInfo.CurrentInfo, out result);
	}

	public static bool TryParse([NotNullWhen(true)] string? s, NumberStyles style, IFormatProvider? provider, out byte result)
	{
		NumberFormatInfo.ValidateParseStyleInteger(style);
		if (s == null)
		{
			result = 0;
			return false;
		}
		return TryParse((ReadOnlySpan<char>)s, style, NumberFormatInfo.GetInstance(provider), out result);
	}

	public static bool TryParse(ReadOnlySpan<char> s, NumberStyles style, IFormatProvider? provider, out byte result)
	{
		NumberFormatInfo.ValidateParseStyleInteger(style);
		return TryParse(s, style, NumberFormatInfo.GetInstance(provider), out result);
	}

	private static bool TryParse(ReadOnlySpan<char> s, NumberStyles style, NumberFormatInfo info, out byte result)
	{
		if (Number.TryParseUInt32(s, style, info, out var result2) != 0 || result2 > 255)
		{
			result = 0;
			return false;
		}
		result = (byte)result2;
		return true;
	}

	public override string ToString()
	{
		return Number.UInt32ToDecStr(this);
	}

	public string ToString(string? format)
	{
		return Number.FormatUInt32(this, format, null);
	}

	public string ToString(IFormatProvider? provider)
	{
		return Number.UInt32ToDecStr(this);
	}

	public string ToString(string? format, IFormatProvider? provider)
	{
		return Number.FormatUInt32(this, format, provider);
	}

	public bool TryFormat(Span<char> destination, out int charsWritten, ReadOnlySpan<char> format = default(ReadOnlySpan<char>), IFormatProvider? provider = null)
	{
		return Number.TryFormatUInt32(this, format, provider, destination, out charsWritten);
	}

	public TypeCode GetTypeCode()
	{
		return TypeCode.Byte;
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
		return this;
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
		throw new InvalidCastException(SR.Format(SR.InvalidCast_FromTo, "Byte", "DateTime"));
	}

	object IConvertible.ToType(Type type, IFormatProvider provider)
	{
		return Convert.DefaultToType(this, type, provider);
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static byte IAdditionOperators<byte, byte, byte>.operator +(byte left, byte right)
	{
		return (byte)(left + right);
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static byte IBinaryInteger<byte>.LeadingZeroCount(byte value)
	{
		return (byte)(BitOperations.LeadingZeroCount(value) - 24);
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static byte IBinaryInteger<byte>.PopCount(byte value)
	{
		return (byte)BitOperations.PopCount(value);
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static byte IBinaryInteger<byte>.RotateLeft(byte value, int rotateAmount)
	{
		return (byte)((value << (rotateAmount & 7)) | (value >> ((8 - rotateAmount) & 7)));
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static byte IBinaryInteger<byte>.RotateRight(byte value, int rotateAmount)
	{
		return (byte)((value >> (rotateAmount & 7)) | (value << ((8 - rotateAmount) & 7)));
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static byte IBinaryInteger<byte>.TrailingZeroCount(byte value)
	{
		return (byte)(BitOperations.TrailingZeroCount(value << 24) - 24);
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static bool IBinaryNumber<byte>.IsPow2(byte value)
	{
		return BitOperations.IsPow2((uint)value);
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static byte IBinaryNumber<byte>.Log2(byte value)
	{
		return (byte)BitOperations.Log2(value);
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static byte IBitwiseOperators<byte, byte, byte>.operator &(byte left, byte right)
	{
		return (byte)(left & right);
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static byte IBitwiseOperators<byte, byte, byte>.operator |(byte left, byte right)
	{
		return (byte)(left | right);
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static byte IBitwiseOperators<byte, byte, byte>.operator ^(byte left, byte right)
	{
		return (byte)(left ^ right);
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static byte IBitwiseOperators<byte, byte, byte>.operator ~(byte value)
	{
		return (byte)(~value);
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static bool IComparisonOperators<byte, byte>.operator <(byte left, byte right)
	{
		return left < right;
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static bool IComparisonOperators<byte, byte>.operator <=(byte left, byte right)
	{
		return left <= right;
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static bool IComparisonOperators<byte, byte>.operator >(byte left, byte right)
	{
		return left > right;
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static bool IComparisonOperators<byte, byte>.operator >=(byte left, byte right)
	{
		return left >= right;
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static byte IDecrementOperators<byte>.operator --(byte value)
	{
		return --value;
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static byte IDivisionOperators<byte, byte, byte>.operator /(byte left, byte right)
	{
		return (byte)(left / right);
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static bool IEqualityOperators<byte, byte>.operator ==(byte left, byte right)
	{
		return left == right;
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static bool IEqualityOperators<byte, byte>.operator !=(byte left, byte right)
	{
		return left != right;
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static byte IIncrementOperators<byte>.operator ++(byte value)
	{
		return ++value;
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static byte IModulusOperators<byte, byte, byte>.operator %(byte left, byte right)
	{
		return (byte)(left % right);
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static byte IMultiplyOperators<byte, byte, byte>.operator *(byte left, byte right)
	{
		return (byte)(left * right);
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static byte INumber<byte>.Abs(byte value)
	{
		return value;
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static byte INumber<byte>.Clamp(byte value, byte min, byte max)
	{
		return Math.Clamp(value, min, max);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static byte INumber<byte>.Create<TOther>(TOther value)
	{
		if (typeof(TOther) == typeof(byte))
		{
			return (byte)(object)value;
		}
		checked
		{
			if (typeof(TOther) == typeof(char))
			{
				return (byte)(char)(object)value;
			}
			if (typeof(TOther) == typeof(decimal))
			{
				return (byte)(decimal)(object)value;
			}
			if (typeof(TOther) == typeof(double))
			{
				return (byte)(double)(object)value;
			}
			if (typeof(TOther) == typeof(short))
			{
				return (byte)(short)(object)value;
			}
			if (typeof(TOther) == typeof(int))
			{
				return (byte)(int)(object)value;
			}
			if (typeof(TOther) == typeof(long))
			{
				return (byte)(long)(object)value;
			}
			if (typeof(TOther) == typeof(IntPtr))
			{
				return (byte)(nint)(IntPtr)(object)value;
			}
			if (typeof(TOther) == typeof(sbyte))
			{
				return (byte)(sbyte)(object)value;
			}
			if (typeof(TOther) == typeof(float))
			{
				return (byte)(float)(object)value;
			}
			if (typeof(TOther) == typeof(ushort))
			{
				return (byte)(ushort)(object)value;
			}
			if (typeof(TOther) == typeof(uint))
			{
				return (byte)(uint)(object)value;
			}
			if (typeof(TOther) == typeof(ulong))
			{
				return (byte)(ulong)(object)value;
			}
			if (typeof(TOther) == typeof(UIntPtr))
			{
				return (byte)(nuint)(UIntPtr)(object)value;
			}
			ThrowHelper.ThrowNotSupportedException();
			return 0;
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static byte INumber<byte>.CreateSaturating<TOther>(TOther value)
	{
		if (typeof(TOther) == typeof(byte))
		{
			return (byte)(object)value;
		}
		if (typeof(TOther) == typeof(char))
		{
			char c = (char)(object)value;
			if (c <= 'ÿ')
			{
				return (byte)c;
			}
			return byte.MaxValue;
		}
		if (typeof(TOther) == typeof(decimal))
		{
			decimal num = (decimal)(object)value;
			if (!(num > 255m))
			{
				if (!(num < 0m))
				{
					return (byte)num;
				}
				return 0;
			}
			return byte.MaxValue;
		}
		if (typeof(TOther) == typeof(double))
		{
			double num2 = (double)(object)value;
			if (!(num2 > 255.0))
			{
				if (!(num2 < 0.0))
				{
					return (byte)num2;
				}
				return 0;
			}
			return byte.MaxValue;
		}
		if (typeof(TOther) == typeof(short))
		{
			short num3 = (short)(object)value;
			if (num3 <= 255)
			{
				if (num3 >= 0)
				{
					return (byte)num3;
				}
				return 0;
			}
			return byte.MaxValue;
		}
		if (typeof(TOther) == typeof(int))
		{
			int num4 = (int)(object)value;
			if (num4 <= 255)
			{
				if (num4 >= 0)
				{
					return (byte)num4;
				}
				return 0;
			}
			return byte.MaxValue;
		}
		if (typeof(TOther) == typeof(long))
		{
			long num5 = (long)(object)value;
			if (num5 <= 255)
			{
				if (num5 >= 0)
				{
					return (byte)num5;
				}
				return 0;
			}
			return byte.MaxValue;
		}
		if (typeof(TOther) == typeof(IntPtr))
		{
			IntPtr intPtr = (IntPtr)(object)value;
			if ((nint)intPtr <= 255)
			{
				if ((nint)intPtr >= 0)
				{
					return (byte)(nint)intPtr;
				}
				return 0;
			}
			return byte.MaxValue;
		}
		if (typeof(TOther) == typeof(sbyte))
		{
			sbyte b = (sbyte)(object)value;
			if (b >= 0)
			{
				return (byte)b;
			}
			return 0;
		}
		if (typeof(TOther) == typeof(float))
		{
			float num6 = (float)(object)value;
			if (!(num6 > 255f))
			{
				if (!(num6 < 0f))
				{
					return (byte)num6;
				}
				return 0;
			}
			return byte.MaxValue;
		}
		if (typeof(TOther) == typeof(ushort))
		{
			ushort num7 = (ushort)(object)value;
			if (num7 <= 255)
			{
				return (byte)num7;
			}
			return byte.MaxValue;
		}
		if (typeof(TOther) == typeof(uint))
		{
			uint num8 = (uint)(object)value;
			if (num8 <= 255)
			{
				return (byte)num8;
			}
			return byte.MaxValue;
		}
		if (typeof(TOther) == typeof(ulong))
		{
			ulong num9 = (ulong)(object)value;
			if (num9 <= 255)
			{
				return (byte)num9;
			}
			return byte.MaxValue;
		}
		if (typeof(TOther) == typeof(UIntPtr))
		{
			UIntPtr uIntPtr = (UIntPtr)(object)value;
			if ((nuint)uIntPtr <= 255)
			{
				return (byte)(nuint)uIntPtr;
			}
			return byte.MaxValue;
		}
		ThrowHelper.ThrowNotSupportedException();
		return 0;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static byte INumber<byte>.CreateTruncating<TOther>(TOther value)
	{
		if (typeof(TOther) == typeof(byte))
		{
			return (byte)(object)value;
		}
		if (typeof(TOther) == typeof(char))
		{
			return (byte)(char)(object)value;
		}
		if (typeof(TOther) == typeof(decimal))
		{
			return (byte)(decimal)(object)value;
		}
		if (typeof(TOther) == typeof(double))
		{
			return (byte)(double)(object)value;
		}
		if (typeof(TOther) == typeof(short))
		{
			return (byte)(short)(object)value;
		}
		if (typeof(TOther) == typeof(int))
		{
			return (byte)(int)(object)value;
		}
		if (typeof(TOther) == typeof(long))
		{
			return (byte)(long)(object)value;
		}
		if (typeof(TOther) == typeof(IntPtr))
		{
			return (byte)(nint)(IntPtr)(object)value;
		}
		if (typeof(TOther) == typeof(sbyte))
		{
			return (byte)(sbyte)(object)value;
		}
		if (typeof(TOther) == typeof(float))
		{
			return (byte)(float)(object)value;
		}
		if (typeof(TOther) == typeof(ushort))
		{
			return (byte)(ushort)(object)value;
		}
		if (typeof(TOther) == typeof(uint))
		{
			return (byte)(uint)(object)value;
		}
		if (typeof(TOther) == typeof(ulong))
		{
			return (byte)(ulong)(object)value;
		}
		if (typeof(TOther) == typeof(UIntPtr))
		{
			return (byte)(nuint)(UIntPtr)(object)value;
		}
		ThrowHelper.ThrowNotSupportedException();
		return 0;
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static (byte Quotient, byte Remainder) INumber<byte>.DivRem(byte left, byte right)
	{
		return Math.DivRem(left, right);
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static byte INumber<byte>.Max(byte x, byte y)
	{
		return Math.Max(x, y);
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static byte INumber<byte>.Min(byte x, byte y)
	{
		return Math.Min(x, y);
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static byte INumber<byte>.Parse(string s, NumberStyles style, IFormatProvider provider)
	{
		return Parse(s, style, provider);
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static byte INumber<byte>.Parse(ReadOnlySpan<char> s, NumberStyles style, IFormatProvider provider)
	{
		return Parse(s, style, provider);
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static byte INumber<byte>.Sign(byte value)
	{
		return (byte)((value != 0) ? 1u : 0u);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static bool INumber<byte>.TryCreate<TOther>(TOther value, out byte result)
	{
		if (typeof(TOther) == typeof(byte))
		{
			result = (byte)(object)value;
			return true;
		}
		if (typeof(TOther) == typeof(char))
		{
			char c = (char)(object)value;
			if (c > 'ÿ')
			{
				result = 0;
				return false;
			}
			result = (byte)c;
			return true;
		}
		if (typeof(TOther) == typeof(decimal))
		{
			decimal num = (decimal)(object)value;
			if (num < 0m || num > 255m)
			{
				result = 0;
				return false;
			}
			result = (byte)num;
			return true;
		}
		if (typeof(TOther) == typeof(double))
		{
			double num2 = (double)(object)value;
			if (num2 < 0.0 || num2 > 255.0)
			{
				result = 0;
				return false;
			}
			result = (byte)num2;
			return true;
		}
		if (typeof(TOther) == typeof(short))
		{
			short num3 = (short)(object)value;
			if (num3 < 0 || num3 > 255)
			{
				result = 0;
				return false;
			}
			result = (byte)num3;
			return true;
		}
		if (typeof(TOther) == typeof(int))
		{
			int num4 = (int)(object)value;
			if (num4 < 0 || num4 > 255)
			{
				result = 0;
				return false;
			}
			result = (byte)num4;
			return true;
		}
		if (typeof(TOther) == typeof(long))
		{
			long num5 = (long)(object)value;
			if (num5 < 0 || num5 > 255)
			{
				result = 0;
				return false;
			}
			result = (byte)num5;
			return true;
		}
		if (typeof(TOther) == typeof(IntPtr))
		{
			IntPtr intPtr = (IntPtr)(object)value;
			if ((nint)intPtr < 0 || (nint)intPtr > 255)
			{
				result = 0;
				return false;
			}
			result = (byte)(nint)intPtr;
			return true;
		}
		if (typeof(TOther) == typeof(sbyte))
		{
			sbyte b = (sbyte)(object)value;
			if (b < 0)
			{
				result = 0;
				return false;
			}
			result = (byte)b;
			return true;
		}
		if (typeof(TOther) == typeof(float))
		{
			float num6 = (float)(object)value;
			if (num6 < 0f || num6 > 255f)
			{
				result = 0;
				return false;
			}
			result = (byte)num6;
			return true;
		}
		if (typeof(TOther) == typeof(ushort))
		{
			ushort num7 = (ushort)(object)value;
			if (num7 > 255)
			{
				result = 0;
				return false;
			}
			result = (byte)num7;
			return true;
		}
		if (typeof(TOther) == typeof(uint))
		{
			uint num8 = (uint)(object)value;
			if (num8 > 255)
			{
				result = 0;
				return false;
			}
			result = (byte)num8;
			return true;
		}
		if (typeof(TOther) == typeof(ulong))
		{
			ulong num9 = (ulong)(object)value;
			if (num9 > 255)
			{
				result = 0;
				return false;
			}
			result = (byte)num9;
			return true;
		}
		if (typeof(TOther) == typeof(UIntPtr))
		{
			UIntPtr uIntPtr = (UIntPtr)(object)value;
			if ((nuint)uIntPtr > 255)
			{
				result = 0;
				return false;
			}
			result = (byte)(nuint)uIntPtr;
			return true;
		}
		ThrowHelper.ThrowNotSupportedException();
		result = 0;
		return false;
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static bool INumber<byte>.TryParse([NotNullWhen(true)] string s, NumberStyles style, IFormatProvider provider, out byte result)
	{
		return TryParse(s, style, provider, out result);
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static bool INumber<byte>.TryParse(ReadOnlySpan<char> s, NumberStyles style, IFormatProvider provider, out byte result)
	{
		return TryParse(s, style, provider, out result);
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static byte IParseable<byte>.Parse(string s, IFormatProvider provider)
	{
		return Parse(s, provider);
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static bool IParseable<byte>.TryParse([NotNullWhen(true)] string s, IFormatProvider provider, out byte result)
	{
		return TryParse(s, NumberStyles.Integer, provider, out result);
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static byte IShiftOperators<byte, byte>.operator <<(byte value, int shiftAmount)
	{
		return (byte)(value << shiftAmount);
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static byte IShiftOperators<byte, byte>.operator >>(byte value, int shiftAmount)
	{
		return (byte)(value >> shiftAmount);
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static byte ISpanParseable<byte>.Parse(ReadOnlySpan<char> s, IFormatProvider provider)
	{
		return Parse(s, NumberStyles.Integer, provider);
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static bool ISpanParseable<byte>.TryParse(ReadOnlySpan<char> s, IFormatProvider provider, out byte result)
	{
		return TryParse(s, NumberStyles.Integer, provider, out result);
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static byte ISubtractionOperators<byte, byte, byte>.operator -(byte left, byte right)
	{
		return (byte)(left - right);
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static byte IUnaryNegationOperators<byte, byte>.operator -(byte value)
	{
		return (byte)(-value);
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static byte IUnaryPlusOperators<byte, byte>.operator +(byte value)
	{
		return value;
	}
}
