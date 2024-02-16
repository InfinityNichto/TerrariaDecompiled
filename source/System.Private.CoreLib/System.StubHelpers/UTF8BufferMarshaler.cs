using System.Text;

namespace System.StubHelpers;

internal static class UTF8BufferMarshaler
{
	internal unsafe static IntPtr ConvertToNative(StringBuilder sb, IntPtr pNativeBuffer, int flags)
	{
		if (sb == null)
		{
			return IntPtr.Zero;
		}
		string text = sb.ToString();
		int byteCount = Encoding.UTF8.GetByteCount(text);
		byte* ptr = (byte*)(void*)pNativeBuffer;
		byteCount = text.GetBytesFromEncoding(ptr, byteCount, Encoding.UTF8);
		ptr[byteCount] = 0;
		return (IntPtr)ptr;
	}

	internal unsafe static void ConvertToManaged(StringBuilder sb, IntPtr pNative)
	{
		if (!(pNative == IntPtr.Zero))
		{
			byte* ptr = (byte*)(void*)pNative;
			int length = string.strlen(ptr);
			sb.ReplaceBufferUtf8Internal(new ReadOnlySpan<byte>(ptr, length));
		}
	}
}
