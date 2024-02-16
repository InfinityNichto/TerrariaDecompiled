using System.Resources;
using FxResources.System.Collections.NonGeneric;

namespace System;

internal static class SR
{
	private static readonly bool s_usingResourceKeys = AppContext.TryGetSwitch("System.Resources.UseSystemResourceKeys", out var isEnabled) && isEnabled;

	private static ResourceManager s_resourceManager;

	internal static ResourceManager ResourceManager => s_resourceManager ?? (s_resourceManager = new ResourceManager(typeof(SR)));

	internal static string Argument_AddingDuplicate_OldAndNewKeys => GetResourceString("Argument_AddingDuplicate_OldAndNewKeys");

	internal static string Arg_ArrayPlusOffTooSmall => GetResourceString("Arg_ArrayPlusOffTooSmall");

	internal static string Arg_RankMultiDimNotSupported => GetResourceString("Arg_RankMultiDimNotSupported");

	internal static string Arg_RemoveArgNotFound => GetResourceString("Arg_RemoveArgNotFound");

	internal static string Argument_InvalidOffLen => GetResourceString("Argument_InvalidOffLen");

	internal static string ArgumentNull_Array => GetResourceString("ArgumentNull_Array");

	internal static string ArgumentNull_Dictionary => GetResourceString("ArgumentNull_Dictionary");

	internal static string ArgumentNull_Key => GetResourceString("ArgumentNull_Key");

	internal static string ArgumentOutOfRange_Index => GetResourceString("ArgumentOutOfRange_Index");

	internal static string ArgumentOutOfRange_NeedNonNegNum => GetResourceString("ArgumentOutOfRange_NeedNonNegNum");

	internal static string ArgumentOutOfRange_SmallCapacity => GetResourceString("ArgumentOutOfRange_SmallCapacity");

	internal static string ArgumentOutOfRange_QueueGrowFactor => GetResourceString("ArgumentOutOfRange_QueueGrowFactor");

	internal static string InvalidOperation_EmptyQueue => GetResourceString("InvalidOperation_EmptyQueue");

	internal static string InvalidOperation_EmptyStack => GetResourceString("InvalidOperation_EmptyStack");

	internal static string InvalidOperation_EnumEnded => GetResourceString("InvalidOperation_EnumEnded");

	internal static string InvalidOperation_EnumFailedVersion => GetResourceString("InvalidOperation_EnumFailedVersion");

	internal static string InvalidOperation_EnumNotStarted => GetResourceString("InvalidOperation_EnumNotStarted");

	internal static string InvalidOperation_EnumOpCantHappen => GetResourceString("InvalidOperation_EnumOpCantHappen");

	internal static string NotSupported_KeyCollectionSet => GetResourceString("NotSupported_KeyCollectionSet");

	internal static string NotSupported_SortedListNestedWrite => GetResourceString("NotSupported_SortedListNestedWrite");

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

	internal static string Format(string resourceFormat, object p1, object p2)
	{
		if (UsingResourceKeys())
		{
			return string.Join(", ", resourceFormat, p1, p2);
		}
		return string.Format(resourceFormat, p1, p2);
	}
}
