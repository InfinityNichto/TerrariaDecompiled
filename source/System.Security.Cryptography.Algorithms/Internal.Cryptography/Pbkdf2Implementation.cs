using System;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Threading;
using Microsoft.Win32.SafeHandles;

namespace Internal.Cryptography;

internal static class Pbkdf2Implementation
{
	private static readonly bool s_useKeyDerivation = OperatingSystem.IsWindowsVersionAtLeast(6, 2);

	private static SafeBCryptAlgorithmHandle s_pbkdf2AlgorithmHandle;

	public static void Fill(ReadOnlySpan<byte> password, ReadOnlySpan<byte> salt, int iterations, HashAlgorithmName hashAlgorithmName, Span<byte> destination)
	{
		if (s_useKeyDerivation)
		{
			FillKeyDerivation(password, salt, iterations, hashAlgorithmName.Name, destination);
		}
		else
		{
			FillDeriveKeyPBKDF2(password, salt, iterations, hashAlgorithmName.Name, destination);
		}
	}

	private unsafe static void FillKeyDerivation(ReadOnlySpan<byte> password, ReadOnlySpan<byte> salt, int iterations, string hashAlgorithmName, Span<byte> destination)
	{
		int hashBlockSize = GetHashBlockSize(hashAlgorithmName);
		Span<byte> span = default(Span<byte>);
		ReadOnlySpan<byte> readOnlySpan = default(Span<byte>);
		int cbSecret;
		if (password.IsEmpty)
		{
			Span<byte> span2 = stackalloc byte[1];
			readOnlySpan = span2;
			cbSecret = 0;
			span = default(Span<byte>);
		}
		else if (password.Length <= hashBlockSize)
		{
			readOnlySpan = password;
			cbSecret = password.Length;
			span = default(Span<byte>);
		}
		else
		{
			Span<byte> destination2 = stackalloc byte[64];
			int num = hashAlgorithmName switch
			{
				"SHA1" => SHA1.HashData(password, destination2), 
				"SHA256" => SHA256.HashData(password, destination2), 
				"SHA384" => SHA384.HashData(password, destination2), 
				"SHA512" => SHA512.HashData(password, destination2), 
				_ => throw new CryptographicException(), 
			};
			span = destination2.Slice(0, num);
			readOnlySpan = span;
			cbSecret = num;
		}
		global::Interop.BCrypt.NTSTATUS nTSTATUS;
		SafeBCryptKeyHandle phKey;
		if (global::Interop.BCrypt.PseudoHandlesSupported)
		{
			fixed (byte* pbSecret = readOnlySpan)
			{
				nTSTATUS = global::Interop.BCrypt.BCryptGenerateSymmetricKey(817u, out phKey, IntPtr.Zero, 0, pbSecret, cbSecret, 0u);
			}
		}
		else
		{
			if (s_pbkdf2AlgorithmHandle == null)
			{
				SafeBCryptAlgorithmHandle phAlgorithm;
				global::Interop.BCrypt.NTSTATUS nTSTATUS2 = global::Interop.BCrypt.BCryptOpenAlgorithmProvider(out phAlgorithm, "PBKDF2", null, global::Interop.BCrypt.BCryptOpenAlgorithmProviderFlags.None);
				if (nTSTATUS2 != 0)
				{
					phAlgorithm.Dispose();
					CryptographicOperations.ZeroMemory(span);
					throw global::Interop.BCrypt.CreateCryptographicException(nTSTATUS2);
				}
				Interlocked.CompareExchange(ref s_pbkdf2AlgorithmHandle, phAlgorithm, null);
			}
			fixed (byte* pbSecret2 = readOnlySpan)
			{
				nTSTATUS = global::Interop.BCrypt.BCryptGenerateSymmetricKey(s_pbkdf2AlgorithmHandle, out phKey, IntPtr.Zero, 0, pbSecret2, cbSecret, 0u);
			}
		}
		CryptographicOperations.ZeroMemory(span);
		if (nTSTATUS != 0)
		{
			phKey.Dispose();
			throw global::Interop.BCrypt.CreateCryptographicException(nTSTATUS);
		}
		ulong num2 = (ulong)iterations;
		using (phKey)
		{
			fixed (char* ptr2 = hashAlgorithmName)
			{
				fixed (byte* ptr = salt)
				{
					fixed (byte* pbDerivedKey = destination)
					{
						Span<global::Interop.BCrypt.BCryptBuffer> span3 = stackalloc global::Interop.BCrypt.BCryptBuffer[3];
						span3[0].BufferType = global::Interop.BCrypt.CngBufferDescriptors.KDF_ITERATION_COUNT;
						span3[0].pvBuffer = (IntPtr)(&num2);
						span3[0].cbBuffer = 8;
						span3[1].BufferType = global::Interop.BCrypt.CngBufferDescriptors.KDF_SALT;
						span3[1].pvBuffer = (IntPtr)ptr;
						span3[1].cbBuffer = salt.Length;
						span3[2].BufferType = global::Interop.BCrypt.CngBufferDescriptors.KDF_HASH_ALGORITHM;
						span3[2].pvBuffer = (IntPtr)ptr2;
						span3[2].cbBuffer = checked((hashAlgorithmName.Length + 1) * 2);
						fixed (global::Interop.BCrypt.BCryptBuffer* ptr3 = span3)
						{
							Unsafe.SkipInit(out global::Interop.BCrypt.BCryptBufferDesc bCryptBufferDesc);
							bCryptBufferDesc.ulVersion = 0;
							bCryptBufferDesc.cBuffers = span3.Length;
							bCryptBufferDesc.pBuffers = (IntPtr)ptr3;
							uint pcbResult;
							global::Interop.BCrypt.NTSTATUS nTSTATUS3 = global::Interop.BCrypt.BCryptKeyDerivation(phKey, &bCryptBufferDesc, pbDerivedKey, destination.Length, out pcbResult, 0);
							if (nTSTATUS3 != 0)
							{
								throw global::Interop.BCrypt.CreateCryptographicException(nTSTATUS3);
							}
							if (destination.Length != pcbResult)
							{
								throw new CryptographicException();
							}
						}
					}
				}
			}
		}
	}

