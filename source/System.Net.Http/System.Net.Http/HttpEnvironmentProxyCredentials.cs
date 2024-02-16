namespace System.Net.Http;

internal sealed class HttpEnvironmentProxyCredentials : ICredentials
{
	private readonly NetworkCredential _httpCred;

	private readonly NetworkCredential _httpsCred;

	private readonly Uri _httpProxy;

	private readonly Uri _httpsProxy;

	public HttpEnvironmentProxyCredentials(Uri httpProxy, NetworkCredential httpCred, Uri httpsProxy, NetworkCredential httpsCred)
	{
		_httpCred = httpCred;
		_httpsCred = httpsCred;
		_httpProxy = httpProxy;
		_httpsProxy = httpsProxy;
	}

	public NetworkCredential GetCredential(Uri uri, string authType)
	{
		if (uri == null)
		{
			return null;
		}
		if (!uri.Equals(_httpProxy))
		{
			if (!uri.Equals(_httpsProxy))
			{
				return null;
			}
			return _httpsCred;
		}
		return _httpCred;
	}

	public static HttpEnvironmentProxyCredentials TryCreate(Uri httpProxy, Uri httpsProxy)
	{
		NetworkCredential networkCredential = null;
		NetworkCredential networkCredential2 = null;
		if (httpProxy != null)
		{
			networkCredential = GetCredentialsFromString(httpProxy.UserInfo);
		}
		if (httpsProxy != null)
		{
			networkCredential2 = GetCredentialsFromString(httpsProxy.UserInfo);
		}
		if (networkCredential == null && networkCredential2 == null)
		{
			return null;
		}
		return new HttpEnvironmentProxyCredentials(httpProxy, networkCredential, httpsProxy, networkCredential2);
	}

	private static NetworkCredential GetCredentialsFromString(string value)
	{
		if (string.IsNullOrWhiteSpace(value))
		{
			return null;
		}
		value = Uri.UnescapeDataString(value);
		string password = "";
		string domain = null;
		int num = value.IndexOf(':');
		if (num != -1)
		{
			password = value.Substring(num + 1);
			value = value.Substring(0, num);
		}
		num = value.IndexOf('\\');
		if (num != -1)
		{
			domain = value.Substring(0, num);
			value = value.Substring(num + 1);
		}
		return new NetworkCredential(value, password, domain);
	}
}
