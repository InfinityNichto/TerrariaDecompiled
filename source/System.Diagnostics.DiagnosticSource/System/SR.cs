using System.Resources;
using FxResources.System.Diagnostics.DiagnosticSource;

namespace System;

internal static class SR
{
	private static readonly bool s_usingResourceKeys = AppContext.TryGetSwitch("System.Resources.UseSystemResourceKeys", out var isEnabled) && isEnabled;

	private static ResourceManager s_resourceManager;

	internal static ResourceManager ResourceManager => s_resourceManager ?? (s_resourceManager = new ResourceManager(typeof(SR)));

	internal static string ActivityIdFormatInvalid => GetResourceString("ActivityIdFormatInvalid");

	internal static string ActivityNotRunning => GetResourceString("ActivityNotRunning");

	internal static string ActivityNotStarted => GetResourceString("ActivityNotStarted");

	internal static string ActivityStartAlreadyStarted => GetResourceString("ActivityStartAlreadyStarted");

	internal static string EndTimeNotUtc => GetResourceString("EndTimeNotUtc");

	internal static string OperationNameInvalid => GetResourceString("OperationNameInvalid");

	internal static string ParentIdAlreadySet => GetResourceString("ParentIdAlreadySet");

	internal static string ParentIdInvalid => GetResourceString("ParentIdInvalid");

	internal static string SetFormatOnStartedActivity => GetResourceString("SetFormatOnStartedActivity");

	internal static string SetParentIdOnActivityWithParent => GetResourceString("SetParentIdOnActivityWithParent");

	internal static string StartTimeNotUtc => GetResourceString("StartTimeNotUtc");

	internal static string KeyAlreadyExist => GetResourceString("KeyAlreadyExist");

	internal static string InvalidTraceParent => GetResourceString("InvalidTraceParent");

	internal static string UnsupportedType => GetResourceString("UnsupportedType");

	internal static string Arg_BufferTooSmall => GetResourceString("Arg_BufferTooSmall");

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
