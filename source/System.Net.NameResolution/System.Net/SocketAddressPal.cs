using System.Buffers.Binary;

namespace System.Net;

internal static class SocketAddressPal
{
	public static uint GetIPv4Address(ReadOnlySpan<byte> buffer)
	{
		return BinaryPrimitives.ReadUInt32LittleEndian(buffer.Slice(4));
	}

	public static void GetIPv6Address(ReadOnlySpan<byte> buffer, Span<byte> address, out uint scope)
	{
		buffer.Slice(8, address.Length).CopyTo(address);
		scope = BinaryPrimitives.ReadUInt32LittleEndian(buffer.Slice(24));
	}
}
