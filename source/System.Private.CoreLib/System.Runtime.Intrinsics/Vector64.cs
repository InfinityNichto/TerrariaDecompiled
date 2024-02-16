using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics.Arm;
using System.Text;
using Internal.Runtime.CompilerServices;

namespace System.Runtime.Intrinsics;

public static class Vector64
{
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector64<U> As<T, U>(this Vector64<T> vector) where T : struct where U : struct
	{
		ThrowHelper.ThrowForUnsupportedIntrinsicsVectorBaseType<T>();
		ThrowHelper.ThrowForUnsupportedIntrinsicsVectorBaseType<U>();
		return Unsafe.As<Vector64<T>, Vector64<U>>(ref vector);
	}

	[Intrinsic]
	public static Vector64<byte> AsByte<T>(this Vector64<T> vector) where T : struct
	{
		return vector.As<T, byte>();
	}

	[Intrinsic]
	public static Vector64<double> AsDouble<T>(this Vector64<T> vector) where T : struct
	{
		return vector.As<T, double>();
	}

	[Intrinsic]
	public static Vector64<short> AsInt16<T>(this Vector64<T> vector) where T : struct
	{
		return vector.As<T, short>();
	}

	[Intrinsic]
	public static Vector64<int> AsInt32<T>(this Vector64<T> vector) where T : struct
	{
		return vector.As<T, int>();
	}

	[Intrinsic]
	public static Vector64<long> AsInt64<T>(this Vector64<T> vector) where T : struct
	{
		return vector.As<T, long>();
	}

	[Intrinsic]
	[CLSCompliant(false)]
	public static Vector64<sbyte> AsSByte<T>(this Vector64<T> vector) where T : struct
	{
		return vector.As<T, sbyte>();
	}

	[Intrinsic]
	public static Vector64<float> AsSingle<T>(this Vector64<T> vector) where T : struct
	{
		return vector.As<T, float>();
	}

	[Intrinsic]
	[CLSCompliant(false)]
	public static Vector64<ushort> AsUInt16<T>(this Vector64<T> vector) where T : struct
	{
		return vector.As<T, ushort>();
	}

	[Intrinsic]
	[CLSCompliant(false)]
	public static Vector64<uint> AsUInt32<T>(this Vector64<T> vector) where T : struct
	{
		return vector.As<T, uint>();
	}

	[Intrinsic]
	[CLSCompliant(false)]
	public static Vector64<ulong> AsUInt64<T>(this Vector64<T> vector) where T : struct
	{
		return vector.As<T, ulong>();
	}

	[Intrinsic]
	public static Vector64<byte> Create(byte value)
	{
		if (AdvSimd.IsSupported)
		{
		}
		return SoftwareFallback(value);
		unsafe static Vector64<byte> SoftwareFallback(byte value)
		{
			byte* source = stackalloc byte[8] { value, value, value, value, value, value, value, value };
			return Unsafe.AsRef<Vector64<byte>>(source);
		}
	}

	[Intrinsic]
	public static Vector64<double> Create(double value)
	{
		if (AdvSimd.IsSupported)
		{
		}
		return SoftwareFallback(value);
		static Vector64<double> SoftwareFallback(double value)
		{
			return Unsafe.As<double, Vector64<double>>(ref value);
		}
	}

	[Intrinsic]
	public static Vector64<short> Create(short value)
	{
		if (AdvSimd.IsSupported)
		{
		}
		return SoftwareFallback(value);
		unsafe static Vector64<short> SoftwareFallback(short value)
		{
			short* source = stackalloc short[4] { value, value, value, value };
			return Unsafe.AsRef<Vector64<short>>(source);
		}
	}

	[Intrinsic]
	public static Vector64<int> Create(int value)
	{
		if (AdvSimd.IsSupported)
		{
		}
		return SoftwareFallback(value);
		unsafe static Vector64<int> SoftwareFallback(int value)
		{
			int* source = stackalloc int[2] { value, value };
			return Unsafe.AsRef<Vector64<int>>(source);
		}
	}

