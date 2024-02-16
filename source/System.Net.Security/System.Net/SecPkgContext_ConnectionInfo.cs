namespace System.Net;

internal struct SecPkgContext_ConnectionInfo
{
	public readonly int Protocol;

	public readonly int DataCipherAlg;

	public readonly int DataKeySize;

	public readonly int DataHashAlg;

	public readonly int DataHashKeySize;

	public readonly int KeyExchangeAlg;

	public readonly int KeyExchKeySize;
}
