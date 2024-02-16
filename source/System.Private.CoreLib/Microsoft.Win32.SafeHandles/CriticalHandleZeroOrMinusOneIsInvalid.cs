using System;
using System.Runtime.InteropServices;

namespace Microsoft.Win32.SafeHandles;

public abstract class CriticalHandleZeroOrMinusOneIsInvalid : CriticalHandle
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

	protected CriticalHandleZeroOrMinusOneIsInvalid()
		: base(IntPtr.Zero)
	{
	}
}
