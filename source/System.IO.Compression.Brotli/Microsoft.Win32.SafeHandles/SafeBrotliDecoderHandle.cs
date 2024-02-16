using System;
using System.Runtime.InteropServices;

namespace Microsoft.Win32.SafeHandles;

internal sealed class SafeBrotliDecoderHandle : SafeHandle
{
	public override bool IsInvalid => handle == IntPtr.Zero;

	public SafeBrotliDecoderHandle()
		: base(IntPtr.Zero, ownsHandle: true)
	{
	}

	protected override bool ReleaseHandle()
	{
		global::Interop.Brotli.BrotliDecoderDestroyInstance(handle);
		return true;
	}
}
