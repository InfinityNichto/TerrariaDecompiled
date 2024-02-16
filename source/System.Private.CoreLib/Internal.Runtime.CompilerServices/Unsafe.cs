using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.Versioning;

namespace Internal.Runtime.CompilerServices;

[CLSCompliant(false)]
public static class Unsafe
{
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	[NonVersionable]
	public unsafe static void* AsPointer<T>(ref T value)
	{
		throw new PlatformNotSupportedException();
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	[NonVersionable]
	public static int SizeOf<T>()
	{
		typeof(T).ToString();
		throw new PlatformNotSupportedException();
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	[NonVersionable]
	[return: NotNullIfNotNull("value")]
	public static T As<T>(object? value) where T : class?
	{
		throw new PlatformNotSupportedException();
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	[NonVersionable]
	public static ref TTo As<TFrom, TTo>(ref TFrom source)
	{
		throw new PlatformNotSupportedException();
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	[NonVersionable]
	public static ref T Add<T>(ref T source, int elementOffset)
	{
		typeof(T).ToString();
		throw new PlatformNotSupportedException();
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	[NonVersionable]
	public static ref T Add<T>(ref T source, IntPtr elementOffset)
	{
		typeof(T).ToString();
		throw new PlatformNotSupportedException();
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	[NonVersionable]
	public unsafe static void* Add<T>(void* source, int elementOffset)
	{
		typeof(T).ToString();
		throw new PlatformNotSupportedException();
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	[NonVersionable]
	internal unsafe static ref T AddByteOffset<T>(ref T source, nuint byteOffset)
	{
		return ref AddByteOffset(ref source, (IntPtr)(void*)byteOffset);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	[NonVersionable]
	public static bool AreSame<T>([AllowNull] ref T left, [AllowNull] ref T right)
	{
		throw new PlatformNotSupportedException();
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	[NonVersionable]
	public static bool IsAddressGreaterThan<T>([AllowNull] ref T left, [AllowNull] ref T right)
	{
		throw new PlatformNotSupportedException();
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	[NonVersionable]
	public static bool IsAddressLessThan<T>([AllowNull] ref T left, [AllowNull] ref T right)
	{
		throw new PlatformNotSupportedException();
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	[NonVersionable]
	public static void InitBlockUnaligned(ref byte startAddress, byte value, uint byteCount)
	{
		for (uint num = 0u; num < byteCount; num++)
		{
			AddByteOffset(ref startAddress, num) = value;
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	[NonVersionable]
	public unsafe static T ReadUnaligned<T>(void* source)
	{
		typeof(T).ToString();
		throw new PlatformNotSupportedException();
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	[NonVersionable]
	public static T ReadUnaligned<T>(ref byte source)
	{
		typeof(T).ToString();
		throw new PlatformNotSupportedException();
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	[NonVersionable]
	public unsafe static void WriteUnaligned<T>(void* destination, T value)
	{
		typeof(T).ToString();
		throw new PlatformNotSupportedException();
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	[NonVersionable]
	public static void WriteUnaligned<T>(ref byte destination, T value)
	{
		typeof(T).ToString();
		throw new PlatformNotSupportedException();
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	[NonVersionable]
	public static ref T AddByteOffset<T>(ref T source, IntPtr byteOffset)
	{
		throw new PlatformNotSupportedException();
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	[NonVersionable]
	public unsafe static T Read<T>(void* source)
	{
		return As<byte, T>(ref *(byte*)source);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	[NonVersionable]
	public static T Read<T>(ref byte source)
	{
		return As<byte, T>(ref source);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	[NonVersionable]
	public unsafe static void Write<T>(void* destination, T value)
	{
		As<byte, T>(ref *(byte*)destination) = value;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	[NonVersionable]
	public static void Write<T>(ref byte destination, T value)
	{
		As<byte, T>(ref destination) = value;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	[NonVersionable]
	public unsafe static ref T AsRef<T>(void* source)
	{
		return ref As<byte, T>(ref *(byte*)source);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	[NonVersionable]
	public static ref T AsRef<T>(in T source)
	{
		throw new PlatformNotSupportedException();
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	[NonVersionable]
	public static IntPtr ByteOffset<T>([AllowNull] ref T origin, [AllowNull] ref T target)
	{
		throw new PlatformNotSupportedException();
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	[NonVersionable]
	public unsafe static ref T NullRef<T>()
	{
		return ref AsRef<T>(null);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	[NonVersionable]
	public unsafe static bool IsNullRef<T>(ref T source)
	{
		return AsPointer(ref source) == null;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Intrinsic]
	[NonVersionable]
	public static void SkipInit<T>(out T value)
	{
		throw new PlatformNotSupportedException();
	}
}
