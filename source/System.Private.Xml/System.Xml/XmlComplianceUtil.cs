using System.Text;

namespace System.Xml;

internal static class XmlComplianceUtil
{
	public static string NonCDataNormalize(string value)
	{
		int length = value.Length;
		if (length <= 0)
		{
			return string.Empty;
		}
		int num = 0;
		StringBuilder stringBuilder = null;
		while (XmlCharType.IsWhiteSpace(value[num]))
		{
			num++;
			if (num == length)
			{
				return " ";
			}
		}
		int num2 = num;
		while (num2 < length)
		{
			if (!XmlCharType.IsWhiteSpace(value[num2]))
			{
				num2++;
				continue;
			}
			int i;
			for (i = num2 + 1; i < length && XmlCharType.IsWhiteSpace(value[i]); i++)
			{
			}
			if (i == length)
			{
				if (stringBuilder == null)
				{
					return value.Substring(num, num2 - num);
				}
				stringBuilder.Append(value, num, num2 - num);
				return stringBuilder.ToString();
			}
			if (i > num2 + 1 || value[num2] != ' ')
			{
				if (stringBuilder == null)
				{
					stringBuilder = new StringBuilder(length);
				}
				stringBuilder.Append(value, num, num2 - num);
				stringBuilder.Append(' ');
				num = i;
				num2 = i;
			}
			else
			{
				num2++;
			}
		}
		if (stringBuilder != null)
		{
			if (num < num2)
			{
				stringBuilder.Append(value, num, num2 - num);
			}
			return stringBuilder.ToString();
		}
		if (num > 0)
		{
			return value.Substring(num, length - num);
		}
		return value;
	}

	public static string CDataNormalize(string value)
	{
		int length = value.Length;
		if (length <= 0)
		{
			return string.Empty;
		}
		int num = 0;
		int num2 = 0;
		StringBuilder stringBuilder = null;
		while (num < length)
		{
			char c = value[num];
			switch (c)
			{
			default:
				num++;
				break;
			case '\t':
			case '\n':
			case '\r':
				if (stringBuilder == null)
				{
					stringBuilder = new StringBuilder(length);
				}
				if (num2 < num)
				{
					stringBuilder.Append(value, num2, num - num2);
				}
				stringBuilder.Append(' ');
				num = ((c != '\r' || num + 1 >= length || value[num + 1] != '\n') ? (num + 1) : (num + 2));
				num2 = num;
				break;
			}
		}
		if (stringBuilder == null)
		{
			return value;
		}
		if (num > num2)
		{
			stringBuilder.Append(value, num2, num - num2);
		}
		return stringBuilder.ToString();
	}

	public static bool IsValidLanguageID(char[] value, int startPos, int length)
	{
		int num = length;
		if (num < 2)
		{
			return false;
		}
		bool flag = false;
		int num2 = startPos;
		char c = value[num2];
		if (XmlCharType.IsLetter(c))
		{
			if (XmlCharType.IsLetter(value[++num2]))
			{
				if (num == 2)
				{
					return true;
				}
				num--;
				num2++;
			}
			else if ('I' != c && 'i' != c && 'X' != c && 'x' != c)
			{
				return false;
			}
			if (value[num2] != '-')
			{
				return false;
			}
			num -= 2;
			while (num-- > 0)
			{
				c = value[++num2];
				if (XmlCharType.IsLetter(c))
				{
					flag = true;
					continue;
				}
				if (c == '-' && flag)
				{
					flag = false;
					continue;
				}
				return false;
			}
			if (flag)
			{
				return true;
			}
		}
		return false;
	}
}
