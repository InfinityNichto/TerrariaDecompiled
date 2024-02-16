using System;
using System.IO;
using System.Security.Cryptography;
using Microsoft.Win32.SafeHandles;

namespace Internal.Cryptography;

internal static class CngCommon
{
	public static byte[] HashData(byte[] data, int offset, int count, HashAlgorithmName hashAlgorithm)
	{
		using Internal.Cryptography.HashProviderCng hashProviderCng = new Internal.Cryptography.HashProviderCng(hashAlgorithm.Name, null);
		hashProviderCng.AppendHashData(data, offset, count);
		return hashProviderCng.FinalizeHashAndReset();
	}

	public static bool TryHashData(ReadOnlySpan<byte> source, Span<byte> destination, HashAlgorithmName hashAlgorithm, out int bytesWritten)
	{
		using Internal.Cryptography.HashProviderCng hashProviderCng = new Internal.Cryptography.HashProviderCng(hashAlgorithm.Name, null);
		if (destination.Length < hashProviderCng.HashSizeInBytes)
		{
			bytesWritten = 0;
			return false;
		}
		hashProviderCng.AppendHashData(source);
		return hashProviderCng.TryFinalizeHashAndReset(destination, out bytesWritten);
	}

	public static byte[] HashData(Stream data, HashAlgorithmName hashAlgorithm)
	{
		using Internal.Cryptography.HashProviderCng hashProviderCng = new Internal.Cryptography.HashProviderCng(hashAlgorithm.Name, null);
		byte[] array = new byte[4096];
		int count;
		while ((count = data.Read(array, 0, array.Length)) > 0)
		{
			hashProviderCng.AppendHashData(array, 0, count);
		}
		return hashProviderCng.FinalizeHashAndReset();
	}

	public unsafe static byte[] SignHash(this SafeNCryptKeyHandle keyHandle, ReadOnlySpan<byte> hash, global::Interop.NCrypt.AsymmetricPaddingMode paddingMode, void* pPaddingInfo, int estimatedSize)
	{
		byte[] array = new byte[estimatedSize];
		int pcbResult;
		global::Interop.NCrypt.ErrorCode errorCode = global::Interop.NCrypt.NCryptSignHash(keyHandle, pPaddingInfo, hash, hash.Length, array, array.Length, out pcbResult, paddingMode);
		if (errorCode == global::Interop.NCrypt.ErrorCode.STATUS_UNSUCCESSFUL)
		{
			errorCode = global::Interop.NCrypt.NCryptSignHash(keyHandle, pPaddingInfo, hash, hash.Length, array, array.Length, out pcbResult, paddingMode);
		}
		if (errorCode == global::Interop.NCrypt.ErrorCode.NTE_BUFFER_TOO_SMALL)
		{
			array = new byte[pcbResult];
			errorCode = global::Interop.NCrypt.NCryptSignHash(keyHandle, pPaddingInfo, hash, hash.Length, array, array.Length, out pcbResult, paddingMode);
		}
		if (errorCode == global::Interop.NCrypt.ErrorCode.STATUS_UNSUCCESSFUL)
		{
			errorCode = global::Interop.NCrypt.NCryptSignHash(keyHandle, pPaddingInfo, hash, hash.Length, array, array.Length, out pcbResult, paddingMode);
		}
		if (errorCode != 0)
		{
			throw errorCode.ToCryptographicException();
		}
		Array.Resize(ref array, pcbResult);
		return array;
	}

	public unsafe static bool TrySignHash(this SafeNCryptKeyHandle keyHandle, ReadOnlySpan<byte> hash, Span<byte> signature, global::Interop.NCrypt.AsymmetricPaddingMode paddingMode, void* pPaddingInfo, out int bytesWritten)
	{
		for (int i = 0; i <= 1; i++)
		{
			int pcbResult;
			global::Interop.NCrypt.ErrorCode errorCode = global::Interop.NCrypt.NCryptSignHash(keyHandle, pPaddingInfo, hash, hash.Length, signature, signature.Length, out pcbResult, paddingMode);
			switch (errorCode)
			{
			case global::Interop.NCrypt.ErrorCode.ERROR_SUCCESS:
				bytesWritten = pcbResult;
				return true;
			case global::Interop.NCrypt.ErrorCode.NTE_BUFFER_TOO_SMALL:
				bytesWritten = 0;
				return false;
			default:
				throw errorCode.ToCryptographicException();
			case global::Interop.NCrypt.ErrorCode.STATUS_UNSUCCESSFUL:
				break;
			}
		}
		throw global::Interop.NCrypt.ErrorCode.STATUS_UNSUCCESSFUL.ToCryptographicException();
	}

	public unsafe static bool VerifyHash(this SafeNCryptKeyHandle keyHandle, ReadOnlySpan<byte> hash, ReadOnlySpan<byte> signature, global::Interop.NCrypt.AsymmetricPaddingMode paddingMode, void* pPaddingInfo)
	{
		global::Interop.NCrypt.ErrorCode errorCode = global::Interop.NCrypt.NCryptVerifySignature(keyHandle, pPaddingInfo, hash, hash.Length, signature, signature.Length, paddingMode);
		if (errorCode == global::Interop.NCrypt.ErrorCode.STATUS_UNSUCCESSFUL)
		{
			errorCode = global::Interop.NCrypt.NCryptVerifySignature(keyHandle, pPaddingInfo, hash, hash.Length, signature, signature.Length, paddingMode);
		}
		return errorCode == global::Interop.NCrypt.ErrorCode.ERROR_SUCCESS;
	}
}
