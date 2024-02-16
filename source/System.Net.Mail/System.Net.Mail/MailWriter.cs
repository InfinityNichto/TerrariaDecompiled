using System.Collections.Specialized;
using System.IO;
using System.Net.Mime;

namespace System.Net.Mail;

internal sealed class MailWriter : BaseWriter
{
	internal MailWriter(Stream stream, bool encodeForTransport)
		: base(stream, encodeForTransport)
	{
	}

	internal override void WriteHeaders(NameValueCollection headers, bool allowUnicode)
	{
		if (headers == null)
		{
			throw new ArgumentNullException("headers");
		}
		foreach (string header in headers)
		{
			string[] values = headers.GetValues(header);
			string[] array = values;
			foreach (string value in array)
			{
				WriteHeader(header, value, allowUnicode);
			}
		}
	}

	internal override void Close()
	{
		_bufferBuilder.Append(BaseWriter.s_crlf);
		Flush(null);
		_stream.Close();
	}

	protected override void OnClose(object sender, EventArgs args)
	{
		_contentStream.Flush();
		_contentStream = null;
	}
}
