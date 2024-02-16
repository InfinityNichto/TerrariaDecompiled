using System.Resources;
using FxResources.System.Runtime.InteropServices;

namespace System;

internal static class SR
{
	private static readonly bool s_usingResourceKeys = AppContext.TryGetSwitch("System.Resources.UseSystemResourceKeys", out var isEnabled) && isEnabled;

	private static ResourceManager s_resourceManager;

	internal static ResourceManager ResourceManager => s_resourceManager ?? (s_resourceManager = new ResourceManager(typeof(FxResources.System.Runtime.InteropServices.SR)));

	internal static string InvalidOperation_HCCountOverflow => GetResourceString("InvalidOperation_HCCountOverflow");

	internal static string Arg_NeedNonNegNumRequired => GetResourceString("Arg_NeedNonNegNumRequired");

	internal static string Arg_InvalidThreshold => GetResourceString("Arg_InvalidThreshold");

	internal static string InvalidOperation_NoComEventInterfaceAttribute => GetResourceString("InvalidOperation_NoComEventInterfaceAttribute");

	internal static string AmbiguousMatch_MultipleEventInterfaceAttributes => GetResourceString("AmbiguousMatch_MultipleEventInterfaceAttributes");

	internal static string InvalidOperation_NoDispIdAttribute => GetResourceString("InvalidOperation_NoDispIdAttribute");

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
