using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics.Arm;
using System.Runtime.Intrinsics.X86;
using System.Text;
using Internal.Runtime.CompilerServices;

namespace System.Runtime.Intrinsics;

public static class Vector128
{
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector128<U> As<T, U>(this Vector128<T> vector) where T : struct where U : struct
	{
		ThrowHelper.ThrowForUnsupportedIntrinsicsVectorBaseType<T>();
		ThrowHelper.ThrowForUnsupportedIntrinsicsVectorBaseType<U>();
		return Unsafe.As<Vector128<T>, Vector128<U>>(ref vector);
	}

	[Intrinsic]
	public static Vector128<byte> AsByte<T>(this Vector128<T> vector) where T : struct
	{
		return vector.As<T, byte>();
	}

	[Intrinsic]
	public static Vector128<double> AsDouble<T>(this Vector128<T> vector) where T : struct
	{
		return vector.As<T, double>();
	}

	[Intrinsic]
	public static Vector128<short> AsInt16<T>(this Vector128<T> vector) where T : struct
	{
		return vector.As<T, short>();
	}

	[Intrinsic]
	public static Vector128<int> AsInt32<T>(this Vector128<T> vector) where T : struct
	{
		return vector.As<T, int>();
	}

	[Intrinsic]
	public static Vector128<long> AsInt64<T>(this Vector128<T> vector) where T : struct
	{
		return vector.As<T, long>();
	}

	[Intrinsic]
	[CLSCompliant(false)]
	public static Vector128<sbyte> AsSByte<T>(this Vector128<T> vector) where T : struct
	{
		return vector.As<T, sbyte>();
	}

	[Intrinsic]
	public static Vector128<float> AsSingle<T>(this Vector128<T> vector) where T : struct
	{
		return vector.As<T, float>();
	}

	[Intrinsic]
	[CLSCompliant(false)]
	public static Vector128<ushort> AsUInt16<T>(this Vector128<T> vector) where T : struct
	{
		return vector.As<T, ushort>();
	}

	[Intrinsic]
	[CLSCompliant(false)]
	public static Vector128<uint> AsUInt32<T>(this Vector128<T> vector) where T : struct
	{
		return vector.As<T, uint>();
	}

	[Intrinsic]
	[CLSCompliant(false)]
	public static Vector128<ulong> AsUInt64<T>(this Vector128<T> vector) where T : struct
	{
		return vector.As<T, ulong>();
	}

	public static Vector128<float> AsVector128(this Vector2 value)
	{
		return new Vector4(value, 0f, 0f).AsVector128();
	}

	public static Vector128<float> AsVector128(this Vector3 value)
	{
		return new Vector4(value, 0f).AsVector128();
	}

	[Intrinsic]
	public static Vector128<float> AsVector128(this Vector4 value)
	{
		return Unsafe.As<Vector4, Vector128<float>>(ref value);
	}

	[Intrinsic]
	public static Vector128<T> AsVector128<T>(this Vector<T> value) where T : struct
	{
		ThrowHelper.ThrowForUnsupportedIntrinsicsVectorBaseType<T>();
		return Unsafe.As<Vector<T>, Vector128<T>>(ref value);
	}

	public static Vector2 AsVector2(this Vector128<float> value)
	{
		return Unsafe.As<Vector128<float>, Vector2>(ref value);
	}

	public static Vector3 AsVector3(this Vector128<float> value)
	{
		return Unsafe.As<Vector128<float>, Vector3>(ref value);
	}

	[Intrinsic]
	public static Vector4 AsVector4(this Vector128<float> value)
	{
		return Unsafe.As<Vector128<float>, Vector4>(ref value);
	}

	[Intrinsic]
	public static Vector<T> AsVector<T>(this Vector128<T> value) where T : struct
	{
		ThrowHelper.ThrowForUnsupportedIntrinsicsVectorBaseType<T>();
		Vector<T> source = default(Vector<T>);
		Unsafe.WriteUnaligned(ref Unsafe.As<Vector<T>, byte>(ref source), value);
		return source;
	}

	[Intrinsic]
	public static Vector128<byte> Create(byte value)
	{
		if (Sse2.IsSupported || AdvSimd.IsSupported)
		{
			return Create(value);
		}
		return SoftwareFallback(value);
		unsafe static Vector128<byte> SoftwareFallback(byte value)
		{
			byte* source = stackalloc byte[16]
			{
				value, value, value, value, value, value, value, value, value, value,
				value, value, value, value, value, value
			};
			return Unsafe.AsRef<Vector128<byte>>(source);
		}
	}

