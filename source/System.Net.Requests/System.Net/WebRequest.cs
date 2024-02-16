using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Net.Cache;
using System.Net.Http;
using System.Net.Security;
using System.Runtime.Serialization;
using System.Security.Principal;
using System.Threading;
using System.Threading.Tasks;

namespace System.Net;

public abstract class WebRequest : MarshalByRefObject, ISerializable
{
	internal sealed class WebRequestPrefixElement
	{
		public readonly string Prefix;

		public readonly IWebRequestCreate Creator;

		public WebRequestPrefixElement(string prefix, IWebRequestCreate creator)
		{
			Prefix = prefix;
			Creator = creator;
		}
	}

	internal sealed class HttpRequestCreator : IWebRequestCreate
	{
		public WebRequest Create(Uri Uri)
		{
			return new HttpWebRequest(Uri);
		}
	}

	private static List<WebRequestPrefixElement> s_prefixList;

	private static object s_internalSyncObject = new object();

	private static IWebProxy s_DefaultWebProxy;

	private static bool s_DefaultWebProxyInitialized;

	internal static List<WebRequestPrefixElement> PrefixList
	{
		get
		{
			return LazyInitializer.EnsureInitialized(ref s_prefixList, ref s_internalSyncObject, delegate
			{
				HttpRequestCreator creator = new HttpRequestCreator();
				FtpWebRequestCreator creator2 = new FtpWebRequestCreator();
				FileWebRequestCreator creator3 = new FileWebRequestCreator();
				return new List<WebRequestPrefixElement>(4)
				{
					new WebRequestPrefixElement("http:", creator),
					new WebRequestPrefixElement("https:", creator),
					new WebRequestPrefixElement("ftp:", creator2),
					new WebRequestPrefixElement("file:", creator3)
				};
			});
		}
		set
		{
			Volatile.Write(ref s_prefixList, value);
		}
	}

	public static RequestCachePolicy? DefaultCachePolicy { get; set; } = new RequestCachePolicy(RequestCacheLevel.BypassCache);


	public virtual RequestCachePolicy? CachePolicy { get; set; }

	public AuthenticationLevel AuthenticationLevel { get; set; } = AuthenticationLevel.MutualAuthRequested;


	public TokenImpersonationLevel ImpersonationLevel { get; set; } = TokenImpersonationLevel.Delegation;


	public virtual string? ConnectionGroupName
	{
		get
		{
			throw System.NotImplemented.ByDesignWithMessage(System.SR.net_PropertyNotImplementedException);
		}
		set
		{
			throw System.NotImplemented.ByDesignWithMessage(System.SR.net_PropertyNotImplementedException);
		}
	}

	public virtual string Method
	{
		get
		{
			throw System.NotImplemented.ByDesignWithMessage(System.SR.net_PropertyNotImplementedException);
		}
		set
		{
			throw System.NotImplemented.ByDesignWithMessage(System.SR.net_PropertyNotImplementedException);
		}
	}

	public virtual Uri RequestUri
	{
		get
		{
			throw System.NotImplemented.ByDesignWithMessage(System.SR.net_PropertyNotImplementedException);
		}
	}

	public virtual WebHeaderCollection Headers
	{
		get
		{
			throw System.NotImplemented.ByDesignWithMessage(System.SR.net_PropertyNotImplementedException);
		}
		set
		{
			throw System.NotImplemented.ByDesignWithMessage(System.SR.net_PropertyNotImplementedException);
		}
	}

	public virtual long ContentLength
	{
		get
		{
			throw System.NotImplemented.ByDesignWithMessage(System.SR.net_PropertyNotImplementedException);
		}
		set
		{
			throw System.NotImplemented.ByDesignWithMessage(System.SR.net_PropertyNotImplementedException);
		}
	}

	public virtual string? ContentType
	{
		get
		{
			throw System.NotImplemented.ByDesignWithMessage(System.SR.net_PropertyNotImplementedException);
		}
		set
		{
			throw System.NotImplemented.ByDesignWithMessage(System.SR.net_PropertyNotImplementedException);
		}
	}

	public virtual ICredentials? Credentials
	{
		get
		{
			throw System.NotImplemented.ByDesignWithMessage(System.SR.net_PropertyNotImplementedException);
		}
		[param: DisallowNull]
		set
		{
			throw System.NotImplemented.ByDesignWithMessage(System.SR.net_PropertyNotImplementedException);
		}
	}

	public virtual int Timeout
	{
		get
		{
			throw System.NotImplemented.ByDesignWithMessage(System.SR.net_PropertyNotImplementedException);
		}
		set
		{
			throw System.NotImplemented.ByDesignWithMessage(System.SR.net_PropertyNotImplementedException);
		}
	}

