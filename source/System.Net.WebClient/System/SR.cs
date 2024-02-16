using System.Resources;
using FxResources.System.Net.WebClient;

namespace System;

internal static class SR
{
	private static readonly bool s_usingResourceKeys = AppContext.TryGetSwitch("System.Resources.UseSystemResourceKeys", out var isEnabled) && isEnabled;

	private static ResourceManager s_resourceManager;

	internal static ResourceManager ResourceManager => s_resourceManager ?? (s_resourceManager = new ResourceManager(typeof(SR)));

	internal static string net_webclient => GetResourceString("net_webclient");

	internal static string net_webclient_ContentType => GetResourceString("net_webclient_ContentType");

	internal static string net_webclient_Multipart => GetResourceString("net_webclient_Multipart");

	internal static string net_webclient_no_concurrent_io_allowed => GetResourceString("net_webclient_no_concurrent_io_allowed");

	internal static string net_webclient_invalid_baseaddress => GetResourceString("net_webclient_invalid_baseaddress");

	internal static string net_webstatus_MessageLengthLimitExceeded => GetResourceString("net_webstatus_MessageLengthLimitExceeded");

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
