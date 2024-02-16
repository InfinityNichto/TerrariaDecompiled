using System.Runtime.InteropServices;

namespace System.Net;

[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
internal struct SecPkgContext_CipherInfo
{
	private readonly int dwVersion;

	private readonly int dwProtocol;

	public readonly int dwCipherSuite;

	private readonly int dwBaseCipherSuite;

	private unsafe fixed char szCipherSuite[64];

	private unsafe fixed char szCipher[64];

	private readonly int dwCipherLen;

	private readonly int dwCipherBlockLen;

	private unsafe fixed char szHash[64];

	private readonly int dwHashLen;

	private unsafe fixed char szExchange[64];

	private readonly int dwMinExchangeLen;

	private readonly int dwMaxExchangeLen;

	private unsafe fixed char szCertificate[64];

	private readonly int dwKeyType;
}
