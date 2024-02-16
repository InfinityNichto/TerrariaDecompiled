namespace System.Net.Security;

internal sealed class SafeDeleteSslContext : SafeDeleteContext
{
	protected override bool ReleaseHandle()
	{
		_EffectiveCredential?.DangerousRelease();
		return global::Interop.SspiCli.DeleteSecurityContext(ref _handle) == 0;
	}
}
