using System.Globalization;

namespace System.Drawing;

internal static class ColorConverterCommon
{
	public static Color ConvertFromString(string strValue, CultureInfo culture)
	{
		string text = strValue.Trim();
		if (text.Length == 0)
		{
			return Color.Empty;
		}
		if (ColorTable.TryGetNamedColor(text, out var result))
		{
			return result;
		}
		char c = culture.TextInfo.ListSeparator[0];
		if (!text.Contains(c))
		{
			if (text.Length >= 2 && (text[0] == '\'' || text[0] == '"') && text[0] == text[text.Length - 1])
			{
				string name = text.Substring(1, text.Length - 2);
				return Color.FromName(name);
			}
			if ((text.Length == 7 && text[0] == '#') || (text.Length == 8 && (text.StartsWith("0x") || text.StartsWith("0X"))) || (text.Length == 8 && (text.StartsWith("&h") || text.StartsWith("&H"))))
			{
				return PossibleKnownColor(Color.FromArgb(-16777216 | IntFromString(text, culture)));
			}
		}
		string[] array = text.Split(c);
		int[] array2 = new int[array.Length];
		for (int i = 0; i < array2.Length; i++)
		{
			array2[i] = IntFromString(array[i], culture);
		}
		return array2.Length switch
		{
			1 => PossibleKnownColor(Color.FromArgb(array2[0])), 
			3 => PossibleKnownColor(Color.FromArgb(array2[0], array2[1], array2[2])), 
			4 => PossibleKnownColor(Color.FromArgb(array2[0], array2[1], array2[2], array2[3])), 
			_ => throw new ArgumentException(System.SR.Format(System.SR.InvalidColor, text)), 
		};
	}

	private static Color PossibleKnownColor(Color color)
	{
		int num = color.ToArgb();
		foreach (Color value in ColorTable.Colors.Values)
		{
			if (value.ToArgb() == num)
			{
				return value;
			}
		}
		return color;
	}

	private static int IntFromString(string text, CultureInfo culture)
	{
		text = text.Trim();
		try
		{
			if (text[0] == '#')
			{
				return IntFromString(text.Substring(1), 16);
			}
			if (text.StartsWith("0x", StringComparison.OrdinalIgnoreCase) || text.StartsWith("&h", StringComparison.OrdinalIgnoreCase))
			{
				return IntFromString(text.Substring(2), 16);
			}
			NumberFormatInfo formatInfo = (NumberFormatInfo)culture.GetFormat(typeof(NumberFormatInfo));
			return IntFromString(text, formatInfo);
		}
		catch (Exception innerException)
		{
			throw new ArgumentException(System.SR.Format(System.SR.ConvertInvalidPrimitive, text, "Int32"), innerException);
		}
	}

	private static int IntFromString(string value, int radix)
	{
		return Convert.ToInt32(value, radix);
	}

	private static int IntFromString(string value, NumberFormatInfo formatInfo)
	{
		return int.Parse(value, NumberStyles.Integer, formatInfo);
	}
}