	public virtual bool UseDefaultCredentials
	{
		get
		{
			throw System.NotImplemented.ByDesignWithMessage(System.SR.net_PropertyNotImplementedException);
		}
		set
		{
			throw System.NotImplemented.ByDesignWithMessage(System.SR.net_PropertyNotImplementedException);
		}
	}

	public static IWebProxy? DefaultWebProxy
	{
		get
		{
			return LazyInitializer.EnsureInitialized(ref s_DefaultWebProxy, ref s_DefaultWebProxyInitialized, ref s_internalSyncObject, () => GetSystemWebProxy());
		}
		set
		{
			lock (s_internalSyncObject)
			{
				s_DefaultWebProxy = value;
				s_DefaultWebProxyInitialized = true;
			}
		}
	}

	public virtual bool PreAuthenticate
	{
		get
		{
			throw System.NotImplemented.ByDesignWithMessage(System.SR.net_PropertyNotImplementedException);
		}
		set
		{
			throw System.NotImplemented.ByDesignWithMessage(System.SR.net_PropertyNotImplementedException);
		}
	}

	public virtual IWebProxy? Proxy
	{
		get
		{
			throw System.NotImplemented.ByDesignWithMessage(System.SR.net_PropertyNotImplementedException);
		}
		set
		{
			throw System.NotImplemented.ByDesignWithMessage(System.SR.net_PropertyNotImplementedException);
		}
	}

	[Obsolete("WebRequest, HttpWebRequest, ServicePoint, and WebClient are obsolete. Use HttpClient instead.", DiagnosticId = "SYSLIB0014", UrlFormat = "https://aka.ms/dotnet-warnings/{0}")]
	protected WebRequest()
	{
	}

	[Obsolete("WebRequest, HttpWebRequest, ServicePoint, and WebClient are obsolete. Use HttpClient instead.", DiagnosticId = "SYSLIB0014", UrlFormat = "https://aka.ms/dotnet-warnings/{0}")]
	protected WebRequest(SerializationInfo serializationInfo, StreamingContext streamingContext)
	{
		throw new PlatformNotSupportedException();
	}

	void ISerializable.GetObjectData(SerializationInfo serializationInfo, StreamingContext streamingContext)
	{
		throw new PlatformNotSupportedException();
	}

	protected virtual void GetObjectData(SerializationInfo serializationInfo, StreamingContext streamingContext)
	{
		throw new PlatformNotSupportedException();
	}

	private static WebRequest Create(Uri requestUri, bool useUriBase)
	{
		WebRequestPrefixElement webRequestPrefixElement = null;
		bool flag = false;
		string text = (useUriBase ? (requestUri.Scheme + ":") : requestUri.AbsoluteUri);
		int length = text.Length;
		List<WebRequestPrefixElement> prefixList = PrefixList;
		for (int i = 0; i < prefixList.Count; i++)
		{
			webRequestPrefixElement = prefixList[i];
			if (length >= webRequestPrefixElement.Prefix.Length && string.Compare(webRequestPrefixElement.Prefix, 0, text, 0, webRequestPrefixElement.Prefix.Length, StringComparison.OrdinalIgnoreCase) == 0)
			{
				flag = true;
				break;
			}
		}
		if (flag)
		{
			return webRequestPrefixElement.Creator.Create(requestUri);
		}
		throw new NotSupportedException(System.SR.net_unknown_prefix);
	}

	[Obsolete("WebRequest, HttpWebRequest, ServicePoint, and WebClient are obsolete. Use HttpClient instead.", DiagnosticId = "SYSLIB0014", UrlFormat = "https://aka.ms/dotnet-warnings/{0}")]
	public static WebRequest Create(string requestUriString)
	{
		if (requestUriString == null)
		{
			throw new ArgumentNullException("requestUriString");
		}
		return Create(new Uri(requestUriString), useUriBase: false);
	}

	[Obsolete("WebRequest, HttpWebRequest, ServicePoint, and WebClient are obsolete. Use HttpClient instead.", DiagnosticId = "SYSLIB0014", UrlFormat = "https://aka.ms/dotnet-warnings/{0}")]
	public static WebRequest Create(Uri requestUri)
	{
		if (requestUri == null)
		{
			throw new ArgumentNullException("requestUri");
		}
		return Create(requestUri, useUriBase: false);
	}

	[Obsolete("WebRequest, HttpWebRequest, ServicePoint, and WebClient are obsolete. Use HttpClient instead.", DiagnosticId = "SYSLIB0014", UrlFormat = "https://aka.ms/dotnet-warnings/{0}")]
	public static WebRequest CreateDefault(Uri requestUri)
	{
		if (requestUri == null)
		{
			throw new ArgumentNullException("requestUri");
		}
		return Create(requestUri, useUriBase: true);
	}

