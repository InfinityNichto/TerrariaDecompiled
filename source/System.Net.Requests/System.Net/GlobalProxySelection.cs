using System.Diagnostics.CodeAnalysis;

namespace System.Net;

[Obsolete("GlobalProxySelection has been deprecated. Use WebRequest.DefaultWebProxy instead to access and set the global default proxy. Use 'null' instead of GetEmptyWebProxy.")]
public class GlobalProxySelection
{
	private sealed class EmptyWebProxy : IWebProxy
	{
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

		public Uri GetProxy(Uri uri)
		{
			return uri;
		}

		public bool IsBypassed(Uri uri)
		{
			return true;
		}
	}

	public static IWebProxy Select
	{
		get
		{
			return WebRequest.DefaultWebProxy ?? GetEmptyWebProxy();
		}
		[param: AllowNull]
		set
		{
			WebRequest.DefaultWebProxy = value;
		}
	}

	public static IWebProxy GetEmptyWebProxy()
	{
		return new EmptyWebProxy();
	}
}
