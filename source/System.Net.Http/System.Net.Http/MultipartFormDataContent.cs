using System.IO;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;

namespace System.Net.Http;

public class MultipartFormDataContent : MultipartContent
{
	public MultipartFormDataContent()
		: base("form-data")
	{
	}

	public MultipartFormDataContent(string boundary)
		: base("form-data", boundary)
	{
	}

	public override void Add(HttpContent content)
	{
		if (content == null)
		{
			throw new ArgumentNullException("content");
		}
		if (content.Headers.ContentDisposition == null)
		{
			content.Headers.ContentDisposition = new ContentDispositionHeaderValue("form-data");
		}
		base.Add(content);
	}

	public void Add(HttpContent content, string name)
	{
		if (content == null)
		{
			throw new ArgumentNullException("content");
		}
		if (string.IsNullOrWhiteSpace(name))
		{
			throw new ArgumentException(System.SR.net_http_argument_empty_string, "name");
		}
		AddInternal(content, name, null);
	}

	public void Add(HttpContent content, string name, string fileName)
	{
		if (content == null)
		{
			throw new ArgumentNullException("content");
		}
		if (string.IsNullOrWhiteSpace(name))
		{
			throw new ArgumentException(System.SR.net_http_argument_empty_string, "name");
		}
		if (string.IsNullOrWhiteSpace(fileName))
		{
			throw new ArgumentException(System.SR.net_http_argument_empty_string, "fileName");
		}
		AddInternal(content, name, fileName);
	}

	private void AddInternal(HttpContent content, string name, string fileName)
	{
		if (content.Headers.ContentDisposition == null)
		{
			ContentDispositionHeaderValue contentDispositionHeaderValue = new ContentDispositionHeaderValue("form-data");
			contentDispositionHeaderValue.Name = name;
			contentDispositionHeaderValue.FileName = fileName;
			contentDispositionHeaderValue.FileNameStar = fileName;
			content.Headers.ContentDisposition = contentDispositionHeaderValue;
		}
		base.Add(content);
	}

	protected override Task SerializeToStreamAsync(Stream stream, TransportContext? context, CancellationToken cancellationToken)
	{
		if (!(GetType() == typeof(MultipartFormDataContent)))
		{
			return base.SerializeToStreamAsync(stream, context, cancellationToken);
		}
		return SerializeToStreamAsyncCore(stream, context, cancellationToken);
	}
}
