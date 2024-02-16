namespace System.Text;

internal static class EncodingExtensions
{
	public static Encoding RemovePreamble(this Encoding encoding)
	{
		if (encoding.Preamble.Length == 0)
		{
			return encoding;
		}
		return new ConsoleEncoding(encoding);
	}
}
