using System.IO;
using System.Net.Sockets;
using System.Runtime.ExceptionServices;
using System.Security.Authentication.ExtendedProtection;
using System.Security.Cryptography.X509Certificates;
using System.Threading;

namespace System.Net.Mail;

internal sealed class SmtpConnection
{
	private sealed class AuthenticateCallbackContext
	{
		internal readonly SmtpConnection _thisPtr;

		internal readonly ISmtpAuthenticationModule _module;

		internal readonly NetworkCredential _credential;

		internal readonly string _spn;

		internal readonly ChannelBinding _token;

		internal Authorization _result;

		internal AuthenticateCallbackContext(SmtpConnection thisPtr, ISmtpAuthenticationModule module, NetworkCredential credential, string spn, ChannelBinding Token)
		{
			_thisPtr = thisPtr;
			_module = module;
			_credential = credential;
			_spn = spn;
			_token = Token;
			_result = null;
		}
	}

	private sealed class ConnectAndHandshakeAsyncResult : System.Net.LazyAsyncResult
	{
		private string _authResponse;

		private readonly SmtpConnection _connection;

		private int _currentModule = -1;

		private readonly int _port;

		private static readonly AsyncCallback s_handshakeCallback = HandshakeCallback;

		private static readonly AsyncCallback s_sendEHelloCallback = SendEHelloCallback;

		private static readonly AsyncCallback s_sendHelloCallback = SendHelloCallback;

		private static readonly AsyncCallback s_authenticateCallback = AuthenticateCallback;

		private static readonly AsyncCallback s_authenticateContinueCallback = AuthenticateContinueCallback;

		private readonly string _host;

		private readonly System.Net.ContextAwareResult _outerResult;

		internal ConnectAndHandshakeAsyncResult(SmtpConnection connection, string host, int port, System.Net.ContextAwareResult outerResult, AsyncCallback callback, object state)
			: base(null, state, callback)
		{
			_connection = connection;
			_host = host;
			_port = port;
			_outerResult = outerResult;
		}

		internal static void End(IAsyncResult result)
		{
			ConnectAndHandshakeAsyncResult connectAndHandshakeAsyncResult = (ConnectAndHandshakeAsyncResult)result;
			object obj = connectAndHandshakeAsyncResult.InternalWaitForCompletion();
			if (obj is Exception source)
			{
				ExceptionDispatchInfo.Throw(source);
			}
		}

		internal void GetConnection()
		{
			if (_connection._isConnected)
			{
				throw new InvalidOperationException(System.SR.SmtpAlreadyConnected);
			}
			InitializeConnection();
		}

		private void InitializeConnection()
		{
			IAsyncResult asyncResult = _connection.BeginInitializeConnection(_host, _port, InitializeConnectionCallback, this);
			if (!asyncResult.CompletedSynchronously)
			{
				return;
			}
			try
			{
				_connection.EndInitializeConnection(asyncResult);
				if (System.Net.NetEventSource.Log.IsEnabled())
				{
					System.Net.NetEventSource.Info(this, "Connect returned", "InitializeConnection");
				}
				Handshake();
			}
			catch (Exception result)
			{
				InvokeCallback(result);
			}
		}

		private static void InitializeConnectionCallback(IAsyncResult result)
		{
			if (result.CompletedSynchronously)
			{
				return;
			}
			ConnectAndHandshakeAsyncResult connectAndHandshakeAsyncResult = (ConnectAndHandshakeAsyncResult)result.AsyncState;
			try
			{
				connectAndHandshakeAsyncResult._connection.EndInitializeConnection(result);
				if (System.Net.NetEventSource.Log.IsEnabled())
				{
					System.Net.NetEventSource.Info(null, $"Connect returned {connectAndHandshakeAsyncResult}", "InitializeConnectionCallback");
				}
				connectAndHandshakeAsyncResult.Handshake();
			}
			catch (Exception result2)
			{
				connectAndHandshakeAsyncResult.InvokeCallback(result2);
			}
		}

