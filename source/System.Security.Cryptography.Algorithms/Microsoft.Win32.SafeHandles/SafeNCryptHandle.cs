using System;
using System.Runtime.InteropServices;

namespace Microsoft.Win32.SafeHandles;

internal class SafeNCryptHandle : SafeHandle
{
	public override bool IsInvalid => handle == IntPtr.Zero;

	public SafeNCryptHandle()
		: base(IntPtr.Zero, ownsHandle: true)
	{
	}

	protected override bool ReleaseHandle()
	{
		global::Interop.NCrypt.ErrorCode errorCode = global::Interop.NCrypt.NCryptFreeObject(handle);
		bool result = errorCode == global::Interop.NCrypt.ErrorCode.ERROR_SUCCESS;
		handle = IntPtr.Zero;
		return result;
	}
}
