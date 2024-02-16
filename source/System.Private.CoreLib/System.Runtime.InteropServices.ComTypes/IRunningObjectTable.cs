namespace System.Runtime.InteropServices.ComTypes;

[ComImport]
[Guid("00000010-0000-0000-C000-000000000046")]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
public interface IRunningObjectTable
{
	int Register(int grfFlags, [MarshalAs(UnmanagedType.Interface)] object punkObject, IMoniker pmkObjectName);

	void Revoke(int dwRegister);

	[PreserveSig]
	int IsRunning(IMoniker pmkObjectName);

	[PreserveSig]
	int GetObject(IMoniker pmkObjectName, [MarshalAs(UnmanagedType.Interface)] out object ppunkObject);

	void NoteChangeTime(int dwRegister, ref FILETIME pfiletime);

	[PreserveSig]
	int GetTimeOfLastChange(IMoniker pmkObjectName, out FILETIME pfiletime);

	void EnumRunning(out IEnumMoniker ppenumMoniker);
}
