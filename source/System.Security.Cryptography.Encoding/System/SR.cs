using System.Resources;
using FxResources.System.Security.Cryptography.Encoding;

namespace System;

internal static class SR
{
	private static readonly bool s_usingResourceKeys = AppContext.TryGetSwitch("System.Resources.UseSystemResourceKeys", out var isEnabled) && isEnabled;

	private static ResourceManager s_resourceManager;

	internal static ResourceManager ResourceManager => s_resourceManager ?? (s_resourceManager = new ResourceManager(typeof(SR)));

	internal static string Cryptography_Oid_InvalidValue => GetResourceString("Cryptography_Oid_InvalidValue");

	internal static string Cryptography_Oid_InvalidName => GetResourceString("Cryptography_Oid_InvalidName");

	internal static string Cryptography_Oid_SetOnceValue => GetResourceString("Cryptography_Oid_SetOnceValue");

	internal static string Cryptography_Oid_SetOnceFriendlyName => GetResourceString("Cryptography_Oid_SetOnceFriendlyName");

	internal static string Arg_RankMultiDimNotSupported => GetResourceString("Arg_RankMultiDimNotSupported");

	internal static string ArgumentOutOfRange_Index => GetResourceString("ArgumentOutOfRange_Index");

	internal static string ArgumentOutOfRange_NeedNonNegNum => GetResourceString("ArgumentOutOfRange_NeedNonNegNum");

	internal static string Argument_InvalidOffLen => GetResourceString("Argument_InvalidOffLen");

	internal static string Argument_PemEncoding_NoPemFound => GetResourceString("Argument_PemEncoding_NoPemFound");

	internal static string Argument_PemEncoding_InvalidLabel => GetResourceString("Argument_PemEncoding_InvalidLabel");

	internal static string Argument_PemEncoding_EncodedSizeTooLarge => GetResourceString("Argument_PemEncoding_EncodedSizeTooLarge");

	internal static string ArgumentOutOfRange_NeedPositiveNumber => GetResourceString("ArgumentOutOfRange_NeedPositiveNumber");

	internal static string ObjectDisposed_Generic => GetResourceString("ObjectDisposed_Generic");

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
}
