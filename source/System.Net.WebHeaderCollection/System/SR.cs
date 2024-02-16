using System.Resources;
using FxResources.System.Net.WebHeaderCollection;

namespace System;

internal static class SR
{
	private static readonly bool s_usingResourceKeys = AppContext.TryGetSwitch("System.Resources.UseSystemResourceKeys", out var isEnabled) && isEnabled;

	private static ResourceManager s_resourceManager;

	internal static ResourceManager ResourceManager => s_resourceManager ?? (s_resourceManager = new ResourceManager(typeof(SR)));

	internal static string net_headers_req => GetResourceString("net_headers_req");

	internal static string net_headers_rsp => GetResourceString("net_headers_rsp");

	internal static string net_emptyStringCall => GetResourceString("net_emptyStringCall");

	internal static string net_WebHeaderInvalidControlChars => GetResourceString("net_WebHeaderInvalidControlChars");

	internal static string net_WebHeaderInvalidCRLFChars => GetResourceString("net_WebHeaderInvalidCRLFChars");

	internal static string net_WebHeaderInvalidHeaderChars => GetResourceString("net_WebHeaderInvalidHeaderChars");

	internal static string net_WebHeaderMissingColon => GetResourceString("net_WebHeaderMissingColon");

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
