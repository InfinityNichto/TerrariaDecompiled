using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Net.Sockets;
using System.Text;

namespace System.Net;

internal sealed class FtpControlStream : CommandStream
{
	private enum GetPathOption
	{
		Normal,
		AssumeFilename,
		AssumeNoFilename
	}

	private Socket _dataSocket;

	private IPEndPoint _passiveEndPoint;

	private TlsStream _tlsStream;

	private StringBuilder _bannerMessage;

	private StringBuilder _welcomeMessage;

	private StringBuilder _exitMessage;

	private WeakReference _credentials;

	private string _currentTypeSetting = string.Empty;

	private long _contentLength = -1L;

	private DateTime _lastModified;

	private bool _dataHandshakeStarted;

	private string _loginDirectory;

	private string _establishedServerDirectory;

	private string _requestedServerDirectory;

	private Uri _responseUri;

	private FtpLoginState _loginState;

	internal FtpStatusCode StatusCode;

	internal string StatusLine;

	private static readonly AsyncCallback s_acceptCallbackDelegate = AcceptCallback;

	private static readonly AsyncCallback s_connectCallbackDelegate = ConnectCallback;

	private static readonly AsyncCallback s_SSLHandshakeCallback = SSLHandshakeCallback;

	internal NetworkCredential Credentials
	{
		get
		{
			if (_credentials != null && _credentials.IsAlive)
			{
				return (NetworkCredential)_credentials.Target;
			}
			return null;
		}
		set
		{
			if (_credentials == null)
			{
				_credentials = new WeakReference(null);
			}
			_credentials.Target = value;
		}
	}

	internal long ContentLength => _contentLength;

	internal DateTime LastModified => _lastModified;

	internal Uri ResponseUri => _responseUri;

	internal string BannerMessage
	{
		get
		{
			if (_bannerMessage == null)
			{
				return null;
			}
			return _bannerMessage.ToString();
		}
	}

	internal string WelcomeMessage
	{
		get
		{
			if (_welcomeMessage == null)
			{
				return null;
			}
			return _welcomeMessage.ToString();
		}
	}

	internal string ExitMessage
	{
		get
		{
			if (_exitMessage == null)
			{
				return null;
			}
			return _exitMessage.ToString();
		}
	}

	internal FtpControlStream(TcpClient client)
		: base(client)
	{
	}

	internal void AbortConnect()
	{
		Socket dataSocket = _dataSocket;
		if (dataSocket != null)
		{
			try
			{
				dataSocket.Close();
			}
			catch (ObjectDisposedException)
			{
			}
		}
	}

	private static void AcceptCallback(IAsyncResult asyncResult)
	{
		FtpControlStream ftpControlStream = (FtpControlStream)asyncResult.AsyncState;
		Socket dataSocket = ftpControlStream._dataSocket;
		try
		{
			ftpControlStream._dataSocket = dataSocket.EndAccept(asyncResult);
			if (!ftpControlStream.ServerAddress.Equals(((IPEndPoint)ftpControlStream._dataSocket.RemoteEndPoint).Address))
			{
				ftpControlStream._dataSocket.Close();
				throw new WebException(System.SR.net_ftp_active_address_different, WebExceptionStatus.ProtocolError);
			}
			ftpControlStream.ContinueCommandPipeline();
		}
		catch (Exception obj)
		{
			ftpControlStream.CloseSocket();
			ftpControlStream.InvokeRequestCallback(obj);
		}
		finally
		{
			dataSocket.Close();
		}
	}

	private static void ConnectCallback(IAsyncResult asyncResult)
	{
		FtpControlStream ftpControlStream = (FtpControlStream)asyncResult.AsyncState;
		try
		{
			ftpControlStream._dataSocket.EndConnect(asyncResult);
			ftpControlStream.ContinueCommandPipeline();
		}
		catch (Exception obj)
		{
			ftpControlStream.CloseSocket();
			ftpControlStream.InvokeRequestCallback(obj);
		}
	}

