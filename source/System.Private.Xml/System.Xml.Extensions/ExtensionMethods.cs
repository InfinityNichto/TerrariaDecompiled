namespace System.Xml.Extensions;

internal static class ExtensionMethods
{
	internal static Uri ToUri(string s)
	{
		if (s != null && s.Length > 0)
		{
			s = s.Trim(' ', '\t', '\n', '\r');
			if (s.Length == 0 || s.IndexOf("##", StringComparison.Ordinal) != -1)
			{
				throw new FormatException(System.SR.Format(System.SR.XmlConvert_BadFormat, s, "Uri"));
			}
		}
		if (!Uri.TryCreate(s, UriKind.RelativeOrAbsolute, out Uri result))
		{
			throw new FormatException(System.SR.Format(System.SR.XmlConvert_BadFormat, s, "Uri"));
		}
		return result;
	}
}
