using System.Buffers;
using System.Globalization;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text.Unicode;
using Internal.Runtime.CompilerServices;

namespace System;

internal static class Marvin
{
	public static ulong DefaultSeed { get; } = GenerateSeed();


	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static int ComputeHash32(ReadOnlySpan<byte> data, ulong seed)
	{
		return ComputeHash32(ref MemoryMarshal.GetReference(data), (uint)data.Length, (uint)seed, (uint)(seed >> 32));
	}

	public static int ComputeHash32(ref byte data, uint count, uint p0, uint p1)
	{
		uint num;
		if (count < 8)
		{
			if (count < 4)
			{
				_ = BitConverter.IsLittleEndian;
				num = 128u;
				if ((count & (true ? 1u : 0u)) != 0)
				{
					num = Unsafe.AddByteOffset(ref data, (nuint)count & (nuint)2u);
					_ = BitConverter.IsLittleEndian;
					num |= 0x8000u;
				}
				if ((count & 2u) != 0)
				{
					_ = BitConverter.IsLittleEndian;
					num <<= 16;
					num |= Unsafe.ReadUnaligned<ushort>(ref data);
				}
				goto IL_00a6;
			}
		}
		else
		{
			uint num2 = count / 8;
			do
			{
				p0 += Unsafe.ReadUnaligned<uint>(ref data);
				uint num3 = Unsafe.ReadUnaligned<uint>(ref Unsafe.AddByteOffset(ref data, 4u));
				Block(ref p0, ref p1);
				p0 += num3;
				Block(ref p0, ref p1);
				data = ref Unsafe.AddByteOffset(ref data, 8u);
			}
			while (--num2 != 0);
			if ((count & 4) == 0)
			{
				goto IL_006a;
			}
		}
		p0 += Unsafe.ReadUnaligned<uint>(ref data);
		Block(ref p0, ref p1);
		goto IL_006a;
		IL_00a6:
		p0 += num;
		Block(ref p0, ref p1);
		Block(ref p0, ref p1);
		return (int)(p1 ^ p0);
		IL_006a:
		num = Unsafe.ReadUnaligned<uint>(ref Unsafe.Add(ref Unsafe.AddByteOffset(ref data, (nuint)count & (nuint)7u), -4));
		count = ~count << 3;
		_ = BitConverter.IsLittleEndian;
		num >>= 8;
		num |= 0x80000000u;
		num >>= (int)(count & 0x1F);
		goto IL_00a6;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static void Block(ref uint rp0, ref uint rp1)
	{
		uint num = rp0;
		uint num2 = rp1;
		num2 ^= num;
		num = BitOperations.RotateLeft(num, 20);
		num += num2;
		num2 = BitOperations.RotateLeft(num2, 9);
		num2 ^= num;
		num = BitOperations.RotateLeft(num, 27);
		num += num2;
		num2 = BitOperations.RotateLeft(num2, 19);
		rp0 = num;
		rp1 = num2;
	}

	private unsafe static ulong GenerateSeed()
	{
		System.Runtime.CompilerServices.Unsafe.SkipInit(out ulong result);
		Interop.GetRandomBytes((byte*)(&result), 8);
		return result;
	}

	public static int ComputeHash32OrdinalIgnoreCase(ref char data, int count, uint p0, uint p1)
	{
		uint num = (uint)count;
		nuint num2 = 0u;
		while (true)
		{
			if (num >= 2)
			{
				uint value = Unsafe.ReadUnaligned<uint>(ref Unsafe.As<char, byte>(ref Unsafe.AddByteOffset(ref data, num2)));
				if (!Utf16Utility.AllCharsInUInt32AreAscii(value))
				{
					break;
				}
				p0 += Utf16Utility.ConvertAllAsciiCharsInUInt32ToUppercase(value);
				Block(ref p0, ref p1);
				num2 += 4;
				num -= 2;
				continue;
			}
			if (num != 0)
			{
				uint value = Unsafe.AddByteOffset(ref data, num2);
				if (value > 127)
				{
					break;
				}
				_ = BitConverter.IsLittleEndian;
				p0 += Utf16Utility.ConvertAllAsciiCharsInUInt32ToUppercase(value) + 8388480;
			}
			_ = BitConverter.IsLittleEndian;
			p0 += 128;
			Block(ref p0, ref p1);
			Block(ref p0, ref p1);
			return (int)(p1 ^ p0);
		}
		return ComputeHash32OrdinalIgnoreCaseSlow(ref Unsafe.AddByteOffset(ref data, num2), (int)num, p0, p1);
	}

	private static int ComputeHash32OrdinalIgnoreCaseSlow(ref char data, int count, uint p0, uint p1)
	{
		char[] array = null;
		Span<char> span = (((uint)count > 64u) ? ((Span<char>)(array = ArrayPool<char>.Shared.Rent(count))) : stackalloc char[64]);
		Span<char> span2 = span;
		int num = Ordinal.ToUpperOrdinal(new ReadOnlySpan<char>(ref data, count), span2);
		int result = ComputeHash32(ref Unsafe.As<char, byte>(ref MemoryMarshal.GetReference(span2)), (uint)(num * 2), p0, p1);
		if (array != null)
		{
			ArrayPool<char>.Shared.Return(array);
		}
		return result;
	}
}
