namespace System.Runtime.InteropServices.ComTypes;

[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
public struct TYPEDESC
{
	public IntPtr lpValue;

	public short vt;
}
