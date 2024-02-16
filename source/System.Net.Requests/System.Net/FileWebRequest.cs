using System.IO;
using System.Runtime.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace System.Net;

public class FileWebRequest : WebRequest, ISerializable
{
	private readonly WebHeaderCollection _headers = new WebHeaderCollection();

	private string _method = "GET";

	private FileAccess _fileAccess = FileAccess.Read;

	private ManualResetEventSlim _blockReaderUntilRequestStreamDisposed;

	private WebResponse _response;

	private WebFileStream _stream;

	private readonly Uri _uri;

	private long _contentLength;

	private int _timeout = 100000;

	private bool _readPending;

	private bool _writePending;

	private bool _writing;

	private bool _syncHint;

	private int _aborted;

	internal bool Aborted => _aborted != 0;

	public override string? ConnectionGroupName { get; set; }

	public override long ContentLength
	{
		get
		{
			return _contentLength;
		}
		set
		{
			if (value < 0)
			{
				throw new ArgumentException(System.SR.net_clsmall, "value");
			}
			_contentLength = value;
		}
	}

	public override string? ContentType
	{
		get
		{
			return _headers["Content-Type"];
		}
		set
		{
			_headers["Content-Type"] = value;
		}
	}

	public override ICredentials? Credentials { get; set; }

	public override WebHeaderCollection Headers => _headers;

	public override string Method
	{
		get
		{
			return _method;
		}
		set
		{
			if (string.IsNullOrEmpty(value))
			{
				throw new ArgumentException(System.SR.net_badmethod, "value");
			}
			_method = value;
		}
	}

	public override bool PreAuthenticate { get; set; }

	public override IWebProxy? Proxy { get; set; }

	public override int Timeout
	{
		get
		{
			return _timeout;
		}
		set
		{
			if (value < 0 && value != -1)
			{
				throw new ArgumentOutOfRangeException("value", System.SR.net_io_timeout_use_ge_zero);
			}
			_timeout = value;
		}
	}

	public override Uri RequestUri => _uri;

	public override bool UseDefaultCredentials
	{
		get
		{
			throw new NotSupportedException(System.SR.net_PropertyNotSupportedException);
		}
		set
		{
			throw new NotSupportedException(System.SR.net_PropertyNotSupportedException);
		}
	}

	internal FileWebRequest(Uri uri)
	{
		if ((object)uri.Scheme != Uri.UriSchemeFile)
		{
			throw new ArgumentOutOfRangeException("uri");
		}
		_uri = uri;
	}

	[Obsolete("Serialization has been deprecated for FileWebRequest.")]
	protected FileWebRequest(SerializationInfo serializationInfo, StreamingContext streamingContext)
		: base(serializationInfo, streamingContext)
	{
		throw new PlatformNotSupportedException();
	}

	void ISerializable.GetObjectData(SerializationInfo serializationInfo, StreamingContext streamingContext)
	{
		GetObjectData(serializationInfo, streamingContext);
	}

	protected override void GetObjectData(SerializationInfo serializationInfo, StreamingContext streamingContext)
	{
		throw new PlatformNotSupportedException();
	}

	private static Exception CreateRequestAbortedException()
	{
		return new WebException(System.SR.Format(System.SR.net_requestaborted, WebExceptionStatus.RequestCanceled), WebExceptionStatus.RequestCanceled);
	}

	private void CheckAndMarkAsyncGetRequestStreamPending()
	{
		if (Aborted)
		{
			throw CreateRequestAbortedException();
		}
		if (string.Equals(_method, "GET", StringComparison.OrdinalIgnoreCase) || string.Equals(_method, "HEAD", StringComparison.OrdinalIgnoreCase))
		{
			throw new ProtocolViolationException(System.SR.net_nouploadonget);
		}
		if (_response != null)
		{
			throw new InvalidOperationException(System.SR.net_reqsubmitted);
		}
		lock (this)
		{
			if (_writePending)
			{
				throw new InvalidOperationException(System.SR.net_repcall);
			}
			_writePending = true;
		}
	}

	private Stream CreateWriteStream()
	{
		try
		{
			if (_stream == null)
			{
				_stream = new WebFileStream(this, _uri.LocalPath, FileMode.Create, FileAccess.Write, FileShare.Read);
				_fileAccess = FileAccess.Write;
				_writing = true;
			}
			return _stream;
		}
		catch (Exception ex)
		{
			throw new WebException(ex.Message, ex);
		}
	}