	private static void SSLHandshakeCallback(IAsyncResult asyncResult)
	{
		FtpControlStream ftpControlStream = (FtpControlStream)asyncResult.AsyncState;
		try
		{
			ftpControlStream._tlsStream.EndAuthenticateAsClient(asyncResult);
			ftpControlStream.ContinueCommandPipeline();
		}
		catch (Exception obj)
		{
			ftpControlStream.CloseSocket();
			ftpControlStream.InvokeRequestCallback(obj);
		}
	}

	private PipelineInstruction QueueOrCreateFtpDataStream(ref Stream stream)
	{
		if (_dataSocket == null)
		{
			throw new System.Net.InternalException();
		}
		if (_tlsStream != null)
		{
			stream = new FtpDataStream(_tlsStream, (FtpWebRequest)_request, IsFtpDataStreamWriteable());
			_tlsStream = null;
			return PipelineInstruction.GiveStream;
		}
		NetworkStream networkStream = new NetworkStream(_dataSocket, ownsSocket: true);
		if (base.UsingSecureStream)
		{
			FtpWebRequest ftpWebRequest = (FtpWebRequest)_request;
			TlsStream tlsStream = new TlsStream(networkStream, _dataSocket, ftpWebRequest.RequestUri.Host, ftpWebRequest.ClientCertificates);
			networkStream = tlsStream;
			if (_isAsync)
			{
				_tlsStream = tlsStream;
				tlsStream.BeginAuthenticateAsClient(s_SSLHandshakeCallback, this);
				return PipelineInstruction.Pause;
			}
			tlsStream.AuthenticateAsClient();
		}
		stream = new FtpDataStream(networkStream, (FtpWebRequest)_request, IsFtpDataStreamWriteable());
		return PipelineInstruction.GiveStream;
	}

	protected override void ClearState()
	{
		_contentLength = -1L;
		_lastModified = DateTime.MinValue;
		_responseUri = null;
		_dataHandshakeStarted = false;
		StatusCode = FtpStatusCode.Undefined;
		StatusLine = null;
		_dataSocket = null;
		_passiveEndPoint = null;
		_tlsStream = null;
		base.ClearState();
	}

