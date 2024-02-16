namespace System.Net.Security;

internal sealed class SafeFreeCredential_SECURITY : System.Net.Security.SafeFreeCredentials
{
	protected override bool ReleaseHandle()
	{
		return global::Interop.SspiCli.FreeCredentialsHandle(ref _handle) == 0;
	}
}
