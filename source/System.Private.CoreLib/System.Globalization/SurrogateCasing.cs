using System.Runtime.CompilerServices;
using System.Text;

namespace System.Globalization;

internal static class SurrogateCasing
{
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal static void ToUpper(char h, char l, out char hr, out char lr)
	{
		UnicodeUtility.GetUtf16SurrogatesFromSupplementaryPlaneScalar(CharUnicodeInfo.ToUpper(UnicodeUtility.GetScalarFromUtf16SurrogatePair(h, l)), out hr, out lr);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal static void ToLower(char h, char l, out char hr, out char lr)
	{
		UnicodeUtility.GetUtf16SurrogatesFromSupplementaryPlaneScalar(CharUnicodeInfo.ToLower(UnicodeUtility.GetScalarFromUtf16SurrogatePair(h, l)), out hr, out lr);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal static bool Equal(char h1, char l1, char h2, char l2)
	{
		ToUpper(h1, l1, out var hr, out var lr);
		ToUpper(h2, l2, out var hr2, out var lr2);
		if (hr == hr2)
		{
			return lr == lr2;
		}
		return false;
	}
}