	[Intrinsic]
	public static Vector64<long> Create(long value)
	{
		if (AdvSimd.Arm64.IsSupported)
		{
		}
		return SoftwareFallback(value);
		static Vector64<long> SoftwareFallback(long value)
		{
			return Unsafe.As<long, Vector64<long>>(ref value);
		}
	}

	[Intrinsic]
	[CLSCompliant(false)]
	public static Vector64<sbyte> Create(sbyte value)
	{
		if (AdvSimd.IsSupported)
		{
		}
		return SoftwareFallback(value);
		unsafe static Vector64<sbyte> SoftwareFallback(sbyte value)
		{
			sbyte* source = stackalloc sbyte[8] { value, value, value, value, value, value, value, value };
			return Unsafe.AsRef<Vector64<sbyte>>(source);
		}
	}

	[Intrinsic]
	public static Vector64<float> Create(float value)
	{
		if (AdvSimd.IsSupported)
		{
		}
		return SoftwareFallback(value);
		unsafe static Vector64<float> SoftwareFallback(float value)
		{
			float* source = stackalloc float[2] { value, value };
			return Unsafe.AsRef<Vector64<float>>(source);
		}
	}

	[Intrinsic]
	[CLSCompliant(false)]
	public static Vector64<ushort> Create(ushort value)
	{
		if (AdvSimd.IsSupported)
		{
		}
		return SoftwareFallback(value);
		unsafe static Vector64<ushort> SoftwareFallback(ushort value)
		{
			ushort* source = stackalloc ushort[4] { value, value, value, value };
			return Unsafe.AsRef<Vector64<ushort>>(source);
		}
	}

	[Intrinsic]
	[CLSCompliant(false)]
	public static Vector64<uint> Create(uint value)
	{
		if (AdvSimd.IsSupported)
		{
		}
		return SoftwareFallback(value);
		unsafe static Vector64<uint> SoftwareFallback(uint value)
		{
			uint* source = stackalloc uint[2] { value, value };
			return Unsafe.AsRef<Vector64<uint>>(source);
		}
	}

	[Intrinsic]
	[CLSCompliant(false)]
	public static Vector64<ulong> Create(ulong value)
	{
		if (AdvSimd.Arm64.IsSupported)
		{
		}
		return SoftwareFallback(value);
		static Vector64<ulong> SoftwareFallback(ulong value)
		{
			return Unsafe.As<ulong, Vector64<ulong>>(ref value);
		}
	}

	[Intrinsic]
	public static Vector64<byte> Create(byte e0, byte e1, byte e2, byte e3, byte e4, byte e5, byte e6, byte e7)
	{
		if (AdvSimd.IsSupported)
		{
		}
		return SoftwareFallback(e0, e1, e2, e3, e4, e5, e6, e7);
		unsafe static Vector64<byte> SoftwareFallback(byte e0, byte e1, byte e2, byte e3, byte e4, byte e5, byte e6, byte e7)
		{
			byte* source = stackalloc byte[8] { e0, e1, e2, e3, e4, e5, e6, e7 };
			return Unsafe.AsRef<Vector64<byte>>(source);
		}
	}

	[Intrinsic]
	public static Vector64<short> Create(short e0, short e1, short e2, short e3)
	{
		if (AdvSimd.IsSupported)
		{
		}
		return SoftwareFallback(e0, e1, e2, e3);
		unsafe static Vector64<short> SoftwareFallback(short e0, short e1, short e2, short e3)
		{
			short* source = stackalloc short[4] { e0, e1, e2, e3 };
			return Unsafe.AsRef<Vector64<short>>(source);
		}
	}

	[Intrinsic]
	public static Vector64<int> Create(int e0, int e1)
	{
		if (AdvSimd.IsSupported)
		{
		}
		return SoftwareFallback(e0, e1);
		unsafe static Vector64<int> SoftwareFallback(int e0, int e1)
		{
			int* source = stackalloc int[2] { e0, e1 };
			return Unsafe.AsRef<Vector64<int>>(source);
		}
	}

