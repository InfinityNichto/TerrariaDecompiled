using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Net.Cache;
using System.Net.Sockets;
using System.Runtime.ExceptionServices;
using System.Security;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Threading;

namespace System.Net;

public sealed class FtpWebRequest : WebRequest
{
	private enum RequestStage
	{
		CheckForError,
		RequestStarted,
		WriteReady,
		ReadReady,
		ReleaseConnection
	}

	private object _syncObject;

	private ICredentials _authInfo;

	private readonly Uri _uri;

	private FtpMethodInfo _methodInfo;

	private string _renameTo;

	private bool _getRequestStreamStarted;

	private bool _getResponseStarted;

	private DateTime _startTime;

	private int _timeout = 100000;

	private int _remainingTimeout;

	private long _contentLength;

	private long _contentOffset;

	private X509CertificateCollection _clientCertificates;

	private bool _passive = true;

	private bool _binary = true;

	private string _connectionGroupName;

	private ServicePoint _servicePoint;

	private bool _async;

	private bool _aborted;

	private bool _timedOut;

	private Exception _exception;

	private TimerThread.Queue _timerQueue = s_DefaultTimerQueue;

	private readonly TimerThread.Callback _timerCallback;

	private bool _enableSsl;

	private FtpControlStream _connection;

	private Stream _stream;

	private RequestStage _requestStage;

	private bool _onceFailed;

	private WebHeaderCollection _ftpRequestHeaders;

	private FtpWebResponse _ftpWebResponse;

	private int _readWriteTimeout = 300000;

	private ContextAwareResult _writeAsyncResult;

	private LazyAsyncResult _readAsyncResult;

	private LazyAsyncResult _requestCompleteAsyncResult;

	private static readonly NetworkCredential s_defaultFtpNetworkCredential = new NetworkCredential("anonymous", "anonymous@", string.Empty);

	private static readonly TimerThread.Queue s_DefaultTimerQueue = TimerThread.GetOrCreateQueue(100000);

	internal FtpMethodInfo MethodInfo => _methodInfo;

	public new static RequestCachePolicy? DefaultCachePolicy
	{
		get
		{
			return WebRequest.DefaultCachePolicy;
		}
		set
		{
		}
	}

	public override string Method
	{
		get
		{
			return _methodInfo.Method;
		}
		set
		{
			if (string.IsNullOrEmpty(value))
			{
				throw new ArgumentException(System.SR.net_ftp_invalid_method_name, "value");
			}
			if (InUse)
			{
				throw new InvalidOperationException(System.SR.net_reqsubmitted);
			}
			try
			{
				_methodInfo = FtpMethodInfo.GetMethodInfo(value);
			}
			catch (ArgumentException)
			{
				throw new ArgumentException(System.SR.net_ftp_unsupported_method, "value");
			}
		}
	}

	public string? RenameTo
	{
		get
		{
			return _renameTo;
		}
		[param: DisallowNull]
		set
		{
			if (InUse)
			{
				throw new InvalidOperationException(System.SR.net_reqsubmitted);
			}
			if (string.IsNullOrEmpty(value))
			{
				throw new ArgumentException(System.SR.net_ftp_invalid_renameto, "value");
			}
			_renameTo = value;
		}
	}

	public override ICredentials? Credentials
	{
		get
		{
			return _authInfo;
		}
		[param: DisallowNull]
		set
		{
			if (InUse)
			{
				throw new InvalidOperationException(System.SR.net_reqsubmitted);
			}
			if (value == null)
			{
				throw new ArgumentNullException("value");
			}
			if (value == CredentialCache.DefaultNetworkCredentials)
			{
				throw new ArgumentException(System.SR.net_ftp_no_defaultcreds, "value");
			}
			_authInfo = value;
		}
	}

	public override Uri RequestUri => _uri;

	public override int Timeout
	{
		get
		{
			return _timeout;
		}
		set
		{
			if (InUse)
			{
				throw new InvalidOperationException(System.SR.net_reqsubmitted);
			}
			if (value < 0 && value != -1)
			{
				throw new ArgumentOutOfRangeException("value", System.SR.net_io_timeout_use_ge_zero);
			}
			if (_timeout != value)
			{
				_timeout = value;
				_timerQueue = null;
			}
		}
	}

	internal int RemainingTimeout => _remainingTimeout;

