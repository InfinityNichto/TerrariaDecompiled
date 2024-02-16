using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics.X86;
using System.Text;
using Internal.Runtime.CompilerServices;

namespace System.Runtime.Intrinsics;

public static class Vector256
{
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector256<U> As<T, U>(this Vector256<T> vector) where T : struct where U : struct
	{
		ThrowHelper.ThrowForUnsupportedIntrinsicsVectorBaseType<T>();
		ThrowHelper.ThrowForUnsupportedIntrinsicsVectorBaseType<U>();
		return Unsafe.As<Vector256<T>, Vector256<U>>(ref vector);
	}

	[Intrinsic]
	public static Vector256<byte> AsByte<T>(this Vector256<T> vector) where T : struct
	{
		return vector.As<T, byte>();
	}

	[Intrinsic]
	public static Vector256<double> AsDouble<T>(this Vector256<T> vector) where T : struct
	{
		return vector.As<T, double>();
	}

	[Intrinsic]
	public static Vector256<short> AsInt16<T>(this Vector256<T> vector) where T : struct
	{
		return vector.As<T, short>();
	}

	[Intrinsic]
	public static Vector256<int> AsInt32<T>(this Vector256<T> vector) where T : struct
	{
		return vector.As<T, int>();
	}

	[Intrinsic]
	public static Vector256<long> AsInt64<T>(this Vector256<T> vector) where T : struct
	{
		return vector.As<T, long>();
	}

	[Intrinsic]
	[CLSCompliant(false)]
	public static Vector256<sbyte> AsSByte<T>(this Vector256<T> vector) where T : struct
	{
		return vector.As<T, sbyte>();
	}

	[Intrinsic]
	public static Vector256<float> AsSingle<T>(this Vector256<T> vector) where T : struct
	{
		return vector.As<T, float>();
	}

	[Intrinsic]
	[CLSCompliant(false)]
	public static Vector256<ushort> AsUInt16<T>(this Vector256<T> vector) where T : struct
	{
		return vector.As<T, ushort>();
	}

	[Intrinsic]
	[CLSCompliant(false)]
	public static Vector256<uint> AsUInt32<T>(this Vector256<T> vector) where T : struct
	{
		return vector.As<T, uint>();
	}

	[Intrinsic]
	[CLSCompliant(false)]
	public static Vector256<ulong> AsUInt64<T>(this Vector256<T> vector) where T : struct
	{
		return vector.As<T, ulong>();
	}

	[Intrinsic]
	public static Vector256<T> AsVector256<T>(this Vector<T> value) where T : struct
	{
		ThrowHelper.ThrowForUnsupportedIntrinsicsVectorBaseType<T>();
		Vector256<T> source = default(Vector256<T>);
		Unsafe.WriteUnaligned(ref Unsafe.As<Vector256<T>, byte>(ref source), value);
		return source;
	}

	[Intrinsic]
	public static Vector<T> AsVector<T>(this Vector256<T> value) where T : struct
	{
		ThrowHelper.ThrowForUnsupportedIntrinsicsVectorBaseType<T>();
		return Unsafe.As<Vector256<T>, Vector<T>>(ref value);
	}

	[Intrinsic]
	public static Vector256<byte> Create(byte value)
	{
		if (Avx.IsSupported)
		{
			return Create(value);
		}
		return SoftwareFallback(value);
		unsafe static Vector256<byte> SoftwareFallback(byte value)
		{
			byte* source = stackalloc byte[32]
			{
				value, value, value, value, value, value, value, value, value, value,
				value, value, value, value, value, value, value, value, value, value,
				value, value, value, value, value, value, value, value, value, value,
				value, value
			};
			return Unsafe.AsRef<Vector256<byte>>(source);
		}
	}

	[Intrinsic]
	public static Vector256<double> Create(double value)
	{
		if (Avx.IsSupported)
		{
			return Create(value);
		}
		return SoftwareFallback(value);
		unsafe static Vector256<double> SoftwareFallback(double value)
		{
			double* source = stackalloc double[4] { value, value, value, value };
			return Unsafe.AsRef<Vector256<double>>(source);
		}
	}

	[Intrinsic]
	public static Vector256<short> Create(short value)
	{
		if (Avx.IsSupported)
		{
			return Create(value);
		}
		return SoftwareFallback(value);
		unsafe static Vector256<short> SoftwareFallback(short value)
		{
			short* source = stackalloc short[16]
			{
				value, value, value, value, value, value, value, value, value, value,
				value, value, value, value, value, value
			};
			return Unsafe.AsRef<Vector256<short>>(source);
		}
	}

	[Intrinsic]
	public static Vector256<int> Create(int value)
	{
		if (Avx.IsSupported)
		{
			return Create(value);
		}
		return SoftwareFallback(value);
		unsafe static Vector256<int> SoftwareFallback(int value)
		{
			int* source = stackalloc int[8] { value, value, value, value, value, value, value, value };
			return Unsafe.AsRef<Vector256<int>>(source);
		}
	}

