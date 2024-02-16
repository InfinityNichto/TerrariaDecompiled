using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Net.NetworkInformation;
using System.Runtime.CompilerServices;
using System.Runtime.Versioning;
using System.Security;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace System.Net.Mail;

[UnsupportedOSPlatform("browser")]
public class SmtpClient : IDisposable
{
	private string _host;

	private int _port;

	private int _timeout = 100000;

	private bool _inCall;

	private bool _cancelled;

	private bool _timedOut;

	private string _targetName;

	private SmtpDeliveryMethod _deliveryMethod;

	private SmtpDeliveryFormat _deliveryFormat;

	private string _pickupDirectoryLocation;

	private SmtpTransport _transport;

	private MailMessage _message;

	private MailWriter _writer;

	private MailAddressCollection _recipients;

	private SendOrPostCallback _onSendCompletedDelegate;

	private Timer _timer;

	private System.Net.ContextAwareResult _operationCompletedResult;

	private AsyncOperation _asyncOp;

	private static readonly AsyncCallback s_contextSafeCompleteCallback = ContextSafeCompleteCallback;

	internal string _clientDomain;

	private bool _disposed;

	private ServicePoint _servicePoint;

	private SmtpFailedRecipientException _failedRecipientException;

	private bool _useDefaultCredentials;

	private ICredentialsByHost _customCredentials;

	public string? Host
	{
		get
		{
			return _host;
		}
		[param: DisallowNull]
		set
		{
			if (InCall)
			{
				throw new InvalidOperationException(System.SR.SmtpInvalidOperationDuringSend);
			}
			if (value == null)
			{
				throw new ArgumentNullException("value");
			}
			if (value.Length == 0)
			{
				throw new ArgumentException(System.SR.net_emptystringset, "value");
			}
			value = value.Trim();
			if (value != _host)
			{
				_host = value;
				_servicePoint = null;
			}
		}
	}

	public int Port
	{
		get
		{
			return _port;
		}
		set
		{
			if (InCall)
			{
				throw new InvalidOperationException(System.SR.SmtpInvalidOperationDuringSend);
			}
			if (value <= 0)
			{
				throw new ArgumentOutOfRangeException("value");
			}
			if (value != _port)
			{
				_port = value;
				_servicePoint = null;
			}
		}
	}

	public bool UseDefaultCredentials
	{
		get
		{
			return _useDefaultCredentials;
		}
		set
		{
			if (InCall)
			{
				throw new InvalidOperationException(System.SR.SmtpInvalidOperationDuringSend);
			}
			_useDefaultCredentials = value;
			UpdateTransportCredentials();
		}
	}

	public ICredentialsByHost? Credentials
	{
		get
		{
			return _transport.Credentials;
		}
		set
		{
			if (InCall)
			{
				throw new InvalidOperationException(System.SR.SmtpInvalidOperationDuringSend);
			}
			_customCredentials = value;
			UpdateTransportCredentials();
		}
	}

	public int Timeout
	{
		get
		{
			return _timeout;
		}
		set
		{
			if (InCall)
			{
				throw new InvalidOperationException(System.SR.SmtpInvalidOperationDuringSend);
			}
			if (value < 0)
			{
				throw new ArgumentOutOfRangeException("value");
			}
			_timeout = value;
		}
	}

	public ServicePoint ServicePoint
	{
		get
		{
			CheckHostAndPort();
			return _servicePoint ?? (_servicePoint = ServicePointManager.FindServicePoint(new Uri("mailto:" + _host + ":" + _port)));
		}
	}

	public SmtpDeliveryMethod DeliveryMethod
	{
		get
		{
			return _deliveryMethod;
		}
		set
		{
			_deliveryMethod = value;
		}
	}

	public SmtpDeliveryFormat DeliveryFormat
	{
		get
		{
			return _deliveryFormat;
		}
		set
		{
			_deliveryFormat = value;
		}
	}

	public string? PickupDirectoryLocation
	{
		get
		{
			return _pickupDirectoryLocation;
		}
		set
		{
			_pickupDirectoryLocation = value;
		}
	}

