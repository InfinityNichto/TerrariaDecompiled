using System.Resources;
using FxResources.System.Diagnostics.TraceSource;

namespace System;

internal static class SR
{
	private static readonly bool s_usingResourceKeys = AppContext.TryGetSwitch("System.Resources.UseSystemResourceKeys", out var isEnabled) && isEnabled;

	private static ResourceManager s_resourceManager;

	internal static ResourceManager ResourceManager => s_resourceManager ?? (s_resourceManager = new ResourceManager(typeof(SR)));

	internal static string ExceptionOccurred => GetResourceString("ExceptionOccurred");

	internal static string MustAddListener => GetResourceString("MustAddListener");

	internal static string TraceListenerFail => GetResourceString("TraceListenerFail");

	internal static string TraceListenerIndentSize => GetResourceString("TraceListenerIndentSize");

	internal static string DebugAssertBanner => GetResourceString("DebugAssertBanner");

	internal static string DebugAssertShortMessage => GetResourceString("DebugAssertShortMessage");

	internal static string DebugAssertLongMessage => GetResourceString("DebugAssertLongMessage");

	internal static string TraceSwitchLevelTooLow => GetResourceString("TraceSwitchLevelTooLow");

	internal static string TraceSwitchInvalidLevel => GetResourceString("TraceSwitchInvalidLevel");

	internal static string TraceSwitchLevelTooHigh => GetResourceString("TraceSwitchLevelTooHigh");

	internal static string InvalidNullEmptyArgument => GetResourceString("InvalidNullEmptyArgument");

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