	[Intrinsic]
	public static Vector128<double> Create(double value)
	{
		if (Sse2.IsSupported || AdvSimd.IsSupported)
		{
			return Create(value);
		}
		return SoftwareFallback(value);
		unsafe static Vector128<double> SoftwareFallback(double value)
		{
			double* source = stackalloc double[2] { value, value };
			return Unsafe.AsRef<Vector128<double>>(source);
		}
	}

	[Intrinsic]
	public static Vector128<short> Create(short value)
	{
		if (Sse2.IsSupported || AdvSimd.IsSupported)
		{
			return Create(value);
		}
		return SoftwareFallback(value);
		unsafe static Vector128<short> SoftwareFallback(short value)
		{
			short* source = stackalloc short[8] { value, value, value, value, value, value, value, value };
			return Unsafe.AsRef<Vector128<short>>(source);
		}
	}

	[Intrinsic]
	public static Vector128<int> Create(int value)
	{
		if (Sse2.IsSupported || AdvSimd.IsSupported)
		{
			return Create(value);
		}
		return SoftwareFallback(value);
		unsafe static Vector128<int> SoftwareFallback(int value)
		{
			int* source = stackalloc int[4] { value, value, value, value };
			return Unsafe.AsRef<Vector128<int>>(source);
		}
	}

	[Intrinsic]
	public static Vector128<long> Create(long value)
	{
		if (Sse2.X64.IsSupported || AdvSimd.Arm64.IsSupported)
		{
			return Create(value);
		}
		return SoftwareFallback(value);
		unsafe static Vector128<long> SoftwareFallback(long value)
		{
			long* source = stackalloc long[2] { value, value };
			return Unsafe.AsRef<Vector128<long>>(source);
		}
	}

	[Intrinsic]
	[CLSCompliant(false)]
	public static Vector128<sbyte> Create(sbyte value)
	{
		if (Sse2.IsSupported || AdvSimd.IsSupported)
		{
			return Create(value);
		}
		return SoftwareFallback(value);
		unsafe static Vector128<sbyte> SoftwareFallback(sbyte value)
		{
			sbyte* source = stackalloc sbyte[16]
			{
				value, value, value, value, value, value, value, value, value, value,
				value, value, value, value, value, value
			};
			return Unsafe.AsRef<Vector128<sbyte>>(source);
		}
	}

	[Intrinsic]
	public static Vector128<float> Create(float value)
	{
		if (Sse.IsSupported || AdvSimd.IsSupported)
		{
			return Create(value);
		}
		return SoftwareFallback(value);
		unsafe static Vector128<float> SoftwareFallback(float value)
		{
			float* source = stackalloc float[4] { value, value, value, value };
			return Unsafe.AsRef<Vector128<float>>(source);
		}
	}

	[Intrinsic]
	[CLSCompliant(false)]
	public static Vector128<ushort> Create(ushort value)
	{
		if (Sse2.IsSupported || AdvSimd.IsSupported)
		{
			return Create(value);
		}
		return SoftwareFallback(value);
		unsafe static Vector128<ushort> SoftwareFallback(ushort value)
		{
			ushort* source = stackalloc ushort[8] { value, value, value, value, value, value, value, value };
			return Unsafe.AsRef<Vector128<ushort>>(source);
		}
	}

	[Intrinsic]
	[CLSCompliant(false)]
	public static Vector128<uint> Create(uint value)
	{
		if (Sse2.IsSupported || AdvSimd.IsSupported)
		{
			return Create(value);
		}
		return SoftwareFallback(value);
		unsafe static Vector128<uint> SoftwareFallback(uint value)
		{
			uint* source = stackalloc uint[4] { value, value, value, value };
			return Unsafe.AsRef<Vector128<uint>>(source);
		}
	}

	[Intrinsic]
	[CLSCompliant(false)]
	public static Vector128<ulong> Create(ulong value)
	{
		if (Sse2.X64.IsSupported || AdvSimd.Arm64.IsSupported)
		{
			return Create(value);
		}
		return SoftwareFallback(value);
		unsafe static Vector128<ulong> SoftwareFallback(ulong value)
		{
			ulong* source = stackalloc ulong[2] { value, value };
			return Unsafe.AsRef<Vector128<ulong>>(source);
		}
	}

