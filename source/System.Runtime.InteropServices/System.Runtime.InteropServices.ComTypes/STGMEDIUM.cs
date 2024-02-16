namespace System.Runtime.InteropServices.ComTypes;

public struct STGMEDIUM
{
	public TYMED tymed;

	public IntPtr unionmember;

	[MarshalAs(UnmanagedType.IUnknown)]
	public object? pUnkForRelease;
}
