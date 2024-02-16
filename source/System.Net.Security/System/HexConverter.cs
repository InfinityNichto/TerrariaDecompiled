using System.Runtime.CompilerServices;

namespace System;

internal static class HexConverter
{
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
}
