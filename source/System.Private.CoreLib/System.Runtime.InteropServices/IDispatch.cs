using System.Runtime.InteropServices.ComTypes;

namespace System.Runtime.InteropServices;

[ComImport]
[Guid("00020400-0000-0000-C000-000000000046")]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
internal interface IDispatch
{
	int GetTypeInfoCount();

	ITypeInfo GetTypeInfo(int iTInfo, int lcid);

	void GetIDsOfNames(ref Guid riid, [In][MarshalAs(UnmanagedType.LPArray, ArraySubType = UnmanagedType.LPWStr, SizeParamIndex = 2)] string[] rgszNames, int cNames, int lcid, [Out] int[] rgDispId);

	void Invoke(int dispIdMember, ref Guid riid, int lcid, InvokeFlags wFlags, ref DISPPARAMS pDispParams, IntPtr pVarResult, IntPtr pExcepInfo, IntPtr puArgErr);
}
