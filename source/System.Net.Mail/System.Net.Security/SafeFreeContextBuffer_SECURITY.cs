namespace System.Net.Security;

internal sealed class SafeFreeContextBuffer_SECURITY : System.Net.Security.SafeFreeContextBuffer
{
	protected override bool ReleaseHandle()
	{
		return global::Interop.SspiCli.FreeContextBuffer(handle) == 0;
	}
}
