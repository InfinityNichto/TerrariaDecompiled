using System.Runtime.CompilerServices;

namespace System.Collections;

internal static class HashHelpers
{
	public static ulong GetFastModMultiplier(uint divisor)
	{
		return ulong.MaxValue / (ulong)divisor + 1;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static uint FastMod(uint value, uint divisor, ulong multiplier)
	{
		return (uint)(((multiplier * value >> 32) + 1) * divisor >> 32);
	}
}