	public int ReadWriteTimeout
	{
		get
		{
			return _readWriteTimeout;
		}
		set
		{
			if (_getResponseStarted)
			{
				throw new InvalidOperationException(System.SR.net_reqsubmitted);
			}
			if (value <= 0 && value != -1)
			{
				throw new ArgumentOutOfRangeException("value", System.SR.net_io_timeout_use_gt_zero);
			}
			_readWriteTimeout = value;
		}
	}

	public long ContentOffset
	{
		get
		{
			return _contentOffset;
		}
		set
		{
			if (InUse)
			{
				throw new InvalidOperationException(System.SR.net_reqsubmitted);
			}
			if (value < 0)
			{
				throw new ArgumentOutOfRangeException("value");
			}
			_contentOffset = value;
		}
	}

	public override long ContentLength
	{
		get
		{
			return _contentLength;
		}
		set
		{
			_contentLength = value;
		}
	}

	public override IWebProxy? Proxy
	{
		get
		{
			return null;
		}
		set
		{
			if (InUse)
			{
				throw new InvalidOperationException(System.SR.net_reqsubmitted);
			}
		}
	}

	public override string? ConnectionGroupName
	{
		get
		{
			return _connectionGroupName;
		}
		set
		{
			if (InUse)
			{
				throw new InvalidOperationException(System.SR.net_reqsubmitted);
			}
			_connectionGroupName = value;
		}
	}

	public ServicePoint ServicePoint => _servicePoint ?? (_servicePoint = ServicePointManager.FindServicePoint(_uri));

	internal bool Aborted => _aborted;

	private TimerThread.Queue TimerQueue
	{
		get
		{
			if (_timerQueue == null)
			{
				_timerQueue = TimerThread.GetOrCreateQueue(RemainingTimeout);
			}
			return _timerQueue;
		}
	}

	public bool KeepAlive
	{
		get
		{
			return true;
		}
		set
		{
			if (InUse)
			{
				throw new InvalidOperationException(System.SR.net_reqsubmitted);
			}
		}
	}

	public override RequestCachePolicy? CachePolicy
	{
		get
		{
			return DefaultCachePolicy;
		}
		set
		{
			if (InUse)
			{
				throw new InvalidOperationException(System.SR.net_reqsubmitted);
			}
		}
	}

	public bool UseBinary
	{
		get
		{
			return _binary;
		}
		set
		{
			if (InUse)
			{
				throw new InvalidOperationException(System.SR.net_reqsubmitted);
			}
			_binary = value;
		}
	}

	public bool UsePassive
	{
		get
		{
			return _passive;
		}
		set
		{
			if (InUse)
			{
				throw new InvalidOperationException(System.SR.net_reqsubmitted);
			}
			_passive = value;
		}
	}

	public X509CertificateCollection ClientCertificates
	{
		get
		{
			return LazyInitializer.EnsureInitialized(ref _clientCertificates, ref _syncObject, () => new X509CertificateCollection());
		}
		set
		{
			if (value == null)
			{
				throw new ArgumentNullException("value");
			}
			_clientCertificates = value;
		}
	}

	public bool EnableSsl
	{
		get
		{
			return _enableSsl;
		}
		set
		{
			if (InUse)
			{
				throw new InvalidOperationException(System.SR.net_reqsubmitted);
			}
			_enableSsl = value;
		}
	}

	public override WebHeaderCollection Headers
	{
		get
		{
			if (_ftpRequestHeaders == null)
			{
				_ftpRequestHeaders = new WebHeaderCollection();
			}
			return _ftpRequestHeaders;
		}
		set
		{
			_ftpRequestHeaders = value;
		}
	}

	public override string? ContentType
	{
		get
		{
			throw ExceptionHelper.PropertyNotSupportedException;
		}
		set
		{
			throw ExceptionHelper.PropertyNotSupportedException;
		}
	}

	public override bool UseDefaultCredentials
	{
		get
		{
			throw ExceptionHelper.PropertyNotSupportedException;
		}
		set
		{
			throw ExceptionHelper.PropertyNotSupportedException;
		}
	}

	public override bool PreAuthenticate
	{
		get
		{
			throw ExceptionHelper.PropertyNotSupportedException;
		}
		set
		{
			throw ExceptionHelper.PropertyNotSupportedException;
		}
	}

	private bool InUse
	{
		get
		{
			if (_getRequestStreamStarted || _getResponseStarted)
			{
				return true;
			}
			return false;
		}
	}

