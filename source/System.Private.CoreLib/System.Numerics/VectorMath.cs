using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.Arm;
using System.Runtime.Intrinsics.X86;

namespace System.Numerics;

internal static class VectorMath
{
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Vector128<float> ConditionalSelectBitwise(Vector128<float> selector, Vector128<float> ifTrue, Vector128<float> ifFalse)
	{
		if (AdvSimd.IsSupported)
		{
		}
		if (Sse.IsSupported)
		{
			return Sse.Or(Sse.And(ifTrue, selector), Sse.AndNot(selector, ifFalse));
		}
		throw new PlatformNotSupportedException();
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Vector128<double> ConditionalSelectBitwise(Vector128<double> selector, Vector128<double> ifTrue, Vector128<double> ifFalse)
	{
		if (AdvSimd.IsSupported)
		{
		}
		if (Sse2.IsSupported)
		{
			return Sse2.Or(Sse2.And(ifTrue, selector), Sse2.AndNot(selector, ifFalse));
		}
		throw new PlatformNotSupportedException();
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool Equal(Vector128<float> vector1, Vector128<float> vector2)
	{
		if (AdvSimd.Arm64.IsSupported)
		{
		}
		if (Sse.IsSupported)
		{
			return Sse.MoveMask(Sse.CompareNotEqual(vector1, vector2)) == 0;
		}
		throw new PlatformNotSupportedException();
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Vector128<float> Lerp(Vector128<float> a, Vector128<float> b, Vector128<float> t)
	{
		if (AdvSimd.IsSupported)
		{
		}
		if (Fma.IsSupported)
		{
			return Fma.MultiplyAdd(Sse.Subtract(b, a), t, a);
		}
		if (Sse.IsSupported)
		{
			return Sse.Add(Sse.Multiply(a, Sse.Subtract(Vector128.Create(1f), t)), Sse.Multiply(b, t));
		}
		throw new PlatformNotSupportedException();
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool NotEqual(Vector128<float> vector1, Vector128<float> vector2)
	{
		if (AdvSimd.IsSupported)
		{
		}
		if (Sse.IsSupported)
		{
			return Sse.MoveMask(Sse.CompareNotEqual(vector1, vector2)) != 0;
		}
		throw new PlatformNotSupportedException();
	}
}
