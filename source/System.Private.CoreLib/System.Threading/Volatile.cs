using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.Versioning;
using Internal.Runtime.CompilerServices;

namespace System.Threading;

public static class Volatile
{
	private struct VolatileBoolean
	{
		public volatile bool Value;
	}

	private struct VolatileByte
	{
		public volatile byte Value;
	}

	private struct VolatileInt16
	{
		public volatile short Value;
	}

	private struct VolatileInt32
	{
		public volatile int Value;
	}

	private struct VolatileIntPtr
	{
		public volatile IntPtr Value;
	}

	private struct VolatileSByte
	{
		public volatile sbyte Value;
	}

	private struct VolatileSingle
	{
		public volatile float Value;
	}

	private struct VolatileUInt16
	{
		public volatile ushort Value;
	}

	private struct VolatileUInt32
	{
		public volatile uint Value;
	}

	private struct VolatileUIntPtr
	{
		public volatile UIntPtr Value;
	}

	private struct VolatileObject
	{
		public volatile object Value;
	}

	[Intrinsic]
	[NonVersionable]
	public static bool Read(ref bool location)
	{
		return Unsafe.As<bool, VolatileBoolean>(ref location).Value;
	}

	[Intrinsic]
	[NonVersionable]
	public static void Write(ref bool location, bool value)
	{
		Unsafe.As<bool, VolatileBoolean>(ref location).Value = value;
	}

	[Intrinsic]
	[NonVersionable]
	public static byte Read(ref byte location)
	{
		return Unsafe.As<byte, VolatileByte>(ref location).Value;
	}

	[Intrinsic]
	[NonVersionable]
	public static void Write(ref byte location, byte value)
	{
		Unsafe.As<byte, VolatileByte>(ref location).Value = value;
	}

	[Intrinsic]
	[NonVersionable]
	public unsafe static double Read(ref double location)
	{
		long num = Read(ref Unsafe.As<double, long>(ref location));
		return *(double*)(&num);
	}

	[Intrinsic]
	[NonVersionable]
	public unsafe static void Write(ref double location, double value)
	{
		Write(ref Unsafe.As<double, long>(ref location), *(long*)(&value));
	}

	[Intrinsic]
	[NonVersionable]
	public static short Read(ref short location)
	{
		return Unsafe.As<short, VolatileInt16>(ref location).Value;
	}

	[Intrinsic]
	[NonVersionable]
	public static void Write(ref short location, short value)
	{
		Unsafe.As<short, VolatileInt16>(ref location).Value = value;
	}

	[Intrinsic]
	[NonVersionable]
	public static int Read(ref int location)
	{
		return Unsafe.As<int, VolatileInt32>(ref location).Value;
	}

	[Intrinsic]
	[NonVersionable]
	public static void Write(ref int location, int value)
	{
		Unsafe.As<int, VolatileInt32>(ref location).Value = value;
	}

	[Intrinsic]
	[NonVersionable]
	public static long Read(ref long location)
	{
		return (long)Unsafe.As<long, VolatileIntPtr>(ref location).Value;
	}

	[Intrinsic]
	[NonVersionable]
	public static void Write(ref long location, long value)
	{
		Unsafe.As<long, VolatileIntPtr>(ref location).Value = (IntPtr)value;
	}

	[Intrinsic]
	[NonVersionable]
	public static IntPtr Read(ref IntPtr location)
	{
		return Unsafe.As<IntPtr, VolatileIntPtr>(ref location).Value;
	}

	[Intrinsic]
	[NonVersionable]
	public static void Write(ref IntPtr location, IntPtr value)
	{
		Unsafe.As<IntPtr, VolatileIntPtr>(ref location).Value = value;
	}

	[CLSCompliant(false)]
	[Intrinsic]
	[NonVersionable]
	public static sbyte Read(ref sbyte location)
	{
		return Unsafe.As<sbyte, VolatileSByte>(ref location).Value;
	}

	[CLSCompliant(false)]
	[Intrinsic]
	[NonVersionable]
	public static void Write(ref sbyte location, sbyte value)
	{
		Unsafe.As<sbyte, VolatileSByte>(ref location).Value = value;
	}

	[Intrinsic]
	[NonVersionable]
	public static float Read(ref float location)
	{
		return Unsafe.As<float, VolatileSingle>(ref location).Value;
	}

	[Intrinsic]
	[NonVersionable]
	public static void Write(ref float location, float value)
	{
		Unsafe.As<float, VolatileSingle>(ref location).Value = value;
	}

	[CLSCompliant(false)]
	[Intrinsic]
	[NonVersionable]
	public static ushort Read(ref ushort location)
	{
		return Unsafe.As<ushort, VolatileUInt16>(ref location).Value;
	}

	[CLSCompliant(false)]
	[Intrinsic]
	[NonVersionable]
	public static void Write(ref ushort location, ushort value)
	{
		Unsafe.As<ushort, VolatileUInt16>(ref location).Value = value;
	}

	[CLSCompliant(false)]
	[Intrinsic]
	[NonVersionable]
	public static uint Read(ref uint location)
	{
		return Unsafe.As<uint, VolatileUInt32>(ref location).Value;
	}

	[CLSCompliant(false)]
	[Intrinsic]
	[NonVersionable]
	public static void Write(ref uint location, uint value)
	{
		Unsafe.As<uint, VolatileUInt32>(ref location).Value = value;
	}

	[CLSCompliant(false)]
	[Intrinsic]
	[NonVersionable]
	public static ulong Read(ref ulong location)
	{
		return (ulong)Read(ref Unsafe.As<ulong, long>(ref location));
	}

	[CLSCompliant(false)]
	[Intrinsic]
	[NonVersionable]
	public static void Write(ref ulong location, ulong value)
	{
		Write(ref Unsafe.As<ulong, long>(ref location), (long)value);
	}

	[CLSCompliant(false)]
	[Intrinsic]
	[NonVersionable]
	public static UIntPtr Read(ref UIntPtr location)
	{
		return Unsafe.As<UIntPtr, VolatileUIntPtr>(ref location).Value;
	}

	[CLSCompliant(false)]
	[Intrinsic]
	[NonVersionable]
	public static void Write(ref UIntPtr location, UIntPtr value)
	{
		Unsafe.As<UIntPtr, VolatileUIntPtr>(ref location).Value = value;
	}

	[Intrinsic]
	[NonVersionable]
	[return: NotNullIfNotNull("location")]
	public static T Read<T>([NotNullIfNotNull("location")] ref T location) where T : class?
	{
		return Unsafe.As<T>(Unsafe.As<T, VolatileObject>(ref location).Value);
	}

	[Intrinsic]
	[NonVersionable]
	public static void Write<T>([NotNullIfNotNull("value")] ref T location, T value) where T : class?
	{
		Unsafe.As<T, VolatileObject>(ref location).Value = value;
	}
}
