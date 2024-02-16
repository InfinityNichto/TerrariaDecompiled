using System.Buffers;

namespace System.Security.Cryptography;

internal static class CryptoPool
{
	internal static byte[] Rent(int minimumLength)
	{
		return ArrayPool<byte>.Shared.Rent(minimumLength);
	}

	internal static void Return(ArraySegment<byte> arraySegment)
	{
		Return(arraySegment.Array, arraySegment.Count);
	}

	internal static void Return(byte[] array, int clearSize = -1)
	{
		bool flag = clearSize < 0;
		if (!flag && clearSize != 0)
		{
			CryptographicOperations.ZeroMemory(array.AsSpan(0, clearSize));
		}
		ArrayPool<byte>.Shared.Return(array, flag);
	}
}
