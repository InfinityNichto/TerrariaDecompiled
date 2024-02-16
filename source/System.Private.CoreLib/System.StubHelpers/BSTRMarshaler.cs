using System.Runtime.InteropServices;

namespace System.StubHelpers;

internal static class BSTRMarshaler
{
	internal unsafe static IntPtr ConvertToNative(string strManaged, IntPtr pNativeBuffer)
	{
		if (strManaged == null)
		{
			return IntPtr.Zero;
		}
		byte data;
		bool flag = strManaged.TryGetTrailByte(out data);
		uint num = (uint)(strManaged.Length * 2);
		if (flag)
		{
			num++;
		}
		byte* ptr;
		if (pNativeBuffer != IntPtr.Zero)
		{
			*(uint*)(void*)pNativeBuffer = num;
			ptr = (byte*)(void*)pNativeBuffer + 4;
		}
		else
		{
			ptr = (byte*)(void*)Marshal.AllocBSTRByteLen(num);
		}
		Buffer.Memmove(ref *(char*)ptr, ref strManaged.GetRawStringData(), (nuint)strManaged.Length + (nuint)1u);
		if (flag)
		{
			ptr[num - 1] = data;
		}
		return (IntPtr)ptr;
	}

	internal unsafe static string ConvertToManaged(IntPtr bstr)
	{
		if (IntPtr.Zero == bstr)
		{
			return null;
		}
		uint num = Marshal.SysStringByteLen(bstr);
		StubHelpers.CheckStringLength(num);
		string text = ((num != 1) ? new string((char*)(void*)bstr, 0, (int)(num / 2)) : string.FastAllocateString(0));
		if ((num & 1) == 1)
		{
			text.SetTrailByte(((byte*)(void*)bstr)[num - 1]);
		}
		return text;
	}

	internal static void ClearNative(IntPtr pNative)
	{
		Marshal.FreeBSTR(pNative);
	}
}
