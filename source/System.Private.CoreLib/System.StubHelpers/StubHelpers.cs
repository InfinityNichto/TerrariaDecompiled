using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace System.StubHelpers;

internal static class StubHelpers
{
	[ThreadStatic]
	private static Exception s_pendingExceptionObject;

	[MethodImpl(MethodImplOptions.InternalCall)]
	internal static extern void InitDeclaringType(IntPtr pMD);

	[MethodImpl(MethodImplOptions.InternalCall)]
	internal static extern IntPtr GetNDirectTarget(IntPtr pMD);

	[MethodImpl(MethodImplOptions.InternalCall)]
	internal static extern IntPtr GetDelegateTarget(Delegate pThis, ref IntPtr pStubArg);

	[MethodImpl(MethodImplOptions.InternalCall)]
	internal static extern void ClearLastError();

	[MethodImpl(MethodImplOptions.InternalCall)]
	internal static extern void SetLastError();

	[MethodImpl(MethodImplOptions.InternalCall)]
	internal static extern void ThrowInteropParamException(int resID, int paramIdx);

	internal static IntPtr AddToCleanupList(ref CleanupWorkListElement pCleanupWorkList, SafeHandle handle)
	{
		SafeHandleCleanupWorkListElement safeHandleCleanupWorkListElement = new SafeHandleCleanupWorkListElement(handle);
		CleanupWorkListElement.AddToCleanupList(ref pCleanupWorkList, safeHandleCleanupWorkListElement);
		return safeHandleCleanupWorkListElement.AddRef();
	}

	internal static void KeepAliveViaCleanupList(ref CleanupWorkListElement pCleanupWorkList, object obj)
	{
		KeepAliveCleanupWorkListElement newElement = new KeepAliveCleanupWorkListElement(obj);
		CleanupWorkListElement.AddToCleanupList(ref pCleanupWorkList, newElement);
	}

	internal static void DestroyCleanupList(ref CleanupWorkListElement pCleanupWorkList)
	{
		if (pCleanupWorkList != null)
		{
			pCleanupWorkList.Destroy();
			pCleanupWorkList = null;
		}
	}

	internal static Exception GetHRExceptionObject(int hr)
	{
		Exception ex = InternalGetHRExceptionObject(hr);
		ex.InternalPreserveStackTrace();
		return ex;
	}

	[MethodImpl(MethodImplOptions.InternalCall)]
	internal static extern Exception InternalGetHRExceptionObject(int hr);

	internal static Exception GetCOMHRExceptionObject(int hr, IntPtr pCPCMD, object pThis)
	{
		Exception ex = InternalGetCOMHRExceptionObject(hr, pCPCMD, pThis);
		ex.InternalPreserveStackTrace();
		return ex;
	}

	[MethodImpl(MethodImplOptions.InternalCall)]
	internal static extern Exception InternalGetCOMHRExceptionObject(int hr, IntPtr pCPCMD, object pThis);

	internal static Exception GetPendingExceptionObject()
	{
		Exception ex = s_pendingExceptionObject;
		ex?.InternalPreserveStackTrace();
		s_pendingExceptionObject = null;
		return ex;
	}

	[MethodImpl(MethodImplOptions.InternalCall)]
	internal static extern IntPtr CreateCustomMarshalerHelper(IntPtr pMD, int paramToken, IntPtr hndManagedType);

	internal static IntPtr SafeHandleAddRef(SafeHandle pHandle, ref bool success)
	{
		if (pHandle == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.pHandle, ExceptionResource.ArgumentNull_SafeHandle);
		}
		pHandle.DangerousAddRef(ref success);
		return pHandle.DangerousGetHandle();
	}

	internal static void SafeHandleRelease(SafeHandle pHandle)
	{
		if (pHandle == null)
		{
			ThrowHelper.ThrowArgumentNullException(ExceptionArgument.pHandle, ExceptionResource.ArgumentNull_SafeHandle);
		}
		pHandle.DangerousRelease();
	}

	[MethodImpl(MethodImplOptions.InternalCall)]
	internal static extern IntPtr GetCOMIPFromRCW(object objSrc, IntPtr pCPCMD, out IntPtr ppTarget, out bool pfNeedsRelease);

	[MethodImpl(MethodImplOptions.InternalCall)]
	internal static extern IntPtr ProfilerBeginTransitionCallback(IntPtr pSecretParam, IntPtr pThread, object pThis);

	[MethodImpl(MethodImplOptions.InternalCall)]
	internal static extern void ProfilerEndTransitionCallback(IntPtr pMD, IntPtr pThread);

	internal static void CheckStringLength(int length)
	{
		CheckStringLength((uint)length);
	}

	internal static void CheckStringLength(uint length)
	{
		if (length > 2147483632)
		{
			throw new MarshalDirectiveException(SR.Marshaler_StringTooLong);
		}
	}

	[MethodImpl(MethodImplOptions.InternalCall)]
	internal unsafe static extern void FmtClassUpdateNativeInternal(object obj, byte* pNative, ref CleanupWorkListElement pCleanupWorkList);

	[MethodImpl(MethodImplOptions.InternalCall)]
	internal unsafe static extern void FmtClassUpdateCLRInternal(object obj, byte* pNative);

	[MethodImpl(MethodImplOptions.InternalCall)]
	internal unsafe static extern void LayoutDestroyNativeInternal(object obj, byte* pNative);

	[MethodImpl(MethodImplOptions.InternalCall)]
	internal static extern object AllocateInternal(IntPtr typeHandle);

	[MethodImpl(MethodImplOptions.InternalCall)]
	internal static extern void MarshalToUnmanagedVaListInternal(IntPtr va_list, uint vaListSize, IntPtr pArgIterator);

	[MethodImpl(MethodImplOptions.InternalCall)]
	internal static extern void MarshalToManagedVaListInternal(IntPtr va_list, IntPtr pArgIterator);

	[MethodImpl(MethodImplOptions.InternalCall)]
	internal static extern uint CalcVaListSize(IntPtr va_list);

	[MethodImpl(MethodImplOptions.InternalCall)]
	internal static extern void ValidateObject(object obj, IntPtr pMD, object pThis);

	[MethodImpl(MethodImplOptions.InternalCall)]
	internal static extern void LogPinnedArgument(IntPtr localDesc, IntPtr nativeArg);

	[MethodImpl(MethodImplOptions.InternalCall)]
	internal static extern void ValidateByref(IntPtr byref, IntPtr pMD, object pThis);

	[MethodImpl(MethodImplOptions.InternalCall)]
	[Intrinsic]
	internal static extern IntPtr GetStubContext();

	[MethodImpl(MethodImplOptions.InternalCall)]
	internal static extern IntPtr GetStubContextAddr();

	[MethodImpl(MethodImplOptions.InternalCall)]
	internal static extern void ArrayTypeCheck(object o, object[] arr);

	[MethodImpl(MethodImplOptions.InternalCall)]
	internal static extern void MulticastDebuggerTraceHelper(object o, int count);

	[MethodImpl(MethodImplOptions.InternalCall)]
	internal static extern IntPtr NextCallReturnAddress();
}
