namespace System.Net.Http;

internal enum HttpConnectionKind : byte
{
	Http,
	Https,
	Proxy,
	ProxyTunnel,
	SslProxyTunnel,
	ProxyConnect,
	SocksTunnel,
	SslSocksTunnel
}
