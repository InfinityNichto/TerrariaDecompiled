using System.Xml.XPath;

namespace System.Xml;

internal sealed class DocumentXPathNodeIterator_ElemChildren_AndSelf_NoLocalName : DocumentXPathNodeIterator_ElemChildren_NoLocalName
{
	internal DocumentXPathNodeIterator_ElemChildren_AndSelf_NoLocalName(DocumentXPathNavigator nav, string nsAtom)
		: base(nav, nsAtom)
	{
	}

	internal DocumentXPathNodeIterator_ElemChildren_AndSelf_NoLocalName(DocumentXPathNodeIterator_ElemChildren_AndSelf_NoLocalName other)
		: base(other)
	{
	}

	public override XPathNodeIterator Clone()
	{
		return new DocumentXPathNodeIterator_ElemChildren_AndSelf_NoLocalName(this);
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
