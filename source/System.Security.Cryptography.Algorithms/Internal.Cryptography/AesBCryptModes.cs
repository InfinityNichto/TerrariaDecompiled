using System;
using System.Security.Cryptography;
using Internal.NativeCrypto;

namespace Internal.Cryptography;

internal static class AesBCryptModes
{
	private static readonly Lazy<SafeAlgorithmHandle> s_hAlgCbc = OpenAesAlgorithm("ChainingModeCBC");

	private static readonly Lazy<SafeAlgorithmHandle> s_hAlgEcb = OpenAesAlgorithm("ChainingModeECB");

	private static readonly Lazy<SafeAlgorithmHandle> s_hAlgCfb128 = OpenAesAlgorithm("ChainingModeCFB", 16);

	private static readonly Lazy<SafeAlgorithmHandle> s_hAlgCfb8 = OpenAesAlgorithm("ChainingModeCFB", 1);

	internal static SafeAlgorithmHandle GetSharedHandle(CipherMode cipherMode, int feedback)
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

	internal static Lazy<SafeAlgorithmHandle> OpenAesAlgorithm(string cipherMode, int feedback = 0)
	{
		return new Lazy<SafeAlgorithmHandle>(delegate
		{
			SafeAlgorithmHandle safeAlgorithmHandle = Cng.BCryptOpenAlgorithmProvider("AES", null, Cng.OpenAlgorithmProviderFlags.NONE);
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
