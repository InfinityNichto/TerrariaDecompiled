using System.Runtime.CompilerServices;

namespace System.StubHelpers;

internal static class MngdRefCustomMarshaler
{
	[MethodImpl(MethodImplOptions.InternalCall)]
	internal static extern void CreateMarshaler(IntPtr pMarshalState, IntPtr pCMHelper);

	[MethodImpl(MethodImplOptions.InternalCall)]
	internal static extern void ConvertContentsToNative(IntPtr pMarshalState, ref object pManagedHome, IntPtr pNativeHome);

	[MethodImpl(MethodImplOptions.InternalCall)]
	internal static extern void ConvertContentsToManaged(IntPtr pMarshalState, ref object pManagedHome, IntPtr pNativeHome);

	[MethodImpl(MethodImplOptions.InternalCall)]
	internal static extern void ClearNative(IntPtr pMarshalState, ref object pManagedHome, IntPtr pNativeHome);

	[MethodImpl(MethodImplOptions.InternalCall)]
	internal static extern void ClearManaged(IntPtr pMarshalState, ref object pManagedHome, IntPtr pNativeHome);
}
