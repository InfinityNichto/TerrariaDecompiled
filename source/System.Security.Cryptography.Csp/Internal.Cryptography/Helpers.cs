using System;
using System.Diagnostics.CodeAnalysis;
using System.Security.Cryptography;

namespace Internal.Cryptography;

internal static class Helpers
{
	public static bool UsesIv(this CipherMode cipherMode)
	{
		return cipherMode != CipherMode.ECB;
	}

	public static byte[] GetCipherIv(this CipherMode cipherMode, byte[] iv)
	{
		if (cipherMode.UsesIv())
		{
			if (iv == null)
			{
				throw new CryptographicException(System.SR.Cryptography_MissingIV);
			}
			return iv;
		}
		return null;
	}

	public static byte[] TrimLargeIV(byte[] currentIV, int blockSizeInBits)
	{
		int num = checked(blockSizeInBits + 7) / 8;
		if (currentIV != null && currentIV.Length > num)
		{
			byte[] array = new byte[num];
			Buffer.BlockCopy(currentIV, 0, array, 0, array.Length);
			return array;
		}
		return currentIV;
	}

	[return: NotNullIfNotNull("src")]
	public static byte[] CloneByteArray(this byte[] src)
	{
		if (src == null)
		{
			return null;
		}
		return (byte[])src.Clone();
	}

	public static int GetPaddingSize(this SymmetricAlgorithm algorithm, CipherMode mode, int feedbackSizeInBits)
	{
		return ((mode == CipherMode.CFB) ? feedbackSizeInBits : algorithm.BlockSize) / 8;
	}
}
