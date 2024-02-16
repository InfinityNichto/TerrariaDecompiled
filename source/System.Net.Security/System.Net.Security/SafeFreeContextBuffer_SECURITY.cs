namespace System.Net.Security;

internal sealed class SafeFreeContextBuffer_SECURITY : SafeFreeContextBuffer
{
	protected override bool ReleaseHandle()
	{
		return global::Interop.SspiCli.FreeContextBuffer(handle) == 0;
	}
}
