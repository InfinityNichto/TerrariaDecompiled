namespace System.Net.NetworkInformation;

internal sealed class SystemTcpConnectionInformation : TcpConnectionInformation
{
	private readonly IPEndPoint _localEndPoint;

	private readonly IPEndPoint _remoteEndPoint;

	private readonly TcpState _state;

	public override TcpState State => _state;

	public override IPEndPoint LocalEndPoint => _localEndPoint;

	public override IPEndPoint RemoteEndPoint => _remoteEndPoint;

	internal SystemTcpConnectionInformation(in global::Interop.IpHlpApi.MibTcpRow row)
	{
		_state = row.state;
		int port = (row.localPort1 << 8) | row.localPort2;
		int port2 = ((_state != TcpState.Listen) ? ((row.remotePort1 << 8) | row.remotePort2) : 0);
		_localEndPoint = new IPEndPoint(row.localAddr, port);
		_remoteEndPoint = new IPEndPoint(row.remoteAddr, port2);
	}

	internal SystemTcpConnectionInformation(in global::Interop.IpHlpApi.MibTcp6RowOwnerPid row)
	{
		_state = row.state;
		int port = (row.localPort1 << 8) | row.localPort2;
		int port2 = ((_state != TcpState.Listen) ? ((row.remotePort1 << 8) | row.remotePort2) : 0);
		_localEndPoint = new IPEndPoint(new IPAddress(row.localAddrAsSpan, row.localScopeId), port);
		_remoteEndPoint = new IPEndPoint(new IPAddress(row.remoteAddrAsSpan, row.remoteScopeId), port2);
	}
}
