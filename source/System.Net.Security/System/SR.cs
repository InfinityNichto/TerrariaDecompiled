using System.Resources;
using FxResources.System.Net.Security;

namespace System;

internal static class SR
{
	private static readonly bool s_usingResourceKeys = AppContext.TryGetSwitch("System.Resources.UseSystemResourceKeys", out var isEnabled) && isEnabled;

	private static ResourceManager s_resourceManager;

	internal static ResourceManager ResourceManager => s_resourceManager ?? (s_resourceManager = new ResourceManager(typeof(SR)));

	internal static string net_noseek => GetResourceString("net_noseek");

	internal static string net_securitypackagesupport => GetResourceString("net_securitypackagesupport");

	internal static string net_MethodNotImplementedException => GetResourceString("net_MethodNotImplementedException");

	internal static string net_io_readfailure => GetResourceString("net_io_readfailure");

	internal static string net_io_connectionclosed => GetResourceString("net_io_connectionclosed");

	internal static string net_io_invalidnestedcall => GetResourceString("net_io_invalidnestedcall");

	internal static string net_io_must_be_rw_stream => GetResourceString("net_io_must_be_rw_stream");

	internal static string net_io_header_id => GetResourceString("net_io_header_id");

	internal static string net_io_out_range => GetResourceString("net_io_out_range");

	internal static string net_io_encrypt => GetResourceString("net_io_encrypt");

	internal static string net_io_decrypt => GetResourceString("net_io_decrypt");

	internal static string net_io_read => GetResourceString("net_io_read");

	internal static string net_io_write => GetResourceString("net_io_write");

	internal static string net_io_eof => GetResourceString("net_io_eof");

	internal static string net_ssl_io_frame => GetResourceString("net_ssl_io_frame");

	internal static string net_ssl_io_renego => GetResourceString("net_ssl_io_renego");

	internal static string net_ssl_io_cert_validation => GetResourceString("net_ssl_io_cert_validation");

	internal static string net_ssl_io_cert_chain_validation => GetResourceString("net_ssl_io_cert_chain_validation");

	internal static string net_ssl_io_cert_custom_validation => GetResourceString("net_ssl_io_cert_custom_validation");

	internal static string net_ssl_io_no_server_cert => GetResourceString("net_ssl_io_no_server_cert");

	internal static string net_ssl_io_already_shutdown => GetResourceString("net_ssl_io_already_shutdown");

	internal static string net_auth_bad_client_creds => GetResourceString("net_auth_bad_client_creds");

	internal static string net_auth_bad_client_creds_or_target_mismatch => GetResourceString("net_auth_bad_client_creds_or_target_mismatch");

	internal static string net_auth_context_expectation => GetResourceString("net_auth_context_expectation");

	internal static string net_auth_context_expectation_remote => GetResourceString("net_auth_context_expectation_remote");

	internal static string net_auth_supported_impl_levels => GetResourceString("net_auth_supported_impl_levels");

	internal static string net_auth_reauth => GetResourceString("net_auth_reauth");

	internal static string net_auth_noauth => GetResourceString("net_auth_noauth");

	internal static string net_auth_client_server => GetResourceString("net_auth_client_server");

	internal static string net_auth_SSPI => GetResourceString("net_auth_SSPI");

	internal static string net_auth_eof => GetResourceString("net_auth_eof");

	internal static string net_auth_tls_alert => GetResourceString("net_auth_tls_alert");

	internal static string net_auth_alert => GetResourceString("net_auth_alert");

	internal static string net_auth_message_not_encrypted => GetResourceString("net_auth_message_not_encrypted");

	internal static string net_auth_must_specify_extended_protection_scheme => GetResourceString("net_auth_must_specify_extended_protection_scheme");

	internal static string net_frame_size => GetResourceString("net_frame_size");

	internal static string net_frame_read_size => GetResourceString("net_frame_read_size");

	internal static string net_frame_max_size => GetResourceString("net_frame_max_size");

	internal static string net_invalid_enum => GetResourceString("net_invalid_enum");

	internal static string net_log_open_store_failed => GetResourceString("net_log_open_store_failed");

	internal static string net_log_remote_cert_has_errors => GetResourceString("net_log_remote_cert_has_errors");

	internal static string net_log_remote_cert_not_available => GetResourceString("net_log_remote_cert_not_available");

	internal static string net_log_remote_cert_name_mismatch => GetResourceString("net_log_remote_cert_name_mismatch");

	internal static string event_OperationReturnedSomething => GetResourceString("event_OperationReturnedSomething");

	internal static string net_log_operation_failed_with_error => GetResourceString("net_log_operation_failed_with_error");

	internal static string SSPIInvalidHandleType => GetResourceString("SSPIInvalidHandleType");

	internal static string security_ExtendedProtectionPolicy_UseDifferentConstructorForNever => GetResourceString("security_ExtendedProtectionPolicy_UseDifferentConstructorForNever");

	internal static string security_ExtendedProtectionPolicy_NoEmptyServiceNameCollection => GetResourceString("security_ExtendedProtectionPolicy_NoEmptyServiceNameCollection");

	internal static string security_ServiceNameCollection_EmptyServiceName => GetResourceString("security_ServiceNameCollection_EmptyServiceName");

	internal static string net_ssl_app_protocols_invalid => GetResourceString("net_ssl_app_protocols_invalid");

	internal static string net_ssl_app_protocol_invalid => GetResourceString("net_ssl_app_protocol_invalid");

	internal static string net_conflicting_options => GetResourceString("net_conflicting_options");

	internal static string net_ssl_ciphersuites_policy_not_supported => GetResourceString("net_ssl_ciphersuites_policy_not_supported");

	internal static string net_ssl_certificate_exist => GetResourceString("net_ssl_certificate_exist");

	internal static string net_ssl_renegotiate_data => GetResourceString("net_ssl_renegotiate_data");

	internal static string net_ssl_renegotiate_buffer => GetResourceString("net_ssl_renegotiate_buffer");

	internal static string net_ssl_trust_store => GetResourceString("net_ssl_trust_store");

	internal static string net_ssl_trust_collection => GetResourceString("net_ssl_trust_collection");

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
