using System.Runtime.CompilerServices;

namespace System.Security.Cryptography;

public static class CryptographicOperations
{
	[MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
	public static bool FixedTimeEquals(ReadOnlySpan<byte> left, ReadOnlySpan<byte> right)
	{
		if (left.Length != right.Length)
		{
			return false;
		}
		int length = left.Length;
		int num = 0;
		for (int i = 0; i < length; i++)
		{
			num |= left[i] - right[i];
		}
		return num == 0;
	}

	[MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
	public static void ZeroMemory(Span<byte> buffer)
	{
		buffer.Clear();
	}
}
