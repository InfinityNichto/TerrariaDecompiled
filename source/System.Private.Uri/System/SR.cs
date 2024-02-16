using System.Resources;
using FxResources.System.Private.Uri;

namespace System;

internal static class SR
{
	private static readonly bool s_usingResourceKeys = AppContext.TryGetSwitch("System.Resources.UseSystemResourceKeys", out var isEnabled) && isEnabled;

	private static ResourceManager s_resourceManager;

	internal static ResourceManager ResourceManager => s_resourceManager ?? (s_resourceManager = new ResourceManager(typeof(SR)));

	internal static string net_uri_BadAuthority => GetResourceString("net_uri_BadAuthority");

	internal static string net_uri_BadAuthorityTerminator => GetResourceString("net_uri_BadAuthorityTerminator");

	internal static string net_uri_BadFormat => GetResourceString("net_uri_BadFormat");

	internal static string net_uri_NeedFreshParser => GetResourceString("net_uri_NeedFreshParser");

	internal static string net_uri_AlreadyRegistered => GetResourceString("net_uri_AlreadyRegistered");

	internal static string net_uri_BadHostName => GetResourceString("net_uri_BadHostName");

	internal static string net_uri_BadPort => GetResourceString("net_uri_BadPort");

	internal static string net_uri_BadScheme => GetResourceString("net_uri_BadScheme");

	internal static string net_uri_BadString => GetResourceString("net_uri_BadString");

	internal static string net_uri_BadUserPassword => GetResourceString("net_uri_BadUserPassword");

	internal static string net_uri_CannotCreateRelative => GetResourceString("net_uri_CannotCreateRelative");

	internal static string net_uri_SchemeLimit => GetResourceString("net_uri_SchemeLimit");

	internal static string net_uri_EmptyUri => GetResourceString("net_uri_EmptyUri");

	internal static string net_uri_InvalidUriKind => GetResourceString("net_uri_InvalidUriKind");

	internal static string net_uri_MustRootedPath => GetResourceString("net_uri_MustRootedPath");

	internal static string net_uri_NotAbsolute => GetResourceString("net_uri_NotAbsolute");

	internal static string net_uri_PortOutOfRange => GetResourceString("net_uri_PortOutOfRange");

	internal static string net_uri_SizeLimit => GetResourceString("net_uri_SizeLimit");

	internal static string net_uri_UserDrivenParsing => GetResourceString("net_uri_UserDrivenParsing");

	internal static string net_uri_NotJustSerialization => GetResourceString("net_uri_NotJustSerialization");

	internal static string net_uri_BadUnicodeHostForIdn => GetResourceString("net_uri_BadUnicodeHostForIdn");

	internal static string Argument_ExtraNotValid => GetResourceString("Argument_ExtraNotValid");

	internal static string Argument_InvalidUriSubcomponent => GetResourceString("Argument_InvalidUriSubcomponent");

	internal static string InvalidNullArgument => GetResourceString("InvalidNullArgument");

	internal static string net_uri_InitializeCalledAlreadyOrTooLate => GetResourceString("net_uri_InitializeCalledAlreadyOrTooLate");

	internal static string net_uri_GetComponentsCalledWhenCanonicalizationDisabled => GetResourceString("net_uri_GetComponentsCalledWhenCanonicalizationDisabled");

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
