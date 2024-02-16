namespace System.Text;

internal static class EncodingHelper
{
	internal static Encoding GetSupportedConsoleEncoding(int codepage)
	{
		int codePage = Encoding.GetEncoding(0).CodePage;
		if (codePage == codepage || codePage != 65001)
		{
			return Encoding.GetEncoding(codepage);
		}
		if (codepage != 65001)
		{
			return new OSEncoding(codepage);
		}
		return new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);
	}
}
