using System.Resources;
using FxResources.System.Collections;

namespace System;

internal static class SR
{
	private static readonly bool s_usingResourceKeys = AppContext.TryGetSwitch("System.Resources.UseSystemResourceKeys", out var isEnabled) && isEnabled;

	private static ResourceManager s_resourceManager;

	internal static ResourceManager ResourceManager => s_resourceManager ?? (s_resourceManager = new ResourceManager(typeof(SR)));

	internal static string Arg_NonZeroLowerBound => GetResourceString("Arg_NonZeroLowerBound");

	internal static string Arg_WrongType => GetResourceString("Arg_WrongType");

	internal static string Arg_ArrayPlusOffTooSmall => GetResourceString("Arg_ArrayPlusOffTooSmall");

	internal static string ArgumentOutOfRange_NeedNonNegNum => GetResourceString("ArgumentOutOfRange_NeedNonNegNum");

	internal static string ArgumentOutOfRange_SmallCapacity => GetResourceString("ArgumentOutOfRange_SmallCapacity");

	internal static string Argument_InvalidOffLen => GetResourceString("Argument_InvalidOffLen");

	internal static string Argument_AddingDuplicate => GetResourceString("Argument_AddingDuplicate");

	internal static string InvalidOperation_EmptyQueue => GetResourceString("InvalidOperation_EmptyQueue");

	internal static string InvalidOperation_EnumOpCantHappen => GetResourceString("InvalidOperation_EnumOpCantHappen");

	internal static string InvalidOperation_EnumFailedVersion => GetResourceString("InvalidOperation_EnumFailedVersion");

	internal static string InvalidOperation_EmptyStack => GetResourceString("InvalidOperation_EmptyStack");

	internal static string InvalidOperation_EnumNotStarted => GetResourceString("InvalidOperation_EnumNotStarted");

	internal static string InvalidOperation_EnumEnded => GetResourceString("InvalidOperation_EnumEnded");

	internal static string NotSupported_KeyCollectionSet => GetResourceString("NotSupported_KeyCollectionSet");

	internal static string NotSupported_ValueCollectionSet => GetResourceString("NotSupported_ValueCollectionSet");

	internal static string Arg_ArrayLengthsDiffer => GetResourceString("Arg_ArrayLengthsDiffer");

	internal static string Arg_BitArrayTypeUnsupported => GetResourceString("Arg_BitArrayTypeUnsupported");

	internal static string Arg_InsufficientSpace => GetResourceString("Arg_InsufficientSpace");

	internal static string Arg_RankMultiDimNotSupported => GetResourceString("Arg_RankMultiDimNotSupported");

	internal static string Argument_ArrayTooLarge => GetResourceString("Argument_ArrayTooLarge");

	internal static string Argument_InvalidArrayType => GetResourceString("Argument_InvalidArrayType");

	internal static string ArgumentOutOfRange_BiggerThanCollection => GetResourceString("ArgumentOutOfRange_BiggerThanCollection");

	internal static string ArgumentOutOfRange_Index => GetResourceString("ArgumentOutOfRange_Index");

	internal static string ExternalLinkedListNode => GetResourceString("ExternalLinkedListNode");

	internal static string LinkedListEmpty => GetResourceString("LinkedListEmpty");

	internal static string LinkedListNodeIsAttached => GetResourceString("LinkedListNodeIsAttached");

	internal static string NotSupported_SortedListNestedWrite => GetResourceString("NotSupported_SortedListNestedWrite");

	internal static string SortedSet_LowerValueGreaterThanUpperValue => GetResourceString("SortedSet_LowerValueGreaterThanUpperValue");

	internal static string Serialization_InvalidOnDeser => GetResourceString("Serialization_InvalidOnDeser");

	internal static string Serialization_MismatchedCount => GetResourceString("Serialization_MismatchedCount");

	internal static string Serialization_MissingValues => GetResourceString("Serialization_MissingValues");

	internal static string Arg_KeyNotFoundWithKey => GetResourceString("Arg_KeyNotFoundWithKey");

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
