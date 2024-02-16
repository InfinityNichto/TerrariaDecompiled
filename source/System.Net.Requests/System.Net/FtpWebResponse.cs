using System.IO;

namespace System.Net;

public class FtpWebResponse : WebResponse, IDisposable
{
	internal sealed class EmptyStream : MemoryStream
	{
		internal EmptyStream()
			: base(Array.Empty<byte>(), writable: false)
		{
		}
	}

	internal Stream _responseStream;

	private readonly long _contentLength;

	private readonly Uri _responseUri;

	private FtpStatusCode _statusCode;

	private string _statusLine;

	private WebHeaderCollection _ftpRequestHeaders;

	private readonly DateTime _lastModified;

	private readonly string _bannerMessage;

	private readonly string _welcomeMessage;

	private string _exitMessage;

	public override long ContentLength => _contentLength;

	public override WebHeaderCollection Headers
	{
		get
		{
			if (_ftpRequestHeaders == null)
			{
				lock (this)
				{
					if (_ftpRequestHeaders == null)
					{
						_ftpRequestHeaders = new WebHeaderCollection();
					}
				}
			}
			return _ftpRequestHeaders;
		}
	}

	public override bool SupportsHeaders => true;

	public override Uri ResponseUri => _responseUri;

	public FtpStatusCode StatusCode => _statusCode;

	public string? StatusDescription => _statusLine;

	public DateTime LastModified => _lastModified;

	public string? BannerMessage => _bannerMessage;

	public string? WelcomeMessage => _welcomeMessage;

	public string? ExitMessage => _exitMessage;

	internal FtpWebResponse(Stream responseStream, long contentLength, Uri responseUri, FtpStatusCode statusCode, string statusLine, DateTime lastModified, string bannerMessage, string welcomeMessage, string exitMessage)
	{
		_responseStream = responseStream;
		if (responseStream == null && contentLength < 0)
		{
			contentLength = 0L;
		}
		_contentLength = contentLength;
		_responseUri = responseUri;
		_statusCode = statusCode;
		_statusLine = statusLine;
		_lastModified = lastModified;
		_bannerMessage = bannerMessage;
		_welcomeMessage = welcomeMessage;
		_exitMessage = exitMessage;
	}

	internal void UpdateStatus(FtpStatusCode statusCode, string statusLine, string exitMessage)
	{
		_statusCode = statusCode;
		_statusLine = statusLine;
		_exitMessage = exitMessage;
	}

	public override Stream GetResponseStream()
	{
		Stream stream = null;
		if (_responseStream != null)
		{
			return _responseStream;
		}
		return _responseStream = new EmptyStream();
	}

	internal void SetResponseStream(Stream stream)
	{
		if (stream != null && stream != Stream.Null && !(stream is EmptyStream))
		{
			_responseStream = stream;
		}
	}

	public override void Close()
	{
		_responseStream?.Close();
	}
}