	public override IAsyncResult BeginGetRequestStream(AsyncCallback? callback, object? state)
	{
		CheckAndMarkAsyncGetRequestStreamPending();
		Task<Stream> task = Task.Factory.StartNew((object s) => ((FileWebRequest)s).CreateWriteStream(), this, CancellationToken.None, TaskCreationOptions.DenyChildAttach, TaskScheduler.Default);
		return System.Threading.Tasks.TaskToApm.Begin(task, callback, state);
	}

	public override Task<Stream> GetRequestStreamAsync()
	{
		CheckAndMarkAsyncGetRequestStreamPending();
		return Task.Factory.StartNew(delegate(object s)
		{
			FileWebRequest fileWebRequest = (FileWebRequest)s;
			Stream result = fileWebRequest.CreateWriteStream();
			fileWebRequest._writePending = false;
			return result;
		}, this, CancellationToken.None, TaskCreationOptions.DenyChildAttach, TaskScheduler.Default);
	}

	private void CheckAndMarkAsyncGetResponsePending()
	{
		if (Aborted)
		{
			throw CreateRequestAbortedException();
		}
		lock (this)
		{
			if (_readPending)
			{
				throw new InvalidOperationException(System.SR.net_repcall);
			}
			_readPending = true;
		}
	}

	private WebResponse CreateResponse()
	{
		if (_writePending || _writing)
		{
			lock (this)
			{
				if (_writePending || _writing)
				{
					_blockReaderUntilRequestStreamDisposed = new ManualResetEventSlim();
				}
			}
		}
		_blockReaderUntilRequestStreamDisposed?.Wait();
		try
		{
			return _response ?? (_response = new FileWebResponse(this, _uri, _fileAccess, !_syncHint));
		}
		catch (Exception ex)
		{
			throw new WebException(ex.Message, ex);
		}
	}

	public override IAsyncResult BeginGetResponse(AsyncCallback? callback, object? state)
	{
		CheckAndMarkAsyncGetResponsePending();
		Task<WebResponse> task = Task.Factory.StartNew((object s) => ((FileWebRequest)s).CreateResponse(), this, CancellationToken.None, TaskCreationOptions.DenyChildAttach, TaskScheduler.Default);
		return System.Threading.Tasks.TaskToApm.Begin(task, callback, state);
	}

	public override Task<WebResponse> GetResponseAsync()
	{
		CheckAndMarkAsyncGetResponsePending();
		return Task.Factory.StartNew(delegate(object s)
		{
			FileWebRequest fileWebRequest = (FileWebRequest)s;
			WebResponse result = fileWebRequest.CreateResponse();
			_readPending = false;
			return result;
		}, this, CancellationToken.None, TaskCreationOptions.DenyChildAttach, TaskScheduler.Default);
	}

	public override Stream EndGetRequestStream(IAsyncResult asyncResult)
	{
		Stream result = System.Threading.Tasks.TaskToApm.End<Stream>(asyncResult);
		_writePending = false;
		return result;
	}

	public override WebResponse EndGetResponse(IAsyncResult asyncResult)
	{
		WebResponse result = System.Threading.Tasks.TaskToApm.End<WebResponse>(asyncResult);
		_readPending = false;
		return result;
	}

	public override Stream GetRequestStream()
	{
		IAsyncResult asyncResult = BeginGetRequestStream(null, null);
		if (Timeout != -1 && !asyncResult.IsCompleted && (!asyncResult.AsyncWaitHandle.WaitOne(Timeout, exitContext: false) || !asyncResult.IsCompleted))
		{
			_stream?.Close();
			throw new WebException(System.SR.net_webstatus_Timeout, WebExceptionStatus.Timeout);
		}
		return EndGetRequestStream(asyncResult);
	}

	public override WebResponse GetResponse()
	{
		_syncHint = true;
		IAsyncResult asyncResult = BeginGetResponse(null, null);
		if (Timeout != -1 && !asyncResult.IsCompleted && (!asyncResult.AsyncWaitHandle.WaitOne(Timeout, exitContext: false) || !asyncResult.IsCompleted))
		{
			_response?.Close();
			throw new WebException(System.SR.net_webstatus_Timeout, WebExceptionStatus.Timeout);
		}
		return EndGetResponse(asyncResult);
	}

	internal void UnblockReader()
	{
		lock (this)
		{
			_blockReaderUntilRequestStreamDisposed?.Set();
		}
		_writing = false;
	}

	public override void Abort()
	{
		if (Interlocked.Increment(ref _aborted) == 1)
		{
			_stream?.Abort();
			_response?.Close();
		}
	}
}
