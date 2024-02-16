using System.Resources;
using FxResources.System.Net.Ping;

namespace System;

internal static class SR
{
	private static readonly bool s_usingResourceKeys = AppContext.TryGetSwitch("System.Resources.UseSystemResourceKeys", out var isEnabled) && isEnabled;

	private static ResourceManager s_resourceManager;

	internal static ResourceManager ResourceManager => s_resourceManager ?? (s_resourceManager = new ResourceManager(typeof(SR)));

	internal static string net_inasync => GetResourceString("net_inasync");

	internal static string net_invalidPingBufferSize => GetResourceString("net_invalidPingBufferSize");

	internal static string net_invalid_ip_addr => GetResourceString("net_invalid_ip_addr");

	internal static string net_ping => GetResourceString("net_ping");

	internal static string net_ipv4_not_installed => GetResourceString("net_ipv4_not_installed");

	internal static string net_ipv6_not_installed => GetResourceString("net_ipv6_not_installed");

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
