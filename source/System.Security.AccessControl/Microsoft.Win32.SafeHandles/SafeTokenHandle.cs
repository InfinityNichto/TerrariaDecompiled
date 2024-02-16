using System;
using System.Runtime.InteropServices;

namespace Microsoft.Win32.SafeHandles;

internal sealed class SafeTokenHandle : SafeHandle
{
	public override bool IsInvalid
	{
		get
		{
			if (!(handle == new IntPtr(0)))
			{
				return handle == new IntPtr(-1);
			}
			return true;
		}
	}

	public SafeTokenHandle()
		: base(IntPtr.Zero, ownsHandle: true)
	{
	}

	internal SafeTokenHandle(IntPtr handle)
		: base(IntPtr.Zero, ownsHandle: true)
	{
		SetHandle(handle);
	}

	protected override bool ReleaseHandle()
	{
		return global::Interop.Kernel32.CloseHandle(handle);
	}
}
