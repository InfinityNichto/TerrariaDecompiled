using System.Diagnostics.CodeAnalysis;
using System.Runtime.Versioning;
using System.Threading;
using System.Threading.Tasks;

namespace System.Net.Sockets;

public class UdpClient : IDisposable
{
	private Socket _clientSocket;

	private bool _active;

	private readonly byte[] _buffer = new byte[65536];

	private AddressFamily _family = AddressFamily.InterNetwork;

	private bool _disposed;

	private bool _isBroadcast;

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

	public int Available => _clientSocket.Available;

	public Socket Client
	{
		get
		{
			return _clientSocket;
		}
		set
		{
			_clientSocket = value;
		}
	}

	public short Ttl
	{
		get
		{
			return _clientSocket.Ttl;
		}
		set
		{
			_clientSocket.Ttl = value;
		}
	}

	public bool DontFragment
	{
		get
		{
			return _clientSocket.DontFragment;
		}
		set
		{
			_clientSocket.DontFragment = value;
		}
	}

	public bool MulticastLoopback
	{
		get
		{
			return _clientSocket.MulticastLoopback;
		}
		set
		{
			_clientSocket.MulticastLoopback = value;
		}
	}

	public bool EnableBroadcast
	{
		get
		{
			return _clientSocket.EnableBroadcast;
		}
		set
		{
			_clientSocket.EnableBroadcast = value;
		}
	}

	public bool ExclusiveAddressUse
	{
		get
		{
			return _clientSocket.ExclusiveAddressUse;
		}
		set
		{
			_clientSocket.ExclusiveAddressUse = value;
		}
	}

	public UdpClient()
		: this(AddressFamily.InterNetwork)
	{
	}

	public UdpClient(AddressFamily family)
	{
		if (family != AddressFamily.InterNetwork && family != AddressFamily.InterNetworkV6)
		{
			throw new ArgumentException(System.SR.Format(System.SR.net_protocol_invalid_family, "UDP"), "family");
		}
		_family = family;
		CreateClientSocket();
	}

	public UdpClient(int port)
		: this(port, AddressFamily.InterNetwork)
	{
	}

	public UdpClient(int port, AddressFamily family)
	{
		if (!System.Net.TcpValidationHelpers.ValidatePortNumber(port))
		{
			throw new ArgumentOutOfRangeException("port");
		}
		if (family != AddressFamily.InterNetwork && family != AddressFamily.InterNetworkV6)
		{
			throw new ArgumentException(System.SR.Format(System.SR.net_protocol_invalid_family, "UDP"), "family");
		}
		_family = family;
		IPEndPoint localEP = ((_family != AddressFamily.InterNetwork) ? new IPEndPoint(IPAddress.IPv6Any, port) : new IPEndPoint(IPAddress.Any, port));
		CreateClientSocket();
		_clientSocket.Bind(localEP);
	}

	public UdpClient(IPEndPoint localEP)
	{
		if (localEP == null)
		{
			throw new ArgumentNullException("localEP");
		}
		_family = localEP.AddressFamily;
		CreateClientSocket();
		_clientSocket.Bind(localEP);
	}

	[SupportedOSPlatform("windows")]
	public void AllowNatTraversal(bool allowed)
	{
		_clientSocket.SetIPProtectionLevel(allowed ? IPProtectionLevel.Unrestricted : IPProtectionLevel.EdgeRestricted);
	}

	private bool IsAddressFamilyCompatible(AddressFamily family)
	{
		if (family == _family)
		{
			return true;
		}
		if (family == AddressFamily.InterNetwork)
		{
			if (_family == AddressFamily.InterNetworkV6)
			{
				return _clientSocket.DualMode;
			}
			return false;
		}
		return false;
	}

	public void Dispose()
	{
		Dispose(disposing: true);
	}

	protected virtual void Dispose(bool disposing)
	{
		if (!disposing)
		{
			return;
		}
		if (System.Net.NetEventSource.Log.IsEnabled())
		{
			System.Net.NetEventSource.Info(this, null, "Dispose");
		}
		if (!_disposed)
		{
			Socket clientSocket = _clientSocket;
			if (clientSocket != null)
			{
				clientSocket.InternalShutdown(SocketShutdown.Both);
				clientSocket.Dispose();
				_clientSocket = null;
			}
			_disposed = true;
			GC.SuppressFinalize(this);
		}
	}

