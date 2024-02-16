using System.Resources;
using FxResources.System.ObjectModel;

namespace System;

internal static class SR
{
	private static readonly bool s_usingResourceKeys = AppContext.TryGetSwitch("System.Resources.UseSystemResourceKeys", out var isEnabled) && isEnabled;

	private static ResourceManager s_resourceManager;

	internal static ResourceManager ResourceManager => s_resourceManager ?? (s_resourceManager = new ResourceManager(typeof(SR)));

	internal static string ArgumentOutOfRange_InvalidThreshold => GetResourceString("ArgumentOutOfRange_InvalidThreshold");

	internal static string Argument_ItemNotExist => GetResourceString("Argument_ItemNotExist");

	internal static string Argument_AddingDuplicate => GetResourceString("Argument_AddingDuplicate");

	internal static string Arg_NonZeroLowerBound => GetResourceString("Arg_NonZeroLowerBound");

	internal static string Arg_ArrayPlusOffTooSmall => GetResourceString("Arg_ArrayPlusOffTooSmall");

	internal static string ArgumentOutOfRange_NeedNonNegNum => GetResourceString("ArgumentOutOfRange_NeedNonNegNum");

	internal static string IndexCannotBeNegative => GetResourceString("IndexCannotBeNegative");

	internal static string NotSupported_ReadOnlyCollection => GetResourceString("NotSupported_ReadOnlyCollection");

	internal static string ObservableCollectionReentrancyNotAllowed => GetResourceString("ObservableCollectionReentrancyNotAllowed");

	internal static string WrongActionForCtor => GetResourceString("WrongActionForCtor");

	internal static string MustBeResetAddOrRemoveActionForCtor => GetResourceString("MustBeResetAddOrRemoveActionForCtor");

	internal static string ResetActionRequiresNullItem => GetResourceString("ResetActionRequiresNullItem");

	internal static string ResetActionRequiresIndexMinus1 => GetResourceString("ResetActionRequiresIndexMinus1");

	internal static string Arg_RankMultiDimNotSupported => GetResourceString("Arg_RankMultiDimNotSupported");

	internal static string Argument_InvalidArrayType => GetResourceString("Argument_InvalidArrayType");

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
}