	[Intrinsic]
	[CLSCompliant(false)]
	public static Vector64<sbyte> Create(sbyte e0, sbyte e1, sbyte e2, sbyte e3, sbyte e4, sbyte e5, sbyte e6, sbyte e7)
	{
		if (AdvSimd.IsSupported)
		{
		}
		return SoftwareFallback(e0, e1, e2, e3, e4, e5, e6, e7);
		unsafe static Vector64<sbyte> SoftwareFallback(sbyte e0, sbyte e1, sbyte e2, sbyte e3, sbyte e4, sbyte e5, sbyte e6, sbyte e7)
		{
			sbyte* source = stackalloc sbyte[8] { e0, e1, e2, e3, e4, e5, e6, e7 };
			return Unsafe.AsRef<Vector64<sbyte>>(source);
		}
	}

	[Intrinsic]
	public static Vector64<float> Create(float e0, float e1)
	{
		if (AdvSimd.IsSupported)
		{
		}
		return SoftwareFallback(e0, e1);
		unsafe static Vector64<float> SoftwareFallback(float e0, float e1)
		{
			float* source = stackalloc float[2] { e0, e1 };
			return Unsafe.AsRef<Vector64<float>>(source);
		}
	}

	[Intrinsic]
	[CLSCompliant(false)]
	public static Vector64<ushort> Create(ushort e0, ushort e1, ushort e2, ushort e3)
	{
		if (AdvSimd.IsSupported)
		{
		}
		return SoftwareFallback(e0, e1, e2, e3);
		unsafe static Vector64<ushort> SoftwareFallback(ushort e0, ushort e1, ushort e2, ushort e3)
		{
			ushort* source = stackalloc ushort[4] { e0, e1, e2, e3 };
			return Unsafe.AsRef<Vector64<ushort>>(source);
		}
	}

	[Intrinsic]
	[CLSCompliant(false)]
	public static Vector64<uint> Create(uint e0, uint e1)
	{
		if (AdvSimd.IsSupported)
		{
		}
		return SoftwareFallback(e0, e1);
		unsafe static Vector64<uint> SoftwareFallback(uint e0, uint e1)
		{
			uint* source = stackalloc uint[2] { e0, e1 };
			return Unsafe.AsRef<Vector64<uint>>(source);
		}
	}

	public static Vector64<byte> CreateScalar(byte value)
	{
		if (AdvSimd.IsSupported)
		{
		}
		return SoftwareFallback(value);
		static Vector64<byte> SoftwareFallback(byte value)
		{
			Vector64<byte> source = Vector64<byte>.Zero;
			Unsafe.WriteUnaligned(ref Unsafe.As<Vector64<byte>, byte>(ref source), value);
			return source;
		}
	}

	public static Vector64<double> CreateScalar(double value)
	{
		if (AdvSimd.IsSupported)
		{
		}
		return SoftwareFallback(value);
		static Vector64<double> SoftwareFallback(double value)
		{
			return Unsafe.As<double, Vector64<double>>(ref value);
		}
	}

	public static Vector64<short> CreateScalar(short value)
	{
		if (AdvSimd.IsSupported)
		{
		}
		return SoftwareFallback(value);
		static Vector64<short> SoftwareFallback(short value)
		{
			Vector64<short> source = Vector64<short>.Zero;
			Unsafe.WriteUnaligned(ref Unsafe.As<Vector64<short>, byte>(ref source), value);
			return source;
		}
	}

	public static Vector64<int> CreateScalar(int value)
	{
		if (AdvSimd.IsSupported)
		{
		}
		return SoftwareFallback(value);
		static Vector64<int> SoftwareFallback(int value)
		{
			Vector64<int> source = Vector64<int>.Zero;
			Unsafe.WriteUnaligned(ref Unsafe.As<Vector64<int>, byte>(ref source), value);
			return source;
		}
	}

