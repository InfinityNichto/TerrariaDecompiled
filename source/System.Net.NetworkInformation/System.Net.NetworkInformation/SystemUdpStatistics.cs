using System.Net.Sockets;

namespace System.Net.NetworkInformation;

internal sealed class SystemUdpStatistics : UdpStatistics
{
	private readonly global::Interop.IpHlpApi.MibUdpStats _stats;

	public override long DatagramsReceived => _stats.datagramsReceived;

	public override long IncomingDatagramsDiscarded => _stats.incomingDatagramsDiscarded;

	public override long IncomingDatagramsWithErrors => _stats.incomingDatagramsWithErrors;

	public override long DatagramsSent => _stats.datagramsSent;

	public override int UdpListeners => (int)_stats.udpListeners;

	internal SystemUdpStatistics(AddressFamily family)
	{
		uint udpStatisticsEx = global::Interop.IpHlpApi.GetUdpStatisticsEx(out _stats, family);
		if (udpStatisticsEx != 0)
		{
			throw new NetworkInformationException((int)udpStatisticsEx);
		}
	}
}
