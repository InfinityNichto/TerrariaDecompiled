using System.Resources;
using FxResources.System.Net.Http;

namespace System;

internal static class SR
{
	private static readonly bool s_usingResourceKeys = AppContext.TryGetSwitch("System.Resources.UseSystemResourceKeys", out var isEnabled) && isEnabled;

	private static ResourceManager s_resourceManager;

	internal static ResourceManager ResourceManager => s_resourceManager ?? (s_resourceManager = new ResourceManager(typeof(SR)));

	internal static string net_http_httpmethod_format_error => GetResourceString("net_http_httpmethod_format_error");

	internal static string net_http_reasonphrase_format_error => GetResourceString("net_http_reasonphrase_format_error");

	internal static string net_http_copyto_array_too_small => GetResourceString("net_http_copyto_array_too_small");

	internal static string net_http_headers_not_found => GetResourceString("net_http_headers_not_found");

	internal static string net_http_headers_single_value_header => GetResourceString("net_http_headers_single_value_header");

	internal static string net_http_headers_invalid_header_name => GetResourceString("net_http_headers_invalid_header_name");

	internal static string net_http_headers_invalid_value => GetResourceString("net_http_headers_invalid_value");

	internal static string net_http_headers_not_allowed_header_name => GetResourceString("net_http_headers_not_allowed_header_name");

	internal static string net_http_headers_invalid_host_header => GetResourceString("net_http_headers_invalid_host_header");

	internal static string net_http_headers_invalid_etag_name => GetResourceString("net_http_headers_invalid_etag_name");

	internal static string net_http_headers_invalid_range => GetResourceString("net_http_headers_invalid_range");

	internal static string net_http_headers_no_newlines => GetResourceString("net_http_headers_no_newlines");

	internal static string net_http_content_buffersize_exceeded => GetResourceString("net_http_content_buffersize_exceeded");

	internal static string net_http_content_no_task_returned => GetResourceString("net_http_content_no_task_returned");

	internal static string net_http_content_stream_already_read => GetResourceString("net_http_content_stream_already_read");

	internal static string net_http_content_readonly_stream => GetResourceString("net_http_content_readonly_stream");

	internal static string net_http_content_invalid_charset => GetResourceString("net_http_content_invalid_charset");

	internal static string net_http_content_stream_copy_error => GetResourceString("net_http_content_stream_copy_error");

	internal static string net_http_content_read_as_stream_has_task => GetResourceString("net_http_content_read_as_stream_has_task");

	internal static string net_http_argument_empty_string => GetResourceString("net_http_argument_empty_string");

	internal static string net_http_client_request_already_sent => GetResourceString("net_http_client_request_already_sent");

	internal static string net_http_operation_started => GetResourceString("net_http_operation_started");

	internal static string net_http_client_execution_error => GetResourceString("net_http_client_execution_error");

	internal static string net_http_client_absolute_baseaddress_required => GetResourceString("net_http_client_absolute_baseaddress_required");

	internal static string net_http_client_invalid_requesturi => GetResourceString("net_http_client_invalid_requesturi");

	internal static string net_http_unsupported_requesturi_scheme => GetResourceString("net_http_unsupported_requesturi_scheme");

	internal static string net_http_parser_invalid_base64_string => GetResourceString("net_http_parser_invalid_base64_string");

	internal static string net_http_handler_noresponse => GetResourceString("net_http_handler_noresponse");

	internal static string net_http_handler_norequest => GetResourceString("net_http_handler_norequest");

	internal static string net_http_message_not_success_statuscode => GetResourceString("net_http_message_not_success_statuscode");

	internal static string net_http_content_field_too_long => GetResourceString("net_http_content_field_too_long");

	internal static string net_http_log_headers_no_newlines => GetResourceString("net_http_log_headers_no_newlines");

	internal static string net_http_log_headers_invalid_quality => GetResourceString("net_http_log_headers_invalid_quality");

	internal static string net_http_handler_not_assigned => GetResourceString("net_http_handler_not_assigned");

	internal static string net_http_invalid_enable_first => GetResourceString("net_http_invalid_enable_first");

	internal static string net_http_content_buffersize_limit => GetResourceString("net_http_content_buffersize_limit");

