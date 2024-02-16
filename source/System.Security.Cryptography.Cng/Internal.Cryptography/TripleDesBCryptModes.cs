using System;
using System.Security.Cryptography;
using Internal.NativeCrypto;

namespace Internal.Cryptography;

internal static class TripleDesBCryptModes
{
	private static readonly Lazy<Internal.NativeCrypto.SafeAlgorithmHandle> s_hAlgCbc = Open3DesAlgorithm("ChainingModeCBC");

	private static readonly Lazy<Internal.NativeCrypto.SafeAlgorithmHandle> s_hAlgEcb = Open3DesAlgorithm("ChainingModeECB");

	private static readonly Lazy<Internal.NativeCrypto.SafeAlgorithmHandle> s_hAlgCfb8 = Open3DesAlgorithm("ChainingModeCFB", 1);

	private static readonly Lazy<Internal.NativeCrypto.SafeAlgorithmHandle> s_hAlgCfb64 = Open3DesAlgorithm("ChainingModeCFB", 8);

	internal static Internal.NativeCrypto.SafeAlgorithmHandle GetSharedHandle(CipherMode cipherMode, int feedback)
	{
		switch (cipherMode)
		{
		case CipherMode.CFB:
			switch (feedback)
			{
			case 1:
				return s_hAlgCfb8.Value;
			case 8:
				return s_hAlgCfb64.Value;
			}
			break;
		case CipherMode.CBC:
			return s_hAlgCbc.Value;
		case CipherMode.ECB:
			return s_hAlgEcb.Value;
		}
		throw new NotSupportedException();
	}

	private static Lazy<Internal.NativeCrypto.SafeAlgorithmHandle> Open3DesAlgorithm(string cipherMode, int feedback = 0)
	{
		return new Lazy<Internal.NativeCrypto.SafeAlgorithmHandle>(delegate
		{
			Internal.NativeCrypto.SafeAlgorithmHandle safeAlgorithmHandle = Internal.NativeCrypto.Cng.BCryptOpenAlgorithmProvider("3DES", null, Internal.NativeCrypto.Cng.OpenAlgorithmProviderFlags.NONE);
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
