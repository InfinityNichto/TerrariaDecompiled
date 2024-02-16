using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace System.Buffers.Binary;

public static class BinaryPrimitives
{
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[CLSCompliant(false)]
	public static sbyte ReverseEndianness(sbyte value)
	{
		return value;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static short ReverseEndianness(short value)
	{
		return (short)ReverseEndianness((ushort)value);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static int ReverseEndianness(int value)
	{
		return (int)ReverseEndianness((uint)value);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	public static long ReverseEndianness(long value)
	{
		return (long)ReverseEndianness((ulong)value);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static byte ReverseEndianness(byte value)
	{
		return value;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[CLSCompliant(false)]
	[Intrinsic]
	public static ushort ReverseEndianness(ushort value)
	{
		return (ushort)((value >> 8) + (value << 8));
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[CLSCompliant(false)]
	[Intrinsic]
	public static uint ReverseEndianness(uint value)
	{
		return BitOperations.RotateRight(value & 0xFF00FFu, 8) + BitOperations.RotateLeft(value & 0xFF00FF00u, 8);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[CLSCompliant(false)]
	[Intrinsic]
	public static ulong ReverseEndianness(ulong value)
	{
		return ((ulong)ReverseEndianness((uint)value) << 32) + ReverseEndianness((uint)(value >> 32));
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static double ReadDoubleBigEndian(ReadOnlySpan<byte> source)
	{
		if (!BitConverter.IsLittleEndian)
		{
		}
		return BitConverter.Int64BitsToDouble(ReverseEndianness(MemoryMarshal.Read<long>(source)));
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Half ReadHalfBigEndian(ReadOnlySpan<byte> source)
	{
		if (!BitConverter.IsLittleEndian)
		{
		}
		return BitConverter.Int16BitsToHalf(ReverseEndianness(MemoryMarshal.Read<short>(source)));
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static short ReadInt16BigEndian(ReadOnlySpan<byte> source)
	{
		short value = MemoryMarshal.Read<short>(source);
		_ = BitConverter.IsLittleEndian;
		return ReverseEndianness(value);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static int ReadInt32BigEndian(ReadOnlySpan<byte> source)
	{
		int value = MemoryMarshal.Read<int>(source);
		_ = BitConverter.IsLittleEndian;
		return ReverseEndianness(value);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static long ReadInt64BigEndian(ReadOnlySpan<byte> source)
	{
		long value = MemoryMarshal.Read<long>(source);
		_ = BitConverter.IsLittleEndian;
		return ReverseEndianness(value);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static float ReadSingleBigEndian(ReadOnlySpan<byte> source)
	{
		if (!BitConverter.IsLittleEndian)
		{
		}
		return BitConverter.Int32BitsToSingle(ReverseEndianness(MemoryMarshal.Read<int>(source)));
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[CLSCompliant(false)]
	public static ushort ReadUInt16BigEndian(ReadOnlySpan<byte> source)
	{
		ushort value = MemoryMarshal.Read<ushort>(source);
		_ = BitConverter.IsLittleEndian;
		return ReverseEndianness(value);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[CLSCompliant(false)]
	public static uint ReadUInt32BigEndian(ReadOnlySpan<byte> source)
	{
		uint value = MemoryMarshal.Read<uint>(source);
		_ = BitConverter.IsLittleEndian;
		return ReverseEndianness(value);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[CLSCompliant(false)]
	public static ulong ReadUInt64BigEndian(ReadOnlySpan<byte> source)
	{
		ulong value = MemoryMarshal.Read<ulong>(source);
		_ = BitConverter.IsLittleEndian;
		return ReverseEndianness(value);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool TryReadDoubleBigEndian(ReadOnlySpan<byte> source, out double value)
	{
		_ = BitConverter.IsLittleEndian;
		long value2;
		bool result = MemoryMarshal.TryRead<long>(source, out value2);
		value = BitConverter.Int64BitsToDouble(ReverseEndianness(value2));
		return result;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool TryReadHalfBigEndian(ReadOnlySpan<byte> source, out Half value)
	{
		_ = BitConverter.IsLittleEndian;
		short value2;
		bool result = MemoryMarshal.TryRead<short>(source, out value2);
		value = BitConverter.Int16BitsToHalf(ReverseEndianness(value2));
		return result;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool TryReadInt16BigEndian(ReadOnlySpan<byte> source, out short value)
	{
		_ = BitConverter.IsLittleEndian;
		short value2;
		bool result = MemoryMarshal.TryRead<short>(source, out value2);
		value = ReverseEndianness(value2);
		return result;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool TryReadInt32BigEndian(ReadOnlySpan<byte> source, out int value)
	{
		_ = BitConverter.IsLittleEndian;
		int value2;
		bool result = MemoryMarshal.TryRead<int>(source, out value2);
		value = ReverseEndianness(value2);
		return result;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool TryReadInt64BigEndian(ReadOnlySpan<byte> source, out long value)
	{
		_ = BitConverter.IsLittleEndian;
		long value2;
		bool result = MemoryMarshal.TryRead<long>(source, out value2);
		value = ReverseEndianness(value2);
		return result;
	}

	public static bool TryReadSingleBigEndian(ReadOnlySpan<byte> source, out float value)
	{
		_ = BitConverter.IsLittleEndian;
		int value2;
		bool result = MemoryMarshal.TryRead<int>(source, out value2);
		value = BitConverter.Int32BitsToSingle(ReverseEndianness(value2));
		return result;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[CLSCompliant(false)]
	public static bool TryReadUInt16BigEndian(ReadOnlySpan<byte> source, out ushort value)
	{
		_ = BitConverter.IsLittleEndian;
		ushort value2;
		bool result = MemoryMarshal.TryRead<ushort>(source, out value2);
		value = ReverseEndianness(value2);
		return result;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[CLSCompliant(false)]
	public static bool TryReadUInt32BigEndian(ReadOnlySpan<byte> source, out uint value)
	{
		_ = BitConverter.IsLittleEndian;
		uint value2;
		bool result = MemoryMarshal.TryRead<uint>(source, out value2);
		value = ReverseEndianness(value2);
		return result;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[CLSCompliant(false)]
	public static bool TryReadUInt64BigEndian(ReadOnlySpan<byte> source, out ulong value)
	{
		_ = BitConverter.IsLittleEndian;
		ulong value2;
		bool result = MemoryMarshal.TryRead<ulong>(source, out value2);
		value = ReverseEndianness(value2);
		return result;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static double ReadDoubleLittleEndian(ReadOnlySpan<byte> source)
	{
		_ = BitConverter.IsLittleEndian;
		return MemoryMarshal.Read<double>(source);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Half ReadHalfLittleEndian(ReadOnlySpan<byte> source)
	{
		_ = BitConverter.IsLittleEndian;
		return MemoryMarshal.Read<Half>(source);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static short ReadInt16LittleEndian(ReadOnlySpan<byte> source)
	{
		short result = MemoryMarshal.Read<short>(source);
		if (!BitConverter.IsLittleEndian)
		{
		}
		return result;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static int ReadInt32LittleEndian(ReadOnlySpan<byte> source)
	{
		int result = MemoryMarshal.Read<int>(source);
		if (!BitConverter.IsLittleEndian)
		{
		}
		return result;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static long ReadInt64LittleEndian(ReadOnlySpan<byte> source)
	{
		long result = MemoryMarshal.Read<long>(source);
		if (!BitConverter.IsLittleEndian)
		{
		}
		return result;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static float ReadSingleLittleEndian(ReadOnlySpan<byte> source)
	{
		_ = BitConverter.IsLittleEndian;
		return MemoryMarshal.Read<float>(source);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[CLSCompliant(false)]
	public static ushort ReadUInt16LittleEndian(ReadOnlySpan<byte> source)
	{
		ushort result = MemoryMarshal.Read<ushort>(source);
		if (!BitConverter.IsLittleEndian)
		{
		}
		return result;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[CLSCompliant(false)]
	public static uint ReadUInt32LittleEndian(ReadOnlySpan<byte> source)
	{
		uint result = MemoryMarshal.Read<uint>(source);
		if (!BitConverter.IsLittleEndian)
		{
		}
		return result;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[CLSCompliant(false)]
	public static ulong ReadUInt64LittleEndian(ReadOnlySpan<byte> source)
	{
		ulong result = MemoryMarshal.Read<ulong>(source);
		if (!BitConverter.IsLittleEndian)
		{
		}
		return result;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool TryReadDoubleLittleEndian(ReadOnlySpan<byte> source, out double value)
	{
		if (!BitConverter.IsLittleEndian)
		{
		}
		return MemoryMarshal.TryRead<double>(source, out value);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool TryReadHalfLittleEndian(ReadOnlySpan<byte> source, out Half value)
	{
		if (!BitConverter.IsLittleEndian)
		{
		}
		return MemoryMarshal.TryRead<Half>(source, out value);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool TryReadInt16LittleEndian(ReadOnlySpan<byte> source, out short value)
	{
		_ = BitConverter.IsLittleEndian;
		return MemoryMarshal.TryRead<short>(source, out value);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool TryReadInt32LittleEndian(ReadOnlySpan<byte> source, out int value)
	{
		_ = BitConverter.IsLittleEndian;
		return MemoryMarshal.TryRead<int>(source, out value);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool TryReadInt64LittleEndian(ReadOnlySpan<byte> source, out long value)
	{
		_ = BitConverter.IsLittleEndian;
		return MemoryMarshal.TryRead<long>(source, out value);
	}

	public static bool TryReadSingleLittleEndian(ReadOnlySpan<byte> source, out float value)
	{
		if (!BitConverter.IsLittleEndian)
		{
		}
		return MemoryMarshal.TryRead<float>(source, out value);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[CLSCompliant(false)]
	public static bool TryReadUInt16LittleEndian(ReadOnlySpan<byte> source, out ushort value)
	{
		_ = BitConverter.IsLittleEndian;
		return MemoryMarshal.TryRead<ushort>(source, out value);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[CLSCompliant(false)]
	public static bool TryReadUInt32LittleEndian(ReadOnlySpan<byte> source, out uint value)
	{
		_ = BitConverter.IsLittleEndian;
		return MemoryMarshal.TryRead<uint>(source, out value);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[CLSCompliant(false)]
	public static bool TryReadUInt64LittleEndian(ReadOnlySpan<byte> source, out ulong value)
	{
		_ = BitConverter.IsLittleEndian;
		return MemoryMarshal.TryRead<ulong>(source, out value);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static void WriteDoubleBigEndian(Span<byte> destination, double value)
	{
		_ = BitConverter.IsLittleEndian;
		long value2 = ReverseEndianness(BitConverter.DoubleToInt64Bits(value));
		MemoryMarshal.Write(destination, ref value2);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static void WriteHalfBigEndian(Span<byte> destination, Half value)
	{
		_ = BitConverter.IsLittleEndian;
		short value2 = ReverseEndianness(BitConverter.HalfToInt16Bits(value));
		MemoryMarshal.Write(destination, ref value2);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static void WriteInt16BigEndian(Span<byte> destination, short value)
	{
		_ = BitConverter.IsLittleEndian;
		value = ReverseEndianness(value);
		MemoryMarshal.Write(destination, ref value);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static void WriteInt32BigEndian(Span<byte> destination, int value)
	{
		_ = BitConverter.IsLittleEndian;
		value = ReverseEndianness(value);
		MemoryMarshal.Write(destination, ref value);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static void WriteInt64BigEndian(Span<byte> destination, long value)
	{
		_ = BitConverter.IsLittleEndian;
		value = ReverseEndianness(value);
		MemoryMarshal.Write(destination, ref value);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static void WriteSingleBigEndian(Span<byte> destination, float value)
	{
		_ = BitConverter.IsLittleEndian;
		int value2 = ReverseEndianness(BitConverter.SingleToInt32Bits(value));
		MemoryMarshal.Write(destination, ref value2);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[CLSCompliant(false)]
	public static void WriteUInt16BigEndian(Span<byte> destination, ushort value)
	{
		_ = BitConverter.IsLittleEndian;
		value = ReverseEndianness(value);
		MemoryMarshal.Write(destination, ref value);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[CLSCompliant(false)]
	public static void WriteUInt32BigEndian(Span<byte> destination, uint value)
	{
		_ = BitConverter.IsLittleEndian;
		value = ReverseEndianness(value);
		MemoryMarshal.Write(destination, ref value);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[CLSCompliant(false)]
	public static void WriteUInt64BigEndian(Span<byte> destination, ulong value)
	{
		_ = BitConverter.IsLittleEndian;
		value = ReverseEndianness(value);
		MemoryMarshal.Write(destination, ref value);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool TryWriteDoubleBigEndian(Span<byte> destination, double value)
	{
		_ = BitConverter.IsLittleEndian;
		long value2 = ReverseEndianness(BitConverter.DoubleToInt64Bits(value));
		return MemoryMarshal.TryWrite(destination, ref value2);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool TryWriteHalfBigEndian(Span<byte> destination, Half value)
	{
		_ = BitConverter.IsLittleEndian;
		short value2 = ReverseEndianness(BitConverter.HalfToInt16Bits(value));
		return MemoryMarshal.TryWrite(destination, ref value2);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool TryWriteInt16BigEndian(Span<byte> destination, short value)
	{
		_ = BitConverter.IsLittleEndian;
		value = ReverseEndianness(value);
		return MemoryMarshal.TryWrite(destination, ref value);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool TryWriteInt32BigEndian(Span<byte> destination, int value)
	{
		_ = BitConverter.IsLittleEndian;
		value = ReverseEndianness(value);
		return MemoryMarshal.TryWrite(destination, ref value);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool TryWriteInt64BigEndian(Span<byte> destination, long value)
	{
		_ = BitConverter.IsLittleEndian;
		value = ReverseEndianness(value);
		return MemoryMarshal.TryWrite(destination, ref value);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool TryWriteSingleBigEndian(Span<byte> destination, float value)
	{
		_ = BitConverter.IsLittleEndian;
		int value2 = ReverseEndianness(BitConverter.SingleToInt32Bits(value));
		return MemoryMarshal.TryWrite(destination, ref value2);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[CLSCompliant(false)]
	public static bool TryWriteUInt16BigEndian(Span<byte> destination, ushort value)
	{
		_ = BitConverter.IsLittleEndian;
		value = ReverseEndianness(value);
		return MemoryMarshal.TryWrite(destination, ref value);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[CLSCompliant(false)]
	public static bool TryWriteUInt32BigEndian(Span<byte> destination, uint value)
	{
		_ = BitConverter.IsLittleEndian;
		value = ReverseEndianness(value);
		return MemoryMarshal.TryWrite(destination, ref value);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[CLSCompliant(false)]
	public static bool TryWriteUInt64BigEndian(Span<byte> destination, ulong value)
	{
		_ = BitConverter.IsLittleEndian;
		value = ReverseEndianness(value);
		return MemoryMarshal.TryWrite(destination, ref value);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static void WriteDoubleLittleEndian(Span<byte> destination, double value)
	{
		if (!BitConverter.IsLittleEndian)
		{
		}
		MemoryMarshal.Write(destination, ref value);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static void WriteHalfLittleEndian(Span<byte> destination, Half value)
	{
		if (!BitConverter.IsLittleEndian)
		{
		}
		MemoryMarshal.Write(destination, ref value);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static void WriteInt16LittleEndian(Span<byte> destination, short value)
	{
		if (!BitConverter.IsLittleEndian)
		{
		}
		MemoryMarshal.Write(destination, ref value);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static void WriteInt32LittleEndian(Span<byte> destination, int value)
	{
		if (!BitConverter.IsLittleEndian)
		{
		}
		MemoryMarshal.Write(destination, ref value);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static void WriteInt64LittleEndian(Span<byte> destination, long value)
	{
		if (!BitConverter.IsLittleEndian)
		{
		}
		MemoryMarshal.Write(destination, ref value);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static void WriteSingleLittleEndian(Span<byte> destination, float value)
	{
		if (!BitConverter.IsLittleEndian)
		{
		}
		MemoryMarshal.Write(destination, ref value);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[CLSCompliant(false)]
	public static void WriteUInt16LittleEndian(Span<byte> destination, ushort value)
	{
		if (!BitConverter.IsLittleEndian)
		{
		}
		MemoryMarshal.Write(destination, ref value);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[CLSCompliant(false)]
	public static void WriteUInt32LittleEndian(Span<byte> destination, uint value)
	{
		if (!BitConverter.IsLittleEndian)
		{
		}
		MemoryMarshal.Write(destination, ref value);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[CLSCompliant(false)]
	public static void WriteUInt64LittleEndian(Span<byte> destination, ulong value)
	{
		if (!BitConverter.IsLittleEndian)
		{
		}
		MemoryMarshal.Write(destination, ref value);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool TryWriteDoubleLittleEndian(Span<byte> destination, double value)
	{
		if (!BitConverter.IsLittleEndian)
		{
		}
		return MemoryMarshal.TryWrite(destination, ref value);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool TryWriteHalfLittleEndian(Span<byte> destination, Half value)
	{
		if (!BitConverter.IsLittleEndian)
		{
		}
		return MemoryMarshal.TryWrite(destination, ref value);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool TryWriteInt16LittleEndian(Span<byte> destination, short value)
	{
		if (!BitConverter.IsLittleEndian)
		{
		}
		return MemoryMarshal.TryWrite(destination, ref value);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool TryWriteInt32LittleEndian(Span<byte> destination, int value)
	{
		if (!BitConverter.IsLittleEndian)
		{
		}
		return MemoryMarshal.TryWrite(destination, ref value);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool TryWriteInt64LittleEndian(Span<byte> destination, long value)
	{
		if (!BitConverter.IsLittleEndian)
		{
		}
		return MemoryMarshal.TryWrite(destination, ref value);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool TryWriteSingleLittleEndian(Span<byte> destination, float value)
	{
		if (!BitConverter.IsLittleEndian)
		{
		}
		return MemoryMarshal.TryWrite(destination, ref value);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[CLSCompliant(false)]
	public static bool TryWriteUInt16LittleEndian(Span<byte> destination, ushort value)
	{
		if (!BitConverter.IsLittleEndian)
		{
		}
		return MemoryMarshal.TryWrite(destination, ref value);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[CLSCompliant(false)]
	public static bool TryWriteUInt32LittleEndian(Span<byte> destination, uint value)
	{
		if (!BitConverter.IsLittleEndian)
		{
		}
		return MemoryMarshal.TryWrite(destination, ref value);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[CLSCompliant(false)]
	public static bool TryWriteUInt64LittleEndian(Span<byte> destination, ulong value)
	{
		if (!BitConverter.IsLittleEndian)
		{
		}
		return MemoryMarshal.TryWrite(destination, ref value);
	}
}