	[Intrinsic]
	public static Vector128<byte> Create(byte e0, byte e1, byte e2, byte e3, byte e4, byte e5, byte e6, byte e7, byte e8, byte e9, byte e10, byte e11, byte e12, byte e13, byte e14, byte e15)
	{
		if (Sse2.IsSupported || AdvSimd.IsSupported)
		{
			return Create(e0, e1, e2, e3, e4, e5, e6, e7, e8, e9, e10, e11, e12, e13, e14, e15);
		}
		return SoftwareFallback(e0, e1, e2, e3, e4, e5, e6, e7, e8, e9, e10, e11, e12, e13, e14, e15);
		unsafe static Vector128<byte> SoftwareFallback(byte e0, byte e1, byte e2, byte e3, byte e4, byte e5, byte e6, byte e7, byte e8, byte e9, byte e10, byte e11, byte e12, byte e13, byte e14, byte e15)
		{
			byte* source = stackalloc byte[16]
			{
				e0, e1, e2, e3, e4, e5, e6, e7, e8, e9,
				e10, e11, e12, e13, e14, e15
			};
			return Unsafe.AsRef<Vector128<byte>>(source);
		}
	}

	[Intrinsic]
	public static Vector128<double> Create(double e0, double e1)
	{
		if (Sse2.IsSupported || AdvSimd.IsSupported)
		{
			return Create(e0, e1);
		}
		return SoftwareFallback(e0, e1);
		unsafe static Vector128<double> SoftwareFallback(double e0, double e1)
		{
			double* source = stackalloc double[2] { e0, e1 };
			return Unsafe.AsRef<Vector128<double>>(source);
		}
	}

	[Intrinsic]
	public static Vector128<short> Create(short e0, short e1, short e2, short e3, short e4, short e5, short e6, short e7)
	{
		if (Sse2.IsSupported || AdvSimd.IsSupported)
		{
			return Create(e0, e1, e2, e3, e4, e5, e6, e7);
		}
		return SoftwareFallback(e0, e1, e2, e3, e4, e5, e6, e7);
		unsafe static Vector128<short> SoftwareFallback(short e0, short e1, short e2, short e3, short e4, short e5, short e6, short e7)
		{
			short* source = stackalloc short[8] { e0, e1, e2, e3, e4, e5, e6, e7 };
			return Unsafe.AsRef<Vector128<short>>(source);
		}
	}

	[Intrinsic]
	public static Vector128<int> Create(int e0, int e1, int e2, int e3)
	{
		if (Sse2.IsSupported || AdvSimd.IsSupported)
		{
			return Create(e0, e1, e2, e3);
		}
		return SoftwareFallback(e0, e1, e2, e3);
		unsafe static Vector128<int> SoftwareFallback(int e0, int e1, int e2, int e3)
		{
			int* source = stackalloc int[4] { e0, e1, e2, e3 };
			return Unsafe.AsRef<Vector128<int>>(source);
		}
	}

	[Intrinsic]
	public static Vector128<long> Create(long e0, long e1)
	{
		if (Sse2.X64.IsSupported || AdvSimd.Arm64.IsSupported)
		{
			return Create(e0, e1);
		}
		return SoftwareFallback(e0, e1);
		unsafe static Vector128<long> SoftwareFallback(long e0, long e1)
		{
			long* source = stackalloc long[2] { e0, e1 };
			return Unsafe.AsRef<Vector128<long>>(source);
		}
	}

	[Intrinsic]
	[CLSCompliant(false)]
	public static Vector128<sbyte> Create(sbyte e0, sbyte e1, sbyte e2, sbyte e3, sbyte e4, sbyte e5, sbyte e6, sbyte e7, sbyte e8, sbyte e9, sbyte e10, sbyte e11, sbyte e12, sbyte e13, sbyte e14, sbyte e15)
	{
		if (Sse2.IsSupported || AdvSimd.IsSupported)
		{
			return Create(e0, e1, e2, e3, e4, e5, e6, e7, e8, e9, e10, e11, e12, e13, e14, e15);
		}
		return SoftwareFallback(e0, e1, e2, e3, e4, e5, e6, e7, e8, e9, e10, e11, e12, e13, e14, e15);
		unsafe static Vector128<sbyte> SoftwareFallback(sbyte e0, sbyte e1, sbyte e2, sbyte e3, sbyte e4, sbyte e5, sbyte e6, sbyte e7, sbyte e8, sbyte e9, sbyte e10, sbyte e11, sbyte e12, sbyte e13, sbyte e14, sbyte e15)
		{
			sbyte* source = stackalloc sbyte[16]
			{
				e0, e1, e2, e3, e4, e5, e6, e7, e8, e9,
				e10, e11, e12, e13, e14, e15
			};
			return Unsafe.AsRef<Vector128<sbyte>>(source);
		}
	}

