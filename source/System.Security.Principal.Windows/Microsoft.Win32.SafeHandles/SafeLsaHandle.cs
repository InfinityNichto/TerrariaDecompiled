using System;
using System.Runtime.InteropServices;

namespace Microsoft.Win32.SafeHandles;

internal sealed class SafeLsaHandle : SafeHandle
{
	public sealed override bool IsInvalid => handle == IntPtr.Zero;

	public SafeLsaHandle()
		: base(IntPtr.Zero, ownsHandle: true)
	{
	}

	protected sealed override bool ReleaseHandle()
	{
		int num = global::Interop.SspiCli.LsaDeregisterLogonProcess(handle);
		return num == 0;
	}
}