	[Intrinsic]
	public static Vector256<long> Create(long value)
	{
		if (Sse2.X64.IsSupported && Avx.IsSupported)
		{
			return Create(value);
		}
		return SoftwareFallback(value);
		unsafe static Vector256<long> SoftwareFallback(long value)
		{
			long* source = stackalloc long[4] { value, value, value, value };
			return Unsafe.AsRef<Vector256<long>>(source);
		}
	}

	[Intrinsic]
	[CLSCompliant(false)]
	public static Vector256<sbyte> Create(sbyte value)
	{
		if (Avx.IsSupported)
		{
			return Create(value);
		}
		return SoftwareFallback(value);
		unsafe static Vector256<sbyte> SoftwareFallback(sbyte value)
		{
			sbyte* source = stackalloc sbyte[32]
			{
				value, value, value, value, value, value, value, value, value, value,
				value, value, value, value, value, value, value, value, value, value,
				value, value, value, value, value, value, value, value, value, value,
				value, value
			};
			return Unsafe.AsRef<Vector256<sbyte>>(source);
		}
	}

	[Intrinsic]
	public static Vector256<float> Create(float value)
	{
		if (Avx.IsSupported)
		{
			return Create(value);
		}
		return SoftwareFallback(value);
		unsafe static Vector256<float> SoftwareFallback(float value)
		{
			float* source = stackalloc float[8] { value, value, value, value, value, value, value, value };
			return Unsafe.AsRef<Vector256<float>>(source);
		}
	}

	[Intrinsic]
	[CLSCompliant(false)]
	public static Vector256<ushort> Create(ushort value)
	{
		if (Avx.IsSupported)
		{
			return Create(value);
		}
		return SoftwareFallback(value);
		unsafe static Vector256<ushort> SoftwareFallback(ushort value)
		{
			ushort* source = stackalloc ushort[16]
			{
				value, value, value, value, value, value, value, value, value, value,
				value, value, value, value, value, value
			};
			return Unsafe.AsRef<Vector256<ushort>>(source);
		}
	}

	[Intrinsic]
	[CLSCompliant(false)]
	public static Vector256<uint> Create(uint value)
	{
		if (Avx.IsSupported)
		{
			return Create(value);
		}
		return SoftwareFallback(value);
		unsafe static Vector256<uint> SoftwareFallback(uint value)
		{
			uint* source = stackalloc uint[8] { value, value, value, value, value, value, value, value };
			return Unsafe.AsRef<Vector256<uint>>(source);
		}
	}

	[Intrinsic]
	[CLSCompliant(false)]
	public static Vector256<ulong> Create(ulong value)
	{
		if (Sse2.X64.IsSupported && Avx.IsSupported)
		{
			return Create(value);
		}
		return SoftwareFallback(value);
		unsafe static Vector256<ulong> SoftwareFallback(ulong value)
		{
			ulong* source = stackalloc ulong[4] { value, value, value, value };
			return Unsafe.AsRef<Vector256<ulong>>(source);
		}
	}

	[Intrinsic]
	public static Vector256<byte> Create(byte e0, byte e1, byte e2, byte e3, byte e4, byte e5, byte e6, byte e7, byte e8, byte e9, byte e10, byte e11, byte e12, byte e13, byte e14, byte e15, byte e16, byte e17, byte e18, byte e19, byte e20, byte e21, byte e22, byte e23, byte e24, byte e25, byte e26, byte e27, byte e28, byte e29, byte e30, byte e31)
	{
		if (Avx.IsSupported)
		{
			return Create(e0, e1, e2, e3, e4, e5, e6, e7, e8, e9, e10, e11, e12, e13, e14, e15, e16, e17, e18, e19, e20, e21, e22, e23, e24, e25, e26, e27, e28, e29, e30, e31);
		}
		return SoftwareFallback(e0, e1, e2, e3, e4, e5, e6, e7, e8, e9, e10, e11, e12, e13, e14, e15, e16, e17, e18, e19, e20, e21, e22, e23, e24, e25, e26, e27, e28, e29, e30, e31);
		unsafe static Vector256<byte> SoftwareFallback(byte e0, byte e1, byte e2, byte e3, byte e4, byte e5, byte e6, byte e7, byte e8, byte e9, byte e10, byte e11, byte e12, byte e13, byte e14, byte e15, byte e16, byte e17, byte e18, byte e19, byte e20, byte e21, byte e22, byte e23, byte e24, byte e25, byte e26, byte e27, byte e28, byte e29, byte e30, byte e31)
		{
			byte* source = stackalloc byte[32]
			{
				e0, e1, e2, e3, e4, e5, e6, e7, e8, e9,
				e10, e11, e12, e13, e14, e15, e16, e17, e18, e19,
				e20, e21, e22, e23, e24, e25, e26, e27, e28, e29,
				e30, e31
			};
			return Unsafe.AsRef<Vector256<byte>>(source);
		}
	}

	[Intrinsic]
	public static Vector256<double> Create(double e0, double e1, double e2, double e3)
	{
		if (Avx.IsSupported)
		{
			return Create(e0, e1, e2, e3);
		}
		return SoftwareFallback(e0, e1, e2, e3);
		unsafe static Vector256<double> SoftwareFallback(double e0, double e1, double e2, double e3)
		{
			double* source = stackalloc double[4] { e0, e1, e2, e3 };
			return Unsafe.AsRef<Vector256<double>>(source);
		}
	}