	[Intrinsic]
	public static Vector128<float> Create(float e0, float e1, float e2, float e3)
	{
		if (Sse.IsSupported || AdvSimd.IsSupported)
		{
			return Create(e0, e1, e2, e3);
		}
		return SoftwareFallback(e0, e1, e2, e3);
		unsafe static Vector128<float> SoftwareFallback(float e0, float e1, float e2, float e3)
		{
			float* source = stackalloc float[4] { e0, e1, e2, e3 };
			return Unsafe.AsRef<Vector128<float>>(source);
		}
	}

	[Intrinsic]
	[CLSCompliant(false)]
	public static Vector128<ushort> Create(ushort e0, ushort e1, ushort e2, ushort e3, ushort e4, ushort e5, ushort e6, ushort e7)
	{
		if (Sse2.IsSupported || AdvSimd.IsSupported)
		{
			return Create(e0, e1, e2, e3, e4, e5, e6, e7);
		}
		return SoftwareFallback(e0, e1, e2, e3, e4, e5, e6, e7);
		unsafe static Vector128<ushort> SoftwareFallback(ushort e0, ushort e1, ushort e2, ushort e3, ushort e4, ushort e5, ushort e6, ushort e7)
		{
			ushort* source = stackalloc ushort[8] { e0, e1, e2, e3, e4, e5, e6, e7 };
			return Unsafe.AsRef<Vector128<ushort>>(source);
		}
	}

	[Intrinsic]
	[CLSCompliant(false)]
	public static Vector128<uint> Create(uint e0, uint e1, uint e2, uint e3)
	{
		if (Sse2.IsSupported || AdvSimd.IsSupported)
		{
			return Create(e0, e1, e2, e3);
		}
		return SoftwareFallback(e0, e1, e2, e3);
		unsafe static Vector128<uint> SoftwareFallback(uint e0, uint e1, uint e2, uint e3)
		{
			uint* source = stackalloc uint[4] { e0, e1, e2, e3 };
			return Unsafe.AsRef<Vector128<uint>>(source);
		}
	}

