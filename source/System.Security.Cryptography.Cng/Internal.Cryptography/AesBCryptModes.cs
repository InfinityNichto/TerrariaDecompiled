using System;
using System.Security.Cryptography;
using Internal.NativeCrypto;

namespace Internal.Cryptography;

internal static class AesBCryptModes
{
	private static readonly Lazy<Internal.NativeCrypto.SafeAlgorithmHandle> s_hAlgCbc = OpenAesAlgorithm("ChainingModeCBC");

	private static readonly Lazy<Internal.NativeCrypto.SafeAlgorithmHandle> s_hAlgEcb = OpenAesAlgorithm("ChainingModeECB");

	private static readonly Lazy<Internal.NativeCrypto.SafeAlgorithmHandle> s_hAlgCfb128 = OpenAesAlgorithm("ChainingModeCFB", 16);

	private static readonly Lazy<Internal.NativeCrypto.SafeAlgorithmHandle> s_hAlgCfb8 = OpenAesAlgorithm("ChainingModeCFB", 1);

	internal static Internal.NativeCrypto.SafeAlgorithmHandle GetSharedHandle(CipherMode cipherMode, int feedback)
	{
		switch (cipherMode)
		{
		case CipherMode.CFB:
			switch (feedback)
			{
			case 16:
				return s_hAlgCfb128.Value;
			case 1:
				return s_hAlgCfb8.Value;
			}
			break;
		case CipherMode.CBC:
			return s_hAlgCbc.Value;
		case CipherMode.ECB:
			return s_hAlgEcb.Value;
		}
		throw new NotSupportedException();
	}

	internal static Lazy<Internal.NativeCrypto.SafeAlgorithmHandle> OpenAesAlgorithm(string cipherMode, int feedback = 0)
	{
		return new Lazy<Internal.NativeCrypto.SafeAlgorithmHandle>(delegate
		{
			Internal.NativeCrypto.SafeAlgorithmHandle safeAlgorithmHandle = Internal.NativeCrypto.Cng.BCryptOpenAlgorithmProvider("AES", null, Internal.NativeCrypto.Cng.OpenAlgorithmProviderFlags.NONE);
			safeAlgorithmHandle.SetCipherMode(cipherMode);
			if (feedback > 0 && feedback != 1)
			{
				try
				{
					safeAlgorithmHandle.SetFeedbackSize(feedback);
				}
				catch (CryptographicException inner)
				{
					throw new CryptographicException(System.SR.Cryptography_FeedbackSizeNotSupported, inner);
				}
			}
			return safeAlgorithmHandle;
		});
	}
}
