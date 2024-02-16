using System.Resources;
using FxResources.System.Net.WebSockets;

namespace System;

internal static class SR
{
	private static readonly bool s_usingResourceKeys = AppContext.TryGetSwitch("System.Resources.UseSystemResourceKeys", out var isEnabled) && isEnabled;

	private static ResourceManager s_resourceManager;

	internal static ResourceManager ResourceManager => s_resourceManager ?? (s_resourceManager = new ResourceManager(typeof(SR)));

	internal static string net_WebSockets_InvalidState => GetResourceString("net_WebSockets_InvalidState");

	internal static string net_WebSockets_Generic => GetResourceString("net_WebSockets_Generic");

	internal static string net_WebSockets_InvalidMessageType_Generic => GetResourceString("net_WebSockets_InvalidMessageType_Generic");

	internal static string net_Websockets_WebSocketBaseFaulted => GetResourceString("net_Websockets_WebSocketBaseFaulted");

	internal static string net_WebSockets_NotAWebSocket_Generic => GetResourceString("net_WebSockets_NotAWebSocket_Generic");

	internal static string net_WebSockets_UnsupportedWebSocketVersion_Generic => GetResourceString("net_WebSockets_UnsupportedWebSocketVersion_Generic");

	internal static string net_WebSockets_UnsupportedProtocol_Generic => GetResourceString("net_WebSockets_UnsupportedProtocol_Generic");

	internal static string net_WebSockets_HeaderError_Generic => GetResourceString("net_WebSockets_HeaderError_Generic");

	internal static string net_WebSockets_ConnectionClosedPrematurely_Generic => GetResourceString("net_WebSockets_ConnectionClosedPrematurely_Generic");

	internal static string net_WebSockets_InvalidState_Generic => GetResourceString("net_WebSockets_InvalidState_Generic");

	internal static string net_WebSockets_ArgumentOutOfRange_TooSmall => GetResourceString("net_WebSockets_ArgumentOutOfRange_TooSmall");

	internal static string net_WebSockets_InvalidEmptySubProtocol => GetResourceString("net_WebSockets_InvalidEmptySubProtocol");

	internal static string net_WebSockets_InvalidCharInProtocolString => GetResourceString("net_WebSockets_InvalidCharInProtocolString");

	internal static string net_WebSockets_ReasonNotNull => GetResourceString("net_WebSockets_ReasonNotNull");

	internal static string net_WebSockets_InvalidCloseStatusCode => GetResourceString("net_WebSockets_InvalidCloseStatusCode");

	internal static string net_WebSockets_InvalidCloseStatusDescription => GetResourceString("net_WebSockets_InvalidCloseStatusDescription");

	internal static string net_WebSockets_Argument_InvalidMessageType => GetResourceString("net_WebSockets_Argument_InvalidMessageType");

	internal static string net_Websockets_ReservedBitsSet => GetResourceString("net_Websockets_ReservedBitsSet");

	internal static string net_Websockets_ClientReceivedMaskedFrame => GetResourceString("net_Websockets_ClientReceivedMaskedFrame");

	internal static string net_Websockets_ContinuationFromFinalFrame => GetResourceString("net_Websockets_ContinuationFromFinalFrame");

	internal static string net_Websockets_NonContinuationAfterNonFinalFrame => GetResourceString("net_Websockets_NonContinuationAfterNonFinalFrame");

	internal static string net_Websockets_InvalidControlMessage => GetResourceString("net_Websockets_InvalidControlMessage");

	internal static string net_Websockets_UnknownOpcode => GetResourceString("net_Websockets_UnknownOpcode");

	internal static string NotReadableStream => GetResourceString("NotReadableStream");

	internal static string NotWriteableStream => GetResourceString("NotWriteableStream");

	internal static string net_WebSockets_ArgumentOutOfRange => GetResourceString("net_WebSockets_ArgumentOutOfRange");

	internal static string net_Websockets_PerMessageCompressedFlagInContinuation => GetResourceString("net_Websockets_PerMessageCompressedFlagInContinuation");

	internal static string net_Websockets_PerMessageCompressedFlagWhenNotEnabled => GetResourceString("net_Websockets_PerMessageCompressedFlagWhenNotEnabled");

	internal static string ZLibErrorDLLLoadError => GetResourceString("ZLibErrorDLLLoadError");

	internal static string ZLibErrorInconsistentStream => GetResourceString("ZLibErrorInconsistentStream");

	internal static string ZLibErrorNotEnoughMemory => GetResourceString("ZLibErrorNotEnoughMemory");

	internal static string ZLibErrorUnexpected => GetResourceString("ZLibErrorUnexpected");

	internal static string ZLibUnsupportedCompression => GetResourceString("ZLibUnsupportedCompression");

	internal static string net_WebSockets_Argument_MessageFlagsHasDifferentCompressionOptions => GetResourceString("net_WebSockets_Argument_MessageFlagsHasDifferentCompressionOptions");

	internal static string net_Websockets_InvalidPayloadLength => GetResourceString("net_Websockets_InvalidPayloadLength");

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

	internal static string Format(string resourceFormat, params object[] args)
	{
		if (args != null)
		{
			if (UsingResourceKeys())
			{
				return resourceFormat + ", " + string.Join(", ", args);
			}
			return string.Format(resourceFormat, args);
		}
		return resourceFormat;
	}
}
