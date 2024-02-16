namespace System.Net.Security;

internal sealed class SslConnectionInfo
{
	public int Protocol { get; }

	public TlsCipherSuite TlsCipherSuite { get; private set; }

	public int DataCipherAlg { get; private set; }

	public int DataKeySize { get; private set; }

	public int DataHashAlg { get; private set; }

	public int DataHashKeySize { get; private set; }

	public int KeyExchangeAlg { get; private set; }

	public int KeyExchKeySize { get; private set; }

	public SslConnectionInfo(SecPkgContext_ConnectionInfo interopConnectionInfo, TlsCipherSuite cipherSuite)
	{
		Protocol = interopConnectionInfo.Protocol;
		DataCipherAlg = interopConnectionInfo.DataCipherAlg;
		DataKeySize = interopConnectionInfo.DataKeySize;
		DataHashAlg = interopConnectionInfo.DataHashAlg;
		DataHashKeySize = interopConnectionInfo.DataHashKeySize;
		KeyExchangeAlg = interopConnectionInfo.KeyExchangeAlg;
		KeyExchKeySize = interopConnectionInfo.KeyExchKeySize;
		TlsCipherSuite = cipherSuite;
	}
}
