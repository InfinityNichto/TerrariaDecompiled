using System.Xml.XPath;

namespace System.Xml;

internal sealed class DocumentXPathNodeIterator_AllElemChildren_AndSelf : DocumentXPathNodeIterator_AllElemChildren
{
	internal DocumentXPathNodeIterator_AllElemChildren_AndSelf(DocumentXPathNavigator nav)
		: base(nav)
	{
	}

	internal DocumentXPathNodeIterator_AllElemChildren_AndSelf(DocumentXPathNodeIterator_AllElemChildren_AndSelf other)
		: base(other)
	{
	}

	public override XPathNodeIterator Clone()
	{
		return new DocumentXPathNodeIterator_AllElemChildren_AndSelf(this);
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
