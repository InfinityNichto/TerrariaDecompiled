using System;
using System.Runtime.InteropServices;
using System.Threading;

namespace Microsoft.Win32.SafeHandles;

internal static class SafeHandleCache<T> where T : SafeHandle
{
	private static T s_invalidHandle;

	internal static T GetInvalidHandle(Func<T> invalidHandleFactory)
	{
		T val = Volatile.Read(ref s_invalidHandle);
		if (val == null)
		{
			T val2 = invalidHandleFactory();
			val = Interlocked.CompareExchange(ref s_invalidHandle, val2, null);
			if (val == null)
			{
				GC.SuppressFinalize(val2);
				val = val2;
			}
			else
			{
				val2.Dispose();
			}
		}
		return val;
	}

	internal static bool IsCachedInvalidHandle(SafeHandle handle)
	{
		return handle == Volatile.Read(ref s_invalidHandle);
	}
}
