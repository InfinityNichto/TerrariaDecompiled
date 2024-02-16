using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.Versioning;

namespace System;

[Serializable]
[CLSCompliant(false)]
[TypeForwardedFrom("mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
public readonly struct UInt32 : IComparable, IConvertible, ISpanFormattable, IFormattable, IComparable<uint>, IEquatable<uint>, IBinaryInteger<uint>, IBinaryNumber<uint>, IBitwiseOperators<uint, uint, uint>, INumber<uint>, IAdditionOperators<uint, uint, uint>, IAdditiveIdentity<uint, uint>, IComparisonOperators<uint, uint>, IEqualityOperators<uint, uint>, IDecrementOperators<uint>, IDivisionOperators<uint, uint, uint>, IIncrementOperators<uint>, IModulusOperators<uint, uint, uint>, IMultiplicativeIdentity<uint, uint>, IMultiplyOperators<uint, uint, uint>, ISpanParseable<uint>, IParseable<uint>, ISubtractionOperators<uint, uint, uint>, IUnaryNegationOperators<uint, uint>, IUnaryPlusOperators<uint, uint>, IShiftOperators<uint, uint>, IMinMaxValue<uint>, IUnsignedNumber<uint>
{
	private readonly uint m_value;

	public const uint MaxValue = 4294967295u;

	public const uint MinValue = 0u;

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static uint IAdditiveIdentity<uint, uint>.AdditiveIdentity => 0u;

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static uint IMinMaxValue<uint>.MinValue => 0u;

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static uint IMinMaxValue<uint>.MaxValue => uint.MaxValue;

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static uint IMultiplicativeIdentity<uint, uint>.MultiplicativeIdentity => 1u;

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static uint INumber<uint>.One => 1u;

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static uint INumber<uint>.Zero => 0u;

	public int CompareTo(object? value)
	{
		if (value == null)
		{
			return 1;
		}
		if (value is uint num)
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
		throw new ArgumentException(SR.Arg_MustBeUInt32);
	}

	public int CompareTo(uint value)
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
		if (!(obj is uint))
		{
			return false;
		}
		return this == (uint)obj;
	}

	[NonVersionable]
	public bool Equals(uint obj)
	{
		return this == obj;
	}

	public override int GetHashCode()
	{
		return (int)this;
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

	public static uint Parse(string s)
	{
		if (s == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.s);
		}
		return Number.ParseUInt32(s, NumberStyles.Integer, NumberFormatInfo.CurrentInfo);
	}

	public static uint Parse(string s, NumberStyles style)
	{
		NumberFormatInfo.ValidateParseStyleInteger(style);
		if (s == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.s);
		}
		return Number.ParseUInt32(s, style, NumberFormatInfo.CurrentInfo);
	}

	public static uint Parse(string s, IFormatProvider? provider)
	{
		if (s == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.s);
		}
		return Number.ParseUInt32(s, NumberStyles.Integer, NumberFormatInfo.GetInstance(provider));
	}

	public static uint Parse(string s, NumberStyles style, IFormatProvider? provider)
	{
		NumberFormatInfo.ValidateParseStyleInteger(style);
		if (s == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.s);
		}
		return Number.ParseUInt32(s, style, NumberFormatInfo.GetInstance(provider));
	}

	public static uint Parse(ReadOnlySpan<char> s, NumberStyles style = NumberStyles.Integer, IFormatProvider? provider = null)
	{
		NumberFormatInfo.ValidateParseStyleInteger(style);
		return Number.ParseUInt32(s, style, NumberFormatInfo.GetInstance(provider));
	}

	public static bool TryParse([NotNullWhen(true)] string? s, out uint result)
	{
		if (s == null)
		{
			result = 0u;
			return false;
		}
		return Number.TryParseUInt32IntegerStyle(s, NumberStyles.Integer, NumberFormatInfo.CurrentInfo, out result) == Number.ParsingStatus.OK;
	}

	public static bool TryParse(ReadOnlySpan<char> s, out uint result)
	{
		return Number.TryParseUInt32IntegerStyle(s, NumberStyles.Integer, NumberFormatInfo.CurrentInfo, out result) == Number.ParsingStatus.OK;
	}

	public static bool TryParse([NotNullWhen(true)] string? s, NumberStyles style, IFormatProvider? provider, out uint result)
	{
		NumberFormatInfo.ValidateParseStyleInteger(style);
		if (s == null)
		{
			result = 0u;
			return false;
		}
		return Number.TryParseUInt32(s, style, NumberFormatInfo.GetInstance(provider), out result) == Number.ParsingStatus.OK;
	}

	public static bool TryParse(ReadOnlySpan<char> s, NumberStyles style, IFormatProvider? provider, out uint result)
	{
		NumberFormatInfo.ValidateParseStyleInteger(style);
		return Number.TryParseUInt32(s, style, NumberFormatInfo.GetInstance(provider), out result) == Number.ParsingStatus.OK;
	}

	public TypeCode GetTypeCode()
	{
		return TypeCode.UInt32;
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
		return this;
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
		throw new InvalidCastException(SR.Format(SR.InvalidCast_FromTo, "UInt32", "DateTime"));
	}

	object IConvertible.ToType(Type type, IFormatProvider provider)
	{
		return Convert.DefaultToType(this, type, provider);
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static uint IAdditionOperators<uint, uint, uint>.operator +(uint left, uint right)
	{
		return left + right;
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static uint IBinaryInteger<uint>.LeadingZeroCount(uint value)
	{
		return (uint)BitOperations.LeadingZeroCount(value);
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static uint IBinaryInteger<uint>.PopCount(uint value)
	{
		return (uint)BitOperations.PopCount(value);
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static uint IBinaryInteger<uint>.RotateLeft(uint value, int rotateAmount)
	{
		return BitOperations.RotateLeft(value, rotateAmount);
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static uint IBinaryInteger<uint>.RotateRight(uint value, int rotateAmount)
	{
		return BitOperations.RotateRight(value, rotateAmount);
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static uint IBinaryInteger<uint>.TrailingZeroCount(uint value)
	{
		return (uint)BitOperations.TrailingZeroCount(value);
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static bool IBinaryNumber<uint>.IsPow2(uint value)
	{
		return BitOperations.IsPow2(value);
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static uint IBinaryNumber<uint>.Log2(uint value)
	{
		return (uint)BitOperations.Log2(value);
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static uint IBitwiseOperators<uint, uint, uint>.operator &(uint left, uint right)
	{
		return left & right;
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static uint IBitwiseOperators<uint, uint, uint>.operator |(uint left, uint right)
	{
		return left | right;
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static uint IBitwiseOperators<uint, uint, uint>.operator ^(uint left, uint right)
	{
		return left ^ right;
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static uint IBitwiseOperators<uint, uint, uint>.operator ~(uint value)
	{
		return ~value;
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static bool IComparisonOperators<uint, uint>.operator <(uint left, uint right)
	{
		return left < right;
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static bool IComparisonOperators<uint, uint>.operator <=(uint left, uint right)
	{
		return left <= right;
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static bool IComparisonOperators<uint, uint>.operator >(uint left, uint right)
	{
		return left > right;
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static bool IComparisonOperators<uint, uint>.operator >=(uint left, uint right)
	{
		return left >= right;
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static uint IDecrementOperators<uint>.operator --(uint value)
	{
		return --value;
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static uint IDivisionOperators<uint, uint, uint>.operator /(uint left, uint right)
	{
		return left / right;
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static bool IEqualityOperators<uint, uint>.operator ==(uint left, uint right)
	{
		return left == right;
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static bool IEqualityOperators<uint, uint>.operator !=(uint left, uint right)
	{
		return left != right;
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static uint IIncrementOperators<uint>.operator ++(uint value)
	{
		return ++value;
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static uint IModulusOperators<uint, uint, uint>.operator %(uint left, uint right)
	{
		return left % right;
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static uint IMultiplyOperators<uint, uint, uint>.operator *(uint left, uint right)
	{
		return left * right;
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static uint INumber<uint>.Abs(uint value)
	{
		return value;
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static uint INumber<uint>.Clamp(uint value, uint min, uint max)
	{
		return Math.Clamp(value, min, max);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static uint INumber<uint>.Create<TOther>(TOther value)
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
			return (uint)(decimal)(object)value;
		}
		checked
		{
			if (typeof(TOther) == typeof(double))
			{
				return (uint)(double)(object)value;
			}
			if (typeof(TOther) == typeof(short))
			{
				return (uint)(short)(object)value;
			}
			if (typeof(TOther) == typeof(int))
			{
				return (uint)(int)(object)value;
			}
			if (typeof(TOther) == typeof(long))
			{
				return (uint)(long)(object)value;
			}
			if (typeof(TOther) == typeof(IntPtr))
			{
				return (uint)(nint)(IntPtr)(object)value;
			}
			if (typeof(TOther) == typeof(sbyte))
			{
				return (uint)(sbyte)(object)value;
			}
			if (typeof(TOther) == typeof(float))
			{
				return (uint)(float)(object)value;
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
				return (uint)(ulong)(object)value;
			}
			if (typeof(TOther) == typeof(UIntPtr))
			{
				return (uint)(UIntPtr)(object)value;
			}
			ThrowHelper.ThrowNotSupportedException();
			return 0u;
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static uint INumber<uint>.CreateSaturating<TOther>(TOther value)
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
			if (!(num > 4294967295m))
			{
				if (!(num < 0m))
				{
					return (uint)num;
				}
				return 0u;
			}
			return uint.MaxValue;
		}
		if (typeof(TOther) == typeof(double))
		{
			double num2 = (double)(object)value;
			if (!(num2 > 4294967295.0))
			{
				if (!(num2 < 0.0))
				{
					return (uint)num2;
				}
				return 0u;
			}
			return uint.MaxValue;
		}
		if (typeof(TOther) == typeof(short))
		{
			short num3 = (short)(object)value;
			if (num3 >= 0)
			{
				return (uint)num3;
			}
			return 0u;
		}
		if (typeof(TOther) == typeof(int))
		{
			int num4 = (int)(object)value;
			if (num4 >= 0)
			{
				return (uint)num4;
			}
			return 0u;
		}
		if (typeof(TOther) == typeof(long))
		{
			long num5 = (long)(object)value;
			if (num5 <= uint.MaxValue)
			{
				if (num5 >= 0)
				{
					return (uint)num5;
				}
				return 0u;
			}
			return uint.MaxValue;
		}
		if (typeof(TOther) == typeof(IntPtr))
		{
			IntPtr intPtr = (IntPtr)(object)value;
			if ((nint)intPtr <= uint.MaxValue)
			{
				if ((nint)intPtr >= 0)
				{
					return (uint)(nint)intPtr;
				}
				return 0u;
			}
			return uint.MaxValue;
		}
		if (typeof(TOther) == typeof(sbyte))
		{
			sbyte b = (sbyte)(object)value;
			if (b >= 0)
			{
				return (uint)b;
			}
			return 0u;
		}
		if (typeof(TOther) == typeof(float))
		{
			float num6 = (float)(object)value;
			if (!(num6 > 4.2949673E+09f))
			{
				if (!(num6 < 0f))
				{
					return (uint)num6;
				}
				return 0u;
			}
			return uint.MaxValue;
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
			ulong num7 = (ulong)(object)value;
			if (num7 <= uint.MaxValue)
			{
				return (uint)num7;
			}
			return uint.MaxValue;
		}
		if (typeof(TOther) == typeof(UIntPtr))
		{
			UIntPtr uIntPtr = (UIntPtr)(object)value;
			if ((nuint)uIntPtr <= uint.MaxValue)
			{
				return (uint)(nuint)uIntPtr;
			}
			return uint.MaxValue;
		}
		ThrowHelper.ThrowNotSupportedException();
		return 0u;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static uint INumber<uint>.CreateTruncating<TOther>(TOther value)
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
			return (uint)(decimal)(object)value;
		}
		if (typeof(TOther) == typeof(double))
		{
			return (uint)(double)(object)value;
		}
		if (typeof(TOther) == typeof(short))
		{
			return (uint)(short)(object)value;
		}
		if (typeof(TOther) == typeof(int))
		{
			return (uint)(int)(object)value;
		}
		if (typeof(TOther) == typeof(long))
		{
			return (uint)(long)(object)value;
		}
		if (typeof(TOther) == typeof(IntPtr))
		{
			return (uint)(nint)(IntPtr)(object)value;
		}
		if (typeof(TOther) == typeof(sbyte))
		{
			return (uint)(sbyte)(object)value;
		}
		if (typeof(TOther) == typeof(float))
		{
			return (uint)(float)(object)value;
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
			return (uint)(ulong)(object)value;
		}
		if (typeof(TOther) == typeof(UIntPtr))
		{
			return (uint)(nuint)(UIntPtr)(object)value;
		}
		ThrowHelper.ThrowNotSupportedException();
		return 0u;
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static (uint Quotient, uint Remainder) INumber<uint>.DivRem(uint left, uint right)
	{
		return Math.DivRem(left, right);
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static uint INumber<uint>.Max(uint x, uint y)
	{
		return Math.Max(x, y);
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static uint INumber<uint>.Min(uint x, uint y)
	{
		return Math.Min(x, y);
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static uint INumber<uint>.Parse(string s, NumberStyles style, IFormatProvider provider)
	{
		return Parse(s, style, provider);
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static uint INumber<uint>.Parse(ReadOnlySpan<char> s, NumberStyles style, IFormatProvider provider)
	{
		return Parse(s, style, provider);
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static uint INumber<uint>.Sign(uint value)
	{
		if (value != 0)
		{
			return 1u;
		}
		return 0u;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static bool INumber<uint>.TryCreate<TOther>(TOther value, out uint result)
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
			if (num < 0m || num > 4294967295m)
			{
				result = 0u;
				return false;
			}
			result = (uint)num;
			return true;
		}
		if (typeof(TOther) == typeof(double))
		{
			double num2 = (double)(object)value;
			if (num2 < 0.0 || num2 > 4294967295.0)
			{
				result = 0u;
				return false;
			}
			result = (uint)num2;
			return true;
		}
		if (typeof(TOther) == typeof(short))
		{
			short num3 = (short)(object)value;
			if (num3 < 0)
			{
				result = 0u;
				return false;
			}
			result = (uint)num3;
			return true;
		}
		if (typeof(TOther) == typeof(int))
		{
			int num4 = (int)(object)value;
			if (num4 < 0)
			{
				result = 0u;
				return false;
			}
			result = (uint)num4;
			return true;
		}
		if (typeof(TOther) == typeof(long))
		{
			long num5 = (long)(object)value;
			if (num5 < 0 || num5 > uint.MaxValue)
			{
				result = 0u;
				return false;
			}
			result = (uint)num5;
			return true;
		}
		if (typeof(TOther) == typeof(IntPtr))
		{
			IntPtr intPtr = (IntPtr)(object)value;
			if ((nint)intPtr < 0 || (nint)intPtr > uint.MaxValue)
			{
				result = 0u;
				return false;
			}
			result = (uint)(nint)intPtr;
			return true;
		}
		if (typeof(TOther) == typeof(sbyte))
		{
			sbyte b = (sbyte)(object)value;
			if (b < 0)
			{
				result = 0u;
				return false;
			}
			result = (uint)b;
			return true;
		}
		if (typeof(TOther) == typeof(float))
		{
			float num6 = (float)(object)value;
			if (num6 < 0f || num6 > 4.2949673E+09f)
			{
				result = 0u;
				return false;
			}
			result = (uint)num6;
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
			ulong num7 = (ulong)(object)value;
			if (num7 > uint.MaxValue)
			{
				result = 0u;
				return false;
			}
			result = (uint)num7;
			return true;
		}
		if (typeof(TOther) == typeof(UIntPtr))
		{
			UIntPtr uIntPtr = (UIntPtr)(object)value;
			if ((nuint)uIntPtr > uint.MaxValue)
			{
				result = 0u;
				return false;
			}
			result = (uint)(nuint)uIntPtr;
			return true;
		}
		ThrowHelper.ThrowNotSupportedException();
		result = 0u;
		return false;
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static bool INumber<uint>.TryParse([NotNullWhen(true)] string s, NumberStyles style, IFormatProvider provider, out uint result)
	{
		return TryParse(s, style, provider, out result);
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static bool INumber<uint>.TryParse(ReadOnlySpan<char> s, NumberStyles style, IFormatProvider provider, out uint result)
	{
		return TryParse(s, style, provider, out result);
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static uint IParseable<uint>.Parse(string s, IFormatProvider provider)
	{
		return Parse(s, provider);
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static bool IParseable<uint>.TryParse([NotNullWhen(true)] string s, IFormatProvider provider, out uint result)
	{
		return TryParse(s, NumberStyles.Integer, provider, out result);
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static uint IShiftOperators<uint, uint>.operator <<(uint value, int shiftAmount)
	{
		return value << shiftAmount;
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static uint IShiftOperators<uint, uint>.operator >>(uint value, int shiftAmount)
	{
		return value >> shiftAmount;
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static uint ISpanParseable<uint>.Parse(ReadOnlySpan<char> s, IFormatProvider provider)
	{
		return Parse(s, NumberStyles.Integer, provider);
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static bool ISpanParseable<uint>.TryParse(ReadOnlySpan<char> s, IFormatProvider provider, out uint result)
	{
		return TryParse(s, NumberStyles.Integer, provider, out result);
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static uint ISubtractionOperators<uint, uint, uint>.operator -(uint left, uint right)
	{
		return left - right;
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static uint IUnaryNegationOperators<uint, uint>.operator -(uint value)
	{
		return 0 - value;
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static uint IUnaryPlusOperators<uint, uint>.operator +(uint value)
	{
		return value;
	}
}
