using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using System.Runtime.Versioning;
using Internal.Runtime.CompilerServices;

namespace System;

[Serializable]
[TypeForwardedFrom("mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
public readonly struct IntPtr : IEquatable<nint>, IComparable, IComparable<nint>, ISpanFormattable, IFormattable, ISerializable, IBinaryInteger<nint>, IBinaryNumber<nint>, IBitwiseOperators<nint, nint, nint>, INumber<nint>, IAdditionOperators<nint, nint, nint>, IAdditiveIdentity<nint, nint>, IComparisonOperators<nint, nint>, IEqualityOperators<nint, nint>, IDecrementOperators<nint>, IDivisionOperators<nint, nint, nint>, IIncrementOperators<nint>, IModulusOperators<nint, nint, nint>, IMultiplicativeIdentity<nint, nint>, IMultiplyOperators<nint, nint, nint>, ISpanParseable<nint>, IParseable<nint>, ISubtractionOperators<nint, nint, nint>, IUnaryNegationOperators<nint, nint>, IUnaryPlusOperators<nint, nint>, IShiftOperators<nint, nint>, IMinMaxValue<nint>, ISignedNumber<nint>
{
	private unsafe readonly void* _value;

	[Intrinsic]
	public static readonly IntPtr Zero;

	public static int Size
	{
		[NonVersionable]
		get
		{
			return 8;
		}
	}

	public static IntPtr MaxValue
	{
		[NonVersionable]
		get
		{
			return (IntPtr)long.MaxValue;
		}
	}

	public static IntPtr MinValue
	{
		[NonVersionable]
		get
		{
			return (IntPtr)long.MinValue;
		}
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static nint IAdditiveIdentity<IntPtr, IntPtr>.AdditiveIdentity => 0;

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static nint IMinMaxValue<IntPtr>.MinValue => MinValue;

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static nint IMinMaxValue<IntPtr>.MaxValue => MaxValue;

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static nint IMultiplicativeIdentity<IntPtr, IntPtr>.MultiplicativeIdentity => 1;

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static nint INumber<IntPtr>.One => 1;

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static nint INumber<IntPtr>.Zero => 0;

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static nint ISignedNumber<IntPtr>.NegativeOne => -1;

	[NonVersionable]
	public unsafe IntPtr(int value)
	{
		_value = (void*)value;
	}

	[NonVersionable]
	public unsafe IntPtr(long value)
	{
		_value = (void*)value;
	}

	[CLSCompliant(false)]
	[NonVersionable]
	public unsafe IntPtr(void* value)
	{
		_value = value;
	}

	private unsafe IntPtr(SerializationInfo info, StreamingContext context)
	{
		long @int = info.GetInt64("value");
		if (Size == 4)
		{
		}
		_value = (void*)@int;
	}

	void ISerializable.GetObjectData(SerializationInfo info, StreamingContext context)
	{
		if (info == null)
		{
			throw new ArgumentNullException("info");
		}
		info.AddValue("value", ToInt64());
	}

	public unsafe override bool Equals([NotNullWhen(true)] object? obj)
	{
		if (obj is IntPtr intPtr)
		{
			return _value == intPtr._value;
		}
		return false;
	}

	public unsafe override int GetHashCode()
	{
		long num = (long)_value;
		return (int)num ^ (int)(num >> 32);
	}

	[NonVersionable]
	public unsafe int ToInt32()
	{
		long num = (long)_value;
		return checked((int)num);
	}

	[NonVersionable]
	public unsafe long ToInt64()
	{
		return (nint)_value;
	}

	[NonVersionable]
	public static explicit operator IntPtr(int value)
	{
		return new IntPtr(value);
	}

	[NonVersionable]
	public static explicit operator IntPtr(long value)
	{
		return new IntPtr(value);
	}

	[CLSCompliant(false)]
	[NonVersionable]
	public unsafe static explicit operator IntPtr(void* value)
	{
		return new IntPtr(value);
	}

	[CLSCompliant(false)]
	[NonVersionable]
	public unsafe static explicit operator void*(IntPtr value)
	{
		return value._value;
	}

	[NonVersionable]
	public unsafe static explicit operator int(IntPtr value)
	{
		long num = (long)value._value;
		return checked((int)num);
	}

	[NonVersionable]
	public unsafe static explicit operator long(IntPtr value)
	{
		return (nint)value._value;
	}

	[NonVersionable]
	public unsafe static bool operator ==(IntPtr value1, IntPtr value2)
	{
		return value1._value == value2._value;
	}

	[NonVersionable]
	public unsafe static bool operator !=(IntPtr value1, IntPtr value2)
	{
		return value1._value != value2._value;
	}

	[NonVersionable]
	public static IntPtr Add(IntPtr pointer, int offset)
	{
		return pointer + offset;
	}

	[NonVersionable]
	public unsafe static IntPtr operator +(IntPtr pointer, int offset)
	{
		return (nint)((byte*)pointer._value + offset);
	}

	[NonVersionable]
	public static IntPtr Subtract(IntPtr pointer, int offset)
	{
		return pointer - offset;
	}

	[NonVersionable]
	public unsafe static IntPtr operator -(IntPtr pointer, int offset)
	{
		return (nint)((byte*)pointer._value - offset);
	}

	[CLSCompliant(false)]
	[NonVersionable]
	public unsafe void* ToPointer()
	{
		return _value;
	}

	public unsafe int CompareTo(object? value)
	{
		if (value == null)
		{
			return 1;
		}
		if (value is IntPtr intPtr)
		{
			if ((nint)_value < (nint)intPtr)
			{
				return -1;
			}
			if ((nint)_value > (nint)intPtr)
			{
				return 1;
			}
			return 0;
		}
		throw new ArgumentException(SR.Arg_MustBeIntPtr);
	}

	public unsafe int CompareTo(IntPtr value)
	{
		return ((long)_value).CompareTo((long)value);
	}

	[NonVersionable]
	public unsafe bool Equals(IntPtr other)
	{
		return (long)_value == (long)other;
	}

	public unsafe override string ToString()
	{
		return ((long)_value).ToString();
	}

	public unsafe string ToString(string? format)
	{
		return ((long)_value).ToString(format);
	}

	public unsafe string ToString(IFormatProvider? provider)
	{
		return ((long)_value).ToString(provider);
	}

	public unsafe string ToString(string? format, IFormatProvider? provider)
	{
		return ((long)_value).ToString(format, provider);
	}

	public unsafe bool TryFormat(Span<char> destination, out int charsWritten, ReadOnlySpan<char> format = default(ReadOnlySpan<char>), IFormatProvider? provider = null)
	{
		return ((long)_value).TryFormat(destination, out charsWritten, format, provider);
	}

	public static IntPtr Parse(string s)
	{
		return (IntPtr)long.Parse(s);
	}

	public static IntPtr Parse(string s, NumberStyles style)
	{
		return (IntPtr)long.Parse(s, style);
	}

	public static IntPtr Parse(string s, IFormatProvider? provider)
	{
		return (IntPtr)long.Parse(s, provider);
	}

	public static IntPtr Parse(string s, NumberStyles style, IFormatProvider? provider)
	{
		return (IntPtr)long.Parse(s, style, provider);
	}

	public static IntPtr Parse(ReadOnlySpan<char> s, NumberStyles style = NumberStyles.Integer, IFormatProvider? provider = null)
	{
		return (IntPtr)long.Parse(s, style, provider);
	}

	public static bool TryParse([NotNullWhen(true)] string? s, out IntPtr result)
	{
		Unsafe.SkipInit<IntPtr>(out result);
		return long.TryParse(s, out Unsafe.As<IntPtr, long>(ref result));
	}

	public static bool TryParse([NotNullWhen(true)] string? s, NumberStyles style, IFormatProvider? provider, out IntPtr result)
	{
		Unsafe.SkipInit<IntPtr>(out result);
		return long.TryParse(s, style, provider, out Unsafe.As<IntPtr, long>(ref result));
	}

	public static bool TryParse(ReadOnlySpan<char> s, out IntPtr result)
	{
		Unsafe.SkipInit<IntPtr>(out result);
		return long.TryParse(s, out Unsafe.As<IntPtr, long>(ref result));
	}

	public static bool TryParse(ReadOnlySpan<char> s, NumberStyles style, IFormatProvider? provider, out IntPtr result)
	{
		Unsafe.SkipInit<IntPtr>(out result);
		return long.TryParse(s, style, provider, out Unsafe.As<IntPtr, long>(ref result));
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static nint IAdditionOperators<IntPtr, IntPtr, IntPtr>.operator +(nint left, nint right)
	{
		return left + right;
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static nint IBinaryInteger<IntPtr>.LeadingZeroCount(nint value)
	{
		_ = Environment.Is64BitProcess;
		return BitOperations.LeadingZeroCount((ulong)value);
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static nint IBinaryInteger<IntPtr>.PopCount(nint value)
	{
		_ = Environment.Is64BitProcess;
		return BitOperations.PopCount((ulong)value);
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static nint IBinaryInteger<IntPtr>.RotateLeft(nint value, int rotateAmount)
	{
		_ = Environment.Is64BitProcess;
		return (nint)BitOperations.RotateLeft((ulong)value, rotateAmount);
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static nint IBinaryInteger<IntPtr>.RotateRight(nint value, int rotateAmount)
	{
		_ = Environment.Is64BitProcess;
		return (nint)BitOperations.RotateRight((ulong)value, rotateAmount);
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static nint IBinaryInteger<IntPtr>.TrailingZeroCount(nint value)
	{
		_ = Environment.Is64BitProcess;
		return BitOperations.TrailingZeroCount((ulong)value);
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static bool IBinaryNumber<IntPtr>.IsPow2(nint value)
	{
		return BitOperations.IsPow2(value);
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static nint IBinaryNumber<IntPtr>.Log2(nint value)
	{
		if (value < 0)
		{
			ThrowHelper.ThrowValueArgumentOutOfRange_NeedNonNegNumException();
		}
		_ = Environment.Is64BitProcess;
		return BitOperations.Log2((ulong)value);
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static nint IBitwiseOperators<IntPtr, IntPtr, IntPtr>.operator &(nint left, nint right)
	{
		return left & right;
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static nint IBitwiseOperators<IntPtr, IntPtr, IntPtr>.operator |(nint left, nint right)
	{
		return left | right;
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static nint IBitwiseOperators<IntPtr, IntPtr, IntPtr>.operator ^(nint left, nint right)
	{
		return left ^ right;
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static nint IBitwiseOperators<IntPtr, IntPtr, IntPtr>.operator ~(nint value)
	{
		return ~value;
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static bool IComparisonOperators<IntPtr, IntPtr>.operator <(nint left, nint right)
	{
		return left < right;
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static bool IComparisonOperators<IntPtr, IntPtr>.operator <=(nint left, nint right)
	{
		return left <= right;
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static bool IComparisonOperators<IntPtr, IntPtr>.operator >(nint left, nint right)
	{
		return left > right;
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static bool IComparisonOperators<IntPtr, IntPtr>.operator >=(nint left, nint right)
	{
		return left >= right;
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static nint IDecrementOperators<IntPtr>.operator --(nint value)
	{
		return --value;
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static nint IDivisionOperators<IntPtr, IntPtr, IntPtr>.operator /(nint left, nint right)
	{
		return left / right;
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static bool IEqualityOperators<IntPtr, IntPtr>.operator ==(nint left, nint right)
	{
		return left == right;
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static bool IEqualityOperators<IntPtr, IntPtr>.operator !=(nint left, nint right)
	{
		return left != right;
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static nint IIncrementOperators<IntPtr>.operator ++(nint value)
	{
		return ++value;
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static nint IModulusOperators<IntPtr, IntPtr, IntPtr>.operator %(nint left, nint right)
	{
		return left % right;
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static nint IMultiplyOperators<IntPtr, IntPtr, IntPtr>.operator *(nint left, nint right)
	{
		return left * right;
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static nint INumber<IntPtr>.Abs(nint value)
	{
		return Math.Abs(value);
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static nint INumber<IntPtr>.Clamp(nint value, nint min, nint max)
	{
		return Math.Clamp(value, min, max);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static nint INumber<IntPtr>.Create<TOther>(TOther value)
	{
		if (typeof(TOther) == typeof(byte))
		{
			return (byte)(object)value;
		}
		if (typeof(TOther) == typeof(char))
		{
			return (char)(object)value;
		}
		checked
		{
			if (typeof(TOther) == typeof(decimal))
			{
				return (nint)(long)(decimal)(object)value;
			}
			if (typeof(TOther) == typeof(double))
			{
				return (nint)(double)(object)value;
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
				return (nint)(long)(object)value;
			}
			if (typeof(TOther) == typeof(IntPtr))
			{
				return (IntPtr)(object)value;
			}
			if (typeof(TOther) == typeof(sbyte))
			{
				return (sbyte)(object)value;
			}
			if (typeof(TOther) == typeof(float))
			{
				return (nint)(float)(object)value;
			}
			if (typeof(TOther) == typeof(ushort))
			{
				return (ushort)(object)value;
			}
			if (typeof(TOther) == typeof(uint))
			{
				return (nint)(uint)(object)value;
			}
			if (typeof(TOther) == typeof(ulong))
			{
				return (nint)(ulong)(object)value;
			}
			if (typeof(TOther) == typeof(UIntPtr))
			{
				return (nint)(nuint)(UIntPtr)(object)value;
			}
			ThrowHelper.ThrowNotSupportedException();
			return 0;
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static nint INumber<IntPtr>.CreateSaturating<TOther>(TOther value)
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
			if (!(num > (decimal)(long)MaxValue))
			{
				if (!(num < (decimal)(long)MinValue))
				{
					return (nint)(long)num;
				}
				return MinValue;
			}
			return MaxValue;
		}
		if (typeof(TOther) == typeof(double))
		{
			double num2 = (double)(object)value;
			if (!(num2 > (double)(nint)MaxValue))
			{
				if (!(num2 < (double)(nint)MinValue))
				{
					return (nint)num2;
				}
				return MinValue;
			}
			return MaxValue;
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
			if (num3 <= (nint)MaxValue)
			{
				if (num3 >= (nint)MinValue)
				{
					return (nint)num3;
				}
				return MinValue;
			}
			return MaxValue;
		}
		if (typeof(TOther) == typeof(IntPtr))
		{
			return (IntPtr)(object)value;
		}
		if (typeof(TOther) == typeof(sbyte))
		{
			return (sbyte)(object)value;
		}
		if (typeof(TOther) == typeof(float))
		{
			float num4 = (float)(object)value;
			if (!(num4 > (float)(nint)MaxValue))
			{
				if (!(num4 < (float)(nint)MinValue))
				{
					return (nint)num4;
				}
				return MinValue;
			}
			return MaxValue;
		}
		if (typeof(TOther) == typeof(ushort))
		{
			return (ushort)(object)value;
		}
		if (typeof(TOther) == typeof(uint))
		{
			uint num5 = (uint)(object)value;
			if (num5 <= (nint)MaxValue)
			{
				return (nint)num5;
			}
			return MaxValue;
		}
		if (typeof(TOther) == typeof(ulong))
		{
			ulong num6 = (ulong)(object)value;
			if (num6 <= (nuint)(nint)MaxValue)
			{
				return (nint)num6;
			}
			return MaxValue;
		}
		if (typeof(TOther) == typeof(UIntPtr))
		{
			UIntPtr uIntPtr = (UIntPtr)(object)value;
			if ((nuint)uIntPtr <= (nuint)(nint)MaxValue)
			{
				return (nint)(nuint)uIntPtr;
			}
			return MaxValue;
		}
		ThrowHelper.ThrowNotSupportedException();
		return 0;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static nint INumber<IntPtr>.CreateTruncating<TOther>(TOther value)
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
			return (nint)(long)(decimal)(object)value;
		}
		if (typeof(TOther) == typeof(double))
		{
			return (nint)(double)(object)value;
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
			return (nint)(long)(object)value;
		}
		if (typeof(TOther) == typeof(IntPtr))
		{
			return (IntPtr)(object)value;
		}
		if (typeof(TOther) == typeof(sbyte))
		{
			return (sbyte)(object)value;
		}
		if (typeof(TOther) == typeof(float))
		{
			return (nint)(float)(object)value;
		}
		if (typeof(TOther) == typeof(ushort))
		{
			return (ushort)(object)value;
		}
		if (typeof(TOther) == typeof(uint))
		{
			return (nint)(uint)(object)value;
		}
		if (typeof(TOther) == typeof(ulong))
		{
			return (nint)(ulong)(object)value;
		}
		if (typeof(TOther) == typeof(UIntPtr))
		{
			return (nint)(nuint)(UIntPtr)(object)value;
		}
		ThrowHelper.ThrowNotSupportedException();
		return 0;
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static (nint Quotient, nint Remainder) INumber<IntPtr>.DivRem(nint left, nint right)
	{
		return Math.DivRem(left, right);
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static nint INumber<IntPtr>.Max(nint x, nint y)
	{
		return Math.Max(x, y);
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static nint INumber<IntPtr>.Min(nint x, nint y)
	{
		return Math.Min(x, y);
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static nint INumber<IntPtr>.Parse(string s, NumberStyles style, IFormatProvider provider)
	{
		return Parse(s, style, provider);
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static nint INumber<IntPtr>.Parse(ReadOnlySpan<char> s, NumberStyles style, IFormatProvider provider)
	{
		return Parse(s, style, provider);
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static nint INumber<IntPtr>.Sign(nint value)
	{
		return Math.Sign(value);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static bool INumber<IntPtr>.TryCreate<TOther>(TOther value, out nint result)
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
			if (num < (decimal)(long)MinValue || num > (decimal)(long)MaxValue)
			{
				result = 0;
				return false;
			}
			result = (nint)(long)num;
			return true;
		}
		if (typeof(TOther) == typeof(double))
		{
			double num2 = (double)(object)value;
			if (num2 < (double)(nint)MinValue || num2 > (double)(nint)MaxValue)
			{
				result = 0;
				return false;
			}
			result = (nint)num2;
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
			if (num3 < (nint)MinValue || num3 > (nint)MaxValue)
			{
				result = 0;
				return false;
			}
			result = (nint)num3;
			return true;
		}
		if (typeof(TOther) == typeof(IntPtr))
		{
			result = (IntPtr)(object)value;
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
			if (num4 < (float)(nint)MinValue || num4 > (float)(nint)MaxValue)
			{
				result = 0;
				return false;
			}
			result = (nint)num4;
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
			if (num5 > (nint)MaxValue)
			{
				result = 0;
				return false;
			}
			result = (nint)num5;
			return true;
		}
		if (typeof(TOther) == typeof(ulong))
		{
			ulong num6 = (ulong)(object)value;
			if (num6 > (nuint)(nint)MaxValue)
			{
				result = 0;
				return false;
			}
			result = (nint)num6;
			return true;
		}
		if (typeof(TOther) == typeof(UIntPtr))
		{
			UIntPtr uIntPtr = (UIntPtr)(object)value;
			if ((nuint)uIntPtr > (nuint)(nint)MaxValue)
			{
				result = 0;
				return false;
			}
			result = (nint)(nuint)uIntPtr;
			return true;
		}
		ThrowHelper.ThrowNotSupportedException();
		result = 0;
		return false;
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static bool INumber<IntPtr>.TryParse([NotNullWhen(true)] string s, NumberStyles style, IFormatProvider provider, out nint result)
	{
		return TryParse(s, style, provider, out result);
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static bool INumber<IntPtr>.TryParse(ReadOnlySpan<char> s, NumberStyles style, IFormatProvider provider, out nint result)
	{
		return TryParse(s, style, provider, out result);
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static nint IParseable<IntPtr>.Parse(string s, IFormatProvider provider)
	{
		return Parse(s, provider);
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static bool IParseable<IntPtr>.TryParse([NotNullWhen(true)] string s, IFormatProvider provider, out nint result)
	{
		return TryParse(s, NumberStyles.Integer, provider, out result);
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static nint IShiftOperators<IntPtr, IntPtr>.operator <<(nint value, int shiftAmount)
	{
		return value << shiftAmount;
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static nint IShiftOperators<IntPtr, IntPtr>.operator >>(nint value, int shiftAmount)
	{
		return value >> shiftAmount;
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static nint ISpanParseable<IntPtr>.Parse(ReadOnlySpan<char> s, IFormatProvider provider)
	{
		return Parse(s, NumberStyles.Integer, provider);
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static bool ISpanParseable<IntPtr>.TryParse(ReadOnlySpan<char> s, IFormatProvider provider, out nint result)
	{
		return TryParse(s, NumberStyles.Integer, provider, out result);
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static nint ISubtractionOperators<IntPtr, IntPtr, IntPtr>.operator -(nint left, nint right)
	{
		return left - right;
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static nint IUnaryNegationOperators<IntPtr, IntPtr>.operator -(nint value)
	{
		return -value;
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static nint IUnaryPlusOperators<IntPtr, IntPtr>.operator +(nint value)
	{
		return value;
	}
}
