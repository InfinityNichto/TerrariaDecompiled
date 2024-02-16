using System.Resources;
using FxResources.System.Threading.Tasks.Parallel;

namespace System;

internal static class SR
{
	private static readonly bool s_usingResourceKeys = AppContext.TryGetSwitch("System.Resources.UseSystemResourceKeys", out var isEnabled) && isEnabled;

	private static ResourceManager s_resourceManager;

	internal static ResourceManager ResourceManager => s_resourceManager ?? (s_resourceManager = new ResourceManager(typeof(SR)));

	internal static string Parallel_Invoke_ActionNull => GetResourceString("Parallel_Invoke_ActionNull");

	internal static string Parallel_ForEach_OrderedPartitionerKeysNotNormalized => GetResourceString("Parallel_ForEach_OrderedPartitionerKeysNotNormalized");

	internal static string Parallel_ForEach_PartitionerNotDynamic => GetResourceString("Parallel_ForEach_PartitionerNotDynamic");

	internal static string Parallel_ForEach_PartitionerReturnedNull => GetResourceString("Parallel_ForEach_PartitionerReturnedNull");

	internal static string Parallel_ForEach_NullEnumerator => GetResourceString("Parallel_ForEach_NullEnumerator");

	internal static string ParallelState_Break_InvalidOperationException_BreakAfterStop => GetResourceString("ParallelState_Break_InvalidOperationException_BreakAfterStop");

	internal static string ParallelState_Stop_InvalidOperationException_StopAfterBreak => GetResourceString("ParallelState_Stop_InvalidOperationException_StopAfterBreak");

	internal static string ParallelState_NotSupportedException_UnsupportedMethod => GetResourceString("ParallelState_NotSupportedException_UnsupportedMethod");

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
