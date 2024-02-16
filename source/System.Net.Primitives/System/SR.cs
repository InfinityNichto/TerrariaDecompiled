using System.Resources;
using FxResources.System.Net.Primitives;

namespace System;

internal static class SR
{
	private static readonly bool s_usingResourceKeys = AppContext.TryGetSwitch("System.Resources.UseSystemResourceKeys", out var isEnabled) && isEnabled;

	private static ResourceManager s_resourceManager;

	internal static ResourceManager ResourceManager => s_resourceManager ?? (s_resourceManager = new ResourceManager(typeof(SR)));

	internal static string net_toosmall => GetResourceString("net_toosmall");

	internal static string net_MethodNotImplementedException => GetResourceString("net_MethodNotImplementedException");

	internal static string net_PropertyNotImplementedException => GetResourceString("net_PropertyNotImplementedException");

	internal static string net_InvalidAddressFamily => GetResourceString("net_InvalidAddressFamily");

	internal static string net_InvalidSocketAddressSize => GetResourceString("net_InvalidSocketAddressSize");

	internal static string net_sockets_invalid_optionValue_all => GetResourceString("net_sockets_invalid_optionValue_all");

	internal static string net_emptystringcall => GetResourceString("net_emptystringcall");

	internal static string dns_bad_ip_address => GetResourceString("dns_bad_ip_address");

	internal static string net_container_add_cookie => GetResourceString("net_container_add_cookie");

	internal static string net_cookie_size => GetResourceString("net_cookie_size");

	internal static string net_cookie_parse_header => GetResourceString("net_cookie_parse_header");

	internal static string net_cookie_attribute => GetResourceString("net_cookie_attribute");

	internal static string net_cookie_format => GetResourceString("net_cookie_format");

	internal static string net_cookie_capacity_range => GetResourceString("net_cookie_capacity_range");

	internal static string net_collection_readonly => GetResourceString("net_collection_readonly");

	internal static string net_nodefaultcreds => GetResourceString("net_nodefaultcreds");

	internal static string InvalidOperation_EnumFailedVersion => GetResourceString("InvalidOperation_EnumFailedVersion");

	internal static string InvalidOperation_EnumOpCantHappen => GetResourceString("InvalidOperation_EnumOpCantHappen");

	internal static string bad_endpoint_string => GetResourceString("bad_endpoint_string");

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
