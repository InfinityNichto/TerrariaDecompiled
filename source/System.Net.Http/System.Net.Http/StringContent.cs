using System.IO;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace System.Net.Http;

public class StringContent : ByteArrayContent
{
	public StringContent(string content)
		: this(content, null, null)
	{
	}

	public StringContent(string content, Encoding? encoding)
		: this(content, encoding, null)
	{
	}

	public StringContent(string content, Encoding? encoding, string? mediaType)
		: base(GetContentByteArray(content, encoding))
	{
		MediaTypeHeaderValue contentType = new MediaTypeHeaderValue((mediaType == null) ? "text/plain" : mediaType)
		{
			CharSet = ((encoding == null) ? HttpContent.DefaultStringEncoding.WebName : encoding.WebName)
		};
		base.Headers.ContentType = contentType;
	}

	private static byte[] GetContentByteArray(string content, Encoding encoding)
	{
		if (content == null)
		{
			throw new ArgumentNullException("content");
		}
		if (encoding == null)
		{
			encoding = HttpContent.DefaultStringEncoding;
		}
		return encoding.GetBytes(content);
	}

	protected override Task SerializeToStreamAsync(Stream stream, TransportContext? context, CancellationToken cancellationToken)
	{
		if (!(GetType() == typeof(StringContent)))
		{
			return base.SerializeToStreamAsync(stream, context, cancellationToken);
		}
		return SerializeToStreamAsyncCore(stream, cancellationToken);
	}

	internal override Stream TryCreateContentReadStream()
	{
		if (!(GetType() == typeof(StringContent)))
		{
			return null;
		}
		return CreateMemoryStreamForByteArray();
	}
}
