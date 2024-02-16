namespace System.Net.NetworkInformation;

internal sealed class SystemIPv4InterfaceStatistics : IPv4InterfaceStatistics
{
	private readonly global::Interop.IpHlpApi.MibIfRow2 _ifRow;

	public override long OutputQueueLength => (long)_ifRow.outQLen;

	public override long BytesSent => (long)_ifRow.outOctets;

	public override long BytesReceived => (long)_ifRow.inOctets;

	public override long UnicastPacketsSent => (long)_ifRow.outUcastPkts;

	public override long UnicastPacketsReceived => (long)_ifRow.inUcastPkts;

	public override long NonUnicastPacketsSent => (long)_ifRow.outNUcastPkts;

	public override long NonUnicastPacketsReceived => (long)_ifRow.inNUcastPkts;

	public override long IncomingPacketsDiscarded => (long)_ifRow.inDiscards;

	public override long OutgoingPacketsDiscarded => (long)_ifRow.outDiscards;

	public override long IncomingPacketsWithErrors => (long)_ifRow.inErrors;

	public override long OutgoingPacketsWithErrors => (long)_ifRow.outErrors;

	public override long IncomingUnknownProtocolPackets => (long)_ifRow.inUnknownProtos;

	internal SystemIPv4InterfaceStatistics(long index)
	{
		_ifRow = SystemIPInterfaceStatistics.GetIfEntry2(index);
	}
}
