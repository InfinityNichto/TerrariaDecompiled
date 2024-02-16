namespace System.Net.Security;

internal sealed class SafeDeleteSslContext : System.Net.Security.SafeDeleteContext
{
	protected override bool ReleaseHandle()
	{
		_EffectiveCredential?.DangerousRelease();
		return global::Interop.SspiCli.DeleteSecurityContext(ref _handle) == 0;
	}
}
