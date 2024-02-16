using System.Resources;
using FxResources.System.Runtime.Numerics;

namespace System;

internal static class SR
{
	private static readonly bool s_usingResourceKeys = AppContext.TryGetSwitch("System.Resources.UseSystemResourceKeys", out var isEnabled) && isEnabled;

	private static ResourceManager s_resourceManager;

	internal static ResourceManager ResourceManager => s_resourceManager ?? (s_resourceManager = new ResourceManager(typeof(SR)));

	internal static string Argument_BadFormatSpecifier => GetResourceString("Argument_BadFormatSpecifier");

	internal static string Argument_InvalidNumberStyles => GetResourceString("Argument_InvalidNumberStyles");

	internal static string Argument_InvalidHexStyle => GetResourceString("Argument_InvalidHexStyle");

	internal static string Argument_MustBeBigInt => GetResourceString("Argument_MustBeBigInt");

	internal static string Format_TooLarge => GetResourceString("Format_TooLarge");

	internal static string ArgumentOutOfRange_MustBeNonNeg => GetResourceString("ArgumentOutOfRange_MustBeNonNeg");

	internal static string Overflow_BigIntInfinity => GetResourceString("Overflow_BigIntInfinity");

	internal static string Overflow_NotANumber => GetResourceString("Overflow_NotANumber");

	internal static string Overflow_ParseBigInteger => GetResourceString("Overflow_ParseBigInteger");

	internal static string Overflow_Int32 => GetResourceString("Overflow_Int32");

	internal static string Overflow_Int64 => GetResourceString("Overflow_Int64");

	internal static string Overflow_UInt32 => GetResourceString("Overflow_UInt32");

	internal static string Overflow_UInt64 => GetResourceString("Overflow_UInt64");

	internal static string Overflow_Decimal => GetResourceString("Overflow_Decimal");

	internal static string Overflow_Negative_Unsigned => GetResourceString("Overflow_Negative_Unsigned");

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
