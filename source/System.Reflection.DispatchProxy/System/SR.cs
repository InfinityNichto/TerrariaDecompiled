using System.Resources;
using FxResources.System.Reflection.DispatchProxy;

namespace System;

internal static class SR
{
	private static readonly bool s_usingResourceKeys = AppContext.TryGetSwitch("System.Resources.UseSystemResourceKeys", out var isEnabled) && isEnabled;

	private static ResourceManager s_resourceManager;

	internal static ResourceManager ResourceManager => s_resourceManager ?? (s_resourceManager = new ResourceManager(typeof(SR)));

	internal static string BaseType_Cannot_Be_Sealed => GetResourceString("BaseType_Cannot_Be_Sealed");

	internal static string BaseType_Must_Have_Default_Ctor => GetResourceString("BaseType_Must_Have_Default_Ctor");

	internal static string InterfaceType_Must_Be_Interface => GetResourceString("InterfaceType_Must_Be_Interface");

	internal static string BaseType_Cannot_Be_Abstract => GetResourceString("BaseType_Cannot_Be_Abstract");

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
