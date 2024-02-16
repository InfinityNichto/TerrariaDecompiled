using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.Arm;
using System.Runtime.Intrinsics.X86;
using System.Runtime.Versioning;

namespace System;

public static class Math
{
	public const double E = 2.718281828459045;

	public const double PI = 3.141592653589793;

	public const double Tau = Math.PI * 2.0;

	private static readonly double[] roundPower10Double = new double[16]
	{
		1.0, 10.0, 100.0, 1000.0, 10000.0, 100000.0, 1000000.0, 10000000.0, 100000000.0, 1000000000.0,
		10000000000.0, 100000000000.0, 1000000000000.0, 10000000000000.0, 100000000000000.0, 1000000000000000.0
	};

	[MethodImpl(MethodImplOptions.InternalCall)]
	[Intrinsic]
	public static extern double Abs(double value);

	[MethodImpl(MethodImplOptions.InternalCall)]
	[Intrinsic]
	public static extern float Abs(float value);

	[MethodImpl(MethodImplOptions.InternalCall)]
	[Intrinsic]
	public static extern double Acos(double d);

	[MethodImpl(MethodImplOptions.InternalCall)]
	[Intrinsic]
	public static extern double Acosh(double d);

	[MethodImpl(MethodImplOptions.InternalCall)]
	[Intrinsic]
	public static extern double Asin(double d);

	[MethodImpl(MethodImplOptions.InternalCall)]
	[Intrinsic]
	public static extern double Asinh(double d);

	[MethodImpl(MethodImplOptions.InternalCall)]
	[Intrinsic]
	public static extern double Atan(double d);

	[MethodImpl(MethodImplOptions.InternalCall)]
	[Intrinsic]
	public static extern double Atanh(double d);

	[MethodImpl(MethodImplOptions.InternalCall)]
	[Intrinsic]
	public static extern double Atan2(double y, double x);

	[MethodImpl(MethodImplOptions.InternalCall)]
	[Intrinsic]
	public static extern double Cbrt(double d);

	[MethodImpl(MethodImplOptions.InternalCall)]
	[Intrinsic]
	public static extern double Ceiling(double a);

	[MethodImpl(MethodImplOptions.InternalCall)]
	[Intrinsic]
	public static extern double Cos(double d);

	[MethodImpl(MethodImplOptions.InternalCall)]
	[Intrinsic]
	public static extern double Cosh(double value);

	[MethodImpl(MethodImplOptions.InternalCall)]
	[Intrinsic]
	public static extern double Exp(double d);

	[MethodImpl(MethodImplOptions.InternalCall)]
	[Intrinsic]
	public static extern double Floor(double d);

	[MethodImpl(MethodImplOptions.InternalCall)]
	[Intrinsic]
	public static extern double FusedMultiplyAdd(double x, double y, double z);

	[MethodImpl(MethodImplOptions.InternalCall)]
	[Intrinsic]
	public static extern int ILogB(double x);

	[MethodImpl(MethodImplOptions.InternalCall)]
	[Intrinsic]
	public static extern double Log(double d);

	[MethodImpl(MethodImplOptions.InternalCall)]
	[Intrinsic]
	public static extern double Log2(double x);

	[MethodImpl(MethodImplOptions.InternalCall)]
	[Intrinsic]
	public static extern double Log10(double d);

	[MethodImpl(MethodImplOptions.InternalCall)]
	[Intrinsic]
	public static extern double Pow(double x, double y);

	[MethodImpl(MethodImplOptions.InternalCall)]
	[Intrinsic]
	public static extern double Sin(double a);

	public unsafe static (double Sin, double Cos) SinCos(double x)
	{
		System.Runtime.CompilerServices.Unsafe.SkipInit(out double item);
		System.Runtime.CompilerServices.Unsafe.SkipInit(out double item2);
		SinCos(x, &item, &item2);
		return (Sin: item, Cos: item2);
	}