	protected override PipelineInstruction PipelineCallback(PipelineEntry entry, ResponseDescription response, bool timeout, ref Stream stream)
	{
		if (System.Net.NetEventSource.Log.IsEnabled())
		{
			System.Net.NetEventSource.Info(this, $"Command:{entry?.Command} Description:{response?.StatusDescription}", "PipelineCallback");
		}
		if (response == null)
		{
			return PipelineInstruction.Abort;
		}
		FtpStatusCode status = (FtpStatusCode)response.Status;
		if (status != FtpStatusCode.ClosingControl)
		{
			StatusCode = status;
			StatusLine = response.StatusDescription;
		}
		if (response.InvalidStatusCode)
		{
			throw new WebException(System.SR.net_InvalidStatusCode, WebExceptionStatus.ProtocolError);
		}
		if (_index == -1)
		{
			switch (status)
			{
			case FtpStatusCode.SendUserCommand:
				_bannerMessage = new StringBuilder();
				_bannerMessage.Append(StatusLine);
				return PipelineInstruction.Advance;
			case FtpStatusCode.ServiceTemporarilyNotAvailable:
				return PipelineInstruction.Reread;
			default:
				throw GenerateException(status, response.StatusDescription, null);
			}
		}
		if (entry.Command == "OPTS utf8 on\r\n")
		{
			if (response.PositiveCompletion)
			{
				base.Encoding = Encoding.UTF8;
			}
			else
			{
				base.Encoding = Encoding.Default;
			}
			return PipelineInstruction.Advance;
		}
		if (entry.Command.IndexOf("USER", StringComparison.Ordinal) != -1 && status == FtpStatusCode.LoggedInProceed)
		{
			_loginState = FtpLoginState.LoggedIn;
			_index++;
		}
		if (response.TransientFailure || response.PermanentFailure)
		{
			if (status == FtpStatusCode.ServiceNotAvailable)
			{
				MarkAsRecoverableFailure();
			}
			throw GenerateException(status, response.StatusDescription, null);
		}
		if (_loginState != FtpLoginState.LoggedIn && entry.Command.IndexOf("PASS", StringComparison.Ordinal) != -1)
		{
			if (status != FtpStatusCode.NeedLoginAccount && status != FtpStatusCode.LoggedInProceed)
			{
				throw GenerateException(status, response.StatusDescription, null);
			}
			_loginState = FtpLoginState.LoggedIn;
		}
		if (entry.HasFlag(PipelineEntryFlags.CreateDataConnection) && (response.PositiveCompletion || response.PositiveIntermediate))
		{
			bool isSocketReady;
			PipelineInstruction result = QueueOrCreateDataConection(entry, response, timeout, ref stream, out isSocketReady);
			if (!isSocketReady)
			{
				return result;
			}
		}
		switch (status)
		{
		case FtpStatusCode.DataAlreadyOpen:
		case FtpStatusCode.OpeningData:
		{
			if (_dataSocket == null)
			{
				return PipelineInstruction.Abort;
			}
			if (!entry.HasFlag(PipelineEntryFlags.GiveDataStream))
			{
				_abortReason = System.SR.Format(System.SR.net_ftp_invalid_status_response, status, entry.Command);
				return PipelineInstruction.Abort;
			}
			TryUpdateContentLength(response.StatusDescription);
			FtpWebRequest ftpWebRequest = (FtpWebRequest)_request;
			if (ftpWebRequest.MethodInfo.ShouldParseForResponseUri)
			{
				TryUpdateResponseUri(response.StatusDescription, ftpWebRequest);
			}
			return QueueOrCreateFtpDataStream(ref stream);
		}
		case FtpStatusCode.LoggedInProceed:
			_welcomeMessage.Append(StatusLine);
			break;
		case FtpStatusCode.ClosingControl:
			_exitMessage.Append(response.StatusDescription);
			CloseSocket();
			break;
		case FtpStatusCode.ServerWantsSecureSession:
		{
			if (base.NetworkStream is TlsStream)
			{
				break;
			}
			FtpWebRequest ftpWebRequest2 = (FtpWebRequest)_request;
			TlsStream tlsStream = new TlsStream(base.NetworkStream, base.Socket, ftpWebRequest2.RequestUri.Host, ftpWebRequest2.ClientCertificates);
			if (_isAsync)
			{
				tlsStream.BeginAuthenticateAsClient(delegate(IAsyncResult ar)
				{
					try
					{
						tlsStream.EndAuthenticateAsClient(ar);
						base.NetworkStream = tlsStream;
						ContinueCommandPipeline();
					}
					catch (Exception obj)
					{
						CloseSocket();
						InvokeRequestCallback(obj);
					}
				}, null);
				return PipelineInstruction.Pause;
			}
			tlsStream.AuthenticateAsClient();
			base.NetworkStream = tlsStream;
			break;
		}
		case FtpStatusCode.FileStatus:
			if (entry.Command.StartsWith("SIZE ", StringComparison.Ordinal))
			{
				_contentLength = GetContentLengthFrom213Response(response.StatusDescription);
			}
			else if (entry.Command.StartsWith("MDTM ", StringComparison.Ordinal))
			{
				_lastModified = GetLastModifiedFrom213Response(response.StatusDescription);
			}
			break;
		case FtpStatusCode.PathnameCreated:
			if (entry.Command == "PWD\r\n" && !entry.HasFlag(PipelineEntryFlags.UserCommand))
			{
				_loginDirectory = GetLoginDirectory(response.StatusDescription);
			}
			break;
		default:
			if (entry.Command.IndexOf("CWD", StringComparison.Ordinal) != -1)
			{
				_establishedServerDirectory = _requestedServerDirectory;
			}
			break;
		}
		if (response.PositiveIntermediate || (!base.UsingSecureStream && entry.Command == "AUTH TLS\r\n"))
		{
			return PipelineInstruction.Reread;
		}
		return PipelineInstruction.Advance;
	}

