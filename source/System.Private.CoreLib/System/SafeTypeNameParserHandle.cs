using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

namespace System;

internal sealed class SafeTypeNameParserHandle : SafeHandleZeroOrMinusOneIsInvalid
{
	[DllImport("QCall", CharSet = CharSet.Unicode)]
	private static extern void _ReleaseTypeNameParser(IntPtr pTypeNameParser);

	public SafeTypeNameParserHandle()
		: base(ownsHandle: true)
	{
	}

	protected override bool ReleaseHandle()
	{
		_ReleaseTypeNameParser(handle);
		handle = IntPtr.Zero;
		return true;
	}
}
