using System.Diagnostics.CodeAnalysis;

namespace System.Net.Sockets;

public struct UdpReceiveResult : IEquatable<UdpReceiveResult>
{
	private readonly byte[] _buffer;

	private readonly IPEndPoint _remoteEndPoint;

	public byte[] Buffer => _buffer;

	public IPEndPoint RemoteEndPoint => _remoteEndPoint;

	public UdpReceiveResult(byte[] buffer, IPEndPoint remoteEndPoint)
	{
		if (buffer == null)
		{
			throw new ArgumentNullException("buffer");
		}
		if (remoteEndPoint == null)
		{
			throw new ArgumentNullException("remoteEndPoint");
		}
		_buffer = buffer;
		_remoteEndPoint = remoteEndPoint;
	}

	public override int GetHashCode()
	{
		if (_buffer == null)
		{
			return 0;
		}
		return _buffer.GetHashCode() ^ _remoteEndPoint.GetHashCode();
	}

	public override bool Equals([NotNullWhen(true)] object? obj)
	{
		if (obj is UdpReceiveResult other)
		{
			return Equals(other);
		}
		return false;
	}

	public bool Equals(UdpReceiveResult other)
	{
		if (object.Equals(_buffer, other._buffer))
		{
			return object.Equals(_remoteEndPoint, other._remoteEndPoint);
		}
		return false;
	}

	public static bool operator ==(UdpReceiveResult left, UdpReceiveResult right)
	{
		return left.Equals(right);
	}

	public static bool operator !=(UdpReceiveResult left, UdpReceiveResult right)
	{
		return !left.Equals(right);
	}
}