	protected override PipelineEntry[] BuildCommandsList(WebRequest req)
	{
		bool flag = false;
		FtpWebRequest ftpWebRequest = (FtpWebRequest)req;
		if (System.Net.NetEventSource.Log.IsEnabled())
		{
			System.Net.NetEventSource.Info(this, null, "BuildCommandsList");
		}
		_responseUri = ftpWebRequest.RequestUri;
		List<PipelineEntry> list = new List<PipelineEntry>();
		if (ftpWebRequest.EnableSsl && !base.UsingSecureStream)
		{
			list.Add(new PipelineEntry(FormatFtpCommand("AUTH", "TLS")));
			flag = true;
		}
		if (flag)
		{
			_loginDirectory = null;
			_establishedServerDirectory = null;
			_requestedServerDirectory = null;
			_currentTypeSetting = string.Empty;
			if (_loginState == FtpLoginState.LoggedIn)
			{
				_loginState = FtpLoginState.LoggedInButNeedsRelogin;
			}
		}
		if (_loginState != FtpLoginState.LoggedIn)
		{
			Credentials = ftpWebRequest.Credentials.GetCredential(ftpWebRequest.RequestUri, "basic");
			_welcomeMessage = new StringBuilder();
			_exitMessage = new StringBuilder();
			string text = string.Empty;
			string text2 = string.Empty;
			if (Credentials != null)
			{
				text = Credentials.UserName;
				string domain = Credentials.Domain;
				if (!string.IsNullOrEmpty(domain))
				{
					text = domain + "\\" + text;
				}
				text2 = Credentials.Password;
			}
			if (text.Length == 0 && text2.Length == 0)
			{
				text = "anonymous";
				text2 = "anonymous@";
			}
			list.Add(new PipelineEntry(FormatFtpCommand("USER", text)));
			list.Add(new PipelineEntry(FormatFtpCommand("PASS", text2), PipelineEntryFlags.DontLogParameter));
			if (ftpWebRequest.EnableSsl && !base.UsingSecureStream)
			{
				list.Add(new PipelineEntry(FormatFtpCommand("PBSZ", "0")));
				list.Add(new PipelineEntry(FormatFtpCommand("PROT", "P")));
			}
			list.Add(new PipelineEntry(FormatFtpCommand("OPTS", "utf8 on")));
			list.Add(new PipelineEntry(FormatFtpCommand("PWD", null)));
		}
		GetPathOption pathOption = GetPathOption.Normal;
		if (ftpWebRequest.MethodInfo.HasFlag(FtpMethodFlags.DoesNotTakeParameter))
		{
			pathOption = GetPathOption.AssumeNoFilename;
		}
		else if (ftpWebRequest.MethodInfo.HasFlag(FtpMethodFlags.ParameterIsDirectory))
		{
			pathOption = GetPathOption.AssumeFilename;
		}
		GetPathInfo(pathOption, ftpWebRequest.RequestUri, out var path, out var directory, out var filename);
		if (filename.Length == 0 && ftpWebRequest.MethodInfo.HasFlag(FtpMethodFlags.TakesParameter))
		{
			throw new WebException(System.SR.net_ftp_invalid_uri);
		}
		if (_establishedServerDirectory != null && _loginDirectory != null && _establishedServerDirectory != _loginDirectory)
		{
			list.Add(new PipelineEntry(FormatFtpCommand("CWD", _loginDirectory), PipelineEntryFlags.UserCommand));
			_requestedServerDirectory = _loginDirectory;
		}
		if (ftpWebRequest.MethodInfo.HasFlag(FtpMethodFlags.MustChangeWorkingDirectoryToPath) && directory.Length > 0)
		{
			list.Add(new PipelineEntry(FormatFtpCommand("CWD", directory), PipelineEntryFlags.UserCommand));
			_requestedServerDirectory = directory;
		}
		if (!ftpWebRequest.MethodInfo.IsCommandOnly)
		{
			string text3 = (ftpWebRequest.UseBinary ? "I" : "A");
			if (_currentTypeSetting != text3)
			{
				list.Add(new PipelineEntry(FormatFtpCommand("TYPE", text3)));
				_currentTypeSetting = text3;
			}
			if (ftpWebRequest.UsePassive)
			{
				string command = ((base.ServerAddress.AddressFamily == AddressFamily.InterNetwork || base.ServerAddress.IsIPv4MappedToIPv6) ? "PASV" : "EPSV");
				list.Add(new PipelineEntry(FormatFtpCommand(command, null), PipelineEntryFlags.CreateDataConnection));
			}
			else
			{
				string command2 = ((base.ServerAddress.AddressFamily == AddressFamily.InterNetwork || base.ServerAddress.IsIPv4MappedToIPv6) ? "PORT" : "EPRT");
				CreateFtpListenerSocket(ftpWebRequest);
				list.Add(new PipelineEntry(FormatFtpCommand(command2, GetPortCommandLine(ftpWebRequest))));
			}
			if (ftpWebRequest.ContentOffset > 0)
			{
				list.Add(new PipelineEntry(FormatFtpCommand("REST", ftpWebRequest.ContentOffset.ToString(CultureInfo.InvariantCulture))));
			}
		}
		PipelineEntryFlags pipelineEntryFlags = PipelineEntryFlags.UserCommand;
		if (!ftpWebRequest.MethodInfo.IsCommandOnly)
		{
			pipelineEntryFlags |= PipelineEntryFlags.GiveDataStream;
			if (!ftpWebRequest.UsePassive)
			{
				pipelineEntryFlags |= PipelineEntryFlags.CreateDataConnection;
			}
		}
		if (ftpWebRequest.MethodInfo.Operation == FtpOperation.Rename)
		{
			string text4 = ((directory.Length == 0) ? string.Empty : (directory + "/"));
			list.Add(new PipelineEntry(FormatFtpCommand("RNFR", text4 + filename), pipelineEntryFlags));
			string parameter = ((string.IsNullOrEmpty(ftpWebRequest.RenameTo) || !ftpWebRequest.RenameTo.StartsWith("/", StringComparison.OrdinalIgnoreCase)) ? (text4 + ftpWebRequest.RenameTo) : ftpWebRequest.RenameTo);
			list.Add(new PipelineEntry(FormatFtpCommand("RNTO", parameter), pipelineEntryFlags));
		}
		else if (ftpWebRequest.MethodInfo.HasFlag(FtpMethodFlags.DoesNotTakeParameter))
		{
			list.Add(new PipelineEntry(FormatFtpCommand(ftpWebRequest.Method, string.Empty), pipelineEntryFlags));
		}
		else if (ftpWebRequest.MethodInfo.HasFlag(FtpMethodFlags.MustChangeWorkingDirectoryToPath))
		{
			list.Add(new PipelineEntry(FormatFtpCommand(ftpWebRequest.Method, filename), pipelineEntryFlags));
		}
		else
		{
			list.Add(new PipelineEntry(FormatFtpCommand(ftpWebRequest.Method, path), pipelineEntryFlags));
		}
		list.Add(new PipelineEntry(FormatFtpCommand("QUIT", null)));
		return list.ToArray();
	}

