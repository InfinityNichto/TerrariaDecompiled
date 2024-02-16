namespace System.Runtime.InteropServices;

[ComImport]
[Guid("00000003-0000-0000-C000-000000000046")]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
internal interface IMarshal
{
	[PreserveSig]
	int GetUnmarshalClass(ref Guid riid, IntPtr pv, int dwDestContext, IntPtr pvDestContext, int mshlflags, out Guid pCid);

	[PreserveSig]
	int GetMarshalSizeMax(ref Guid riid, IntPtr pv, int dwDestContext, IntPtr pvDestContext, int mshlflags, out int pSize);

	[PreserveSig]
	int MarshalInterface(IntPtr pStm, ref Guid riid, IntPtr pv, int dwDestContext, IntPtr pvDestContext, int mshlflags);

	[PreserveSig]
	int UnmarshalInterface(IntPtr pStm, ref Guid riid, out IntPtr ppv);

	[PreserveSig]
	int ReleaseMarshalData(IntPtr pStm);

	[PreserveSig]
	int DisconnectObject(int dwReserved);
}
