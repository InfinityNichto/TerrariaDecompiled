using System.Threading;
using Microsoft.Win32.SafeHandles;

namespace System.Net.Security;

internal sealed class SecurityContextTokenHandle : CriticalHandleZeroOrMinusOneIsInvalid
{
	private int _disposed;

	internal IntPtr DangerousGetHandle()
	{
		return handle;
	}

	protected override bool ReleaseHandle()
	{
		if (!IsInvalid && Interlocked.Increment(ref _disposed) == 1)
		{
			return global::Interop.Kernel32.CloseHandle(handle);
		}
		return true;
	}
}
