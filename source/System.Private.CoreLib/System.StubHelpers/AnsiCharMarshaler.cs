using System.Runtime.InteropServices;
using System.Text;

namespace System.StubHelpers;

internal static class AnsiCharMarshaler
{
	internal unsafe static byte[] DoAnsiConversion(string str, bool fBestFit, bool fThrowOnUnmappableChar, out int cbLength)
	{
		byte[] array = new byte[checked((str.Length + 1) * Marshal.SystemMaxDBCSCharSize)];
		fixed (byte* buffer = &array[0])
		{
			cbLength = Marshal.StringToAnsiString(str, buffer, array.Length, fBestFit, fThrowOnUnmappableChar);
		}
		return array;
	}

	internal unsafe static byte ConvertToNative(char managedChar, bool fBestFit, bool fThrowOnUnmappableChar)
	{
		int num = 2 * Marshal.SystemMaxDBCSCharSize;
		byte* ptr = stackalloc byte[(int)(uint)num];
		int num2 = Marshal.StringToAnsiString(managedChar.ToString(), ptr, num, fBestFit, fThrowOnUnmappableChar);
		return *ptr;
	}

	internal static char ConvertToManaged(byte nativeChar)
	{
		ReadOnlySpan<byte> bytes = new ReadOnlySpan<byte>(ref nativeChar, 1);
		string @string = Encoding.Default.GetString(bytes);
		return @string[0];
	}
}
