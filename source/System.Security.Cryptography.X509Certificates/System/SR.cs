using System.Resources;
using FxResources.System.Security.Cryptography.X509Certificates;

namespace System;

internal static class SR
{
	private static readonly bool s_usingResourceKeys = AppContext.TryGetSwitch("System.Resources.UseSystemResourceKeys", out var isEnabled) && isEnabled;

	private static ResourceManager s_resourceManager;

	internal static ResourceManager ResourceManager => s_resourceManager ?? (s_resourceManager = new ResourceManager(typeof(SR)));

	internal static string Arg_EmptyOrNullArray => GetResourceString("Arg_EmptyOrNullArray");

	internal static string Arg_EmptyOrNullString => GetResourceString("Arg_EmptyOrNullString");

	internal static string Arg_EmptyOrNullString_Named => GetResourceString("Arg_EmptyOrNullString_Named");

	internal static string Arg_EnumIllegalVal => GetResourceString("Arg_EnumIllegalVal");

	internal static string Arg_InvalidHandle => GetResourceString("Arg_InvalidHandle");

	internal static string Arg_InvalidType => GetResourceString("Arg_InvalidType");

	internal static string Arg_OutOfRange_NeedNonNegNum => GetResourceString("Arg_OutOfRange_NeedNonNegNum");

	internal static string Arg_RankMultiDimNotSupported => GetResourceString("Arg_RankMultiDimNotSupported");

	internal static string Argument_InvalidFlag => GetResourceString("Argument_InvalidFlag");

	internal static string Argument_InvalidNameType => GetResourceString("Argument_InvalidNameType");

	internal static string Argument_InvalidOffLen => GetResourceString("Argument_InvalidOffLen");

	internal static string Argument_InvalidOidValue => GetResourceString("Argument_InvalidOidValue");

	internal static string ArgumentOutOfRange_Index => GetResourceString("ArgumentOutOfRange_Index");

	internal static string Cryptography_Cert_AlreadyHasPrivateKey => GetResourceString("Cryptography_Cert_AlreadyHasPrivateKey");

	internal static string Cryptography_CertReq_AlgorithmMustMatch => GetResourceString("Cryptography_CertReq_AlgorithmMustMatch");

	internal static string Cryptography_CertReq_BasicConstraintsRequired => GetResourceString("Cryptography_CertReq_BasicConstraintsRequired");

	internal static string Cryptography_CertReq_DatesReversed => GetResourceString("Cryptography_CertReq_DatesReversed");

	internal static string Cryptography_CertReq_DuplicateExtension => GetResourceString("Cryptography_CertReq_DuplicateExtension");

	internal static string Cryptography_CertReq_IssuerBasicConstraintsInvalid => GetResourceString("Cryptography_CertReq_IssuerBasicConstraintsInvalid");

	internal static string Cryptography_CertReq_IssuerKeyUsageInvalid => GetResourceString("Cryptography_CertReq_IssuerKeyUsageInvalid");

	internal static string Cryptography_CertReq_IssuerRequiresPrivateKey => GetResourceString("Cryptography_CertReq_IssuerRequiresPrivateKey");

	internal static string Cryptography_CertReq_NotAfterNotNested => GetResourceString("Cryptography_CertReq_NotAfterNotNested");

	internal static string Cryptography_CertReq_NotBeforeNotNested => GetResourceString("Cryptography_CertReq_NotBeforeNotNested");

	internal static string Cryptography_CertReq_NoKeyProvided => GetResourceString("Cryptography_CertReq_NoKeyProvided");

	internal static string Cryptography_CertReq_RSAPaddingRequired => GetResourceString("Cryptography_CertReq_RSAPaddingRequired");

	internal static string Cryptography_ECC_NamedCurvesOnly => GetResourceString("Cryptography_ECC_NamedCurvesOnly");

	internal static string Cryptography_Der_Invalid_Encoding => GetResourceString("Cryptography_Der_Invalid_Encoding");

	internal static string Cryptography_HashAlgorithmNameNullOrEmpty => GetResourceString("Cryptography_HashAlgorithmNameNullOrEmpty");

	internal static string Cryptography_InvalidContextHandle => GetResourceString("Cryptography_InvalidContextHandle");

	internal static string Cryptography_InvalidHandle => GetResourceString("Cryptography_InvalidHandle");

	internal static string Cryptography_InvalidPaddingMode => GetResourceString("Cryptography_InvalidPaddingMode");

	internal static string Cryptography_InvalidStoreHandle => GetResourceString("Cryptography_InvalidStoreHandle");

	internal static string Cryptography_CustomTrustCertsInSystemMode => GetResourceString("Cryptography_CustomTrustCertsInSystemMode");

	internal static string Cryptography_InvalidTrustCertificate => GetResourceString("Cryptography_InvalidTrustCertificate");

	internal static string Cryptography_Pfx_NoCertificates => GetResourceString("Cryptography_Pfx_NoCertificates");

	internal static string Cryptography_PrivateKey_DoesNotMatch => GetResourceString("Cryptography_PrivateKey_DoesNotMatch");

	internal static string Cryptography_PrivateKey_WrongAlgorithm => GetResourceString("Cryptography_PrivateKey_WrongAlgorithm");

	internal static string Cryptography_UnknownHashAlgorithm => GetResourceString("Cryptography_UnknownHashAlgorithm");

	internal static string Cryptography_UnknownKeyAlgorithm => GetResourceString("Cryptography_UnknownKeyAlgorithm");

	internal static string Cryptography_X509_ExtensionMismatch => GetResourceString("Cryptography_X509_ExtensionMismatch");

	internal static string Cryptography_X509_InvalidContentType => GetResourceString("Cryptography_X509_InvalidContentType");

	internal static string Cryptography_X509_InvalidFindType => GetResourceString("Cryptography_X509_InvalidFindType");

	internal static string Cryptography_X509_InvalidFindValue => GetResourceString("Cryptography_X509_InvalidFindValue");

	internal static string Cryptography_X509_InvalidFlagCombination => GetResourceString("Cryptography_X509_InvalidFlagCombination");

	internal static string Cryptography_X509_StoreNotOpen => GetResourceString("Cryptography_X509_StoreNotOpen");

	internal static string Cryptography_X509_NoPemCertificate => GetResourceString("Cryptography_X509_NoPemCertificate");

	internal static string Cryptography_X509_NoOrMismatchedPemKey => GetResourceString("Cryptography_X509_NoOrMismatchedPemKey");

	internal static string InvalidOperation_EnumNotStarted => GetResourceString("InvalidOperation_EnumNotStarted");

	internal static string NotSupported_ECDsa_Csp => GetResourceString("NotSupported_ECDsa_Csp");

	internal static string NotSupported_ECDiffieHellman_Csp => GetResourceString("NotSupported_ECDiffieHellman_Csp");

	internal static string NotSupported_KeyAlgorithm => GetResourceString("NotSupported_KeyAlgorithm");

	internal static string NotSupported_ImmutableX509Certificate => GetResourceString("NotSupported_ImmutableX509Certificate");

	internal static string Unknown_Error => GetResourceString("Unknown_Error");

	internal static string Cryptography_Invalid_IA5String => GetResourceString("Cryptography_Invalid_IA5String");

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
