using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace System.StubHelpers;

internal static class InterfaceMarshaler
{
	[MethodImpl(MethodImplOptions.InternalCall)]
	internal static extern IntPtr ConvertToNative(object objSrc, IntPtr itfMT, IntPtr classMT, int flags);

	[MethodImpl(MethodImplOptions.InternalCall)]
	internal static extern object ConvertToManaged(ref IntPtr ppUnk, IntPtr itfMT, IntPtr classMT, int flags);

	[DllImport("QCall")]
	internal static extern void ClearNative(IntPtr pUnk);
}
