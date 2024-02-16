using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.Arm;
using System.Runtime.Intrinsics.X86;

namespace System;

public static class MathF
{
	public const float E = 2.7182817f;

	public const float PI = 3.1415927f;

	public const float Tau = (float)Math.PI * 2f;

	private static readonly float[] roundPower10Single = new float[7] { 1f, 10f, 100f, 1000f, 10000f, 100000f, 1000000f };

	[MethodImpl(MethodImplOptions.InternalCall)]
	[Intrinsic]
	public static extern float Acos(float x);

	[MethodImpl(MethodImplOptions.InternalCall)]
	[Intrinsic]
	public static extern float Acosh(float x);

	[MethodImpl(MethodImplOptions.InternalCall)]
	[Intrinsic]
	public static extern float Asin(float x);

	[MethodImpl(MethodImplOptions.InternalCall)]
	[Intrinsic]
	public static extern float Asinh(float x);

	[MethodImpl(MethodImplOptions.InternalCall)]
	[Intrinsic]
	public static extern float Atan(float x);

	[MethodImpl(MethodImplOptions.InternalCall)]
	[Intrinsic]
	public static extern float Atanh(float x);

	[MethodImpl(MethodImplOptions.InternalCall)]
	[Intrinsic]
	public static extern float Atan2(float y, float x);

	[MethodImpl(MethodImplOptions.InternalCall)]
	[Intrinsic]
	public static extern float Cbrt(float x);

	[MethodImpl(MethodImplOptions.InternalCall)]
	[Intrinsic]
	public static extern float Ceiling(float x);

	[MethodImpl(MethodImplOptions.InternalCall)]
	[Intrinsic]
	public static extern float Cos(float x);

	[MethodImpl(MethodImplOptions.InternalCall)]
	[Intrinsic]
	public static extern float Cosh(float x);

	[MethodImpl(MethodImplOptions.InternalCall)]
	[Intrinsic]
	public static extern float Exp(float x);

	[MethodImpl(MethodImplOptions.InternalCall)]
	[Intrinsic]
	public static extern float Floor(float x);

	[MethodImpl(MethodImplOptions.InternalCall)]
	[Intrinsic]
	public static extern float FusedMultiplyAdd(float x, float y, float z);

	[MethodImpl(MethodImplOptions.InternalCall)]
	[Intrinsic]
	public static extern int ILogB(float x);

	[MethodImpl(MethodImplOptions.InternalCall)]
	[Intrinsic]
	public static extern float Log(float x);

	[MethodImpl(MethodImplOptions.InternalCall)]
	[Intrinsic]
	public static extern float Log2(float x);

	[MethodImpl(MethodImplOptions.InternalCall)]
	[Intrinsic]
	public static extern float Log10(float x);

	[MethodImpl(MethodImplOptions.InternalCall)]
	[Intrinsic]
	public static extern float Pow(float x, float y);

	[MethodImpl(MethodImplOptions.InternalCall)]
	[Intrinsic]
	public static extern float Sin(float x);

	public unsafe static (float Sin, float Cos) SinCos(float x)
	{
		System.Runtime.CompilerServices.Unsafe.SkipInit(out float item);
		System.Runtime.CompilerServices.Unsafe.SkipInit(out float item2);
		SinCos(x, &item, &item2);
		return (Sin: item, Cos: item2);
	}

	[MethodImpl(MethodImplOptions.InternalCall)]
	[Intrinsic]
	public static extern float Sinh(float x);

	[MethodImpl(MethodImplOptions.InternalCall)]
	[Intrinsic]
	public static extern float Sqrt(float x);

	[MethodImpl(MethodImplOptions.InternalCall)]
	[Intrinsic]
	public static extern float Tan(float x);

	[MethodImpl(MethodImplOptions.InternalCall)]
	[Intrinsic]
	public static extern float Tanh(float x);

	[MethodImpl(MethodImplOptions.InternalCall)]
	private unsafe static extern float ModF(float x, float* intptr);

	[MethodImpl(MethodImplOptions.InternalCall)]
	private unsafe static extern void SinCos(float x, float* sin, float* cos);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static float Abs(float x)
	{
		return Math.Abs(x);
	}

	public static float BitDecrement(float x)
	{
		int num = BitConverter.SingleToInt32Bits(x);
		if ((num & 0x7F800000) >= 2139095040)
		{
			if (num != 2139095040)
			{
				return x;
			}
			return float.MaxValue;
		}
		if (num == 0)
		{
			return -1E-45f;
		}
		num += ((num < 0) ? 1 : (-1));
		return BitConverter.Int32BitsToSingle(num);
	}

