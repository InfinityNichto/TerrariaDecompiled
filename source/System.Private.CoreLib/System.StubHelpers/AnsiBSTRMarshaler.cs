using System.Runtime.InteropServices;

namespace System.StubHelpers;

internal static class AnsiBSTRMarshaler
{
	internal unsafe static IntPtr ConvertToNative(int flags, string strManaged)
	{
		if (strManaged == null)
		{
			return IntPtr.Zero;
		}
		byte[] array = null;
		int cbLength = 0;
		if (strManaged.Length > 0)
		{
			array = AnsiCharMarshaler.DoAnsiConversion(strManaged, (flags & 0xFF) != 0, flags >> 8 != 0, out cbLength);
		}
		uint num = (uint)cbLength;
		IntPtr intPtr = Marshal.AllocBSTRByteLen(num);
		if (array != null)
		{
			Buffer.Memmove(ref *(byte*)(void*)intPtr, ref MemoryMarshal.GetArrayDataReference(array), num);
		}
		return intPtr;
	}

	internal unsafe static string ConvertToManaged(IntPtr bstr)
	{
		if (IntPtr.Zero == bstr)
		{
			return null;
		}
		return new string((sbyte*)(void*)bstr);
	}

	internal static void ClearNative(IntPtr pNative)
	{
		Marshal.FreeBSTR(pNative);
	}
}
