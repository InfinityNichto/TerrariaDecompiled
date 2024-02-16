namespace System.Runtime.InteropServices.ComTypes;

[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
public struct PARAMDESC
{
	public IntPtr lpVarValue;

	public PARAMFLAG wParamFlags;
}