	[Intrinsic]
	public static Vector256<short> Create(short e0, short e1, short e2, short e3, short e4, short e5, short e6, short e7, short e8, short e9, short e10, short e11, short e12, short e13, short e14, short e15)
	{
		if (Avx.IsSupported)
		{
			return Create(e0, e1, e2, e3, e4, e5, e6, e7, e8, e9, e10, e11, e12, e13, e14, e15);
		}
		return SoftwareFallback(e0, e1, e2, e3, e4, e5, e6, e7, e8, e9, e10, e11, e12, e13, e14, e15);
		unsafe static Vector256<short> SoftwareFallback(short e0, short e1, short e2, short e3, short e4, short e5, short e6, short e7, short e8, short e9, short e10, short e11, short e12, short e13, short e14, short e15)
		{
			short* source = stackalloc short[16]
			{
				e0, e1, e2, e3, e4, e5, e6, e7, e8, e9,
				e10, e11, e12, e13, e14, e15
			};
			return Unsafe.AsRef<Vector256<short>>(source);
		}
	}

	[Intrinsic]
	public static Vector256<int> Create(int e0, int e1, int e2, int e3, int e4, int e5, int e6, int e7)
	{
		if (Avx.IsSupported)
		{
			return Create(e0, e1, e2, e3, e4, e5, e6, e7);
		}
		return SoftwareFallback(e0, e1, e2, e3, e4, e5, e6, e7);
		unsafe static Vector256<int> SoftwareFallback(int e0, int e1, int e2, int e3, int e4, int e5, int e6, int e7)
		{
			int* source = stackalloc int[8] { e0, e1, e2, e3, e4, e5, e6, e7 };
			return Unsafe.AsRef<Vector256<int>>(source);
		}
	}

	[Intrinsic]
	public static Vector256<long> Create(long e0, long e1, long e2, long e3)
	{
		if (Sse2.X64.IsSupported && Avx.IsSupported)
		{
			return Create(e0, e1, e2, e3);
		}
		return SoftwareFallback(e0, e1, e2, e3);
		unsafe static Vector256<long> SoftwareFallback(long e0, long e1, long e2, long e3)
		{
			long* source = stackalloc long[4] { e0, e1, e2, e3 };
			return Unsafe.AsRef<Vector256<long>>(source);
		}
	}

	[Intrinsic]
	[CLSCompliant(false)]
	public static Vector256<sbyte> Create(sbyte e0, sbyte e1, sbyte e2, sbyte e3, sbyte e4, sbyte e5, sbyte e6, sbyte e7, sbyte e8, sbyte e9, sbyte e10, sbyte e11, sbyte e12, sbyte e13, sbyte e14, sbyte e15, sbyte e16, sbyte e17, sbyte e18, sbyte e19, sbyte e20, sbyte e21, sbyte e22, sbyte e23, sbyte e24, sbyte e25, sbyte e26, sbyte e27, sbyte e28, sbyte e29, sbyte e30, sbyte e31)
	{
		if (Avx.IsSupported)
		{
			return Create(e0, e1, e2, e3, e4, e5, e6, e7, e8, e9, e10, e11, e12, e13, e14, e15, e16, e17, e18, e19, e20, e21, e22, e23, e24, e25, e26, e27, e28, e29, e30, e31);
		}
		return SoftwareFallback(e0, e1, e2, e3, e4, e5, e6, e7, e8, e9, e10, e11, e12, e13, e14, e15, e16, e17, e18, e19, e20, e21, e22, e23, e24, e25, e26, e27, e28, e29, e30, e31);
		unsafe static Vector256<sbyte> SoftwareFallback(sbyte e0, sbyte e1, sbyte e2, sbyte e3, sbyte e4, sbyte e5, sbyte e6, sbyte e7, sbyte e8, sbyte e9, sbyte e10, sbyte e11, sbyte e12, sbyte e13, sbyte e14, sbyte e15, sbyte e16, sbyte e17, sbyte e18, sbyte e19, sbyte e20, sbyte e21, sbyte e22, sbyte e23, sbyte e24, sbyte e25, sbyte e26, sbyte e27, sbyte e28, sbyte e29, sbyte e30, sbyte e31)
		{
			sbyte* source = stackalloc sbyte[32]
			{
				e0, e1, e2, e3, e4, e5, e6, e7, e8, e9,
				e10, e11, e12, e13, e14, e15, e16, e17, e18, e19,
				e20, e21, e22, e23, e24, e25, e26, e27, e28, e29,
				e30, e31
			};
			return Unsafe.AsRef<Vector256<sbyte>>(source);
		}
	}

	[Intrinsic]
	public static Vector256<float> Create(float e0, float e1, float e2, float e3, float e4, float e5, float e6, float e7)
	{
		if (Avx.IsSupported)
		{
			return Create(e0, e1, e2, e3, e4, e5, e6, e7);
		}
		return SoftwareFallback(e0, e1, e2, e3, e4, e5, e6, e7);
		unsafe static Vector256<float> SoftwareFallback(float e0, float e1, float e2, float e3, float e4, float e5, float e6, float e7)
		{
			float* source = stackalloc float[8] { e0, e1, e2, e3, e4, e5, e6, e7 };
			return Unsafe.AsRef<Vector256<float>>(source);
		}
	}