	private PipelineInstruction QueueOrCreateDataConection(PipelineEntry entry, ResponseDescription response, bool timeout, ref Stream stream, out bool isSocketReady)
	{
		isSocketReady = false;
		if (_dataHandshakeStarted)
		{
			isSocketReady = true;
			return PipelineInstruction.Pause;
		}
		_dataHandshakeStarted = true;
		bool flag = false;
		int port = -1;
		if (entry.Command == "PASV\r\n" || entry.Command == "EPSV\r\n")
		{
			if (!response.PositiveCompletion)
			{
				_abortReason = System.SR.Format(System.SR.net_ftp_server_failed_passive, response.Status);
				return PipelineInstruction.Abort;
			}
			port = ((!(entry.Command == "PASV\r\n")) ? GetPortV6(response.StatusDescription) : GetPortV4(response.StatusDescription));
			flag = true;
		}
		if (flag)
		{
			try
			{
				_dataSocket = CreateFtpDataSocket((FtpWebRequest)_request, base.Socket);
			}
			catch (ObjectDisposedException)
			{
				throw ExceptionHelper.RequestAbortedException;
			}
			IPEndPoint localEP = new IPEndPoint(((IPEndPoint)base.Socket.LocalEndPoint).Address, 0);
			_dataSocket.Bind(localEP);
			_passiveEndPoint = new IPEndPoint(base.ServerAddress, port);
		}
		if (_passiveEndPoint != null)
		{
			IPEndPoint passiveEndPoint = _passiveEndPoint;
			_passiveEndPoint = null;
			if (System.Net.NetEventSource.Log.IsEnabled())
			{
				System.Net.NetEventSource.Info(this, "starting Connect()", "QueueOrCreateDataConection");
			}
			if (_isAsync)
			{
				_dataSocket.BeginConnect(passiveEndPoint, s_connectCallbackDelegate, this);
				return PipelineInstruction.Pause;
			}
			_dataSocket.Connect(passiveEndPoint);
			return PipelineInstruction.Advance;
		}
		if (System.Net.NetEventSource.Log.IsEnabled())
		{
			System.Net.NetEventSource.Info(this, "starting Accept()", "QueueOrCreateDataConection");
		}
		if (_isAsync)
		{
			_dataSocket.BeginAccept(s_acceptCallbackDelegate, this);
			return PipelineInstruction.Pause;
		}
		Socket dataSocket = _dataSocket;
		try
		{
			_dataSocket = _dataSocket.Accept();
			if (!base.ServerAddress.Equals(((IPEndPoint)_dataSocket.RemoteEndPoint).Address))
			{
				_dataSocket.Close();
				throw new WebException(System.SR.net_ftp_active_address_different, WebExceptionStatus.ProtocolError);
			}
			isSocketReady = true;
			return PipelineInstruction.Pause;
		}
		finally
		{
			dataSocket.Close();
		}
	}

