using Microsoft.Win32.SafeHandles;

namespace System.Net.NetworkInformation;

internal sealed class SafeFreeMibTable : SafeHandleZeroOrMinusOneIsInvalid
{
	public SafeFreeMibTable()
		: base(ownsHandle: true)
	{
	}

	protected override bool ReleaseHandle()
	{
		global::Interop.IpHlpApi.FreeMibTable(handle);
		handle = IntPtr.Zero;
		return true;
	}
}
