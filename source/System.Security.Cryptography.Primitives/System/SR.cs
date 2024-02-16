using System.Resources;
using FxResources.System.Security.Cryptography.Primitives;

namespace System;

internal static class SR
{
	private static readonly bool s_usingResourceKeys = AppContext.TryGetSwitch("System.Resources.UseSystemResourceKeys", out var isEnabled) && isEnabled;

	private static ResourceManager s_resourceManager;

	internal static ResourceManager ResourceManager => s_resourceManager ?? (s_resourceManager = new ResourceManager(typeof(SR)));

	internal static string Arg_CryptographyException => GetResourceString("Arg_CryptographyException");

	internal static string Argument_DestinationTooShort => GetResourceString("Argument_DestinationTooShort");

	internal static string Argument_InvalidOffLen => GetResourceString("Argument_InvalidOffLen");

	internal static string Argument_InvalidValue => GetResourceString("Argument_InvalidValue");

	internal static string Argument_StreamNotReadable => GetResourceString("Argument_StreamNotReadable");

	internal static string Argument_StreamNotWritable => GetResourceString("Argument_StreamNotWritable");

	internal static string Argument_BitsMustBeWholeBytes => GetResourceString("Argument_BitsMustBeWholeBytes");

	internal static string ArgumentOutOfRange_NeedNonNegNum => GetResourceString("ArgumentOutOfRange_NeedNonNegNum");

	internal static string ArgumentOutOfRange_NeedPosNum => GetResourceString("ArgumentOutOfRange_NeedPosNum");

	internal static string Cryptography_CryptoStream_FlushFinalBlockTwice => GetResourceString("Cryptography_CryptoStream_FlushFinalBlockTwice");

	internal static string Cryptography_DefaultAlgorithm_NotSupported => GetResourceString("Cryptography_DefaultAlgorithm_NotSupported");

	internal static string Cryptography_HashNotYetFinalized => GetResourceString("Cryptography_HashNotYetFinalized");

	internal static string Cryptography_InvalidFeedbackSize => GetResourceString("Cryptography_InvalidFeedbackSize");

	internal static string Cryptography_InvalidBlockSize => GetResourceString("Cryptography_InvalidBlockSize");

	internal static string Cryptography_InvalidCipherMode => GetResourceString("Cryptography_InvalidCipherMode");

	internal static string Cryptography_InvalidIVSize => GetResourceString("Cryptography_InvalidIVSize");

	internal static string Cryptography_InvalidKeySize => GetResourceString("Cryptography_InvalidKeySize");

	internal static string Cryptography_InvalidPaddingMode => GetResourceString("Cryptography_InvalidPaddingMode");

	internal static string Cryptography_InvalidHashAlgorithmOid => GetResourceString("Cryptography_InvalidHashAlgorithmOid");

	internal static string Cryptography_MatchBlockSize => GetResourceString("Cryptography_MatchBlockSize");

	internal static string Cryptography_MatchFeedbackSize => GetResourceString("Cryptography_MatchFeedbackSize");

	internal static string Cryptography_PlaintextTooLarge => GetResourceString("Cryptography_PlaintextTooLarge");

	internal static string Cryptography_EncryptedIncorrectLength => GetResourceString("Cryptography_EncryptedIncorrectLength");

	internal static string NotSupported_SubclassOverride => GetResourceString("NotSupported_SubclassOverride");

	internal static string NotSupported_UnreadableStream => GetResourceString("NotSupported_UnreadableStream");

	internal static string NotSupported_UnseekableStream => GetResourceString("NotSupported_UnseekableStream");

	internal static string NotSupported_UnwritableStream => GetResourceString("NotSupported_UnwritableStream");

	internal static string HashNameMultipleSetNotSupported => GetResourceString("HashNameMultipleSetNotSupported");

	internal static string CryptoConfigNotSupported => GetResourceString("CryptoConfigNotSupported");

	internal static string InvalidOperation_IncorrectImplementation => GetResourceString("InvalidOperation_IncorrectImplementation");

	internal static string InvalidOperation_UnsupportedBlockSize => GetResourceString("InvalidOperation_UnsupportedBlockSize");

	private static bool UsingResourceKeys()
	{
		return s_usingResourceKeys;
	}

	internal static string GetResourceString(string resourceKey)
	{
		if (UsingResourceKeys())
		{
			return resourceKey;
		}
		string result = null;
		try
		{
			result = ResourceManager.GetString(resourceKey);
		}
		catch (MissingManifestResourceException)
		{
		}
		return result;
	}

	internal static string Format(string resourceFormat, object p1)
	{
		if (UsingResourceKeys())
		{
			return string.Join(", ", resourceFormat, p1);
		}
		return string.Format(resourceFormat, p1);
	}
}
