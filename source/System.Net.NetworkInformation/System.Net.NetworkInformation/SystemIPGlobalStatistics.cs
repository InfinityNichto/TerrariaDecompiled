using System.Net.Sockets;

namespace System.Net.NetworkInformation;

internal sealed class SystemIPGlobalStatistics : IPGlobalStatistics
{
	private readonly global::Interop.IpHlpApi.MibIpStats _stats;

	public override bool ForwardingEnabled => _stats.forwardingEnabled;

	public override int DefaultTtl => (int)_stats.defaultTtl;

	public override long ReceivedPackets => _stats.packetsReceived;

	public override long ReceivedPacketsWithHeadersErrors => _stats.receivedPacketsWithHeaderErrors;

	public override long ReceivedPacketsWithAddressErrors => _stats.receivedPacketsWithAddressErrors;

	public override long ReceivedPacketsForwarded => _stats.packetsForwarded;

	public override long ReceivedPacketsWithUnknownProtocol => _stats.receivedPacketsWithUnknownProtocols;

	public override long ReceivedPacketsDiscarded => _stats.receivedPacketsDiscarded;

	public override long ReceivedPacketsDelivered => _stats.receivedPacketsDelivered;

	public override long OutputPacketRequests => _stats.packetOutputRequests;

	public override long OutputPacketRoutingDiscards => _stats.outputPacketRoutingDiscards;

	public override long OutputPacketsDiscarded => _stats.outputPacketsDiscarded;

	public override long OutputPacketsWithNoRoute => _stats.outputPacketsWithNoRoute;

	public override long PacketReassemblyTimeout => _stats.packetReassemblyTimeout;

	public override long PacketReassembliesRequired => _stats.packetsReassemblyRequired;

	public override long PacketsReassembled => _stats.packetsReassembled;

	public override long PacketReassemblyFailures => _stats.packetsReassemblyFailed;

	public override long PacketsFragmented => _stats.packetsFragmented;

	public override long PacketFragmentFailures => _stats.packetsFragmentFailed;

	public override int NumberOfInterfaces => (int)_stats.interfaces;

	public override int NumberOfIPAddresses => (int)_stats.ipAddresses;

	public override int NumberOfRoutes => (int)_stats.routes;

	internal SystemIPGlobalStatistics(AddressFamily family)
	{
		uint ipStatisticsEx = global::Interop.IpHlpApi.GetIpStatisticsEx(out _stats, family);
		if (ipStatisticsEx != 0)
		{
			throw new NetworkInformationException((int)ipStatisticsEx);
		}
	}
}
