using System.Xml.XPath;

namespace System.Xml;

internal class DocumentXPathNodeIterator_ElemChildren : DocumentXPathNodeIterator_ElemDescendants
{
	protected string localNameAtom;

	protected string nsAtom;

	internal DocumentXPathNodeIterator_ElemChildren(DocumentXPathNavigator nav, string localNameAtom, string nsAtom)
		: base(nav)
	{
		this.localNameAtom = localNameAtom;
		this.nsAtom = nsAtom;
	}

	internal DocumentXPathNodeIterator_ElemChildren(DocumentXPathNodeIterator_ElemChildren other)
		: base(other)
	{
		localNameAtom = other.localNameAtom;
		nsAtom = other.nsAtom;
	}

	public override XPathNodeIterator Clone()
	{
		return new DocumentXPathNodeIterator_ElemChildren(this);
	}

	protected override bool Match(XmlNode node)
	{
		if (Ref.Equal(node.LocalName, localNameAtom))
		{
			return Ref.Equal(node.NamespaceURI, nsAtom);
		}
		return false;
	}
}