		private void Handshake()
		{
			_connection._responseReader = new SmtpReplyReaderFactory(_connection._networkStream);
			SmtpReplyReader nextReplyReader = _connection.Reader.GetNextReplyReader();
			IAsyncResult asyncResult = nextReplyReader.BeginReadLine(s_handshakeCallback, this);
			if (!asyncResult.CompletedSynchronously)
			{
				return;
			}
			LineInfo lineInfo = nextReplyReader.EndReadLine(asyncResult);
			if (lineInfo.StatusCode != SmtpStatusCode.ServiceReady)
			{
				throw new SmtpException(lineInfo.StatusCode, lineInfo.Line, serverResponse: true);
			}
			try
			{
				SendEHello();
			}
			catch
			{
				SendHello();
			}
		}

		private static void HandshakeCallback(IAsyncResult result)
		{
			if (result.CompletedSynchronously)
			{
				return;
			}
			ConnectAndHandshakeAsyncResult connectAndHandshakeAsyncResult = (ConnectAndHandshakeAsyncResult)result.AsyncState;
			try
			{
				try
				{
					LineInfo lineInfo = connectAndHandshakeAsyncResult._connection.Reader.CurrentReader.EndReadLine(result);
					if (lineInfo.StatusCode != SmtpStatusCode.ServiceReady)
					{
						connectAndHandshakeAsyncResult.InvokeCallback(new SmtpException(lineInfo.StatusCode, lineInfo.Line, serverResponse: true));
					}
					else if (connectAndHandshakeAsyncResult.SendEHello())
					{
					}
				}
				catch (SmtpException)
				{
					if (connectAndHandshakeAsyncResult.SendHello())
					{
					}
				}
			}
			catch (Exception result2)
			{
				connectAndHandshakeAsyncResult.InvokeCallback(result2);
			}
		}

		private bool SendEHello()
		{
			IAsyncResult asyncResult = EHelloCommand.BeginSend(_connection, _connection._client._clientDomain, s_sendEHelloCallback, this);
			if (asyncResult.CompletedSynchronously)
			{
				_connection._extensions = EHelloCommand.EndSend(asyncResult);
				_connection.ParseExtensions(_connection._extensions);
				if (_connection._networkStream is System.Net.TlsStream)
				{
					Authenticate();
					return true;
				}
				if (_connection.EnableSsl)
				{
					if (!_connection._serverSupportsStartTls && !(_connection._networkStream is System.Net.TlsStream))
					{
						throw new SmtpException(System.SR.MailServerDoesNotSupportStartTls);
					}
					SendStartTls();
				}
				else
				{
					Authenticate();
				}
				return true;
			}
			return false;
		}

		private static void SendEHelloCallback(IAsyncResult result)
		{
			if (result.CompletedSynchronously)
			{
				return;
			}
			ConnectAndHandshakeAsyncResult connectAndHandshakeAsyncResult = (ConnectAndHandshakeAsyncResult)result.AsyncState;
			try
			{
				try
				{
					connectAndHandshakeAsyncResult._connection._extensions = EHelloCommand.EndSend(result);
					connectAndHandshakeAsyncResult._connection.ParseExtensions(connectAndHandshakeAsyncResult._connection._extensions);
					if (connectAndHandshakeAsyncResult._connection._networkStream is System.Net.TlsStream)
					{
						connectAndHandshakeAsyncResult.Authenticate();
						return;
					}
				}
				catch (SmtpException ex)
				{
					if (ex.StatusCode != SmtpStatusCode.CommandUnrecognized && ex.StatusCode != SmtpStatusCode.CommandNotImplemented)
					{
						throw;
					}
					if (!connectAndHandshakeAsyncResult.SendHello())
					{
						return;
					}
				}
				if (connectAndHandshakeAsyncResult._connection.EnableSsl)
				{
					if (!connectAndHandshakeAsyncResult._connection._serverSupportsStartTls && !(connectAndHandshakeAsyncResult._connection._networkStream is System.Net.TlsStream))
					{
						throw new SmtpException(System.SR.MailServerDoesNotSupportStartTls);
					}
					connectAndHandshakeAsyncResult.SendStartTls();
				}
				else
				{
					connectAndHandshakeAsyncResult.Authenticate();
				}
			}
			catch (Exception result2)
			{
				connectAndHandshakeAsyncResult.InvokeCallback(result2);
			}
		}

