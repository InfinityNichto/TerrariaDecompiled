using System.Resources;
using FxResources.System.Linq;

namespace System;

internal static class SR
{
	private static readonly bool s_usingResourceKeys = AppContext.TryGetSwitch("System.Resources.UseSystemResourceKeys", out var isEnabled) && isEnabled;

	private static ResourceManager s_resourceManager;

	internal static ResourceManager ResourceManager => s_resourceManager ?? (s_resourceManager = new ResourceManager(typeof(SR)));

	internal static string EmptyEnumerable => GetResourceString("EmptyEnumerable");

	internal static string MoreThanOneElement => GetResourceString("MoreThanOneElement");

	internal static string MoreThanOneMatch => GetResourceString("MoreThanOneMatch");

	internal static string NoElements => GetResourceString("NoElements");

	internal static string NoMatch => GetResourceString("NoMatch");

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
