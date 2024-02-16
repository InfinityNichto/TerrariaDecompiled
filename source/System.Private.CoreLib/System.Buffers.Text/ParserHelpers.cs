using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using Internal.Runtime.CompilerServices;

namespace System.Buffers.Text;

internal static class ParserHelpers
{
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool IsDigit(int i)
	{
		return (uint)(i - 48) <= 9u;
	}

	public static bool TryParseThrowFormatException(out int bytesConsumed)
	{
		bytesConsumed = 0;
		ThrowHelper.ThrowFormatException_BadFormatSpecifier();
		return false;
	}

	public static bool TryParseThrowFormatException<T>(out T value, out int bytesConsumed) where T : struct
	{
		value = default(T);
		return TryParseThrowFormatException(out bytesConsumed);
	}

	[DoesNotReturn]
	[StackTraceHidden]
	public static bool TryParseThrowFormatException<T>(ReadOnlySpan<byte> source, out T value, out int bytesConsumed) where T : struct
	{
		Unsafe.SkipInit<T>(out value);
		Unsafe.SkipInit<int>(out bytesConsumed);
		ThrowHelper.ThrowFormatException_BadFormatSpecifier();
		return false;
	}
}