	public static float BitIncrement(float x)
	{
		int num = BitConverter.SingleToInt32Bits(x);
		if ((num & 0x7F800000) >= 2139095040)
		{
			if (num != -8388608)
			{
				return x;
			}
			return float.MinValue;
		}
		if (num == int.MinValue)
		{
			return float.Epsilon;
		}
		num += ((num >= 0) ? 1 : (-1));
		return BitConverter.Int32BitsToSingle(num);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static float CopySign(float x, float y)
	{
		if (Sse.IsSupported || AdvSimd.IsSupported)
		{
			return VectorMath.ConditionalSelectBitwise(Vector128.CreateScalarUnsafe(-0f), Vector128.CreateScalarUnsafe(y), Vector128.CreateScalarUnsafe(x)).ToScalar();
		}
		return SoftwareFallback(x, y);
		static float SoftwareFallback(float x, float y)
		{
			int num = BitConverter.SingleToInt32Bits(x);
			int num2 = BitConverter.SingleToInt32Bits(y);
			num &= 0x7FFFFFFF;
			num2 &= int.MinValue;
			return BitConverter.Int32BitsToSingle(num | num2);
		}
	}

	public static float IEEERemainder(float x, float y)
	{
		if (float.IsNaN(x))
		{
			return x;
		}
		if (float.IsNaN(y))
		{
			return y;
		}
		float num = x % y;
		if (float.IsNaN(num))
		{
			return float.NaN;
		}
		if (num == 0f && float.IsNegative(x))
		{
			return -0f;
		}
		float num2 = num - Abs(y) * (float)Sign(x);
		if (Abs(num2) == Abs(num))
		{
			float x2 = x / y;
			float x3 = Round(x2);
			if (Abs(x3) > Abs(x2))
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

	public static float Log(float x, float y)
	{
		if (float.IsNaN(x))
		{
			return x;
		}
		if (float.IsNaN(y))
		{
			return y;
		}
		if (y == 1f)
		{
			return float.NaN;
		}
		if (x != 1f && (y == 0f || float.IsPositiveInfinity(y)))
		{
			return float.NaN;
		}
		return Log(x) / Log(y);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static float Max(float x, float y)
	{
		return Math.Max(x, y);
	}

	public static float MaxMagnitude(float x, float y)
	{
		float num = Abs(x);
		float num2 = Abs(y);
		if (num > num2 || float.IsNaN(num))
		{
			return x;
		}
		if (num == num2)
		{
			if (!float.IsNegative(x))
			{
				return x;
			}
			return y;
		}
		return y;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static float Min(float x, float y)
	{
		return Math.Min(x, y);
	}

	public static float MinMagnitude(float x, float y)
	{
		float num = Abs(x);
		float num2 = Abs(y);
		if (num < num2 || float.IsNaN(num))
		{
			return x;
		}
		if (num == num2)
		{
			if (!float.IsNegative(x))
			{
				return y;
			}
			return x;
		}
		return y;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static float ReciprocalEstimate(float x)
	{
		if (Sse.IsSupported)
		{
			return Sse.ReciprocalScalar(Vector128.CreateScalarUnsafe(x)).ToScalar();
		}
		if (AdvSimd.Arm64.IsSupported)
		{
		}
		return 1f / x;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static float ReciprocalSqrtEstimate(float x)
	{
		if (Sse.IsSupported)
		{
			return Sse.ReciprocalSqrtScalar(Vector128.CreateScalarUnsafe(x)).ToScalar();
		}
		if (AdvSimd.Arm64.IsSupported)
		{
		}
		return 1f / Sqrt(x);
	}

	[Intrinsic]
	public static float Round(float x)
	{
		uint num = BitConverter.SingleToUInt32Bits(x);
		int num2 = float.ExtractExponentFromBits(num);
		if (num2 <= 126)
		{
			if (num << 1 == 0)
			{
				return x;
			}
			float x2 = ((num2 == 126 && float.ExtractSignificandFromBits(num) != 0) ? 1f : 0f);
			return CopySign(x2, x);
		}
		if (num2 >= 150)
		{
			return x;
		}
		uint num3 = (uint)(1 << 150 - num2);
		uint num4 = num3 - 1;
		num += num3 >> 1;
		num = (((num & num4) != 0) ? (num & ~num4) : (num & ~num3));
		return BitConverter.UInt32BitsToSingle(num);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static float Round(float x, int digits)
	{
		return Round(x, digits, MidpointRounding.ToEven);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static float Round(float x, MidpointRounding mode)
	{
		return Round(x, 0, mode);
	}

	public unsafe static float Round(float x, int digits, MidpointRounding mode)
	{
		if (digits < 0 || digits > 6)
		{
			throw new ArgumentOutOfRangeException("digits", SR.ArgumentOutOfRange_RoundingDigits_MathF);
		}
		if (mode < MidpointRounding.ToEven || mode > MidpointRounding.ToPositiveInfinity)
		{
			throw new ArgumentException(SR.Format(SR.Argument_InvalidEnumValue, mode, "MidpointRounding"), "mode");
		}
		if (Abs(x) < 100000000f)
		{
			float num = roundPower10Single[digits];
			x *= num;
			switch (mode)
			{
			case MidpointRounding.ToEven:
				x = Round(x);
				break;
			case MidpointRounding.AwayFromZero:
			{
				float x2 = ModF(x, &x);
				if (Abs(x2) >= 0.5f)
				{
					x += (float)Sign(x2);
				}
				break;
			}
			case MidpointRounding.ToZero:
				x = Truncate(x);
				break;
			case MidpointRounding.ToNegativeInfinity:
				x = Floor(x);
				break;
			case MidpointRounding.ToPositiveInfinity:
				x = Ceiling(x);
				break;
			default:
				throw new ArgumentException(SR.Format(SR.Argument_InvalidEnumValue, mode, "MidpointRounding"), "mode");
			}
			x /= num;
		}
		return x;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static int Sign(float x)
	{
		return Math.Sign(x);
	}

	public unsafe static float Truncate(float x)
	{
		ModF(x, &x);
		return x;
	}

	public static float ScaleB(float x, int n)
	{
		float num = x;
		if (n > 127)
		{
			num *= 1.7014118E+38f;
			n -= 127;
			if (n > 127)
			{
				num *= 1.7014118E+38f;
				n -= 127;
				if (n > 127)
				{
					n = 127;
				}
			}
		}
		else if (n < -126)
		{
			num *= 1.9721523E-31f;
			n += 102;
			if (n < -126)
			{
				num *= 1.9721523E-31f;
				n += 102;
				if (n < -126)
				{
					n = -126;
				}
			}
		}
		float num2 = BitConverter.Int32BitsToSingle(127 + n << 23);
		return num * num2;
	}
}
