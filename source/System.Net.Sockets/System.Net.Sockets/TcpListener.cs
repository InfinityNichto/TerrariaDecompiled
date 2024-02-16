using System.Runtime.Versioning;
using System.Threading;
using System.Threading.Tasks;

namespace System.Net.Sockets;

public class TcpListener
{
	private readonly IPEndPoint _serverSocketEP;

	private Socket _serverSocket;

	private bool _active;

	private bool _exclusiveAddressUse;

	private bool? _allowNatTraversal;

	public Socket Server
	{
		get
		{
			CreateNewSocketIfNeeded();
			return _serverSocket;
		}
	}

	protected bool Active => _active;

	public EndPoint LocalEndpoint
	{
		get
		{
			if (!_active)
			{
				return _serverSocketEP;
			}
			return _serverSocket.LocalEndPoint;
		}
	}

	public bool ExclusiveAddressUse
	{
		get
		{
			if (_serverSocket == null)
			{
				return _exclusiveAddressUse;
			}
			return _serverSocket.ExclusiveAddressUse;
		}
		set
		{
			if (_active)
			{
				throw new InvalidOperationException(System.SR.net_tcplistener_mustbestopped);
			}
			if (_serverSocket != null)
			{
				_serverSocket.ExclusiveAddressUse = value;
			}
			_exclusiveAddressUse = value;
		}
	}

