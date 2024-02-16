using System.Net.Sockets;

namespace System.Net.NetworkInformation;

internal sealed class SystemTcpStatistics : TcpStatistics
{
	private readonly global::Interop.IpHlpApi.MibTcpStats _stats;

	public override long MinimumTransmissionTimeout => _stats.minimumRetransmissionTimeOut;

	public override long MaximumTransmissionTimeout => _stats.maximumRetransmissionTimeOut;

	public override long MaximumConnections => _stats.maximumConnections;

	public override long ConnectionsInitiated => _stats.activeOpens;

	public override long ConnectionsAccepted => _stats.passiveOpens;

	public override long FailedConnectionAttempts => _stats.failedConnectionAttempts;

	public override long ResetConnections => _stats.resetConnections;

	public override long CurrentConnections => _stats.currentConnections;

	public override long SegmentsReceived => _stats.segmentsReceived;

	public override long SegmentsSent => _stats.segmentsSent;

	public override long SegmentsResent => _stats.segmentsResent;

	public override long ErrorsReceived => _stats.errorsReceived;

	public override long ResetsSent => _stats.segmentsSentWithReset;

	public override long CumulativeConnections => _stats.cumulativeConnections;

	internal SystemTcpStatistics(AddressFamily family)
	{
		uint tcpStatisticsEx = global::Interop.IpHlpApi.GetTcpStatisticsEx(out _stats, family);
		if (tcpStatisticsEx != 0)
		{
			throw new NetworkInformationException((int)tcpStatisticsEx);
		}
	}
}
