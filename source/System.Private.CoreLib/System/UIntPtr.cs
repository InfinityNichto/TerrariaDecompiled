using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using System.Runtime.Versioning;
using Internal.Runtime.CompilerServices;

namespace System;

[Serializable]
[CLSCompliant(false)]
[TypeForwardedFrom("mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
public readonly struct UIntPtr : IEquatable<nuint>, IComparable, IComparable<nuint>, ISpanFormattable, IFormattable, ISerializable, IBinaryInteger<nuint>, IBinaryNumber<nuint>, IBitwiseOperators<nuint, nuint, nuint>, INumber<nuint>, IAdditionOperators<nuint, nuint, nuint>, IAdditiveIdentity<nuint, nuint>, IComparisonOperators<nuint, nuint>, IEqualityOperators<nuint, nuint>, IDecrementOperators<nuint>, IDivisionOperators<nuint, nuint, nuint>, IIncrementOperators<nuint>, IModulusOperators<nuint, nuint, nuint>, IMultiplicativeIdentity<nuint, nuint>, IMultiplyOperators<nuint, nuint, nuint>, ISpanParseable<nuint>, IParseable<nuint>, ISubtractionOperators<nuint, nuint, nuint>, IUnaryNegationOperators<nuint, nuint>, IUnaryPlusOperators<nuint, nuint>, IShiftOperators<nuint, nuint>, IMinMaxValue<nuint>, IUnsignedNumber<nuint>
{
	private unsafe readonly void* _value;

	[Intrinsic]
	public static readonly UIntPtr Zero;

	public static int Size
	{
		[NonVersionable]
		get
		{
			return 8;
		}
	}

	public static UIntPtr MaxValue
	{
		[NonVersionable]
		get
		{
			return (UIntPtr)ulong.MaxValue;
		}
	}

	public static UIntPtr MinValue
	{
		[NonVersionable]
		get
		{
			return (UIntPtr)0uL;
		}
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static nuint IAdditiveIdentity<UIntPtr, UIntPtr>.AdditiveIdentity => 0u;

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static nuint IMinMaxValue<UIntPtr>.MinValue => MinValue;

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static nuint IMinMaxValue<UIntPtr>.MaxValue => MaxValue;

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static nuint IMultiplicativeIdentity<UIntPtr, UIntPtr>.MultiplicativeIdentity => 1u;

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static nuint INumber<UIntPtr>.One => 1u;

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static nuint INumber<UIntPtr>.Zero => 0u;

	[NonVersionable]
	public unsafe UIntPtr(uint value)
	{
		_value = (void*)value;
	}

	[NonVersionable]
	public unsafe UIntPtr(ulong value)
	{
		_value = (void*)value;
	}

	[NonVersionable]
	public unsafe UIntPtr(void* value)
	{
		_value = value;
	}

	private unsafe UIntPtr(SerializationInfo info, StreamingContext context)
	{
		ulong uInt = info.GetUInt64("value");
		if (Size == 4)
		{
		}
		_value = (void*)uInt;
	}

	void ISerializable.GetObjectData(SerializationInfo info, StreamingContext context)
	{
		if (info == null)
		{
			throw new ArgumentNullException("info");
		}
		info.AddValue("value", ToUInt64());
	}

	public unsafe override bool Equals([NotNullWhen(true)] object? obj)
	{
		if (obj is UIntPtr)
		{
			return _value == ((UIntPtr)obj)._value;
		}
		return false;
	}

	public unsafe override int GetHashCode()
	{
		ulong num = (ulong)_value;
		return (int)num ^ (int)(num >> 32);
	}

	[NonVersionable]
	public unsafe uint ToUInt32()
	{
		return checked((uint)_value);
	}

	[NonVersionable]
	public unsafe ulong ToUInt64()
	{
		return (ulong)_value;
	}

	[NonVersionable]
	public static explicit operator UIntPtr(uint value)
	{
		return new UIntPtr(value);
	}

	[NonVersionable]
	public static explicit operator UIntPtr(ulong value)
	{
		return new UIntPtr(value);
	}

	[NonVersionable]
	public unsafe static explicit operator UIntPtr(void* value)
	{
		return new UIntPtr(value);
	}

	[NonVersionable]
	public unsafe static explicit operator void*(UIntPtr value)
	{
		return value._value;
	}

	[NonVersionable]
	public unsafe static explicit operator uint(UIntPtr value)
	{
		return checked((uint)value._value);
	}

	[NonVersionable]
	public unsafe static explicit operator ulong(UIntPtr value)
	{
		return (ulong)value._value;
	}

	[NonVersionable]
	public unsafe static bool operator ==(UIntPtr value1, UIntPtr value2)
	{
		return value1._value == value2._value;
	}

	[NonVersionable]
	public unsafe static bool operator !=(UIntPtr value1, UIntPtr value2)
	{
		return value1._value != value2._value;
	}

	[NonVersionable]
	public static UIntPtr Add(UIntPtr pointer, int offset)
	{
		return pointer + offset;
	}

	[NonVersionable]
	public unsafe static UIntPtr operator +(UIntPtr pointer, int offset)
	{
		return (UIntPtr)((byte*)pointer._value + offset);
	}

	[NonVersionable]
	public static UIntPtr Subtract(UIntPtr pointer, int offset)
	{
		return pointer - offset;
	}

	[NonVersionable]
	public unsafe static UIntPtr operator -(UIntPtr pointer, int offset)
	{
		return (UIntPtr)((byte*)pointer._value - offset);
	}

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
		if (value is UIntPtr uIntPtr)
		{
			if ((nuint)_value < (nuint)uIntPtr)
			{
				return -1;
			}
			if ((nuint)_value > (nuint)uIntPtr)
			{
				return 1;
			}
			return 0;
		}
		throw new ArgumentException(SR.Arg_MustBeUIntPtr);
	}

	public unsafe int CompareTo(UIntPtr value)
	{
		return ((ulong)_value).CompareTo((ulong)value);
	}

	[NonVersionable]
	public unsafe bool Equals(UIntPtr other)
	{
		return _value == (void*)other;
	}

	public unsafe override string ToString()
	{
		return ((ulong)_value).ToString();
	}

	public unsafe string ToString(string? format)
	{
		return ((ulong)_value).ToString(format);
	}

	public unsafe string ToString(IFormatProvider? provider)
	{
		return ((ulong)_value).ToString(provider);
	}

	public unsafe string ToString(string? format, IFormatProvider? provider)
	{
		return ((ulong)_value).ToString(format, provider);
	}

	public unsafe bool TryFormat(Span<char> destination, out int charsWritten, ReadOnlySpan<char> format = default(ReadOnlySpan<char>), IFormatProvider? provider = null)
	{
		return ((ulong)_value).TryFormat(destination, out charsWritten, format, provider);
	}

	public static UIntPtr Parse(string s)
	{
		return (UIntPtr)ulong.Parse(s);
	}

	public static UIntPtr Parse(string s, NumberStyles style)
	{
		return (UIntPtr)ulong.Parse(s, style);
	}

	public static UIntPtr Parse(string s, IFormatProvider? provider)
	{
		return (UIntPtr)ulong.Parse(s, provider);
	}

	public static UIntPtr Parse(string s, NumberStyles style, IFormatProvider? provider)
	{
		return (UIntPtr)ulong.Parse(s, style, provider);
	}

	public static UIntPtr Parse(ReadOnlySpan<char> s, NumberStyles style = NumberStyles.Integer, IFormatProvider? provider = null)
	{
		return (UIntPtr)ulong.Parse(s, style, provider);
	}

	public static bool TryParse([NotNullWhen(true)] string? s, out UIntPtr result)
	{
		Unsafe.SkipInit<UIntPtr>(out result);
		return ulong.TryParse(s, out Unsafe.As<UIntPtr, ulong>(ref result));
	}

	public static bool TryParse([NotNullWhen(true)] string? s, NumberStyles style, IFormatProvider? provider, out UIntPtr result)
	{
		Unsafe.SkipInit<UIntPtr>(out result);
		return ulong.TryParse(s, style, provider, out Unsafe.As<UIntPtr, ulong>(ref result));
	}

	public static bool TryParse(ReadOnlySpan<char> s, out UIntPtr result)
	{
		Unsafe.SkipInit<UIntPtr>(out result);
		return ulong.TryParse(s, out Unsafe.As<UIntPtr, ulong>(ref result));
	}

	public static bool TryParse(ReadOnlySpan<char> s, NumberStyles style, IFormatProvider? provider, out UIntPtr result)
	{
		Unsafe.SkipInit<UIntPtr>(out result);
		return ulong.TryParse(s, style, provider, out Unsafe.As<UIntPtr, ulong>(ref result));
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static nuint IAdditionOperators<UIntPtr, UIntPtr, UIntPtr>.operator +(nuint left, nuint right)
	{
		return left + right;
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static nuint IBinaryInteger<UIntPtr>.LeadingZeroCount(nuint value)
	{
		_ = Environment.Is64BitProcess;
		return (nuint)BitOperations.LeadingZeroCount(value);
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static nuint IBinaryInteger<UIntPtr>.PopCount(nuint value)
	{
		_ = Environment.Is64BitProcess;
		return (nuint)BitOperations.PopCount(value);
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static nuint IBinaryInteger<UIntPtr>.RotateLeft(nuint value, int rotateAmount)
	{
		_ = Environment.Is64BitProcess;
		return (nuint)BitOperations.RotateLeft(value, rotateAmount);
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static nuint IBinaryInteger<UIntPtr>.RotateRight(nuint value, int rotateAmount)
	{
		_ = Environment.Is64BitProcess;
		return (nuint)BitOperations.RotateRight(value, rotateAmount);
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static nuint IBinaryInteger<UIntPtr>.TrailingZeroCount(nuint value)
	{
		_ = Environment.Is64BitProcess;
		return (nuint)BitOperations.TrailingZeroCount(value);
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static bool IBinaryNumber<UIntPtr>.IsPow2(nuint value)
	{
		_ = Environment.Is64BitProcess;
		return BitOperations.IsPow2((ulong)value);
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static nuint IBinaryNumber<UIntPtr>.Log2(nuint value)
	{
		_ = Environment.Is64BitProcess;
		return (nuint)BitOperations.Log2(value);
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static nuint IBitwiseOperators<UIntPtr, UIntPtr, UIntPtr>.operator &(nuint left, nuint right)
	{
		return left & right;
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static nuint IBitwiseOperators<UIntPtr, UIntPtr, UIntPtr>.operator |(nuint left, nuint right)
	{
		return left | right;
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static nuint IBitwiseOperators<UIntPtr, UIntPtr, UIntPtr>.operator ^(nuint left, nuint right)
	{
		return left ^ right;
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static nuint IBitwiseOperators<UIntPtr, UIntPtr, UIntPtr>.operator ~(nuint value)
	{
		return ~value;
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static bool IComparisonOperators<UIntPtr, UIntPtr>.operator <(nuint left, nuint right)
	{
		return left < right;
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static bool IComparisonOperators<UIntPtr, UIntPtr>.operator <=(nuint left, nuint right)
	{
		return left <= right;
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static bool IComparisonOperators<UIntPtr, UIntPtr>.operator >(nuint left, nuint right)
	{
		return left > right;
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static bool IComparisonOperators<UIntPtr, UIntPtr>.operator >=(nuint left, nuint right)
	{
		return left >= right;
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static nuint IDecrementOperators<UIntPtr>.operator --(nuint value)
	{
		return --value;
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static nuint IDivisionOperators<UIntPtr, UIntPtr, UIntPtr>.operator /(nuint left, nuint right)
	{
		return left / right;
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static bool IEqualityOperators<UIntPtr, UIntPtr>.operator ==(nuint left, nuint right)
	{
		return left == right;
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static bool IEqualityOperators<UIntPtr, UIntPtr>.operator !=(nuint left, nuint right)
	{
		return left != right;
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static nuint IIncrementOperators<UIntPtr>.operator ++(nuint value)
	{
		return ++value;
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static nuint IModulusOperators<UIntPtr, UIntPtr, UIntPtr>.operator %(nuint left, nuint right)
	{
		return left % right;
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static nuint IMultiplyOperators<UIntPtr, UIntPtr, UIntPtr>.operator *(nuint left, nuint right)
	{
		return left * right;
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static nuint INumber<UIntPtr>.Abs(nuint value)
	{
		return value;
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static nuint INumber<UIntPtr>.Clamp(nuint value, nuint min, nuint max)
	{
		return Math.Clamp(value, min, max);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static nuint INumber<UIntPtr>.Create<TOther>(TOther value)
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
				return (nuint)(ulong)(decimal)(object)value;
			}
			if (typeof(TOther) == typeof(double))
			{
				return (nuint)(double)(object)value;
			}
			if (typeof(TOther) == typeof(short))
			{
				return (nuint)(short)(object)value;
			}
			if (typeof(TOther) == typeof(int))
			{
				return (nuint)(int)(object)value;
			}
			if (typeof(TOther) == typeof(long))
			{
				return (nuint)(long)(object)value;
			}
			if (typeof(TOther) == typeof(IntPtr))
			{
				return (nuint)(nint)(IntPtr)(object)value;
			}
			if (typeof(TOther) == typeof(sbyte))
			{
				return (nuint)(sbyte)(object)value;
			}
			if (typeof(TOther) == typeof(float))
			{
				return (nuint)(float)(object)value;
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
				return (nuint)(ulong)(object)value;
			}
			if (typeof(TOther) == typeof(UIntPtr))
			{
				return (UIntPtr)(object)value;
			}
			ThrowHelper.ThrowNotSupportedException();
			return 0u;
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static nuint INumber<UIntPtr>.CreateSaturating<TOther>(TOther value)
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
			if (!(num > (decimal)(ulong)MaxValue))
			{
				if (!(num < 0m))
				{
					return (nuint)(ulong)num;
				}
				return MinValue;
			}
			return MaxValue;
		}
		if (typeof(TOther) == typeof(double))
		{
			double num2 = (double)(object)value;
			if (!(num2 > (double)(nint)(nuint)MaxValue))
			{
				if (!(num2 < 0.0))
				{
					return (nuint)num2;
				}
				return MinValue;
			}
			return MaxValue;
		}
		if (typeof(TOther) == typeof(short))
		{
			short num3 = (short)(object)value;
			if (num3 >= 0)
			{
				return (nuint)num3;
			}
			return MinValue;
		}
		if (typeof(TOther) == typeof(int))
		{
			int num4 = (int)(object)value;
			if (num4 >= 0)
			{
				return (nuint)num4;
			}
			return MinValue;
		}
		if (typeof(TOther) == typeof(long))
		{
			long num5 = (long)(object)value;
			if (Size == 4)
			{
			}
			if (num5 >= 0)
			{
				return (nuint)num5;
			}
			return MinValue;
		}
		if (typeof(TOther) == typeof(IntPtr))
		{
			IntPtr intPtr = (IntPtr)(object)value;
			if ((nint)intPtr >= 0)
			{
				return (nuint)(nint)intPtr;
			}
			return MinValue;
		}
		if (typeof(TOther) == typeof(sbyte))
		{
			sbyte b = (sbyte)(object)value;
			if (b >= 0)
			{
				return (nuint)b;
			}
			return MinValue;
		}
		if (typeof(TOther) == typeof(float))
		{
			float num6 = (float)(object)value;
			if (!(num6 > (float)(nint)(nuint)MaxValue))
			{
				if (!(num6 < 0f))
				{
					return (nuint)num6;
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
			return (uint)(object)value;
		}
		if (typeof(TOther) == typeof(ulong))
		{
			ulong num7 = (ulong)(object)value;
			if (num7 <= (nuint)MaxValue)
			{
				return (nuint)num7;
			}
			return MaxValue;
		}
		if (typeof(TOther) == typeof(UIntPtr))
		{
			return (UIntPtr)(object)value;
		}
		ThrowHelper.ThrowNotSupportedException();
		return 0u;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static nuint INumber<UIntPtr>.CreateTruncating<TOther>(TOther value)
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
			return (nuint)(ulong)(decimal)(object)value;
		}
		if (typeof(TOther) == typeof(double))
		{
			return (nuint)(double)(object)value;
		}
		if (typeof(TOther) == typeof(short))
		{
			return (nuint)(short)(object)value;
		}
		if (typeof(TOther) == typeof(int))
		{
			return (nuint)(int)(object)value;
		}
		if (typeof(TOther) == typeof(long))
		{
			return (nuint)(long)(object)value;
		}
		if (typeof(TOther) == typeof(IntPtr))
		{
			return (nuint)(nint)(IntPtr)(object)value;
		}
		if (typeof(TOther) == typeof(sbyte))
		{
			return (nuint)(sbyte)(object)value;
		}
		if (typeof(TOther) == typeof(float))
		{
			return (nuint)(float)(object)value;
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
			return (nuint)(ulong)(object)value;
		}
		if (typeof(TOther) == typeof(UIntPtr))
		{
			return (UIntPtr)(object)value;
		}
		ThrowHelper.ThrowNotSupportedException();
		return 0u;
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static (nuint Quotient, nuint Remainder) INumber<UIntPtr>.DivRem(nuint left, nuint right)
	{
		return Math.DivRem(left, right);
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static nuint INumber<UIntPtr>.Max(nuint x, nuint y)
	{
		return Math.Max(x, y);
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static nuint INumber<UIntPtr>.Min(nuint x, nuint y)
	{
		return Math.Min(x, y);
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static nuint INumber<UIntPtr>.Parse(string s, NumberStyles style, IFormatProvider provider)
	{
		return Parse(s, style, provider);
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static nuint INumber<UIntPtr>.Parse(ReadOnlySpan<char> s, NumberStyles style, IFormatProvider provider)
	{
		return Parse(s, style, provider);
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static nuint INumber<UIntPtr>.Sign(nuint value)
	{
		return (nuint)(int)((value != 0) ? 1u : 0u);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static bool INumber<UIntPtr>.TryCreate<TOther>(TOther value, out nuint result)
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
			if (num < 0m || num > (decimal)(ulong)MaxValue)
			{
				result = 0u;
				return false;
			}
			result = (nuint)(ulong)num;
			return true;
		}
		if (typeof(TOther) == typeof(double))
		{
			double num2 = (double)(object)value;
			if (num2 < 0.0 || num2 > (double)(nint)(nuint)MaxValue)
			{
				result = 0u;
				return false;
			}
			result = (nuint)num2;
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
			result = (nuint)num3;
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
			result = (nuint)num4;
			return true;
		}
		if (typeof(TOther) == typeof(long))
		{
			long num5 = (long)(object)value;
			if (num5 < 0 || Size == 4)
			{
				result = 0u;
				return false;
			}
			result = (nuint)num5;
			return true;
		}
		if (typeof(TOther) == typeof(IntPtr))
		{
			IntPtr intPtr = (IntPtr)(object)value;
			if ((nint)intPtr < 0)
			{
				result = 0u;
				return false;
			}
			result = (nuint)(nint)intPtr;
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
			result = (nuint)b;
			return true;
		}
		if (typeof(TOther) == typeof(float))
		{
			float num6 = (float)(object)value;
			if (num6 < 0f || num6 > (float)(nint)(nuint)MaxValue)
			{
				result = 0u;
				return false;
			}
			result = (nuint)num6;
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
			if (num7 > (nuint)MaxValue)
			{
				result = 0u;
				return false;
			}
			result = (nuint)num7;
			return true;
		}
		if (typeof(TOther) == typeof(UIntPtr))
		{
			result = (UIntPtr)(object)value;
			return true;
		}
		ThrowHelper.ThrowNotSupportedException();
		result = 0u;
		return false;
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static bool INumber<UIntPtr>.TryParse([NotNullWhen(true)] string s, NumberStyles style, IFormatProvider provider, out nuint result)
	{
		return TryParse(s, style, provider, out result);
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static bool INumber<UIntPtr>.TryParse(ReadOnlySpan<char> s, NumberStyles style, IFormatProvider provider, out nuint result)
	{
		return TryParse(s, style, provider, out result);
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static nuint IParseable<UIntPtr>.Parse(string s, IFormatProvider provider)
	{
		return Parse(s, provider);
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static bool IParseable<UIntPtr>.TryParse([NotNullWhen(true)] string s, IFormatProvider provider, out nuint result)
	{
		return TryParse(s, NumberStyles.Integer, provider, out result);
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static nuint IShiftOperators<UIntPtr, UIntPtr>.operator <<(nuint value, int shiftAmount)
	{
		return value << shiftAmount;
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static nuint IShiftOperators<UIntPtr, UIntPtr>.operator >>(nuint value, int shiftAmount)
	{
		return value >> shiftAmount;
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static nuint ISpanParseable<UIntPtr>.Parse(ReadOnlySpan<char> s, IFormatProvider provider)
	{
		return Parse(s, NumberStyles.Integer, provider);
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static bool ISpanParseable<UIntPtr>.TryParse(ReadOnlySpan<char> s, IFormatProvider provider, out nuint result)
	{
		return TryParse(s, NumberStyles.Integer, provider, out result);
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static nuint ISubtractionOperators<UIntPtr, UIntPtr, UIntPtr>.operator -(nuint left, nuint right)
	{
		return left - right;
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static nuint IUnaryNegationOperators<UIntPtr, UIntPtr>.operator -(nuint value)
	{
		return 0 - value;
	}

	[RequiresPreviewFeatures("Generic Math is in preview.", Url = "https://aka.ms/dotnet-warnings/generic-math-preview")]
	static nuint IUnaryPlusOperators<UIntPtr, UIntPtr>.operator +(nuint value)
	{
		return value;
	}
}
