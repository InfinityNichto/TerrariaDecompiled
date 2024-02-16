using System.Collections.Generic;

namespace System.Net.Security;

public sealed class CipherSuitesPolicy
{
	internal CipherSuitesPolicyPal Pal { get; private set; }

	[CLSCompliant(false)]
	public IEnumerable<TlsCipherSuite> AllowedCipherSuites
	{
		get
		{
			foreach (TlsCipherSuite cipherSuite in Pal.GetCipherSuites())
			{
				yield return cipherSuite;
			}
		}
	}

	[CLSCompliant(false)]
	public CipherSuitesPolicy(IEnumerable<TlsCipherSuite> allowedCipherSuites)
	{
		if (allowedCipherSuites == null)
		{
			throw new ArgumentNullException("allowedCipherSuites");
		}
		Pal = new CipherSuitesPolicyPal(allowedCipherSuites);
	}
}