	public static Vector64<long> CreateScalar(long value)
	{
		if (AdvSimd.Arm64.IsSupported)
		{
		}
		return SoftwareFallback(value);
		static Vector64<long> SoftwareFallback(long value)
		{
			return Unsafe.As<long, Vector64<long>>(ref value);
		}
	}

	[CLSCompliant(false)]
	public static Vector64<sbyte> CreateScalar(sbyte value)
	{
		if (AdvSimd.IsSupported)
		{
		}
		return SoftwareFallback(value);
		static Vector64<sbyte> SoftwareFallback(sbyte value)
		{
			Vector64<sbyte> source = Vector64<sbyte>.Zero;
			Unsafe.WriteUnaligned(ref Unsafe.As<Vector64<sbyte>, byte>(ref source), value);
			return source;
		}
	}

	public static Vector64<float> CreateScalar(float value)
	{
		if (AdvSimd.IsSupported)
		{
		}
		return SoftwareFallback(value);
		static Vector64<float> SoftwareFallback(float value)
		{
			Vector64<float> source = Vector64<float>.Zero;
			Unsafe.WriteUnaligned(ref Unsafe.As<Vector64<float>, byte>(ref source), value);
			return source;
		}
	}

	[CLSCompliant(false)]
	public static Vector64<ushort> CreateScalar(ushort value)
	{
		if (AdvSimd.IsSupported)
		{
		}
		return SoftwareFallback(value);
		static Vector64<ushort> SoftwareFallback(ushort value)
		{
			Vector64<ushort> source = Vector64<ushort>.Zero;
			Unsafe.WriteUnaligned(ref Unsafe.As<Vector64<ushort>, byte>(ref source), value);
			return source;
		}
	}

	[CLSCompliant(false)]
	public static Vector64<uint> CreateScalar(uint value)
	{
		if (AdvSimd.IsSupported)
		{
		}
		return SoftwareFallback(value);
		static Vector64<uint> SoftwareFallback(uint value)
		{
			Vector64<uint> source = Vector64<uint>.Zero;
			Unsafe.WriteUnaligned(ref Unsafe.As<Vector64<uint>, byte>(ref source), value);
			return source;
		}
	}

	[CLSCompliant(false)]
	public static Vector64<ulong> CreateScalar(ulong value)
	{
		if (AdvSimd.Arm64.IsSupported)
		{
		}
		return SoftwareFallback(value);
		static Vector64<ulong> SoftwareFallback(ulong value)
		{
			return Unsafe.As<ulong, Vector64<ulong>>(ref value);
		}
	}

	[Intrinsic]
	public unsafe static Vector64<byte> CreateScalarUnsafe(byte value)
	{
		byte* ptr = stackalloc byte[8];
		*ptr = value;
		return Unsafe.AsRef<Vector64<byte>>(ptr);
	}

	[Intrinsic]
	public unsafe static Vector64<short> CreateScalarUnsafe(short value)
	{
		short* ptr = stackalloc short[4];
		*ptr = value;
		return Unsafe.AsRef<Vector64<short>>(ptr);
	}

	[Intrinsic]
	public unsafe static Vector64<int> CreateScalarUnsafe(int value)
	{
		int* ptr = stackalloc int[2];
		*ptr = value;
		return Unsafe.AsRef<Vector64<int>>(ptr);
	}

	[CLSCompliant(false)]
	[Intrinsic]
	public unsafe static Vector64<sbyte> CreateScalarUnsafe(sbyte value)
	{
		sbyte* ptr = stackalloc sbyte[8];
		*ptr = value;
		return Unsafe.AsRef<Vector64<sbyte>>(ptr);
	}

	[Intrinsic]
	public unsafe static Vector64<float> CreateScalarUnsafe(float value)
	{
		float* ptr = stackalloc float[2];
		*ptr = value;
		return Unsafe.AsRef<Vector64<float>>(ptr);
	}

	[CLSCompliant(false)]
	[Intrinsic]
	public unsafe static Vector64<ushort> CreateScalarUnsafe(ushort value)
	{
		ushort* ptr = stackalloc ushort[4];
		*ptr = value;
		return Unsafe.AsRef<Vector64<ushort>>(ptr);
	}

