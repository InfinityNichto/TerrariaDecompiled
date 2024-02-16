using System;
using System.Security.Cryptography;
using Internal.NativeCrypto;

namespace Internal.Cryptography;

internal static class RC2BCryptModes
{
	internal static SafeAlgorithmHandle GetHandle(CipherMode cipherMode, int effectiveKeyLength)
	{
		return cipherMode switch
		{
			CipherMode.CBC => OpenRC2Algorithm("ChainingModeCBC", effectiveKeyLength), 
			CipherMode.ECB => OpenRC2Algorithm("ChainingModeECB", effectiveKeyLength), 
			_ => throw new NotSupportedException(), 
		};
	}

	private static SafeAlgorithmHandle OpenRC2Algorithm(string cipherMode, int effectiveKeyLength)
	{
		SafeAlgorithmHandle safeAlgorithmHandle = Cng.BCryptOpenAlgorithmProvider("RC2", null, Cng.OpenAlgorithmProviderFlags.NONE);
		safeAlgorithmHandle.SetCipherMode(cipherMode);
		if (effectiveKeyLength != 0)
		{
			safeAlgorithmHandle.SetEffectiveKeyLength(effectiveKeyLength);
		}
		return safeAlgorithmHandle;
	}
}