	private static void GetPathInfo(GetPathOption pathOption, Uri uri, out string path, out string directory, out string filename)
	{
		path = uri.GetComponents(UriComponents.Path, UriFormat.Unescaped);
		int num = path.LastIndexOf('/');
		if (pathOption == GetPathOption.AssumeFilename && num != -1 && num == path.Length - 1)
		{
			path = path.Substring(0, path.Length - 1);
			num = path.LastIndexOf('/');
		}
		if (pathOption == GetPathOption.AssumeNoFilename)
		{
			directory = path;
			filename = string.Empty;
		}
		else
		{
			directory = path.Substring(0, num + 1);
			filename = path.Substring(num + 1, path.Length - (num + 1));
		}
		if (directory.Length > 1 && directory[directory.Length - 1] == '/')
		{
			directory = directory.Substring(0, directory.Length - 1);
		}
	}

	private string FormatAddress(IPAddress address, int Port)
	{
		byte[] addressBytes = address.GetAddressBytes();
		StringBuilder stringBuilder = new StringBuilder(32);
		for (int i = (address.IsIPv4MappedToIPv6 ? 12 : 0); i < addressBytes.Length; i++)
		{
			stringBuilder.Append(addressBytes[i]);
			stringBuilder.Append(',');
		}
		stringBuilder.Append(Port / 256);
		stringBuilder.Append(',');
		stringBuilder.Append(Port % 256);
		return stringBuilder.ToString();
	}

	private string FormatAddressV6(IPAddress address, int port)
	{
		return "|2|" + address.ToString() + "|" + port.ToString(NumberFormatInfo.InvariantInfo) + "|";
	}

	private long GetContentLengthFrom213Response(string responseString)
	{
		string[] array = responseString.Split(' ');
		if (array.Length < 2)
		{
			throw new FormatException(System.SR.Format(System.SR.net_ftp_response_invalid_format, responseString));
		}
		return Convert.ToInt64(array[1], NumberFormatInfo.InvariantInfo);
	}

