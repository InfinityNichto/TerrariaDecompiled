namespace System.Xml.Xsl.Runtime;

internal sealed class TokenInfo
{
	public char startChar;

	public int startIdx;

	public string formatString;

	public int length;

	private TokenInfo()
	{
	}

	public static TokenInfo CreateSeparator(string formatString, int startIdx, int tokLen)
	{
		TokenInfo tokenInfo = new TokenInfo();
		tokenInfo.startIdx = startIdx;
		tokenInfo.formatString = formatString;
		tokenInfo.length = tokLen;
		return tokenInfo;
	}

	public static TokenInfo CreateFormat(string formatString, int startIdx, int tokLen)
	{
		TokenInfo tokenInfo = new TokenInfo();
		tokenInfo.formatString = null;
		tokenInfo.length = 1;
		bool flag = false;
		char c = formatString[startIdx];
		switch (c)
		{
		default:
			if (CharUtil.IsDecimalDigitOne(c))
			{
				break;
			}
			if (CharUtil.IsDecimalDigitOne((char)(c + 1)))
			{
				int num = startIdx;
				do
				{
					tokenInfo.length++;
				}
				while (--tokLen > 0 && c == formatString[++num]);
				if (formatString[num] == (c = (char)(c + 1)))
				{
					break;
				}
			}
			flag = true;
			break;
		case '1':
		case 'A':
		case 'I':
		case 'a':
		case 'i':
			break;
		}
		if (tokLen != 1)
		{
			flag = true;
		}
		if (flag)
		{
			tokenInfo.startChar = '1';
			tokenInfo.length = 1;
		}
		else
		{
			tokenInfo.startChar = c;
		}
		return tokenInfo;
	}
}