	public bool EnableSsl
	{
		get
		{
			return _transport.EnableSsl;
		}
		set
		{
			_transport.EnableSsl = value;
		}
	}

	public X509CertificateCollection ClientCertificates => _transport.ClientCertificates;

	public string? TargetName
	{
		get
		{
			return _targetName;
		}
		set
		{
			_targetName = value;
		}
	}

	private bool ServerSupportsEai => _transport.ServerSupportsEai;

	internal bool InCall
	{
		get
		{
			return _inCall;
		}
		set
		{
			_inCall = value;
		}
	}

	public event SendCompletedEventHandler? SendCompleted;

	public SmtpClient()
	{
		Initialize();
	}

	public SmtpClient(string? host)
	{
		_host = host;
		Initialize();
	}

	public SmtpClient(string? host, int port)
	{
		if (port < 0)
		{
			throw new ArgumentOutOfRangeException("port");
		}
		_host = host;
		_port = port;
		Initialize();
	}

	[MemberNotNull("_transport")]
	[MemberNotNull("_onSendCompletedDelegate")]
	[MemberNotNull("_clientDomain")]
	private void Initialize()
	{
		_transport = new SmtpTransport(this);
		if (System.Net.NetEventSource.Log.IsEnabled())
		{
			System.Net.NetEventSource.Associate(this, _transport, "Initialize");
		}
		_onSendCompletedDelegate = SendCompletedWaitCallback;
		if (_host != null && _host.Length != 0)
		{
			_host = _host.Trim();
		}
		if (_port == 0)
		{
			_port = 25;
		}
		if (_targetName == null)
		{
			_targetName = "SMTPSVC/" + _host;
		}
		if (_clientDomain != null)
		{
			return;
		}
		string text = IPGlobalProperties.GetIPGlobalProperties().HostName;
		IdnMapping idnMapping = new IdnMapping();
		try
		{
			text = idnMapping.GetAscii(text);
		}
		catch (ArgumentException)
		{
		}
		StringBuilder stringBuilder = new StringBuilder();
		foreach (char c in text)
		{
			if (c <= '\u007f')
			{
				stringBuilder.Append(c);
			}
		}
		if (stringBuilder.Length > 0)
		{
			_clientDomain = stringBuilder.ToString();
		}
		else
		{
			_clientDomain = "LocalHost";
		}
	}

	private void UpdateTransportCredentials()
	{
		SmtpTransport transport = _transport;
		ICredentialsByHost credentials;
		if (!_useDefaultCredentials)
		{
			credentials = _customCredentials;
		}
		else
		{
			ICredentialsByHost defaultNetworkCredentials = CredentialCache.DefaultNetworkCredentials;
			credentials = defaultNetworkCredentials;
		}
		transport.Credentials = credentials;
	}

	private bool IsUnicodeSupported()
	{
		if (DeliveryMethod == SmtpDeliveryMethod.Network)
		{
			if (ServerSupportsEai)
			{
				return DeliveryFormat == SmtpDeliveryFormat.International;
			}
			return false;
		}
		return DeliveryFormat == SmtpDeliveryFormat.International;
	}

	internal MailWriter GetFileMailWriter(string pickupDirectory)
	{
		if (System.Net.NetEventSource.Log.IsEnabled())
		{
			System.Net.NetEventSource.Info(this, FormattableStringFactory.Create("{0}={1}", "pickupDirectory", pickupDirectory), "GetFileMailWriter");
		}
		if (!Path.IsPathRooted(pickupDirectory))
		{
			throw new SmtpException(System.SR.SmtpNeedAbsolutePickupDirectory);
		}
		string path2;
		do
		{
			string path = $"{Guid.NewGuid()}.eml";
			path2 = Path.Combine(pickupDirectory, path);
		}
		while (File.Exists(path2));
		FileStream stream = new FileStream(path2, FileMode.CreateNew);
		return new MailWriter(stream, encodeForTransport: false);
	}