	[Intrinsic]
	[CLSCompliant(false)]
	public static Vector128<ulong> Create(ulong e0, ulong e1)
	{
		if (Sse2.X64.IsSupported || AdvSimd.Arm64.IsSupported)
		{
			return Create(e0, e1);
		}
		return SoftwareFallback(e0, e1);
		unsafe static Vector128<ulong> SoftwareFallback(ulong e0, ulong e1)
		{
			ulong* source = stackalloc ulong[2] { e0, e1 };
			return Unsafe.AsRef<Vector128<ulong>>(source);
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Vector128<byte> Create(Vector64<byte> lower, Vector64<byte> upper)
	{
		if (AdvSimd.IsSupported)
		{
		}
		return SoftwareFallback(lower, upper);
		static Vector128<byte> SoftwareFallback(Vector64<byte> lower, Vector64<byte> upper)
		{
			Vector128<byte> source = Vector128<byte>.Zero;
			ref Vector64<byte> reference = ref Unsafe.As<Vector128<byte>, Vector64<byte>>(ref source);
			reference = lower;
			Unsafe.Add(ref reference, 1) = upper;
			return source;
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Vector128<double> Create(Vector64<double> lower, Vector64<double> upper)
	{
		if (AdvSimd.IsSupported)
		{
		}
		return SoftwareFallback(lower, upper);
		static Vector128<double> SoftwareFallback(Vector64<double> lower, Vector64<double> upper)
		{
			Vector128<double> source = Vector128<double>.Zero;
			ref Vector64<double> reference = ref Unsafe.As<Vector128<double>, Vector64<double>>(ref source);
			reference = lower;
			Unsafe.Add(ref reference, 1) = upper;
			return source;
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Vector128<short> Create(Vector64<short> lower, Vector64<short> upper)
	{
		if (AdvSimd.IsSupported)
		{
		}
		return SoftwareFallback(lower, upper);
		static Vector128<short> SoftwareFallback(Vector64<short> lower, Vector64<short> upper)
		{
			Vector128<short> source = Vector128<short>.Zero;
			ref Vector64<short> reference = ref Unsafe.As<Vector128<short>, Vector64<short>>(ref source);
			reference = lower;
			Unsafe.Add(ref reference, 1) = upper;
			return source;
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Vector128<int> Create(Vector64<int> lower, Vector64<int> upper)
	{
		if (AdvSimd.IsSupported)
		{
		}
		return SoftwareFallback(lower, upper);
		static Vector128<int> SoftwareFallback(Vector64<int> lower, Vector64<int> upper)
		{
			Vector128<int> source = Vector128<int>.Zero;
			ref Vector64<int> reference = ref Unsafe.As<Vector128<int>, Vector64<int>>(ref source);
			reference = lower;
			Unsafe.Add(ref reference, 1) = upper;
			return source;
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Vector128<long> Create(Vector64<long> lower, Vector64<long> upper)
	{
		if (AdvSimd.IsSupported)
		{
		}
		return SoftwareFallback(lower, upper);
		static Vector128<long> SoftwareFallback(Vector64<long> lower, Vector64<long> upper)
		{
			Vector128<long> source = Vector128<long>.Zero;
			ref Vector64<long> reference = ref Unsafe.As<Vector128<long>, Vector64<long>>(ref source);
			reference = lower;
			Unsafe.Add(ref reference, 1) = upper;
			return source;
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[CLSCompliant(false)]
	public static Vector128<sbyte> Create(Vector64<sbyte> lower, Vector64<sbyte> upper)
	{
		if (AdvSimd.IsSupported)
		{
		}
		return SoftwareFallback(lower, upper);
		static Vector128<sbyte> SoftwareFallback(Vector64<sbyte> lower, Vector64<sbyte> upper)
		{
			Vector128<sbyte> source = Vector128<sbyte>.Zero;
			ref Vector64<sbyte> reference = ref Unsafe.As<Vector128<sbyte>, Vector64<sbyte>>(ref source);
			reference = lower;
			Unsafe.Add(ref reference, 1) = upper;
			return source;
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Vector128<float> Create(Vector64<float> lower, Vector64<float> upper)
	{
		if (AdvSimd.IsSupported)
		{
		}
		return SoftwareFallback(lower, upper);
		static Vector128<float> SoftwareFallback(Vector64<float> lower, Vector64<float> upper)
		{
			Vector128<float> source = Vector128<float>.Zero;
			ref Vector64<float> reference = ref Unsafe.As<Vector128<float>, Vector64<float>>(ref source);
			reference = lower;
			Unsafe.Add(ref reference, 1) = upper;
			return source;
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[CLSCompliant(false)]
	public static Vector128<ushort> Create(Vector64<ushort> lower, Vector64<ushort> upper)
	{
		if (AdvSimd.IsSupported)
		{
		}
		return SoftwareFallback(lower, upper);
		static Vector128<ushort> SoftwareFallback(Vector64<ushort> lower, Vector64<ushort> upper)
		{
			Vector128<ushort> source = Vector128<ushort>.Zero;
			ref Vector64<ushort> reference = ref Unsafe.As<Vector128<ushort>, Vector64<ushort>>(ref source);
			reference = lower;
			Unsafe.Add(ref reference, 1) = upper;
			return source;
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[CLSCompliant(false)]
	public static Vector128<uint> Create(Vector64<uint> lower, Vector64<uint> upper)
	{
		if (AdvSimd.IsSupported)
		{
		}
		return SoftwareFallback(lower, upper);
		static Vector128<uint> SoftwareFallback(Vector64<uint> lower, Vector64<uint> upper)
		{
			Vector128<uint> source = Vector128<uint>.Zero;
			ref Vector64<uint> reference = ref Unsafe.As<Vector128<uint>, Vector64<uint>>(ref source);
			reference = lower;
			Unsafe.Add(ref reference, 1) = upper;
			return source;
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[CLSCompliant(false)]
	public static Vector128<ulong> Create(Vector64<ulong> lower, Vector64<ulong> upper)
	{
		if (AdvSimd.IsSupported)
		{
		}
		return SoftwareFallback(lower, upper);
		static Vector128<ulong> SoftwareFallback(Vector64<ulong> lower, Vector64<ulong> upper)
		{
			Vector128<ulong> source = Vector128<ulong>.Zero;
			ref Vector64<ulong> reference = ref Unsafe.As<Vector128<ulong>, Vector64<ulong>>(ref source);
			reference = lower;
			Unsafe.Add(ref reference, 1) = upper;
			return source;
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Vector128<byte> CreateScalar(byte value)
	{
		if (AdvSimd.IsSupported)
		{
		}
		if (Sse2.IsSupported)
		{
			return Sse2.ConvertScalarToVector128UInt32(value).AsByte();
		}
		return SoftwareFallback(value);
		static Vector128<byte> SoftwareFallback(byte value)
		{
			Vector128<byte> source = Vector128<byte>.Zero;
			Unsafe.WriteUnaligned(ref Unsafe.As<Vector128<byte>, byte>(ref source), value);
			return source;
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Vector128<double> CreateScalar(double value)
	{
		if (AdvSimd.IsSupported)
		{
		}
		if (Sse2.IsSupported)
		{
			return Sse2.MoveScalar(Vector128<double>.Zero, CreateScalarUnsafe(value));
		}
		return SoftwareFallback(value);
		static Vector128<double> SoftwareFallback(double value)
		{
			Vector128<double> source = Vector128<double>.Zero;
			Unsafe.WriteUnaligned(ref Unsafe.As<Vector128<double>, byte>(ref source), value);
			return source;
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Vector128<short> CreateScalar(short value)
	{
		if (AdvSimd.IsSupported)
		{
		}
		if (Sse2.IsSupported)
		{
			return Sse2.ConvertScalarToVector128UInt32((ushort)value).AsInt16();
		}
		return SoftwareFallback(value);
		static Vector128<short> SoftwareFallback(short value)
		{
			Vector128<short> source = Vector128<short>.Zero;
			Unsafe.WriteUnaligned(ref Unsafe.As<Vector128<short>, byte>(ref source), value);
			return source;
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Vector128<int> CreateScalar(int value)
	{
		if (AdvSimd.IsSupported)
		{
		}
		if (Sse2.IsSupported)
		{
			return Sse2.ConvertScalarToVector128Int32(value);
		}
		return SoftwareFallback(value);
		static Vector128<int> SoftwareFallback(int value)
		{
			Vector128<int> source = Vector128<int>.Zero;
			Unsafe.WriteUnaligned(ref Unsafe.As<Vector128<int>, byte>(ref source), value);
			return source;
		}
	}

	public static Vector128<long> CreateScalar(long value)
	{
		if (AdvSimd.IsSupported)
		{
		}
		if (Sse2.X64.IsSupported)
		{
			return Sse2.X64.ConvertScalarToVector128Int64(value);
		}
		return SoftwareFallback(value);
		static Vector128<long> SoftwareFallback(long value)
		{
			Vector128<long> source = Vector128<long>.Zero;
			Unsafe.WriteUnaligned(ref Unsafe.As<Vector128<long>, byte>(ref source), value);
			return source;
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[CLSCompliant(false)]
	public static Vector128<sbyte> CreateScalar(sbyte value)
	{
		if (AdvSimd.IsSupported)
		{
		}
		if (Sse2.IsSupported)
		{
			return Sse2.ConvertScalarToVector128UInt32((byte)value).AsSByte();
		}
		return SoftwareFallback(value);
		static Vector128<sbyte> SoftwareFallback(sbyte value)
		{
			Vector128<sbyte> source = Vector128<sbyte>.Zero;
			Unsafe.WriteUnaligned(ref Unsafe.As<Vector128<sbyte>, byte>(ref source), value);
			return source;
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Vector128<float> CreateScalar(float value)
	{
		if (AdvSimd.IsSupported)
		{
		}
		if (Sse.IsSupported)
		{
			return Sse.MoveScalar(Vector128<float>.Zero, CreateScalarUnsafe(value));
		}
		return SoftwareFallback(value);
		static Vector128<float> SoftwareFallback(float value)
		{
			Vector128<float> source = Vector128<float>.Zero;
			Unsafe.WriteUnaligned(ref Unsafe.As<Vector128<float>, byte>(ref source), value);
			return source;
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[CLSCompliant(false)]
	public static Vector128<ushort> CreateScalar(ushort value)
	{
		if (AdvSimd.IsSupported)
		{
		}
		if (Sse2.IsSupported)
		{
			return Sse2.ConvertScalarToVector128UInt32(value).AsUInt16();
		}
		return SoftwareFallback(value);
		static Vector128<ushort> SoftwareFallback(ushort value)
		{
			Vector128<ushort> source = Vector128<ushort>.Zero;
			Unsafe.WriteUnaligned(ref Unsafe.As<Vector128<ushort>, byte>(ref source), value);
			return source;
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[CLSCompliant(false)]
	public static Vector128<uint> CreateScalar(uint value)
	{
		if (AdvSimd.IsSupported)
		{
		}
		if (Sse2.IsSupported)
		{
			return Sse2.ConvertScalarToVector128UInt32(value);
		}
		return SoftwareFallback(value);
		static Vector128<uint> SoftwareFallback(uint value)
		{
			Vector128<uint> source = Vector128<uint>.Zero;
			Unsafe.WriteUnaligned(ref Unsafe.As<Vector128<uint>, byte>(ref source), value);
			return source;
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[CLSCompliant(false)]
	public static Vector128<ulong> CreateScalar(ulong value)
	{
		if (AdvSimd.IsSupported)
		{
		}
		if (Sse2.X64.IsSupported)
		{
			return Sse2.X64.ConvertScalarToVector128UInt64(value);
		}
		return SoftwareFallback(value);
		static Vector128<ulong> SoftwareFallback(ulong value)
		{
			Vector128<ulong> source = Vector128<ulong>.Zero;
			Unsafe.WriteUnaligned(ref Unsafe.As<Vector128<ulong>, byte>(ref source), value);
			return source;
		}
	}

	[Intrinsic]
	public unsafe static Vector128<byte> CreateScalarUnsafe(byte value)
	{
		byte* ptr = stackalloc byte[16];
		*ptr = value;
		return Unsafe.AsRef<Vector128<byte>>(ptr);
	}

	[Intrinsic]
	public unsafe static Vector128<double> CreateScalarUnsafe(double value)
	{
		double* ptr = stackalloc double[2];
		*ptr = value;
		return Unsafe.AsRef<Vector128<double>>(ptr);
	}

	[Intrinsic]
	public unsafe static Vector128<short> CreateScalarUnsafe(short value)
	{
		short* ptr = stackalloc short[8];
		*ptr = value;
		return Unsafe.AsRef<Vector128<short>>(ptr);
	}

	[Intrinsic]
	public unsafe static Vector128<int> CreateScalarUnsafe(int value)
	{
		int* ptr = stackalloc int[4];
		*ptr = value;
		return Unsafe.AsRef<Vector128<int>>(ptr);
	}

	[Intrinsic]
	public unsafe static Vector128<long> CreateScalarUnsafe(long value)
	{
		long* ptr = stackalloc long[2];
		*ptr = value;
		return Unsafe.AsRef<Vector128<long>>(ptr);
	}

	[Intrinsic]
	[CLSCompliant(false)]
	public unsafe static Vector128<sbyte> CreateScalarUnsafe(sbyte value)
	{
		sbyte* ptr = stackalloc sbyte[16];
		*ptr = value;
		return Unsafe.AsRef<Vector128<sbyte>>(ptr);
	}

	[Intrinsic]
	public unsafe static Vector128<float> CreateScalarUnsafe(float value)
	{
		float* ptr = stackalloc float[4];
		*ptr = value;
		return Unsafe.AsRef<Vector128<float>>(ptr);
	}

	[Intrinsic]
	[CLSCompliant(false)]
	public unsafe static Vector128<ushort> CreateScalarUnsafe(ushort value)
	{
		ushort* ptr = stackalloc ushort[8];
		*ptr = value;
		return Unsafe.AsRef<Vector128<ushort>>(ptr);
	}

	[Intrinsic]
	[CLSCompliant(false)]
	public unsafe static Vector128<uint> CreateScalarUnsafe(uint value)
	{
		uint* ptr = stackalloc uint[4];
		*ptr = value;
		return Unsafe.AsRef<Vector128<uint>>(ptr);
	}

	[Intrinsic]
	[CLSCompliant(false)]
	public unsafe static Vector128<ulong> CreateScalarUnsafe(ulong value)
	{
		ulong* ptr = stackalloc ulong[2];
		*ptr = value;
		return Unsafe.AsRef<Vector128<ulong>>(ptr);
	}

	[Intrinsic]
	public static T GetElement<T>(this Vector128<T> vector, int index) where T : struct
	{
		ThrowHelper.ThrowForUnsupportedIntrinsicsVectorBaseType<T>();
		if ((uint)index >= (uint)Vector128<T>.Count)
		{
			ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.index);
		}
		return Unsafe.Add(ref Unsafe.As<Vector128<T>, T>(ref vector), index);
	}

	[Intrinsic]
	public static Vector128<T> WithElement<T>(this Vector128<T> vector, int index, T value) where T : struct
	{
		ThrowHelper.ThrowForUnsupportedIntrinsicsVectorBaseType<T>();
		if ((uint)index >= (uint)Vector128<T>.Count)
		{
			ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.index);
		}
		Vector128<T> source = vector;
		Unsafe.Add(ref Unsafe.As<Vector128<T>, T>(ref source), index) = value;
		return source;
	}

	[Intrinsic]
	public static Vector64<T> GetLower<T>(this Vector128<T> vector) where T : struct
	{
		ThrowHelper.ThrowForUnsupportedIntrinsicsVectorBaseType<T>();
		return Unsafe.As<Vector128<T>, Vector64<T>>(ref vector);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Vector128<T> WithLower<T>(this Vector128<T> vector, Vector64<T> value) where T : struct
	{
		if (AdvSimd.IsSupported)
		{
		}
		return SoftwareFallback(vector, value);
		static Vector128<T> SoftwareFallback(Vector128<T> vector, Vector64<T> value)
		{
			ThrowHelper.ThrowForUnsupportedIntrinsicsVectorBaseType<T>();
			Vector128<T> source = vector;
			Unsafe.As<Vector128<T>, Vector64<T>>(ref source) = value;
			return source;
		}
	}

	[Intrinsic]
	public static Vector64<T> GetUpper<T>(this Vector128<T> vector) where T : struct
	{
		ThrowHelper.ThrowForUnsupportedIntrinsicsVectorBaseType<T>();
		return Unsafe.Add(ref Unsafe.As<Vector128<T>, Vector64<T>>(ref vector), 1);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Vector128<T> WithUpper<T>(this Vector128<T> vector, Vector64<T> value) where T : struct
	{
		if (AdvSimd.IsSupported)
		{
		}
		return SoftwareFallback(vector, value);
		static Vector128<T> SoftwareFallback(Vector128<T> vector, Vector64<T> value)
		{
			ThrowHelper.ThrowForUnsupportedIntrinsicsVectorBaseType<T>();
			Vector128<T> source = vector;
			Unsafe.Add(ref Unsafe.As<Vector128<T>, Vector64<T>>(ref source), 1) = value;
			return source;
		}
	}

	[Intrinsic]
	public static T ToScalar<T>(this Vector128<T> vector) where T : struct
	{
		ThrowHelper.ThrowForUnsupportedIntrinsicsVectorBaseType<T>();
		return Unsafe.As<Vector128<T>, T>(ref vector);
	}

	[Intrinsic]
	public static Vector256<T> ToVector256<T>(this Vector128<T> vector) where T : struct
	{
		ThrowHelper.ThrowForUnsupportedIntrinsicsVectorBaseType<T>();
		Vector256<T> source = Vector256<T>.Zero;
		Unsafe.As<Vector256<T>, Vector128<T>>(ref source) = vector;
		return source;
	}

	[Intrinsic]
	public unsafe static Vector256<T> ToVector256Unsafe<T>(this Vector128<T> vector) where T : struct
	{
		ThrowHelper.ThrowForUnsupportedIntrinsicsVectorBaseType<T>();
		byte* source = stackalloc byte[32];
		Unsafe.AsRef<Vector128<T>>(source) = vector;
		return Unsafe.AsRef<Vector256<T>>(source);
	}
}
[StructLayout(LayoutKind.Sequential, Size = 16)]
[Intrinsic]
[DebuggerDisplay("{DisplayString,nq}")]
[DebuggerTypeProxy(typeof(Vector128DebugView<>))]
public readonly struct Vector128<T> : IEquatable<Vector128<T>> where T : struct
{
	private readonly ulong _00;

	private readonly ulong _01;

	public static int Count
	{
		[Intrinsic]
		get
		{
			ThrowHelper.ThrowForUnsupportedIntrinsicsVectorBaseType<T>();
			return 16 / Unsafe.SizeOf<T>();
		}
	}

	public static Vector128<T> Zero
	{
		[Intrinsic]
		get
		{
			ThrowHelper.ThrowForUnsupportedIntrinsicsVectorBaseType<T>();
			return default(Vector128<T>);
		}
	}

	public static Vector128<T> AllBitsSet
	{
		[Intrinsic]
		get
		{
			ThrowHelper.ThrowForUnsupportedIntrinsicsVectorBaseType<T>();
			return Vector128.Create(uint.MaxValue).As<uint, T>();
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
	public bool Equals(Vector128<T> other)
	{
		ThrowHelper.ThrowForUnsupportedIntrinsicsVectorBaseType<T>();
		if (Sse.IsSupported && typeof(T) == typeof(float))
		{
			Vector128<float> value = Sse.CompareEqual(this.AsSingle(), other.AsSingle());
			return Sse.MoveMask(value) == 15;
		}
		if (Sse2.IsSupported)
		{
			if (typeof(T) == typeof(double))
			{
				Vector128<double> value2 = Sse2.CompareEqual(this.AsDouble(), other.AsDouble());
				return Sse2.MoveMask(value2) == 3;
			}
			Vector128<byte> value3 = Sse2.CompareEqual(this.AsByte(), other.AsByte());
			return Sse2.MoveMask(value3) == 65535;
		}
		return SoftwareFallback(in this, other);
		static bool SoftwareFallback(in Vector128<T> vector, Vector128<T> other)
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
		if (obj is Vector128<T>)
		{
			return Equals((Vector128<T>)obj);
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