	internal static string net_http_chunked_not_allowed_with_empty_content => GetResourceString("net_http_chunked_not_allowed_with_empty_content");

	internal static string net_http_value_must_be_greater_than => GetResourceString("net_http_value_must_be_greater_than");

	internal static string net_http_value_must_be_greater_than_or_equal => GetResourceString("net_http_value_must_be_greater_than_or_equal");

	internal static string net_http_invalid_response => GetResourceString("net_http_invalid_response");

	internal static string net_http_invalid_response_premature_eof => GetResourceString("net_http_invalid_response_premature_eof");

	internal static string net_http_invalid_response_missing_frame => GetResourceString("net_http_invalid_response_missing_frame");

	internal static string net_http_invalid_response_premature_eof_bytecount => GetResourceString("net_http_invalid_response_premature_eof_bytecount");

	internal static string net_http_invalid_response_chunk_header_invalid => GetResourceString("net_http_invalid_response_chunk_header_invalid");

	internal static string net_http_invalid_response_chunk_extension_invalid => GetResourceString("net_http_invalid_response_chunk_extension_invalid");

	internal static string net_http_invalid_response_chunk_terminator_invalid => GetResourceString("net_http_invalid_response_chunk_terminator_invalid");

	internal static string net_http_invalid_response_status_line => GetResourceString("net_http_invalid_response_status_line");

	internal static string net_http_invalid_response_status_code => GetResourceString("net_http_invalid_response_status_code");

	internal static string net_http_invalid_response_status_reason => GetResourceString("net_http_invalid_response_status_reason");

	internal static string net_http_invalid_response_multiple_status_codes => GetResourceString("net_http_invalid_response_multiple_status_codes");

	internal static string net_http_invalid_response_header_folder => GetResourceString("net_http_invalid_response_header_folder");

	internal static string net_http_invalid_response_header_line => GetResourceString("net_http_invalid_response_header_line");

	internal static string net_http_invalid_response_header_name => GetResourceString("net_http_invalid_response_header_name");

	internal static string net_http_request_aborted => GetResourceString("net_http_request_aborted");

	internal static string net_http_invalid_response_pseudo_header_in_trailer => GetResourceString("net_http_invalid_response_pseudo_header_in_trailer");

	internal static string net_http_response_headers_exceeded_length => GetResourceString("net_http_response_headers_exceeded_length");

	internal static string ArgumentOutOfRange_NeedNonNegativeNum => GetResourceString("ArgumentOutOfRange_NeedNonNegativeNum");

	internal static string ObjectDisposed_StreamClosed => GetResourceString("ObjectDisposed_StreamClosed");

	internal static string net_http_invalid_proxy_scheme => GetResourceString("net_http_invalid_proxy_scheme");

	internal static string net_http_request_invalid_char_encoding => GetResourceString("net_http_request_invalid_char_encoding");

	internal static string net_http_ssl_connection_failed => GetResourceString("net_http_ssl_connection_failed");

	internal static string net_http_unsupported_chunking => GetResourceString("net_http_unsupported_chunking");

	internal static string net_http_unsupported_version => GetResourceString("net_http_unsupported_version");

	internal static string IO_SeekBeforeBegin => GetResourceString("IO_SeekBeforeBegin");

	internal static string net_ssl_http2_requires_tls12 => GetResourceString("net_ssl_http2_requires_tls12");

	internal static string net_http_request_no_host => GetResourceString("net_http_request_no_host");

	internal static string net_http_http2_connection_error => GetResourceString("net_http_http2_connection_error");

	internal static string net_http_http2_stream_error => GetResourceString("net_http_http2_stream_error");

	internal static string net_http_http2_connection_not_established => GetResourceString("net_http_http2_connection_not_established");

	internal static string net_http_http2_invalidinitialstreamwindowsize => GetResourceString("net_http_http2_invalidinitialstreamwindowsize");

	internal static string net_log_operation_failed_with_error => GetResourceString("net_log_operation_failed_with_error");

	internal static string net_securitypackagesupport => GetResourceString("net_securitypackagesupport");

	internal static string net_http_authconnectionfailure => GetResourceString("net_http_authconnectionfailure");

