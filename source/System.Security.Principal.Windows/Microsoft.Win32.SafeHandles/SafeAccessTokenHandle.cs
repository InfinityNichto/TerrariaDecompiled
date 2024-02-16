using System;
using System.Runtime.InteropServices;

namespace Microsoft.Win32.SafeHandles;

public sealed class SafeAccessTokenHandle : SafeHandle
{
	public static SafeAccessTokenHandle InvalidHandle => new SafeAccessTokenHandle(IntPtr.Zero);

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

	public SafeAccessTokenHandle()
		: base(IntPtr.Zero, ownsHandle: true)
	{
	}

	public SafeAccessTokenHandle(IntPtr handle)
		: base(handle, ownsHandle: true)
	{
	}

	protected override bool ReleaseHandle()
	{
		return global::Interop.Kernel32.CloseHandle(handle);
	}
}
