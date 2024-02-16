using System.Resources;
using FxResources.System.Resources.Writer;

namespace System;

internal static class SR
{
	private static readonly bool s_usingResourceKeys = AppContext.TryGetSwitch("System.Resources.UseSystemResourceKeys", out var isEnabled) && isEnabled;

	private static ResourceManager s_resourceManager;

	internal static ResourceManager ResourceManager => s_resourceManager ?? (s_resourceManager = new ResourceManager(typeof(SR)));

	internal static string InvalidOperation_ResourceWriterSaved => GetResourceString("InvalidOperation_ResourceWriterSaved");

	internal static string Argument_StreamNotWritable => GetResourceString("Argument_StreamNotWritable");

	internal static string NotSupported_UnseekableStream => GetResourceString("NotSupported_UnseekableStream");

	internal static string NotSupported_BinarySerializedResources => GetResourceString("NotSupported_BinarySerializedResources");

	internal static string ArgumentOutOfRange_StreamLength => GetResourceString("ArgumentOutOfRange_StreamLength");

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
