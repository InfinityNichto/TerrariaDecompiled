namespace System.Net.Security;

internal sealed class SafeFreeContextBufferChannelBinding_SECURITY : SafeFreeContextBufferChannelBinding
{
	protected override bool ReleaseHandle()
	{
		return global::Interop.SspiCli.FreeContextBuffer(handle) == 0;
	}
}
