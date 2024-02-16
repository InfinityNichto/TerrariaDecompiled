using System;

namespace Internal.Cryptography.Pal.Native;

internal struct CERT_POLICY_INFO
{
	public IntPtr pszPolicyIdentifier;

	public int cPolicyQualifier;

	public IntPtr rgPolicyQualifier;
}
