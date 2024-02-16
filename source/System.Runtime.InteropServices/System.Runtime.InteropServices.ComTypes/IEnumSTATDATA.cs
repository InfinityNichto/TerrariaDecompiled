namespace System.Runtime.InteropServices.ComTypes;

[ComImport]
[Guid("00000105-0000-0000-C000-000000000046")]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
public interface IEnumSTATDATA
{
	[PreserveSig]
	int Next(int celt, [Out][MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 0)] STATDATA[] rgelt, [Out][MarshalAs(UnmanagedType.LPArray, SizeConst = 1)] int[] pceltFetched);

	[PreserveSig]
	int Skip(int celt);

	[PreserveSig]
	int Reset();

	void Clone(out IEnumSTATDATA newEnum);
}
