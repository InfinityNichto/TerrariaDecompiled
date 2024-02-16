using System.Resources;
using FxResources.System.Net.Requests;

namespace System;

internal static class SR
{
	private static readonly bool s_usingResourceKeys = AppContext.TryGetSwitch("System.Resources.UseSystemResourceKeys", out var isEnabled) && isEnabled;

	private static ResourceManager s_resourceManager;

	internal static ResourceManager ResourceManager => s_resourceManager ?? (s_resourceManager = new ResourceManager(typeof(SR)));

	internal static string net_unknown_prefix => GetResourceString("net_unknown_prefix");

	internal static string net_reqsubmitted => GetResourceString("net_reqsubmitted");

	internal static string net_io_timeout_use_ge_zero => GetResourceString("net_io_timeout_use_ge_zero");

	internal static string net_writestarted => GetResourceString("net_writestarted");

	internal static string net_badmethod => GetResourceString("net_badmethod");

	internal static string net_io_invalidasyncresult => GetResourceString("net_io_invalidasyncresult");

	internal static string net_io_invalidendcall => GetResourceString("net_io_invalidendcall");

	internal static string net_servererror => GetResourceString("net_servererror");

	internal static string net_reqaborted => GetResourceString("net_reqaborted");

	internal static string net_MethodNotImplementedException => GetResourceString("net_MethodNotImplementedException");

	internal static string net_PropertyNotImplementedException => GetResourceString("net_PropertyNotImplementedException");

	internal static string net_nouploadonget => GetResourceString("net_nouploadonget");

	internal static string net_repcall => GetResourceString("net_repcall");

	internal static string net_invalid_enum => GetResourceString("net_invalid_enum");

	internal static string net_requestaborted => GetResourceString("net_requestaborted");

	internal static string net_clsmall => GetResourceString("net_clsmall");

	internal static string net_webstatus_Timeout => GetResourceString("net_webstatus_Timeout");

	internal static string net_baddate => GetResourceString("net_baddate");

	internal static string net_connarg => GetResourceString("net_connarg");

	internal static string net_fromto => GetResourceString("net_fromto");

	internal static string net_needchunked => GetResourceString("net_needchunked");

	internal static string net_no100 => GetResourceString("net_no100");

	internal static string net_nochunked => GetResourceString("net_nochunked");

	internal static string net_nottoken => GetResourceString("net_nottoken");

	internal static string net_rangetoosmall => GetResourceString("net_rangetoosmall");

	internal static string net_rangetype => GetResourceString("net_rangetype");

	internal static string net_toosmall => GetResourceString("net_toosmall");

	internal static string net_wrongversion => GetResourceString("net_wrongversion");

	internal static string net_WebHeaderInvalidControlChars => GetResourceString("net_WebHeaderInvalidControlChars");

	internal static string net_WebHeaderInvalidCRLFChars => GetResourceString("net_WebHeaderInvalidCRLFChars");

	internal static string net_timeout => GetResourceString("net_timeout");

	internal static string net_PropertyNotSupportedException => GetResourceString("net_PropertyNotSupportedException");

	internal static string net_InvalidStatusCode => GetResourceString("net_InvalidStatusCode");

	internal static string net_io_timeout_use_gt_zero => GetResourceString("net_io_timeout_use_gt_zero");

	internal static string net_ftp_servererror => GetResourceString("net_ftp_servererror");

	internal static string net_ftp_active_address_different => GetResourceString("net_ftp_active_address_different");

	internal static string net_ftp_invalid_method_name => GetResourceString("net_ftp_invalid_method_name");

	internal static string net_ftp_invalid_renameto => GetResourceString("net_ftp_invalid_renameto");

	internal static string net_ftp_invalid_response_filename => GetResourceString("net_ftp_invalid_response_filename");

	internal static string net_ftp_invalid_status_response => GetResourceString("net_ftp_invalid_status_response");

	internal static string net_ftp_invalid_uri => GetResourceString("net_ftp_invalid_uri");

	internal static string net_ftp_no_defaultcreds => GetResourceString("net_ftp_no_defaultcreds");

	internal static string net_ftp_response_invalid_format => GetResourceString("net_ftp_response_invalid_format");

	internal static string net_ftp_server_failed_passive => GetResourceString("net_ftp_server_failed_passive");

	internal static string net_ftp_unsupported_method => GetResourceString("net_ftp_unsupported_method");

	internal static string net_ftp_protocolerror => GetResourceString("net_ftp_protocolerror");

	internal static string net_ftp_receivefailure => GetResourceString("net_ftp_receivefailure");

	internal static string net_webstatus_NameResolutionFailure => GetResourceString("net_webstatus_NameResolutionFailure");

	internal static string net_webstatus_ConnectFailure => GetResourceString("net_webstatus_ConnectFailure");

	internal static string net_ftpstatuscode_ServiceNotAvailable => GetResourceString("net_ftpstatuscode_ServiceNotAvailable");

	internal static string net_ftpstatuscode_CantOpenData => GetResourceString("net_ftpstatuscode_CantOpenData");

	internal static string net_ftpstatuscode_ConnectionClosed => GetResourceString("net_ftpstatuscode_ConnectionClosed");

	internal static string net_ftpstatuscode_ActionNotTakenFileUnavailableOrBusy => GetResourceString("net_ftpstatuscode_ActionNotTakenFileUnavailableOrBusy");

	internal static string net_ftpstatuscode_ActionAbortedLocalProcessingError => GetResourceString("net_ftpstatuscode_ActionAbortedLocalProcessingError");

	internal static string net_ftpstatuscode_ActionNotTakenInsufficientSpace => GetResourceString("net_ftpstatuscode_ActionNotTakenInsufficientSpace");

	internal static string net_ftpstatuscode_CommandSyntaxError => GetResourceString("net_ftpstatuscode_CommandSyntaxError");

	internal static string net_ftpstatuscode_ArgumentSyntaxError => GetResourceString("net_ftpstatuscode_ArgumentSyntaxError");

	internal static string net_ftpstatuscode_CommandNotImplemented => GetResourceString("net_ftpstatuscode_CommandNotImplemented");

	internal static string net_ftpstatuscode_BadCommandSequence => GetResourceString("net_ftpstatuscode_BadCommandSequence");

	internal static string net_ftpstatuscode_NotLoggedIn => GetResourceString("net_ftpstatuscode_NotLoggedIn");

	internal static string net_ftpstatuscode_AccountNeeded => GetResourceString("net_ftpstatuscode_AccountNeeded");

	internal static string net_ftpstatuscode_ActionNotTakenFileUnavailable => GetResourceString("net_ftpstatuscode_ActionNotTakenFileUnavailable");

	internal static string net_ftpstatuscode_ActionAbortedUnknownPageType => GetResourceString("net_ftpstatuscode_ActionAbortedUnknownPageType");

	internal static string net_ftpstatuscode_FileActionAborted => GetResourceString("net_ftpstatuscode_FileActionAborted");

	internal static string net_ftpstatuscode_ActionNotTakenFilenameNotAllowed => GetResourceString("net_ftpstatuscode_ActionNotTakenFilenameNotAllowed");

	internal static string net_invalid_host => GetResourceString("net_invalid_host");

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
