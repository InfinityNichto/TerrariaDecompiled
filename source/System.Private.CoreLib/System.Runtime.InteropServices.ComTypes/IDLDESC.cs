namespace System.Runtime.InteropServices.ComTypes;

[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
public struct IDLDESC
{
	public IntPtr dwReserved;

	public IDLFLAG wIDLFlags;
}
