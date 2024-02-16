using System.Text;
using System.Xml.Xsl.Qil;

namespace System.Xml.Xsl.Xslt;

internal sealed class Key : XslNode
{
	public readonly string Match;

	public readonly string Use;

	public QilFunction Function;

	public Key(QilName name, string match, string use, XslVersion xslVer)
		: base(XslNodeType.Key, name, null, xslVer)
	{
		Match = match;
		Use = use;
	}

	public string GetDebugName()
	{
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.Append("<xsl:key name=\"");
		stringBuilder.Append(Name.QualifiedName);
		stringBuilder.Append('"');
		if (Match != null)
		{
			stringBuilder.Append(" match=\"");
			stringBuilder.Append(Match);
			stringBuilder.Append('"');
		}
		if (Use != null)
		{
			stringBuilder.Append(" use=\"");
			stringBuilder.Append(Use);
			stringBuilder.Append('"');
		}
		stringBuilder.Append('>');
		return stringBuilder.ToString();
	}
}
