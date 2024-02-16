using System;
using System.Buffers;
using System.Runtime.InteropServices;
using System.Security.Cryptography;

namespace Internal.Cryptography;

internal sealed class CngAsnFormatter : AsnFormatter
{
	protected unsafe override string FormatNative(Oid oid, byte[] rawData, bool multiLine)
	{
		string s = string.Empty;
		if (oid != null && oid.Value != null)
		{
			s = oid.Value;
		}
		int dwFormatStrType = (multiLine ? 1 : 0);
		int pcbFormat = 0;
		IntPtr intPtr = Marshal.StringToHGlobalAnsi(s);
		char[] array = null;
		try
		{
			if (global::Interop.Crypt32.CryptFormatObject(1, 0, dwFormatStrType, IntPtr.Zero, (byte*)(void*)intPtr, rawData, rawData.Length, null, ref pcbFormat))
			{
				int num = (pcbFormat + 1) / 2;
				Span<char> span = ((num > 256) ? ((Span<char>)(array = ArrayPool<char>.Shared.Rent(num))) : stackalloc char[256]);
				Span<char> span2 = span;
				fixed (char* ptr = span2)
				{
					if (global::Interop.Crypt32.CryptFormatObject(1, 0, dwFormatStrType, IntPtr.Zero, (byte*)(void*)intPtr, rawData, rawData.Length, ptr, ref pcbFormat))
					{
						return new string(ptr);
					}
				}
			}
		}
		finally
		{
			Marshal.FreeHGlobal(intPtr);
			if (array != null)
			{
				ArrayPool<char>.Shared.Return(array);
			}
		}
		return null;
	}
}
