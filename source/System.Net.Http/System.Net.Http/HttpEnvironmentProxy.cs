using System.Diagnostics.CodeAnalysis;

namespace System.Net.Http;

internal sealed class HttpEnvironmentProxy : IWebProxy
{
	private readonly Uri _httpProxyUri;

	private readonly Uri _httpsProxyUri;

	private readonly string[] _bypass;

	private ICredentials _credentials;

	public ICredentials Credentials
	{
		get
		{
			return _credentials;
		}
		set
		{
			_credentials = value;
		}
	}

	private HttpEnvironmentProxy(Uri httpProxy, Uri httpsProxy, string bypassList)
	{
		_httpProxyUri = httpProxy;
		_httpsProxyUri = httpsProxy;
		_credentials = HttpEnvironmentProxyCredentials.TryCreate(httpProxy, httpsProxy);
		_bypass = bypassList?.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
	}

	private static Uri GetUriFromString(string value)
	{
		if (string.IsNullOrEmpty(value))
		{
			return null;
		}
		if (value.StartsWith("http://", StringComparison.OrdinalIgnoreCase))
		{
			value = value.Substring(7);
		}
		string text = null;
		string text2 = null;
		ushort result = 80;
		int num = value.LastIndexOf('@');
		if (num != -1)
		{
			string text3 = value.Substring(0, num);
			try
			{
				text3 = Uri.UnescapeDataString(text3);
			}
			catch
			{
			}
			value = value.Substring(num + 1);
			num = text3.IndexOf(':');
			if (num == -1)
			{
				text = text3;
			}
			else
			{
				text = text3.Substring(0, num);
				text2 = text3.Substring(num + 1);
			}
		}
		int num2 = value.IndexOf(']');
		num = value.LastIndexOf(':');
		string host;
		if (num == -1 || (num2 != -1 && num < num2))
		{
			host = value;
		}
		else
		{
			host = value.Substring(0, num);
			int i;
			for (i = num + 1; i < value.Length && char.IsDigit(value[i]); i++)
			{
			}
			if (!ushort.TryParse(value.AsSpan(num + 1, i - num - 1), out result))
			{
				return null;
			}
		}
		try
		{
			UriBuilder uriBuilder = new UriBuilder("http", host, result);
			if (text != null)
			{
				uriBuilder.UserName = Uri.EscapeDataString(text);
			}
			if (text2 != null)
			{
				uriBuilder.Password = Uri.EscapeDataString(text2);
			}
			return uriBuilder.Uri;
		}
		catch
		{
		}
		return null;
	}

	private bool IsMatchInBypassList(Uri input)
	{
		if (_bypass != null)
		{
			string[] bypass = _bypass;
			foreach (string text in bypass)
			{
				if (text[0] == '.')
				{
					if (text.Length - 1 == input.Host.Length && string.Compare(text, 1, input.Host, 0, input.Host.Length, StringComparison.OrdinalIgnoreCase) == 0)
					{
						return true;
					}
					if (input.Host.EndsWith(text, StringComparison.OrdinalIgnoreCase))
					{
						return true;
					}
				}
				else if (string.Equals(text, input.Host, StringComparison.OrdinalIgnoreCase))
				{
					return true;
				}
			}
		}
		return false;
	}

	public Uri GetProxy(Uri uri)
	{
		if (!HttpUtilities.IsSupportedNonSecureScheme(uri.Scheme))
		{
			return _httpsProxyUri;
		}
		return _httpProxyUri;
	}

	public bool IsBypassed(Uri uri)
	{
		if (!(GetProxy(uri) == null))
		{
			return IsMatchInBypassList(uri);
		}
		return true;
	}

	public static bool TryCreate([NotNullWhen(true)] out IWebProxy proxy)
	{
		Uri uri = null;
		if (Environment.GetEnvironmentVariable("GATEWAY_INTERFACE") == null)
		{
			uri = GetUriFromString(Environment.GetEnvironmentVariable("HTTP_PROXY"));
		}
		Uri uri2 = GetUriFromString(Environment.GetEnvironmentVariable("HTTPS_PROXY"));
		if (uri == null || uri2 == null)
		{
			Uri uriFromString = GetUriFromString(Environment.GetEnvironmentVariable("ALL_PROXY"));
			if (uri == null)
			{
				uri = uriFromString;
			}
			if (uri2 == null)
			{
				uri2 = uriFromString;
			}
		}
		if (uri == null && uri2 == null)
		{
			proxy = null;
			return false;
		}
		string environmentVariable = Environment.GetEnvironmentVariable("NO_PROXY");
		proxy = new HttpEnvironmentProxy(uri, uri2, environmentVariable);
		return true;
	}
}
