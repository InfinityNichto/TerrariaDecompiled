using System.Net.Sockets;

namespace System.Net.NetworkInformation;

internal sealed class SystemIcmpV6Statistics : IcmpV6Statistics
{
	private readonly global::Interop.IpHlpApi.MibIcmpInfoEx _stats;

	public override long MessagesSent => _stats.outStats.dwMsgs;

	public override long MessagesReceived => _stats.inStats.dwMsgs;

	public override long ErrorsSent => _stats.outStats.dwErrors;

	public override long ErrorsReceived => _stats.inStats.dwErrors;

	public override long DestinationUnreachableMessagesSent => _stats.outStats.rgdwTypeCount[1];

	public override long DestinationUnreachableMessagesReceived => _stats.inStats.rgdwTypeCount[1];

	public override long PacketTooBigMessagesSent => _stats.outStats.rgdwTypeCount[2];

	public override long PacketTooBigMessagesReceived => _stats.inStats.rgdwTypeCount[2];

	public override long TimeExceededMessagesSent => _stats.outStats.rgdwTypeCount[3];

	public override long TimeExceededMessagesReceived => _stats.inStats.rgdwTypeCount[3];

	public override long ParameterProblemsSent => _stats.outStats.rgdwTypeCount[4];

	public override long ParameterProblemsReceived => _stats.inStats.rgdwTypeCount[4];

	public override long EchoRequestsSent => _stats.outStats.rgdwTypeCount[128];

	public override long EchoRequestsReceived => _stats.inStats.rgdwTypeCount[128];

	public override long EchoRepliesSent => _stats.outStats.rgdwTypeCount[129];

	public override long EchoRepliesReceived => _stats.inStats.rgdwTypeCount[129];

	public override long MembershipQueriesSent => _stats.outStats.rgdwTypeCount[130];

	public override long MembershipQueriesReceived => _stats.inStats.rgdwTypeCount[130];

	public override long MembershipReportsSent => _stats.outStats.rgdwTypeCount[131];

	public override long MembershipReportsReceived => _stats.inStats.rgdwTypeCount[131];

	public override long MembershipReductionsSent => _stats.outStats.rgdwTypeCount[132];

	public override long MembershipReductionsReceived => _stats.inStats.rgdwTypeCount[132];

	public override long RouterAdvertisementsSent => _stats.outStats.rgdwTypeCount[134];

	public override long RouterAdvertisementsReceived => _stats.inStats.rgdwTypeCount[134];

	public override long RouterSolicitsSent => _stats.outStats.rgdwTypeCount[133];

	public override long RouterSolicitsReceived => _stats.inStats.rgdwTypeCount[133];

	public override long NeighborAdvertisementsSent => _stats.outStats.rgdwTypeCount[136];

	public override long NeighborAdvertisementsReceived => _stats.inStats.rgdwTypeCount[136];

	public override long NeighborSolicitsSent => _stats.outStats.rgdwTypeCount[135];

	public override long NeighborSolicitsReceived => _stats.inStats.rgdwTypeCount[135];

	public override long RedirectsSent => _stats.outStats.rgdwTypeCount[137];

	public override long RedirectsReceived => _stats.inStats.rgdwTypeCount[137];

	internal SystemIcmpV6Statistics()
	{
		uint icmpStatisticsEx = global::Interop.IpHlpApi.GetIcmpStatisticsEx(out _stats, AddressFamily.InterNetworkV6);
		if (icmpStatisticsEx != 0)
		{
			throw new NetworkInformationException((int)icmpStatisticsEx);
		}
	}
}
