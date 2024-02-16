namespace System.Runtime.InteropServices.ComTypes;

[ComImport]
[Guid("0000010b-0000-0000-C000-000000000046")]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
public interface IPersistFile
{
	void GetClassID(out Guid pClassID);

	[PreserveSig]
	int IsDirty();

	void Load([MarshalAs(UnmanagedType.LPWStr)] string pszFileName, int dwMode);

	void Save([MarshalAs(UnmanagedType.LPWStr)] string? pszFileName, [MarshalAs(UnmanagedType.Bool)] bool fRemember);

	void SaveCompleted([MarshalAs(UnmanagedType.LPWStr)] string pszFileName);

	void GetCurFile([MarshalAs(UnmanagedType.LPWStr)] out string ppszFileName);
}