	[Intrinsic]
	[CLSCompliant(false)]
	public static Vector256<ushort> Create(ushort e0, ushort e1, ushort e2, ushort e3, ushort e4, ushort e5, ushort e6, ushort e7, ushort e8, ushort e9, ushort e10, ushort e11, ushort e12, ushort e13, ushort e14, ushort e15)
	{
		if (Avx.IsSupported)
		{
			return Create(e0, e1, e2, e3, e4, e5, e6, e7, e8, e9, e10, e11, e12, e13, e14, e15);
		}
		return SoftwareFallback(e0, e1, e2, e3, e4, e5, e6, e7, e8, e9, e10, e11, e12, e13, e14, e15);
		unsafe static Vector256<ushort> SoftwareFallback(ushort e0, ushort e1, ushort e2, ushort e3, ushort e4, ushort e5, ushort e6, ushort e7, ushort e8, ushort e9, ushort e10, ushort e11, ushort e12, ushort e13, ushort e14, ushort e15)
		{
			ushort* source = stackalloc ushort[16]
			{
				e0, e1, e2, e3, e4, e5, e6, e7, e8, e9,
				e10, e11, e12, e13, e14, e15
			};
			return Unsafe.AsRef<Vector256<ushort>>(source);
		}
	}

	[Intrinsic]
	[CLSCompliant(false)]
	public static Vector256<uint> Create(uint e0, uint e1, uint e2, uint e3, uint e4, uint e5, uint e6, uint e7)
	{
		if (Avx.IsSupported)
		{
			return Create(e0, e1, e2, e3, e4, e5, e6, e7);
		}
		return SoftwareFallback(e0, e1, e2, e3, e4, e5, e6, e7);
		unsafe static Vector256<uint> SoftwareFallback(uint e0, uint e1, uint e2, uint e3, uint e4, uint e5, uint e6, uint e7)
		{
			uint* source = stackalloc uint[8] { e0, e1, e2, e3, e4, e5, e6, e7 };
			return Unsafe.AsRef<Vector256<uint>>(source);
		}
	}

