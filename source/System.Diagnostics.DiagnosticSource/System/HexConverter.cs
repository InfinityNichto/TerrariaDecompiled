using System.Runtime.CompilerServices;

namespace System;

internal static class HexConverter
{
	public enum Casing : uint
	{
		Upper = 0u,
		Lower = 8224u
	}

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
	public static int FromLowerChar(int c)
	{
		switch (c)
		{
		case 48:
		case 49:
		case 50:
		case 51:
		case 52:
		case 53:
		case 54:
		case 55:
		case 56:
		case 57:
			return c - 48;
		case 97:
		case 98:
		case 99:
		case 100:
		case 101:
		case 102:
			return c - 97 + 10;
		default:
			return 255;
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool IsHexLowerChar(int c)
	{
		if ((uint)(c - 48) > 9u)
		{
			return (uint)(c - 97) <= 5u;
		}
		return true;
	}
}
