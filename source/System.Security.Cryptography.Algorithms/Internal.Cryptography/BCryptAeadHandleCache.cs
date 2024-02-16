using System;
using System.Threading;
using Internal.NativeCrypto;

namespace Internal.Cryptography;

internal static class BCryptAeadHandleCache
{
	private static SafeAlgorithmHandle s_aesCcm;

	private static SafeAlgorithmHandle s_aesGcm;

	private static SafeAlgorithmHandle s_chaCha20Poly1305;

	internal static SafeAlgorithmHandle AesCcm => GetCachedAlgorithmHandle(ref s_aesCcm, "AES", "ChainingModeCCM");

	internal static SafeAlgorithmHandle AesGcm => GetCachedAlgorithmHandle(ref s_aesGcm, "AES", "ChainingModeGCM");

	internal static bool IsChaCha20Poly1305Supported { get; } = OperatingSystem.IsWindowsVersionAtLeast(10, 0, 20142);


	internal static SafeAlgorithmHandle ChaCha20Poly1305 => GetCachedAlgorithmHandle(ref s_chaCha20Poly1305, "CHACHA20_POLY1305");

	private static SafeAlgorithmHandle GetCachedAlgorithmHandle(ref SafeAlgorithmHandle handle, string algId, string chainingMode = null)
	{
		SafeAlgorithmHandle safeAlgorithmHandle = Volatile.Read(ref handle);
		if (safeAlgorithmHandle != null)
		{
			return safeAlgorithmHandle;
		}
		SafeAlgorithmHandle safeAlgorithmHandle2 = Cng.BCryptOpenAlgorithmProvider(algId, null, Cng.OpenAlgorithmProviderFlags.NONE);
		if (chainingMode != null)
		{
			safeAlgorithmHandle2.SetCipherMode(chainingMode);
		}
		safeAlgorithmHandle = Interlocked.CompareExchange(ref handle, safeAlgorithmHandle2, null);
		if (safeAlgorithmHandle != null)
		{
			safeAlgorithmHandle2.Dispose();
			return safeAlgorithmHandle;
		}
		return safeAlgorithmHandle2;
	}
}