	internal FtpWebRequest(Uri uri)
	{
		if (System.Net.NetEventSource.Log.IsEnabled())
		{
			System.Net.NetEventSource.Info(this, uri, ".ctor");
		}
		if ((object)uri.Scheme != Uri.UriSchemeFtp)
		{
			throw new ArgumentOutOfRangeException("uri");
		}
		_timerCallback = TimerCallback;
		_syncObject = new object();
		NetworkCredential networkCredential = null;
		_uri = uri;
		_methodInfo = FtpMethodInfo.GetMethodInfo("RETR");
		if (_uri.UserInfo != null && _uri.UserInfo.Length != 0)
		{
			string userInfo = _uri.UserInfo;
			string userName = userInfo;
			string password = "";
			int num = userInfo.IndexOf(':');
			if (num != -1)
			{
				userName = Uri.UnescapeDataString(userInfo.Substring(0, num));
				num++;
				password = Uri.UnescapeDataString(userInfo.Substring(num, userInfo.Length - num));
			}
			networkCredential = new NetworkCredential(userName, password);
		}
		if (networkCredential == null)
		{
			networkCredential = s_defaultFtpNetworkCredential;
		}
		_authInfo = networkCredential;
	}

	public override WebResponse GetResponse()
	{
		if (System.Net.NetEventSource.Log.IsEnabled())
		{
			System.Net.NetEventSource.Info(this, $"Method: {_methodInfo.Method}", "GetResponse");
		}
		try
		{
			CheckError();
			if (_ftpWebResponse != null)
			{
				return _ftpWebResponse;
			}
			if (_getResponseStarted)
			{
				throw new InvalidOperationException(System.SR.net_repcall);
			}
			_getResponseStarted = true;
			_startTime = DateTime.UtcNow;
			_remainingTimeout = Timeout;
			if (Timeout != -1)
			{
				_remainingTimeout = Timeout - (int)(DateTime.UtcNow - _startTime).TotalMilliseconds;
				if (_remainingTimeout <= 0)
				{
					throw ExceptionHelper.TimeoutException;
				}
			}
			RequestStage requestStage = FinishRequestStage(RequestStage.RequestStarted);
			if (requestStage >= RequestStage.RequestStarted)
			{
				if (requestStage < RequestStage.ReadReady)
				{
					lock (_syncObject)
					{
						if (_requestStage < RequestStage.ReadReady)
						{
							_readAsyncResult = new LazyAsyncResult(null, null, null);
						}
					}
					if (_readAsyncResult != null)
					{
						_readAsyncResult.InternalWaitForCompletion();
					}
					CheckError();
				}
			}
			else
			{
				SubmitRequest(isAsync: false);
				if (_methodInfo.IsUpload)
				{
					FinishRequestStage(RequestStage.WriteReady);
				}
				else
				{
					FinishRequestStage(RequestStage.ReadReady);
				}
				CheckError();
				EnsureFtpWebResponse(null);
			}
		}
		catch (Exception ex)
		{
			if (System.Net.NetEventSource.Log.IsEnabled())
			{
				System.Net.NetEventSource.Error(this, ex, "GetResponse");
			}
			if (_exception == null)
			{
				if (System.Net.NetEventSource.Log.IsEnabled())
				{
					System.Net.NetEventSource.Error(this, ex, "GetResponse");
				}
				SetException(ex);
				FinishRequestStage(RequestStage.CheckForError);
			}
			throw;
		}
		return _ftpWebResponse;
	}

	public override IAsyncResult BeginGetResponse(AsyncCallback? callback, object? state)
	{
		if (System.Net.NetEventSource.Log.IsEnabled())
		{
			System.Net.NetEventSource.Info(this, $"Method: {_methodInfo.Method}", "BeginGetResponse");
		}
		ContextAwareResult contextAwareResult;
		try
		{
			if (_ftpWebResponse != null)
			{
				contextAwareResult = new ContextAwareResult(this, state, callback);
				contextAwareResult.InvokeCallback(_ftpWebResponse);
				return contextAwareResult;
			}
			if (_getResponseStarted)
			{
				throw new InvalidOperationException(System.SR.net_repcall);
			}
			_getResponseStarted = true;
			CheckError();
			RequestStage requestStage = FinishRequestStage(RequestStage.RequestStarted);
			contextAwareResult = (ContextAwareResult)(_readAsyncResult = new ContextAwareResult(captureIdentity: true, forceCaptureContext: true, this, state, callback));
			if (requestStage >= RequestStage.RequestStarted)
			{
				contextAwareResult.StartPostingAsyncOp();
				contextAwareResult.FinishPostingAsyncOp();
				if (requestStage >= RequestStage.ReadReady)
				{
					contextAwareResult = null;
				}
				else
				{
					lock (_syncObject)
					{
						if (_requestStage >= RequestStage.ReadReady)
						{
							contextAwareResult = null;
						}
					}
				}
				if (contextAwareResult == null)
				{
					contextAwareResult = (ContextAwareResult)_readAsyncResult;
					if (!contextAwareResult.InternalPeekCompleted)
					{
						contextAwareResult.InvokeCallback();
					}
				}
			}
			else
			{
				lock (contextAwareResult.StartPostingAsyncOp())
				{
					SubmitRequest(isAsync: true);
					contextAwareResult.FinishPostingAsyncOp();
				}
				FinishRequestStage(RequestStage.CheckForError);
			}
		}
		catch (Exception message)
		{
			if (System.Net.NetEventSource.Log.IsEnabled())
			{
				System.Net.NetEventSource.Error(this, message, "BeginGetResponse");
			}
			throw;
		}
		return contextAwareResult;
	}

