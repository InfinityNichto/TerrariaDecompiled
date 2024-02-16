using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Internal.Runtime.CompilerServices;

namespace System;

public static class Buffer
{
	[StructLayout(LayoutKind.Sequential, Size = 16)]
	private struct Block16
	{
	}

	[StructLayout(LayoutKind.Sequential, Size = 64)]
	private struct Block64
	{
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	internal unsafe static void _ZeroMemory(ref byte b, nuint byteLength)
	{
		fixed (byte* b2 = &b)
		{
			__ZeroMemory(b2, byteLength);
		}
	}

	[DllImport("QCall", CharSet = CharSet.Unicode)]
	private unsafe static extern void __ZeroMemory(void* b, nuint byteLength);

	internal static void BulkMoveWithWriteBarrier(ref byte destination, ref byte source, nuint byteCount)
	{
		if (byteCount <= 16384)
		{
			__BulkMoveWithWriteBarrier(ref destination, ref source, byteCount);
		}
		else
		{
			_BulkMoveWithWriteBarrier(ref destination, ref source, byteCount);
		}
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	private static void _BulkMoveWithWriteBarrier(ref byte destination, ref byte source, nuint byteCount)
	{
		if (Unsafe.AreSame(ref source, ref destination))
		{
			return;
		}
		if ((nuint)(nint)Unsafe.ByteOffset(ref source, ref destination) >= byteCount)
		{
			do
			{
				byteCount -= 16384;
				__BulkMoveWithWriteBarrier(ref destination, ref source, 16384u);
				destination = ref Unsafe.AddByteOffset(ref destination, 16384u);
				source = ref Unsafe.AddByteOffset(ref source, 16384u);
			}
			while (byteCount > 16384);
		}
		else
		{
			do
			{
				byteCount -= 16384;
				__BulkMoveWithWriteBarrier(ref Unsafe.AddByteOffset(ref destination, byteCount), ref Unsafe.AddByteOffset(ref source, byteCount), 16384u);
			}
			while (byteCount > 16384);
		}
		__BulkMoveWithWriteBarrier(ref destination, ref source, byteCount);
	}

	[MethodImpl(MethodImplOptions.InternalCall)]
	private static extern void __BulkMoveWithWriteBarrier(ref byte destination, ref byte source, nuint byteCount);

	[DllImport("QCall", CharSet = CharSet.Unicode)]
	private unsafe static extern void __Memmove(byte* dest, byte* src, nuint len);

	internal unsafe static void Memcpy(byte* dest, byte* src, int len)
	{
		Memmove(ref *dest, ref *src, (uint)len);
	}

	internal unsafe static void Memcpy(byte* pDest, int destIndex, byte[] src, int srcIndex, int len)
	{
		Memmove(ref pDest[(uint)destIndex], ref Unsafe.Add(ref MemoryMarshal.GetArrayDataReference(src), (nint)(uint)srcIndex), (uint)len);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal static void Memmove<T>(ref T destination, ref T source, nuint elementCount)
	{
		if (!RuntimeHelpers.IsReferenceOrContainsReferences<T>())
		{
			Memmove(ref Unsafe.As<T, byte>(ref destination), ref Unsafe.As<T, byte>(ref source), elementCount * (nuint)Unsafe.SizeOf<T>());
		}
		else
		{
			BulkMoveWithWriteBarrier(ref Unsafe.As<T, byte>(ref destination), ref Unsafe.As<T, byte>(ref source), elementCount * (nuint)Unsafe.SizeOf<T>());
		}
	}

	public static void BlockCopy(Array src, int srcOffset, Array dst, int dstOffset, int count)
	{
		if (src == null)
		{
			throw new ArgumentNullException("src");
		}
		if (dst == null)
		{
			throw new ArgumentNullException("dst");
		}
		nuint num = src.NativeLength;
		if (src.GetType() != typeof(byte[]))
		{
			if (!src.GetCorElementTypeOfElementType().IsPrimitiveType())
			{
				throw new ArgumentException(SR.Arg_MustBePrimArray, "src");
			}
			num *= src.GetElementSize();
		}
		nuint num2 = num;
		if (src != dst)
		{
			num2 = dst.NativeLength;
			if (dst.GetType() != typeof(byte[]))
			{
				if (!dst.GetCorElementTypeOfElementType().IsPrimitiveType())
				{
					throw new ArgumentException(SR.Arg_MustBePrimArray, "dst");
				}
				num2 *= dst.GetElementSize();
			}
		}
		if (srcOffset < 0)
		{
			throw new ArgumentOutOfRangeException("srcOffset", SR.ArgumentOutOfRange_MustBeNonNegInt32);
		}
		if (dstOffset < 0)
		{
			throw new ArgumentOutOfRangeException("dstOffset", SR.ArgumentOutOfRange_MustBeNonNegInt32);
		}
		if (count < 0)
		{
			throw new ArgumentOutOfRangeException("count", SR.ArgumentOutOfRange_MustBeNonNegInt32);
		}
		nuint num3 = (nuint)count;
		nuint num4 = (nuint)srcOffset;
		nuint num5 = (nuint)dstOffset;
		if (num < num4 + num3 || num2 < num5 + num3)
		{
			throw new ArgumentException(SR.Argument_InvalidOffLen);
		}
		Memmove(ref Unsafe.AddByteOffset(ref MemoryMarshal.GetArrayDataReference(dst), num5), ref Unsafe.AddByteOffset(ref MemoryMarshal.GetArrayDataReference(src), num4), num3);
	}

	public static int ByteLength(Array array)
	{
		if (array == null)
		{
			throw new ArgumentNullException("array");
		}
		if (!array.GetCorElementTypeOfElementType().IsPrimitiveType())
		{
			throw new ArgumentException(SR.Arg_MustBePrimArray, "array");
		}
		nuint num = array.NativeLength * array.GetElementSize();
		return checked((int)num);
	}

	public static byte GetByte(Array array, int index)
	{
		if ((uint)index >= (uint)ByteLength(array))
		{
			ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.index);
		}
		return Unsafe.Add(ref MemoryMarshal.GetArrayDataReference(array), index);
	}

	public static void SetByte(Array array, int index, byte value)
	{
		if ((uint)index >= (uint)ByteLength(array))
		{
			ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.index);
		}
		Unsafe.Add(ref MemoryMarshal.GetArrayDataReference(array), index) = value;
	}