	[MethodImpl(MethodImplOptions.InternalCall)]
	[Intrinsic]
	public static extern double Sinh(double value);

	[MethodImpl(MethodImplOptions.InternalCall)]
	[Intrinsic]
	public static extern double Sqrt(double d);

	[MethodImpl(MethodImplOptions.InternalCall)]
	[Intrinsic]
	public static extern double Tan(double a);

	[MethodImpl(MethodImplOptions.InternalCall)]
	[Intrinsic]
	public static extern double Tanh(double value);

	[MethodImpl(MethodImplOptions.InternalCall)]
	private unsafe static extern double ModF(double x, double* intptr);

	[MethodImpl(MethodImplOptions.InternalCall)]
	private unsafe static extern void SinCos(double x, double* sin, double* cos);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static short Abs(short value)
	{
		if (value < 0)
		{
			value = (short)(-value);
			if (value < 0)
			{
				ThrowAbsOverflow();
			}
		}
		return value;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static int Abs(int value)
	{
		if (value < 0)
		{
			value = -value;
			if (value < 0)
			{
				ThrowAbsOverflow();
			}
		}
		return value;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static long Abs(long value)
	{
		if (value < 0)
		{
			value = -value;
			if (value < 0)
			{
				ThrowAbsOverflow();
			}
		}
		return value;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static nint Abs(nint value)
	{
		if (value < 0)
		{
			value = -value;
			if (value < 0)
			{
				ThrowAbsOverflow();
			}
		}
		return value;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[CLSCompliant(false)]
	public static sbyte Abs(sbyte value)
	{
		if (value < 0)
		{
			value = (sbyte)(-value);
			if (value < 0)
			{
				ThrowAbsOverflow();
			}
		}
		return value;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static decimal Abs(decimal value)
	{
		return decimal.Abs(in value);
	}

	[DoesNotReturn]
	[StackTraceHidden]
	private static void ThrowAbsOverflow()
	{
		throw new OverflowException(SR.Overflow_NegateTwosCompNum);
	}

	public static long BigMul(int a, int b)
	{
		return (long)a * (long)b;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[CLSCompliant(false)]
	public unsafe static ulong BigMul(ulong a, ulong b, out ulong low)
	{
		if (Bmi2.X64.IsSupported)
		{
			System.Runtime.CompilerServices.Unsafe.SkipInit(out ulong num);
			ulong result = Bmi2.X64.MultiplyNoFlags(a, b, &num);
			low = num;
			return result;
		}
		if (ArmBase.Arm64.IsSupported)
		{
		}
		return SoftwareFallback(a, b, out low);
		static ulong SoftwareFallback(ulong a, ulong b, out ulong low)
		{
			uint num2 = (uint)a;
			uint num3 = (uint)(a >> 32);
			uint num4 = (uint)b;
			uint num5 = (uint)(b >> 32);
			ulong num6 = (ulong)num2 * (ulong)num4;
			ulong num7 = (ulong)((long)num3 * (long)num4) + (num6 >> 32);
			ulong num8 = (ulong)((long)num2 * (long)num5 + (uint)num7);
			low = (num8 << 32) | (uint)num6;
			return (ulong)((long)num3 * (long)num5 + (long)(num7 >> 32)) + (num8 >> 32);
		}
	}

	public static long BigMul(long a, long b, out long low)
	{
		if (ArmBase.Arm64.IsSupported)
		{
		}
		ulong low2;
		ulong num = BigMul((ulong)a, (ulong)b, out low2);
		low = (long)low2;
		return (long)num - ((a >> 63) & b) - ((b >> 63) & a);
	}

	public static double BitDecrement(double x)
	{
		long num = BitConverter.DoubleToInt64Bits(x);
		if (((num >> 32) & 0x7FF00000) >= 2146435072)
		{
			if (num != 9218868437227405312L)
			{
				return x;
			}
			return double.MaxValue;
		}
		if (num == 0L)
		{
			return -5E-324;
		}
		num += ((num < 0) ? 1 : (-1));
		return BitConverter.Int64BitsToDouble(num);
	}

	public static double BitIncrement(double x)
	{
		long num = BitConverter.DoubleToInt64Bits(x);
		if (((num >> 32) & 0x7FF00000) >= 2146435072)
		{
			if (num != -4503599627370496L)
			{
				return x;
			}
			return double.MinValue;
		}
		if (num == long.MinValue)
		{
			return double.Epsilon;
		}
		num += ((num >= 0) ? 1 : (-1));
		return BitConverter.Int64BitsToDouble(num);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static double CopySign(double x, double y)
	{
		if (Sse2.IsSupported || AdvSimd.IsSupported)
		{
			return VectorMath.ConditionalSelectBitwise(Vector128.CreateScalarUnsafe(-0.0), Vector128.CreateScalarUnsafe(y), Vector128.CreateScalarUnsafe(x)).ToScalar();
		}
		return SoftwareFallback(x, y);
		static double SoftwareFallback(double x, double y)
		{
			long num = BitConverter.DoubleToInt64Bits(x);
			long num2 = BitConverter.DoubleToInt64Bits(y);
			num &= 0x7FFFFFFFFFFFFFFFL;
			num2 &= long.MinValue;
			return BitConverter.Int64BitsToDouble(num | num2);
		}
	}

	public static int DivRem(int a, int b, out int result)
	{
		int num = a / b;
		result = a - num * b;
		return num;
	}

	public static long DivRem(long a, long b, out long result)
	{
		long num = a / b;
		result = a - num * b;
		return num;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[NonVersionable]
	[CLSCompliant(false)]
	public static (sbyte Quotient, sbyte Remainder) DivRem(sbyte left, sbyte right)
	{
		sbyte b = (sbyte)(left / right);
		return (Quotient: b, Remainder: (sbyte)(left - b * right));
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[NonVersionable]
	public static (byte Quotient, byte Remainder) DivRem(byte left, byte right)
	{
		byte b = (byte)(left / right);
		return (Quotient: b, Remainder: (byte)(left - b * right));
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[NonVersionable]
	public static (short Quotient, short Remainder) DivRem(short left, short right)
	{
		short num = (short)(left / right);
		return (Quotient: num, Remainder: (short)(left - num * right));
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[NonVersionable]
	[CLSCompliant(false)]
	public static (ushort Quotient, ushort Remainder) DivRem(ushort left, ushort right)
	{
		ushort num = (ushort)(left / right);
		return (Quotient: num, Remainder: (ushort)(left - num * right));
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[NonVersionable]
	public static (int Quotient, int Remainder) DivRem(int left, int right)
	{
		int num = left / right;
		return (Quotient: num, Remainder: left - num * right);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[NonVersionable]
	[CLSCompliant(false)]
	public static (uint Quotient, uint Remainder) DivRem(uint left, uint right)
	{
		uint num = left / right;
		return (Quotient: num, Remainder: left - num * right);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[NonVersionable]
	public static (long Quotient, long Remainder) DivRem(long left, long right)
	{
		long num = left / right;
		return (Quotient: num, Remainder: left - num * right);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[NonVersionable]
	[CLSCompliant(false)]
	public static (ulong Quotient, ulong Remainder) DivRem(ulong left, ulong right)
	{
		ulong num = left / right;
		return (Quotient: num, Remainder: left - num * right);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[NonVersionable]
	public static (nint Quotient, nint Remainder) DivRem(nint left, nint right)
	{
		nint num = left / right;
		return (Quotient: num, Remainder: left - num * right);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[NonVersionable]
	[CLSCompliant(false)]
	public static (nuint Quotient, nuint Remainder) DivRem(nuint left, nuint right)
	{
		nuint num = left / right;
		return (Quotient: num, Remainder: left - num * right);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static decimal Ceiling(decimal d)
	{
		return decimal.Ceiling(d);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static byte Clamp(byte value, byte min, byte max)
	{
		if (min > max)
		{
			ThrowMinMaxException(min, max);
		}
		if (value < min)
		{
			return min;
		}
		if (value > max)
		{
			return max;
		}
		return value;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static decimal Clamp(decimal value, decimal min, decimal max)
	{
		if (min > max)
		{
			ThrowMinMaxException(min, max);
		}
		if (value < min)
		{
			return min;
		}
		if (value > max)
		{
			return max;
		}
		return value;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static double Clamp(double value, double min, double max)
	{
		if (min > max)
		{
			ThrowMinMaxException(min, max);
		}
		if (value < min)
		{
			return min;
		}
		if (value > max)
		{
			return max;
		}
		return value;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static short Clamp(short value, short min, short max)
	{
		if (min > max)
		{
			ThrowMinMaxException(min, max);
		}
		if (value < min)
		{
			return min;
		}
		if (value > max)
		{
			return max;
		}
		return value;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static int Clamp(int value, int min, int max)
	{
		if (min > max)
		{
			ThrowMinMaxException(min, max);
		}
		if (value < min)
		{
			return min;
		}
		if (value > max)
		{
			return max;
		}
		return value;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static long Clamp(long value, long min, long max)
	{
		if (min > max)
		{
			ThrowMinMaxException(min, max);
		}
		if (value < min)
		{
			return min;
		}
		if (value > max)
		{
			return max;
		}
		return value;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static nint Clamp(nint value, nint min, nint max)
	{
		if (min > max)
		{
			ThrowMinMaxException(min, max);
		}
		if (value < min)
		{
			return min;
		}
		if (value > max)
		{
			return max;
		}
		return value;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[CLSCompliant(false)]
	public static sbyte Clamp(sbyte value, sbyte min, sbyte max)
	{
		if (min > max)
		{
			ThrowMinMaxException(min, max);
		}
		if (value < min)
		{
			return min;
		}
		if (value > max)
		{
			return max;
		}
		return value;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static float Clamp(float value, float min, float max)
	{
		if (min > max)
		{
			ThrowMinMaxException(min, max);
		}
		if (value < min)
		{
			return min;
		}
		if (value > max)
		{
			return max;
		}
		return value;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[CLSCompliant(false)]
	public static ushort Clamp(ushort value, ushort min, ushort max)
	{
		if (min > max)
		{
			ThrowMinMaxException(min, max);
		}
		if (value < min)
		{
			return min;
		}
		if (value > max)
		{
			return max;
		}
		return value;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[CLSCompliant(false)]
	public static uint Clamp(uint value, uint min, uint max)
	{
		if (min > max)
		{
			ThrowMinMaxException(min, max);
		}
		if (value < min)
		{
			return min;
		}
		if (value > max)
		{
			return max;
		}
		return value;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[CLSCompliant(false)]
	public static ulong Clamp(ulong value, ulong min, ulong max)
	{
		if (min > max)
		{
			ThrowMinMaxException(min, max);
		}
		if (value < min)
		{
			return min;
		}
		if (value > max)
		{
			return max;
		}
		return value;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[CLSCompliant(false)]
	public static nuint Clamp(nuint value, nuint min, nuint max)
	{
		if (min > max)
		{
			ThrowMinMaxException(min, max);
		}
		if (value < min)
		{
			return min;
		}
		if (value > max)
		{
			return max;
		}
		return value;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static decimal Floor(decimal d)
	{
		return decimal.Floor(d);
	}

	public static double IEEERemainder(double x, double y)
	{
		if (double.IsNaN(x))
		{
			return x;
		}
		if (double.IsNaN(y))
		{
			return y;
		}
		double num = x % y;
		if (double.IsNaN(num))
		{
			return double.NaN;
		}
		if (num == 0.0 && double.IsNegative(x))
		{
			return -0.0;
		}
		double num2 = num - Abs(y) * (double)Sign(x);
		if (Abs(num2) == Abs(num))
		{
			double num3 = x / y;
			double value = Round(num3);
			if (Abs(value) > Abs(num3))
			{
				return num2;
			}
			return num;
		}
		if (Abs(num2) < Abs(num))
		{
			return num2;
		}
		return num;
	}

	public static double Log(double a, double newBase)
	{
		if (double.IsNaN(a))
		{
			return a;
		}
		if (double.IsNaN(newBase))
		{
			return newBase;
		}
		if (newBase == 1.0)
		{
			return double.NaN;
		}
		if (a != 1.0 && (newBase == 0.0 || double.IsPositiveInfinity(newBase)))
		{
			return double.NaN;
		}
		return Log(a) / Log(newBase);
	}

	[NonVersionable]
	public static byte Max(byte val1, byte val2)
	{
		if (val1 < val2)
		{
			return val2;
		}
		return val1;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static decimal Max(decimal val1, decimal val2)
	{
		return decimal.Max(in val1, in val2);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static double Max(double val1, double val2)
	{
		if (val1 != val2)
		{
			if (!double.IsNaN(val1))
			{
				if (!(val2 < val1))
				{
					return val2;
				}
				return val1;
			}
			return val1;
		}
		if (!double.IsNegative(val2))
		{
			return val2;
		}
		return val1;
	}

	[NonVersionable]
	public static short Max(short val1, short val2)
	{
		if (val1 < val2)
		{
			return val2;
		}
		return val1;
	}

	[NonVersionable]
	public static int Max(int val1, int val2)
	{
		if (val1 < val2)
		{
			return val2;
		}
		return val1;
	}

	[NonVersionable]
	public static long Max(long val1, long val2)
	{
		if (val1 < val2)
		{
			return val2;
		}
		return val1;
	}

	[NonVersionable]
	public static nint Max(nint val1, nint val2)
	{
		if (val1 < val2)
		{
			return val2;
		}
		return val1;
	}

	[CLSCompliant(false)]
	[NonVersionable]
	public static sbyte Max(sbyte val1, sbyte val2)
	{
		if (val1 < val2)
		{
			return val2;
		}
		return val1;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static float Max(float val1, float val2)
	{
		if (val1 != val2)
		{
			if (!float.IsNaN(val1))
			{
				if (!(val2 < val1))
				{
					return val2;
				}
				return val1;
			}
			return val1;
		}
		if (!float.IsNegative(val2))
		{
			return val2;
		}
		return val1;
	}

	[CLSCompliant(false)]
	[NonVersionable]
	public static ushort Max(ushort val1, ushort val2)
	{
		if (val1 < val2)
		{
			return val2;
		}
		return val1;
	}

	[CLSCompliant(false)]
	[NonVersionable]
	public static uint Max(uint val1, uint val2)
	{
		if (val1 < val2)
		{
			return val2;
		}
		return val1;
	}

	[CLSCompliant(false)]
	[NonVersionable]
	public static ulong Max(ulong val1, ulong val2)
	{
		if (val1 < val2)
		{
			return val2;
		}
		return val1;
	}

	[CLSCompliant(false)]
	[NonVersionable]
	public static nuint Max(nuint val1, nuint val2)
	{
		if (val1 < val2)
		{
			return val2;
		}
		return val1;
	}

	public static double MaxMagnitude(double x, double y)
	{
		double num = Abs(x);
		double num2 = Abs(y);
		if (num > num2 || double.IsNaN(num))
		{
			return x;
		}
		if (num == num2)
		{
			if (!double.IsNegative(x))
			{
				return x;
			}
			return y;
		}
		return y;
	}

	[NonVersionable]
	public static byte Min(byte val1, byte val2)
	{
		if (val1 > val2)
		{
			return val2;
		}
		return val1;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static decimal Min(decimal val1, decimal val2)
	{
		return decimal.Min(in val1, in val2);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static double Min(double val1, double val2)
	{
		if (val1 != val2 && !double.IsNaN(val1))
		{
			if (!(val1 < val2))
			{
				return val2;
			}
			return val1;
		}
		if (!double.IsNegative(val1))
		{
			return val2;
		}
		return val1;
	}

	[NonVersionable]
	public static short Min(short val1, short val2)
	{
		if (val1 > val2)
		{
			return val2;
		}
		return val1;
	}

	[NonVersionable]
	public static int Min(int val1, int val2)
	{
		if (val1 > val2)
		{
			return val2;
		}
		return val1;
	}

	[NonVersionable]
	public static long Min(long val1, long val2)
	{
		if (val1 > val2)
		{
			return val2;
		}
		return val1;
	}

	[NonVersionable]
	public static nint Min(nint val1, nint val2)
	{
		if (val1 > val2)
		{
			return val2;
		}
		return val1;
	}

	[CLSCompliant(false)]
	[NonVersionable]
	public static sbyte Min(sbyte val1, sbyte val2)
	{
		if (val1 > val2)
		{
			return val2;
		}
		return val1;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static float Min(float val1, float val2)
	{
		if (val1 != val2 && !float.IsNaN(val1))
		{
			if (!(val1 < val2))
			{
				return val2;
			}
			return val1;
		}
		if (!float.IsNegative(val1))
		{
			return val2;
		}
		return val1;
	}

	[CLSCompliant(false)]
	[NonVersionable]
	public static ushort Min(ushort val1, ushort val2)
	{
		if (val1 > val2)
		{
			return val2;
		}
		return val1;
	}

	[CLSCompliant(false)]
	[NonVersionable]
	public static uint Min(uint val1, uint val2)
	{
		if (val1 > val2)
		{
			return val2;
		}
		return val1;
	}

	[CLSCompliant(false)]
	[NonVersionable]
	public static ulong Min(ulong val1, ulong val2)
	{
		if (val1 > val2)
		{
			return val2;
		}
		return val1;
	}

	[CLSCompliant(false)]
	[NonVersionable]
	public static nuint Min(nuint val1, nuint val2)
	{
		if (val1 > val2)
		{
			return val2;
		}
		return val1;
	}

	public static double MinMagnitude(double x, double y)
	{
		double num = Abs(x);
		double num2 = Abs(y);
		if (num < num2 || double.IsNaN(num))
		{
			return x;
		}
		if (num == num2)
		{
			if (!double.IsNegative(x))
			{
				return y;
			}
			return x;
		}
		return y;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static double ReciprocalEstimate(double d)
	{
		if (AdvSimd.Arm64.IsSupported)
		{
		}
		return 1.0 / d;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static double ReciprocalSqrtEstimate(double d)
	{
		if (AdvSimd.Arm64.IsSupported)
		{
		}
		return 1.0 / Sqrt(d);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static decimal Round(decimal d)
	{
		return decimal.Round(d, 0);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static decimal Round(decimal d, int decimals)
	{
		return decimal.Round(d, decimals);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static decimal Round(decimal d, MidpointRounding mode)
	{
		return decimal.Round(d, 0, mode);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static decimal Round(decimal d, int decimals, MidpointRounding mode)
	{
		return decimal.Round(d, decimals, mode);
	}

	[Intrinsic]
	public static double Round(double a)
	{
		ulong num = BitConverter.DoubleToUInt64Bits(a);
		int num2 = double.ExtractExponentFromBits(num);
		if (num2 <= 1022)
		{
			if (num << 1 == 0L)
			{
				return a;
			}
			double x = ((num2 == 1022 && double.ExtractSignificandFromBits(num) != 0L) ? 1.0 : 0.0);
			return CopySign(x, a);
		}
		if (num2 >= 1075)
		{
			return a;
		}
		ulong num3 = (ulong)(1L << 1075 - num2);
		ulong num4 = num3 - 1;
		num += num3 >> 1;
		num = (((num & num4) != 0L) ? (num & ~num4) : (num & ~num3));
		return BitConverter.UInt64BitsToDouble(num);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static double Round(double value, int digits)
	{
		return Round(value, digits, MidpointRounding.ToEven);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static double Round(double value, MidpointRounding mode)
	{
		return Round(value, 0, mode);
	}

	public unsafe static double Round(double value, int digits, MidpointRounding mode)
	{
		if (digits < 0 || digits > 15)
		{
			throw new ArgumentOutOfRangeException("digits", SR.ArgumentOutOfRange_RoundingDigits);
		}
		if (mode < MidpointRounding.ToEven || mode > MidpointRounding.ToPositiveInfinity)
		{
			throw new ArgumentException(SR.Format(SR.Argument_InvalidEnumValue, mode, "MidpointRounding"), "mode");
		}
		if (Abs(value) < 10000000000000000.0)
		{
			double num = roundPower10Double[digits];
			value *= num;
			switch (mode)
			{
			case MidpointRounding.ToEven:
				value = Round(value);
				break;
			case MidpointRounding.AwayFromZero:
			{
				double value2 = ModF(value, &value);
				if (Abs(value2) >= 0.5)
				{
					value += (double)Sign(value2);
				}
				break;
			}
			case MidpointRounding.ToZero:
				value = Truncate(value);
				break;
			case MidpointRounding.ToNegativeInfinity:
				value = Floor(value);
				break;
			case MidpointRounding.ToPositiveInfinity:
				value = Ceiling(value);
				break;
			default:
				throw new ArgumentException(SR.Format(SR.Argument_InvalidEnumValue, mode, "MidpointRounding"), "mode");
			}
			value /= num;
		}
		return value;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static int Sign(decimal value)
	{
		return decimal.Sign(in value);
	}

	public static int Sign(double value)
	{
		if (value < 0.0)
		{
			return -1;
		}
		if (value > 0.0)
		{
			return 1;
		}
		if (value == 0.0)
		{
			return 0;
		}
		throw new ArithmeticException(SR.Arithmetic_NaN);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static int Sign(short value)
	{
		return Sign((int)value);
	}

	public static int Sign(int value)
	{
		return (value >> 31) | (-value >>> 31);
	}

	public static int Sign(long value)
	{
		return (int)((value >> 63) | (-value >>> 63));
	}

	public static int Sign(nint value)
	{
		return (int)((long)(value >> 63) | (long)((ulong)(-value) >> 63));
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[CLSCompliant(false)]
	public static int Sign(sbyte value)
	{
		return Sign((int)value);
	}

	public static int Sign(float value)
	{
		if (value < 0f)
		{
			return -1;
		}
		if (value > 0f)
		{
			return 1;
		}
		if (value == 0f)
		{
			return 0;
		}
		throw new ArithmeticException(SR.Arithmetic_NaN);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static decimal Truncate(decimal d)
	{
		return decimal.Truncate(d);
	}

	public unsafe static double Truncate(double d)
	{
		ModF(d, &d);
		return d;
	}

	[DoesNotReturn]
	private static void ThrowMinMaxException<T>(T min, T max)
	{
		throw new ArgumentException(SR.Format(SR.Argument_MinMaxValue, min, max));
	}

	public static double ScaleB(double x, int n)
	{
		double num = x;
		if (n > 1023)
		{
			num *= 8.98846567431158E+307;
			n -= 1023;
			if (n > 1023)
			{
				num *= 8.98846567431158E+307;
				n -= 1023;
				if (n > 1023)
				{
					n = 1023;
				}
			}
		}
		else if (n < -1022)
		{
			num *= 2.004168360008973E-292;
			n += 969;
			if (n < -1022)
			{
				num *= 2.004168360008973E-292;
				n += 969;
				if (n < -1022)
				{
					n = -1022;
				}
			}
		}
		double num2 = BitConverter.Int64BitsToDouble((long)(1023 + n) << 52);
		return num * num2;
	}
}
