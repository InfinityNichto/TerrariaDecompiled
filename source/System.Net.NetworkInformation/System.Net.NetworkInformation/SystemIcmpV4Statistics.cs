namespace System.Net.NetworkInformation;

internal sealed class SystemIcmpV4Statistics : IcmpV4Statistics
{
	private readonly global::Interop.IpHlpApi.MibIcmpInfo _stats;

	public override long MessagesSent => _stats.outStats.messages;

	public override long MessagesReceived => _stats.inStats.messages;

	public override long ErrorsSent => _stats.outStats.errors;

	public override long ErrorsReceived => _stats.inStats.errors;

	public override long DestinationUnreachableMessagesSent => _stats.outStats.destinationUnreachables;

	public override long DestinationUnreachableMessagesReceived => _stats.inStats.destinationUnreachables;

	public override long TimeExceededMessagesSent => _stats.outStats.timeExceeds;

	public override long TimeExceededMessagesReceived => _stats.inStats.timeExceeds;

	public override long ParameterProblemsSent => _stats.outStats.parameterProblems;

	public override long ParameterProblemsReceived => _stats.inStats.parameterProblems;

	public override long SourceQuenchesSent => _stats.outStats.sourceQuenches;

	public override long SourceQuenchesReceived => _stats.inStats.sourceQuenches;

	public override long RedirectsSent => _stats.outStats.redirects;

	public override long RedirectsReceived => _stats.inStats.redirects;

	public override long EchoRequestsSent => _stats.outStats.echoRequests;

	public override long EchoRequestsReceived => _stats.inStats.echoRequests;

	public override long EchoRepliesSent => _stats.outStats.echoReplies;

	public override long EchoRepliesReceived => _stats.inStats.echoReplies;

	public override long TimestampRequestsSent => _stats.outStats.timestampRequests;

	public override long TimestampRequestsReceived => _stats.inStats.timestampRequests;

	public override long TimestampRepliesSent => _stats.outStats.timestampReplies;

	public override long TimestampRepliesReceived => _stats.inStats.timestampReplies;

	public override long AddressMaskRequestsSent => _stats.outStats.addressMaskRequests;

	public override long AddressMaskRequestsReceived => _stats.inStats.addressMaskRequests;

	public override long AddressMaskRepliesSent => _stats.outStats.addressMaskReplies;

	public override long AddressMaskRepliesReceived => _stats.inStats.addressMaskReplies;

	internal SystemIcmpV4Statistics()
	{
		uint icmpStatistics = global::Interop.IpHlpApi.GetIcmpStatistics(out _stats);
		if (icmpStatistics != 0)
		{
			throw new NetworkInformationException((int)icmpStatistics);
		}
	}
}