	public override WebResponse EndGetResponse(IAsyncResult asyncResult)
	{
		try
		{
			if (asyncResult == null)
			{
				throw new ArgumentNullException("asyncResult");
			}
			if (!(asyncResult is LazyAsyncResult lazyAsyncResult))
			{
				throw new ArgumentException(System.SR.net_io_invalidasyncresult, "asyncResult");
			}
			if (lazyAsyncResult.EndCalled)
			{
				throw new InvalidOperationException(System.SR.Format(System.SR.net_io_invalidendcall, "EndGetResponse"));
			}
			lazyAsyncResult.InternalWaitForCompletion();
			lazyAsyncResult.EndCalled = true;
			CheckError();
		}
		catch (Exception message)
		{
			if (System.Net.NetEventSource.Log.IsEnabled())
			{
				System.Net.NetEventSource.Error(this, message, "EndGetResponse");
			}
			throw;
		}
		return _ftpWebResponse;
	}

	public override Stream GetRequestStream()
	{
		if (System.Net.NetEventSource.Log.IsEnabled())
		{
			System.Net.NetEventSource.Info(this, $"Method: {_methodInfo.Method}", "GetRequestStream");
		}
		try
		{
			if (_getRequestStreamStarted)
			{
				throw new InvalidOperationException(System.SR.net_repcall);
			}
			_getRequestStreamStarted = true;
			if (!_methodInfo.IsUpload)
			{
				throw new ProtocolViolationException(System.SR.net_nouploadonget);
			}
			CheckError();
			_startTime = DateTime.UtcNow;
			_remainingTimeout = Timeout;
			if (Timeout != -1)
			{
				_remainingTimeout = Timeout - (int)(DateTime.UtcNow - _startTime).TotalMilliseconds;
				if (_remainingTimeout <= 0)
				{
					throw ExceptionHelper.TimeoutException;
				}
			}
			FinishRequestStage(RequestStage.RequestStarted);
			SubmitRequest(isAsync: false);
			FinishRequestStage(RequestStage.WriteReady);
			CheckError();
			if (_stream.CanTimeout)
			{
				_stream.WriteTimeout = ReadWriteTimeout;
				_stream.ReadTimeout = ReadWriteTimeout;
			}
		}
		catch (Exception message)
		{
			if (System.Net.NetEventSource.Log.IsEnabled())
			{
				System.Net.NetEventSource.Error(this, message, "GetRequestStream");
			}
			throw;
		}
		return _stream;
	}

	public override IAsyncResult BeginGetRequestStream(AsyncCallback? callback, object? state)
	{
		if (System.Net.NetEventSource.Log.IsEnabled())
		{
			System.Net.NetEventSource.Info(this, $"Method: {_methodInfo.Method}", "BeginGetRequestStream");
		}
		ContextAwareResult contextAwareResult = null;
		try
		{
			if (_getRequestStreamStarted)
			{
				throw new InvalidOperationException(System.SR.net_repcall);
			}
			_getRequestStreamStarted = true;
			if (!_methodInfo.IsUpload)
			{
				throw new ProtocolViolationException(System.SR.net_nouploadonget);
			}
			CheckError();
			FinishRequestStage(RequestStage.RequestStarted);
			contextAwareResult = new ContextAwareResult(captureIdentity: true, forceCaptureContext: true, this, state, callback);
			lock (contextAwareResult.StartPostingAsyncOp())
			{
				_writeAsyncResult = contextAwareResult;
				SubmitRequest(isAsync: true);
				contextAwareResult.FinishPostingAsyncOp();
				FinishRequestStage(RequestStage.CheckForError);
				return contextAwareResult;
			}
		}
		catch (Exception message)
		{
			if (System.Net.NetEventSource.Log.IsEnabled())
			{
				System.Net.NetEventSource.Error(this, message, "BeginGetRequestStream");
			}
			throw;
		}
	}