	[CLSCompliant(false)]
	[Intrinsic]
	public unsafe static Vector64<uint> CreateScalarUnsafe(uint value)
	{
		uint* ptr = stackalloc uint[2];
		*ptr = value;
		return Unsafe.AsRef<Vector64<uint>>(ptr);
	}

	[Intrinsic]
	public static T GetElement<T>(this Vector64<T> vector, int index) where T : struct
	{
		ThrowHelper.ThrowForUnsupportedIntrinsicsVectorBaseType<T>();
		if ((uint)index >= (uint)Vector64<T>.Count)
		{
			ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.index);
		}
		return Unsafe.Add(ref Unsafe.As<Vector64<T>, T>(ref vector), index);
	}

	[Intrinsic]
	public static Vector64<T> WithElement<T>(this Vector64<T> vector, int index, T value) where T : struct
	{
		ThrowHelper.ThrowForUnsupportedIntrinsicsVectorBaseType<T>();
		if ((uint)index >= (uint)Vector64<T>.Count)
		{
			ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.index);
		}
		Vector64<T> source = vector;
		Unsafe.Add(ref Unsafe.As<Vector64<T>, T>(ref source), index) = value;
		return source;
	}

	[Intrinsic]
	public static T ToScalar<T>(this Vector64<T> vector) where T : struct
	{
		ThrowHelper.ThrowForUnsupportedIntrinsicsVectorBaseType<T>();
		return Unsafe.As<Vector64<T>, T>(ref vector);
	}

	[Intrinsic]
	public static Vector128<T> ToVector128<T>(this Vector64<T> vector) where T : struct
	{
		ThrowHelper.ThrowForUnsupportedIntrinsicsVectorBaseType<T>();
		Vector128<T> source = Vector128<T>.Zero;
		Unsafe.As<Vector128<T>, Vector64<T>>(ref source) = vector;
		return source;
	}

	[Intrinsic]
	public unsafe static Vector128<T> ToVector128Unsafe<T>(this Vector64<T> vector) where T : struct
	{
		ThrowHelper.ThrowForUnsupportedIntrinsicsVectorBaseType<T>();
		byte* source = stackalloc byte[16];
		Unsafe.AsRef<Vector64<T>>(source) = vector;
		return Unsafe.AsRef<Vector128<T>>(source);
	}
}
[StructLayout(LayoutKind.Sequential, Size = 8)]
[Intrinsic]
[DebuggerDisplay("{DisplayString,nq}")]
[DebuggerTypeProxy(typeof(Vector64DebugView<>))]
public readonly struct Vector64<T> : IEquatable<Vector64<T>> where T : struct
{
	private readonly ulong _00;

	public static int Count
	{
		[Intrinsic]
		get
		{
			ThrowHelper.ThrowForUnsupportedIntrinsicsVectorBaseType<T>();
			return 8 / Unsafe.SizeOf<T>();
		}
	}

	public static Vector64<T> Zero
	{
		[Intrinsic]
		get
		{
			ThrowHelper.ThrowForUnsupportedIntrinsicsVectorBaseType<T>();
			return default(Vector64<T>);
		}
	}

	public static Vector64<T> AllBitsSet
	{
		[Intrinsic]
		get
		{
			ThrowHelper.ThrowForUnsupportedIntrinsicsVectorBaseType<T>();
			return Vector64.Create(uint.MaxValue).As<uint, T>();
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

	public bool Equals(Vector64<T> other)
	{
		ThrowHelper.ThrowForUnsupportedIntrinsicsVectorBaseType<T>();
		for (int i = 0; i < Count; i++)
		{
			if (!((IEquatable<T>)(object)this.GetElement(i)).Equals(other.GetElement(i)))
			{
				return false;
			}
		}
		return true;
	}

	public override bool Equals([NotNullWhen(true)] object? obj)
	{
		if (obj is Vector64<T>)
		{
			return Equals((Vector64<T>)obj);
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
