using System.Resources;
using FxResources.System.Security.Cryptography.Cng;

namespace System;

internal static class SR
{
	private static readonly bool s_usingResourceKeys = AppContext.TryGetSwitch("System.Resources.UseSystemResourceKeys", out var isEnabled) && isEnabled;

	private static ResourceManager s_resourceManager;

	internal static ResourceManager ResourceManager => s_resourceManager ?? (s_resourceManager = new ResourceManager(typeof(SR)));

	internal static string ArgumentOutOfRange_NeedNonNegNum => GetResourceString("ArgumentOutOfRange_NeedNonNegNum");

	internal static string Argument_Invalid_SafeHandleInvalidOrClosed => GetResourceString("Argument_Invalid_SafeHandleInvalidOrClosed");

	internal static string Argument_InvalidOffLen => GetResourceString("Argument_InvalidOffLen");

	internal static string Argument_InvalidValue => GetResourceString("Argument_InvalidValue");

	internal static string ArgumentNull_Buffer => GetResourceString("ArgumentNull_Buffer");

	internal static string Cryptography_AlgorithmNotSupported => GetResourceString("Cryptography_AlgorithmNotSupported");

	internal static string Cryptography_AlgKdfRequiresChars => GetResourceString("Cryptography_AlgKdfRequiresChars");

	internal static string Cryptography_ArgECDHKeySizeMismatch => GetResourceString("Cryptography_ArgECDHKeySizeMismatch");

	internal static string Cryptography_ArgECDHRequiresECDHKey => GetResourceString("Cryptography_ArgECDHRequiresECDHKey");

	internal static string Cryptography_TlsRequiresLabelAndSeed => GetResourceString("Cryptography_TlsRequiresLabelAndSeed");

	internal static string Cryptography_ArgDSARequiresDSAKey => GetResourceString("Cryptography_ArgDSARequiresDSAKey");

	internal static string Cryptography_ArgECDsaRequiresECDsaKey => GetResourceString("Cryptography_ArgECDsaRequiresECDsaKey");

	internal static string Cryptography_ArgRSARequiresRSAKey => GetResourceString("Cryptography_ArgRSARequiresRSAKey");

	internal static string Cryptography_CipherModeFeedbackNotSupported => GetResourceString("Cryptography_CipherModeFeedbackNotSupported");

	internal static string Cryptography_CngKeyWrongAlgorithm => GetResourceString("Cryptography_CngKeyWrongAlgorithm");

	internal static string Cryptography_Der_Invalid_Encoding => GetResourceString("Cryptography_Der_Invalid_Encoding");

	internal static string Cryptography_Encryption_MessageTooLong => GetResourceString("Cryptography_Encryption_MessageTooLong");

	internal static string Cryptography_HashAlgorithmNameNullOrEmpty => GetResourceString("Cryptography_HashAlgorithmNameNullOrEmpty");

	internal static string Cryptography_InvalidAlgorithmGroup => GetResourceString("Cryptography_InvalidAlgorithmGroup");

	internal static string Cryptography_InvalidAlgorithmName => GetResourceString("Cryptography_InvalidAlgorithmName");

	internal static string Cryptography_InvalidCipherMode => GetResourceString("Cryptography_InvalidCipherMode");

	internal static string Cryptography_InvalidDsaParameters_MissingFields => GetResourceString("Cryptography_InvalidDsaParameters_MissingFields");

	internal static string Cryptography_InvalidDsaParameters_MismatchedPGY => GetResourceString("Cryptography_InvalidDsaParameters_MismatchedPGY");

	internal static string Cryptography_InvalidDsaParameters_MismatchedQX => GetResourceString("Cryptography_InvalidDsaParameters_MismatchedQX");

	internal static string Cryptography_InvalidDsaParameters_MismatchedPJ => GetResourceString("Cryptography_InvalidDsaParameters_MismatchedPJ");

	internal static string Cryptography_InvalidDsaParameters_SeedRestriction_ShortKey => GetResourceString("Cryptography_InvalidDsaParameters_SeedRestriction_ShortKey");

	internal static string Cryptography_InvalidDsaParameters_QRestriction_ShortKey => GetResourceString("Cryptography_InvalidDsaParameters_QRestriction_ShortKey");

