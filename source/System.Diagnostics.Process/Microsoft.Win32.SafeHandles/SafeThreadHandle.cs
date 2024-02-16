using System;
using System.Runtime.InteropServices;

namespace Microsoft.Win32.SafeHandles;

internal sealed class SafeThreadHandle : SafeHandle
{
	public override bool IsInvalid
	{
		get
		{
			if (!(handle == IntPtr.Zero))
			{
				return handle == new IntPtr(-1);
			}
			return true;
		}
	}

	public SafeThreadHandle()
		: base(new IntPtr(0), ownsHandle: true)
	{
	}

	protected override bool ReleaseHandle()
	{
		return global::Interop.Kernel32.CloseHandle(handle);
	}
}
