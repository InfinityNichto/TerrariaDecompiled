using System.IO;
using System.Net;
using System.Net.Cache;
using System.Runtime.Versioning;
using System.Threading;
using System.Threading.Tasks;

namespace System.Xml;

public class XmlUrlResolver : XmlResolver
{
	private static XmlDownloadManager s_downloadManager;

	private ICredentials _credentials;

	private IWebProxy _proxy;

	private static XmlDownloadManager DownloadManager => s_downloadManager ?? Interlocked.CompareExchange(ref s_downloadManager, new XmlDownloadManager(), null) ?? s_downloadManager;

	[UnsupportedOSPlatform("browser")]
	public override ICredentials? Credentials
	{
		set
		{
			_credentials = value;
		}
	}

	[UnsupportedOSPlatform("browser")]
	public IWebProxy? Proxy
	{
		set
		{
			_proxy = value;
		}
	}

	public RequestCachePolicy CachePolicy
	{
		set
		{
		}
	}

	public override object? GetEntity(Uri absoluteUri, string? role, Type? ofObjectToReturn)
	{
		if ((object)ofObjectToReturn == null || ofObjectToReturn == typeof(Stream) || ofObjectToReturn == typeof(object))
		{
			return DownloadManager.GetStream(absoluteUri, _credentials, _proxy);
		}
		throw new XmlException(System.SR.Xml_UnsupportedClass, string.Empty);
	}

	public override Uri ResolveUri(Uri? baseUri, string? relativeUri)
	{
		return base.ResolveUri(baseUri, relativeUri);
	}

	public override async Task<object> GetEntityAsync(Uri absoluteUri, string? role, Type? ofObjectToReturn)
	{
		if (ofObjectToReturn == null || ofObjectToReturn == typeof(Stream) || ofObjectToReturn == typeof(object))
		{
			return await DownloadManager.GetStreamAsync(absoluteUri, _credentials, _proxy).ConfigureAwait(continueOnCapturedContext: false);
		}
		throw new XmlException(System.SR.Xml_UnsupportedClass, string.Empty);
	}
}
