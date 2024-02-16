using System.Runtime.CompilerServices;

namespace System.StubHelpers;

internal static class MngdNativeArrayMarshaler
{
	internal struct MarshalerState
	{
		private IntPtr m_pElementMT;

		private IntPtr m_Array;

		private IntPtr m_pManagedNativeArrayMarshaler;

		private int m_NativeDataValid;

		private int m_BestFitMap;

		private int m_ThrowOnUnmappableChar;

		private short m_vt;
	}

	[MethodImpl(MethodImplOptions.InternalCall)]
	internal static extern void CreateMarshaler(IntPtr pMarshalState, IntPtr pMT, int dwFlags, IntPtr pManagedMarshaler);

	[MethodImpl(MethodImplOptions.InternalCall)]
	internal static extern void ConvertSpaceToNative(IntPtr pMarshalState, ref object pManagedHome, IntPtr pNativeHome);

	[MethodImpl(MethodImplOptions.InternalCall)]
	internal static extern void ConvertContentsToNative(IntPtr pMarshalState, ref object pManagedHome, IntPtr pNativeHome);

	[MethodImpl(MethodImplOptions.InternalCall)]
	internal static extern void ConvertSpaceToManaged(IntPtr pMarshalState, ref object pManagedHome, IntPtr pNativeHome, int cElements);

	[MethodImpl(MethodImplOptions.InternalCall)]
	internal static extern void ConvertContentsToManaged(IntPtr pMarshalState, ref object pManagedHome, IntPtr pNativeHome);

	[MethodImpl(MethodImplOptions.InternalCall)]
	internal static extern void ClearNative(IntPtr pMarshalState, ref object pManagedHome, IntPtr pNativeHome, int cElements);

	[MethodImpl(MethodImplOptions.InternalCall)]
	internal static extern void ClearNativeContents(IntPtr pMarshalState, ref object pManagedHome, IntPtr pNativeHome, int cElements);
}
