using System;
using System.Runtime.InteropServices;

namespace Microsoft.Win32.SafeHandles;

internal abstract class SafeBCryptHandle : SafeHandle, IDisposable
{
	public sealed override bool IsInvalid => handle == IntPtr.Zero;

	protected SafeBCryptHandle()
		: base(IntPtr.Zero, ownsHandle: true)
	{
	}

	protected abstract override bool ReleaseHandle();
}
