namespace System.Xml.Xsl.Xslt;

internal sealed class Number : XslNode
{
	public readonly NumberLevel Level;

	public readonly string Count;

	public readonly string From;

	public readonly string Value;

	public readonly string Format;

	public readonly string Lang;

	public readonly string LetterValue;

	public readonly string GroupingSeparator;

	public readonly string GroupingSize;

	public Number(NumberLevel level, string count, string from, string value, string format, string lang, string letterValue, string groupingSeparator, string groupingSize, XslVersion xslVer)
		: base(XslNodeType.Number, null, null, xslVer)
	{
		Level = level;
		Count = count;
		From = from;
		Value = value;
		Format = format;
		Lang = lang;
		LetterValue = letterValue;
		GroupingSeparator = groupingSeparator;
		GroupingSize = groupingSize;
	}
}
