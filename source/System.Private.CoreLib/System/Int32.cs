using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.Versioning;

namespace System;

[Serializable]
[TypeForwardedFrom("mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
public readonly struct Int32 : IComparable, IConvertible, ISpanFormattable, IFormattable, IComparable<int>, IEquatable<int>, IBinaryInteger<int>, IBinaryNumber<int>, IBitwiseOperators<int, int, int>, INumber<int>, IAdditionOperators<int, int, int>, IAdditiveIdentity<int, int>, IComparisonOperators<int, int>, IEqualityOperators<int, int>, IDecrementOperators<int>, IDivisionOperators<int, int, int>, IIncrementOperators<int>, IModulusOperators<int, int, int>, IMultiplicativeIdentity<int, int>, IMultiplyOperators<int, int, int>, ISpanParseable<int>, IParseable<int>, ISubtractionOperators<int, int, int>, IUnaryNegationOperators<int, int>, IUnaryPlusOperators<int, int>, IShiftOperators<int, int>, IMinMaxValue<int>, ISignedNumber<int>
{
	private readonly int m_value;

	public const int MaxValue = 2147483647;

	public const int MinValue = -2147483648;

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static int IAdditiveIdentity<int, int>.AdditiveIdentity => 0;

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static int IMinMaxValue<int>.MinValue => int.MinValue;

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static int IMinMaxValue<int>.MaxValue => int.MaxValue;

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static int IMultiplicativeIdentity<int, int>.MultiplicativeIdentity => 1;

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static int INumber<int>.One => 1;

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static int INumber<int>.Zero => 0;

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static int ISignedNumber<int>.NegativeOne => -1;

	public int CompareTo(object? value)
	{
		if (value == null)
		{
			return 1;
		}
		if (value is int num)
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
		throw new ArgumentException(SR.Arg_MustBeInt32);
	}

	public int CompareTo(int value)
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
		if (!(obj is int))
		{
			return false;
		}
		return this == (int)obj;
	}

	[NonVersionable]
	public bool Equals(int obj)
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
		return Number.FormatInt32(this, -1, format, provider);
	}

	public bool TryFormat(Span<char> destination, out int charsWritten, ReadOnlySpan<char> format = default(ReadOnlySpan<char>), IFormatProvider? provider = null)
	{
		return Number.TryFormatInt32(this, -1, format, provider, destination, out charsWritten);
	}

	public static int Parse(string s)
	{
		if (s == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.s);
		}
		return Number.ParseInt32(s, NumberStyles.Integer, NumberFormatInfo.CurrentInfo);
	}

	public static int Parse(string s, NumberStyles style)
	{
		NumberFormatInfo.ValidateParseStyleInteger(style);
		if (s == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.s);
		}
		return Number.ParseInt32(s, style, NumberFormatInfo.CurrentInfo);
	}

	public static int Parse(string s, IFormatProvider? provider)
	{
		if (s == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.s);
		}
		return Number.ParseInt32(s, NumberStyles.Integer, NumberFormatInfo.GetInstance(provider));
	}

	public static int Parse(string s, NumberStyles style, IFormatProvider? provider)
	{
		NumberFormatInfo.ValidateParseStyleInteger(style);
		if (s == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.s);
		}
		return Number.ParseInt32(s, style, NumberFormatInfo.GetInstance(provider));
	}

	public static int Parse(ReadOnlySpan<char> s, NumberStyles style = NumberStyles.Integer, IFormatProvider? provider = null)
	{
		NumberFormatInfo.ValidateParseStyleInteger(style);
		return Number.ParseInt32(s, style, NumberFormatInfo.GetInstance(provider));
	}

	public static bool TryParse([NotNullWhen(true)] string? s, out int result)
	{
		if (s == null)
		{
			result = 0;
			return false;
		}
		return Number.TryParseInt32IntegerStyle(s, NumberStyles.Integer, NumberFormatInfo.CurrentInfo, out result) == Number.ParsingStatus.OK;
	}

	public static bool TryParse(ReadOnlySpan<char> s, out int result)
	{
		return Number.TryParseInt32IntegerStyle(s, NumberStyles.Integer, NumberFormatInfo.CurrentInfo, out result) == Number.ParsingStatus.OK;
	}

	public static bool TryParse([NotNullWhen(true)] string? s, NumberStyles style, IFormatProvider? provider, out int result)
	{
		NumberFormatInfo.ValidateParseStyleInteger(style);
		if (s == null)
		{
			result = 0;
			return false;
		}
		return Number.TryParseInt32(s, style, NumberFormatInfo.GetInstance(provider), out result) == Number.ParsingStatus.OK;
	}

	public static bool TryParse(ReadOnlySpan<char> s, NumberStyles style, IFormatProvider? provider, out int result)
	{
		NumberFormatInfo.ValidateParseStyleInteger(style);
		return Number.TryParseInt32(s, style, NumberFormatInfo.GetInstance(provider), out result) == Number.ParsingStatus.OK;
	}

	public TypeCode GetTypeCode()
	{
		return TypeCode.Int32;
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
		throw new InvalidCastException(SR.Format(SR.InvalidCast_FromTo, "Int32", "DateTime"));
	}

	object IConvertible.ToType(Type type, IFormatProvider provider)
	{
		return Convert.DefaultToType(this, type, provider);
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static int IAdditionOperators<int, int, int>.operator +(int left, int right)
	{
		return left + right;
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static int IBinaryInteger<int>.LeadingZeroCount(int value)
	{
		return BitOperations.LeadingZeroCount((uint)value);
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static int IBinaryInteger<int>.PopCount(int value)
	{
		return BitOperations.PopCount((uint)value);
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static int IBinaryInteger<int>.RotateLeft(int value, int rotateAmount)
	{
		return (int)BitOperations.RotateLeft((uint)value, rotateAmount);
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static int IBinaryInteger<int>.RotateRight(int value, int rotateAmount)
	{
		return (int)BitOperations.RotateRight((uint)value, rotateAmount);
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static int IBinaryInteger<int>.TrailingZeroCount(int value)
	{
		return BitOperations.TrailingZeroCount(value);
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static bool IBinaryNumber<int>.IsPow2(int value)
	{
		return BitOperations.IsPow2(value);
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static int IBinaryNumber<int>.Log2(int value)
	{
		if (value < 0)
		{
			ThrowHelper.ThrowValueArgumentOutOfRange_NeedNonNegNumException();
		}
		return BitOperations.Log2((uint)value);
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static int IBitwiseOperators<int, int, int>.operator &(int left, int right)
	{
		return left & right;
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static int IBitwiseOperators<int, int, int>.operator |(int left, int right)
	{
		return left | right;
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static int IBitwiseOperators<int, int, int>.operator ^(int left, int right)
	{
		return left ^ right;
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static int IBitwiseOperators<int, int, int>.operator ~(int value)
	{
		return ~value;
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static bool IComparisonOperators<int, int>.operator <(int left, int right)
	{
		return left < right;
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static bool IComparisonOperators<int, int>.operator <=(int left, int right)
	{
		return left <= right;
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static bool IComparisonOperators<int, int>.operator >(int left, int right)
	{
		return left > right;
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static bool IComparisonOperators<int, int>.operator >=(int left, int right)
	{
		return left >= right;
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static int IDecrementOperators<int>.operator --(int value)
	{
		return --value;
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static int IDivisionOperators<int, int, int>.operator /(int left, int right)
	{
		return left / right;
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static bool IEqualityOperators<int, int>.operator ==(int left, int right)
	{
		return left == right;
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static bool IEqualityOperators<int, int>.operator !=(int left, int right)
	{
		return left != right;
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static int IIncrementOperators<int>.operator ++(int value)
	{
		return ++value;
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static int IModulusOperators<int, int, int>.operator %(int left, int right)
	{
		return left % right;
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static int IMultiplyOperators<int, int, int>.operator *(int left, int right)
	{
		return left * right;
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static int INumber<int>.Abs(int value)
	{
		return Math.Abs(value);
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static int INumber<int>.Clamp(int value, int min, int max)
	{
		return Math.Clamp(value, min, max);
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	internal static int Create<TOther>(TOther value) where TOther : INumber<TOther>
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
			return (int)(decimal)(object)value;
		}
		checked
		{
			if (typeof(TOther) == typeof(double))
			{
				return (int)(double)(object)value;
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
				return (int)(long)(object)value;
			}
			if (typeof(TOther) == typeof(IntPtr))
			{
				return (int)(IntPtr)(object)value;
			}
			if (typeof(TOther) == typeof(sbyte))
			{
				return (sbyte)(object)value;
			}
			if (typeof(TOther) == typeof(float))
			{
				return (int)(float)(object)value;
			}
			if (typeof(TOther) == typeof(ushort))
			{
				return (ushort)(object)value;
			}
			if (typeof(TOther) == typeof(uint))
			{
				return (int)(uint)(object)value;
			}
			if (typeof(TOther) == typeof(ulong))
			{
				return (int)(ulong)(object)value;
			}
			if (typeof(TOther) == typeof(UIntPtr))
			{
				return (int)(nuint)(UIntPtr)(object)value;
			}
			ThrowHelper.ThrowNotSupportedException();
			return 0;
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static int INumber<int>.Create<TOther>(TOther value)
	{
		return Create(value);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static int INumber<int>.CreateSaturating<TOther>(TOther value)
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
			if (!(num > 2147483647m))
			{
				if (!(num < -2147483648m))
				{
					return (int)num;
				}
				return int.MinValue;
			}
			return int.MaxValue;
		}
		if (typeof(TOther) == typeof(double))
		{
			double num2 = (double)(object)value;
			if (!(num2 > 2147483647.0))
			{
				if (!(num2 < -2147483648.0))
				{
					return (int)num2;
				}
				return int.MinValue;
			}
			return int.MaxValue;
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
			long num3 = (long)(object)value;
			if (num3 <= int.MaxValue)
			{
				if (num3 >= int.MinValue)
				{
					return (int)num3;
				}
				return int.MinValue;
			}
			return int.MaxValue;
		}
		if (typeof(TOther) == typeof(IntPtr))
		{
			IntPtr intPtr = (IntPtr)(object)value;
			if ((nint)intPtr <= int.MaxValue)
			{
				if ((nint)intPtr >= int.MinValue)
				{
					return (int)(nint)intPtr;
				}
				return int.MinValue;
			}
			return int.MaxValue;
		}
		if (typeof(TOther) == typeof(sbyte))
		{
			return (sbyte)(object)value;
		}
		if (typeof(TOther) == typeof(float))
		{
			float num4 = (float)(object)value;
			if (!(num4 > 2.1474836E+09f))
			{
				if (!(num4 < -2.1474836E+09f))
				{
					return (int)num4;
				}
				return int.MinValue;
			}
			return int.MaxValue;
		}
		if (typeof(TOther) == typeof(ushort))
		{
			return (ushort)(object)value;
		}
		if (typeof(TOther) == typeof(uint))
		{
			uint num5 = (uint)(object)value;
			if (num5 <= int.MaxValue)
			{
				return (int)num5;
			}
			return int.MaxValue;
		}
		if (typeof(TOther) == typeof(ulong))
		{
			ulong num6 = (ulong)(object)value;
			if (num6 <= int.MaxValue)
			{
				return (int)num6;
			}
			return int.MaxValue;
		}
		if (typeof(TOther) == typeof(UIntPtr))
		{
			UIntPtr uIntPtr = (UIntPtr)(object)value;
			if ((nuint)uIntPtr <= int.MaxValue)
			{
				return (int)(nuint)uIntPtr;
			}
			return int.MaxValue;
		}
		ThrowHelper.ThrowNotSupportedException();
		return 0;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static int INumber<int>.CreateTruncating<TOther>(TOther value)
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
			return (int)(decimal)(object)value;
		}
		if (typeof(TOther) == typeof(double))
		{
			return (int)(double)(object)value;
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
			return (int)(long)(object)value;
		}
		if (typeof(TOther) == typeof(IntPtr))
		{
			return (int)(nint)(IntPtr)(object)value;
		}
		if (typeof(TOther) == typeof(sbyte))
		{
			return (sbyte)(object)value;
		}
		if (typeof(TOther) == typeof(float))
		{
			return (int)(float)(object)value;
		}
		if (typeof(TOther) == typeof(ushort))
		{
			return (ushort)(object)value;
		}
		if (typeof(TOther) == typeof(uint))
		{
			return (int)(uint)(object)value;
		}
		if (typeof(TOther) == typeof(ulong))
		{
			return (int)(ulong)(object)value;
		}
		if (typeof(TOther) == typeof(UIntPtr))
		{
			return (int)(nuint)(UIntPtr)(object)value;
		}
		ThrowHelper.ThrowNotSupportedException();
		return 0;
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static (int Quotient, int Remainder) INumber<int>.DivRem(int left, int right)
	{
		return Math.DivRem(left, right);
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static int INumber<int>.Max(int x, int y)
	{
		return Math.Max(x, y);
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static int INumber<int>.Min(int x, int y)
	{
		return Math.Min(x, y);
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static int INumber<int>.Parse(string s, NumberStyles style, IFormatProvider provider)
	{
		return Parse(s, style, provider);
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static int INumber<int>.Parse(ReadOnlySpan<char> s, NumberStyles style, IFormatProvider provider)
	{
		return Parse(s, style, provider);
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static int INumber<int>.Sign(int value)
	{
		return Math.Sign(value);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static bool INumber<int>.TryCreate<TOther>(TOther value, out int result)
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
			if (num < -2147483648m || num > 2147483647m)
			{
				result = 0;
				return false;
			}
			result = (int)num;
			return true;
		}
		if (typeof(TOther) == typeof(double))
		{
			double num2 = (double)(object)value;
			if (num2 < -2147483648.0 || num2 > 2147483647.0)
			{
				result = 0;
				return false;
			}
			result = (int)num2;
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
			long num3 = (long)(object)value;
			if (num3 < int.MinValue || num3 > int.MaxValue)
			{
				result = 0;
				return false;
			}
			result = (int)num3;
			return true;
		}
		if (typeof(TOther) == typeof(IntPtr))
		{
			IntPtr intPtr = (IntPtr)(object)value;
			if ((nint)intPtr < int.MinValue || (nint)intPtr > int.MaxValue)
			{
				result = 0;
				return false;
			}
			result = (int)(nint)intPtr;
			return true;
		}
		if (typeof(TOther) == typeof(sbyte))
		{
			result = (sbyte)(object)value;
			return true;
		}
		if (typeof(TOther) == typeof(float))
		{
			float num4 = (float)(object)value;
			if (num4 < -2.1474836E+09f || num4 > 2.1474836E+09f)
			{
				result = 0;
				return false;
			}
			result = (int)num4;
			return true;
		}
		if (typeof(TOther) == typeof(ushort))
		{
			result = (ushort)(object)value;
			return true;
		}
		if (typeof(TOther) == typeof(uint))
		{
			uint num5 = (uint)(object)value;
			if (num5 > int.MaxValue)
			{
				result = 0;
				return false;
			}
			result = (int)num5;
			return true;
		}
		if (typeof(TOther) == typeof(ulong))
		{
			ulong num6 = (ulong)(object)value;
			if (num6 > int.MaxValue)
			{
				result = 0;
				return false;
			}
			result = (int)num6;
			return true;
		}
		if (typeof(TOther) == typeof(UIntPtr))
		{
			UIntPtr uIntPtr = (UIntPtr)(object)value;
			if ((nuint)uIntPtr > int.MaxValue)
			{
				result = 0;
				return false;
			}
			result = (int)(nuint)uIntPtr;
			return true;
		}
		ThrowHelper.ThrowNotSupportedException();
		result = 0;
		return false;
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static bool INumber<int>.TryParse([NotNullWhen(true)] string s, NumberStyles style, IFormatProvider provider, out int result)
	{
		return TryParse(s, style, provider, out result);
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static bool INumber<int>.TryParse(ReadOnlySpan<char> s, NumberStyles style, IFormatProvider provider, out int result)
	{
		return TryParse(s, style, provider, out result);
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static int IParseable<int>.Parse(string s, IFormatProvider provider)
	{
		return Parse(s, provider);
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static bool IParseable<int>.TryParse([NotNullWhen(true)] string s, IFormatProvider provider, out int result)
	{
		return TryParse(s, NumberStyles.Integer, provider, out result);
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static int IShiftOperators<int, int>.operator <<(int value, int shiftAmount)
	{
		return value << shiftAmount;
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static int IShiftOperators<int, int>.operator >>(int value, int shiftAmount)
	{
		return value >> shiftAmount;
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static int ISpanParseable<int>.Parse(ReadOnlySpan<char> s, IFormatProvider provider)
	{
		return Parse(s, NumberStyles.Integer, provider);
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static bool ISpanParseable<int>.TryParse(ReadOnlySpan<char> s, IFormatProvider provider, out int result)
	{
		return TryParse(s, NumberStyles.Integer, provider, out result);
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static int ISubtractionOperators<int, int, int>.operator -(int left, int right)
	{
		return left - right;
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static int IUnaryNegationOperators<int, int>.operator -(int value)
	{
		return -value;
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static int IUnaryPlusOperators<int, int>.operator +(int value)
	{
		return value;
	}
}
