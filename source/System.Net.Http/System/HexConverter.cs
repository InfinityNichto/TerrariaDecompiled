using System.Runtime.CompilerServices;

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
	public static void ToCharsBuffer(byte value, Span<char> buffer, int startingIndex = 0, Casing casing = Casing.Upper)
	{
		uint num = (uint)(((value & 0xF0) << 4) + (value & 0xF) - 35209);
		uint num2 = ((((0 - num) & 0x7070) >> 4) + num + 47545) | (uint)casing;
		buffer[startingIndex + 1] = (char)(num2 & 0xFFu);
		buffer[startingIndex] = (char)(num2 >> 8);
	}

	public static void EncodeToUtf16(ReadOnlySpan<byte> bytes, Span<char> chars, Casing casing = Casing.Upper)
	{
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
	public static int FromUpperChar(int c)
	{
		if (c <= 71)
		{
			return CharToHexLookup[c];
		}
		return 255;
	}
}
