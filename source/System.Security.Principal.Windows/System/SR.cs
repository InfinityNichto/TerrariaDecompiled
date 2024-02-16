using System.Resources;
using FxResources.System.Security.Principal.Windows;

namespace System;

internal static class SR
{
	private static readonly bool s_usingResourceKeys = AppContext.TryGetSwitch("System.Resources.UseSystemResourceKeys", out var isEnabled) && isEnabled;

	private static ResourceManager s_resourceManager;

	internal static ResourceManager ResourceManager => s_resourceManager ?? (s_resourceManager = new ResourceManager(typeof(SR)));

	internal static string Arg_EmptyCollection => GetResourceString("Arg_EmptyCollection");

	internal static string Arg_EnumIllegalVal => GetResourceString("Arg_EnumIllegalVal");

	internal static string Argument_ImpersonateUser => GetResourceString("Argument_ImpersonateUser");

	internal static string Argument_ImproperType => GetResourceString("Argument_ImproperType");

	internal static string Argument_InvalidImpersonationToken => GetResourceString("Argument_InvalidImpersonationToken");

	internal static string Argument_InvalidValue => GetResourceString("Argument_InvalidValue");

	internal static string Argument_TokenZero => GetResourceString("Argument_TokenZero");

	internal static string ArgumentOutOfRange_ArrayTooSmall => GetResourceString("ArgumentOutOfRange_ArrayTooSmall");

	internal static string ArgumentOutOfRange_NeedNonNegNum => GetResourceString("ArgumentOutOfRange_NeedNonNegNum");

	internal static string Argument_StringZeroLength => GetResourceString("Argument_StringZeroLength");

	internal static string IdentityReference_AccountNameTooLong => GetResourceString("IdentityReference_AccountNameTooLong");

	internal static string IdentityReference_CannotCreateLogonIdsSid => GetResourceString("IdentityReference_CannotCreateLogonIdsSid");

	internal static string IdentityReference_DomainNameTooLong => GetResourceString("IdentityReference_DomainNameTooLong");

	internal static string IdentityReference_DomainSidRequired => GetResourceString("IdentityReference_DomainSidRequired");

	internal static string IdentityReference_IdentifierAuthorityTooLarge => GetResourceString("IdentityReference_IdentifierAuthorityTooLarge");

	internal static string IdentityReference_IdentityNotMapped => GetResourceString("IdentityReference_IdentityNotMapped");

	internal static string IdentityReference_InvalidNumberOfSubauthorities => GetResourceString("IdentityReference_InvalidNumberOfSubauthorities");

	internal static string IdentityReference_InvalidSidRevision => GetResourceString("IdentityReference_InvalidSidRevision");

	internal static string IdentityReference_MustBeIdentityReference => GetResourceString("IdentityReference_MustBeIdentityReference");

	internal static string IdentityReference_NotAWindowsDomain => GetResourceString("IdentityReference_NotAWindowsDomain");

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
