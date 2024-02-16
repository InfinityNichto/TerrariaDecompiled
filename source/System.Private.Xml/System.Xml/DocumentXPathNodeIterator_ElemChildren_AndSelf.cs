using System.Xml.XPath;

namespace System.Xml;

internal sealed class DocumentXPathNodeIterator_ElemChildren_AndSelf : DocumentXPathNodeIterator_ElemChildren
{
	internal DocumentXPathNodeIterator_ElemChildren_AndSelf(DocumentXPathNavigator nav, string localNameAtom, string nsAtom)
		: base(nav, localNameAtom, nsAtom)
	{
	}

	internal DocumentXPathNodeIterator_ElemChildren_AndSelf(DocumentXPathNodeIterator_ElemChildren_AndSelf other)
		: base(other)
	{
	}

	public override XPathNodeIterator Clone()
	{
		return new DocumentXPathNodeIterator_ElemChildren_AndSelf(this);
	}

	public override bool MoveNext()
	{
		if (CurrentPosition == 0)
		{
			DocumentXPathNavigator documentXPathNavigator = (DocumentXPathNavigator)Current;
			XmlNode xmlNode = (XmlNode)documentXPathNavigator.UnderlyingObject;
			if (xmlNode.NodeType == XmlNodeType.Element && Match(xmlNode))
			{
				SetPosition(1);
				return true;
			}
		}
		return base.MoveNext();
	}
}
