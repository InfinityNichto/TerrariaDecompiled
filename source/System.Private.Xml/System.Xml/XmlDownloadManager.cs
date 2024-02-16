using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace System.Xml;

internal sealed class XmlDownloadManager
{
	internal Stream GetStream(Uri uri, ICredentials credentials, IWebProxy proxy)
	{
		if (uri.Scheme == "file")
		{
			return new FileStream(uri.LocalPath, FileMode.Open, FileAccess.Read, FileShare.Read, 1);
		}
		return GetNonFileStreamAsync(uri, credentials, proxy).GetAwaiter().GetResult();
	}

	internal Task<Stream> GetStreamAsync(Uri uri, ICredentials credentials, IWebProxy proxy)
	{
		if (uri.Scheme == "file")
		{
			Uri fileUri = uri;
			return Task.Run((Func<Stream>)(() => new FileStream(fileUri.LocalPath, FileMode.Open, FileAccess.Read, FileShare.Read, 1, useAsync: true)));
		}
		return GetNonFileStreamAsync(uri, credentials, proxy);
	}

	private async Task<Stream> GetNonFileStreamAsync(Uri uri, ICredentials credentials, IWebProxy proxy)
	{
		HttpClientHandler httpClientHandler = new HttpClientHandler();
		using HttpClient client = new HttpClient(httpClientHandler);
		if (credentials != null)
		{
			httpClientHandler.Credentials = credentials;
		}
		if (proxy != null)
		{
			httpClientHandler.Proxy = proxy;
		}
		using Stream stream = await client.GetStreamAsync(uri).ConfigureAwait(continueOnCapturedContext: false);
		MemoryStream memoryStream = new MemoryStream();
		stream.CopyTo(memoryStream);
		memoryStream.Position = 0L;
		return memoryStream;
	}
}
