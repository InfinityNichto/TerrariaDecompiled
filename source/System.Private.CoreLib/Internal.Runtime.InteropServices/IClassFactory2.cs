using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

namespace Internal.Runtime.InteropServices;

[ComImport]
[ComVisible(false)]
[Guid("B196B28F-BAB4-101A-B69C-00AA00341D07")]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
internal interface IClassFactory2 : IClassFactory
{
	[RequiresUnreferencedCode("Built-in COM support is not trim compatible", Url = "https://aka.ms/dotnet-illink/com")]
	new void CreateInstance([MarshalAs(UnmanagedType.Interface)] object pUnkOuter, ref Guid riid, out IntPtr ppvObject);

	new void LockServer([MarshalAs(UnmanagedType.Bool)] bool fLock);

	void GetLicInfo(ref LICINFO pLicInfo);

	void RequestLicKey(int dwReserved, [MarshalAs(UnmanagedType.BStr)] out string pBstrKey);

	void CreateInstanceLic([MarshalAs(UnmanagedType.Interface)] object pUnkOuter, [MarshalAs(UnmanagedType.Interface)] object pUnkReserved, ref Guid riid, [MarshalAs(UnmanagedType.BStr)] string bstrKey, out IntPtr ppvObject);
}
