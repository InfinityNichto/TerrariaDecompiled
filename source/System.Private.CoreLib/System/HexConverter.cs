using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
using Internal.Runtime.CompilerServices;

namespace System;

internal static class HexConverter
{
	public enum Casing : uint
	{
		Upper = 0u,
		Lower = 8224u
	}

	public static ReadOnlySpan<byte> CharToHexLookup => new byte[256]
	{
		255, 255, 255, 255, 255, 255, 255, 255, 255, 255,
		255, 255, 255, 255, 255, 255, 255, 255, 255, 255,
		255, 255, 255, 255, 255, 255, 255, 255, 255, 255,
		255, 255, 255, 255, 255, 255, 255, 255, 255, 255,
		255, 255, 255, 255, 255, 255, 255, 255, 0, 1,
		2, 3, 4, 5, 6, 7, 8, 9, 255, 255,
		255, 255, 255, 255, 255, 10, 11, 12, 13, 14,
		15, 255, 255, 255, 255, 255, 255, 255, 255, 255,
		255, 255, 255, 255, 255, 255, 255, 255, 255, 255,
		255, 255, 255, 255, 255, 255, 255, 10, 11, 12,
		13, 14, 15, 255, 255, 255, 255, 255, 255, 255,
		255, 255, 255, 255, 255, 255, 255, 255, 255, 255,
		255, 255, 255, 255, 255, 255, 255, 255, 255, 255,
		255, 255, 255, 255, 255, 255, 255, 255, 255, 255,
		255, 255, 255, 255, 255, 255, 255, 255, 255, 255,
		255, 255, 255, 255, 255, 255, 255, 255, 255, 255,
		255, 255, 255, 255, 255, 255, 255, 255, 255, 255,
		255, 255, 255, 255, 255, 255, 255, 255, 255, 255,
		255, 255, 255, 255, 255, 255, 255, 255, 255, 255,
		255, 255, 255, 255, 255, 255, 255, 255, 255, 255,
		255, 255, 255, 255, 255, 255, 255, 255, 255, 255,
		255, 255, 255, 255, 255, 255, 255, 255, 255, 255,
		255, 255, 255, 255, 255, 255, 255, 255, 255, 255,
		255, 255, 255, 255, 255, 255, 255, 255, 255, 255,
		255, 255, 255, 255, 255, 255, 255, 255, 255, 255,
		255, 255, 255, 255, 255, 255
	};

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static void ToBytesBuffer(byte value, Span<byte> buffer, int startingIndex = 0, Casing casing = Casing.Upper)
	{
		uint num = (uint)(((value & 0xF0) << 4) + (value & 0xF) - 35209);
		uint num2 = ((((0 - num) & 0x7070) >> 4) + num + 47545) | (uint)casing;
		buffer[startingIndex + 1] = (byte)num2;
		buffer[startingIndex] = (byte)(num2 >> 8);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static void ToCharsBuffer(byte value, Span<char> buffer, int startingIndex = 0, Casing casing = Casing.Upper)
	{
		uint num = (uint)(((value & 0xF0) << 4) + (value & 0xF) - 35209);
		uint num2 = ((((0 - num) & 0x7070) >> 4) + num + 47545) | (uint)casing;
		buffer[startingIndex + 1] = (char)(num2 & 0xFFu);
		buffer[startingIndex] = (char)(num2 >> 8);
	}

	private static void EncodeToUtf16_Ssse3(ReadOnlySpan<byte> bytes, Span<char> chars, Casing casing)
	{
		nint num = 0;
		Vector128<byte> mask = Vector128.Create(byte.MaxValue, byte.MaxValue, 0, byte.MaxValue, byte.MaxValue, byte.MaxValue, 1, byte.MaxValue, byte.MaxValue, byte.MaxValue, 2, byte.MaxValue, byte.MaxValue, byte.MaxValue, 3, byte.MaxValue);
		Vector128<byte> value = ((casing == Casing.Upper) ? Vector128.Create((byte)48, (byte)49, (byte)50, (byte)51, (byte)52, (byte)53, (byte)54, (byte)55, (byte)56, (byte)57, (byte)65, (byte)66, (byte)67, (byte)68, (byte)69, (byte)70) : Vector128.Create((byte)48, (byte)49, (byte)50, (byte)51, (byte)52, (byte)53, (byte)54, (byte)55, (byte)56, (byte)57, (byte)97, (byte)98, (byte)99, (byte)100, (byte)101, (byte)102));
		do
		{
			uint value2 = Unsafe.ReadUnaligned<uint>(ref Unsafe.Add(ref MemoryMarshal.GetReference(bytes), num));
			Vector128<byte> vector = Ssse3.Shuffle(Vector128.CreateScalarUnsafe(value2).AsByte(), mask);
			Vector128<byte> right = Sse2.ShiftRightLogical(Sse2.ShiftRightLogical128BitLane(vector, 2).AsInt32(), 4).AsByte();
			Vector128<byte> mask2 = Sse2.And(Sse2.Or(vector, right), Vector128.Create((byte)15));
			Vector128<byte> left = Ssse3.Shuffle(value, mask2);
			left = Sse2.And(left, Vector128.Create((ushort)255).AsByte());
			Unsafe.WriteUnaligned(ref Unsafe.As<char, byte>(ref Unsafe.Add(ref MemoryMarshal.GetReference(chars), num * 2)), left);
			num += 4;
		}
		while (num < bytes.Length - 3);
		for (; num < bytes.Length; num++)
		{
			ToCharsBuffer(Unsafe.Add(ref MemoryMarshal.GetReference(bytes), num), chars, (int)num * 2, casing);
		}
	}

	public static void EncodeToUtf16(ReadOnlySpan<byte> bytes, Span<char> chars, Casing casing = Casing.Upper)
	{
		if (Ssse3.IsSupported && bytes.Length >= 4)
		{
			EncodeToUtf16_Ssse3(bytes, chars, casing);
			return;
		}
		for (int i = 0; i < bytes.Length; i++)
		{
			ToCharsBuffer(bytes[i], chars, i * 2, casing);
		}
	}

	public unsafe static string ToString(ReadOnlySpan<byte> bytes, Casing casing = Casing.Upper)
	{
		fixed (byte* ptr = bytes)
		{
			return string.Create(bytes.Length * 2, ((IntPtr)ptr, bytes.Length, casing), delegate(Span<char> chars, (IntPtr Ptr, int Length, Casing casing) args)
			{
				ReadOnlySpan<byte> bytes2 = new ReadOnlySpan<byte>((void*)args.Ptr, args.Length);
				EncodeToUtf16(bytes2, chars, args.casing);
			});
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static char ToCharUpper(int value)
	{
		value &= 0xF;
		value += 48;
		if (value > 57)
		{
			value += 7;
		}
		return (char)value;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static char ToCharLower(int value)
	{
		value &= 0xF;
		value += 48;
		if (value > 57)
		{
			value += 39;
		}
		return (char)value;
	}

	public static bool TryDecodeFromUtf16(ReadOnlySpan<char> chars, Span<byte> bytes)
	{
		int charsProcessed;
		return TryDecodeFromUtf16(chars, bytes, out charsProcessed);
	}

	public static bool TryDecodeFromUtf16(ReadOnlySpan<char> chars, Span<byte> bytes, out int charsProcessed)
	{
		int num = 0;
		int num2 = 0;
		int num3 = 0;
		int num4 = 0;
		while (num2 < bytes.Length)
		{
			num3 = FromChar(chars[num + 1]);
			num4 = FromChar(chars[num]);
			if ((num3 | num4) == 255)
			{
				break;
			}
			bytes[num2++] = (byte)((num4 << 4) | num3);
			num += 2;
		}
		if (num3 == 255)
		{
			num++;
		}
		charsProcessed = num;
		return (num3 | num4) != 255;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static int FromChar(int c)
	{
		if (c < CharToHexLookup.Length)
		{
			return CharToHexLookup[c];
		}
		return 255;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool IsHexChar(int c)
	{
		_ = IntPtr.Size;
		ulong num = (uint)(c - 48);
		ulong num2 = (ulong)(-17875860044349952L << (int)num);
		ulong num3 = num - 64;
		if ((long)(num2 & num3) >= 0L)
		{
			return false;
		}
		return true;
	}
}
