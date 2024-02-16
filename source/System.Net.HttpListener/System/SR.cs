using System.Resources;
using FxResources.System.Net.HttpListener;

namespace System;

internal static class SR
{
	private static readonly bool s_usingResourceKeys = AppContext.TryGetSwitch("System.Resources.UseSystemResourceKeys", out var isEnabled) && isEnabled;

	private static ResourceManager s_resourceManager;

	internal static ResourceManager ResourceManager => s_resourceManager ?? (s_resourceManager = new ResourceManager(typeof(SR)));

	internal static string net_log_listener_delegate_exception => GetResourceString("net_log_listener_delegate_exception");

	internal static string net_log_listener_unsupported_authentication_scheme => GetResourceString("net_log_listener_unsupported_authentication_scheme");

	internal static string net_log_listener_unmatched_authentication_scheme => GetResourceString("net_log_listener_unmatched_authentication_scheme");

	internal static string net_io_invalidasyncresult => GetResourceString("net_io_invalidasyncresult");

	internal static string net_io_invalidendcall => GetResourceString("net_io_invalidendcall");

	internal static string net_listener_cannot_set_custom_cbt => GetResourceString("net_listener_cannot_set_custom_cbt");

	internal static string net_listener_detach_error => GetResourceString("net_listener_detach_error");

	internal static string net_listener_scheme => GetResourceString("net_listener_scheme");

	internal static string net_listener_host => GetResourceString("net_listener_host");

	internal static string net_listener_mustcall => GetResourceString("net_listener_mustcall");

	internal static string net_listener_slash => GetResourceString("net_listener_slash");

	internal static string net_listener_already => GetResourceString("net_listener_already");

	internal static string net_log_listener_no_cbt_disabled => GetResourceString("net_log_listener_no_cbt_disabled");

	internal static string net_log_listener_no_cbt_http => GetResourceString("net_log_listener_no_cbt_http");

	internal static string net_log_listener_no_cbt_trustedproxy => GetResourceString("net_log_listener_no_cbt_trustedproxy");

	internal static string net_log_listener_cbt => GetResourceString("net_log_listener_cbt");

	internal static string net_log_listener_no_spn_kerberos => GetResourceString("net_log_listener_no_spn_kerberos");

	internal static string net_log_listener_no_spn_disabled => GetResourceString("net_log_listener_no_spn_disabled");

	internal static string net_log_listener_no_spn_cbt => GetResourceString("net_log_listener_no_spn_cbt");

	internal static string net_log_listener_no_spn_whensupported => GetResourceString("net_log_listener_no_spn_whensupported");

	internal static string net_log_listener_no_spn_loopback => GetResourceString("net_log_listener_no_spn_loopback");

	internal static string net_log_listener_spn => GetResourceString("net_log_listener_spn");

	internal static string net_log_listener_spn_passed => GetResourceString("net_log_listener_spn_passed");

	internal static string net_log_listener_spn_failed => GetResourceString("net_log_listener_spn_failed");

	internal static string net_log_listener_spn_failed_always => GetResourceString("net_log_listener_spn_failed_always");

	internal static string net_log_listener_spn_failed_empty => GetResourceString("net_log_listener_spn_failed_empty");

	internal static string net_log_listener_spn_failed_dump => GetResourceString("net_log_listener_spn_failed_dump");

	internal static string net_log_listener_spn_add => GetResourceString("net_log_listener_spn_add");

	internal static string net_log_listener_spn_not_add => GetResourceString("net_log_listener_spn_not_add");

	internal static string net_log_listener_spn_remove => GetResourceString("net_log_listener_spn_remove");

	internal static string net_log_listener_spn_not_remove => GetResourceString("net_log_listener_spn_not_remove");

	internal static string net_listener_no_spns => GetResourceString("net_listener_no_spns");

	internal static string net_ssp_dont_support_cbt => GetResourceString("net_ssp_dont_support_cbt");

	internal static string net_PropertyNotImplementedException => GetResourceString("net_PropertyNotImplementedException");

	internal static string net_array_too_small => GetResourceString("net_array_too_small");

	internal static string net_listener_mustcompletecall => GetResourceString("net_listener_mustcompletecall");

	internal static string net_listener_invalid_cbt_type => GetResourceString("net_listener_invalid_cbt_type");

	internal static string net_listener_callinprogress => GetResourceString("net_listener_callinprogress");

	internal static string net_log_listener_cant_create_uri => GetResourceString("net_log_listener_cant_create_uri");

	internal static string net_log_listener_cant_convert_raw_path => GetResourceString("net_log_listener_cant_convert_raw_path");

	internal static string net_log_listener_cant_convert_percent_value => GetResourceString("net_log_listener_cant_convert_percent_value");

	internal static string net_log_listener_cant_convert_to_utf8 => GetResourceString("net_log_listener_cant_convert_to_utf8");

