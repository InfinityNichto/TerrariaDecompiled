using System.Resources;
using FxResources.System.IO.IsolatedStorage;

namespace System;

internal static class SR
{
	private static readonly bool s_usingResourceKeys = AppContext.TryGetSwitch("System.Resources.UseSystemResourceKeys", out var isEnabled) && isEnabled;

	private static ResourceManager s_resourceManager;

	internal static ResourceManager ResourceManager => s_resourceManager ?? (s_resourceManager = new ResourceManager(typeof(SR)));

	internal static string IsolatedStorage_StoreNotOpen => GetResourceString("IsolatedStorage_StoreNotOpen");

	internal static string IsolatedStorage_Operation_ISFS => GetResourceString("IsolatedStorage_Operation_ISFS");

	internal static string IsolatedStorage_Path => GetResourceString("IsolatedStorage_Path");

	internal static string IsolatedStorage_FileOpenMode => GetResourceString("IsolatedStorage_FileOpenMode");

	internal static string IsolatedStorage_Operation => GetResourceString("IsolatedStorage_Operation");

	internal static string IsolatedStorage_DeleteFile => GetResourceString("IsolatedStorage_DeleteFile");

	internal static string IsolatedStorage_CreateDirectory => GetResourceString("IsolatedStorage_CreateDirectory");

	internal static string IsolatedStorage_DeleteDirectory => GetResourceString("IsolatedStorage_DeleteDirectory");

	internal static string IsolatedStorage_Exception => GetResourceString("IsolatedStorage_Exception");

	internal static string IsolatedStorage_Init => GetResourceString("IsolatedStorage_Init");

	internal static string IsolatedStorage_QuotaIsUndefined => GetResourceString("IsolatedStorage_QuotaIsUndefined");

	internal static string IsolatedStorage_NotValidOnDesktop => GetResourceString("IsolatedStorage_NotValidOnDesktop");

	internal static string Argument_EmptyPath => GetResourceString("Argument_EmptyPath");

	internal static string PathNotFound_Path => GetResourceString("PathNotFound_Path");

	internal static string IsolatedStorage_ApplicationUndefined => GetResourceString("IsolatedStorage_ApplicationUndefined");

	internal static string IsolatedStorage_AssemblyUndefined => GetResourceString("IsolatedStorage_AssemblyUndefined");

	internal static string IsolatedStorage_CurrentSizeUndefined => GetResourceString("IsolatedStorage_CurrentSizeUndefined");

	internal static string IsolatedStorage_DeleteDirectories => GetResourceString("IsolatedStorage_DeleteDirectories");

	internal static string IsolatedStorage_Scope_Invalid => GetResourceString("IsolatedStorage_Scope_Invalid");

	internal static string IsolatedStorage_Scope_U_R_M => GetResourceString("IsolatedStorage_Scope_U_R_M");

	internal static string PlatformNotSupported_CAS => GetResourceString("PlatformNotSupported_CAS");

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
