using System.Runtime.InteropServices;

namespace Microsoft.Win32.SafeHandles;

internal sealed class SafeLsaReturnBufferHandle : SafeBuffer
{
	public SafeLsaReturnBufferHandle()
		: base(ownsHandle: true)
	{
	}

	protected override bool ReleaseHandle()
	{
		return global::Interop.SspiCli.LsaFreeReturnBuffer(handle) >= 0;
	}
}