		private bool SendHello()
		{
			IAsyncResult asyncResult = HelloCommand.BeginSend(_connection, _connection._client._clientDomain, s_sendHelloCallback, this);
			if (asyncResult.CompletedSynchronously)
			{
				_connection._supportedAuth = SupportedAuth.Login;
				HelloCommand.EndSend(asyncResult);
				Authenticate();
				return true;
			}
			return false;
		}

		private static void SendHelloCallback(IAsyncResult result)
		{
			if (!result.CompletedSynchronously)
			{
				ConnectAndHandshakeAsyncResult connectAndHandshakeAsyncResult = (ConnectAndHandshakeAsyncResult)result.AsyncState;
				try
				{
					HelloCommand.EndSend(result);
					connectAndHandshakeAsyncResult.Authenticate();
				}
				catch (Exception result2)
				{
					connectAndHandshakeAsyncResult.InvokeCallback(result2);
				}
			}
		}

		private bool SendStartTls()
		{
			IAsyncResult asyncResult = StartTlsCommand.BeginSend(_connection, SendStartTlsCallback, this);
			if (asyncResult.CompletedSynchronously)
			{
				StartTlsCommand.EndSend(asyncResult);
				TlsStreamAuthenticate();
				return true;
			}
			return false;
		}

		private static void SendStartTlsCallback(IAsyncResult result)
		{
			if (!result.CompletedSynchronously)
			{
				ConnectAndHandshakeAsyncResult connectAndHandshakeAsyncResult = (ConnectAndHandshakeAsyncResult)result.AsyncState;
				try
				{
					StartTlsCommand.EndSend(result);
					connectAndHandshakeAsyncResult.TlsStreamAuthenticate();
				}
				catch (Exception result2)
				{
					connectAndHandshakeAsyncResult.InvokeCallback(result2);
				}
			}
		}

		private bool TlsStreamAuthenticate()
		{
			_connection._networkStream = new System.Net.TlsStream(_connection._networkStream, _connection._tcpClient.Client, _host, _connection._clientCertificates);
			IAsyncResult asyncResult = ((System.Net.TlsStream)_connection._networkStream).BeginAuthenticateAsClient(TlsStreamAuthenticateCallback, this);
			if (asyncResult.CompletedSynchronously)
			{
				((System.Net.TlsStream)_connection._networkStream).EndAuthenticateAsClient(asyncResult);
				_connection._responseReader = new SmtpReplyReaderFactory(_connection._networkStream);
				SendEHello();
				return true;
			}
			return false;
		}

		private static void TlsStreamAuthenticateCallback(IAsyncResult result)
		{
			if (!result.CompletedSynchronously)
			{
				ConnectAndHandshakeAsyncResult connectAndHandshakeAsyncResult = (ConnectAndHandshakeAsyncResult)result.AsyncState;
				try
				{
					(connectAndHandshakeAsyncResult._connection._networkStream as System.Net.TlsStream).EndAuthenticateAsClient(result);
					connectAndHandshakeAsyncResult._connection._responseReader = new SmtpReplyReaderFactory(connectAndHandshakeAsyncResult._connection._networkStream);
					connectAndHandshakeAsyncResult.SendEHello();
				}
				catch (Exception result2)
				{
					connectAndHandshakeAsyncResult.InvokeCallback(result2);
				}
			}
		}