	public override Stream EndGetRequestStream(IAsyncResult asyncResult)
	{
		Stream stream = null;
		try
		{
			if (asyncResult == null)
			{
				throw new ArgumentNullException("asyncResult");
			}
			if (!(asyncResult is LazyAsyncResult lazyAsyncResult))
			{
				throw new ArgumentException(System.SR.net_io_invalidasyncresult, "asyncResult");
			}
			if (lazyAsyncResult.EndCalled)
			{
				throw new InvalidOperationException(System.SR.Format(System.SR.net_io_invalidendcall, "EndGetResponse"));
			}
			lazyAsyncResult.InternalWaitForCompletion();
			lazyAsyncResult.EndCalled = true;
			CheckError();
			stream = _stream;
			lazyAsyncResult.EndCalled = true;
			if (stream.CanTimeout)
			{
				stream.WriteTimeout = ReadWriteTimeout;
				stream.ReadTimeout = ReadWriteTimeout;
			}
		}
		catch (Exception message)
		{
			if (System.Net.NetEventSource.Log.IsEnabled())
			{
				System.Net.NetEventSource.Error(this, message, "EndGetRequestStream");
			}
			throw;
		}
		return stream;
	}

	private void SubmitRequest(bool isAsync)
	{
		try
		{
			_async = isAsync;
			while (true)
			{
				FtpControlStream ftpControlStream = _connection;
				if (ftpControlStream == null)
				{
					if (isAsync)
					{
						CreateConnectionAsync();
						return;
					}
					ftpControlStream = (_connection = CreateConnection());
				}
				if (!isAsync && Timeout != -1)
				{
					_remainingTimeout = Timeout - (int)(DateTime.UtcNow - _startTime).TotalMilliseconds;
					if (_remainingTimeout <= 0)
					{
						break;
					}
				}
				if (System.Net.NetEventSource.Log.IsEnabled())
				{
					System.Net.NetEventSource.Info(this, "Request being submitted", "SubmitRequest");
				}
				ftpControlStream.SetSocketTimeoutOption(RemainingTimeout);
				try
				{
					Stream stream = TimedSubmitRequestHelper(isAsync);
					return;
				}
				catch (Exception e)
				{
					if (AttemptedRecovery(e))
					{
						if (!isAsync && Timeout != -1)
						{
							_remainingTimeout = Timeout - (int)(DateTime.UtcNow - _startTime).TotalMilliseconds;
							if (_remainingTimeout <= 0)
							{
								throw;
							}
						}
						continue;
					}
					throw;
				}
			}
			throw ExceptionHelper.TimeoutException;
		}
		catch (WebException ex)
		{
			if (ex.InnerException is IOException { InnerException: SocketException { SocketErrorCode: SocketError.TimedOut } })
			{
				SetException(ExceptionDispatchInfo.SetCurrentStackTrace(new WebException(System.SR.net_timeout, WebExceptionStatus.Timeout)));
			}
			else
			{
				SetException(ex);
			}
		}
		catch (Exception exception)
		{
			SetException(exception);
		}
	}

	private Exception TranslateConnectException(Exception e)
	{
		if (e is SocketException ex)
		{
			if (ex.SocketErrorCode == SocketError.HostNotFound)
			{
				return new WebException(System.SR.net_webstatus_NameResolutionFailure, WebExceptionStatus.NameResolutionFailure);
			}
			return new WebException(System.SR.net_webstatus_ConnectFailure, WebExceptionStatus.ConnectFailure);
		}
		return e;
	}

	private async void CreateConnectionAsync()
	{
		object obj;
		try
		{
			TcpClient client = new TcpClient();
			await client.ConnectAsync(_uri.Host, _uri.Port).ConfigureAwait(continueOnCapturedContext: false);
			obj = new FtpControlStream(client);
		}
		catch (Exception e)
		{
			obj = TranslateConnectException(e);
		}
		AsyncRequestCallback(obj);
	}

	private FtpControlStream CreateConnection()
	{
		string host = _uri.Host;
		int port = _uri.Port;
		TcpClient tcpClient = new TcpClient();
		try
		{
			tcpClient.Connect(host, port);
		}
		catch (Exception e)
		{
			throw TranslateConnectException(e);
		}
		return new FtpControlStream(tcpClient);
	}

