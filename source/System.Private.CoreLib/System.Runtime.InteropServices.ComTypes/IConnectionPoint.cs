namespace System.Runtime.InteropServices.ComTypes;

[ComImport]
[Guid("B196B286-BAB4-101A-B69C-00AA00341D07")]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
public interface IConnectionPoint
{
	void GetConnectionInterface(out Guid pIID);

	void GetConnectionPointContainer(out IConnectionPointContainer ppCPC);

	void Advise([MarshalAs(UnmanagedType.Interface)] object pUnkSink, out int pdwCookie);

	void Unadvise(int dwCookie);

	void EnumConnections(out IEnumConnections ppEnum);
}
