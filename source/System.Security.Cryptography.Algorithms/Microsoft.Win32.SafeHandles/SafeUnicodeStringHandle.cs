using System;
using System.Runtime.InteropServices;

namespace Microsoft.Win32.SafeHandles;

internal sealed class SafeUnicodeStringHandle : SafeHandle
{
	public sealed override bool IsInvalid => handle == IntPtr.Zero;

	public SafeUnicodeStringHandle(string s)
		: base(IntPtr.Zero, ownsHandle: true)
	{
		handle = Marshal.StringToHGlobalUni(s);
	}

	public unsafe SafeUnicodeStringHandle(ReadOnlySpan<char> s)
		: base(IntPtr.Zero, ownsHandle: true)
	{
		checked
		{
			if (s != default(ReadOnlySpan<char>))
			{
				int num = s.Length + 1;
				int cb = num * 2;
				handle = Marshal.AllocHGlobal(cb);
				Span<char> destination = new Span<char>(handle.ToPointer(), num);
				s.CopyTo(destination);
				destination[s.Length] = '\0';
			}
		}
	}

	protected sealed override bool ReleaseHandle()
	{
		Marshal.FreeHGlobal(handle);
		return true;
	}
}
