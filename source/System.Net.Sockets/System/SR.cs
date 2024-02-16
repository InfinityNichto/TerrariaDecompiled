using System.Resources;
using FxResources.System.Net.Sockets;

namespace System;

internal static class SR
{
	private static readonly bool s_usingResourceKeys = AppContext.TryGetSwitch("System.Resources.UseSystemResourceKeys", out var isEnabled) && isEnabled;

	private static ResourceManager s_resourceManager;

	internal static ResourceManager ResourceManager => s_resourceManager ?? (s_resourceManager = new ResourceManager(typeof(SR)));

	internal static string Arg_InvalidHandle => GetResourceString("Arg_InvalidHandle");

	internal static string net_invalidversion => GetResourceString("net_invalidversion");

	internal static string net_noseek => GetResourceString("net_noseek");

	internal static string net_invasync => GetResourceString("net_invasync");

	internal static string net_io_timeout_use_gt_zero => GetResourceString("net_io_timeout_use_gt_zero");

	internal static string net_notconnected => GetResourceString("net_notconnected");

	internal static string net_notstream => GetResourceString("net_notstream");

	internal static string net_stopped => GetResourceString("net_stopped");

	internal static string net_udpconnected => GetResourceString("net_udpconnected");

	internal static string net_readonlystream => GetResourceString("net_readonlystream");

	internal static string net_writeonlystream => GetResourceString("net_writeonlystream");

	internal static string net_InvalidAddressFamily => GetResourceString("net_InvalidAddressFamily");

	internal static string net_InvalidEndPointAddressFamily => GetResourceString("net_InvalidEndPointAddressFamily");

	internal static string net_InvalidSocketAddressSize => GetResourceString("net_InvalidSocketAddressSize");

	internal static string net_invalidAddressList => GetResourceString("net_invalidAddressList");

	internal static string net_protocol_invalid_family => GetResourceString("net_protocol_invalid_family");

	internal static string net_protocol_invalid_multicast_family => GetResourceString("net_protocol_invalid_multicast_family");

	internal static string net_sockets_zerolist => GetResourceString("net_sockets_zerolist");

	internal static string net_sockets_blocking => GetResourceString("net_sockets_blocking");

	internal static string net_sockets_useblocking => GetResourceString("net_sockets_useblocking");

	internal static string net_sockets_select => GetResourceString("net_sockets_select");

	internal static string net_sockets_toolarge_select => GetResourceString("net_sockets_toolarge_select");

	internal static string net_sockets_empty_select => GetResourceString("net_sockets_empty_select");

	internal static string net_sockets_mustbind => GetResourceString("net_sockets_mustbind");

	internal static string net_sockets_mustlisten => GetResourceString("net_sockets_mustlisten");

	internal static string net_sockets_mustnotlisten => GetResourceString("net_sockets_mustnotlisten");

	internal static string net_sockets_mustnotbebound => GetResourceString("net_sockets_mustnotbebound");

	internal static string net_sockets_namedmustnotbebound => GetResourceString("net_sockets_namedmustnotbebound");

	internal static string net_sockets_invalid_ipaddress_length => GetResourceString("net_sockets_invalid_ipaddress_length");

	internal static string net_sockets_invalid_optionValue => GetResourceString("net_sockets_invalid_optionValue");

	internal static string net_sockets_invalid_optionValue_all => GetResourceString("net_sockets_invalid_optionValue_all");

	internal static string net_sockets_invalid_dnsendpoint => GetResourceString("net_sockets_invalid_dnsendpoint");

	internal static string net_sockets_disconnectedConnect => GetResourceString("net_sockets_disconnectedConnect");

	internal static string net_sockets_disconnectedAccept => GetResourceString("net_sockets_disconnectedAccept");

	internal static string net_tcplistener_mustbestopped => GetResourceString("net_tcplistener_mustbestopped");

	internal static string net_socketopinprogress => GetResourceString("net_socketopinprogress");

	internal static string net_buffercounttoosmall => GetResourceString("net_buffercounttoosmall");

	internal static string net_multibuffernotsupported => GetResourceString("net_multibuffernotsupported");

	internal static string net_ambiguousbuffers => GetResourceString("net_ambiguousbuffers");

	internal static string net_io_writefailure => GetResourceString("net_io_writefailure");

	internal static string net_io_readfailure => GetResourceString("net_io_readfailure");

	internal static string net_value_cannot_be_negative => GetResourceString("net_value_cannot_be_negative");

	internal static string ArgumentOutOfRange_Bounds_Lower_Upper_Named => GetResourceString("ArgumentOutOfRange_Bounds_Lower_Upper_Named");

	internal static string ArgumentOutOfRange_PathLengthInvalid => GetResourceString("ArgumentOutOfRange_PathLengthInvalid");

	internal static string net_io_readwritefailure => GetResourceString("net_io_readwritefailure");

	internal static string InvalidOperation_BufferNotExplicitArray => GetResourceString("InvalidOperation_BufferNotExplicitArray");

	internal static string InvalidOperation_IncorrectToken => GetResourceString("InvalidOperation_IncorrectToken");

	internal static string InvalidOperation_MultipleContinuations => GetResourceString("InvalidOperation_MultipleContinuations");

	internal static string net_sockets_sendpackelement_FileStreamMustBeAsync => GetResourceString("net_sockets_sendpackelement_FileStreamMustBeAsync");

	internal static string net_sockets_valuetaskmisuse => GetResourceString("net_sockets_valuetaskmisuse");

	internal static string net_sockets_invalid_socketinformation => GetResourceString("net_sockets_invalid_socketinformation");

	internal static string net_sockets_asyncoperations_notallowed => GetResourceString("net_sockets_asyncoperations_notallowed");

	internal static string InvalidNullArgument => GetResourceString("InvalidNullArgument");

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
}
