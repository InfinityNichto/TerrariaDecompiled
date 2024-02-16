using System.Resources;
using FxResources.System.Collections.Specialized;

namespace System;

internal static class SR
{
	private static readonly bool s_usingResourceKeys = AppContext.TryGetSwitch("System.Resources.UseSystemResourceKeys", out var isEnabled) && isEnabled;

	private static ResourceManager s_resourceManager;

	internal static ResourceManager ResourceManager => s_resourceManager ?? (s_resourceManager = new ResourceManager(typeof(SR)));

	internal static string Argument_AddingDuplicate => GetResourceString("Argument_AddingDuplicate");

	internal static string Argument_InvalidValue_TooSmall => GetResourceString("Argument_InvalidValue_TooSmall");

	internal static string ArgumentOutOfRange_NeedNonNegNum_Index => GetResourceString("ArgumentOutOfRange_NeedNonNegNum_Index");

	internal static string InvalidOperation_EnumFailedVersion => GetResourceString("InvalidOperation_EnumFailedVersion");

	internal static string InvalidOperation_EnumOpCantHappen => GetResourceString("InvalidOperation_EnumOpCantHappen");

	internal static string Arg_MultiRank => GetResourceString("Arg_MultiRank");

	internal static string Arg_InsufficientSpace => GetResourceString("Arg_InsufficientSpace");

	internal static string CollectionReadOnly => GetResourceString("CollectionReadOnly");

	internal static string BitVectorFull => GetResourceString("BitVectorFull");

	internal static string OrderedDictionary_ReadOnly => GetResourceString("OrderedDictionary_ReadOnly");

	internal static string Argument_ImplementIComparable => GetResourceString("Argument_ImplementIComparable");

	internal static string OrderedDictionary_SerializationMismatch => GetResourceString("OrderedDictionary_SerializationMismatch");

	internal static string Serialization_InvalidOnDeser => GetResourceString("Serialization_InvalidOnDeser");

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