	private DateTime GetLastModifiedFrom213Response(string str)
	{
		DateTime result = _lastModified;
		string[] array = str.Split(' ', '.');
		if (array.Length < 2)
		{
			return result;
		}
		string text = array[1];
		if (text.Length < 14)
		{
			return result;
		}
		int year = Convert.ToInt32(text.Substring(0, 4), NumberFormatInfo.InvariantInfo);
		int month = Convert.ToInt16(text.Substring(4, 2), NumberFormatInfo.InvariantInfo);
		int day = Convert.ToInt16(text.Substring(6, 2), NumberFormatInfo.InvariantInfo);
		int hour = Convert.ToInt16(text.Substring(8, 2), NumberFormatInfo.InvariantInfo);
		int minute = Convert.ToInt16(text.Substring(10, 2), NumberFormatInfo.InvariantInfo);
		int second = Convert.ToInt16(text.Substring(12, 2), NumberFormatInfo.InvariantInfo);
		int millisecond = 0;
		if (array.Length > 2)
		{
			millisecond = Convert.ToInt16(array[2], NumberFormatInfo.InvariantInfo);
		}
		try
		{
			result = new DateTime(year, month, day, hour, minute, second, millisecond);
			result = result.ToLocalTime();
		}
		catch (ArgumentOutOfRangeException)
		{
		}
		catch (ArgumentException)
		{
		}
		return result;
	}

	private void TryUpdateResponseUri(string str, FtpWebRequest request)
	{
		Uri uri = request.RequestUri;
		int num = str.IndexOf("for ", StringComparison.Ordinal);
		if (num == -1)
		{
			return;
		}
		num += 4;
		int num2 = str.LastIndexOf('(');
		if (num2 == -1)
		{
			num2 = str.Length;
		}
		if (num2 > num)
		{
			string text = str.Substring(num, num2 - num);
			text = text.TrimEnd(' ', '.', '\r', '\n');
			string text2 = text.Replace("%", "%25");
			text2 = text2.Replace("#", "%23");
			string absolutePath = uri.AbsolutePath;
			if (absolutePath.Length > 0 && absolutePath[absolutePath.Length - 1] != '/')
			{
				UriBuilder uriBuilder = new UriBuilder(uri);
				uriBuilder.Path = absolutePath + "/";
				uri = uriBuilder.Uri;
			}
			if (!Uri.TryCreate(uri, text2, out Uri result))
			{
				throw new FormatException(System.SR.Format(System.SR.net_ftp_invalid_response_filename, text));
			}
			if (!uri.IsBaseOf(result) || uri.Segments.Length != result.Segments.Length - 1)
			{
				throw new FormatException(System.SR.Format(System.SR.net_ftp_invalid_response_filename, text));
			}
			_responseUri = result;
		}
	}

	private void TryUpdateContentLength(string str)
	{
		int num = str.LastIndexOf('(');
		if (num == -1)
		{
			return;
		}
		int num2 = str.IndexOf(" bytes).", StringComparison.Ordinal);
		if (num2 != -1 && num2 > num)
		{
			num++;
			if (long.TryParse(str.AsSpan(num, num2 - num), NumberStyles.AllowLeadingWhite | NumberStyles.AllowTrailingWhite, NumberFormatInfo.InvariantInfo, out var result))
			{
				_contentLength = result;
			}
		}
	}

	private string GetLoginDirectory(string str)
	{
		int num = str.IndexOf('"');
		int num2 = str.LastIndexOf('"');
		if (num != -1 && num2 != -1 && num != num2)
		{
			return str.Substring(num + 1, num2 - num - 1);
		}
		return string.Empty;
	}

	private int GetPortV4(string responseString)
	{
		string[] array = responseString.Split(' ', '(', ',', ')');
		if (array.Length <= 7)
		{
			throw new FormatException(System.SR.Format(System.SR.net_ftp_response_invalid_format, responseString));
		}
		int num = array.Length - 1;
		if (!char.IsNumber(array[num], 0))
		{
			num--;
		}
		int num2 = Convert.ToByte(array[num--], NumberFormatInfo.InvariantInfo);
		return num2 | (Convert.ToByte(array[num--], NumberFormatInfo.InvariantInfo) << 8);
	}