	private Stream TimedSubmitRequestHelper(bool isAsync)
	{
		if (isAsync)
		{
			if (_requestCompleteAsyncResult == null)
			{
				_requestCompleteAsyncResult = new LazyAsyncResult(null, null, null);
			}
			return _connection.SubmitRequest(this, isAsync: true, readInitalResponseOnConnect: true);
		}
		Stream stream = null;
		bool flag = false;
		TimerThread.Timer timer = TimerQueue.CreateTimer(_timerCallback, null);
		try
		{
			stream = _connection.SubmitRequest(this, isAsync: false, readInitalResponseOnConnect: true);
		}
		catch (Exception ex)
		{
			if ((!(ex is SocketException) && !(ex is ObjectDisposedException)) || !timer.HasExpired)
			{
				timer.Cancel();
				throw;
			}
			flag = true;
		}
		if (flag || !timer.Cancel())
		{
			_timedOut = true;
			throw ExceptionHelper.TimeoutException;
		}
		if (stream != null)
		{
			lock (_syncObject)
			{
				if (_aborted)
				{
					((ICloseEx)stream).CloseEx(CloseExState.Abort | CloseExState.Silent);
					CheckError();
					throw new System.Net.InternalException();
				}
				_stream = stream;
			}
		}
		return stream;
	}

	private void TimerCallback(TimerThread.Timer timer, int timeNoticed, object context)
	{
		if (System.Net.NetEventSource.Log.IsEnabled())
		{
			System.Net.NetEventSource.Info(this, null, "TimerCallback");
		}
		FtpControlStream connection = _connection;
		if (connection != null)
		{
			if (System.Net.NetEventSource.Log.IsEnabled())
			{
				System.Net.NetEventSource.Info(this, "aborting connection", "TimerCallback");
			}
			connection.AbortConnect();
		}
	}

	private bool AttemptedRecovery(Exception e)
	{
		if (e is OutOfMemoryException || _onceFailed || _aborted || _timedOut || _connection == null || !_connection.RecoverableFailure)
		{
			return false;
		}
		_onceFailed = true;
		lock (_syncObject)
		{
			if (_connection == null)
			{
				return false;
			}
			_connection.CloseSocket();
			if (System.Net.NetEventSource.Log.IsEnabled())
			{
				System.Net.NetEventSource.Info(this, $"Releasing connection: {_connection}", "AttemptedRecovery");
			}
			_connection = null;
		}
		return true;
	}

	private void SetException(Exception exception)
	{
		if (System.Net.NetEventSource.Log.IsEnabled())
		{
			System.Net.NetEventSource.Info(this, null, "SetException");
		}
		if (exception is OutOfMemoryException)
		{
			_exception = exception;
			throw exception;
		}
		FtpControlStream connection = _connection;
		if (_exception == null)
		{
			if (exception is WebException)
			{
				EnsureFtpWebResponse(exception);
				_exception = new WebException(exception.Message, null, ((WebException)exception).Status, _ftpWebResponse);
			}
			else if (exception is AuthenticationException || exception is SecurityException)
			{
				_exception = exception;
			}
			else if (connection != null && connection.StatusCode != 0)
			{
				EnsureFtpWebResponse(exception);
				_exception = new WebException(System.SR.Format(System.SR.net_ftp_servererror, connection.StatusLine), exception, WebExceptionStatus.ProtocolError, _ftpWebResponse);
			}
			else
			{
				_exception = new WebException(exception.Message, exception);
			}
			if (connection != null && _ftpWebResponse != null)
			{
				_ftpWebResponse.UpdateStatus(connection.StatusCode, connection.StatusLine, connection.ExitMessage);
			}
		}
	}

	private void CheckError()
	{
		if (_exception != null)
		{
			ExceptionDispatchInfo.Throw(_exception);
		}
	}

	internal void RequestCallback(object obj)
	{
		if (_async)
		{
			AsyncRequestCallback(obj);
		}
		else
		{
			SyncRequestCallback(obj);
		}
	}

	private void SyncRequestCallback(object obj)
	{
		RequestStage stage = RequestStage.CheckForError;
		try
		{
			bool flag = obj == null;
			Exception ex = obj as Exception;
			if (System.Net.NetEventSource.Log.IsEnabled())
			{
				System.Net.NetEventSource.Info(this, $"exp:{ex} completedRequest:{flag}", "SyncRequestCallback");
			}
			if (ex != null)
			{
				SetException(ex);
				return;
			}
			if (!flag)
			{
				throw new System.Net.InternalException();
			}
			FtpControlStream connection = _connection;
			if (connection != null)
			{
				EnsureFtpWebResponse(null);
				_ftpWebResponse.UpdateStatus(connection.StatusCode, connection.StatusLine, connection.ExitMessage);
			}
			stage = RequestStage.ReleaseConnection;
		}
		catch (Exception exception)
		{
			SetException(exception);
		}
		finally
		{
			FinishRequestStage(stage);
			CheckError();
		}
	}

