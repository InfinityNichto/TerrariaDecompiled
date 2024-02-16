using System;
using System.Runtime.InteropServices;

namespace Microsoft.Win32.SafeHandles;

internal sealed class SafeBrotliEncoderHandle : SafeHandle
{
	public override bool IsInvalid => handle == IntPtr.Zero;

	public SafeBrotliEncoderHandle()
		: base(IntPtr.Zero, ownsHandle: true)
	{
	}

	protected override bool ReleaseHandle()
	{
		global::Interop.Brotli.BrotliEncoderDestroyInstance(handle);
		return true;
	}
}