	internal static string Cryptography_InvalidDsaParameters_QRestriction_LargeKey => GetResourceString("Cryptography_InvalidDsaParameters_QRestriction_LargeKey");

	internal static string Cryptography_CurveNotSupported => GetResourceString("Cryptography_CurveNotSupported");

	internal static string Cryptography_InvalidCurveOid => GetResourceString("Cryptography_InvalidCurveOid");

	internal static string Cryptography_InvalidIVSize => GetResourceString("Cryptography_InvalidIVSize");

	internal static string Cryptography_InvalidKeyBlobFormat => GetResourceString("Cryptography_InvalidKeyBlobFormat");

	internal static string Cryptography_InvalidKeySize => GetResourceString("Cryptography_InvalidKeySize");

	internal static string Cryptography_InvalidPadding => GetResourceString("Cryptography_InvalidPadding");

	internal static string Cryptography_InvalidProviderName => GetResourceString("Cryptography_InvalidProviderName");

	internal static string Cryptography_InvalidRsaParameters => GetResourceString("Cryptography_InvalidRsaParameters");

	internal static string Cryptography_KeyBlobParsingError => GetResourceString("Cryptography_KeyBlobParsingError");

	internal static string Cryptography_KeyTooSmall => GetResourceString("Cryptography_KeyTooSmall");

	internal static string Cryptography_OpenEphemeralKeyHandleWithoutEphemeralFlag => GetResourceString("Cryptography_OpenEphemeralKeyHandleWithoutEphemeralFlag");

	internal static string Cryptography_OpenInvalidHandle => GetResourceString("Cryptography_OpenInvalidHandle");

	internal static string Cryptography_MissingIV => GetResourceString("Cryptography_MissingIV");

	internal static string Cryptography_MustTransformWholeBlock => GetResourceString("Cryptography_MustTransformWholeBlock");

	internal static string Cryptography_NotValidPrivateKey => GetResourceString("Cryptography_NotValidPrivateKey");

	internal static string Cryptography_NotValidPublicOrPrivateKey => GetResourceString("Cryptography_NotValidPublicOrPrivateKey");

	internal static string Cryptography_PartialBlock => GetResourceString("Cryptography_PartialBlock");

	internal static string Cryptography_RSA_DecryptWrongSize => GetResourceString("Cryptography_RSA_DecryptWrongSize");

	internal static string Cryptography_Pkcs8_EncryptedReadFailed => GetResourceString("Cryptography_Pkcs8_EncryptedReadFailed");

	internal static string Cryptography_SignHash_WrongSize => GetResourceString("Cryptography_SignHash_WrongSize");

	internal static string Cryptography_UnsupportedPaddingMode => GetResourceString("Cryptography_UnsupportedPaddingMode");

	internal static string Cryptography_TransformBeyondEndOfBuffer => GetResourceString("Cryptography_TransformBeyondEndOfBuffer");

	internal static string Cryptography_UnexpectedTransformTruncation => GetResourceString("Cryptography_UnexpectedTransformTruncation");

	internal static string Cryptography_UnknownAlgorithmIdentifier => GetResourceString("Cryptography_UnknownAlgorithmIdentifier");

	internal static string Cryptography_UnknownHashAlgorithm => GetResourceString("Cryptography_UnknownHashAlgorithm");

	internal static string Cryptography_UnknownPaddingMode => GetResourceString("Cryptography_UnknownPaddingMode");

	internal static string Cryptography_WeakKey => GetResourceString("Cryptography_WeakKey");

	internal static string Cryptography_ECC_NamedCurvesOnly => GetResourceString("Cryptography_ECC_NamedCurvesOnly");

	internal static string Cryptography_CSP_NoPrivateKey => GetResourceString("Cryptography_CSP_NoPrivateKey");

	internal static string Cryptography_InvalidECCharacteristic2Curve => GetResourceString("Cryptography_InvalidECCharacteristic2Curve");

	internal static string Argument_DestinationTooShort => GetResourceString("Argument_DestinationTooShort");

	internal static string Cryptography_FeedbackSizeNotSupported => GetResourceString("Cryptography_FeedbackSizeNotSupported");

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
