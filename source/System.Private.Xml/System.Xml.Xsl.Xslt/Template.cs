using System.Globalization;
using System.Text;
using System.Xml.Xsl.Qil;

namespace System.Xml.Xsl.Xslt;

internal sealed class Template : ProtoTemplate
{
	public readonly string Match;

	public readonly QilName Mode;

	public readonly double Priority;

	public int ImportPrecedence;

	public int OrderNumber;

	public Template(QilName name, string match, QilName mode, double priority, XslVersion xslVer)
		: base(XslNodeType.Template, name, xslVer)
	{
		Match = match;
		Mode = mode;
		Priority = priority;
	}

	public override string GetDebugName()
	{
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.Append("<xsl:template");
		if (Match != null)
		{
			stringBuilder.Append(" match=\"");
			stringBuilder.Append(Match);
			stringBuilder.Append('"');
		}
		if (Name != null)
		{
			stringBuilder.Append(" name=\"");
			stringBuilder.Append(Name.QualifiedName);
			stringBuilder.Append('"');
		}
		if (!double.IsNaN(Priority))
		{
			stringBuilder.Append(" priority=\"");
			stringBuilder.Append(Priority.ToString(CultureInfo.InvariantCulture));
			stringBuilder.Append('"');
		}
		if (Mode.LocalName.Length != 0)
		{
			stringBuilder.Append(" mode=\"");
			stringBuilder.Append(Mode.QualifiedName);
			stringBuilder.Append('"');
		}
		stringBuilder.Append('>');
		return stringBuilder.ToString();
	}
}
