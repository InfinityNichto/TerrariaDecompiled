using System.Resources;
using FxResources.System.ComponentModel.Primitives;

namespace System;

internal static class SR
{
	private static readonly bool s_usingResourceKeys = AppContext.TryGetSwitch("System.Resources.UseSystemResourceKeys", out var isEnabled) && isEnabled;

	private static ResourceManager s_resourceManager;

	internal static ResourceManager ResourceManager => s_resourceManager ?? (s_resourceManager = new ResourceManager(typeof(SR)));

	internal static string InvalidEnumArgument => GetResourceString("InvalidEnumArgument", "The value of argument '{0}' ({1}) is invalid for Enum type '{2}'.");

	internal static string PropertyCategoryAction => GetResourceString("PropertyCategoryAction", "Action");

	internal static string PropertyCategoryAppearance => GetResourceString("PropertyCategoryAppearance", "Appearance");

	internal static string PropertyCategoryAsynchronous => GetResourceString("PropertyCategoryAsynchronous", "Asynchronous");

	internal static string PropertyCategoryBehavior => GetResourceString("PropertyCategoryBehavior", "Behavior");

	internal static string PropertyCategoryConfig => GetResourceString("PropertyCategoryConfig", "Configurations");

	internal static string PropertyCategoryData => GetResourceString("PropertyCategoryData", "Data");

	internal static string PropertyCategoryDDE => GetResourceString("PropertyCategoryDDE", "DDE");

	internal static string PropertyCategoryDefault => GetResourceString("PropertyCategoryDefault", "Misc");

	internal static string PropertyCategoryDesign => GetResourceString("PropertyCategoryDesign", "Design");

	internal static string PropertyCategoryDragDrop => GetResourceString("PropertyCategoryDragDrop", "Drag Drop");

	internal static string PropertyCategoryFocus => GetResourceString("PropertyCategoryFocus", "Focus");

	internal static string PropertyCategoryFont => GetResourceString("PropertyCategoryFont", "Font");

	internal static string PropertyCategoryFormat => GetResourceString("PropertyCategoryFormat", "Format");

	internal static string PropertyCategoryKey => GetResourceString("PropertyCategoryKey", "Key");

	internal static string PropertyCategoryLayout => GetResourceString("PropertyCategoryLayout", "Layout");

	internal static string PropertyCategoryList => GetResourceString("PropertyCategoryList", "List");

	internal static string PropertyCategoryMouse => GetResourceString("PropertyCategoryMouse", "Mouse");

	internal static string PropertyCategoryPosition => GetResourceString("PropertyCategoryPosition", "Position");

	internal static string PropertyCategoryScale => GetResourceString("PropertyCategoryScale", "Scale");

	internal static string PropertyCategoryText => GetResourceString("PropertyCategoryText", "Text");

	internal static string PropertyCategoryWindowStyle => GetResourceString("PropertyCategoryWindowStyle", "Window Style");

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

	internal static string GetResourceString(string resourceKey, string defaultString)
	{
		string resourceString = GetResourceString(resourceKey);
		if (!(resourceKey == resourceString) && resourceString != null)
		{
			return resourceString;
		}
		return defaultString;
	}

	internal static string Format(string resourceFormat, object p1, object p2, object p3)
	{
		if (UsingResourceKeys())
		{
			return string.Join(", ", resourceFormat, p1, p2, p3);
		}
		return string.Format(resourceFormat, p1, p2, p3);
	}
}