		private void Authenticate()
		{
			if (_connection._credentials != null)
			{
				while (++_currentModule < _connection._authenticationModules.Length)
				{
					ISmtpAuthenticationModule smtpAuthenticationModule = _connection._authenticationModules[_currentModule];
					if (!_connection.AuthSupported(smtpAuthenticationModule))
					{
						continue;
					}
					NetworkCredential credential = _connection._credentials.GetCredential(_host, _port, smtpAuthenticationModule.AuthenticationType);
					if (credential == null)
					{
						continue;
					}
					Authorization authorization = _connection.SetContextAndTryAuthenticate(smtpAuthenticationModule, credential, _outerResult);
					if (authorization == null || authorization.Message == null)
					{
						continue;
					}
					IAsyncResult asyncResult = AuthCommand.BeginSend(_connection, _connection._authenticationModules[_currentModule].AuthenticationType, authorization.Message, s_authenticateCallback, this);
					if (!asyncResult.CompletedSynchronously)
					{
						return;
					}
					LineInfo lineInfo = AuthCommand.EndSend(asyncResult);
					if (lineInfo.StatusCode == (SmtpStatusCode)334)
					{
						_authResponse = lineInfo.Line;
						if (!AuthenticateContinue())
						{
							return;
						}
					}
					else if (lineInfo.StatusCode == (SmtpStatusCode)235)
					{
						smtpAuthenticationModule.CloseContext(_connection);
						_connection._isConnected = true;
						break;
					}
				}
			}
			_connection._isConnected = true;
			InvokeCallback();
		}

		private static void AuthenticateCallback(IAsyncResult result)
		{
			if (result.CompletedSynchronously)
			{
				return;
			}
			ConnectAndHandshakeAsyncResult connectAndHandshakeAsyncResult = (ConnectAndHandshakeAsyncResult)result.AsyncState;
			try
			{
				LineInfo lineInfo = AuthCommand.EndSend(result);
				if (lineInfo.StatusCode == (SmtpStatusCode)334)
				{
					connectAndHandshakeAsyncResult._authResponse = lineInfo.Line;
					if (!connectAndHandshakeAsyncResult.AuthenticateContinue())
					{
						return;
					}
				}
				else if (lineInfo.StatusCode == (SmtpStatusCode)235)
				{
					connectAndHandshakeAsyncResult._connection._authenticationModules[connectAndHandshakeAsyncResult._currentModule].CloseContext(connectAndHandshakeAsyncResult._connection);
					connectAndHandshakeAsyncResult._connection._isConnected = true;
					connectAndHandshakeAsyncResult.InvokeCallback();
					return;
				}
				connectAndHandshakeAsyncResult.Authenticate();
			}
			catch (Exception result2)
			{
				connectAndHandshakeAsyncResult.InvokeCallback(result2);
			}
		}

		private bool AuthenticateContinue()
		{
			while (true)
			{
				Authorization authorization = _connection._authenticationModules[_currentModule].Authenticate(_authResponse, null, _connection, _connection._client.TargetName, null);
				if (authorization == null)
				{
					throw new SmtpException(System.SR.SmtpAuthenticationFailed);
				}
				IAsyncResult asyncResult = AuthCommand.BeginSend(_connection, authorization.Message, s_authenticateContinueCallback, this);
				if (!asyncResult.CompletedSynchronously)
				{
					return false;
				}
				LineInfo lineInfo = AuthCommand.EndSend(asyncResult);
				if (lineInfo.StatusCode == (SmtpStatusCode)235)
				{
					_connection._authenticationModules[_currentModule].CloseContext(_connection);
					_connection._isConnected = true;
					InvokeCallback();
					return false;
				}
				if (lineInfo.StatusCode != (SmtpStatusCode)334)
				{
					break;
				}
				_authResponse = lineInfo.Line;
			}
			return true;
		}

