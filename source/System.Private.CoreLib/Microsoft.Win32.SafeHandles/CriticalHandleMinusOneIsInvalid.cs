using System;
using System.Runtime.InteropServices;

namespace Microsoft.Win32.SafeHandles;

public abstract class CriticalHandleMinusOneIsInvalid : CriticalHandle
{
	public override bool IsInvalid => handle == new IntPtr(-1);

	protected CriticalHandleMinusOneIsInvalid()
		: base(new IntPtr(-1))
	{
	}
}
