using System.Resources;
using FxResources.System.ComponentModel.EventBasedAsync;

namespace System;

internal static class SR
{
	private static readonly bool s_usingResourceKeys = AppContext.TryGetSwitch("System.Resources.UseSystemResourceKeys", out var isEnabled) && isEnabled;

	private static ResourceManager s_resourceManager;

	internal static ResourceManager ResourceManager => s_resourceManager ?? (s_resourceManager = new ResourceManager(typeof(SR)));

	internal static string Async_NullDelegate => GetResourceString("Async_NullDelegate");

	internal static string Async_OperationAlreadyCompleted => GetResourceString("Async_OperationAlreadyCompleted");

	internal static string Async_OperationCancelled => GetResourceString("Async_OperationCancelled");

	internal static string Async_ExceptionOccurred => GetResourceString("Async_ExceptionOccurred");

	internal static string BackgroundWorker_WorkerAlreadyRunning => GetResourceString("BackgroundWorker_WorkerAlreadyRunning");

	internal static string BackgroundWorker_WorkerDoesntReportProgress => GetResourceString("BackgroundWorker_WorkerDoesntReportProgress");

	internal static string BackgroundWorker_WorkerDoesntSupportCancellation => GetResourceString("BackgroundWorker_WorkerDoesntSupportCancellation");

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
