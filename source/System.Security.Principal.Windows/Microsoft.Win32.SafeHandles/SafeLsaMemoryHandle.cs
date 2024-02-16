using System.Runtime.InteropServices;

namespace Microsoft.Win32.SafeHandles;

internal sealed class SafeLsaMemoryHandle : SafeBuffer
{
	public SafeLsaMemoryHandle()
		: base(ownsHandle: true)
	{
	}

	protected override bool ReleaseHandle()
	{
		return global::Interop.Advapi32.LsaFreeMemory(handle) == 0;
	}
}
