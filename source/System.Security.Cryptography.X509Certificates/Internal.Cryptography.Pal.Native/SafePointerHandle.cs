using System;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

namespace Internal.Cryptography.Pal.Native;

internal abstract class SafePointerHandle<T> : SafeHandle where T : SafeHandle, new()
{
	public sealed override bool IsInvalid => handle == IntPtr.Zero;

	public static T InvalidHandle => Microsoft.Win32.SafeHandles.SafeHandleCache<T>.GetInvalidHandle(() => new T());

	protected SafePointerHandle()
		: base(IntPtr.Zero, ownsHandle: true)
	{
	}

	protected override void Dispose(bool disposing)
	{
		if (!Microsoft.Win32.SafeHandles.SafeHandleCache<T>.IsCachedInvalidHandle(this))
		{
			base.Dispose(disposing);
		}
	}
}
