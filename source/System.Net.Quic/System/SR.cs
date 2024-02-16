using System.Resources;
using FxResources.System.Net.Quic;

namespace System;

internal static class SR
{
	private static readonly bool s_usingResourceKeys = AppContext.TryGetSwitch("System.Resources.UseSystemResourceKeys", out var isEnabled) && isEnabled;

	private static ResourceManager s_resourceManager;

	internal static ResourceManager ResourceManager => s_resourceManager ?? (s_resourceManager = new ResourceManager(typeof(SR)));

	internal static string net_quic_addressfamily_notsupported => GetResourceString("net_quic_addressfamily_notsupported");

	internal static string net_quic_connectionaborted => GetResourceString("net_quic_connectionaborted");

	internal static string net_quic_operationaborted => GetResourceString("net_quic_operationaborted");

	internal static string net_quic_reading_notallowed => GetResourceString("net_quic_reading_notallowed");

	internal static string net_quic_sending_aborted => GetResourceString("net_quic_sending_aborted");

	internal static string net_quic_streamaborted => GetResourceString("net_quic_streamaborted");

	internal static string net_quic_unsupported_address_family => GetResourceString("net_quic_unsupported_address_family");

	internal static string net_quic_writing_notallowed => GetResourceString("net_quic_writing_notallowed");

	internal static string net_quic_timeout_use_gt_zero => GetResourceString("net_quic_timeout_use_gt_zero");

	internal static string net_quic_timeout => GetResourceString("net_quic_timeout");

	internal static string net_quic_ssl_option => GetResourceString("net_quic_ssl_option");

	internal static string net_quic_cert_custom_validation => GetResourceString("net_quic_cert_custom_validation");

	internal static string net_quic_cert_chain_validation => GetResourceString("net_quic_cert_chain_validation");

	internal static string net_quic_not_connected => GetResourceString("net_quic_not_connected");

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
