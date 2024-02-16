using System.Buffers.Binary;
using System.Net.Sockets;

namespace System.Net;

internal static class SocketAddressPal
{
	public static AddressFamily GetAddressFamily(ReadOnlySpan<byte> buffer)
	{
		return (AddressFamily)BitConverter.ToInt16(buffer);
	}

	public static void SetAddressFamily(byte[] buffer, AddressFamily family)
	{
		if (family > (AddressFamily)65535)
		{
			throw new PlatformNotSupportedException();
		}
		BinaryPrimitives.WriteUInt16LittleEndian(buffer, (ushort)family);
	}

	public static void SetPort(byte[] buffer, ushort port)
	{
		BinaryPrimitives.WriteUInt16BigEndian(buffer.AsSpan(2), port);
	}

	public static void SetIPv4Address(byte[] buffer, uint address)
	{
		BinaryPrimitives.WriteUInt32LittleEndian(buffer.AsSpan(4), address);
	}

	public static void SetIPv6Address(byte[] buffer, Span<byte> address, uint scope)
	{
		BinaryPrimitives.WriteUInt32LittleEndian(buffer.AsSpan(4), 0u);
		BinaryPrimitives.WriteUInt32LittleEndian(buffer.AsSpan(24), scope);
		address.CopyTo(buffer.AsSpan(8));
	}
}