	private int GetPortV6(string responseString)
	{
		int num = responseString.LastIndexOf('(');
		int num2 = responseString.LastIndexOf(')');
		if (num == -1 || num2 <= num)
		{
			throw new FormatException(System.SR.Format(System.SR.net_ftp_response_invalid_format, responseString));
		}
		string text = responseString.Substring(num + 1, num2 - num - 1);
		string[] array = text.Split('|');
		if (array.Length < 4)
		{
			throw new FormatException(System.SR.Format(System.SR.net_ftp_response_invalid_format, responseString));
		}
		return Convert.ToInt32(array[3], NumberFormatInfo.InvariantInfo);
	}

	private void CreateFtpListenerSocket(FtpWebRequest request)
	{
		IPEndPoint localEP = new IPEndPoint(((IPEndPoint)base.Socket.LocalEndPoint).Address, 0);
		try
		{
			_dataSocket = CreateFtpDataSocket(request, base.Socket);
		}
		catch (ObjectDisposedException)
		{
			throw ExceptionHelper.RequestAbortedException;
		}
		_dataSocket.Bind(localEP);
		_dataSocket.Listen(1);
	}

	private string GetPortCommandLine(FtpWebRequest request)
	{
		try
		{
			IPEndPoint iPEndPoint = (IPEndPoint)_dataSocket.LocalEndPoint;
			if (base.ServerAddress.AddressFamily == AddressFamily.InterNetwork || base.ServerAddress.IsIPv4MappedToIPv6)
			{
				return FormatAddress(iPEndPoint.Address, iPEndPoint.Port);
			}
			if (base.ServerAddress.AddressFamily == AddressFamily.InterNetworkV6)
			{
				return FormatAddressV6(iPEndPoint.Address, iPEndPoint.Port);
			}
			throw new System.Net.InternalException();
		}
		catch (Exception innerException)
		{
			throw GenerateException(System.SR.net_ftp_protocolerror, WebExceptionStatus.ProtocolError, innerException);
		}
	}

	private string FormatFtpCommand(string command, string parameter)
	{
		if (!string.IsNullOrEmpty(parameter))
		{
			return command + " " + parameter + "\r\n";
		}
		return command + "\r\n";
	}

	private Socket CreateFtpDataSocket(FtpWebRequest request, Socket templateSocket)
	{
		Socket socket = new Socket(templateSocket.AddressFamily, templateSocket.SocketType, templateSocket.ProtocolType);
		if (templateSocket.AddressFamily == AddressFamily.InterNetworkV6 && templateSocket.DualMode)
		{
			socket.DualMode = true;
		}
		return socket;
	}

	protected override bool CheckValid(ResponseDescription response, ref int validThrough, ref int completeLength)
	{
		if (System.Net.NetEventSource.Log.IsEnabled())
		{
			System.Net.NetEventSource.Info(this, $"CheckValid({response.StatusBuffer})", "CheckValid");
		}
		if (response.StatusBuffer.Length < 4)
		{
			return true;
		}
		string text = response.StatusBuffer.ToString();
		if (response.Status == -1)
		{
			if (!char.IsDigit(text[0]) || !char.IsDigit(text[1]) || !char.IsDigit(text[2]) || (text[3] != ' ' && text[3] != '-'))
			{
				return false;
			}
			response.StatusCodeString = text.Substring(0, 3);
			response.Status = Convert.ToInt16(response.StatusCodeString, NumberFormatInfo.InvariantInfo);
			if (text[3] == '-')
			{
				response.Multiline = true;
			}
		}
		int num = 0;
		while ((num = text.IndexOf("\r\n", validThrough, StringComparison.Ordinal)) != -1)
		{
			int num2 = validThrough;
			validThrough = num + 2;
			if (!response.Multiline)
			{
				completeLength = validThrough;
				return true;
			}
			if (text.Length > num2 + 4 && text.Substring(num2, 3) == response.StatusCodeString && text[num2 + 3] == ' ')
			{
				completeLength = validThrough;
				return true;
			}
		}
		return true;
	}

	private TriState IsFtpDataStreamWriteable()
	{
		if (_request is FtpWebRequest ftpWebRequest)
		{
			if (ftpWebRequest.MethodInfo.IsUpload)
			{
				return TriState.True;
			}
			if (ftpWebRequest.MethodInfo.IsDownload)
			{
				return TriState.False;
			}
		}
		return TriState.Unspecified;
	}
}
