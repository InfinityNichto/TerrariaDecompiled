using System.Collections.Specialized;
using System.IO;
using System.Text;

namespace System.Net.Mime;

internal sealed class MimeWriter : BaseWriter
{
	private static readonly byte[] s_DASHDASH = new byte[2] { 45, 45 };

	private readonly byte[] _boundaryBytes;

	private bool _writeBoundary = true;

	internal MimeWriter(Stream stream, string boundary)
		: base(stream, shouldEncodeLeadingDots: false)
	{
		if (boundary == null)
		{
			throw new ArgumentNullException("boundary");
		}
		_boundaryBytes = Encoding.ASCII.GetBytes(boundary);
	}

	internal override void WriteHeaders(NameValueCollection headers, bool allowUnicode)
	{
		if (headers == null)
		{
			throw new ArgumentNullException("headers");
		}
		foreach (string header in headers)
		{
			WriteHeader(header, headers[header], allowUnicode);
		}
	}

	internal IAsyncResult BeginClose(AsyncCallback callback, object state)
	{
		MultiAsyncResult multiAsyncResult = new MultiAsyncResult(this, callback, state);
		Close(multiAsyncResult);
		multiAsyncResult.CompleteSequence();
		return multiAsyncResult;
	}

	internal void EndClose(IAsyncResult result)
	{
		MultiAsyncResult.End(result);
		_stream.Close();
	}

	internal override void Close()
	{
		Close(null);
		_stream.Close();
	}

	private void Close(MultiAsyncResult multiResult)
	{
		_bufferBuilder.Append(BaseWriter.s_crlf);
		_bufferBuilder.Append(s_DASHDASH);
		_bufferBuilder.Append(_boundaryBytes);
		_bufferBuilder.Append(s_DASHDASH);
		_bufferBuilder.Append(BaseWriter.s_crlf);
		Flush(multiResult);
	}

	protected override void OnClose(object sender, EventArgs args)
	{
		if (_contentStream == sender)
		{
			_contentStream.Flush();
			_contentStream = null;
			_writeBoundary = true;
			_isInContent = false;
		}
	}

	protected override void CheckBoundary()
	{
		if (_writeBoundary)
		{
			_bufferBuilder.Append(BaseWriter.s_crlf);
			_bufferBuilder.Append(s_DASHDASH);
			_bufferBuilder.Append(_boundaryBytes);
			_bufferBuilder.Append(BaseWriter.s_crlf);
			_writeBoundary = false;
		}
	}
}
