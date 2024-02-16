using System.Resources;
using FxResources.System.Threading.Tasks.Dataflow;

namespace System;

internal static class SR
{
	private static readonly bool s_usingResourceKeys = AppContext.TryGetSwitch("System.Resources.UseSystemResourceKeys", out var isEnabled) && isEnabled;

	private static ResourceManager s_resourceManager;

	internal static ResourceManager ResourceManager => s_resourceManager ?? (s_resourceManager = new ResourceManager(typeof(SR)));

	internal static string ArgumentOutOfRange_BatchSizeMustBeNoGreaterThanBoundedCapacity => GetResourceString("ArgumentOutOfRange_BatchSizeMustBeNoGreaterThanBoundedCapacity");

	internal static string ArgumentOutOfRange_GenericPositive => GetResourceString("ArgumentOutOfRange_GenericPositive");

	internal static string ArgumentOutOfRange_NeedNonNegOrNegative1 => GetResourceString("ArgumentOutOfRange_NeedNonNegOrNegative1");

	internal static string Argument_BoundedCapacityNotSupported => GetResourceString("Argument_BoundedCapacityNotSupported");

	internal static string Argument_CantConsumeFromANullSource => GetResourceString("Argument_CantConsumeFromANullSource");

	internal static string Argument_InvalidMessageHeader => GetResourceString("Argument_InvalidMessageHeader");

	internal static string Argument_InvalidMessageId => GetResourceString("Argument_InvalidMessageId");

	internal static string Argument_NonGreedyNotSupported => GetResourceString("Argument_NonGreedyNotSupported");

	internal static string InvalidOperation_DataNotAvailableForReceive => GetResourceString("InvalidOperation_DataNotAvailableForReceive");

	internal static string InvalidOperation_FailedToConsumeReservedMessage => GetResourceString("InvalidOperation_FailedToConsumeReservedMessage");

	internal static string InvalidOperation_MessageNotReservedByTarget => GetResourceString("InvalidOperation_MessageNotReservedByTarget");

	internal static string NotSupported_MemberNotNeeded => GetResourceString("NotSupported_MemberNotNeeded");

	internal static string InvalidOperation_ErrorDuringCleanup => GetResourceString("InvalidOperation_ErrorDuringCleanup");

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
