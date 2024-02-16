namespace System.Runtime.InteropServices.ComTypes;

[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
public struct ELEMDESC
{
	[StructLayout(LayoutKind.Explicit, CharSet = CharSet.Unicode)]
	public struct DESCUNION
	{
		[FieldOffset(0)]
		public IDLDESC idldesc;

		[FieldOffset(0)]
		public PARAMDESC paramdesc;
	}

	public TYPEDESC tdesc;

	public DESCUNION desc;
}
