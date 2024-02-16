using System;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using Microsoft.Win32.SafeHandles;

namespace Internal.Cryptography;

internal static class HashProviderDispenser
{
	public static class OneShotHashProvider
	{
		public static int MacData(string hashAlgorithmId, ReadOnlySpan<byte> key, ReadOnlySpan<byte> source, Span<byte> destination)
		{
			int hashSizeInBytes;
			if (global::Interop.BCrypt.PseudoHandlesSupported)
			{
				HashDataUsingPseudoHandle(hashAlgorithmId, source, key, isHmac: true, destination, out hashSizeInBytes);
				return hashSizeInBytes;
			}
			SafeBCryptAlgorithmHandle cachedBCryptAlgorithmHandle = global::Interop.BCrypt.BCryptAlgorithmCache.GetCachedBCryptAlgorithmHandle(hashAlgorithmId, global::Interop.BCrypt.BCryptOpenAlgorithmProviderFlags.BCRYPT_ALG_HANDLE_HMAC_FLAG, out hashSizeInBytes);
			if (destination.Length < hashSizeInBytes)
			{
				throw new CryptographicException();
			}
			HashUpdateAndFinish(cachedBCryptAlgorithmHandle, hashSizeInBytes, key, source, destination);
			return hashSizeInBytes;
		}

		public static int HashData(string hashAlgorithmId, ReadOnlySpan<byte> source, Span<byte> destination)
		{
			int hashSizeInBytes;
			if (global::Interop.BCrypt.PseudoHandlesSupported)
			{
				HashDataUsingPseudoHandle(hashAlgorithmId, source, default(ReadOnlySpan<byte>), isHmac: false, destination, out hashSizeInBytes);
				return hashSizeInBytes;
			}
			SafeBCryptAlgorithmHandle cachedBCryptAlgorithmHandle = global::Interop.BCrypt.BCryptAlgorithmCache.GetCachedBCryptAlgorithmHandle(hashAlgorithmId, global::Interop.BCrypt.BCryptOpenAlgorithmProviderFlags.None, out hashSizeInBytes);
			if (destination.Length < hashSizeInBytes)
			{
				throw new CryptographicException();
			}
			HashUpdateAndFinish(cachedBCryptAlgorithmHandle, hashSizeInBytes, default(ReadOnlySpan<byte>), source, destination);
			return hashSizeInBytes;
		}

		private unsafe static void HashDataUsingPseudoHandle(string hashAlgorithmId, ReadOnlySpan<byte> source, ReadOnlySpan<byte> key, bool isHmac, Span<byte> destination, out int hashSize)
		{
			hashSize = 0;
			global::Interop.BCrypt.BCryptAlgPseudoHandle bCryptAlgPseudoHandle;
			int num;
			switch (hashAlgorithmId)
			{
			case "MD5":
				bCryptAlgPseudoHandle = (isHmac ? global::Interop.BCrypt.BCryptAlgPseudoHandle.BCRYPT_HMAC_MD5_ALG_HANDLE : global::Interop.BCrypt.BCryptAlgPseudoHandle.BCRYPT_MD5_ALG_HANDLE);
				num = 16;
				break;
			case "SHA1":
				bCryptAlgPseudoHandle = (isHmac ? global::Interop.BCrypt.BCryptAlgPseudoHandle.BCRYPT_HMAC_SHA1_ALG_HANDLE : global::Interop.BCrypt.BCryptAlgPseudoHandle.BCRYPT_SHA1_ALG_HANDLE);
				num = 20;
				break;
			case "SHA256":
				bCryptAlgPseudoHandle = (isHmac ? global::Interop.BCrypt.BCryptAlgPseudoHandle.BCRYPT_HMAC_SHA256_ALG_HANDLE : global::Interop.BCrypt.BCryptAlgPseudoHandle.BCRYPT_SHA256_ALG_HANDLE);
				num = 32;
				break;
			case "SHA384":
				bCryptAlgPseudoHandle = (isHmac ? global::Interop.BCrypt.BCryptAlgPseudoHandle.BCRYPT_HMAC_SHA384_ALG_HANDLE : global::Interop.BCrypt.BCryptAlgPseudoHandle.BCRYPT_SHA384_ALG_HANDLE);
				num = 48;
				break;
			case "SHA512":
				bCryptAlgPseudoHandle = (isHmac ? global::Interop.BCrypt.BCryptAlgPseudoHandle.BCRYPT_HMAC_SHA512_ALG_HANDLE : global::Interop.BCrypt.BCryptAlgPseudoHandle.BCRYPT_SHA512_ALG_HANDLE);
				num = 64;
				break;
			default:
				throw new CryptographicException();
			}
			if (destination.Length < num)
			{
				throw new CryptographicException();
			}
			fixed (byte* pbSecret = &MemoryMarshal.GetReference(key))
			{
				fixed (byte* pbInput = &MemoryMarshal.GetReference(source))
				{
					fixed (byte* pbOutput = &MemoryMarshal.GetReference(destination))
					{
						global::Interop.BCrypt.NTSTATUS nTSTATUS = global::Interop.BCrypt.BCryptHash((nuint)bCryptAlgPseudoHandle, pbSecret, key.Length, pbInput, source.Length, pbOutput, num);
						if (nTSTATUS != 0)
						{
							throw global::Interop.BCrypt.CreateCryptographicException(nTSTATUS);
						}
					}
				}
			}
			hashSize = num;
		}

		private static void HashUpdateAndFinish(SafeBCryptAlgorithmHandle algHandle, int hashSize, ReadOnlySpan<byte> key, ReadOnlySpan<byte> source, Span<byte> destination)
		{
			global::Interop.BCrypt.NTSTATUS nTSTATUS = global::Interop.BCrypt.BCryptCreateHash(algHandle, out var phHash, IntPtr.Zero, 0, key, key.Length, global::Interop.BCrypt.BCryptCreateHashFlags.None);
			if (nTSTATUS != 0)
			{
				phHash.Dispose();
				throw global::Interop.BCrypt.CreateCryptographicException(nTSTATUS);
			}
			using (phHash)
			{
				nTSTATUS = global::Interop.BCrypt.BCryptHashData(phHash, source, source.Length, 0);
				if (nTSTATUS != 0)
				{
					throw global::Interop.BCrypt.CreateCryptographicException(nTSTATUS);
				}
				global::Interop.BCrypt.BCryptFinishHash(phHash, destination, hashSize, 0);
			}
		}
	}

	public static HashProvider CreateHashProvider(string hashAlgorithmId)
	{
		return new HashProviderCng(hashAlgorithmId, null);
	}

	public static HashProvider CreateMacProvider(string hashAlgorithmId, ReadOnlySpan<byte> key)
	{
		return new HashProviderCng(hashAlgorithmId, key, isHmac: true);
	}
}