	protected void OnSendCompleted(AsyncCompletedEventArgs e)
	{
		this.SendCompleted?.Invoke(this, e);
	}

	private void SendCompletedWaitCallback(object operationState)
	{
		OnSendCompleted((AsyncCompletedEventArgs)operationState);
	}

	public void Send(string from, string recipients, string? subject, string? body)
	{
		if (_disposed)
		{
			throw new ObjectDisposedException(GetType().FullName);
		}
		MailMessage message = new MailMessage(from, recipients, subject, body);
		Send(message);
	}

	public void Send(MailMessage message)
	{
		if (_disposed)
		{
			throw new ObjectDisposedException(GetType().FullName);
		}
		if (System.Net.NetEventSource.Log.IsEnabled())
		{
			System.Net.NetEventSource.Info(this, $"DeliveryMethod={DeliveryMethod}", "Send");
			System.Net.NetEventSource.Associate(this, message, "Send");
		}
		SmtpFailedRecipientException exception = null;
		if (InCall)
		{
			throw new InvalidOperationException(System.SR.net_inasync);
		}
		if (message == null)
		{
			throw new ArgumentNullException("message");
		}
		if (DeliveryMethod == SmtpDeliveryMethod.Network)
		{
			CheckHostAndPort();
		}
		MailAddressCollection mailAddressCollection = new MailAddressCollection();
		if (message.From == null)
		{
			throw new InvalidOperationException(System.SR.SmtpFromRequired);
		}
		if (message.To != null)
		{
			foreach (MailAddress item in message.To)
			{
				mailAddressCollection.Add(item);
			}
		}
		if (message.Bcc != null)
		{
			foreach (MailAddress item2 in message.Bcc)
			{
				mailAddressCollection.Add(item2);
			}
		}
		if (message.CC != null)
		{
			foreach (MailAddress item3 in message.CC)
			{
				mailAddressCollection.Add(item3);
			}
		}
		if (mailAddressCollection.Count == 0)
		{
			throw new InvalidOperationException(System.SR.SmtpRecipientRequired);
		}
		_transport.IdentityRequired = false;
		try
		{
			InCall = true;
			_timedOut = false;
			_timer = new Timer(TimeOutCallback, null, Timeout, Timeout);
			bool flag = false;
			string pickupDirectoryLocation = PickupDirectoryLocation;
			MailWriter mailWriter;
			switch (DeliveryMethod)
			{
			case SmtpDeliveryMethod.PickupDirectoryFromIis:
				throw new NotSupportedException(System.SR.SmtpGetIisPickupDirectoryNotSupported);
			case SmtpDeliveryMethod.SpecifiedPickupDirectory:
				if (EnableSsl)
				{
					throw new SmtpException(System.SR.SmtpPickupDirectoryDoesnotSupportSsl);
				}
				flag = IsUnicodeSupported();
				ValidateUnicodeRequirement(message, mailAddressCollection, flag);
				mailWriter = GetFileMailWriter(pickupDirectoryLocation);
				break;
			default:
				GetConnection();
				flag = IsUnicodeSupported();
				ValidateUnicodeRequirement(message, mailAddressCollection, flag);
				mailWriter = _transport.SendMail(message.Sender ?? message.From, mailAddressCollection, message.BuildDeliveryStatusNotificationString(), flag, out exception);
				break;
			}
			_message = message;
			message.Send(mailWriter, DeliveryMethod != SmtpDeliveryMethod.Network, flag);
			mailWriter.Close();
			if (DeliveryMethod == SmtpDeliveryMethod.Network && exception != null)
			{
				throw exception;
			}
		}
		catch (Exception ex)
		{
			if (System.Net.NetEventSource.Log.IsEnabled())
			{
				System.Net.NetEventSource.Error(this, ex, "Send");
			}
			if (ex is SmtpFailedRecipientException && !((SmtpFailedRecipientException)ex).fatal)
			{
				throw;
			}
			Abort();
			if (_timedOut)
			{
				throw new SmtpException(System.SR.net_timeout);
			}
			if (ex is SecurityException || ex is AuthenticationException || ex is SmtpException)
			{
				throw;
			}
			throw new SmtpException(System.SR.SmtpSendMailFailure, ex);
		}
		finally
		{
			InCall = false;
			if (_timer != null)
			{
				_timer.Dispose();
			}
		}
	}

