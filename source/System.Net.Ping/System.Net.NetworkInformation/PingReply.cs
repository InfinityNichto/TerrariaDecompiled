namespace System.Net.NetworkInformation;

public class PingReply
{
	public IPStatus Status { get; }

	public IPAddress Address { get; }

	public long RoundtripTime { get; }

	public PingOptions? Options { get; }

	public byte[] Buffer { get; }

	internal PingReply(IPAddress address, PingOptions options, IPStatus ipStatus, long rtt, byte[] buffer)
	{
		Address = address;
		Options = options;
		Status = ipStatus;
		RoundtripTime = rtt;
		Buffer = buffer;
	}
}
