namespace System.Net.Http;

public sealed class SocketsHttpConnectionContext
{
	private readonly DnsEndPoint _dnsEndPoint;

	private readonly HttpRequestMessage _initialRequestMessage;

	public DnsEndPoint DnsEndPoint => _dnsEndPoint;

	public HttpRequestMessage InitialRequestMessage => _initialRequestMessage;

	internal SocketsHttpConnectionContext(DnsEndPoint dnsEndPoint, HttpRequestMessage initialRequestMessage)
	{
		_dnsEndPoint = dnsEndPoint;
		_initialRequestMessage = initialRequestMessage;
	}
}
