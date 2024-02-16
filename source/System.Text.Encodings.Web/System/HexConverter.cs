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
}