	internal static string net_log_listener_cant_convert_bytes => GetResourceString("net_log_listener_cant_convert_bytes");

	internal static string net_invalidstatus => GetResourceString("net_invalidstatus");

	internal static string net_WebHeaderInvalidControlChars => GetResourceString("net_WebHeaderInvalidControlChars");

	internal static string net_rspsubmitted => GetResourceString("net_rspsubmitted");

	internal static string net_nochunkuploadonhttp10 => GetResourceString("net_nochunkuploadonhttp10");

	internal static string net_cookie_exists => GetResourceString("net_cookie_exists");

	internal static string net_clsmall => GetResourceString("net_clsmall");

	internal static string net_wrongversion => GetResourceString("net_wrongversion");

	internal static string net_noseek => GetResourceString("net_noseek");

	internal static string net_writeonlystream => GetResourceString("net_writeonlystream");

	internal static string net_entitytoobig => GetResourceString("net_entitytoobig");

	internal static string net_io_notenoughbyteswritten => GetResourceString("net_io_notenoughbyteswritten");

	internal static string net_listener_close_urlgroup_error => GetResourceString("net_listener_close_urlgroup_error");

	internal static string net_WebSockets_NativeSendResponseHeaders => GetResourceString("net_WebSockets_NativeSendResponseHeaders");

	internal static string net_WebSockets_ClientAcceptingNoProtocols => GetResourceString("net_WebSockets_ClientAcceptingNoProtocols");

	internal static string net_WebSockets_AcceptUnsupportedProtocol => GetResourceString("net_WebSockets_AcceptUnsupportedProtocol");

	internal static string net_WebSockets_AcceptNotAWebSocket => GetResourceString("net_WebSockets_AcceptNotAWebSocket");

	internal static string net_WebSockets_AcceptHeaderNotFound => GetResourceString("net_WebSockets_AcceptHeaderNotFound");

	internal static string net_WebSockets_AcceptUnsupportedWebSocketVersion => GetResourceString("net_WebSockets_AcceptUnsupportedWebSocketVersion");

	internal static string net_WebSockets_InvalidEmptySubProtocol => GetResourceString("net_WebSockets_InvalidEmptySubProtocol");

	internal static string net_WebSockets_InvalidCharInProtocolString => GetResourceString("net_WebSockets_InvalidCharInProtocolString");

	internal static string net_WebSockets_ReasonNotNull => GetResourceString("net_WebSockets_ReasonNotNull");

	internal static string net_WebSockets_InvalidCloseStatusCode => GetResourceString("net_WebSockets_InvalidCloseStatusCode");

	internal static string net_WebSockets_InvalidCloseStatusDescription => GetResourceString("net_WebSockets_InvalidCloseStatusDescription");

	internal static string net_WebSockets_ArgumentOutOfRange_TooSmall => GetResourceString("net_WebSockets_ArgumentOutOfRange_TooSmall");

	internal static string net_WebSockets_ArgumentOutOfRange_TooBig => GetResourceString("net_WebSockets_ArgumentOutOfRange_TooBig");

	internal static string net_WebSockets_UnsupportedPlatform => GetResourceString("net_WebSockets_UnsupportedPlatform");

	internal static string net_readonlystream => GetResourceString("net_readonlystream");

	internal static string net_WebSockets_InvalidState_ClosedOrAborted => GetResourceString("net_WebSockets_InvalidState_ClosedOrAborted");

	internal static string net_WebSockets_ReceiveAsyncDisallowedAfterCloseAsync => GetResourceString("net_WebSockets_ReceiveAsyncDisallowedAfterCloseAsync");

	internal static string net_Websockets_AlreadyOneOutstandingOperation => GetResourceString("net_Websockets_AlreadyOneOutstandingOperation");

	internal static string net_WebSockets_InvalidMessageType => GetResourceString("net_WebSockets_InvalidMessageType");

	internal static string net_WebSockets_InvalidBufferType => GetResourceString("net_WebSockets_InvalidBufferType");

	internal static string net_WebSockets_ArgumentOutOfRange_InternalBuffer => GetResourceString("net_WebSockets_ArgumentOutOfRange_InternalBuffer");

	internal static string net_WebSockets_Argument_InvalidMessageType => GetResourceString("net_WebSockets_Argument_InvalidMessageType");

	internal static string net_securitypackagesupport => GetResourceString("net_securitypackagesupport");

	internal static string net_log_operation_failed_with_error => GetResourceString("net_log_operation_failed_with_error");

	internal static string SSPIInvalidHandleType => GetResourceString("SSPIInvalidHandleType");

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

	internal static string Format(string resourceFormat, object p1, object p2, object p3)
	{
		if (UsingResourceKeys())
		{
			return string.Join(", ", resourceFormat, p1, p2, p3);
		}
		return string.Format(resourceFormat, p1, p2, p3);
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
