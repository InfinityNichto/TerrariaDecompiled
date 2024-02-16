using System.Runtime.InteropServices;

namespace System.StubHelpers;

internal static class CSTRMarshaler
{
	internal unsafe static IntPtr ConvertToNative(int flags, string strManaged, IntPtr pNativeBuffer)
	{
		if (strManaged == null)
		{
			return IntPtr.Zero;
		}
		byte* ptr = (byte*)(void*)pNativeBuffer;
		int num;
		if (ptr != null || Marshal.SystemMaxDBCSCharSize == 1)
		{
			num = checked((strManaged.Length + 1) * Marshal.SystemMaxDBCSCharSize + 1);
			bool flag = false;
			if (ptr == null)
			{
				ptr = (byte*)(void*)Marshal.AllocCoTaskMem(num);
				flag = true;
			}
			try
			{
				num = Marshal.StringToAnsiString(strManaged, ptr, num, (flags & 0xFF) != 0, flags >> 8 != 0);
			}
			catch (Exception) when (flag)
			{
				Marshal.FreeCoTaskMem((IntPtr)ptr);
				throw;
			}
		}
		else if (strManaged.Length == 0)
		{
			num = 0;
			ptr = (byte*)(void*)Marshal.AllocCoTaskMem(2);
		}
		else
		{
			byte[] array = AnsiCharMarshaler.DoAnsiConversion(strManaged, (flags & 0xFF) != 0, flags >> 8 != 0, out num);
			ptr = (byte*)(void*)Marshal.AllocCoTaskMem(num + 2);
			Buffer.Memmove(ref *ptr, ref MemoryMarshal.GetArrayDataReference(array), (nuint)num);
		}
		ptr[num] = 0;
		ptr[num + 1] = 0;
		return (IntPtr)ptr;
	}

	internal unsafe static string ConvertToManaged(IntPtr cstr)
	{
		if (IntPtr.Zero == cstr)
		{
			return null;
		}
		return new string((sbyte*)(void*)cstr);
	}

	internal static void ClearNative(IntPtr pNative)
	{
		Marshal.FreeCoTaskMem(pNative);
	}

	internal unsafe static void ConvertFixedToNative(int flags, string strManaged, IntPtr pNativeBuffer, int length)
	{
		if (strManaged == null)
		{
			if (length > 0)
			{
				*(sbyte*)(void*)pNativeBuffer = 0;
			}
			return;
		}
		int num = strManaged.Length;
		if (num >= length)
		{
			num = length - 1;
		}
		byte* ptr = (byte*)(void*)pNativeBuffer;
		bool flag = flags >> 8 != 0;
		bool flag2 = (flags & 0xFF) != 0;
		uint num2 = 0u;
		int num3;
		fixed (char* lpWideCharStr = strManaged)
		{
			num3 = Interop.Kernel32.WideCharToMultiByte(0u, (!flag2) ? 1024u : 0u, lpWideCharStr, num, ptr, length, IntPtr.Zero, flag ? new IntPtr(&num2) : IntPtr.Zero);
		}
		if (num2 != 0)
		{
			throw new ArgumentException(SR.Interop_Marshal_Unmappable_Char);
		}
		if (num3 == length)
		{
			num3--;
		}
		ptr[num3] = 0;
	}

	internal unsafe static string ConvertFixedToManaged(IntPtr cstr, int length)
	{
		int num = SpanHelpers.IndexOf(ref *(byte*)(void*)cstr, 0, length);
		if (num != -1)
		{
			length = num;
		}
		return new string((sbyte*)(void*)cstr, 0, length);
	}
}