	private void AsyncRequestCallback(object obj)
	{
		RequestStage stage = RequestStage.CheckForError;
		try
		{
			FtpControlStream ftpControlStream = obj as FtpControlStream;
			FtpDataStream ftpDataStream = obj as FtpDataStream;
			Exception ex = obj as Exception;
			bool flag = obj == null;
			if (System.Net.NetEventSource.Log.IsEnabled())
			{
				System.Net.NetEventSource.Info(this, $"stream:{ftpDataStream} conn:{ftpControlStream} exp:{ex} completedRequest:{flag}", "AsyncRequestCallback");
			}
			while (true)
			{
				if (ex != null)
				{
					if (AttemptedRecovery(ex))
					{
						ftpControlStream = CreateConnection();
						if (ftpControlStream == null)
						{
							return;
						}
						ex = null;
					}
					if (ex != null)
					{
						SetException(ex);
						return;
					}
				}
				if (ftpControlStream == null)
				{
					break;
				}
				lock (_syncObject)
				{
					if (_aborted)
					{
						if (System.Net.NetEventSource.Log.IsEnabled())
						{
							System.Net.NetEventSource.Info(this, $"Releasing connect:{ftpControlStream}", "AsyncRequestCallback");
						}
						ftpControlStream.CloseSocket();
						return;
					}
					_connection = ftpControlStream;
					if (System.Net.NetEventSource.Log.IsEnabled())
					{
						System.Net.NetEventSource.Associate(this, _connection, "AsyncRequestCallback");
					}
				}
				try
				{
					ftpDataStream = (FtpDataStream)TimedSubmitRequestHelper(isAsync: true);
					return;
				}
				catch (Exception ex2)
				{
					ex = ex2;
				}
			}
			if (ftpDataStream != null)
			{
				lock (_syncObject)
				{
					if (_aborted)
					{
						((ICloseEx)ftpDataStream).CloseEx(CloseExState.Abort | CloseExState.Silent);
						return;
					}
					_stream = ftpDataStream;
				}
				ftpDataStream.SetSocketTimeoutOption(Timeout);
				EnsureFtpWebResponse(null);
				stage = (ftpDataStream.CanRead ? RequestStage.ReadReady : RequestStage.WriteReady);
			}
			else
			{
				if (!flag)
				{
					throw new System.Net.InternalException();
				}
				ftpControlStream = _connection;
				if (ftpControlStream != null)
				{
					EnsureFtpWebResponse(null);
					_ftpWebResponse.UpdateStatus(ftpControlStream.StatusCode, ftpControlStream.StatusLine, ftpControlStream.ExitMessage);
				}
				stage = RequestStage.ReleaseConnection;
			}
		}
		catch (Exception exception)
		{
			SetException(exception);
		}
		finally
		{
			FinishRequestStage(stage);
		}
	}

