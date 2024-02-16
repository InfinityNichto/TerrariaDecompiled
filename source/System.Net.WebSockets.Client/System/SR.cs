using System.Resources;
using FxResources.System.Net.WebSockets.Client;

namespace System;

internal static class SR
{
	private static readonly bool s_usingResourceKeys = AppContext.TryGetSwitch("System.Resources.UseSystemResourceKeys", out var isEnabled) && isEnabled;

	private static ResourceManager s_resourceManager;

	internal static ResourceManager ResourceManager => s_resourceManager ?? (s_resourceManager = new ResourceManager(typeof(SR)));

	internal static string net_webstatus_ConnectFailure => GetResourceString("net_webstatus_ConnectFailure");

	internal static string net_uri_NotAbsolute => GetResourceString("net_uri_NotAbsolute");

	internal static string net_WebSockets_AcceptUnsupportedProtocol => GetResourceString("net_WebSockets_AcceptUnsupportedProtocol");

	internal static string net_WebSockets_ArgumentOutOfRange_TooSmall => GetResourceString("net_WebSockets_ArgumentOutOfRange_TooSmall");

	internal static string net_WebSockets_Connect101Expected => GetResourceString("net_WebSockets_Connect101Expected");

	internal static string net_WebSockets_MissingResponseHeader => GetResourceString("net_WebSockets_MissingResponseHeader");

	internal static string net_WebSockets_InvalidCharInProtocolString => GetResourceString("net_WebSockets_InvalidCharInProtocolString");

	internal static string net_WebSockets_InvalidEmptySubProtocol => GetResourceString("net_WebSockets_InvalidEmptySubProtocol");

	internal static string net_WebSockets_Scheme => GetResourceString("net_WebSockets_Scheme");

	internal static string net_WebSockets_AlreadyStarted => GetResourceString("net_WebSockets_AlreadyStarted");

	internal static string net_WebSockets_InvalidResponseHeader => GetResourceString("net_WebSockets_InvalidResponseHeader");

	internal static string net_WebSockets_NotConnected => GetResourceString("net_WebSockets_NotConnected");

	internal static string net_WebSockets_NoDuplicateProtocol => GetResourceString("net_WebSockets_NoDuplicateProtocol");

	internal static string net_WebSockets_ServerWindowBitsNegotiationFailure => GetResourceString("net_WebSockets_ServerWindowBitsNegotiationFailure");

	internal static string net_WebSockets_ClientWindowBitsNegotiationFailure => GetResourceString("net_WebSockets_ClientWindowBitsNegotiationFailure");

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
