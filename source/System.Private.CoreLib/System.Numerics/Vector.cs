using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using Internal.Runtime.CompilerServices;

namespace System.Numerics;

[Intrinsic]
public static class Vector
{
	public static bool IsHardwareAccelerated
	{
		[Intrinsic]
		get
		{
			return false;
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector<float> ConditionalSelect(Vector<int> condition, Vector<float> left, Vector<float> right)
	{
		return Vector<float>.ConditionalSelect((Vector<float>)condition, left, right);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector<double> ConditionalSelect(Vector<long> condition, Vector<double> left, Vector<double> right)
	{
		return Vector<double>.ConditionalSelect((Vector<double>)condition, left, right);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector<T> ConditionalSelect<T>(Vector<T> condition, Vector<T> left, Vector<T> right) where T : struct
	{
		return Vector<T>.ConditionalSelect(condition, left, right);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector<T> Equals<T>(Vector<T> left, Vector<T> right) where T : struct
	{
		return Vector<T>.Equals(left, right);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector<int> Equals(Vector<float> left, Vector<float> right)
	{
		return (Vector<int>)Vector<float>.Equals(left, right);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector<int> Equals(Vector<int> left, Vector<int> right)
	{
		return Vector<int>.Equals(left, right);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector<long> Equals(Vector<double> left, Vector<double> right)
	{
		return (Vector<long>)Vector<double>.Equals(left, right);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector<long> Equals(Vector<long> left, Vector<long> right)
	{
		return Vector<long>.Equals(left, right);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool EqualsAll<T>(Vector<T> left, Vector<T> right) where T : struct
	{
		return left == right;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool EqualsAny<T>(Vector<T> left, Vector<T> right) where T : struct
	{
		return !Vector<T>.Equals(left, right).Equals(Vector<T>.Zero);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector<T> LessThan<T>(Vector<T> left, Vector<T> right) where T : struct
	{
		return Vector<T>.LessThan(left, right);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector<int> LessThan(Vector<float> left, Vector<float> right)
	{
		return (Vector<int>)Vector<float>.LessThan(left, right);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector<int> LessThan(Vector<int> left, Vector<int> right)
	{
		return Vector<int>.LessThan(left, right);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector<long> LessThan(Vector<double> left, Vector<double> right)
	{
		return (Vector<long>)Vector<double>.LessThan(left, right);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector<long> LessThan(Vector<long> left, Vector<long> right)
	{
		return Vector<long>.LessThan(left, right);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool LessThanAll<T>(Vector<T> left, Vector<T> right) where T : struct
	{
		return ((Vector<int>)Vector<T>.LessThan(left, right)).Equals(Vector<int>.AllBitsSet);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool LessThanAny<T>(Vector<T> left, Vector<T> right) where T : struct
	{
		return !((Vector<int>)Vector<T>.LessThan(left, right)).Equals(Vector<int>.Zero);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector<T> LessThanOrEqual<T>(Vector<T> left, Vector<T> right) where T : struct
	{
		return Vector<T>.LessThanOrEqual(left, right);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector<int> LessThanOrEqual(Vector<float> left, Vector<float> right)
	{
		return (Vector<int>)Vector<float>.LessThanOrEqual(left, right);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector<int> LessThanOrEqual(Vector<int> left, Vector<int> right)
	{
		return Vector<int>.LessThanOrEqual(left, right);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector<long> LessThanOrEqual(Vector<long> left, Vector<long> right)
	{
		return Vector<long>.LessThanOrEqual(left, right);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector<long> LessThanOrEqual(Vector<double> left, Vector<double> right)
	{
		return (Vector<long>)Vector<double>.LessThanOrEqual(left, right);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool LessThanOrEqualAll<T>(Vector<T> left, Vector<T> right) where T : struct
	{
		return ((Vector<int>)Vector<T>.LessThanOrEqual(left, right)).Equals(Vector<int>.AllBitsSet);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool LessThanOrEqualAny<T>(Vector<T> left, Vector<T> right) where T : struct
	{
		return !((Vector<int>)Vector<T>.LessThanOrEqual(left, right)).Equals(Vector<int>.Zero);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector<T> GreaterThan<T>(Vector<T> left, Vector<T> right) where T : struct
	{
		return Vector<T>.GreaterThan(left, right);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector<int> GreaterThan(Vector<float> left, Vector<float> right)
	{
		return (Vector<int>)Vector<float>.GreaterThan(left, right);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector<int> GreaterThan(Vector<int> left, Vector<int> right)
	{
		return Vector<int>.GreaterThan(left, right);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector<long> GreaterThan(Vector<double> left, Vector<double> right)
	{
		return (Vector<long>)Vector<double>.GreaterThan(left, right);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector<long> GreaterThan(Vector<long> left, Vector<long> right)
	{
		return Vector<long>.GreaterThan(left, right);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool GreaterThanAll<T>(Vector<T> left, Vector<T> right) where T : struct
	{
		return ((Vector<int>)Vector<T>.GreaterThan(left, right)).Equals(Vector<int>.AllBitsSet);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool GreaterThanAny<T>(Vector<T> left, Vector<T> right) where T : struct
	{
		return !((Vector<int>)Vector<T>.GreaterThan(left, right)).Equals(Vector<int>.Zero);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector<T> GreaterThanOrEqual<T>(Vector<T> left, Vector<T> right) where T : struct
	{
		return Vector<T>.GreaterThanOrEqual(left, right);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector<int> GreaterThanOrEqual(Vector<float> left, Vector<float> right)
	{
		return (Vector<int>)Vector<float>.GreaterThanOrEqual(left, right);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector<int> GreaterThanOrEqual(Vector<int> left, Vector<int> right)
	{
		return Vector<int>.GreaterThanOrEqual(left, right);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector<long> GreaterThanOrEqual(Vector<long> left, Vector<long> right)
	{
		return Vector<long>.GreaterThanOrEqual(left, right);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector<long> GreaterThanOrEqual(Vector<double> left, Vector<double> right)
	{
		return (Vector<long>)Vector<double>.GreaterThanOrEqual(left, right);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool GreaterThanOrEqualAll<T>(Vector<T> left, Vector<T> right) where T : struct
	{
		return ((Vector<int>)Vector<T>.GreaterThanOrEqual(left, right)).Equals(Vector<int>.AllBitsSet);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool GreaterThanOrEqualAny<T>(Vector<T> left, Vector<T> right) where T : struct
	{
		return !((Vector<int>)Vector<T>.GreaterThanOrEqual(left, right)).Equals(Vector<int>.Zero);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector<T> Abs<T>(Vector<T> value) where T : struct
	{
		return Vector<T>.Abs(value);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector<T> Min<T>(Vector<T> left, Vector<T> right) where T : struct
	{
		return Vector<T>.Min(left, right);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector<T> Max<T>(Vector<T> left, Vector<T> right) where T : struct
	{
		return Vector<T>.Max(left, right);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static T Dot<T>(Vector<T> left, Vector<T> right) where T : struct
	{
		return Vector<T>.Dot(left, right);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector<T> SquareRoot<T>(Vector<T> value) where T : struct
	{
		return Vector<T>.SquareRoot(value);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector<float> Ceiling(Vector<float> value)
	{
		return Vector<float>.Ceiling(value);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector<double> Ceiling(Vector<double> value)
	{
		return Vector<double>.Ceiling(value);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector<float> Floor(Vector<float> value)
	{
		return Vector<float>.Floor(value);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector<double> Floor(Vector<double> value)
	{
		return Vector<double>.Floor(value);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Vector<T> Add<T>(Vector<T> left, Vector<T> right) where T : struct
	{
		return left + right;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Vector<T> Subtract<T>(Vector<T> left, Vector<T> right) where T : struct
	{
		return left - right;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Vector<T> Multiply<T>(Vector<T> left, Vector<T> right) where T : struct
	{
		return left * right;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Vector<T> Multiply<T>(Vector<T> left, T right) where T : struct
	{
		return left * right;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Vector<T> Multiply<T>(T left, Vector<T> right) where T : struct
	{
		return left * right;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Vector<T> Divide<T>(Vector<T> left, Vector<T> right) where T : struct
	{
		return left / right;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Vector<T> Negate<T>(Vector<T> value) where T : struct
	{
		return -value;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Vector<T> BitwiseAnd<T>(Vector<T> left, Vector<T> right) where T : struct
	{
		return left & right;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Vector<T> BitwiseOr<T>(Vector<T> left, Vector<T> right) where T : struct
	{
		return left | right;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Vector<T> OnesComplement<T>(Vector<T> value) where T : struct
	{
		return ~value;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Vector<T> Xor<T>(Vector<T> left, Vector<T> right) where T : struct
	{
		return left ^ right;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector<T> AndNot<T>(Vector<T> left, Vector<T> right) where T : struct
	{
		return left & ~right;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Vector<byte> AsVectorByte<T>(Vector<T> value) where T : struct
	{
		return (Vector<byte>)value;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[CLSCompliant(false)]
	public static Vector<sbyte> AsVectorSByte<T>(Vector<T> value) where T : struct
	{
		return (Vector<sbyte>)value;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[CLSCompliant(false)]
	public static Vector<ushort> AsVectorUInt16<T>(Vector<T> value) where T : struct
	{
		return (Vector<ushort>)value;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Vector<short> AsVectorInt16<T>(Vector<T> value) where T : struct
	{
		return (Vector<short>)value;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[CLSCompliant(false)]
	public static Vector<uint> AsVectorUInt32<T>(Vector<T> value) where T : struct
	{
		return (Vector<uint>)value;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Vector<int> AsVectorInt32<T>(Vector<T> value) where T : struct
	{
		return (Vector<int>)value;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[CLSCompliant(false)]
	public static Vector<ulong> AsVectorUInt64<T>(Vector<T> value) where T : struct
	{
		return (Vector<ulong>)value;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Vector<long> AsVectorInt64<T>(Vector<T> value) where T : struct
	{
		return (Vector<long>)value;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Vector<float> AsVectorSingle<T>(Vector<T> value) where T : struct
	{
		return (Vector<float>)value;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Vector<double> AsVectorDouble<T>(Vector<T> value) where T : struct
	{
		return (Vector<double>)value;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[CLSCompliant(false)]
	public static Vector<nuint> AsVectorNUInt<T>(Vector<T> value) where T : struct
	{
		return (Vector<nuint>)value;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Vector<nint> AsVectorNInt<T>(Vector<T> value) where T : struct
	{
		return (Vector<nint>)value;
	}

	[CLSCompliant(false)]
	[Intrinsic]
	public unsafe static void Widen(Vector<byte> source, out Vector<ushort> low, out Vector<ushort> high)
	{
		int count = Vector<byte>.Count;
		ushort* ptr = stackalloc ushort[count / 2];
		for (int i = 0; i < count / 2; i++)
		{
			ptr[i] = source[i];
		}
		ushort* ptr2 = stackalloc ushort[count / 2];
		for (int j = 0; j < count / 2; j++)
		{
			ptr2[j] = source[j + count / 2];
		}
		low = *(Vector<ushort>*)ptr;
		high = *(Vector<ushort>*)ptr2;
	}

	[CLSCompliant(false)]
	[Intrinsic]
	public unsafe static void Widen(Vector<ushort> source, out Vector<uint> low, out Vector<uint> high)
	{
		int count = Vector<ushort>.Count;
		uint* ptr = stackalloc uint[count / 2];
		for (int i = 0; i < count / 2; i++)
		{
			ptr[i] = source[i];
		}
		uint* ptr2 = stackalloc uint[count / 2];
		for (int j = 0; j < count / 2; j++)
		{
			ptr2[j] = source[j + count / 2];
		}
		low = *(Vector<uint>*)ptr;
		high = *(Vector<uint>*)ptr2;
	}

	[CLSCompliant(false)]
	[Intrinsic]
	public unsafe static void Widen(Vector<uint> source, out Vector<ulong> low, out Vector<ulong> high)
	{
		int count = Vector<uint>.Count;
		ulong* ptr = stackalloc ulong[count / 2];
		for (int i = 0; i < count / 2; i++)
		{
			ptr[i] = source[i];
		}
		ulong* ptr2 = stackalloc ulong[count / 2];
		for (int j = 0; j < count / 2; j++)
		{
			ptr2[j] = source[j + count / 2];
		}
		low = *(Vector<ulong>*)ptr;
		high = *(Vector<ulong>*)ptr2;
	}

	[CLSCompliant(false)]
	[Intrinsic]
	public unsafe static void Widen(Vector<sbyte> source, out Vector<short> low, out Vector<short> high)
	{
		int count = Vector<sbyte>.Count;
		short* ptr = stackalloc short[count / 2];
		for (int i = 0; i < count / 2; i++)
		{
			ptr[i] = source[i];
		}
		short* ptr2 = stackalloc short[count / 2];
		for (int j = 0; j < count / 2; j++)
		{
			ptr2[j] = source[j + count / 2];
		}
		low = *(Vector<short>*)ptr;
		high = *(Vector<short>*)ptr2;
	}

	[Intrinsic]
	public unsafe static void Widen(Vector<short> source, out Vector<int> low, out Vector<int> high)
	{
		int count = Vector<short>.Count;
		int* ptr = stackalloc int[count / 2];
		for (int i = 0; i < count / 2; i++)
		{
			ptr[i] = source[i];
		}
		int* ptr2 = stackalloc int[count / 2];
		for (int j = 0; j < count / 2; j++)
		{
			ptr2[j] = source[j + count / 2];
		}
		low = *(Vector<int>*)ptr;
		high = *(Vector<int>*)ptr2;
	}

	[Intrinsic]
	public unsafe static void Widen(Vector<int> source, out Vector<long> low, out Vector<long> high)
	{
		int count = Vector<int>.Count;
		long* ptr = stackalloc long[count / 2];
		for (int i = 0; i < count / 2; i++)
		{
			ptr[i] = source[i];
		}
		long* ptr2 = stackalloc long[count / 2];
		for (int j = 0; j < count / 2; j++)
		{
			ptr2[j] = source[j + count / 2];
		}
		low = *(Vector<long>*)ptr;
		high = *(Vector<long>*)ptr2;
	}

	[Intrinsic]
	public unsafe static void Widen(Vector<float> source, out Vector<double> low, out Vector<double> high)
	{
		int count = Vector<float>.Count;
		double* ptr = stackalloc double[count / 2];
		for (int i = 0; i < count / 2; i++)
		{
			ptr[i] = source[i];
		}
		double* ptr2 = stackalloc double[count / 2];
		for (int j = 0; j < count / 2; j++)
		{
			ptr2[j] = source[j + count / 2];
		}
		low = *(Vector<double>*)ptr;
		high = *(Vector<double>*)ptr2;
	}

	[CLSCompliant(false)]
	[Intrinsic]
	public unsafe static Vector<byte> Narrow(Vector<ushort> low, Vector<ushort> high)
	{
		int count = Vector<byte>.Count;
		byte* ptr = stackalloc byte[(int)(uint)count];
		for (int i = 0; i < count / 2; i++)
		{
			ptr[i] = (byte)low[i];
		}
		for (int j = 0; j < count / 2; j++)
		{
			ptr[j + count / 2] = (byte)high[j];
		}
		return *(Vector<byte>*)ptr;
	}

	[CLSCompliant(false)]
	[Intrinsic]
	public unsafe static Vector<ushort> Narrow(Vector<uint> low, Vector<uint> high)
	{
		int count = Vector<ushort>.Count;
		ushort* ptr = stackalloc ushort[count];
		for (int i = 0; i < count / 2; i++)
		{
			ptr[i] = (ushort)low[i];
		}
		for (int j = 0; j < count / 2; j++)
		{
			ptr[j + count / 2] = (ushort)high[j];
		}
		return *(Vector<ushort>*)ptr;
	}

	[CLSCompliant(false)]
	[Intrinsic]
	public unsafe static Vector<uint> Narrow(Vector<ulong> low, Vector<ulong> high)
	{
		int count = Vector<uint>.Count;
		uint* ptr = stackalloc uint[count];
		for (int i = 0; i < count / 2; i++)
		{
			ptr[i] = (uint)low[i];
		}
		for (int j = 0; j < count / 2; j++)
		{
			ptr[j + count / 2] = (uint)high[j];
		}
		return *(Vector<uint>*)ptr;
	}

	[CLSCompliant(false)]
	[Intrinsic]
	public unsafe static Vector<sbyte> Narrow(Vector<short> low, Vector<short> high)
	{
		int count = Vector<sbyte>.Count;
		sbyte* ptr = stackalloc sbyte[(int)(uint)count];
		for (int i = 0; i < count / 2; i++)
		{
			ptr[i] = (sbyte)low[i];
		}
		for (int j = 0; j < count / 2; j++)
		{
			ptr[j + count / 2] = (sbyte)high[j];
		}
		return *(Vector<sbyte>*)ptr;
	}

	[Intrinsic]
	public unsafe static Vector<short> Narrow(Vector<int> low, Vector<int> high)
	{
		int count = Vector<short>.Count;
		short* ptr = stackalloc short[count];
		for (int i = 0; i < count / 2; i++)
		{
			ptr[i] = (short)low[i];
		}
		for (int j = 0; j < count / 2; j++)
		{
			ptr[j + count / 2] = (short)high[j];
		}
		return *(Vector<short>*)ptr;
	}

	[Intrinsic]
	public unsafe static Vector<int> Narrow(Vector<long> low, Vector<long> high)
	{
		int count = Vector<int>.Count;
		int* ptr = stackalloc int[count];
		for (int i = 0; i < count / 2; i++)
		{
			ptr[i] = (int)low[i];
		}
		for (int j = 0; j < count / 2; j++)
		{
			ptr[j + count / 2] = (int)high[j];
		}
		return *(Vector<int>*)ptr;
	}

	[Intrinsic]
	public unsafe static Vector<float> Narrow(Vector<double> low, Vector<double> high)
	{
		int count = Vector<float>.Count;
		float* ptr = stackalloc float[count];
		for (int i = 0; i < count / 2; i++)
		{
			ptr[i] = (float)low[i];
		}
		for (int j = 0; j < count / 2; j++)
		{
			ptr[j + count / 2] = (float)high[j];
		}
		return *(Vector<float>*)ptr;
	}

	[Intrinsic]
	public unsafe static Vector<float> ConvertToSingle(Vector<int> value)
	{
		int count = Vector<float>.Count;
		float* ptr = stackalloc float[count];
		for (int i = 0; i < count; i++)
		{
			ptr[i] = value[i];
		}
		return *(Vector<float>*)ptr;
	}

	[CLSCompliant(false)]
	[Intrinsic]
	public unsafe static Vector<float> ConvertToSingle(Vector<uint> value)
	{
		int count = Vector<float>.Count;
		float* ptr = stackalloc float[count];
		for (int i = 0; i < count; i++)
		{
			ptr[i] = value[i];
		}
		return *(Vector<float>*)ptr;
	}

	[Intrinsic]
	public unsafe static Vector<double> ConvertToDouble(Vector<long> value)
	{
		int count = Vector<double>.Count;
		double* ptr = stackalloc double[count];
		for (int i = 0; i < count; i++)
		{
			ptr[i] = value[i];
		}
		return *(Vector<double>*)ptr;
	}

	[CLSCompliant(false)]
	[Intrinsic]
	public unsafe static Vector<double> ConvertToDouble(Vector<ulong> value)
	{
		int count = Vector<double>.Count;
		double* ptr = stackalloc double[count];
		for (int i = 0; i < count; i++)
		{
			ptr[i] = value[i];
		}
		return *(Vector<double>*)ptr;
	}

	[Intrinsic]
	public unsafe static Vector<int> ConvertToInt32(Vector<float> value)
	{
		int count = Vector<int>.Count;
		int* ptr = stackalloc int[count];
		for (int i = 0; i < count; i++)
		{
			ptr[i] = (int)value[i];
		}
		return *(Vector<int>*)ptr;
	}

	[CLSCompliant(false)]
	[Intrinsic]
	public unsafe static Vector<uint> ConvertToUInt32(Vector<float> value)
	{
		int count = Vector<uint>.Count;
		uint* ptr = stackalloc uint[count];
		for (int i = 0; i < count; i++)
		{
			ptr[i] = (uint)value[i];
		}
		return *(Vector<uint>*)ptr;
	}

	[Intrinsic]
	public unsafe static Vector<long> ConvertToInt64(Vector<double> value)
	{
		int count = Vector<long>.Count;
		long* ptr = stackalloc long[count];
		for (int i = 0; i < count; i++)
		{
			ptr[i] = (long)value[i];
		}
		return *(Vector<long>*)ptr;
	}

	[CLSCompliant(false)]
	[Intrinsic]
	public unsafe static Vector<ulong> ConvertToUInt64(Vector<double> value)
	{
		int count = Vector<ulong>.Count;
		ulong* ptr = stackalloc ulong[count];
		for (int i = 0; i < count; i++)
		{
			ptr[i] = (ulong)value[i];
		}
		return *(Vector<ulong>*)ptr;
	}

	[DoesNotReturn]
	internal static void ThrowInsufficientNumberOfElementsException(int requiredElementCount)
	{
		throw new IndexOutOfRangeException(SR.Format(SR.Arg_InsufficientNumberOfElements, requiredElementCount, "values"));
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector<TTo> As<TFrom, TTo>(this Vector<TFrom> vector) where TFrom : struct where TTo : struct
	{
		ThrowHelper.ThrowForUnsupportedNumericsVectorBaseType<TFrom>();
		ThrowHelper.ThrowForUnsupportedNumericsVectorBaseType<TTo>();
		return Unsafe.As<Vector<TFrom>, Vector<TTo>>(ref vector);
	}

	[Intrinsic]
	public static T Sum<T>(Vector<T> value) where T : struct
	{
		return Vector<T>.Sum(value);
	}
}
[Intrinsic]
public struct Vector<T> : IEquatable<Vector<T>>, IFormattable where T : struct
{
	private Register register;

	public static int Count
	{
		[Intrinsic]
		get
		{
			ThrowHelper.ThrowForUnsupportedNumericsVectorBaseType<T>();
			return Unsafe.SizeOf<Vector<T>>() / Unsafe.SizeOf<T>();
		}
	}

	public static Vector<T> Zero
	{
		[Intrinsic]
		get
		{
			ThrowHelper.ThrowForUnsupportedNumericsVectorBaseType<T>();
			return default(Vector<T>);
		}
	}

	public static Vector<T> One
	{
		[Intrinsic]
		get
		{
			ThrowHelper.ThrowForUnsupportedNumericsVectorBaseType<T>();
			return new Vector<T>(GetOneValue());
		}
	}

	internal static Vector<T> AllBitsSet
	{
		[Intrinsic]
		get
		{
			ThrowHelper.ThrowForUnsupportedNumericsVectorBaseType<T>();
			return new Vector<T>(GetAllBitsSetValue());
		}
	}

	public readonly T this[int index]
	{
		[Intrinsic]
		get
		{
			ThrowHelper.ThrowForUnsupportedNumericsVectorBaseType<T>();
			if ((uint)index >= (uint)Count)
			{
				ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.index);
			}
			return GetElement(index);
		}
	}

	[Intrinsic]
	public Vector(T value)
	{
		ThrowHelper.ThrowForUnsupportedNumericsVectorBaseType<T>();
		Unsafe.SkipInit<Vector<T>>(out this);
		for (nint num = 0; num < Count; num++)
		{
			SetElement(num, value);
		}
	}

	[Intrinsic]
	public Vector(T[] values)
		: this(values, 0)
	{
	}

	[Intrinsic]
	public Vector(T[] values, int index)
	{
		ThrowHelper.ThrowForUnsupportedNumericsVectorBaseType<T>();
		if (values == null)
		{
			throw new NullReferenceException(SR.Arg_NullArgumentNullRef);
		}
		if (index < 0 || values.Length - index < Count)
		{
			Vector.ThrowInsufficientNumberOfElementsException(Count);
		}
		this = Unsafe.ReadUnaligned<Vector<T>>(ref Unsafe.As<T, byte>(ref values[index]));
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Vector(ReadOnlySpan<byte> values)
	{
		ThrowHelper.ThrowForUnsupportedNumericsVectorBaseType<T>();
		if (values.Length < Vector<byte>.Count)
		{
			Vector.ThrowInsufficientNumberOfElementsException(Vector<byte>.Count);
		}
		this = Unsafe.ReadUnaligned<Vector<T>>(ref MemoryMarshal.GetReference(values));
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Vector(ReadOnlySpan<T> values)
	{
		ThrowHelper.ThrowForUnsupportedNumericsVectorBaseType<T>();
		if (values.Length < Count)
		{
			Vector.ThrowInsufficientNumberOfElementsException(Count);
		}
		this = Unsafe.ReadUnaligned<Vector<T>>(ref Unsafe.As<T, byte>(ref MemoryMarshal.GetReference(values)));
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Vector(Span<T> values)
		: this((ReadOnlySpan<T>)values)
	{
	}

	public readonly void CopyTo(Span<byte> destination)
	{
		ThrowHelper.ThrowForUnsupportedNumericsVectorBaseType<T>();
		if ((uint)destination.Length < (uint)Vector<byte>.Count)
		{
			ThrowHelper.ThrowArgumentException_DestinationTooShort();
		}
		Unsafe.WriteUnaligned(ref MemoryMarshal.GetReference(destination), this);
	}

	public readonly void CopyTo(Span<T> destination)
	{
		ThrowHelper.ThrowForUnsupportedNumericsVectorBaseType<T>();
		if ((uint)destination.Length < (uint)Count)
		{
			ThrowHelper.ThrowArgumentException_DestinationTooShort();
		}
		Unsafe.WriteUnaligned(ref Unsafe.As<T, byte>(ref MemoryMarshal.GetReference(destination)), this);
	}

	[Intrinsic]
	public readonly void CopyTo(T[] destination)
	{
		CopyTo(destination, 0);
	}

	[Intrinsic]
	public readonly void CopyTo(T[] destination, int startIndex)
	{
		ThrowHelper.ThrowForUnsupportedNumericsVectorBaseType<T>();
		if (destination == null)
		{
			throw new NullReferenceException(SR.Arg_NullArgumentNullRef);
		}
		if ((uint)startIndex >= (uint)destination.Length)
		{
			throw new ArgumentOutOfRangeException("startIndex", SR.Format(SR.Arg_ArgumentOutOfRangeException, startIndex));
		}
		if (destination.Length - startIndex < Count)
		{
			throw new ArgumentException(SR.Format(SR.Arg_ElementsInSourceIsGreaterThanDestination, startIndex));
		}
		Unsafe.WriteUnaligned(ref Unsafe.As<T, byte>(ref destination[startIndex]), this);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public override readonly bool Equals([NotNullWhen(true)] object? obj)
	{
		if (obj is Vector<T> other)
		{
			return Equals(other);
		}
		return false;
	}

	[Intrinsic]
	public readonly bool Equals(Vector<T> other)
	{
		return this == other;
	}

	public override readonly int GetHashCode()
	{
		ThrowHelper.ThrowForUnsupportedNumericsVectorBaseType<T>();
		HashCode hashCode = default(HashCode);
		for (nint num = 0; num < Count; num++)
		{
			hashCode.Add(GetElement(num));
		}
		return hashCode.ToHashCode();
	}

	public override readonly string ToString()
	{
		return ToString("G", CultureInfo.CurrentCulture);
	}

	public readonly string ToString(string? format)
	{
		return ToString(format, CultureInfo.CurrentCulture);
	}

	public readonly string ToString(string? format, IFormatProvider? formatProvider)
	{
		ThrowHelper.ThrowForUnsupportedNumericsVectorBaseType<T>();
		StringBuilder stringBuilder = new StringBuilder();
		string numberGroupSeparator = NumberFormatInfo.GetInstance(formatProvider).NumberGroupSeparator;
		stringBuilder.Append('<');
		for (int i = 0; i < Count - 1; i++)
		{
			stringBuilder.Append(((IFormattable)(object)GetElement(i)).ToString(format, formatProvider));
			stringBuilder.Append(numberGroupSeparator);
			stringBuilder.Append(' ');
		}
		stringBuilder.Append(((IFormattable)(object)GetElement(Count - 1)).ToString(format, formatProvider));
		stringBuilder.Append('>');
		return stringBuilder.ToString();
	}

	public readonly bool TryCopyTo(Span<byte> destination)
	{
		ThrowHelper.ThrowForUnsupportedNumericsVectorBaseType<T>();
		if ((uint)destination.Length < (uint)Vector<byte>.Count)
		{
			return false;
		}
		Unsafe.WriteUnaligned(ref MemoryMarshal.GetReference(destination), this);
		return true;
	}

	public readonly bool TryCopyTo(Span<T> destination)
	{
		ThrowHelper.ThrowForUnsupportedNumericsVectorBaseType<T>();
		if ((uint)destination.Length < (uint)Count)
		{
			return false;
		}
		Unsafe.WriteUnaligned(ref Unsafe.As<T, byte>(ref MemoryMarshal.GetReference(destination)), this);
		return true;
	}

	[Intrinsic]
	public static Vector<T> operator +(Vector<T> left, Vector<T> right)
	{
		Vector<T> result = default(Vector<T>);
		for (nint num = 0; num < Count; num++)
		{
			result.SetElement(num, ScalarAdd(left.GetElement(num), right.GetElement(num)));
		}
		return result;
	}

	[Intrinsic]
	public static Vector<T> operator -(Vector<T> left, Vector<T> right)
	{
		Vector<T> result = default(Vector<T>);
		for (nint num = 0; num < Count; num++)
		{
			result.SetElement(num, ScalarSubtract(left.GetElement(num), right.GetElement(num)));
		}
		return result;
	}

	[Intrinsic]
	public static Vector<T> operator *(Vector<T> left, Vector<T> right)
	{
		Vector<T> result = default(Vector<T>);
		for (nint num = 0; num < Count; num++)
		{
			result.SetElement(num, ScalarMultiply(left.GetElement(num), right.GetElement(num)));
		}
		return result;
	}

	[Intrinsic]
	public static Vector<T> operator *(Vector<T> value, T factor)
	{
		Vector<T> result = default(Vector<T>);
		for (nint num = 0; num < Count; num++)
		{
			result.SetElement(num, ScalarMultiply(value.GetElement(num), factor));
		}
		return result;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static Vector<T> operator *(T factor, Vector<T> value)
	{
		return value * factor;
	}

	[Intrinsic]
	public static Vector<T> operator /(Vector<T> left, Vector<T> right)
	{
		Vector<T> result = default(Vector<T>);
		for (nint num = 0; num < Count; num++)
		{
			result.SetElement(num, ScalarDivide(left.GetElement(num), right.GetElement(num)));
		}
		return result;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Vector<T> operator -(Vector<T> value)
	{
		return Zero - value;
	}

	[Intrinsic]
	public static Vector<T> operator &(Vector<T> left, Vector<T> right)
	{
		ThrowHelper.ThrowForUnsupportedNumericsVectorBaseType<T>();
		Vector<T> result = default(Vector<T>);
		result.register.uint64_0 = left.register.uint64_0 & right.register.uint64_0;
		result.register.uint64_1 = left.register.uint64_1 & right.register.uint64_1;
		return result;
	}

	[Intrinsic]
	public static Vector<T> operator |(Vector<T> left, Vector<T> right)
	{
		ThrowHelper.ThrowForUnsupportedNumericsVectorBaseType<T>();
		Vector<T> result = default(Vector<T>);
		result.register.uint64_0 = left.register.uint64_0 | right.register.uint64_0;
		result.register.uint64_1 = left.register.uint64_1 | right.register.uint64_1;
		return result;
	}

	[Intrinsic]
	public static Vector<T> operator ^(Vector<T> left, Vector<T> right)
	{
		ThrowHelper.ThrowForUnsupportedNumericsVectorBaseType<T>();
		Vector<T> result = default(Vector<T>);
		result.register.uint64_0 = left.register.uint64_0 ^ right.register.uint64_0;
		result.register.uint64_1 = left.register.uint64_1 ^ right.register.uint64_1;
		return result;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Vector<T> operator ~(Vector<T> value)
	{
		return AllBitsSet ^ value;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static bool operator ==(Vector<T> left, Vector<T> right)
	{
		for (nint num = 0; num < Count; num++)
		{
			if (!ScalarEquals(left.GetElement(num), right.GetElement(num)))
			{
				return false;
			}
		}
		return true;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static bool operator !=(Vector<T> left, Vector<T> right)
	{
		return !(left == right);
	}

	[Intrinsic]
	public static explicit operator Vector<byte>(Vector<T> value)
	{
		ThrowHelper.ThrowForUnsupportedNumericsVectorBaseType<T>();
		return Unsafe.As<Vector<T>, Vector<byte>>(ref value);
	}

	[CLSCompliant(false)]
	[Intrinsic]
	public static explicit operator Vector<sbyte>(Vector<T> value)
	{
		ThrowHelper.ThrowForUnsupportedNumericsVectorBaseType<T>();
		return Unsafe.As<Vector<T>, Vector<sbyte>>(ref value);
	}

	[CLSCompliant(false)]
	[Intrinsic]
	public static explicit operator Vector<ushort>(Vector<T> value)
	{
		ThrowHelper.ThrowForUnsupportedNumericsVectorBaseType<T>();
		return Unsafe.As<Vector<T>, Vector<ushort>>(ref value);
	}

	[Intrinsic]
	public static explicit operator Vector<short>(Vector<T> value)
	{
		ThrowHelper.ThrowForUnsupportedNumericsVectorBaseType<T>();
		return Unsafe.As<Vector<T>, Vector<short>>(ref value);
	}

	[CLSCompliant(false)]
	[Intrinsic]
	public static explicit operator Vector<uint>(Vector<T> value)
	{
		ThrowHelper.ThrowForUnsupportedNumericsVectorBaseType<T>();
		return Unsafe.As<Vector<T>, Vector<uint>>(ref value);
	}

	[Intrinsic]
	public static explicit operator Vector<int>(Vector<T> value)
	{
		ThrowHelper.ThrowForUnsupportedNumericsVectorBaseType<T>();
		return Unsafe.As<Vector<T>, Vector<int>>(ref value);
	}

	[CLSCompliant(false)]
	[Intrinsic]
	public static explicit operator Vector<ulong>(Vector<T> value)
	{
		ThrowHelper.ThrowForUnsupportedNumericsVectorBaseType<T>();
		return Unsafe.As<Vector<T>, Vector<ulong>>(ref value);
	}

	[Intrinsic]
	public static explicit operator Vector<long>(Vector<T> value)
	{
		ThrowHelper.ThrowForUnsupportedNumericsVectorBaseType<T>();
		return Unsafe.As<Vector<T>, Vector<long>>(ref value);
	}

	[Intrinsic]
	public static explicit operator Vector<float>(Vector<T> value)
	{
		ThrowHelper.ThrowForUnsupportedNumericsVectorBaseType<T>();
		return Unsafe.As<Vector<T>, Vector<float>>(ref value);
	}

	[Intrinsic]
	public static explicit operator Vector<double>(Vector<T> value)
	{
		ThrowHelper.ThrowForUnsupportedNumericsVectorBaseType<T>();
		return Unsafe.As<Vector<T>, Vector<double>>(ref value);
	}

	[CLSCompliant(false)]
	[Intrinsic]
	public static explicit operator Vector<nuint>(Vector<T> value)
	{
		ThrowHelper.ThrowForUnsupportedNumericsVectorBaseType<T>();
		return Unsafe.As<Vector<T>, Vector<UIntPtr>>(ref value);
	}

	[Intrinsic]
	public static explicit operator Vector<nint>(Vector<T> value)
	{
		ThrowHelper.ThrowForUnsupportedNumericsVectorBaseType<T>();
		return Unsafe.As<Vector<T>, Vector<IntPtr>>(ref value);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	internal static Vector<T> Equals(Vector<T> left, Vector<T> right)
	{
		Vector<T> result = default(Vector<T>);
		for (nint num = 0; num < Count; num++)
		{
			T value = (ScalarEquals(left.GetElement(num), right.GetElement(num)) ? GetAllBitsSetValue() : default(T));
			result.SetElement(num, value);
		}
		return result;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	internal static Vector<T> LessThan(Vector<T> left, Vector<T> right)
	{
		Vector<T> result = default(Vector<T>);
		for (nint num = 0; num < Count; num++)
		{
			T value = (ScalarLessThan(left.GetElement(num), right.GetElement(num)) ? GetAllBitsSetValue() : default(T));
			result.SetElement(num, value);
		}
		return result;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	internal static Vector<T> GreaterThan(Vector<T> left, Vector<T> right)
	{
		Vector<T> result = default(Vector<T>);
		for (nint num = 0; num < Count; num++)
		{
			T value = (ScalarGreaterThan(left.GetElement(num), right.GetElement(num)) ? GetAllBitsSetValue() : default(T));
			result.SetElement(num, value);
		}
		return result;
	}

	[Intrinsic]
	internal static Vector<T> GreaterThanOrEqual(Vector<T> left, Vector<T> right)
	{
		Vector<T> result = default(Vector<T>);
		for (nint num = 0; num < Count; num++)
		{
			T value = (ScalarGreaterThanOrEqual(left.GetElement(num), right.GetElement(num)) ? GetAllBitsSetValue() : default(T));
			result.SetElement(num, value);
		}
		return result;
	}

	[Intrinsic]
	internal static Vector<T> LessThanOrEqual(Vector<T> left, Vector<T> right)
	{
		Vector<T> result = default(Vector<T>);
		for (nint num = 0; num < Count; num++)
		{
			T value = (ScalarLessThanOrEqual(left.GetElement(num), right.GetElement(num)) ? GetAllBitsSetValue() : default(T));
			result.SetElement(num, value);
		}
		return result;
	}

	[Intrinsic]
	internal static Vector<T> ConditionalSelect(Vector<T> condition, Vector<T> left, Vector<T> right)
	{
		ThrowHelper.ThrowForUnsupportedNumericsVectorBaseType<T>();
		Vector<T> result = default(Vector<T>);
		result.register.uint64_0 = (left.register.uint64_0 & condition.register.uint64_0) | (right.register.uint64_0 & ~condition.register.uint64_0);
		result.register.uint64_1 = (left.register.uint64_1 & condition.register.uint64_1) | (right.register.uint64_1 & ~condition.register.uint64_1);
		return result;
	}

	[Intrinsic]
	internal static Vector<T> Abs(Vector<T> value)
	{
		if (typeof(T) == typeof(byte))
		{
			return value;
		}
		if (typeof(T) == typeof(ushort))
		{
			return value;
		}
		if (typeof(T) == typeof(uint))
		{
			return value;
		}
		if (typeof(T) == typeof(ulong))
		{
			return value;
		}
		if (typeof(T) == typeof(UIntPtr))
		{
			return value;
		}
		Vector<T> result = default(Vector<T>);
		for (nint num = 0; num < Count; num++)
		{
			result.SetElement(num, ScalarAbs(value.GetElement(num)));
		}
		return result;
	}

	[Intrinsic]
	internal static Vector<T> Min(Vector<T> left, Vector<T> right)
	{
		Vector<T> result = default(Vector<T>);
		for (nint num = 0; num < Count; num++)
		{
			T value = (ScalarLessThan(left.GetElement(num), right.GetElement(num)) ? left.GetElement(num) : right.GetElement(num));
			result.SetElement(num, value);
		}
		return result;
	}

	[Intrinsic]
	internal static Vector<T> Max(Vector<T> left, Vector<T> right)
	{
		Vector<T> result = default(Vector<T>);
		for (nint num = 0; num < Count; num++)
		{
			T value = (ScalarGreaterThan(left.GetElement(num), right.GetElement(num)) ? left.GetElement(num) : right.GetElement(num));
			result.SetElement(num, value);
		}
		return result;
	}

	[Intrinsic]
	internal static T Dot(Vector<T> left, Vector<T> right)
	{
		T val = default(T);
		for (nint num = 0; num < Count; num++)
		{
			val = ScalarAdd(val, ScalarMultiply(left.GetElement(num), right.GetElement(num)));
		}
		return val;
	}

	[Intrinsic]
	internal static T Sum(Vector<T> value)
	{
		T val = default(T);
		for (nint num = 0; num < Count; num++)
		{
			val = ScalarAdd(val, value.GetElement(num));
		}
		return val;
	}

	[Intrinsic]
	internal static Vector<T> SquareRoot(Vector<T> value)
	{
		Vector<T> result = default(Vector<T>);
		for (nint num = 0; num < Count; num++)
		{
			result.SetElement(num, ScalarSqrt(value.GetElement(num)));
		}
		return result;
	}

	[Intrinsic]
	internal static Vector<T> Ceiling(Vector<T> value)
	{
		Vector<T> result = default(Vector<T>);
		for (nint num = 0; num < Count; num++)
		{
			result.SetElement(num, ScalarCeiling(value.GetElement(num)));
		}
		return result;
	}

	[Intrinsic]
	internal static Vector<T> Floor(Vector<T> value)
	{
		Vector<T> result = default(Vector<T>);
		for (nint num = 0; num < Count; num++)
		{
			result.SetElement(num, ScalarFloor(value.GetElement(num)));
		}
		return result;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static bool ScalarEquals(T left, T right)
	{
		if (typeof(T) == typeof(byte))
		{
			return (byte)(object)left == (byte)(object)right;
		}
		if (typeof(T) == typeof(sbyte))
		{
			return (sbyte)(object)left == (sbyte)(object)right;
		}
		if (typeof(T) == typeof(ushort))
		{
			return (ushort)(object)left == (ushort)(object)right;
		}
		if (typeof(T) == typeof(short))
		{
			return (short)(object)left == (short)(object)right;
		}
		if (typeof(T) == typeof(uint))
		{
			return (uint)(object)left == (uint)(object)right;
		}
		if (typeof(T) == typeof(int))
		{
			return (int)(object)left == (int)(object)right;
		}
		if (typeof(T) == typeof(ulong))
		{
			return (ulong)(object)left == (ulong)(object)right;
		}
		if (typeof(T) == typeof(long))
		{
			return (long)(object)left == (long)(object)right;
		}
		if (typeof(T) == typeof(float))
		{
			return (float)(object)left == (float)(object)right;
		}
		if (typeof(T) == typeof(double))
		{
			return (double)(object)left == (double)(object)right;
		}
		if (typeof(T) == typeof(UIntPtr))
		{
			return (UIntPtr)(object)left == (UIntPtr)(object)right;
		}
		if (typeof(T) == typeof(IntPtr))
		{
			return (IntPtr)(object)left == (IntPtr)(object)right;
		}
		throw new NotSupportedException(SR.Arg_TypeNotSupported);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static bool ScalarLessThan(T left, T right)
	{
		if (typeof(T) == typeof(byte))
		{
			return (byte)(object)left < (byte)(object)right;
		}
		if (typeof(T) == typeof(sbyte))
		{
			return (sbyte)(object)left < (sbyte)(object)right;
		}
		if (typeof(T) == typeof(ushort))
		{
			return (ushort)(object)left < (ushort)(object)right;
		}
		if (typeof(T) == typeof(short))
		{
			return (short)(object)left < (short)(object)right;
		}
		if (typeof(T) == typeof(uint))
		{
			return (uint)(object)left < (uint)(object)right;
		}
		if (typeof(T) == typeof(int))
		{
			return (int)(object)left < (int)(object)right;
		}
		if (typeof(T) == typeof(ulong))
		{
			return (ulong)(object)left < (ulong)(object)right;
		}
		if (typeof(T) == typeof(long))
		{
			return (long)(object)left < (long)(object)right;
		}
		if (typeof(T) == typeof(float))
		{
			return (float)(object)left < (float)(object)right;
		}
		if (typeof(T) == typeof(double))
		{
			return (double)(object)left < (double)(object)right;
		}
		if (typeof(T) == typeof(UIntPtr))
		{
			return (nuint)(UIntPtr)(object)left < (nuint)(UIntPtr)(object)right;
		}
		if (typeof(T) == typeof(IntPtr))
		{
			return (nint)(IntPtr)(object)left < (nint)(IntPtr)(object)right;
		}
		throw new NotSupportedException(SR.Arg_TypeNotSupported);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static bool ScalarLessThanOrEqual(T left, T right)
	{
		if (typeof(T) == typeof(byte))
		{
			return (byte)(object)left <= (byte)(object)right;
		}
		if (typeof(T) == typeof(sbyte))
		{
			return (sbyte)(object)left <= (sbyte)(object)right;
		}
		if (typeof(T) == typeof(ushort))
		{
			return (ushort)(object)left <= (ushort)(object)right;
		}
		if (typeof(T) == typeof(short))
		{
			return (short)(object)left <= (short)(object)right;
		}
		if (typeof(T) == typeof(uint))
		{
			return (uint)(object)left <= (uint)(object)right;
		}
		if (typeof(T) == typeof(int))
		{
			return (int)(object)left <= (int)(object)right;
		}
		if (typeof(T) == typeof(ulong))
		{
			return (ulong)(object)left <= (ulong)(object)right;
		}
		if (typeof(T) == typeof(long))
		{
			return (long)(object)left <= (long)(object)right;
		}
		if (typeof(T) == typeof(float))
		{
			return (float)(object)left <= (float)(object)right;
		}
		if (typeof(T) == typeof(double))
		{
			return (double)(object)left <= (double)(object)right;
		}
		if (typeof(T) == typeof(UIntPtr))
		{
			return (nuint)(UIntPtr)(object)left <= (nuint)(UIntPtr)(object)right;
		}
		if (typeof(T) == typeof(IntPtr))
		{
			return (nint)(IntPtr)(object)left <= (nint)(IntPtr)(object)right;
		}
		throw new NotSupportedException(SR.Arg_TypeNotSupported);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static bool ScalarGreaterThan(T left, T right)
	{
		if (typeof(T) == typeof(byte))
		{
			return (byte)(object)left > (byte)(object)right;
		}
		if (typeof(T) == typeof(sbyte))
		{
			return (sbyte)(object)left > (sbyte)(object)right;
		}
		if (typeof(T) == typeof(ushort))
		{
			return (ushort)(object)left > (ushort)(object)right;
		}
		if (typeof(T) == typeof(short))
		{
			return (short)(object)left > (short)(object)right;
		}
		if (typeof(T) == typeof(uint))
		{
			return (uint)(object)left > (uint)(object)right;
		}
		if (typeof(T) == typeof(int))
		{
			return (int)(object)left > (int)(object)right;
		}
		if (typeof(T) == typeof(ulong))
		{
			return (ulong)(object)left > (ulong)(object)right;
		}
		if (typeof(T) == typeof(long))
		{
			return (long)(object)left > (long)(object)right;
		}
		if (typeof(T) == typeof(float))
		{
			return (float)(object)left > (float)(object)right;
		}
		if (typeof(T) == typeof(double))
		{
			return (double)(object)left > (double)(object)right;
		}
		if (typeof(T) == typeof(UIntPtr))
		{
			return (nuint)(UIntPtr)(object)left > (nuint)(UIntPtr)(object)right;
		}
		if (typeof(T) == typeof(IntPtr))
		{
			return (nint)(IntPtr)(object)left > (nint)(IntPtr)(object)right;
		}
		throw new NotSupportedException(SR.Arg_TypeNotSupported);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static bool ScalarGreaterThanOrEqual(T left, T right)
	{
		if (typeof(T) == typeof(byte))
		{
			return (byte)(object)left >= (byte)(object)right;
		}
		if (typeof(T) == typeof(sbyte))
		{
			return (sbyte)(object)left >= (sbyte)(object)right;
		}
		if (typeof(T) == typeof(ushort))
		{
			return (ushort)(object)left >= (ushort)(object)right;
		}
		if (typeof(T) == typeof(short))
		{
			return (short)(object)left >= (short)(object)right;
		}
		if (typeof(T) == typeof(uint))
		{
			return (uint)(object)left >= (uint)(object)right;
		}
		if (typeof(T) == typeof(int))
		{
			return (int)(object)left >= (int)(object)right;
		}
		if (typeof(T) == typeof(ulong))
		{
			return (ulong)(object)left >= (ulong)(object)right;
		}
		if (typeof(T) == typeof(long))
		{
			return (long)(object)left >= (long)(object)right;
		}
		if (typeof(T) == typeof(float))
		{
			return (float)(object)left >= (float)(object)right;
		}
		if (typeof(T) == typeof(double))
		{
			return (double)(object)left >= (double)(object)right;
		}
		if (typeof(T) == typeof(UIntPtr))
		{
			return (nuint)(UIntPtr)(object)left >= (nuint)(UIntPtr)(object)right;
		}
		if (typeof(T) == typeof(IntPtr))
		{
			return (nint)(IntPtr)(object)left >= (nint)(IntPtr)(object)right;
		}
		throw new NotSupportedException(SR.Arg_TypeNotSupported);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static T ScalarAdd(T left, T right)
	{
		if (typeof(T) == typeof(byte))
		{
			return (T)(object)(byte)((byte)(object)left + (byte)(object)right);
		}
		if (typeof(T) == typeof(sbyte))
		{
			return (T)(object)(sbyte)((sbyte)(object)left + (sbyte)(object)right);
		}
		if (typeof(T) == typeof(ushort))
		{
			return (T)(object)(ushort)((ushort)(object)left + (ushort)(object)right);
		}
		if (typeof(T) == typeof(short))
		{
			return (T)(object)(short)((short)(object)left + (short)(object)right);
		}
		if (typeof(T) == typeof(uint))
		{
			return (T)(object)((uint)(object)left + (uint)(object)right);
		}
		if (typeof(T) == typeof(int))
		{
			return (T)(object)((int)(object)left + (int)(object)right);
		}
		if (typeof(T) == typeof(ulong))
		{
			return (T)(object)((ulong)(object)left + (ulong)(object)right);
		}
		if (typeof(T) == typeof(long))
		{
			return (T)(object)((long)(object)left + (long)(object)right);
		}
		if (typeof(T) == typeof(float))
		{
			return (T)(object)((float)(object)left + (float)(object)right);
		}
		if (typeof(T) == typeof(double))
		{
			return (T)(object)((double)(object)left + (double)(object)right);
		}
		if (typeof(T) == typeof(UIntPtr))
		{
			return (T)(object)(nuint)((nint)(nuint)(UIntPtr)(object)left + (nint)(nuint)(UIntPtr)(object)right);
		}
		if (typeof(T) == typeof(IntPtr))
		{
			return (T)(object)((nint)(IntPtr)(object)left + (nint)(IntPtr)(object)right);
		}
		throw new NotSupportedException(SR.Arg_TypeNotSupported);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static T ScalarSubtract(T left, T right)
	{
		if (typeof(T) == typeof(byte))
		{
			return (T)(object)(byte)((byte)(object)left - (byte)(object)right);
		}
		if (typeof(T) == typeof(sbyte))
		{
			return (T)(object)(sbyte)((sbyte)(object)left - (sbyte)(object)right);
		}
		if (typeof(T) == typeof(ushort))
		{
			return (T)(object)(ushort)((ushort)(object)left - (ushort)(object)right);
		}
		if (typeof(T) == typeof(short))
		{
			return (T)(object)(short)((short)(object)left - (short)(object)right);
		}
		if (typeof(T) == typeof(uint))
		{
			return (T)(object)((uint)(object)left - (uint)(object)right);
		}
		if (typeof(T) == typeof(int))
		{
			return (T)(object)((int)(object)left - (int)(object)right);
		}
		if (typeof(T) == typeof(ulong))
		{
			return (T)(object)((ulong)(object)left - (ulong)(object)right);
		}
		if (typeof(T) == typeof(long))
		{
			return (T)(object)((long)(object)left - (long)(object)right);
		}
		if (typeof(T) == typeof(float))
		{
			return (T)(object)((float)(object)left - (float)(object)right);
		}
		if (typeof(T) == typeof(double))
		{
			return (T)(object)((double)(object)left - (double)(object)right);
		}
		if (typeof(T) == typeof(UIntPtr))
		{
			return (T)(object)(nuint)((nint)(nuint)(UIntPtr)(object)left - (nint)(nuint)(UIntPtr)(object)right);
		}
		if (typeof(T) == typeof(IntPtr))
		{
			return (T)(object)((nint)(IntPtr)(object)left - (nint)(IntPtr)(object)right);
		}
		throw new NotSupportedException(SR.Arg_TypeNotSupported);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static T ScalarMultiply(T left, T right)
	{
		if (typeof(T) == typeof(byte))
		{
			return (T)(object)(byte)((byte)(object)left * (byte)(object)right);
		}
		if (typeof(T) == typeof(sbyte))
		{
			return (T)(object)(sbyte)((sbyte)(object)left * (sbyte)(object)right);
		}
		if (typeof(T) == typeof(ushort))
		{
			return (T)(object)(ushort)((ushort)(object)left * (ushort)(object)right);
		}
		if (typeof(T) == typeof(short))
		{
			return (T)(object)(short)((short)(object)left * (short)(object)right);
		}
		if (typeof(T) == typeof(uint))
		{
			return (T)(object)((uint)(object)left * (uint)(object)right);
		}
		if (typeof(T) == typeof(int))
		{
			return (T)(object)((int)(object)left * (int)(object)right);
		}
		if (typeof(T) == typeof(ulong))
		{
			return (T)(object)((ulong)(object)left * (ulong)(object)right);
		}
		if (typeof(T) == typeof(long))
		{
			return (T)(object)((long)(object)left * (long)(object)right);
		}
		if (typeof(T) == typeof(float))
		{
			return (T)(object)((float)(object)left * (float)(object)right);
		}
		if (typeof(T) == typeof(double))
		{
			return (T)(object)((double)(object)left * (double)(object)right);
		}
		if (typeof(T) == typeof(UIntPtr))
		{
			return (T)(object)(nuint)((nint)(nuint)(UIntPtr)(object)left * (nint)(nuint)(UIntPtr)(object)right);
		}
		if (typeof(T) == typeof(IntPtr))
		{
			return (T)(object)((nint)(IntPtr)(object)left * (nint)(IntPtr)(object)right);
		}
		throw new NotSupportedException(SR.Arg_TypeNotSupported);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static T ScalarDivide(T left, T right)
	{
		if (typeof(T) == typeof(byte))
		{
			return (T)(object)(byte)((byte)(object)left / (byte)(object)right);
		}
		if (typeof(T) == typeof(sbyte))
		{
			return (T)(object)(sbyte)((sbyte)(object)left / (sbyte)(object)right);
		}
		if (typeof(T) == typeof(ushort))
		{
			return (T)(object)(ushort)((ushort)(object)left / (ushort)(object)right);
		}
		if (typeof(T) == typeof(short))
		{
			return (T)(object)(short)((short)(object)left / (short)(object)right);
		}
		if (typeof(T) == typeof(uint))
		{
			return (T)(object)((uint)(object)left / (uint)(object)right);
		}
		if (typeof(T) == typeof(int))
		{
			return (T)(object)((int)(object)left / (int)(object)right);
		}
		if (typeof(T) == typeof(ulong))
		{
			return (T)(object)((ulong)(object)left / (ulong)(object)right);
		}
		if (typeof(T) == typeof(long))
		{
			return (T)(object)((long)(object)left / (long)(object)right);
		}
		if (typeof(T) == typeof(float))
		{
			return (T)(object)((float)(object)left / (float)(object)right);
		}
		if (typeof(T) == typeof(double))
		{
			return (T)(object)((double)(object)left / (double)(object)right);
		}
		if (typeof(T) == typeof(UIntPtr))
		{
			return (T)(object)((nuint)(UIntPtr)(object)left / (nuint)(UIntPtr)(object)right);
		}
		if (typeof(T) == typeof(IntPtr))
		{
			return (T)(object)((nint)(IntPtr)(object)left / (nint)(IntPtr)(object)right);
		}
		throw new NotSupportedException(SR.Arg_TypeNotSupported);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static T GetOneValue()
	{
		if (typeof(T) == typeof(byte))
		{
			return (T)(object)(byte)1;
		}
		if (typeof(T) == typeof(sbyte))
		{
			return (T)(object)(sbyte)1;
		}
		if (typeof(T) == typeof(ushort))
		{
			return (T)(object)(ushort)1;
		}
		if (typeof(T) == typeof(short))
		{
			return (T)(object)(short)1;
		}
		if (typeof(T) == typeof(uint))
		{
			return (T)(object)1u;
		}
		if (typeof(T) == typeof(int))
		{
			return (T)(object)1;
		}
		if (typeof(T) == typeof(ulong))
		{
			return (T)(object)1uL;
		}
		if (typeof(T) == typeof(long))
		{
			return (T)(object)1L;
		}
		if (typeof(T) == typeof(float))
		{
			return (T)(object)1f;
		}
		if (typeof(T) == typeof(double))
		{
			return (T)(object)1.0;
		}
		if (typeof(T) == typeof(UIntPtr))
		{
			return (T)(object)(nuint)1u;
		}
		if (typeof(T) == typeof(IntPtr))
		{
			return (T)(object)(nint)1;
		}
		throw new NotSupportedException(SR.Arg_TypeNotSupported);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static T GetAllBitsSetValue()
	{
		if (typeof(T) == typeof(byte))
		{
			return (T)(object)byte.MaxValue;
		}
		if (typeof(T) == typeof(sbyte))
		{
			return (T)(object)(sbyte)(-1);
		}
		if (typeof(T) == typeof(ushort))
		{
			return (T)(object)ushort.MaxValue;
		}
		if (typeof(T) == typeof(short))
		{
			return (T)(object)(short)(-1);
		}
		if (typeof(T) == typeof(uint))
		{
			return (T)(object)uint.MaxValue;
		}
		if (typeof(T) == typeof(int))
		{
			return (T)(object)(-1);
		}
		if (typeof(T) == typeof(ulong))
		{
			return (T)(object)ulong.MaxValue;
		}
		if (typeof(T) == typeof(long))
		{
			return (T)(object)(-1L);
		}
		if (typeof(T) == typeof(float))
		{
			return (T)(object)BitConverter.Int32BitsToSingle(-1);
		}
		if (typeof(T) == typeof(double))
		{
			return (T)(object)BitConverter.Int64BitsToDouble(-1L);
		}
		if (typeof(T) == typeof(UIntPtr))
		{
			return (T)(object)UIntPtr.MaxValue;
		}
		if (typeof(T) == typeof(IntPtr))
		{
			return (T)(object)(nint)(-1);
		}
		throw new NotSupportedException(SR.Arg_TypeNotSupported);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static T ScalarAbs(T value)
	{
		if (typeof(T) == typeof(sbyte))
		{
			return (T)(object)Math.Abs((sbyte)(object)value);
		}
		if (typeof(T) == typeof(short))
		{
			return (T)(object)Math.Abs((short)(object)value);
		}
		if (typeof(T) == typeof(int))
		{
			return (T)(object)Math.Abs((int)(object)value);
		}
		if (typeof(T) == typeof(long))
		{
			return (T)(object)Math.Abs((long)(object)value);
		}
		if (typeof(T) == typeof(float))
		{
			return (T)(object)Math.Abs((float)(object)value);
		}
		if (typeof(T) == typeof(double))
		{
			return (T)(object)Math.Abs((double)(object)value);
		}
		if (typeof(T) == typeof(IntPtr))
		{
			return (T)(object)Math.Abs((IntPtr)(object)value);
		}
		throw new NotSupportedException(SR.Arg_TypeNotSupported);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static T ScalarSqrt(T value)
	{
		if (typeof(T) == typeof(byte))
		{
			return (T)(object)(byte)Math.Sqrt((int)(byte)(object)value);
		}
		if (typeof(T) == typeof(sbyte))
		{
			return (T)(object)(sbyte)Math.Sqrt((sbyte)(object)value);
		}
		if (typeof(T) == typeof(ushort))
		{
			return (T)(object)(ushort)Math.Sqrt((int)(ushort)(object)value);
		}
		if (typeof(T) == typeof(short))
		{
			return (T)(object)(short)Math.Sqrt((short)(object)value);
		}
		if (typeof(T) == typeof(uint))
		{
			return (T)(object)(uint)Math.Sqrt((uint)(object)value);
		}
		if (typeof(T) == typeof(int))
		{
			return (T)(object)(int)Math.Sqrt((int)(object)value);
		}
		if (typeof(T) == typeof(ulong))
		{
			return (T)(object)(ulong)Math.Sqrt((ulong)(object)value);
		}
		if (typeof(T) == typeof(long))
		{
			return (T)(object)(long)Math.Sqrt((long)(object)value);
		}
		if (typeof(T) == typeof(float))
		{
			return (T)(object)(float)Math.Sqrt((float)(object)value);
		}
		if (typeof(T) == typeof(double))
		{
			return (T)(object)Math.Sqrt((double)(object)value);
		}
		if (typeof(T) == typeof(UIntPtr))
		{
			return (T)(object)(UIntPtr)Math.Sqrt((nint)(nuint)(UIntPtr)(object)value);
		}
		if (typeof(T) == typeof(IntPtr))
		{
			return (T)(object)(IntPtr)Math.Sqrt((nint)(IntPtr)(object)value);
		}
		throw new NotSupportedException(SR.Arg_TypeNotSupported);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static T ScalarCeiling(T value)
	{
		if (typeof(T) == typeof(float))
		{
			return (T)(object)MathF.Ceiling((float)(object)value);
		}
		if (typeof(T) == typeof(double))
		{
			return (T)(object)Math.Ceiling((double)(object)value);
		}
		throw new NotSupportedException(SR.Arg_TypeNotSupported);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static T ScalarFloor(T value)
	{
		if (typeof(T) == typeof(float))
		{
			return (T)(object)MathF.Floor((float)(object)value);
		}
		if (typeof(T) == typeof(double))
		{
			return (T)(object)Math.Floor((double)(object)value);
		}
		throw new NotSupportedException(SR.Arg_TypeNotSupported);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private readonly T GetElement(nint index)
	{
		return Unsafe.Add(ref Unsafe.As<Vector<T>, T>(ref Unsafe.AsRef(in this)), index);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private void SetElement(nint index, T value)
	{
		Unsafe.Add(ref Unsafe.As<Vector<T>, T>(ref Unsafe.AsRef(in this)), index) = value;
	}
}