	internal unsafe static void ZeroMemory(byte* dest, nuint len)
	{
		SpanHelpers.ClearWithoutReferences(ref *dest, len);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[CLSCompliant(false)]
	public unsafe static void MemoryCopy(void* source, void* destination, long destinationSizeInBytes, long sourceBytesToCopy)
	{
		if (sourceBytesToCopy > destinationSizeInBytes)
		{
			ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.sourceBytesToCopy);
		}
		Memmove(ref *(byte*)destination, ref *(byte*)source, checked((nuint)sourceBytesToCopy));
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[CLSCompliant(false)]
	public unsafe static void MemoryCopy(void* source, void* destination, ulong destinationSizeInBytes, ulong sourceBytesToCopy)
	{
		if (sourceBytesToCopy > destinationSizeInBytes)
		{
			ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.sourceBytesToCopy);
		}
		Memmove(ref *(byte*)destination, ref *(byte*)source, checked((nuint)sourceBytesToCopy));
	}

	internal static void Memmove(ref byte dest, ref byte src, nuint len)
	{
		if ((nuint)(nint)Unsafe.ByteOffset(ref src, ref dest) >= len && (nuint)(nint)Unsafe.ByteOffset(ref dest, ref src) >= len)
		{
			ref byte source = ref Unsafe.Add(ref src, (nint)len);
			ref byte source2 = ref Unsafe.Add(ref dest, (nint)len);
			if (len > 16)
			{
				if (len > 64)
				{
					if (len > 2048)
					{
						goto IL_01db;
					}
					nuint num = len >> 6;
					do
					{
						Unsafe.As<byte, Block64>(ref dest) = Unsafe.As<byte, Block64>(ref src);
						dest = ref Unsafe.Add(ref dest, 64);
						src = ref Unsafe.Add(ref src, 64);
						num--;
					}
					while (num != 0);
					len %= 64;
					if (len <= 16)
					{
						Unsafe.As<byte, Block16>(ref Unsafe.Add(ref source2, -16)) = Unsafe.As<byte, Block16>(ref Unsafe.Add(ref source, -16));
						return;
					}
				}
				Unsafe.As<byte, Block16>(ref dest) = Unsafe.As<byte, Block16>(ref src);
				if (len > 32)
				{
					Unsafe.As<byte, Block16>(ref Unsafe.Add(ref dest, 16)) = Unsafe.As<byte, Block16>(ref Unsafe.Add(ref src, 16));
					if (len > 48)
					{
						Unsafe.As<byte, Block16>(ref Unsafe.Add(ref dest, 32)) = Unsafe.As<byte, Block16>(ref Unsafe.Add(ref src, 32));
					}
				}
				Unsafe.As<byte, Block16>(ref Unsafe.Add(ref source2, -16)) = Unsafe.As<byte, Block16>(ref Unsafe.Add(ref source, -16));
			}
			else if ((len & 0x18) != 0)
			{
				Unsafe.As<byte, long>(ref dest) = Unsafe.As<byte, long>(ref src);
				Unsafe.As<byte, long>(ref Unsafe.Add(ref source2, -8)) = Unsafe.As<byte, long>(ref Unsafe.Add(ref source, -8));
			}
			else if ((len & 4) != 0)
			{
				Unsafe.As<byte, int>(ref dest) = Unsafe.As<byte, int>(ref src);
				Unsafe.As<byte, int>(ref Unsafe.Add(ref source2, -4)) = Unsafe.As<byte, int>(ref Unsafe.Add(ref source, -4));
			}
			else if (len != 0)
			{
				dest = src;
				if ((len & 2) != 0)
				{
					Unsafe.As<byte, short>(ref Unsafe.Add(ref source2, -2)) = Unsafe.As<byte, short>(ref Unsafe.Add(ref source, -2));
				}
			}
			return;
		}
		if (Unsafe.AreSame(ref dest, ref src))
		{
			return;
		}
		goto IL_01db;
		IL_01db:
		_Memmove(ref dest, ref src, len);
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	private unsafe static void _Memmove(ref byte dest, ref byte src, nuint len)
	{
		fixed (byte* dest2 = &dest)
		{
			fixed (byte* src2 = &src)
			{
				__Memmove(dest2, src2, len);
			}
		}
	}
}
