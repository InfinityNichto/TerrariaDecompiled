using System.Runtime.CompilerServices;

namespace System;

internal static class HexConverter
{
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
