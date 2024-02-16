using System.Runtime.CompilerServices;

namespace System.Text;

internal static class UnicodeUtility
{
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
}