	public void SendAsync(string from, string recipients, string? subject, string? body, object? userToken)
	{
		if (_disposed)
		{
			throw new ObjectDisposedException(GetType().FullName);
		}
		SendAsync(new MailMessage(from, recipients, subject, body), userToken);
	}

	public void SendAsync(MailMessage message, object? userToken)
	{
		if (_disposed)
		{
			throw new ObjectDisposedException(GetType().FullName);
		}
		try
		{
			if (InCall)
			{
				throw new InvalidOperationException(System.SR.net_inasync);
			}
			if (message == null)
			{
				throw new ArgumentNullException("message");
			}
			if (DeliveryMethod == SmtpDeliveryMethod.Network)
			{
				CheckHostAndPort();
			}
			_recipients = new MailAddressCollection();
			if (message.From == null)
			{
				throw new InvalidOperationException(System.SR.SmtpFromRequired);
			}
			if (message.To != null)
			{
				foreach (MailAddress item in message.To)
				{
					_recipients.Add(item);
				}
			}
			if (message.Bcc != null)
			{
				foreach (MailAddress item2 in message.Bcc)
				{
					_recipients.Add(item2);
				}
			}
			if (message.CC != null)
			{
				foreach (MailAddress item3 in message.CC)
				{
					_recipients.Add(item3);
				}
			}
			if (_recipients.Count == 0)
			{
				throw new InvalidOperationException(System.SR.SmtpRecipientRequired);
			}
			InCall = true;
			_cancelled = false;
			_message = message;
			string pickupDirectoryLocation = PickupDirectoryLocation;
			_transport.IdentityRequired = Credentials != null && (Credentials == CredentialCache.DefaultNetworkCredentials || !(Credentials is CredentialCache cache) || IsSystemNetworkCredentialInCache(cache));
			_asyncOp = AsyncOperationManager.CreateOperation(userToken);
			switch (DeliveryMethod)
			{
			case SmtpDeliveryMethod.PickupDirectoryFromIis:
				throw new NotSupportedException(System.SR.SmtpGetIisPickupDirectoryNotSupported);
			case SmtpDeliveryMethod.SpecifiedPickupDirectory:
			{
				if (EnableSsl)
				{
					throw new SmtpException(System.SR.SmtpPickupDirectoryDoesnotSupportSsl);
				}
				_writer = GetFileMailWriter(pickupDirectoryLocation);
				bool allowUnicode = IsUnicodeSupported();
				ValidateUnicodeRequirement(message, _recipients, allowUnicode);
				message.Send(_writer, sendEnvelope: true, allowUnicode);
				if (_writer != null)
				{
					_writer.Close();
				}
				AsyncCompletedEventArgs arg = new AsyncCompletedEventArgs(null, cancelled: false, _asyncOp.UserSuppliedState);
				InCall = false;
				_asyncOp.PostOperationCompleted(_onSendCompletedDelegate, arg);
				return;
			}
			}
			_operationCompletedResult = new System.Net.ContextAwareResult(_transport.IdentityRequired, forceCaptureContext: true, null, this, s_contextSafeCompleteCallback);
			lock (_operationCompletedResult.StartPostingAsyncOp())
			{
				if (System.Net.NetEventSource.Log.IsEnabled())
				{
					System.Net.NetEventSource.Info(this, $"Calling BeginConnect. Transport: {_transport}", "SendAsync");
				}
				_transport.BeginGetConnection(_operationCompletedResult, ConnectCallback, _operationCompletedResult, Host, Port);
				_operationCompletedResult.FinishPostingAsyncOp();
			}
		}
		catch (Exception ex)
		{
			InCall = false;
			if (System.Net.NetEventSource.Log.IsEnabled())
			{
				System.Net.NetEventSource.Error(this, ex, "SendAsync");
			}
			if (ex is SmtpFailedRecipientException && !((SmtpFailedRecipientException)ex).fatal)
			{
				throw;
			}
			Abort();
			if (ex is SecurityException || ex is AuthenticationException || ex is SmtpException)
			{
				throw;
			}
			throw new SmtpException(System.SR.SmtpSendMailFailure, ex);
		}
	}

