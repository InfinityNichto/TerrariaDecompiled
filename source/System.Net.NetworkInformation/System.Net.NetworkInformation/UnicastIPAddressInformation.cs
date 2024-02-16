using System.Net.Sockets;

namespace System.Net.NetworkInformation;

public abstract class UnicastIPAddressInformation : IPAddressInformation
{
	public abstract long AddressPreferredLifetime { get; }

	public abstract long AddressValidLifetime { get; }

	public abstract long DhcpLeaseLifetime { get; }

	public abstract DuplicateAddressDetectionState DuplicateAddressDetectionState { get; }

	public abstract PrefixOrigin PrefixOrigin { get; }

	public abstract SuffixOrigin SuffixOrigin { get; }

	public abstract IPAddress IPv4Mask { get; }

	public virtual int PrefixLength
	{
		get
		{
			throw System.NotImplemented.ByDesignWithMessage(System.SR.net_PropertyNotImplementedException);
		}
	}

	internal static IPAddress PrefixLengthToSubnetMask(byte prefixLength, AddressFamily family)
	{
		Span<byte> span = ((family != AddressFamily.InterNetwork) ? stackalloc byte[16] : stackalloc byte[4]);
		Span<byte> span2 = span;
		span2.Clear();
		for (int i = 0; i < prefixLength; i++)
		{
			span2[i / 8] |= (byte)(128 >> i % 8);
		}
		return new IPAddress(span2);
	}
}
