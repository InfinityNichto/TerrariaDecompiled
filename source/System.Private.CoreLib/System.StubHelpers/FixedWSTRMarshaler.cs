namespace System.StubHelpers;

internal static class FixedWSTRMarshaler
{
	internal unsafe static void ConvertToNative(string strManaged, IntPtr nativeHome, int length)
	{
		ReadOnlySpan<char> readOnlySpan = strManaged;
		Span<char> destination = new Span<char>((void*)nativeHome, length);
		int num = Math.Min(readOnlySpan.Length, length - 1);
		readOnlySpan.Slice(0, num).CopyTo(destination);
		destination[num] = '\0';
	}

	internal unsafe static string ConvertToManaged(IntPtr nativeHome, int length)
	{
		int num = SpanHelpers.IndexOf(ref *(char*)(void*)nativeHome, '\0', length);
		if (num != -1)
		{
			length = num;
		}
		return new string((char*)(void*)nativeHome, 0, length);
	}
}
