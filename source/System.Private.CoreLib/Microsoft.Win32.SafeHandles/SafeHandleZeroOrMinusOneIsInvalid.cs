using System;
using System.Runtime.InteropServices;

namespace Microsoft.Win32.SafeHandles;

public abstract class SafeHandleZeroOrMinusOneIsInvalid : SafeHandle
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

	protected SafeHandleZeroOrMinusOneIsInvalid(bool ownsHandle)
		: base(IntPtr.Zero, ownsHandle)
	{
	}
}
