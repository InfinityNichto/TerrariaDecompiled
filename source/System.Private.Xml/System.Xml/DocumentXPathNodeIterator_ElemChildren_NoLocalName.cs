using System.Xml.XPath;

namespace System.Xml;

internal class DocumentXPathNodeIterator_ElemChildren_NoLocalName : DocumentXPathNodeIterator_ElemDescendants
{
	private readonly string _nsAtom;

	internal DocumentXPathNodeIterator_ElemChildren_NoLocalName(DocumentXPathNavigator nav, string nsAtom)
		: base(nav)
	{
		_nsAtom = nsAtom;
	}

	internal DocumentXPathNodeIterator_ElemChildren_NoLocalName(DocumentXPathNodeIterator_ElemChildren_NoLocalName other)
		: base(other)
	{
		_nsAtom = other._nsAtom;
	}

	public override XPathNodeIterator Clone()
	{
		return new DocumentXPathNodeIterator_ElemChildren_NoLocalName(this);
	}

	protected override bool Match(XmlNode node)
	{
		return Ref.Equal(node.NamespaceURI, _nsAtom);
	}
}
