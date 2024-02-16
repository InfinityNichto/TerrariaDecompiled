namespace Internal.Cryptography.Pal.Native;

internal struct CERT_POLICIES_INFO
{
	public int cPolicyInfo;

	public unsafe CERT_POLICY_INFO* rgPolicyInfo;
}
