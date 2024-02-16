using System.Globalization;
using System.IO;
using System.Runtime.Serialization;

namespace System.Net;

public class FileWebResponse : WebResponse, ISerializable
{
	private readonly long _contentLength;

	private readonly FileAccess _fileAccess;

	private readonly WebHeaderCollection _headers;

	private readonly Uri _uri;

	private Stream _stream;

	private bool _closed;

	public override long ContentLength
	{
		get
		{
			CheckDisposed();
			return _contentLength;
		}
	}

	public override string ContentType
	{
		get
		{
			CheckDisposed();
			return "application/octet-stream";
		}
	}

	public override WebHeaderCollection Headers
	{
		get
		{
			CheckDisposed();
			return _headers;
		}
	}

	public override bool SupportsHeaders => true;

	public override Uri ResponseUri
	{
		get
		{
			CheckDisposed();
			return _uri;
		}
	}

	internal FileWebResponse(FileWebRequest request, Uri uri, FileAccess access, bool useAsync)
	{
		try
		{
			_fileAccess = access;
			if (access == FileAccess.Write)
			{
				_stream = Stream.Null;
			}
			else
			{
				_stream = new WebFileStream(request, uri.LocalPath, FileMode.Open, FileAccess.Read, FileShare.Read, 8192, useAsync);
				_contentLength = _stream.Length;
			}
			_headers = new WebHeaderCollection();
			_headers["Content-Length"] = _contentLength.ToString(NumberFormatInfo.InvariantInfo);
			_headers["Content-Type"] = "application/octet-stream";
			_uri = uri;
		}
		catch (Exception ex)
		{
			throw new WebException(ex.Message, ex, WebExceptionStatus.ConnectFailure, null);
		}
	}

	[Obsolete("Serialization has been deprecated for FileWebResponse.")]
	protected FileWebResponse(SerializationInfo serializationInfo, StreamingContext streamingContext)
		: base(serializationInfo, streamingContext)
	{
		throw new PlatformNotSupportedException();
	}

	void ISerializable.GetObjectData(SerializationInfo serializationInfo, StreamingContext streamingContext)
	{
		throw new PlatformNotSupportedException();
	}

	protected override void GetObjectData(SerializationInfo serializationInfo, StreamingContext streamingContext)
	{
		throw new PlatformNotSupportedException();
	}

	private void CheckDisposed()
	{
		if (_closed)
		{
			throw new ObjectDisposedException(GetType().FullName);
		}
	}

	public override void Close()
	{
		if (!_closed)
		{
			_closed = true;
			Stream stream = _stream;
			if (stream != null)
			{
				stream.Close();
				_stream = null;
			}
		}
	}

	public override Stream GetResponseStream()
	{
		CheckDisposed();
		return _stream;
	}
}
