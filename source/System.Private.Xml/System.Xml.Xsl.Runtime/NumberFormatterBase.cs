using System.Text;

namespace System.Xml.Xsl.Runtime;

internal abstract class NumberFormatterBase
{
	private static readonly int[] s_romanDigitValue = new int[13]
	{
		1, 4, 5, 9, 10, 40, 50, 90, 100, 400,
		500, 900, 1000
	};

	public static void ConvertToAlphabetic(StringBuilder sb, double val, char firstChar, int totalChars)
	{
		char[] array = new char[7];
		int num = 7;
		int num2 = (int)val;
		while (num2 > totalChars)
		{
			int num3 = --num2 / totalChars;
			array[--num] = (char)(firstChar + (num2 - num3 * totalChars));
			num2 = num3;
		}
		array[--num] = (char)(firstChar + --num2);
		sb.Append(array, num, 7 - num);
	}

	public static void ConvertToRoman(StringBuilder sb, double val, bool upperCase)
	{
		int num = (int)val;
		string value = (upperCase ? "IIVIXXLXCCDCM" : "iivixxlxccdcm");
		int num2 = s_romanDigitValue.Length;
		while (num2-- != 0)
		{
			while (num >= s_romanDigitValue[num2])
			{
				num -= s_romanDigitValue[num2];
				sb.Append(value, num2, 1 + (num2 & 1));
			}
		}
	}
}
