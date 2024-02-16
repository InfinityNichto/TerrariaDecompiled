using System.Resources;
using FxResources.System.Security.Cryptography.Csp;

namespace System;

internal static class SR
{
	private static readonly bool s_usingResourceKeys = AppContext.TryGetSwitch("System.Resources.UseSystemResourceKeys", out var isEnabled) && isEnabled;

	private static ResourceManager s_resourceManager;

	internal static ResourceManager ResourceManager => s_resourceManager ?? (s_resourceManager = new ResourceManager(typeof(SR)));

	internal static string Arg_EnumIllegalVal => GetResourceString("Arg_EnumIllegalVal");

	internal static string Argument_InvalidValue => GetResourceString("Argument_InvalidValue");

	internal static string ArgumentOutOfRange_NeedNonNegNum => GetResourceString("ArgumentOutOfRange_NeedNonNegNum");

	internal static string ArgumentOutOfRange_NeedPosNum => GetResourceString("ArgumentOutOfRange_NeedPosNum");

	internal static string Cryptography_CSP_NoPrivateKey => GetResourceString("Cryptography_CSP_NoPrivateKey");

	internal static string Cryptography_CSP_NotFound => GetResourceString("Cryptography_CSP_NotFound");

	internal static string Cryptography_CSP_WrongKeySpec => GetResourceString("Cryptography_CSP_WrongKeySpec");

	internal static string Cryptography_HashAlgorithmNameNullOrEmpty => GetResourceString("Cryptography_HashAlgorithmNameNullOrEmpty");

	internal static string Cryptography_InvalidDSASignatureSize => GetResourceString("Cryptography_InvalidDSASignatureSize");

	internal static string Cryptography_InvalidHashSize => GetResourceString("Cryptography_InvalidHashSize");

	internal static string Cryptography_InvalidIVSize => GetResourceString("Cryptography_InvalidIVSize");

	internal static string Cryptography_InvalidKey_Weak => GetResourceString("Cryptography_InvalidKey_Weak");

	internal static string Cryptography_InvalidKey_SemiWeak => GetResourceString("Cryptography_InvalidKey_SemiWeak");

	internal static string Cryptography_InvalidKeySize => GetResourceString("Cryptography_InvalidKeySize");

	internal static string Cryptography_InvalidOID => GetResourceString("Cryptography_InvalidOID");

	internal static string Cryptography_InvalidPadding => GetResourceString("Cryptography_InvalidPadding");

	internal static string Cryptography_InvalidPaddingMode => GetResourceString("Cryptography_InvalidPaddingMode");

	internal static string Cryptography_MissingIV => GetResourceString("Cryptography_MissingIV");

	internal static string Cryptography_MustTransformWholeBlock => GetResourceString("Cryptography_MustTransformWholeBlock");

	internal static string Cryptography_OpenInvalidHandle => GetResourceString("Cryptography_OpenInvalidHandle");

	internal static string Cryptography_PartialBlock => GetResourceString("Cryptography_PartialBlock");

	internal static string Cryptography_PasswordDerivedBytes_InvalidAlgorithm => GetResourceString("Cryptography_PasswordDerivedBytes_InvalidAlgorithm");

	internal static string Cryptography_PasswordDerivedBytes_InvalidIV => GetResourceString("Cryptography_PasswordDerivedBytes_InvalidIV");

	internal static string Cryptography_PasswordDerivedBytes_TooManyBytes => GetResourceString("Cryptography_PasswordDerivedBytes_TooManyBytes");

	internal static string Cryptography_PasswordDerivedBytes_ValuesFixed => GetResourceString("Cryptography_PasswordDerivedBytes_ValuesFixed");

	internal static string Cryptography_RC2_EKSKS2 => GetResourceString("Cryptography_RC2_EKSKS2");

	internal static string Cryptography_RSA_DecryptWrongSize => GetResourceString("Cryptography_RSA_DecryptWrongSize");

	internal static string Cryptography_TransformBeyondEndOfBuffer => GetResourceString("Cryptography_TransformBeyondEndOfBuffer");

	internal static string Cryptography_UnknownHashAlgorithm => GetResourceString("Cryptography_UnknownHashAlgorithm");

	internal static string Cryptography_UnknownPaddingMode => GetResourceString("Cryptography_UnknownPaddingMode");

	internal static string CryptSetKeyParam_Failed => GetResourceString("CryptSetKeyParam_Failed");

	internal static string CspParameter_invalid => GetResourceString("CspParameter_invalid");

	internal static string Argument_DestinationTooShort => GetResourceString("Argument_DestinationTooShort");

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

	internal static string Format(string resourceFormat, object p1, object p2)
	{
		if (UsingResourceKeys())
		{
			return string.Join(", ", resourceFormat, p1, p2);
		}
		return string.Format(resourceFormat, p1, p2);
	}
}
