using System.Resources;
using FxResources.System.Threading.Channels;

namespace System;

internal static class SR
{
	private static readonly bool s_usingResourceKeys = AppContext.TryGetSwitch("System.Resources.UseSystemResourceKeys", out var isEnabled) && isEnabled;

	private static ResourceManager s_resourceManager;

	internal static ResourceManager ResourceManager => s_resourceManager ?? (s_resourceManager = new ResourceManager(typeof(SR)));

	internal static string ChannelClosedException_DefaultMessage => GetResourceString("ChannelClosedException_DefaultMessage");

	internal static string InvalidOperation_IncompleteAsyncOperation => GetResourceString("InvalidOperation_IncompleteAsyncOperation");

	internal static string InvalidOperation_MultipleContinuations => GetResourceString("InvalidOperation_MultipleContinuations");

	internal static string InvalidOperation_IncorrectToken => GetResourceString("InvalidOperation_IncorrectToken");

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
