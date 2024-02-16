using System.Xml.XPath;

namespace MS.Internal.Xml.XPath;

internal sealed class OperandQuery : ValueQuery
{
	internal object val;

	public override XPathResultType StaticType => GetXPathType(val);

	public OperandQuery(object val)
	{
		this.val = val;
	}

	public override object Evaluate(XPathNodeIterator nodeIterator)
	{
		return val;
	}

	public override XPathNodeIterator Clone()
	{
		return this;
	}
}
