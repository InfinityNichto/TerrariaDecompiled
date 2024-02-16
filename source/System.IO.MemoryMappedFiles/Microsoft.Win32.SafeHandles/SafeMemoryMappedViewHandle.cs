using System;
using System.Runtime.InteropServices;

namespace Microsoft.Win32.SafeHandles;

public sealed class SafeMemoryMappedViewHandle : SafeBuffer
{
	public SafeMemoryMappedViewHandle()
		: base(ownsHandle: true)
	{
	}

	protected override bool ReleaseHandle()
	{
		IntPtr lpBaseAddress = handle;
		handle = IntPtr.Zero;
		return global::Interop.Kernel32.UnmapViewOfFile(lpBaseAddress);
	}
}
