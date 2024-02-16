namespace System.IO;

internal static class TextWriterExtensions
{
	public static void WritePartialString(this TextWriter writer, string value, int offset, int count)
	{
		if (offset == 0 && count == value.Length)
		{
			writer.Write(value);
			return;
		}
		ReadOnlySpan<char> buffer = value.AsSpan(offset, count);
		writer.Write(buffer);
	}
}
