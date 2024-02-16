using System.Runtime.CompilerServices;

namespace System.StubHelpers;

internal static class ObjectMarshaler
{
	[MethodImpl(MethodImplOptions.InternalCall)]
	internal static extern void ConvertToNative(object objSrc, IntPtr pDstVariant);

	[MethodImpl(MethodImplOptions.InternalCall)]
	internal static extern object ConvertToManaged(IntPtr pSrcVariant);

	[MethodImpl(MethodImplOptions.InternalCall)]
	internal static extern void ClearNative(IntPtr pVariant);
}
