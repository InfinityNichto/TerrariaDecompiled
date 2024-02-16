using Microsoft.Win32.SafeHandles;

namespace System.Net.NetworkInformation;

internal sealed class SafeCancelMibChangeNotify : SafeHandleZeroOrMinusOneIsInvalid
{
	public SafeCancelMibChangeNotify()
		: base(ownsHandle: true)
	{
	}

	protected override bool ReleaseHandle()
	{
		uint num = global::Interop.IpHlpApi.CancelMibChangeNotify2(handle);
		handle = IntPtr.Zero;
		return num == 0;
	}
}