	[Intrinsic]
	[CLSCompliant(false)]
	public static Vector256<ulong> Create(ulong e0, ulong e1, ulong e2, ulong e3)
	{
		if (Sse2.X64.IsSupported && Avx.IsSupported)
		{
			return Create(e0, e1, e2, e3);
		}
		return SoftwareFallback(e0, e1, e2, e3);
		unsafe static Vector256<ulong> SoftwareFallback(ulong e0, ulong e1, ulong e2, ulong e3)
		{
			ulong* source = stackalloc ulong[4] { e0, e1, e2, e3 };
			return Unsafe.AsRef<Vector256<ulong>>(source);
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Vector256<byte> Create(Vector128<byte> lower, Vector128<byte> upper)
	{
		if (Avx.IsSupported)
		{
			Vector256<byte> vector = lower.ToVector256Unsafe();
			return vector.WithUpper(upper);
		}
		return SoftwareFallback(lower, upper);
		static Vector256<byte> SoftwareFallback(Vector128<byte> lower, Vector128<byte> upper)
		{
			Vector256<byte> source = Vector256<byte>.Zero;
			ref Vector128<byte> reference = ref Unsafe.As<Vector256<byte>, Vector128<byte>>(ref source);
			reference = lower;
			Unsafe.Add(ref reference, 1) = upper;
			return source;
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Vector256<double> Create(Vector128<double> lower, Vector128<double> upper)
	{
		if (Avx.IsSupported)
		{
			Vector256<double> vector = lower.ToVector256Unsafe();
			return vector.WithUpper(upper);
		}
		return SoftwareFallback(lower, upper);
		static Vector256<double> SoftwareFallback(Vector128<double> lower, Vector128<double> upper)
		{
			Vector256<double> source = Vector256<double>.Zero;
			ref Vector128<double> reference = ref Unsafe.As<Vector256<double>, Vector128<double>>(ref source);
			reference = lower;
			Unsafe.Add(ref reference, 1) = upper;
			return source;
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Vector256<short> Create(Vector128<short> lower, Vector128<short> upper)
	{
		if (Avx.IsSupported)
		{
			Vector256<short> vector = lower.ToVector256Unsafe();
			return vector.WithUpper(upper);
		}
		return SoftwareFallback(lower, upper);
		static Vector256<short> SoftwareFallback(Vector128<short> lower, Vector128<short> upper)
		{
			Vector256<short> source = Vector256<short>.Zero;
			ref Vector128<short> reference = ref Unsafe.As<Vector256<short>, Vector128<short>>(ref source);
			reference = lower;
			Unsafe.Add(ref reference, 1) = upper;
			return source;
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Vector256<int> Create(Vector128<int> lower, Vector128<int> upper)
	{
		if (Avx.IsSupported)
		{
			Vector256<int> vector = lower.ToVector256Unsafe();
			return vector.WithUpper(upper);
		}
		return SoftwareFallback(lower, upper);
		static Vector256<int> SoftwareFallback(Vector128<int> lower, Vector128<int> upper)
		{
			Vector256<int> source = Vector256<int>.Zero;
			ref Vector128<int> reference = ref Unsafe.As<Vector256<int>, Vector128<int>>(ref source);
			reference = lower;
			Unsafe.Add(ref reference, 1) = upper;
			return source;
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Vector256<long> Create(Vector128<long> lower, Vector128<long> upper)
	{
		if (Avx.IsSupported)
		{
			Vector256<long> vector = lower.ToVector256Unsafe();
			return vector.WithUpper(upper);
		}
		return SoftwareFallback(lower, upper);
		static Vector256<long> SoftwareFallback(Vector128<long> lower, Vector128<long> upper)
		{
			Vector256<long> source = Vector256<long>.Zero;
			ref Vector128<long> reference = ref Unsafe.As<Vector256<long>, Vector128<long>>(ref source);
			reference = lower;
			Unsafe.Add(ref reference, 1) = upper;
			return source;
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[CLSCompliant(false)]
	public static Vector256<sbyte> Create(Vector128<sbyte> lower, Vector128<sbyte> upper)
	{
		if (Avx.IsSupported)
		{
			Vector256<sbyte> vector = lower.ToVector256Unsafe();
			return vector.WithUpper(upper);
		}
		return SoftwareFallback(lower, upper);
		static Vector256<sbyte> SoftwareFallback(Vector128<sbyte> lower, Vector128<sbyte> upper)
		{
			Vector256<sbyte> source = Vector256<sbyte>.Zero;
			ref Vector128<sbyte> reference = ref Unsafe.As<Vector256<sbyte>, Vector128<sbyte>>(ref source);
			reference = lower;
			Unsafe.Add(ref reference, 1) = upper;
			return source;
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Vector256<float> Create(Vector128<float> lower, Vector128<float> upper)
	{
		if (Avx.IsSupported)
		{
			Vector256<float> vector = lower.ToVector256Unsafe();
			return vector.WithUpper(upper);
		}
		return SoftwareFallback(lower, upper);
		static Vector256<float> SoftwareFallback(Vector128<float> lower, Vector128<float> upper)
		{
			Vector256<float> source = Vector256<float>.Zero;
			ref Vector128<float> reference = ref Unsafe.As<Vector256<float>, Vector128<float>>(ref source);
			reference = lower;
			Unsafe.Add(ref reference, 1) = upper;
			return source;
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[CLSCompliant(false)]
	public static Vector256<ushort> Create(Vector128<ushort> lower, Vector128<ushort> upper)
	{
		if (Avx.IsSupported)
		{
			Vector256<ushort> vector = lower.ToVector256Unsafe();
			return vector.WithUpper(upper);
		}
		return SoftwareFallback(lower, upper);
		static Vector256<ushort> SoftwareFallback(Vector128<ushort> lower, Vector128<ushort> upper)
		{
			Vector256<ushort> source = Vector256<ushort>.Zero;
			ref Vector128<ushort> reference = ref Unsafe.As<Vector256<ushort>, Vector128<ushort>>(ref source);
			reference = lower;
			Unsafe.Add(ref reference, 1) = upper;
			return source;
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[CLSCompliant(false)]
	public static Vector256<uint> Create(Vector128<uint> lower, Vector128<uint> upper)
	{
		if (Avx.IsSupported)
		{
			Vector256<uint> vector = lower.ToVector256Unsafe();
			return vector.WithUpper(upper);
		}
		return SoftwareFallback(lower, upper);
		static Vector256<uint> SoftwareFallback(Vector128<uint> lower, Vector128<uint> upper)
		{
			Vector256<uint> source = Vector256<uint>.Zero;
			ref Vector128<uint> reference = ref Unsafe.As<Vector256<uint>, Vector128<uint>>(ref source);
			reference = lower;
			Unsafe.Add(ref reference, 1) = upper;
			return source;
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[CLSCompliant(false)]
	public static Vector256<ulong> Create(Vector128<ulong> lower, Vector128<ulong> upper)
	{
		if (Avx.IsSupported)
		{
			Vector256<ulong> vector = lower.ToVector256Unsafe();
			return vector.WithUpper(upper);
		}
		return SoftwareFallback(lower, upper);
		static Vector256<ulong> SoftwareFallback(Vector128<ulong> lower, Vector128<ulong> upper)
		{
			Vector256<ulong> source = Vector256<ulong>.Zero;
			ref Vector128<ulong> reference = ref Unsafe.As<Vector256<ulong>, Vector128<ulong>>(ref source);
			reference = lower;
			Unsafe.Add(ref reference, 1) = upper;
			return source;
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Vector256<byte> CreateScalar(byte value)
	{
		if (Avx.IsSupported)
		{
			return Vector128.CreateScalar(value).ToVector256();
		}
		return SoftwareFallback(value);
		static Vector256<byte> SoftwareFallback(byte value)
		{
			Vector256<byte> source = Vector256<byte>.Zero;
			Unsafe.WriteUnaligned(ref Unsafe.As<Vector256<byte>, byte>(ref source), value);
			return source;
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Vector256<double> CreateScalar(double value)
	{
		if (Avx.IsSupported)
		{
			return Vector128.CreateScalar(value).ToVector256();
		}
		return SoftwareFallback(value);
		static Vector256<double> SoftwareFallback(double value)
		{
			Vector256<double> source = Vector256<double>.Zero;
			Unsafe.WriteUnaligned(ref Unsafe.As<Vector256<double>, byte>(ref source), value);
			return source;
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Vector256<short> CreateScalar(short value)
	{
		if (Avx.IsSupported)
		{
			return Vector128.CreateScalar(value).ToVector256();
		}
		return SoftwareFallback(value);
		static Vector256<short> SoftwareFallback(short value)
		{
			Vector256<short> source = Vector256<short>.Zero;
			Unsafe.WriteUnaligned(ref Unsafe.As<Vector256<short>, byte>(ref source), value);
			return source;
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Vector256<int> CreateScalar(int value)
	{
		if (Avx.IsSupported)
		{
			return Vector128.CreateScalar(value).ToVector256();
		}
		return SoftwareFallback(value);
		static Vector256<int> SoftwareFallback(int value)
		{
			Vector256<int> source = Vector256<int>.Zero;
			Unsafe.WriteUnaligned(ref Unsafe.As<Vector256<int>, byte>(ref source), value);
			return source;
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Vector256<long> CreateScalar(long value)
	{
		if (Sse2.X64.IsSupported && Avx.IsSupported)
		{
			return Vector128.CreateScalar(value).ToVector256();
		}
		return SoftwareFallback(value);
		static Vector256<long> SoftwareFallback(long value)
		{
			Vector256<long> source = Vector256<long>.Zero;
			Unsafe.WriteUnaligned(ref Unsafe.As<Vector256<long>, byte>(ref source), value);
			return source;
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[CLSCompliant(false)]
	public static Vector256<sbyte> CreateScalar(sbyte value)
	{
		if (Avx.IsSupported)
		{
			return Vector128.CreateScalar(value).ToVector256();
		}
		return SoftwareFallback(value);
		static Vector256<sbyte> SoftwareFallback(sbyte value)
		{
			Vector256<sbyte> source = Vector256<sbyte>.Zero;
			Unsafe.WriteUnaligned(ref Unsafe.As<Vector256<sbyte>, byte>(ref source), value);
			return source;
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Vector256<float> CreateScalar(float value)
	{
		if (Avx.IsSupported)
		{
			return Vector128.CreateScalar(value).ToVector256();
		}
		return SoftwareFallback(value);
		static Vector256<float> SoftwareFallback(float value)
		{
			Vector256<float> source = Vector256<float>.Zero;
			Unsafe.WriteUnaligned(ref Unsafe.As<Vector256<float>, byte>(ref source), value);
			return source;
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[CLSCompliant(false)]
	public static Vector256<ushort> CreateScalar(ushort value)
	{
		if (Avx.IsSupported)
		{
			return Vector128.CreateScalar(value).ToVector256();
		}
		return SoftwareFallback(value);
		static Vector256<ushort> SoftwareFallback(ushort value)
		{
			Vector256<ushort> source = Vector256<ushort>.Zero;
			Unsafe.WriteUnaligned(ref Unsafe.As<Vector256<ushort>, byte>(ref source), value);
			return source;
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[CLSCompliant(false)]
	public static Vector256<uint> CreateScalar(uint value)
	{
		if (Avx.IsSupported)
		{
			return Vector128.CreateScalar(value).ToVector256();
		}
		return SoftwareFallback(value);
		static Vector256<uint> SoftwareFallback(uint value)
		{
			Vector256<uint> source = Vector256<uint>.Zero;
			Unsafe.WriteUnaligned(ref Unsafe.As<Vector256<uint>, byte>(ref source), value);
			return source;
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[CLSCompliant(false)]
	public static Vector256<ulong> CreateScalar(ulong value)
	{
		if (Sse2.X64.IsSupported && Avx.IsSupported)
		{
			return Vector128.CreateScalar(value).ToVector256();
		}
		return SoftwareFallback(value);
		static Vector256<ulong> SoftwareFallback(ulong value)
		{
			Vector256<ulong> source = Vector256<ulong>.Zero;
			Unsafe.WriteUnaligned(ref Unsafe.As<Vector256<ulong>, byte>(ref source), value);
			return source;
		}
	}

	[Intrinsic]
	public unsafe static Vector256<byte> CreateScalarUnsafe(byte value)
	{
		byte* ptr = stackalloc byte[32];
		*ptr = value;
		return Unsafe.AsRef<Vector256<byte>>(ptr);
	}

	[Intrinsic]
	public unsafe static Vector256<double> CreateScalarUnsafe(double value)
	{
		double* ptr = stackalloc double[4];
		*ptr = value;
		return Unsafe.AsRef<Vector256<double>>(ptr);
	}

	[Intrinsic]
	public unsafe static Vector256<short> CreateScalarUnsafe(short value)
	{
		short* ptr = stackalloc short[16];
		*ptr = value;
		return Unsafe.AsRef<Vector256<short>>(ptr);
	}

	[Intrinsic]
	public unsafe static Vector256<int> CreateScalarUnsafe(int value)
	{
		int* ptr = stackalloc int[8];
		*ptr = value;
		return Unsafe.AsRef<Vector256<int>>(ptr);
	}

	[Intrinsic]
	public unsafe static Vector256<long> CreateScalarUnsafe(long value)
	{
		long* ptr = stackalloc long[4];
		*ptr = value;
		return Unsafe.AsRef<Vector256<long>>(ptr);
	}

	[Intrinsic]
	[CLSCompliant(false)]
	public unsafe static Vector256<sbyte> CreateScalarUnsafe(sbyte value)
	{
		sbyte* ptr = stackalloc sbyte[32];
		*ptr = value;
		return Unsafe.AsRef<Vector256<sbyte>>(ptr);
	}

	[Intrinsic]
	public unsafe static Vector256<float> CreateScalarUnsafe(float value)
	{
		float* ptr = stackalloc float[8];
		*ptr = value;
		return Unsafe.AsRef<Vector256<float>>(ptr);
	}

	[Intrinsic]
	[CLSCompliant(false)]
	public unsafe static Vector256<ushort> CreateScalarUnsafe(ushort value)
	{
		ushort* ptr = stackalloc ushort[16];
		*ptr = value;
		return Unsafe.AsRef<Vector256<ushort>>(ptr);
	}

	[Intrinsic]
	[CLSCompliant(false)]
	public unsafe static Vector256<uint> CreateScalarUnsafe(uint value)
	{
		uint* ptr = stackalloc uint[8];
		*ptr = value;
		return Unsafe.AsRef<Vector256<uint>>(ptr);
	}

	[Intrinsic]
	[CLSCompliant(false)]
	public unsafe static Vector256<ulong> CreateScalarUnsafe(ulong value)
	{
		ulong* ptr = stackalloc ulong[4];
		*ptr = value;
		return Unsafe.AsRef<Vector256<ulong>>(ptr);
	}

	[Intrinsic]
	public static T GetElement<T>(this Vector256<T> vector, int index) where T : struct
	{
		ThrowHelper.ThrowForUnsupportedIntrinsicsVectorBaseType<T>();
		if ((uint)index >= (uint)Vector256<T>.Count)
		{
			ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.index);
		}
		return Unsafe.Add(ref Unsafe.As<Vector256<T>, T>(ref vector), index);
	}

	[Intrinsic]
	public static Vector256<T> WithElement<T>(this Vector256<T> vector, int index, T value) where T : struct
	{
		ThrowHelper.ThrowForUnsupportedIntrinsicsVectorBaseType<T>();
		if ((uint)index >= (uint)Vector256<T>.Count)
		{
			ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.index);
		}
		Vector256<T> source = vector;
		Unsafe.Add(ref Unsafe.As<Vector256<T>, T>(ref source), index) = value;
		return source;
	}

	[Intrinsic]
	public static Vector128<T> GetLower<T>(this Vector256<T> vector) where T : struct
	{
		ThrowHelper.ThrowForUnsupportedIntrinsicsVectorBaseType<T>();
		return Unsafe.As<Vector256<T>, Vector128<T>>(ref vector);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Vector256<T> WithLower<T>(this Vector256<T> vector, Vector128<T> value) where T : struct
	{
		ThrowHelper.ThrowForUnsupportedIntrinsicsVectorBaseType<T>();
		if (Avx2.IsSupported && typeof(T) != typeof(float) && typeof(T) != typeof(double))
		{
			return Avx2.InsertVector128(vector.AsByte(), value.AsByte(), 0).As<byte, T>();
		}
		if (Avx.IsSupported)
		{
			return Avx.InsertVector128(vector.AsSingle(), value.AsSingle(), 0).As<float, T>();
		}
		return SoftwareFallback(vector, value);
		static Vector256<T> SoftwareFallback(Vector256<T> vector, Vector128<T> value)
		{
			Vector256<T> source = vector;
			Unsafe.As<Vector256<T>, Vector128<T>>(ref source) = value;
			return source;
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Vector128<T> GetUpper<T>(this Vector256<T> vector) where T : struct
	{
		ThrowHelper.ThrowForUnsupportedIntrinsicsVectorBaseType<T>();
		if (Avx2.IsSupported && typeof(T) != typeof(float) && typeof(T) != typeof(double))
		{
			return Avx2.ExtractVector128(vector.AsByte(), 1).As<byte, T>();
		}
		if (Avx.IsSupported)
		{
			return Avx.ExtractVector128(vector.AsSingle(), 1).As<float, T>();
		}
		return SoftwareFallback(vector);
		static Vector128<T> SoftwareFallback(Vector256<T> vector)
		{
			return Unsafe.Add(ref Unsafe.As<Vector256<T>, Vector128<T>>(ref vector), 1);
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Vector256<T> WithUpper<T>(this Vector256<T> vector, Vector128<T> value) where T : struct
	{
		ThrowHelper.ThrowForUnsupportedIntrinsicsVectorBaseType<T>();
		if (Avx2.IsSupported && typeof(T) != typeof(float) && typeof(T) != typeof(double))
		{
			return Avx2.InsertVector128(vector.AsByte(), value.AsByte(), 1).As<byte, T>();
		}
		if (Avx.IsSupported)
		{
			return Avx.InsertVector128(vector.AsSingle(), value.AsSingle(), 1).As<float, T>();
		}
		return SoftwareFallback(vector, value);
		static Vector256<T> SoftwareFallback(Vector256<T> vector, Vector128<T> value)
		{
			Vector256<T> source = vector;
			Unsafe.Add(ref Unsafe.As<Vector256<T>, Vector128<T>>(ref source), 1) = value;
			return source;
		}
	}

	[Intrinsic]
	public static T ToScalar<T>(this Vector256<T> vector) where T : struct
	{
		ThrowHelper.ThrowForUnsupportedIntrinsicsVectorBaseType<T>();
		return Unsafe.As<Vector256<T>, T>(ref vector);
	}
}
[StructLayout(LayoutKind.Sequential, Size = 32)]
[Intrinsic]
[DebuggerDisplay("{DisplayString,nq}")]
[DebuggerTypeProxy(typeof(Vector256DebugView<>))]
public readonly struct Vector256<T> : IEquatable<Vector256<T>> where T : struct
{
	private readonly ulong _00;

	private readonly ulong _01;

	private readonly ulong _02;

	private readonly ulong _03;

	public static int Count
	{
		[Intrinsic]
		get
		{
			ThrowHelper.ThrowForUnsupportedIntrinsicsVectorBaseType<T>();
			return 32 / Unsafe.SizeOf<T>();
		}
	}

	public static Vector256<T> Zero
	{
		[Intrinsic]
		get
		{
			ThrowHelper.ThrowForUnsupportedIntrinsicsVectorBaseType<T>();
			return default(Vector256<T>);
		}
	}

	public static Vector256<T> AllBitsSet
	{
		[Intrinsic]
		get
		{
			ThrowHelper.ThrowForUnsupportedIntrinsicsVectorBaseType<T>();
			return Vector256.Create(uint.MaxValue).As<uint, T>();
		}
	}

	internal string DisplayString
	{
		get
		{
			if (IsSupported)
			{
				return ToString();
			}
			return SR.NotSupported_Type;
		}
	}

	internal static bool IsSupported
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		get
		{
			if (!(typeof(T) == typeof(byte)) && !(typeof(T) == typeof(sbyte)) && !(typeof(T) == typeof(short)) && !(typeof(T) == typeof(ushort)) && !(typeof(T) == typeof(int)) && !(typeof(T) == typeof(uint)) && !(typeof(T) == typeof(long)) && !(typeof(T) == typeof(ulong)) && !(typeof(T) == typeof(float)))
			{
				return typeof(T) == typeof(double);
			}
			return true;
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool Equals(Vector256<T> other)
	{
		if (Avx.IsSupported)
		{
			if (typeof(T) == typeof(float))
			{
				Vector256<float> value = Avx.Compare(this.AsSingle(), other.AsSingle(), FloatComparisonMode.OrderedEqualNonSignaling);
				return Avx.MoveMask(value) == 255;
			}
			if (typeof(T) == typeof(double))
			{
				Vector256<double> value2 = Avx.Compare(this.AsDouble(), other.AsDouble(), FloatComparisonMode.OrderedEqualNonSignaling);
				return Avx.MoveMask(value2) == 15;
			}
		}
		if (Avx2.IsSupported)
		{
			Vector256<byte> value3 = Avx2.CompareEqual(this.AsByte(), other.AsByte());
			return Avx2.MoveMask(value3) == -1;
		}
		return SoftwareFallback(in this, other);
		static bool SoftwareFallback(in Vector256<T> vector, Vector256<T> other)
		{
			for (int i = 0; i < Count; i++)
			{
				if (!((IEquatable<T>)(object)vector.GetElement(i)).Equals(other.GetElement(i)))
				{
					return false;
				}
			}
			return true;
		}
	}

	public override bool Equals([NotNullWhen(true)] object? obj)
	{
		if (obj is Vector256<T>)
		{
			return Equals((Vector256<T>)obj);
		}
		return false;
	}

	public override int GetHashCode()
	{
		ThrowHelper.ThrowForUnsupportedIntrinsicsVectorBaseType<T>();
		HashCode hashCode = default(HashCode);
		for (int i = 0; i < Count; i++)
		{
			hashCode.Add(this.GetElement(i).GetHashCode());
		}
		return hashCode.ToHashCode();
	}

	public override string ToString()
	{
		ThrowHelper.ThrowForUnsupportedIntrinsicsVectorBaseType<T>();
		int num = Count - 1;
		Span<char> initialBuffer = stackalloc char[64];
		ValueStringBuilder valueStringBuilder = new ValueStringBuilder(initialBuffer);
		CultureInfo invariantCulture = CultureInfo.InvariantCulture;
		valueStringBuilder.Append('<');
		for (int i = 0; i < num; i++)
		{
			valueStringBuilder.Append(((IFormattable)(object)this.GetElement(i)).ToString("G", invariantCulture));
			valueStringBuilder.Append(',');
			valueStringBuilder.Append(' ');
		}
		valueStringBuilder.Append(((IFormattable)(object)this.GetElement(num)).ToString("G", invariantCulture));
		valueStringBuilder.Append('>');
		return valueStringBuilder.ToString();
	}
}
