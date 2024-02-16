namespace System.Runtime.InteropServices.ComTypes;

[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
public struct STATSTG
{
	public string pwcsName;

	public int type;

	public long cbSize;

	public FILETIME mtime;

	public FILETIME ctime;

	public FILETIME atime;

	public int grfMode;

	public int grfLocksSupported;

	public Guid clsid;

	public int grfStateBits;

	public int reserved;
}
