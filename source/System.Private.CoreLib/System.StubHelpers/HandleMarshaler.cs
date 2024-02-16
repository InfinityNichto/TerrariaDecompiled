using System.Runtime.InteropServices;
using Internal.Runtime.CompilerServices;

namespace System.StubHelpers;

internal sealed class HandleMarshaler
{
	internal static IntPtr ConvertSafeHandleToNative(SafeHandle handle, ref CleanupWorkListElement cleanupWorkList)
	{
		if (Unsafe.IsNullRef(ref cleanupWorkList))
		{
			throw new InvalidOperationException(SR.Interop_Marshal_SafeHandle_InvalidOperation);
		}
		if (handle == null)
		{
			throw new ArgumentNullException("handle");
		}
		return StubHelpers.AddToCleanupList(ref cleanupWorkList, handle);
	}

	internal static void ThrowSafeHandleFieldChanged()
	{
		throw new NotSupportedException(SR.Interop_Marshal_CannotCreateSafeHandleField);
	}

	internal static void ThrowCriticalHandleFieldChanged()
	{
		throw new NotSupportedException(SR.Interop_Marshal_CannotCreateCriticalHandleField);
	}
}