	private bool IsSystemNetworkCredentialInCache(CredentialCache cache)
	{
		foreach (NetworkCredential item in cache)
		{
			if (item == CredentialCache.DefaultNetworkCredentials)
			{
				return true;
			}
		}
		return false;
	}

	public void SendAsyncCancel()
	{
		if (_disposed)
		{
			throw new ObjectDisposedException(GetType().FullName);
		}
		if (InCall && !_cancelled)
		{
			_cancelled = true;
			Abort();
		}
	}

	public Task SendMailAsync(string from, string recipients, string? subject, string? body)
	{
		MailMessage message = new MailMessage(from, recipients, subject, body);
		return SendMailAsync(message, default(CancellationToken));
	}

	public Task SendMailAsync(MailMessage message)
	{
		return SendMailAsync(message, default(CancellationToken));
	}

	public Task SendMailAsync(string from, string recipients, string? subject, string? body, CancellationToken cancellationToken)
	{
		MailMessage message = new MailMessage(from, recipients, subject, body);
		return SendMailAsync(message, cancellationToken);
	}

	public Task SendMailAsync(MailMessage message, CancellationToken cancellationToken)
	{
		if (cancellationToken.IsCancellationRequested)
		{
			return Task.FromCanceled(cancellationToken);
		}
		TaskCompletionSource tcs = new TaskCompletionSource();
		CancellationTokenRegistration ctr = default(CancellationTokenRegistration);
		int state = 0;
		SendCompletedEventHandler handler = null;
		handler = delegate(object sender, AsyncCompletedEventArgs e)
		{
			if (e.UserState == tcs)
			{
				try
				{
					((SmtpClient)sender).SendCompleted -= handler;
					if (Interlocked.Exchange(ref state, 1) != 0)
					{
						ctr.Dispose();
					}
				}
				catch (ObjectDisposedException)
				{
				}
				finally
				{
					if (e.Error != null)
					{
						tcs.TrySetException(e.Error);
					}
					else if (e.Cancelled)
					{
						tcs.TrySetCanceled();
					}
					else
					{
						tcs.TrySetResult();
					}
				}
			}
		};
		SendCompleted += handler;
		try
		{
			SendAsync(message, tcs);
		}
		catch
		{
			SendCompleted -= handler;
			throw;
		}
		ctr = cancellationToken.Register(delegate(object s)
		{
			((SmtpClient)s).SendAsyncCancel();
		}, this);
		if (Interlocked.Exchange(ref state, 1) != 0)
		{
			ctr.Dispose();
		}
		return tcs.Task;
	}

	private void CheckHostAndPort()
	{
		if (_host == null || _host.Length == 0)
		{
			throw new InvalidOperationException(System.SR.UnspecifiedHost);
		}
		if (_port <= 0 || _port > 65535)
		{
			throw new InvalidOperationException(System.SR.InvalidPort);
		}
	}

	private void TimeOutCallback(object state)
	{
		if (!_timedOut)
		{
			_timedOut = true;
			Abort();
		}
	}

