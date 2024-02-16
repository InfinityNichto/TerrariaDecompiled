using System.IO;
using System.Text;

namespace System.Net.Sockets;

public sealed class UnixDomainSocketEndPoint : EndPoint
{
	private readonly string _path;

	private readonly byte[] _encodedPath;

	private static readonly int s_nativePathOffset = 2;

	private static readonly int s_nativePathLength = 108;

	private static readonly int s_nativeAddressSize = s_nativePathOffset + s_nativePathLength;

	internal string? BoundFileName { get; }

	internal static int MaxAddressSize => s_nativeAddressSize;

	public override AddressFamily AddressFamily => AddressFamily.Unix;

	public UnixDomainSocketEndPoint(string path)
		: this(path, null)
	{
	}

	private UnixDomainSocketEndPoint(string path, string boundFileName)
	{
		if (path == null)
		{
			throw new ArgumentNullException("path");
		}
		BoundFileName = boundFileName;
		bool flag = IsAbstract(path);
		int num = Encoding.UTF8.GetByteCount(path);
		if (!flag)
		{
			num++;
		}
		if (path.Length == 0 || num > s_nativePathLength)
		{
			throw new ArgumentOutOfRangeException("path", path, System.SR.Format(System.SR.ArgumentOutOfRange_PathLengthInvalid, path, s_nativePathLength));
		}
		_path = path;
		_encodedPath = new byte[num];
		int bytes = Encoding.UTF8.GetBytes(path, 0, path.Length, _encodedPath, 0);
		if (!Socket.OSSupportsUnixDomainSockets)
		{
			throw new PlatformNotSupportedException();
		}
	}

	internal UnixDomainSocketEndPoint(SocketAddress socketAddress)
	{
		if (socketAddress == null)
		{
			throw new ArgumentNullException("socketAddress");
		}
		if (socketAddress.Family != AddressFamily.Unix || socketAddress.Size > s_nativeAddressSize)
		{
			throw new ArgumentOutOfRangeException("socketAddress");
		}
		if (socketAddress.Size > s_nativePathOffset)
		{
			_encodedPath = new byte[socketAddress.Size - s_nativePathOffset];
			for (int i = 0; i < _encodedPath.Length; i++)
			{
				_encodedPath[i] = socketAddress[s_nativePathOffset + i];
			}
			int num = _encodedPath.Length;
			if (!IsAbstract(_encodedPath))
			{
				while (_encodedPath[num - 1] == 0)
				{
					num--;
				}
			}
			_path = Encoding.UTF8.GetString(_encodedPath, 0, num);
		}
		else
		{
			_encodedPath = Array.Empty<byte>();
			_path = string.Empty;
		}
	}

	public override SocketAddress Serialize()
	{
		SocketAddress socketAddress = CreateSocketAddressForSerialize();
		for (int i = 0; i < _encodedPath.Length; i++)
		{
			socketAddress[s_nativePathOffset + i] = _encodedPath[i];
		}
		return socketAddress;
	}

	public override EndPoint Create(SocketAddress socketAddress)
	{
		return new UnixDomainSocketEndPoint(socketAddress);
	}

	public override string ToString()
	{
		if (IsAbstract(_path))
		{
			return "@" + _path.AsSpan(1);
		}
		return _path;
	}

	internal UnixDomainSocketEndPoint CreateBoundEndPoint()
	{
		if (IsAbstract(_path))
		{
			return this;
		}
		return new UnixDomainSocketEndPoint(_path, Path.GetFullPath(_path));
	}

	internal UnixDomainSocketEndPoint CreateUnboundEndPoint()
	{
		if (IsAbstract(_path) || BoundFileName == null)
		{
			return this;
		}
		return new UnixDomainSocketEndPoint(_path, null);
	}

	private static bool IsAbstract(string path)
	{
		if (path.Length > 0)
		{
			return path[0] == '\0';
		}
		return false;
	}

	private static bool IsAbstract(byte[] encodedPath)
	{
		if (encodedPath.Length != 0)
		{
			return encodedPath[0] == 0;
		}
		return false;
	}

	private SocketAddress CreateSocketAddressForSerialize()
	{
		return new SocketAddress(AddressFamily.Unix, s_nativeAddressSize);
	}
}
