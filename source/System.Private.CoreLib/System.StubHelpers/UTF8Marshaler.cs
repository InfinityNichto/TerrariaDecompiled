using System.Runtime.InteropServices;
using System.Text;

namespace System.StubHelpers;

internal static class UTF8Marshaler
{
	internal unsafe static IntPtr ConvertToNative(int flags, string strManaged, IntPtr pNativeBuffer)
	{
		if (strManaged == null)
		{
			return IntPtr.Zero;
		}
		byte* ptr = (byte*)(void*)pNativeBuffer;
		int cbNativeBuffer;
		if (ptr != null)
		{
			cbNativeBuffer = (strManaged.Length + 1) * 3;
			cbNativeBuffer = strManaged.GetBytesFromEncoding(ptr, cbNativeBuffer, Encoding.UTF8);
		}
		else
		{
			cbNativeBuffer = Encoding.UTF8.GetByteCount(strManaged);
			ptr = (byte*)(void*)Marshal.AllocCoTaskMem(cbNativeBuffer + 1);
			strManaged.GetBytesFromEncoding(ptr, cbNativeBuffer, Encoding.UTF8);
		}
		ptr[cbNativeBuffer] = 0;
		return (IntPtr)ptr;
	}

	internal unsafe static string ConvertToManaged(IntPtr cstr)
	{
		if (IntPtr.Zero == cstr)
		{
			return null;
		}
		byte* ptr = (byte*)(void*)cstr;
		int byteLength = string.strlen(ptr);
		return string.CreateStringFromEncoding(ptr, byteLength, Encoding.UTF8);
	}

	internal static void ClearNative(IntPtr pNative)
	{
		Marshal.FreeCoTaskMem(pNative);
	}
}
