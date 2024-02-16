using System.Diagnostics.CodeAnalysis;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Threading.Tasks;

namespace System.Net.Sockets;

public class TcpClient : IDisposable
{
	private AddressFamily _family;

	private Socket _clientSocket;

	private NetworkStream _dataStream;

	private volatile int _disposed;

	private bool _active;

	private bool Disposed => _disposed != 0;

	protected bool Active
	{
		get
		{
			return _active;
		}
		set
		{
			_active = value;
		}
	}

	public int Available => Client?.Available ?? 0;

	public Socket Client
	{
		get
		{
			if (!Disposed)
			{
				return _clientSocket;
			}
			return null;
		}
		set
		{
			_clientSocket = value;
			_family = _clientSocket?.AddressFamily ?? AddressFamily.Unknown;
		}
	}

	public bool Connected => Client?.Connected ?? false;

	public bool ExclusiveAddressUse
	{
		get
		{
			return Client?.ExclusiveAddressUse ?? false;
		}
		set
		{
			if (_clientSocket != null)
			{
				_clientSocket.ExclusiveAddressUse = value;
			}
		}
	}

	public int ReceiveBufferSize
	{
		get
		{
			return (int)Client.GetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReceiveBuffer);
		}
		set
		{
			Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReceiveBuffer, value);
		}
	}

	public int SendBufferSize
	{
		get
		{
			return (int)Client.GetSocketOption(SocketOptionLevel.Socket, SocketOptionName.SendBuffer);
		}
		set
		{
			Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.SendBuffer, value);
		}
	}

	public int ReceiveTimeout
	{
		get
		{
			return (int)Client.GetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReceiveTimeout);
		}
		set
		{
			Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReceiveTimeout, value);
		}
	}

	public int SendTimeout
	{
		get
		{
			return (int)Client.GetSocketOption(SocketOptionLevel.Socket, SocketOptionName.SendTimeout);
		}
		set
		{
			Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.SendTimeout, value);
		}
	}

	public LingerOption? LingerState
	{
		get
		{
			return Client.LingerState;
		}
		[param: DisallowNull]
		set
		{
			Client.LingerState = value;
		}
	}

	public bool NoDelay
	{
		get
		{
			return Client.NoDelay;
		}
		set
		{
			Client.NoDelay = value;
		}
	}

	public TcpClient()
		: this(AddressFamily.Unknown)
	{
	}

	public TcpClient(AddressFamily family)
	{
		if (System.Net.NetEventSource.Log.IsEnabled())
		{
			System.Net.NetEventSource.Info(this, family, ".ctor");
		}
		if (family != AddressFamily.InterNetwork && family != AddressFamily.InterNetworkV6 && family != AddressFamily.Unknown)
		{
			throw new ArgumentException(System.SR.Format(System.SR.net_protocol_invalid_family, "TCP"), "family");
		}
		_family = family;
		InitializeClientSocket();
	}

	public TcpClient(IPEndPoint localEP)
	{
		if (System.Net.NetEventSource.Log.IsEnabled())
		{
			System.Net.NetEventSource.Info(this, localEP, ".ctor");
		}
		if (localEP == null)
		{
			throw new ArgumentNullException("localEP");
		}
		_family = localEP.AddressFamily;
		InitializeClientSocket();
		_clientSocket.Bind(localEP);
	}

	public TcpClient(string hostname, int port)
	{
		if (System.Net.NetEventSource.Log.IsEnabled())
		{
			System.Net.NetEventSource.Info(this, hostname, ".ctor");
		}
		if (hostname == null)
		{
			throw new ArgumentNullException("hostname");
		}
		if (!System.Net.TcpValidationHelpers.ValidatePortNumber(port))
		{
			throw new ArgumentOutOfRangeException("port");
		}
		_family = AddressFamily.Unknown;
		try
		{
			Connect(hostname, port);
		}
		catch
		{
			_clientSocket?.Close();
			throw;
		}
	}

	internal TcpClient(Socket acceptedSocket)
	{
		_clientSocket = acceptedSocket;
		_active = true;
	}

	public void Connect(string hostname, int port)
	{
		ThrowIfDisposed();
		if (hostname == null)
		{
			throw new ArgumentNullException("hostname");
		}
		if (!System.Net.TcpValidationHelpers.ValidatePortNumber(port))
		{
			throw new ArgumentOutOfRangeException("port");
		}
		if (_active)
		{
			throw new SocketException(10056);
		}
		IPAddress[] hostAddresses = Dns.GetHostAddresses(hostname);
		ExceptionDispatchInfo exceptionDispatchInfo = null;
		try
		{
			IPAddress[] array = hostAddresses;
			foreach (IPAddress iPAddress in array)
			{
				try
				{
					if (_clientSocket == null)
					{
						if ((iPAddress.AddressFamily == AddressFamily.InterNetwork && Socket.OSSupportsIPv4) || Socket.OSSupportsIPv6)
						{
							Socket socket = new Socket(iPAddress.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
							if (iPAddress.IsIPv4MappedToIPv6)
							{
								socket.DualMode = true;
							}
							Interlocked.Exchange(ref _clientSocket, socket);
							if (Disposed)
							{
								socket.Dispose();
							}
							try
							{
								socket.Connect(iPAddress, port);
							}
							catch
							{
								_clientSocket = null;
								throw;
							}
						}
						_family = iPAddress.AddressFamily;
						_active = true;
						break;
					}
					if (iPAddress.AddressFamily == _family || _family == AddressFamily.Unknown)
					{
						Connect(new IPEndPoint(iPAddress, port));
						_active = true;
						break;
					}
				}
				catch (Exception ex) when (!(ex is OutOfMemoryException))
				{
					exceptionDispatchInfo = ExceptionDispatchInfo.Capture(ex);
				}
			}
		}
		finally
		{
			if (!_active)
			{
				exceptionDispatchInfo?.Throw();
				throw new SocketException(10057);
			}
		}
	}

	public void Connect(IPAddress address, int port)
	{
		ThrowIfDisposed();
		if (address == null)
		{
			throw new ArgumentNullException("address");
		}
		if (!System.Net.TcpValidationHelpers.ValidatePortNumber(port))
		{
			throw new ArgumentOutOfRangeException("port");
		}
		IPEndPoint remoteEP = new IPEndPoint(address, port);
		Connect(remoteEP);
	}

	public void Connect(IPEndPoint remoteEP)
	{
		ThrowIfDisposed();
		if (remoteEP == null)
		{
			throw new ArgumentNullException("remoteEP");
		}
		Client.Connect(remoteEP);
		_family = Client.AddressFamily;
		_active = true;
	}

	public void Connect(IPAddress[] ipAddresses, int port)
	{
		Client.Connect(ipAddresses, port);
		_family = Client.AddressFamily;
		_active = true;
	}

	public Task ConnectAsync(IPAddress address, int port)
	{
		return CompleteConnectAsync(Client.ConnectAsync(address, port));
	}

	public Task ConnectAsync(string host, int port)
	{
		return CompleteConnectAsync(Client.ConnectAsync(host, port));
	}

	public Task ConnectAsync(IPAddress[] addresses, int port)
	{
		return CompleteConnectAsync(Client.ConnectAsync(addresses, port));
	}

	public Task ConnectAsync(IPEndPoint remoteEP)
	{
		return CompleteConnectAsync(Client.ConnectAsync(remoteEP));
	}

	private async Task CompleteConnectAsync(Task task)
	{
		await task.ConfigureAwait(continueOnCapturedContext: false);
		_active = true;
	}

	public ValueTask ConnectAsync(IPAddress address, int port, CancellationToken cancellationToken)
	{
		return CompleteConnectAsync(Client.ConnectAsync(address, port, cancellationToken));
	}

	public ValueTask ConnectAsync(string host, int port, CancellationToken cancellationToken)
	{
		return CompleteConnectAsync(Client.ConnectAsync(host, port, cancellationToken));
	}

	public ValueTask ConnectAsync(IPAddress[] addresses, int port, CancellationToken cancellationToken)
	{
		return CompleteConnectAsync(Client.ConnectAsync(addresses, port, cancellationToken));
	}

	public ValueTask ConnectAsync(IPEndPoint remoteEP, CancellationToken cancellationToken)
	{
		return CompleteConnectAsync(Client.ConnectAsync(remoteEP, cancellationToken));
	}

	private async ValueTask CompleteConnectAsync(ValueTask task)
	{
		await task.ConfigureAwait(continueOnCapturedContext: false);
		_active = true;
	}

	public IAsyncResult BeginConnect(IPAddress address, int port, AsyncCallback? requestCallback, object? state)
	{
		return Client.BeginConnect(address, port, requestCallback, state);
	}

	public IAsyncResult BeginConnect(string host, int port, AsyncCallback? requestCallback, object? state)
	{
		return Client.BeginConnect(host, port, requestCallback, state);
	}

	public IAsyncResult BeginConnect(IPAddress[] addresses, int port, AsyncCallback? requestCallback, object? state)
	{
		return Client.BeginConnect(addresses, port, requestCallback, state);
	}

	public void EndConnect(IAsyncResult asyncResult)
	{
		_clientSocket.EndConnect(asyncResult);
		_active = true;
	}

	public NetworkStream GetStream()
	{
		ThrowIfDisposed();
		if (!Connected)
		{
			throw new InvalidOperationException(System.SR.net_notconnected);
		}
		if (_dataStream == null)
		{
			_dataStream = new NetworkStream(Client, ownsSocket: true);
		}
		return _dataStream;
	}

	public void Close()
	{
		Dispose();
	}

	protected virtual void Dispose(bool disposing)
	{
		if (Interlocked.CompareExchange(ref _disposed, 1, 0) != 0 || !disposing)
		{
			return;
		}
		IDisposable dataStream = _dataStream;
		if (dataStream != null)
		{
			dataStream.Dispose();
		}
		else
		{
			Socket socket = Volatile.Read(ref _clientSocket);
			if (socket != null)
			{
				try
				{
					socket.InternalShutdown(SocketShutdown.Both);
				}
				finally
				{
					socket.Close();
				}
			}
		}
		GC.SuppressFinalize(this);
	}

	public void Dispose()
	{
		Dispose(disposing: true);
	}

	~TcpClient()
	{
		Dispose(disposing: false);
	}

	private void InitializeClientSocket()
	{
		if (_family == AddressFamily.Unknown)
		{
			_clientSocket = new Socket(SocketType.Stream, ProtocolType.Tcp);
			if (_clientSocket.AddressFamily == AddressFamily.InterNetwork)
			{
				_family = AddressFamily.InterNetwork;
			}
		}
		else
		{
			_clientSocket = new Socket(_family, SocketType.Stream, ProtocolType.Tcp);
		}
	}

	private void ThrowIfDisposed()
	{
		if (Disposed)
		{
			ThrowObjectDisposedException();
		}
		void ThrowObjectDisposedException()
		{
			throw new ObjectDisposedException(GetType().FullName);
		}
	}
}
