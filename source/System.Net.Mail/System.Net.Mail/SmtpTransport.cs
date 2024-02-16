using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;

namespace System.Net.Mail;

internal sealed class SmtpTransport
{
	private readonly ISmtpAuthenticationModule[] _authenticationModules;

	private SmtpConnection _connection;

	private readonly SmtpClient _client;

	private ICredentialsByHost _credentials;

	private readonly List<SmtpFailedRecipientException> _failedRecipientExceptions = new List<SmtpFailedRecipientException>();

	private bool _identityRequired;

	private bool _shouldAbort;

	private bool _enableSsl;

	private X509CertificateCollection _clientCertificates;

	internal ICredentialsByHost Credentials
	{
		get
		{
			return _credentials;
		}
		set
		{
			_credentials = value;
		}
	}

	internal bool IdentityRequired
	{
		get
		{
			return _identityRequired;
		}
		set
		{
			_identityRequired = value;
		}
	}

	internal bool IsConnected
	{
		get
		{
			if (_connection != null)
			{
				return _connection.IsConnected;
			}
			return false;
		}
	}

	internal bool EnableSsl
	{
		get
		{
			return _enableSsl;
		}
		set
		{
			_enableSsl = value;
		}
	}

	internal X509CertificateCollection ClientCertificates => _clientCertificates ?? (_clientCertificates = new X509CertificateCollection());

	internal bool ServerSupportsEai
	{
		get
		{
			if (_connection != null)
			{
				return _connection.ServerSupportsEai;
			}
			return false;
		}
	}

	internal SmtpTransport(SmtpClient client)
		: this(client, SmtpAuthenticationManager.GetModules())
	{
	}

	internal SmtpTransport(SmtpClient client, ISmtpAuthenticationModule[] authenticationModules)
	{
		_client = client;
		if (authenticationModules == null)
		{
			throw new ArgumentNullException("authenticationModules");
		}
		_authenticationModules = authenticationModules;
	}

	internal void GetConnection(string host, int port)
	{
		lock (this)
		{
			_connection = new SmtpConnection(this, _client, _credentials, _authenticationModules);
			if (_shouldAbort)
			{
				_connection.Abort();
			}
			_shouldAbort = false;
		}
		if (System.Net.NetEventSource.Log.IsEnabled())
		{
			System.Net.NetEventSource.Associate(this, _connection, "GetConnection");
		}
		if (EnableSsl)
		{
			_connection.EnableSsl = true;
			_connection.ClientCertificates = ClientCertificates;
		}
		_connection.GetConnection(host, port);
	}

	internal IAsyncResult BeginGetConnection(System.Net.ContextAwareResult outerResult, AsyncCallback callback, object state, string host, int port)
	{
		IAsyncResult asyncResult = null;
		try
		{
			_connection = new SmtpConnection(this, _client, _credentials, _authenticationModules);
			if (System.Net.NetEventSource.Log.IsEnabled())
			{
				System.Net.NetEventSource.Associate(this, _connection, "BeginGetConnection");
			}
			if (EnableSsl)
			{
				_connection.EnableSsl = true;
				_connection.ClientCertificates = ClientCertificates;
			}
			asyncResult = _connection.BeginGetConnection(outerResult, callback, state, host, port);
		}
		catch (Exception innerException)
		{
			throw new SmtpException(System.SR.MailHostNotFound, innerException);
		}
		if (System.Net.NetEventSource.Log.IsEnabled())
		{
			System.Net.NetEventSource.Info(this, "Sync completion", "BeginGetConnection");
		}
		return asyncResult;
	}

	internal void EndGetConnection(IAsyncResult result)
	{
		_connection.EndGetConnection(result);
	}

	internal IAsyncResult BeginSendMail(MailAddress sender, MailAddressCollection recipients, string deliveryNotify, bool allowUnicode, AsyncCallback callback, object state)
	{
		if (sender == null)
		{
			throw new ArgumentNullException("sender");
		}
		if (recipients == null)
		{
			throw new ArgumentNullException("recipients");
		}
		SendMailAsyncResult sendMailAsyncResult = new SendMailAsyncResult(_connection, sender, recipients, allowUnicode, _connection.DSNEnabled ? deliveryNotify : null, callback, state);
		sendMailAsyncResult.Send();
		return sendMailAsyncResult;
	}

	internal void ReleaseConnection()
	{
		_connection?.ReleaseConnection();
	}

	internal void Abort()
	{
		lock (this)
		{
			if (_connection != null)
			{
				_connection.Abort();
			}
			else
			{
				_shouldAbort = true;
			}
		}
	}

	internal MailWriter EndSendMail(IAsyncResult result)
	{
		return SendMailAsyncResult.End(result);
	}

	internal MailWriter SendMail(MailAddress sender, MailAddressCollection recipients, string deliveryNotify, bool allowUnicode, out SmtpFailedRecipientException exception)
	{
		if (sender == null)
		{
			throw new ArgumentNullException("sender");
		}
		if (recipients == null)
		{
			throw new ArgumentNullException("recipients");
		}
		MailCommand.Send(_connection, SmtpCommands.Mail, sender, allowUnicode);
		_failedRecipientExceptions.Clear();
		exception = null;
		foreach (MailAddress recipient in recipients)
		{
			string smtpAddress = recipient.GetSmtpAddress(allowUnicode);
			string to = smtpAddress + (_connection.DSNEnabled ? deliveryNotify : string.Empty);
			if (!RecipientCommand.Send(_connection, to, out var response))
			{
				_failedRecipientExceptions.Add(new SmtpFailedRecipientException(_connection.Reader.StatusCode, smtpAddress, response));
			}
		}
		if (_failedRecipientExceptions.Count > 0)
		{
			if (_failedRecipientExceptions.Count == 1)
			{
				exception = _failedRecipientExceptions[0];
			}
			else
			{
				exception = new SmtpFailedRecipientsException(_failedRecipientExceptions, _failedRecipientExceptions.Count == recipients.Count);
			}
			if (_failedRecipientExceptions.Count == recipients.Count)
			{
				exception.fatal = true;
				throw exception;
			}
		}
		DataCommand.Send(_connection);
		return new MailWriter(_connection.GetClosableStream(), encodeForTransport: true);
	}
}