	private void Complete(Exception exception, IAsyncResult result)
	{
		System.Net.ContextAwareResult contextAwareResult = (System.Net.ContextAwareResult)result.AsyncState;
		try
		{
			if (_cancelled)
			{
				exception = null;
				Abort();
			}
			else if (exception != null && (!(exception is SmtpFailedRecipientException) || ((SmtpFailedRecipientException)exception).fatal))
			{
				if (System.Net.NetEventSource.Log.IsEnabled())
				{
					System.Net.NetEventSource.Error(this, exception, "Complete");
				}
				Abort();
				if (!(exception is SmtpException))
				{
					exception = new SmtpException(System.SR.SmtpSendMailFailure, exception);
				}
			}
			else if (_writer != null)
			{
				try
				{
					_writer.Close();
				}
				catch (SmtpException ex)
				{
					exception = ex;
				}
			}
		}
		finally
		{
			contextAwareResult.InvokeCallback(exception);
		}
		if (System.Net.NetEventSource.Log.IsEnabled())
		{
			System.Net.NetEventSource.Info(this, "Complete", "Complete");
		}
	}

	private static void ContextSafeCompleteCallback(IAsyncResult ar)
	{
		System.Net.ContextAwareResult contextAwareResult = (System.Net.ContextAwareResult)ar;
		SmtpClient smtpClient = (SmtpClient)ar.AsyncState;
		Exception error = contextAwareResult.Result as Exception;
		AsyncOperation asyncOp = smtpClient._asyncOp;
		AsyncCompletedEventArgs arg = new AsyncCompletedEventArgs(error, smtpClient._cancelled, asyncOp.UserSuppliedState);
		smtpClient.InCall = false;
		smtpClient._failedRecipientException = null;
		asyncOp.PostOperationCompleted(smtpClient._onSendCompletedDelegate, arg);
	}

	private void SendMessageCallback(IAsyncResult result)
	{
		try
		{
			_message.EndSend(result);
			Complete(_failedRecipientException, result);
		}
		catch (Exception exception)
		{
			Complete(exception, result);
		}
	}

	private void SendMailCallback(IAsyncResult result)
	{
		try
		{
			_writer = _transport.EndSendMail(result);
			SendMailAsyncResult sendMailAsyncResult = (SendMailAsyncResult)result;
			_failedRecipientException = sendMailAsyncResult.GetFailedRecipientException();
		}
		catch (Exception exception)
		{
			Complete(exception, result);
			return;
		}
		try
		{
			if (_cancelled)
			{
				Complete(null, result);
			}
			else
			{
				_message.BeginSend(_writer, DeliveryMethod != SmtpDeliveryMethod.Network, IsUnicodeSupported(), SendMessageCallback, result.AsyncState);
			}
		}
		catch (Exception exception2)
		{
			Complete(exception2, result);
		}
	}

	private void ConnectCallback(IAsyncResult result)
	{
		try
		{
			_transport.EndGetConnection(result);
			if (_cancelled)
			{
				Complete(null, result);
				return;
			}
			bool allowUnicode = IsUnicodeSupported();
			ValidateUnicodeRequirement(_message, _recipients, allowUnicode);
			_transport.BeginSendMail(_message.Sender ?? _message.From, _recipients, _message.BuildDeliveryStatusNotificationString(), allowUnicode, SendMailCallback, result.AsyncState);
		}
		catch (Exception exception)
		{
			Complete(exception, result);
		}
	}

	private void ValidateUnicodeRequirement(MailMessage message, MailAddressCollection recipients, bool allowUnicode)
	{
		foreach (MailAddress recipient in recipients)
		{
			recipient.GetSmtpAddress(allowUnicode);
		}
		message.Sender?.GetSmtpAddress(allowUnicode);
		message.From.GetSmtpAddress(allowUnicode);
	}

	private void GetConnection()
	{
		if (!_transport.IsConnected)
		{
			_transport.GetConnection(_host, _port);
		}
	}

	private void Abort()
	{
		_transport.Abort();
	}

	public void Dispose()
	{
		Dispose(disposing: true);
		GC.SuppressFinalize(this);
	}

	protected virtual void Dispose(bool disposing)
	{
		if (disposing && !_disposed)
		{
			if (InCall && !_cancelled)
			{
				_cancelled = true;
				Abort();
			}
			else
			{
				_transport?.ReleaseConnection();
			}
			_timer?.Dispose();
			_disposed = true;
		}
	}
}