		private static void AuthenticateContinueCallback(IAsyncResult result)
		{
			if (result.CompletedSynchronously)
			{
				return;
			}
			ConnectAndHandshakeAsyncResult connectAndHandshakeAsyncResult = (ConnectAndHandshakeAsyncResult)result.AsyncState;
			try
			{
				LineInfo lineInfo = AuthCommand.EndSend(result);
				if (lineInfo.StatusCode == (SmtpStatusCode)235)
				{
					connectAndHandshakeAsyncResult._connection._authenticationModules[connectAndHandshakeAsyncResult._currentModule].CloseContext(connectAndHandshakeAsyncResult._connection);
					connectAndHandshakeAsyncResult._connection._isConnected = true;
					connectAndHandshakeAsyncResult.InvokeCallback();
					return;
				}
				if (lineInfo.StatusCode == (SmtpStatusCode)334)
				{
					connectAndHandshakeAsyncResult._authResponse = lineInfo.Line;
					if (!connectAndHandshakeAsyncResult.AuthenticateContinue())
					{
						return;
					}
				}
				connectAndHandshakeAsyncResult.Authenticate();
			}
			catch (Exception result2)
			{
				connectAndHandshakeAsyncResult.InvokeCallback(result2);
			}
		}
	}

	private static readonly ContextCallback s_AuthenticateCallback = AuthenticateCallback;

	private readonly BufferBuilder _bufferBuilder = new BufferBuilder();

	private bool _isConnected;

	private bool _isClosed;

	private bool _isStreamOpen;

	private readonly EventHandler _onCloseHandler;

	internal SmtpTransport _parent;

	private readonly SmtpClient _client;

	private NetworkStream _networkStream;

	internal TcpClient _tcpClient;

	private SmtpReplyReaderFactory _responseReader;

	private readonly ICredentialsByHost _credentials;

	private string[] _extensions;

	private bool _enableSsl;

	private X509CertificateCollection _clientCertificates;

	private bool _serverSupportsEai;

	private bool _dsnEnabled;

	private bool _serverSupportsStartTls;

	private bool _sawNegotiate;

	private SupportedAuth _supportedAuth;

	private readonly ISmtpAuthenticationModule[] _authenticationModules;

	private static readonly char[] s_authExtensionSplitters = new char[2] { ' ', '=' };

	internal BufferBuilder BufferBuilder => _bufferBuilder;

	internal bool IsConnected => _isConnected;

	internal bool IsStreamOpen => _isStreamOpen;

	internal SmtpReplyReaderFactory Reader => _responseReader;

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

	internal X509CertificateCollection ClientCertificates
	{
		set
		{
			_clientCertificates = value;
		}
	}

	internal bool DSNEnabled => _dsnEnabled;

	internal bool ServerSupportsEai => _serverSupportsEai;

	internal SmtpConnection(SmtpTransport parent, SmtpClient client, ICredentialsByHost credentials, ISmtpAuthenticationModule[] authenticationModules)
	{
		_client = client;
		_credentials = credentials;
		_authenticationModules = authenticationModules;
		_parent = parent;
		_tcpClient = new TcpClient();
		_onCloseHandler = OnClose;
	}

	internal void InitializeConnection(string host, int port)
	{
		_tcpClient.Connect(host, port);
		_networkStream = _tcpClient.GetStream();
	}

	internal IAsyncResult BeginInitializeConnection(string host, int port, AsyncCallback callback, object state)
	{
		return _tcpClient.BeginConnect(host, port, callback, state);
	}

	internal void EndInitializeConnection(IAsyncResult result)
	{
		_tcpClient.EndConnect(result);
		_networkStream = _tcpClient.GetStream();
	}

	internal IAsyncResult BeginGetConnection(System.Net.ContextAwareResult outerResult, AsyncCallback callback, object state, string host, int port)
	{
		ConnectAndHandshakeAsyncResult connectAndHandshakeAsyncResult = new ConnectAndHandshakeAsyncResult(this, host, port, outerResult, callback, state);
		connectAndHandshakeAsyncResult.GetConnection();
		return connectAndHandshakeAsyncResult;
	}

	internal IAsyncResult BeginFlush(AsyncCallback callback, object state)
	{
		return _networkStream.BeginWrite(_bufferBuilder.GetBuffer(), 0, _bufferBuilder.Length, callback, state);
	}

	internal void EndFlush(IAsyncResult result)
	{
		_networkStream.EndWrite(result);
		_bufferBuilder.Reset();
	}

	internal void Flush()
	{
		_networkStream.Write(_bufferBuilder.GetBuffer(), 0, _bufferBuilder.Length);
		_bufferBuilder.Reset();
	}

