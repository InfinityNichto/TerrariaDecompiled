using System.Collections.Generic;

namespace System.Net.Security;

internal sealed class CipherSuitesPolicyPal
{
	internal CipherSuitesPolicyPal(IEnumerable<TlsCipherSuite> allowedCipherSuites)
	{
		throw new PlatformNotSupportedException(System.SR.net_ssl_ciphersuites_policy_not_supported);
	}

	internal IEnumerable<TlsCipherSuite> GetCipherSuites()
	{
		return null;
	}
}
