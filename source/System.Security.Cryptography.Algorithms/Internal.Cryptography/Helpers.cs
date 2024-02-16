using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.Versioning;
using System.Security.Cryptography;

namespace Internal.Cryptography;

internal static class Helpers
{
	[UnsupportedOSPlatformGuard("android")]
	public static bool IsRC2Supported => !OperatingSystem.IsAndroid();

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

	public static byte[] FixupKeyParity(this byte[] key)
	{
		byte[] array = new byte[key.Length];
		for (int i = 0; i < key.Length; i++)
		{
			array[i] = (byte)(key[i] & 0xFEu);
			byte b = (byte)((array[i] & 0xFu) ^ (uint)(array[i] >> 4));
			byte b2 = (byte)((b & 3u) ^ (uint)(b >> 2));
			if ((byte)((b2 & 1) ^ (b2 >> 1)) == 0)
			{
				array[i] |= 1;
			}
		}
		return array;
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

	internal static bool TryCopyToDestination(this ReadOnlySpan<byte> source, Span<byte> destination, out int bytesWritten)
	{
		if (source.TryCopyTo(destination))
		{
			bytesWritten = source.Length;
			return true;
		}
		bytesWritten = 0;
		return false;
	}
}