	private void ShutdownConnection(bool isAbort)
	{
		if (!_isClosed)
		{
			lock (this)
			{
				if (!_isClosed && _tcpClient != null)
				{
					try
					{
						try
						{
							if (isAbort)
							{
								_tcpClient.LingerState = new LingerOption(enable: true, 0);
							}
							else
							{
								_tcpClient.Client.Blocking = false;
								QuitCommand.Send(this);
							}
						}
						finally
						{
							_networkStream?.Close();
							_tcpClient.Dispose();
						}
					}
					catch (IOException)
					{
					}
					catch (ObjectDisposedException)
					{
					}
				}
				_isClosed = true;
			}
		}
		_isConnected = false;
	}

	internal void ReleaseConnection()
	{
		ShutdownConnection(isAbort: false);
	}

	internal void Abort()
	{
		ShutdownConnection(isAbort: true);
	}

	internal void GetConnection(string host, int port)
	{
		if (_isConnected)
		{
			throw new InvalidOperationException(System.SR.SmtpAlreadyConnected);
		}
		InitializeConnection(host, port);
		_responseReader = new SmtpReplyReaderFactory(_networkStream);
		LineInfo lineInfo = _responseReader.GetNextReplyReader().ReadLine();
		SmtpStatusCode statusCode = lineInfo.StatusCode;
		if (statusCode != SmtpStatusCode.ServiceReady)
		{
			throw new SmtpException(lineInfo.StatusCode, lineInfo.Line, serverResponse: true);
		}
		try
		{
			_extensions = EHelloCommand.Send(this, _client._clientDomain);
			ParseExtensions(_extensions);
		}
		catch (SmtpException ex)
		{
			if (ex.StatusCode != SmtpStatusCode.CommandUnrecognized && ex.StatusCode != SmtpStatusCode.CommandNotImplemented)
			{
				throw;
			}
			HelloCommand.Send(this, _client._clientDomain);
			_supportedAuth = SupportedAuth.Login;
		}
		if (_enableSsl)
		{
			if (!_serverSupportsStartTls && !(_networkStream is System.Net.TlsStream))
			{
				throw new SmtpException(System.SR.MailServerDoesNotSupportStartTls);
			}
			StartTlsCommand.Send(this);
			System.Net.TlsStream tlsStream = new System.Net.TlsStream(_networkStream, _tcpClient.Client, host, _clientCertificates);
			tlsStream.AuthenticateAsClient();
			_networkStream = tlsStream;
			_responseReader = new SmtpReplyReaderFactory(_networkStream);
			_extensions = EHelloCommand.Send(this, _client._clientDomain);
			ParseExtensions(_extensions);
		}
		if (_credentials != null)
		{
			for (int i = 0; i < _authenticationModules.Length; i++)
			{
				if (!AuthSupported(_authenticationModules[i]))
				{
					continue;
				}
				NetworkCredential credential = _credentials.GetCredential(host, port, _authenticationModules[i].AuthenticationType);
				if (credential == null)
				{
					continue;
				}
				Authorization authorization = SetContextAndTryAuthenticate(_authenticationModules[i], credential, null);
				if (authorization == null || authorization.Message == null)
				{
					continue;
				}
				lineInfo = AuthCommand.Send(this, _authenticationModules[i].AuthenticationType, authorization.Message);
				if (lineInfo.StatusCode == SmtpStatusCode.CommandParameterNotImplemented)
				{
					continue;
				}
				while (lineInfo.StatusCode == (SmtpStatusCode)334)
				{
					authorization = _authenticationModules[i].Authenticate(lineInfo.Line, null, this, _client.TargetName, null);
					if (authorization == null)
					{
						throw new SmtpException(System.SR.SmtpAuthenticationFailed);
					}
					lineInfo = AuthCommand.Send(this, authorization.Message);
					if (lineInfo.StatusCode == (SmtpStatusCode)235)
					{
						_authenticationModules[i].CloseContext(this);
						_isConnected = true;
						return;
					}
				}
			}
		}
		_isConnected = true;
	}

