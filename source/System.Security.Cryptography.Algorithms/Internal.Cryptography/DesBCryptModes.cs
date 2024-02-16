using System;
using System.Security.Cryptography;
using Internal.NativeCrypto;

namespace Internal.Cryptography;

internal static class DesBCryptModes
{
	private static readonly Lazy<SafeAlgorithmHandle> s_hAlgCbc = OpenDesAlgorithm("ChainingModeCBC");

	private static readonly Lazy<SafeAlgorithmHandle> s_hAlgEcb = OpenDesAlgorithm("ChainingModeECB");

	private static readonly Lazy<SafeAlgorithmHandle> s_hAlgCfb8 = OpenDesAlgorithm("ChainingModeCFB");

	internal static SafeAlgorithmHandle GetSharedHandle(CipherMode cipherMode, int feedback)
	{
		switch (cipherMode)
		{
		case CipherMode.CFB:
			if (feedback != 1)
			{
				break;
			}
			return s_hAlgCfb8.Value;
		case CipherMode.CBC:
			return s_hAlgCbc.Value;
		case CipherMode.ECB:
			return s_hAlgEcb.Value;
		}
		throw new NotSupportedException();
	}

	private static Lazy<SafeAlgorithmHandle> OpenDesAlgorithm(string cipherMode)
	{
		return new Lazy<SafeAlgorithmHandle>(delegate
		{
			SafeAlgorithmHandle safeAlgorithmHandle = Cng.BCryptOpenAlgorithmProvider("DES", null, Cng.OpenAlgorithmProviderFlags.NONE);
			safeAlgorithmHandle.SetCipherMode(cipherMode);
			return safeAlgorithmHandle;
		});
	}
}