	public TcpListener(IPEndPoint localEP)
	{
		if (System.Net.NetEventSource.Log.IsEnabled())
		{
			System.Net.NetEventSource.Info(this, localEP, ".ctor");
		}
		if (localEP == null)
		{
			throw new ArgumentNullException("localEP");
		}
		_serverSocketEP = localEP;
		_serverSocket = new Socket(_serverSocketEP.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
	}

	public TcpListener(IPAddress localaddr, int port)
	{
		if (System.Net.NetEventSource.Log.IsEnabled())
		{
			System.Net.NetEventSource.Info(this, localaddr, ".ctor");
		}
		if (localaddr == null)
		{
			throw new ArgumentNullException("localaddr");
		}
		if (!System.Net.TcpValidationHelpers.ValidatePortNumber(port))
		{
			throw new ArgumentOutOfRangeException("port");
		}
		_serverSocketEP = new IPEndPoint(localaddr, port);
		_serverSocket = new Socket(_serverSocketEP.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
	}

	[Obsolete("This constructor has been deprecated. Use TcpListener(IPAddress localaddr, int port) instead.")]
	public TcpListener(int port)
	{
		if (!System.Net.TcpValidationHelpers.ValidatePortNumber(port))
		{
			throw new ArgumentOutOfRangeException("port");
		}
		_serverSocketEP = new IPEndPoint(IPAddress.Any, port);
		_serverSocket = new Socket(_serverSocketEP.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
	}

	[SupportedOSPlatform("windows")]
	public void AllowNatTraversal(bool allowed)
	{
		if (_active)
		{
			throw new InvalidOperationException(System.SR.net_tcplistener_mustbestopped);
		}
		if (_serverSocket != null)
		{
			SetIPProtectionLevel(allowed);
		}
		else
		{
			_allowNatTraversal = allowed;
		}
	}

	public void Start()
	{
		Start(int.MaxValue);
	}

	public void Start(int backlog)
	{
		if (backlog > int.MaxValue || backlog < 0)
		{
			throw new ArgumentOutOfRangeException("backlog");
		}
		if (!_active)
		{
			CreateNewSocketIfNeeded();
			_serverSocket.Bind(_serverSocketEP);
			try
			{
				_serverSocket.Listen(backlog);
			}
			catch (SocketException)
			{
				Stop();
				throw;
			}
			_active = true;
		}
	}

	public void Stop()
	{
		_serverSocket?.Dispose();
		_active = false;
		_serverSocket = null;
	}

	public bool Pending()
	{
		if (!_active)
		{
			throw new InvalidOperationException(System.SR.net_stopped);
		}
		return _serverSocket.Poll(0, SelectMode.SelectRead);
	}

	public Socket AcceptSocket()
	{
		if (!_active)
		{
			throw new InvalidOperationException(System.SR.net_stopped);
		}
		return _serverSocket.Accept();
	}

	public TcpClient AcceptTcpClient()
	{
		if (!_active)
		{
			throw new InvalidOperationException(System.SR.net_stopped);
		}
		Socket acceptedSocket = _serverSocket.Accept();
		return new TcpClient(acceptedSocket);
	}

	public IAsyncResult BeginAcceptSocket(AsyncCallback? callback, object? state)
	{
		return System.Threading.Tasks.TaskToApm.Begin(AcceptSocketAsync(), callback, state);
	}

	public Socket EndAcceptSocket(IAsyncResult asyncResult)
	{
		return EndAcceptCore<Socket>(asyncResult);
	}

	public IAsyncResult BeginAcceptTcpClient(AsyncCallback? callback, object? state)
	{
		return System.Threading.Tasks.TaskToApm.Begin(AcceptTcpClientAsync(), callback, state);
	}

	public TcpClient EndAcceptTcpClient(IAsyncResult asyncResult)
	{
		return EndAcceptCore<TcpClient>(asyncResult);
	}

	public Task<Socket> AcceptSocketAsync()
	{
		return AcceptSocketAsync(CancellationToken.None).AsTask();
	}

	public ValueTask<Socket> AcceptSocketAsync(CancellationToken cancellationToken)
	{
		if (!_active)
		{
			throw new InvalidOperationException(System.SR.net_stopped);
		}
		return _serverSocket.AcceptAsync(cancellationToken);
	}

	public Task<TcpClient> AcceptTcpClientAsync()
	{
		return AcceptTcpClientAsync(CancellationToken.None).AsTask();
	}

	public ValueTask<TcpClient> AcceptTcpClientAsync(CancellationToken cancellationToken)
	{
		return WaitAndWrap(AcceptSocketAsync(cancellationToken));
		static async ValueTask<TcpClient> WaitAndWrap(ValueTask<Socket> task)
		{
			return new TcpClient(await task.ConfigureAwait(continueOnCapturedContext: false));
		}
	}

	public static TcpListener Create(int port)
	{
		if (!System.Net.TcpValidationHelpers.ValidatePortNumber(port))
		{
			throw new ArgumentOutOfRangeException("port");
		}
		TcpListener tcpListener;
		if (Socket.OSSupportsIPv6)
		{
			tcpListener = new TcpListener(IPAddress.IPv6Any, port);
			tcpListener.Server.DualMode = true;
		}
		else
		{
			tcpListener = new TcpListener(IPAddress.Any, port);
		}
		return tcpListener;
	}

	[SupportedOSPlatform("windows")]
	private void SetIPProtectionLevel(bool allowed)
	{
		_serverSocket.SetIPProtectionLevel(allowed ? IPProtectionLevel.Unrestricted : IPProtectionLevel.EdgeRestricted);
	}

	private void CreateNewSocketIfNeeded()
	{
		if (_serverSocket == null)
		{
			_serverSocket = new Socket(_serverSocketEP.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
		}
		if (_exclusiveAddressUse)
		{
			_serverSocket.ExclusiveAddressUse = true;
		}
		if (_allowNatTraversal.HasValue)
		{
			SetIPProtectionLevel(_allowNatTraversal.GetValueOrDefault());
			_allowNatTraversal = null;
		}
	}

	private TResult EndAcceptCore<TResult>(IAsyncResult asyncResult)
	{
		try
		{
			return System.Threading.Tasks.TaskToApm.End<TResult>(asyncResult);
		}
		catch (SocketException) when (!_active)
		{
			throw new ObjectDisposedException(typeof(Socket).FullName);
		}
	}
}