	private Authorization SetContextAndTryAuthenticate(ISmtpAuthenticationModule module, NetworkCredential credential, System.Net.ContextAwareResult context)
	{
		if (credential == CredentialCache.DefaultNetworkCredentials)
		{
			try
			{
				ExecutionContext executionContext = context?.ContextCopy;
				if (executionContext != null)
				{
					AuthenticateCallbackContext authenticateCallbackContext = new AuthenticateCallbackContext(this, module, credential, _client.TargetName, null);
					ExecutionContext.Run(executionContext, s_AuthenticateCallback, authenticateCallbackContext);
					return authenticateCallbackContext._result;
				}
				return module.Authenticate(null, credential, this, _client.TargetName, null);
			}
			catch
			{
				throw;
			}
		}
		return module.Authenticate(null, credential, this, _client.TargetName, null);
	}

	private static void AuthenticateCallback(object state)
	{
		AuthenticateCallbackContext authenticateCallbackContext = (AuthenticateCallbackContext)state;
		authenticateCallbackContext._result = authenticateCallbackContext._module.Authenticate(null, authenticateCallbackContext._credential, authenticateCallbackContext._thisPtr, authenticateCallbackContext._spn, authenticateCallbackContext._token);
	}

	internal void EndGetConnection(IAsyncResult result)
	{
		ConnectAndHandshakeAsyncResult.End(result);
	}

	internal Stream GetClosableStream()
	{
		ClosableStream result = new ClosableStream(_networkStream, _onCloseHandler);
		_isStreamOpen = true;
		return result;
	}

	private void OnClose(object sender, EventArgs args)
	{
		_isStreamOpen = false;
		DataStopCommand.Send(this);
	}

	internal void ParseExtensions(string[] extensions)
	{
		_supportedAuth = SupportedAuth.None;
		foreach (string text in extensions)
		{
			if (string.Compare(text, 0, "auth", 0, 4, StringComparison.OrdinalIgnoreCase) == 0)
			{
				string[] array = text.Remove(0, 4).Split(s_authExtensionSplitters, StringSplitOptions.RemoveEmptyEntries);
				string[] array2 = array;
				foreach (string a in array2)
				{
					if (string.Equals(a, "login", StringComparison.OrdinalIgnoreCase))
					{
						_supportedAuth |= SupportedAuth.Login;
					}
					else if (string.Equals(a, "ntlm", StringComparison.OrdinalIgnoreCase))
					{
						_supportedAuth |= SupportedAuth.NTLM;
					}
					else if (string.Equals(a, "gssapi", StringComparison.OrdinalIgnoreCase))
					{
						_supportedAuth |= SupportedAuth.GSSAPI;
					}
				}
			}
			else if (string.Compare(text, 0, "dsn ", 0, 3, StringComparison.OrdinalIgnoreCase) == 0)
			{
				_dsnEnabled = true;
			}
			else if (string.Compare(text, 0, "STARTTLS", 0, 8, StringComparison.OrdinalIgnoreCase) == 0)
			{
				_serverSupportsStartTls = true;
			}
			else if (string.Compare(text, 0, "SMTPUTF8", 0, 8, StringComparison.OrdinalIgnoreCase) == 0)
			{
				_serverSupportsEai = true;
			}
		}
	}

	internal bool AuthSupported(ISmtpAuthenticationModule module)
	{
		if (module is SmtpLoginAuthenticationModule)
		{
			if ((_supportedAuth & SupportedAuth.Login) > SupportedAuth.None)
			{
				return true;
			}
		}
		else if (module is SmtpNegotiateAuthenticationModule)
		{
			if ((_supportedAuth & SupportedAuth.GSSAPI) > SupportedAuth.None)
			{
				_sawNegotiate = true;
				return true;
			}
		}
		else if (module is SmtpNtlmAuthenticationModule && !_sawNegotiate && (_supportedAuth & SupportedAuth.NTLM) > SupportedAuth.None)
		{
			return true;
		}
		return false;
	}
}
