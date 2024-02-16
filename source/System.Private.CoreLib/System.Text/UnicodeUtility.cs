using System.Runtime.CompilerServices;

namespace System.Text;

internal static class UnicodeUtility
{
	public static int GetPlane(uint codePoint)
	{
		return (int)(codePoint >> 16);
	}

	public static uint GetScalarFromUtf16SurrogatePair(uint highSurrogateCodePoint, uint lowSurrogateCodePoint)
	{
		return (highSurrogateCodePoint << 10) + lowSurrogateCodePoint - 56613888;
	}

	public static int GetUtf16SequenceLength(uint value)
	{
		value -= 65536;
		value += 33554432;
		value >>= 24;
		return (int)value;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static void GetUtf16SurrogatesFromSupplementaryPlaneScalar(uint value, out char highSurrogateCodePoint, out char lowSurrogateCodePoint)
	{
		highSurrogateCodePoint = (char)(value + 56557568 >> 10);
		lowSurrogateCodePoint = (char)((value & 0x3FF) + 56320);
	}

	public static int GetUtf8SequenceLength(uint value)
	{
		int num = (int)(value - 2048) >> 31;
		value ^= 0xF800u;
		value -= 63616;
		value += 67108864;
		value >>= 24;
		return (int)value + num * 2;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool IsAsciiCodePoint(uint value)
	{
		return value <= 127;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool IsBmpCodePoint(uint value)
	{
		return value <= 65535;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool IsHighSurrogateCodePoint(uint value)
	{
		return IsInRangeInclusive(value, 55296u, 56319u);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool IsInRangeInclusive(uint value, uint lowerBound, uint upperBound)
	{
		return value - lowerBound <= upperBound - lowerBound;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool IsLowSurrogateCodePoint(uint value)
	{
		return IsInRangeInclusive(value, 56320u, 57343u);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool IsSurrogateCodePoint(uint value)
	{
		return IsInRangeInclusive(value, 55296u, 57343u);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool IsValidCodePoint(uint codePoint)
	{
		return codePoint <= 1114111;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool IsValidUnicodeScalar(uint value)
	{
		return ((value - 1114112) ^ 0xD800) >= 4293855232u;
	}
}
