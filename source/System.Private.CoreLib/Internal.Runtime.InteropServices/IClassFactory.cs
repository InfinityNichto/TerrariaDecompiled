using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

namespace Internal.Runtime.InteropServices;

[ComImport]
[ComVisible(false)]
[Guid("00000001-0000-0000-C000-000000000046")]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
public interface IClassFactory
{
	[RequiresUnreferencedCode("Built-in COM support is not trim compatible", Url = "https://aka.ms/dotnet-illink/com")]
	void CreateInstance([MarshalAs(UnmanagedType.Interface)] object? pUnkOuter, ref Guid riid, out IntPtr ppvObject);

	void LockServer([MarshalAs(UnmanagedType.Bool)] bool fLock);
}
