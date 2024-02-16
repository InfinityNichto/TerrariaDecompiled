using System.Collections.Generic;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;

namespace System.Net.Sockets;

public static class SocketTaskExtensions
{
	[EditorBrowsable(EditorBrowsableState.Never)]
	public static Task<Socket> AcceptAsync(this Socket socket)
	{
		return socket.AcceptAsync();
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	public static Task<Socket> AcceptAsync(this Socket socket, Socket? acceptSocket)
	{
		return socket.AcceptAsync(acceptSocket);
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	public static Task ConnectAsync(this Socket socket, EndPoint remoteEP)
	{
		return socket.ConnectAsync(remoteEP);
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	public static ValueTask ConnectAsync(this Socket socket, EndPoint remoteEP, CancellationToken cancellationToken)
	{
		return socket.ConnectAsync(remoteEP, cancellationToken);
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	public static Task ConnectAsync(this Socket socket, IPAddress address, int port)
	{
		return socket.ConnectAsync(address, port);
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	public static ValueTask ConnectAsync(this Socket socket, IPAddress address, int port, CancellationToken cancellationToken)
	{
		return socket.ConnectAsync(address, port, cancellationToken);
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	public static Task ConnectAsync(this Socket socket, IPAddress[] addresses, int port)
	{
		return socket.ConnectAsync(addresses, port);
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	public static ValueTask ConnectAsync(this Socket socket, IPAddress[] addresses, int port, CancellationToken cancellationToken)
	{
		return socket.ConnectAsync(addresses, port, cancellationToken);
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	public static Task ConnectAsync(this Socket socket, string host, int port)
	{
		return socket.ConnectAsync(host, port);
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	public static ValueTask ConnectAsync(this Socket socket, string host, int port, CancellationToken cancellationToken)
	{
		return socket.ConnectAsync(host, port, cancellationToken);
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	public static Task<int> ReceiveAsync(this Socket socket, ArraySegment<byte> buffer, SocketFlags socketFlags)
	{
		return socket.ReceiveAsync(buffer, socketFlags);
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	public static ValueTask<int> ReceiveAsync(this Socket socket, Memory<byte> buffer, SocketFlags socketFlags, CancellationToken cancellationToken = default(CancellationToken))
	{
		return socket.ReceiveAsync(buffer, socketFlags, cancellationToken);
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	public static Task<int> ReceiveAsync(this Socket socket, IList<ArraySegment<byte>> buffers, SocketFlags socketFlags)
	{
		return socket.ReceiveAsync(buffers, socketFlags);
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	public static Task<SocketReceiveFromResult> ReceiveFromAsync(this Socket socket, ArraySegment<byte> buffer, SocketFlags socketFlags, EndPoint remoteEndPoint)
	{
		return socket.ReceiveFromAsync(buffer, socketFlags, remoteEndPoint);
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	public static Task<SocketReceiveMessageFromResult> ReceiveMessageFromAsync(this Socket socket, ArraySegment<byte> buffer, SocketFlags socketFlags, EndPoint remoteEndPoint)
	{
		return socket.ReceiveMessageFromAsync(buffer, socketFlags, remoteEndPoint);
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	public static Task<int> SendAsync(this Socket socket, ArraySegment<byte> buffer, SocketFlags socketFlags)
	{
		return socket.SendAsync(buffer, socketFlags);
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	public static ValueTask<int> SendAsync(this Socket socket, ReadOnlyMemory<byte> buffer, SocketFlags socketFlags, CancellationToken cancellationToken = default(CancellationToken))
	{
		return socket.SendAsync(buffer, socketFlags, cancellationToken);
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	public static Task<int> SendAsync(this Socket socket, IList<ArraySegment<byte>> buffers, SocketFlags socketFlags)
	{
		return socket.SendAsync(buffers, socketFlags);
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	public static Task<int> SendToAsync(this Socket socket, ArraySegment<byte> buffer, SocketFlags socketFlags, EndPoint remoteEP)
	{
		return socket.SendToAsync(buffer, socketFlags, remoteEP);
	}
}