	private unsafe static void FillDeriveKeyPBKDF2(ReadOnlySpan<byte> password, ReadOnlySpan<byte> salt, int iterations, string hashAlgorithmName, Span<byte> destination)
	{
		int hashSizeInBytes;
		SafeBCryptAlgorithmHandle cachedBCryptAlgorithmHandle = global::Interop.BCrypt.BCryptAlgorithmCache.GetCachedBCryptAlgorithmHandle(hashAlgorithmName, global::Interop.BCrypt.BCryptOpenAlgorithmProviderFlags.BCRYPT_ALG_HANDLE_HMAC_FLAG, out hashSizeInBytes);
		fixed (byte* pbPassword = password)
		{
			fixed (byte* pbSalt = salt)
			{
				fixed (byte* pbDerivedKey = destination)
				{
					global::Interop.BCrypt.NTSTATUS nTSTATUS = global::Interop.BCrypt.BCryptDeriveKeyPBKDF2(cachedBCryptAlgorithmHandle, pbPassword, password.Length, pbSalt, salt.Length, (ulong)iterations, pbDerivedKey, destination.Length, 0u);
					if (nTSTATUS != 0)
					{
						throw global::Interop.BCrypt.CreateCryptographicException(nTSTATUS);
					}
				}
			}
		}
	}

	private static int GetHashBlockSize(string hashAlgorithmName)
	{
		switch (hashAlgorithmName)
		{
		case "SHA1":
		case "SHA256":
			return 64;
		case "SHA384":
		case "SHA512":
			return 128;
		default:
			throw new CryptographicException();
		}
	}
}