	private RequestStage FinishRequestStage(RequestStage stage)
	{
		if (System.Net.NetEventSource.Log.IsEnabled())
		{
			System.Net.NetEventSource.Info(this, $"state:{stage}", "FinishRequestStage");
		}
		if (_exception != null)
		{
			stage = RequestStage.ReleaseConnection;
		}
		RequestStage requestStage;
		LazyAsyncResult writeAsyncResult;
		LazyAsyncResult readAsyncResult;
		FtpControlStream connection;
		lock (_syncObject)
		{
			requestStage = _requestStage;
			if (stage == RequestStage.CheckForError)
			{
				return requestStage;
			}
			if (requestStage == RequestStage.ReleaseConnection && stage == RequestStage.ReleaseConnection)
			{
				return RequestStage.ReleaseConnection;
			}
			if (stage > requestStage)
			{
				_requestStage = stage;
			}
			if (stage <= RequestStage.RequestStarted)
			{
				return requestStage;
			}
			writeAsyncResult = _writeAsyncResult;
			readAsyncResult = _readAsyncResult;
			connection = _connection;
			if (stage == RequestStage.ReleaseConnection)
			{
				if (_exception == null && !_aborted && requestStage != RequestStage.ReadReady && _methodInfo.IsDownload && !_ftpWebResponse.IsFromCache)
				{
					return requestStage;
				}
				_connection = null;
			}
		}
		try
		{
			if ((stage == RequestStage.ReleaseConnection || requestStage == RequestStage.ReleaseConnection) && connection != null)
			{
				try
				{
					if (_exception != null)
					{
						connection.Abort(_exception);
					}
				}
				finally
				{
					if (System.Net.NetEventSource.Log.IsEnabled())
					{
						System.Net.NetEventSource.Info(this, $"Releasing connection: {connection}", "FinishRequestStage");
					}
					connection.CloseSocket();
					if (_async && _requestCompleteAsyncResult != null)
					{
						_requestCompleteAsyncResult.InvokeCallback();
					}
				}
			}
			return requestStage;
		}
		finally
		{
			try
			{
				if (stage >= RequestStage.WriteReady)
				{
					if (_methodInfo.IsUpload && !_getRequestStreamStarted)
					{
						if (_stream != null)
						{
							_stream.Close();
						}
					}
					else if (writeAsyncResult != null && !writeAsyncResult.InternalPeekCompleted)
					{
						writeAsyncResult.InvokeCallback();
					}
				}
			}
			finally
			{
				if (stage >= RequestStage.ReadReady && readAsyncResult != null && !readAsyncResult.InternalPeekCompleted)
				{
					readAsyncResult.InvokeCallback();
				}
			}
		}
	}

	public override void Abort()
	{
		if (_aborted)
		{
			return;
		}
		try
		{
			Stream stream;
			FtpControlStream connection;
			lock (_syncObject)
			{
				if (_requestStage >= RequestStage.ReleaseConnection)
				{
					return;
				}
				_aborted = true;
				stream = _stream;
				connection = _connection;
				_exception = ExceptionHelper.RequestAbortedException;
			}
			if (stream != null)
			{
				((ICloseEx)stream).CloseEx(CloseExState.Abort | CloseExState.Silent);
			}
			connection?.Abort(ExceptionHelper.RequestAbortedException);
		}
		catch (Exception message)
		{
			if (System.Net.NetEventSource.Log.IsEnabled())
			{
				System.Net.NetEventSource.Error(this, message, "Abort");
			}
			throw;
		}
	}

	private void EnsureFtpWebResponse(Exception exception)
	{
		if (_ftpWebResponse == null || (_ftpWebResponse.GetResponseStream() is FtpWebResponse.EmptyStream && _stream != null))
		{
			lock (_syncObject)
			{
				if (_ftpWebResponse == null || (_ftpWebResponse.GetResponseStream() is FtpWebResponse.EmptyStream && _stream != null))
				{
					Stream stream = _stream;
					if (_methodInfo.IsUpload)
					{
						stream = null;
					}
					if (_stream != null && _stream.CanRead && _stream.CanTimeout)
					{
						_stream.ReadTimeout = ReadWriteTimeout;
						_stream.WriteTimeout = ReadWriteTimeout;
					}
					FtpControlStream connection = _connection;
					long num = connection?.ContentLength ?? (-1);
					if (stream == null && num < 0)
					{
						num = 0L;
					}
					if (_ftpWebResponse != null)
					{
						_ftpWebResponse.SetResponseStream(stream);
					}
					else if (connection != null)
					{
						_ftpWebResponse = new FtpWebResponse(stream, num, connection.ResponseUri, connection.StatusCode, connection.StatusLine, connection.LastModified, connection.BannerMessage, connection.WelcomeMessage, connection.ExitMessage);
					}
					else
					{
						_ftpWebResponse = new FtpWebResponse(stream, -1L, _uri, FtpStatusCode.Undefined, null, DateTime.Now, null, null, null);
					}
				}
			}
		}
		if (System.Net.NetEventSource.Log.IsEnabled())
		{
			System.Net.NetEventSource.Info(this, $"Returns {_ftpWebResponse} with stream {_ftpWebResponse._responseStream}", "EnsureFtpWebResponse");
		}
	}

	internal void DataStreamClosed(CloseExState closeState)
	{
		if ((closeState & CloseExState.Abort) == 0)
		{
			if (!_async)
			{
				if (_connection != null)
				{
					_connection.CheckContinuePipeline();
				}
			}
			else
			{
				_requestCompleteAsyncResult.InternalWaitForCompletion();
				CheckError();
			}
		}
		else
		{
			_connection?.Abort(ExceptionHelper.RequestAbortedException);
		}
	}
}