	[Obsolete("WebRequest, HttpWebRequest, ServicePoint, and WebClient are obsolete. Use HttpClient instead.", DiagnosticId = "SYSLIB0014", UrlFormat = "https://aka.ms/dotnet-warnings/{0}")]
	public static HttpWebRequest CreateHttp(string requestUriString)
	{
		if (requestUriString == null)
		{
			throw new ArgumentNullException("requestUriString");
		}
		return CreateHttp(new Uri(requestUriString));
	}

	[Obsolete("WebRequest, HttpWebRequest, ServicePoint, and WebClient are obsolete. Use HttpClient instead.", DiagnosticId = "SYSLIB0014", UrlFormat = "https://aka.ms/dotnet-warnings/{0}")]
	public static HttpWebRequest CreateHttp(Uri requestUri)
	{
		if (requestUri == null)
		{
			throw new ArgumentNullException("requestUri");
		}
		if (requestUri.Scheme != "http" && requestUri.Scheme != "https")
		{
			throw new NotSupportedException(System.SR.net_unknown_prefix);
		}
		return (HttpWebRequest)CreateDefault(requestUri);
	}

	public static bool RegisterPrefix(string prefix, IWebRequestCreate creator)
	{
		bool flag = false;
		if (prefix == null)
		{
			throw new ArgumentNullException("prefix");
		}
		if (creator == null)
		{
			throw new ArgumentNullException("creator");
		}
		lock (s_internalSyncObject)
		{
			List<WebRequestPrefixElement> list = new List<WebRequestPrefixElement>(PrefixList);
			if (Uri.TryCreate(prefix, UriKind.Absolute, out Uri result))
			{
				string text = result.AbsoluteUri;
				if (!prefix.EndsWith('/') && result.GetComponents(UriComponents.PathAndQuery | UriComponents.Fragment, UriFormat.UriEscaped).Equals("/"))
				{
					text = text.Substring(0, text.Length - 1);
				}
				prefix = text;
			}
			int i;
			for (i = 0; i < list.Count; i++)
			{
				WebRequestPrefixElement webRequestPrefixElement = list[i];
				if (prefix.Length > webRequestPrefixElement.Prefix.Length)
				{
					break;
				}
				if (prefix.Length == webRequestPrefixElement.Prefix.Length && string.Equals(webRequestPrefixElement.Prefix, prefix, StringComparison.OrdinalIgnoreCase))
				{
					flag = true;
					break;
				}
			}
			if (!flag)
			{
				list.Insert(i, new WebRequestPrefixElement(prefix, creator));
				PrefixList = list;
			}
		}
		return !flag;
	}

	public virtual Stream GetRequestStream()
	{
		throw System.NotImplemented.ByDesignWithMessage(System.SR.net_MethodNotImplementedException);
	}

	public virtual WebResponse GetResponse()
	{
		throw System.NotImplemented.ByDesignWithMessage(System.SR.net_MethodNotImplementedException);
	}

	public virtual IAsyncResult BeginGetResponse(AsyncCallback? callback, object? state)
	{
		throw System.NotImplemented.ByDesignWithMessage(System.SR.net_MethodNotImplementedException);
	}

	public virtual WebResponse EndGetResponse(IAsyncResult asyncResult)
	{
		throw System.NotImplemented.ByDesignWithMessage(System.SR.net_MethodNotImplementedException);
	}

	public virtual IAsyncResult BeginGetRequestStream(AsyncCallback? callback, object? state)
	{
		throw System.NotImplemented.ByDesignWithMessage(System.SR.net_MethodNotImplementedException);
	}

	public virtual Stream EndGetRequestStream(IAsyncResult asyncResult)
	{
		throw System.NotImplemented.ByDesignWithMessage(System.SR.net_MethodNotImplementedException);
	}

	public virtual Task<Stream> GetRequestStreamAsync()
	{
		return Task.Run(() => Task<Stream>.Factory.FromAsync((AsyncCallback callback, object state) => ((WebRequest)state).BeginGetRequestStream(callback, state), (IAsyncResult iar) => ((WebRequest)iar.AsyncState).EndGetRequestStream(iar), this));
	}

	public virtual Task<WebResponse> GetResponseAsync()
	{
		return Task.Run(() => Task<WebResponse>.Factory.FromAsync((AsyncCallback callback, object state) => ((WebRequest)state).BeginGetResponse(callback, state), (IAsyncResult iar) => ((WebRequest)iar.AsyncState).EndGetResponse(iar), this));
	}

	public virtual void Abort()
	{
		throw System.NotImplemented.ByDesignWithMessage(System.SR.net_MethodNotImplementedException);
	}

	public static IWebProxy GetSystemWebProxy()
	{
		return HttpClient.DefaultProxy;
	}
}
