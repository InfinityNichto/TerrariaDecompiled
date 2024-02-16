using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.Versioning;

namespace System;

[Serializable]
[CLSCompliant(false)]
[TypeForwardedFrom("mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
public readonly struct UInt16 : IComparable, IConvertible, ISpanFormattable, IFormattable, IComparable<ushort>, IEquatable<ushort>, IBinaryInteger<ushort>, IBinaryNumber<ushort>, IBitwiseOperators<ushort, ushort, ushort>, INumber<ushort>, IAdditionOperators<ushort, ushort, ushort>, IAdditiveIdentity<ushort, ushort>, IComparisonOperators<ushort, ushort>, IEqualityOperators<ushort, ushort>, IDecrementOperators<ushort>, IDivisionOperators<ushort, ushort, ushort>, IIncrementOperators<ushort>, IModulusOperators<ushort, ushort, ushort>, IMultiplicativeIdentity<ushort, ushort>, IMultiplyOperators<ushort, ushort, ushort>, ISpanParseable<ushort>, IParseable<ushort>, ISubtractionOperators<ushort, ushort, ushort>, IUnaryNegationOperators<ushort, ushort>, IUnaryPlusOperators<ushort, ushort>, IShiftOperators<ushort, ushort>, IMinMaxValue<ushort>, IUnsignedNumber<ushort>
{
	private readonly ushort m_value;

	public const ushort MaxValue = 65535;

	public const ushort MinValue = 0;

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static ushort IAdditiveIdentity<ushort, ushort>.AdditiveIdentity => 0;

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static ushort IMinMaxValue<ushort>.MinValue => 0;

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static ushort IMinMaxValue<ushort>.MaxValue => ushort.MaxValue;

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static ushort IMultiplicativeIdentity<ushort, ushort>.MultiplicativeIdentity => 1;

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static ushort INumber<ushort>.One => 1;

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static ushort INumber<ushort>.Zero => 0;

	public int CompareTo(object? value)
	{
		if (value == null)
		{
			return 1;
		}
		if (value is ushort)
		{
			return this - (ushort)value;
		}
		throw new ArgumentException(SR.Arg_MustBeUInt16);
	}

	public int CompareTo(ushort value)
	{
		return this - value;
	}

	public override bool Equals([NotNullWhen(true)] object? obj)
	{
		if (!(obj is ushort))
		{
			return false;
		}
		return this == (ushort)obj;
	}

	[NonVersionable]
	public bool Equals(ushort obj)
	{
		return this == obj;
	}

	public override int GetHashCode()
	{
		return this;
	}

	public override string ToString()
	{
		return Number.UInt32ToDecStr(this);
	}

	public string ToString(IFormatProvider? provider)
	{
		return Number.UInt32ToDecStr(this);
	}

	public string ToString(string? format)
	{
		return Number.FormatUInt32(this, format, null);
	}

	public string ToString(string? format, IFormatProvider? provider)
	{
		return Number.FormatUInt32(this, format, provider);
	}

	public bool TryFormat(Span<char> destination, out int charsWritten, ReadOnlySpan<char> format = default(ReadOnlySpan<char>), IFormatProvider? provider = null)
	{
		return Number.TryFormatUInt32(this, format, provider, destination, out charsWritten);
	}

	public static ushort Parse(string s)
	{
		if (s == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.s);
		}
		return Parse((ReadOnlySpan<char>)s, NumberStyles.Integer, NumberFormatInfo.CurrentInfo);
	}

	public static ushort Parse(string s, NumberStyles style)
	{
		NumberFormatInfo.ValidateParseStyleInteger(style);
		if (s == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.s);
		}
		return Parse((ReadOnlySpan<char>)s, style, NumberFormatInfo.CurrentInfo);
	}

	public static ushort Parse(string s, IFormatProvider? provider)
	{
		if (s == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.s);
		}
		return Parse((ReadOnlySpan<char>)s, NumberStyles.Integer, NumberFormatInfo.GetInstance(provider));
	}

	public static ushort Parse(string s, NumberStyles style, IFormatProvider? provider)
	{
		NumberFormatInfo.ValidateParseStyleInteger(style);
		if (s == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.s);
		}
		return Parse((ReadOnlySpan<char>)s, style, NumberFormatInfo.GetInstance(provider));
	}

	public static ushort Parse(ReadOnlySpan<char> s, NumberStyles style = NumberStyles.Integer, IFormatProvider? provider = null)
	{
		NumberFormatInfo.ValidateParseStyleInteger(style);
		return Parse(s, style, NumberFormatInfo.GetInstance(provider));
	}

	private static ushort Parse(ReadOnlySpan<char> s, NumberStyles style, NumberFormatInfo info)
	{
		uint result;
		Number.ParsingStatus parsingStatus = Number.TryParseUInt32(s, style, info, out result);
		if (parsingStatus != 0)
		{
			Number.ThrowOverflowOrFormatException(parsingStatus, TypeCode.UInt16);
		}
		if (result > 65535)
		{
			Number.ThrowOverflowException(TypeCode.UInt16);
		}
		return (ushort)result;
	}

	public static bool TryParse([NotNullWhen(true)] string? s, out ushort result)
	{
		if (s == null)
		{
			result = 0;
			return false;
		}
		return TryParse((ReadOnlySpan<char>)s, NumberStyles.Integer, NumberFormatInfo.CurrentInfo, out result);
	}

	public static bool TryParse(ReadOnlySpan<char> s, out ushort result)
	{
		return TryParse(s, NumberStyles.Integer, NumberFormatInfo.CurrentInfo, out result);
	}

	public static bool TryParse([NotNullWhen(true)] string? s, NumberStyles style, IFormatProvider? provider, out ushort result)
	{
		NumberFormatInfo.ValidateParseStyleInteger(style);
		if (s == null)
		{
			result = 0;
			return false;
		}
		return TryParse((ReadOnlySpan<char>)s, style, NumberFormatInfo.GetInstance(provider), out result);
	}

	public static bool TryParse(ReadOnlySpan<char> s, NumberStyles style, IFormatProvider? provider, out ushort result)
	{
		NumberFormatInfo.ValidateParseStyleInteger(style);
		return TryParse(s, style, NumberFormatInfo.GetInstance(provider), out result);
	}

	private static bool TryParse(ReadOnlySpan<char> s, NumberStyles style, NumberFormatInfo info, out ushort result)
	{
		if (Number.TryParseUInt32(s, style, info, out var result2) != 0 || result2 > 65535)
		{
			result = 0;
			return false;
		}
		result = (ushort)result2;
		return true;
	}

	public TypeCode GetTypeCode()
	{
		return TypeCode.UInt16;
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
		return this;
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
		throw new InvalidCastException(SR.Format(SR.InvalidCast_FromTo, "UInt16", "DateTime"));
	}

	object IConvertible.ToType(Type type, IFormatProvider provider)
	{
		return Convert.DefaultToType(this, type, provider);
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static ushort IAdditionOperators<ushort, ushort, ushort>.operator +(ushort left, ushort right)
	{
		return (ushort)(left + right);
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static ushort IBinaryInteger<ushort>.LeadingZeroCount(ushort value)
	{
		return (ushort)(BitOperations.LeadingZeroCount(value) - 16);
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static ushort IBinaryInteger<ushort>.PopCount(ushort value)
	{
		return (ushort)BitOperations.PopCount(value);
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static ushort IBinaryInteger<ushort>.RotateLeft(ushort value, int rotateAmount)
	{
		return (ushort)((value << (rotateAmount & 0xF)) | (value >> ((16 - rotateAmount) & 0xF)));
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static ushort IBinaryInteger<ushort>.RotateRight(ushort value, int rotateAmount)
	{
		return (ushort)((value >> (rotateAmount & 0xF)) | (value << ((16 - rotateAmount) & 0xF)));
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static ushort IBinaryInteger<ushort>.TrailingZeroCount(ushort value)
	{
		return (ushort)(BitOperations.TrailingZeroCount(value << 16) - 16);
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static bool IBinaryNumber<ushort>.IsPow2(ushort value)
	{
		return BitOperations.IsPow2((uint)value);
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static ushort IBinaryNumber<ushort>.Log2(ushort value)
	{
		return (ushort)BitOperations.Log2(value);
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static ushort IBitwiseOperators<ushort, ushort, ushort>.operator &(ushort left, ushort right)
	{
		return (ushort)(left & right);
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static ushort IBitwiseOperators<ushort, ushort, ushort>.operator |(ushort left, ushort right)
	{
		return (ushort)(left | right);
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static ushort IBitwiseOperators<ushort, ushort, ushort>.operator ^(ushort left, ushort right)
	{
		return (ushort)(left ^ right);
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static ushort IBitwiseOperators<ushort, ushort, ushort>.operator ~(ushort value)
	{
		return (ushort)(~value);
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static bool IComparisonOperators<ushort, ushort>.operator <(ushort left, ushort right)
	{
		return left < right;
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static bool IComparisonOperators<ushort, ushort>.operator <=(ushort left, ushort right)
	{
		return left <= right;
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static bool IComparisonOperators<ushort, ushort>.operator >(ushort left, ushort right)
	{
		return left > right;
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static bool IComparisonOperators<ushort, ushort>.operator >=(ushort left, ushort right)
	{
		return left >= right;
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static ushort IDecrementOperators<ushort>.operator --(ushort value)
	{
		return --value;
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static ushort IDivisionOperators<ushort, ushort, ushort>.operator /(ushort left, ushort right)
	{
		return (ushort)(left / right);
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static bool IEqualityOperators<ushort, ushort>.operator ==(ushort left, ushort right)
	{
		return left == right;
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static bool IEqualityOperators<ushort, ushort>.operator !=(ushort left, ushort right)
	{
		return left != right;
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static ushort IIncrementOperators<ushort>.operator ++(ushort value)
	{
		return ++value;
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static ushort IModulusOperators<ushort, ushort, ushort>.operator %(ushort left, ushort right)
	{
		return (ushort)(left % right);
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static ushort IMultiplyOperators<ushort, ushort, ushort>.operator *(ushort left, ushort right)
	{
		return (ushort)(left * right);
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static ushort INumber<ushort>.Abs(ushort value)
	{
		return value;
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static ushort INumber<ushort>.Clamp(ushort value, ushort min, ushort max)
	{
		return Math.Clamp(value, min, max);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static ushort INumber<ushort>.Create<TOther>(TOther value)
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
			return (ushort)(decimal)(object)value;
		}
		checked
		{
			if (typeof(TOther) == typeof(double))
			{
				return (ushort)(double)(object)value;
			}
			if (typeof(TOther) == typeof(short))
			{
				return (ushort)(short)(object)value;
			}
			if (typeof(TOther) == typeof(int))
			{
				return (ushort)(int)(object)value;
			}
			if (typeof(TOther) == typeof(long))
			{
				return (ushort)(long)(object)value;
			}
			if (typeof(TOther) == typeof(IntPtr))
			{
				return (ushort)(nint)(IntPtr)(object)value;
			}
			if (typeof(TOther) == typeof(sbyte))
			{
				return (ushort)(sbyte)(object)value;
			}
			if (typeof(TOther) == typeof(float))
			{
				return (ushort)(float)(object)value;
			}
			if (typeof(TOther) == typeof(ushort))
			{
				return (ushort)(object)value;
			}
			if (typeof(TOther) == typeof(uint))
			{
				return (ushort)(uint)(object)value;
			}
			if (typeof(TOther) == typeof(ulong))
			{
				return (ushort)(ulong)(object)value;
			}
			if (typeof(TOther) == typeof(UIntPtr))
			{
				return (ushort)(nuint)(UIntPtr)(object)value;
			}
			ThrowHelper.ThrowNotSupportedException();
			return 0;
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static ushort INumber<ushort>.CreateSaturating<TOther>(TOther value)
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
			if (!(num > 65535m))
			{
				if (!(num < 0m))
				{
					return (ushort)num;
				}
				return 0;
			}
			return ushort.MaxValue;
		}
		if (typeof(TOther) == typeof(double))
		{
			double num2 = (double)(object)value;
			if (!(num2 > 65535.0))
			{
				if (!(num2 < 0.0))
				{
					return (ushort)num2;
				}
				return 0;
			}
			return ushort.MaxValue;
		}
		if (typeof(TOther) == typeof(short))
		{
			short num3 = (short)(object)value;
			if (num3 >= 0)
			{
				return (ushort)num3;
			}
			return 0;
		}
		if (typeof(TOther) == typeof(int))
		{
			int num4 = (int)(object)value;
			if (num4 <= 65535)
			{
				if (num4 >= 0)
				{
					return (ushort)num4;
				}
				return 0;
			}
			return ushort.MaxValue;
		}
		if (typeof(TOther) == typeof(long))
		{
			long num5 = (long)(object)value;
			if (num5 <= 65535)
			{
				if (num5 >= 0)
				{
					return (ushort)num5;
				}
				return 0;
			}
			return ushort.MaxValue;
		}
		if (typeof(TOther) == typeof(IntPtr))
		{
			IntPtr intPtr = (IntPtr)(object)value;
			if ((nint)intPtr <= 65535)
			{
				if ((nint)intPtr >= 0)
				{
					return (ushort)(nint)intPtr;
				}
				return 0;
			}
			return ushort.MaxValue;
		}
		if (typeof(TOther) == typeof(sbyte))
		{
			sbyte b = (sbyte)(object)value;
			if (b >= 0)
			{
				return (ushort)b;
			}
			return 0;
		}
		if (typeof(TOther) == typeof(float))
		{
			float num6 = (float)(object)value;
			if (!(num6 > 65535f))
			{
				if (!(num6 < 0f))
				{
					return (ushort)num6;
				}
				return 0;
			}
			return ushort.MaxValue;
		}
		if (typeof(TOther) == typeof(ushort))
		{
			return (ushort)(object)value;
		}
		if (typeof(TOther) == typeof(uint))
		{
			uint num7 = (uint)(object)value;
			if (num7 <= 65535)
			{
				return (ushort)num7;
			}
			return ushort.MaxValue;
		}
		if (typeof(TOther) == typeof(ulong))
		{
			ulong num8 = (ulong)(object)value;
			if (num8 <= 65535)
			{
				return (ushort)num8;
			}
			return ushort.MaxValue;
		}
		if (typeof(TOther) == typeof(UIntPtr))
		{
			UIntPtr uIntPtr = (UIntPtr)(object)value;
			if ((nuint)uIntPtr <= 65535)
			{
				return (ushort)(nuint)uIntPtr;
			}
			return ushort.MaxValue;
		}
		ThrowHelper.ThrowNotSupportedException();
		return 0;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static ushort INumber<ushort>.CreateTruncating<TOther>(TOther value)
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
			return (ushort)(decimal)(object)value;
		}
		if (typeof(TOther) == typeof(double))
		{
			return (ushort)(double)(object)value;
		}
		if (typeof(TOther) == typeof(short))
		{
			return (ushort)(short)(object)value;
		}
		if (typeof(TOther) == typeof(int))
		{
			return (ushort)(int)(object)value;
		}
		if (typeof(TOther) == typeof(long))
		{
			return (ushort)(long)(object)value;
		}
		if (typeof(TOther) == typeof(IntPtr))
		{
			return (ushort)(nint)(IntPtr)(object)value;
		}
		if (typeof(TOther) == typeof(sbyte))
		{
			return (ushort)(sbyte)(object)value;
		}
		if (typeof(TOther) == typeof(float))
		{
			return (ushort)(float)(object)value;
		}
		if (typeof(TOther) == typeof(ushort))
		{
			return (ushort)(object)value;
		}
		if (typeof(TOther) == typeof(uint))
		{
			return (ushort)(uint)(object)value;
		}
		if (typeof(TOther) == typeof(ulong))
		{
			return (ushort)(ulong)(object)value;
		}
		if (typeof(TOther) == typeof(UIntPtr))
		{
			return (ushort)(nuint)(UIntPtr)(object)value;
		}
		ThrowHelper.ThrowNotSupportedException();
		return 0;
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static (ushort Quotient, ushort Remainder) INumber<ushort>.DivRem(ushort left, ushort right)
	{
		return Math.DivRem(left, right);
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static ushort INumber<ushort>.Max(ushort x, ushort y)
	{
		return Math.Max(x, y);
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static ushort INumber<ushort>.Min(ushort x, ushort y)
	{
		return Math.Min(x, y);
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static ushort INumber<ushort>.Parse(string s, NumberStyles style, IFormatProvider provider)
	{
		return Parse(s, style, provider);
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static ushort INumber<ushort>.Parse(ReadOnlySpan<char> s, NumberStyles style, IFormatProvider provider)
	{
		return Parse(s, style, provider);
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static ushort INumber<ushort>.Sign(ushort value)
	{
		return (ushort)((value != 0) ? 1u : 0u);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static bool INumber<ushort>.TryCreate<TOther>(TOther value, out ushort result)
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
			if (num < 0m || num > 65535m)
			{
				result = 0;
				return false;
			}
			result = (ushort)num;
			return true;
		}
		if (typeof(TOther) == typeof(double))
		{
			double num2 = (double)(object)value;
			if (num2 < 0.0 || num2 > 65535.0)
			{
				result = 0;
				return false;
			}
			result = (ushort)num2;
			return true;
		}
		if (typeof(TOther) == typeof(short))
		{
			short num3 = (short)(object)value;
			if (num3 < 0)
			{
				result = 0;
				return false;
			}
			result = (ushort)num3;
			return true;
		}
		if (typeof(TOther) == typeof(int))
		{
			int num4 = (int)(object)value;
			if (num4 < 0 || num4 > 65535)
			{
				result = 0;
				return false;
			}
			result = (ushort)num4;
			return true;
		}
		if (typeof(TOther) == typeof(long))
		{
			long num5 = (long)(object)value;
			if (num5 < 0 || num5 > 65535)
			{
				result = 0;
				return false;
			}
			result = (ushort)num5;
			return true;
		}
		if (typeof(TOther) == typeof(IntPtr))
		{
			IntPtr intPtr = (IntPtr)(object)value;
			if ((nint)intPtr < 0 || (nint)intPtr > 65535)
			{
				result = 0;
				return false;
			}
			result = (ushort)(nint)intPtr;
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
			result = (ushort)b;
			return true;
		}
		if (typeof(TOther) == typeof(float))
		{
			float num6 = (float)(object)value;
			if (num6 < 0f || num6 > 65535f)
			{
				result = 0;
				return false;
			}
			result = (ushort)num6;
			return true;
		}
		if (typeof(TOther) == typeof(ushort))
		{
			result = (ushort)(object)value;
			return true;
		}
		if (typeof(TOther) == typeof(uint))
		{
			uint num7 = (uint)(object)value;
			if (num7 > 65535)
			{
				result = 0;
				return false;
			}
			result = (ushort)num7;
			return true;
		}
		if (typeof(TOther) == typeof(ulong))
		{
			ulong num8 = (ulong)(object)value;
			if (num8 > 65535)
			{
				result = 0;
				return false;
			}
			result = (ushort)num8;
			return true;
		}
		if (typeof(TOther) == typeof(UIntPtr))
		{
			UIntPtr uIntPtr = (UIntPtr)(object)value;
			if ((nuint)uIntPtr > 65535)
			{
				result = 0;
				return false;
			}
			result = (ushort)(nuint)uIntPtr;
			return true;
		}
		ThrowHelper.ThrowNotSupportedException();
		result = 0;
		return false;
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static bool INumber<ushort>.TryParse([NotNullWhen(true)] string s, NumberStyles style, IFormatProvider provider, out ushort result)
	{
		return TryParse(s, style, provider, out result);
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static bool INumber<ushort>.TryParse(ReadOnlySpan<char> s, NumberStyles style, IFormatProvider provider, out ushort result)
	{
		return TryParse(s, style, provider, out result);
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static ushort IParseable<ushort>.Parse(string s, IFormatProvider provider)
	{
		return Parse(s, provider);
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static bool IParseable<ushort>.TryParse([NotNullWhen(true)] string s, IFormatProvider provider, out ushort result)
	{
		return TryParse(s, NumberStyles.Integer, provider, out result);
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static ushort IShiftOperators<ushort, ushort>.operator <<(ushort value, int shiftAmount)
	{
		return (ushort)(value << shiftAmount);
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static ushort IShiftOperators<ushort, ushort>.operator >>(ushort value, int shiftAmount)
	{
		return (ushort)(value >> shiftAmount);
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static ushort ISpanParseable<ushort>.Parse(ReadOnlySpan<char> s, IFormatProvider provider)
	{
		return Parse(s, NumberStyles.Integer, provider);
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static bool ISpanParseable<ushort>.TryParse(ReadOnlySpan<char> s, IFormatProvider provider, out ushort result)
	{
		return TryParse(s, NumberStyles.Integer, provider, out result);
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static ushort ISubtractionOperators<ushort, ushort, ushort>.operator -(ushort left, ushort right)
	{
		return (ushort)(left - right);
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static ushort IUnaryNegationOperators<ushort, ushort>.operator -(ushort value)
	{
		return (ushort)(-value);
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static ushort IUnaryPlusOperators<ushort, ushort>.operator +(ushort value)
	{
		return value;
	}
}