	internal static string net_http_hpack_huffman_decode_failed => GetResourceString("net_http_hpack_huffman_decode_failed");

	internal static string net_http_hpack_incomplete_header_block => GetResourceString("net_http_hpack_incomplete_header_block");

	internal static string net_http_hpack_late_dynamic_table_size_update => GetResourceString("net_http_hpack_late_dynamic_table_size_update");

	internal static string net_http_hpack_bad_integer => GetResourceString("net_http_hpack_bad_integer");

	internal static string net_http_disposed_while_in_use => GetResourceString("net_http_disposed_while_in_use");

	internal static string net_http_hpack_large_table_size_update => GetResourceString("net_http_hpack_large_table_size_update");

	internal static string net_http_server_shutdown => GetResourceString("net_http_server_shutdown");

	internal static string net_http_hpack_invalid_index => GetResourceString("net_http_hpack_invalid_index");

	internal static string net_http_hpack_unexpected_end => GetResourceString("net_http_hpack_unexpected_end");

	internal static string net_http_headers_exceeded_length => GetResourceString("net_http_headers_exceeded_length");

	internal static string net_http_invalid_header_name => GetResourceString("net_http_invalid_header_name");

	internal static string net_http_http3_connection_error => GetResourceString("net_http_http3_connection_error");

	internal static string net_http_retry_on_older_version => GetResourceString("net_http_retry_on_older_version");

	internal static string net_http_content_write_larger_than_content_length => GetResourceString("net_http_content_write_larger_than_content_length");

	internal static string net_http_qpack_no_dynamic_table => GetResourceString("net_http_qpack_no_dynamic_table");

	internal static string net_http_request_timedout => GetResourceString("net_http_request_timedout");

	internal static string net_http_connect_timedout => GetResourceString("net_http_connect_timedout");

	internal static string net_http_missing_sync_implementation => GetResourceString("net_http_missing_sync_implementation");

	internal static string net_http_http2_sync_not_supported => GetResourceString("net_http_http2_sync_not_supported");

	internal static string net_http_upgrade_not_enabled_sync => GetResourceString("net_http_upgrade_not_enabled_sync");

	internal static string net_http_requested_version_cannot_establish => GetResourceString("net_http_requested_version_cannot_establish");

	internal static string net_http_requested_version_server_refused => GetResourceString("net_http_requested_version_server_refused");

	internal static string net_http_exception_during_plaintext_filter => GetResourceString("net_http_exception_during_plaintext_filter");

	internal static string net_http_null_from_connect_callback => GetResourceString("net_http_null_from_connect_callback");

	internal static string net_http_null_from_plaintext_filter => GetResourceString("net_http_null_from_plaintext_filter");

	internal static string net_socks_auth_failed => GetResourceString("net_socks_auth_failed");

	internal static string net_socks_bad_address_type => GetResourceString("net_socks_bad_address_type");

	internal static string net_socks_connection_failed => GetResourceString("net_socks_connection_failed");

	internal static string net_socks_ipv6_notsupported => GetResourceString("net_socks_ipv6_notsupported");

	internal static string net_socks_no_auth_method => GetResourceString("net_socks_no_auth_method");

	internal static string net_socks_no_ipv4_address => GetResourceString("net_socks_no_ipv4_address");

	internal static string net_socks_unexpected_version => GetResourceString("net_socks_unexpected_version");

	internal static string net_socks_string_too_long => GetResourceString("net_socks_string_too_long");

	internal static string net_socks_auth_required => GetResourceString("net_socks_auth_required");

	internal static string net_http_proxy_tunnel_returned_failure_status_code => GetResourceString("net_http_proxy_tunnel_returned_failure_status_code");

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

	internal static string Format(IFormatProvider provider, string resourceFormat, object p1)
	{
		if (UsingResourceKeys())
		{
			return string.Join(", ", resourceFormat, p1);
		}
		return string.Format(provider, resourceFormat, p1);
	}

	internal static string Format(IFormatProvider provider, string resourceFormat, object p1, object p2)
	{
		if (UsingResourceKeys())
		{
			return string.Join(", ", resourceFormat, p1, p2);
		}
		return string.Format(provider, resourceFormat, p1, p2);
	}
}