	private void CheckForBroadcast(IPAddress ipAddress)
	{
		if (_clientSocket != null && !_isBroadcast && IsBroadcast(ipAddress))
		{
			_isBroadcast = true;
			_clientSocket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.Broadcast, 1);
		}
	}

	private bool IsBroadcast(IPAddress address)
	{
		if (address.AddressFamily == AddressFamily.InterNetworkV6)
		{
			return false;
		}
		return address.Equals(IPAddress.Broadcast);
	}

	public IAsyncResult BeginSend(byte[] datagram, int bytes, AsyncCallback? requestCallback, object? state)
	{
		return BeginSend(datagram, bytes, null, requestCallback, state);
	}

	public IAsyncResult BeginSend(byte[] datagram, int bytes, string? hostname, int port, AsyncCallback? requestCallback, object? state)
	{
		return BeginSend(datagram, bytes, GetEndpoint(hostname, port), requestCallback, state);
	}

	public IAsyncResult BeginSend(byte[] datagram, int bytes, IPEndPoint? endPoint, AsyncCallback? requestCallback, object? state)
	{
		ValidateDatagram(datagram, bytes, endPoint);
		if (endPoint == null)
		{
			return _clientSocket.BeginSend(datagram, 0, bytes, SocketFlags.None, requestCallback, state);
		}
		CheckForBroadcast(endPoint.Address);
		return _clientSocket.BeginSendTo(datagram, 0, bytes, SocketFlags.None, endPoint, requestCallback, state);
	}

	public int EndSend(IAsyncResult asyncResult)
	{
		ThrowIfDisposed();
		if (!_active)
		{
			return _clientSocket.EndSendTo(asyncResult);
		}
		return _clientSocket.EndSend(asyncResult);
	}

	private void ValidateDatagram(byte[] datagram, int bytes, IPEndPoint endPoint)
	{
		ThrowIfDisposed();
		if (datagram == null)
		{
			throw new ArgumentNullException("datagram");
		}
		if (bytes > datagram.Length || bytes < 0)
		{
			throw new ArgumentOutOfRangeException("bytes");
		}
		if (_active && endPoint != null)
		{
			throw new InvalidOperationException(System.SR.net_udpconnected);
		}
	}

	private IPEndPoint GetEndpoint(string hostname, int port)
	{
		if (_active && (hostname != null || port != 0))
		{
			throw new InvalidOperationException(System.SR.net_udpconnected);
		}
		IPEndPoint result = null;
		if (hostname != null && port != 0)
		{
			IPAddress[] hostAddresses = Dns.GetHostAddresses(hostname);
			int i;
			for (i = 0; i < hostAddresses.Length && !IsAddressFamilyCompatible(hostAddresses[i].AddressFamily); i++)
			{
			}
			if (hostAddresses.Length == 0 || i == hostAddresses.Length)
			{
				throw new ArgumentException(System.SR.net_invalidAddressList, "hostname");
			}
			CheckForBroadcast(hostAddresses[i]);
			result = new IPEndPoint(hostAddresses[i], port);
		}
		return result;
	}

	public IAsyncResult BeginReceive(AsyncCallback? requestCallback, object? state)
	{
		ThrowIfDisposed();
		EndPoint remoteEP = ((_family != AddressFamily.InterNetwork) ? IPEndPointStatics.IPv6Any : IPEndPointStatics.Any);
		return _clientSocket.BeginReceiveFrom(_buffer, 0, 65536, SocketFlags.None, ref remoteEP, requestCallback, state);
	}

	public byte[] EndReceive(IAsyncResult asyncResult, ref IPEndPoint? remoteEP)
	{
		ThrowIfDisposed();
		EndPoint endPoint = ((_family != AddressFamily.InterNetwork) ? IPEndPointStatics.IPv6Any : IPEndPointStatics.Any);
		int num = _clientSocket.EndReceiveFrom(asyncResult, ref endPoint);
		remoteEP = (IPEndPoint)endPoint;
		if (num < 65536)
		{
			byte[] array = new byte[num];
			Buffer.BlockCopy(_buffer, 0, array, 0, num);
			return array;
		}
		return _buffer;
	}

	public void JoinMulticastGroup(IPAddress multicastAddr)
	{
		ThrowIfDisposed();
		if (multicastAddr == null)
		{
			throw new ArgumentNullException("multicastAddr");
		}
		if (multicastAddr.AddressFamily != _family)
		{
			throw new ArgumentException(System.SR.Format(System.SR.net_protocol_invalid_multicast_family, "UDP"), "multicastAddr");
		}
		if (_family == AddressFamily.InterNetwork)
		{
			MulticastOption optionValue = new MulticastOption(multicastAddr);
			_clientSocket.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.AddMembership, optionValue);
		}
		else
		{
			IPv6MulticastOption optionValue2 = new IPv6MulticastOption(multicastAddr);
			_clientSocket.SetSocketOption(SocketOptionLevel.IPv6, SocketOptionName.AddMembership, optionValue2);
		}
	}

	public void JoinMulticastGroup(IPAddress multicastAddr, IPAddress localAddress)
	{
		ThrowIfDisposed();
		if (_family != AddressFamily.InterNetwork)
		{
			throw new SocketException(10045);
		}
		MulticastOption optionValue = new MulticastOption(multicastAddr, localAddress);
		_clientSocket.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.AddMembership, optionValue);
	}

	public void JoinMulticastGroup(int ifindex, IPAddress multicastAddr)
	{
		ThrowIfDisposed();
		if (multicastAddr == null)
		{
			throw new ArgumentNullException("multicastAddr");
		}
		if (ifindex < 0)
		{
			throw new ArgumentException(System.SR.net_value_cannot_be_negative, "ifindex");
		}
		if (_family != AddressFamily.InterNetworkV6)
		{
			throw new SocketException(10045);
		}
		IPv6MulticastOption optionValue = new IPv6MulticastOption(multicastAddr, ifindex);
		_clientSocket.SetSocketOption(SocketOptionLevel.IPv6, SocketOptionName.AddMembership, optionValue);
	}

	public void JoinMulticastGroup(IPAddress multicastAddr, int timeToLive)
	{
		ThrowIfDisposed();
		if (multicastAddr == null)
		{
			throw new ArgumentNullException("multicastAddr");
		}
		if (!RangeValidationHelpers.ValidateRange(timeToLive, 0, 255))
		{
			throw new ArgumentOutOfRangeException("timeToLive");
		}
		JoinMulticastGroup(multicastAddr);
		_clientSocket.SetSocketOption((_family != AddressFamily.InterNetwork) ? SocketOptionLevel.IPv6 : SocketOptionLevel.IP, SocketOptionName.MulticastTimeToLive, timeToLive);
	}

	public void DropMulticastGroup(IPAddress multicastAddr)
	{
		ThrowIfDisposed();
		if (multicastAddr == null)
		{
			throw new ArgumentNullException("multicastAddr");
		}
		if (multicastAddr.AddressFamily != _family)
		{
			throw new ArgumentException(System.SR.Format(System.SR.net_protocol_invalid_multicast_family, "UDP"), "multicastAddr");
		}
		if (_family == AddressFamily.InterNetwork)
		{
			MulticastOption optionValue = new MulticastOption(multicastAddr);
			_clientSocket.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.DropMembership, optionValue);
		}
		else
		{
			IPv6MulticastOption optionValue2 = new IPv6MulticastOption(multicastAddr);
			_clientSocket.SetSocketOption(SocketOptionLevel.IPv6, SocketOptionName.DropMembership, optionValue2);
		}
	}

	public void DropMulticastGroup(IPAddress multicastAddr, int ifindex)
	{
		ThrowIfDisposed();
		if (multicastAddr == null)
		{
			throw new ArgumentNullException("multicastAddr");
		}
		if (ifindex < 0)
		{
			throw new ArgumentException(System.SR.net_value_cannot_be_negative, "ifindex");
		}
		if (_family != AddressFamily.InterNetworkV6)
		{
			throw new SocketException(10045);
		}
		IPv6MulticastOption optionValue = new IPv6MulticastOption(multicastAddr, ifindex);
		_clientSocket.SetSocketOption(SocketOptionLevel.IPv6, SocketOptionName.DropMembership, optionValue);
	}

	public Task<int> SendAsync(byte[] datagram, int bytes)
	{
		return SendAsync(datagram, bytes, null);
	}

	public ValueTask<int> SendAsync(ReadOnlyMemory<byte> datagram, CancellationToken cancellationToken = default(CancellationToken))
	{
		return SendAsync(datagram, null, cancellationToken);
	}

	public Task<int> SendAsync(byte[] datagram, int bytes, string? hostname, int port)
	{
		return SendAsync(datagram, bytes, GetEndpoint(hostname, port));
	}

	public ValueTask<int> SendAsync(ReadOnlyMemory<byte> datagram, string? hostname, int port, CancellationToken cancellationToken = default(CancellationToken))
	{
		return SendAsync(datagram, GetEndpoint(hostname, port), cancellationToken);
	}

	public Task<int> SendAsync(byte[] datagram, int bytes, IPEndPoint? endPoint)
	{
		ValidateDatagram(datagram, bytes, endPoint);
		if (endPoint == null)
		{
			return _clientSocket.SendAsync(new ArraySegment<byte>(datagram, 0, bytes), SocketFlags.None);
		}
		CheckForBroadcast(endPoint.Address);
		return _clientSocket.SendToAsync(new ArraySegment<byte>(datagram, 0, bytes), SocketFlags.None, endPoint);
	}

	public ValueTask<int> SendAsync(ReadOnlyMemory<byte> datagram, IPEndPoint? endPoint, CancellationToken cancellationToken = default(CancellationToken))
	{
		ThrowIfDisposed();
		if (endPoint == null)
		{
			return _clientSocket.SendAsync(datagram, SocketFlags.None, cancellationToken);
		}
		if (_active)
		{
			throw new InvalidOperationException(System.SR.net_udpconnected);
		}
		CheckForBroadcast(endPoint.Address);
		return _clientSocket.SendToAsync(datagram, SocketFlags.None, endPoint, cancellationToken);
	}

	public Task<UdpReceiveResult> ReceiveAsync()
	{
		ThrowIfDisposed();
		return WaitAndWrap(_clientSocket.ReceiveFromAsync(new ArraySegment<byte>(_buffer, 0, 65536), SocketFlags.None, (_family == AddressFamily.InterNetwork) ? IPEndPointStatics.Any : IPEndPointStatics.IPv6Any));
		async Task<UdpReceiveResult> WaitAndWrap(Task<SocketReceiveFromResult> task)
		{
			SocketReceiveFromResult socketReceiveFromResult = await task.ConfigureAwait(continueOnCapturedContext: false);
			byte[] buffer = ((socketReceiveFromResult.ReceivedBytes < 65536) ? _buffer.AsSpan(0, socketReceiveFromResult.ReceivedBytes).ToArray() : _buffer);
			return new UdpReceiveResult(buffer, (IPEndPoint)socketReceiveFromResult.RemoteEndPoint);
		}
	}

	public ValueTask<UdpReceiveResult> ReceiveAsync(CancellationToken cancellationToken)
	{
		ThrowIfDisposed();
		return WaitAndWrap(_clientSocket.ReceiveFromAsync(_buffer, SocketFlags.None, (_family == AddressFamily.InterNetwork) ? IPEndPointStatics.Any : IPEndPointStatics.IPv6Any, cancellationToken));
		async ValueTask<UdpReceiveResult> WaitAndWrap(ValueTask<SocketReceiveFromResult> task)
		{
			SocketReceiveFromResult socketReceiveFromResult = await task.ConfigureAwait(continueOnCapturedContext: false);
			byte[] buffer = ((socketReceiveFromResult.ReceivedBytes < 65536) ? _buffer.AsSpan(0, socketReceiveFromResult.ReceivedBytes).ToArray() : _buffer);
			return new UdpReceiveResult(buffer, (IPEndPoint)socketReceiveFromResult.RemoteEndPoint);
		}
	}

	private void CreateClientSocket()
	{
		_clientSocket = new Socket(_family, SocketType.Dgram, ProtocolType.Udp);
	}

	public UdpClient(string hostname, int port)
	{
		if (hostname == null)
		{
			throw new ArgumentNullException("hostname");
		}
		if (!System.Net.TcpValidationHelpers.ValidatePortNumber(port))
		{
			throw new ArgumentOutOfRangeException("port");
		}
		Connect(hostname, port);
	}

	public void Close()
	{
		Dispose(disposing: true);
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
		IPAddress[] hostAddresses = Dns.GetHostAddresses(hostname);
		Exception ex = null;
		Socket socket = null;
		Socket socket2 = null;
		try
		{
			if (_clientSocket == null)
			{
				if (Socket.OSSupportsIPv4)
				{
					socket2 = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
				}
				if (Socket.OSSupportsIPv6)
				{
					socket = new Socket(AddressFamily.InterNetworkV6, SocketType.Dgram, ProtocolType.Udp);
				}
			}
			IPAddress[] array = hostAddresses;
			foreach (IPAddress iPAddress in array)
			{
				try
				{
					if (_clientSocket == null)
					{
						if (iPAddress.AddressFamily == AddressFamily.InterNetwork && socket2 != null)
						{
							socket2.Connect(iPAddress, port);
							_clientSocket = socket2;
							socket?.Close();
						}
						else if (socket != null)
						{
							socket.Connect(iPAddress, port);
							_clientSocket = socket;
							socket2?.Close();
						}
						_family = iPAddress.AddressFamily;
						_active = true;
						break;
					}
					if (IsAddressFamilyCompatible(iPAddress.AddressFamily))
					{
						Connect(new IPEndPoint(iPAddress, port));
						_active = true;
						break;
					}
				}
				catch (Exception ex2)
				{
					if (ExceptionCheck.IsFatal(ex2))
					{
						throw;
					}
					ex = ex2;
				}
			}
		}
		catch (Exception ex3)
		{
			if (ExceptionCheck.IsFatal(ex3))
			{
				throw;
			}
			ex = ex3;
		}
		finally
		{
			if (!_active)
			{
				socket?.Close();
				socket2?.Close();
				if (ex != null)
				{
					throw ex;
				}
				throw new SocketException(10057);
			}
		}
	}

	public void Connect(IPAddress addr, int port)
	{
		ThrowIfDisposed();
		if (addr == null)
		{
			throw new ArgumentNullException("addr");
		}
		if (!System.Net.TcpValidationHelpers.ValidatePortNumber(port))
		{
			throw new ArgumentOutOfRangeException("port");
		}
		IPEndPoint endPoint = new IPEndPoint(addr, port);
		Connect(endPoint);
	}

	public void Connect(IPEndPoint endPoint)
	{
		ThrowIfDisposed();
		if (endPoint == null)
		{
			throw new ArgumentNullException("endPoint");
		}
		CheckForBroadcast(endPoint.Address);
		Client.Connect(endPoint);
		_active = true;
	}

	public byte[] Receive([NotNull] ref IPEndPoint? remoteEP)
	{
		ThrowIfDisposed();
		EndPoint remoteEP2 = ((_family != AddressFamily.InterNetwork) ? IPEndPointStatics.IPv6Any : IPEndPointStatics.Any);
		int num = Client.ReceiveFrom(_buffer, 65536, SocketFlags.None, ref remoteEP2);
		remoteEP = (IPEndPoint)remoteEP2;
		if (num < 65536)
		{
			byte[] array = new byte[num];
			Buffer.BlockCopy(_buffer, 0, array, 0, num);
			return array;
		}
		return _buffer;
	}

	public int Send(byte[] dgram, int bytes, IPEndPoint? endPoint)
	{
		ThrowIfDisposed();
		if (dgram == null)
		{
			throw new ArgumentNullException("dgram");
		}
		if (_active && endPoint != null)
		{
			throw new InvalidOperationException(System.SR.net_udpconnected);
		}
		if (endPoint == null)
		{
			return Client.Send(dgram, 0, bytes, SocketFlags.None);
		}
		CheckForBroadcast(endPoint.Address);
		return Client.SendTo(dgram, 0, bytes, SocketFlags.None, endPoint);
	}

	public int Send(ReadOnlySpan<byte> datagram, IPEndPoint? endPoint)
	{
		ThrowIfDisposed();
		if (_active && endPoint != null)
		{
			throw new InvalidOperationException(System.SR.net_udpconnected);
		}
		if (endPoint == null)
		{
			return Client.Send(datagram, SocketFlags.None);
		}
		CheckForBroadcast(endPoint.Address);
		return Client.SendTo(datagram, SocketFlags.None, endPoint);
	}

	public int Send(byte[] dgram, int bytes, string? hostname, int port)
	{
		return Send(dgram, bytes, GetEndpoint(hostname, port));
	}

	public int Send(ReadOnlySpan<byte> datagram, string? hostname, int port)
	{
		return Send(datagram, GetEndpoint(hostname, port));
	}

	public int Send(byte[] dgram, int bytes)
	{
		ThrowIfDisposed();
		if (dgram == null)
		{
			throw new ArgumentNullException("dgram");
		}
		if (!_active)
		{
			throw new InvalidOperationException(System.SR.net_notconnected);
		}
		return Client.Send(dgram, 0, bytes, SocketFlags.None);
	}

	public int Send(ReadOnlySpan<byte> datagram)
	{
		ThrowIfDisposed();
		if (!_active)
		{
			throw new InvalidOperationException(System.SR.net_notconnected);
		}
		return Client.Send(datagram, SocketFlags.None);
	}

	private void ThrowIfDisposed()
	{
		if (_disposed)
		{
			ThrowObjectDisposedException();
		}
		void ThrowObjectDisposedException()
		{
			throw new ObjectDisposedException(GetType().FullName);
		}
	}
}
