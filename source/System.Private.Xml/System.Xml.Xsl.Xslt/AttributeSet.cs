using System.Text;
using System.Xml.Xsl.Qil;

namespace System.Xml.Xsl.Xslt;

internal sealed class AttributeSet : ProtoTemplate
{
	public CycleCheck CycleCheck;

	public AttributeSet(QilName name, XslVersion xslVer)
		: base(XslNodeType.AttributeSet, name, xslVer)
	{
	}

	public override string GetDebugName()
	{
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.Append("<xsl:attribute-set name=\"");
		stringBuilder.Append(Name.QualifiedName);
		stringBuilder.Append("\">");
		return stringBuilder.ToString();
	}

	public new void AddContent(XslNode node)
	{
		base.AddContent(node);
	}

	public void MergeContent(AttributeSet other)
	{
		InsertContent(other.Content);
	}
}
