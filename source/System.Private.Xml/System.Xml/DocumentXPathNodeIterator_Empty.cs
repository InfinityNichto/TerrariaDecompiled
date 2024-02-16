using System.Xml.XPath;

namespace System.Xml;

internal sealed class DocumentXPathNodeIterator_Empty : XPathNodeIterator
{
	private readonly XPathNavigator _nav;

	public override XPathNavigator Current => _nav;

	public override int CurrentPosition => 0;

	public override int Count => 0;

	internal DocumentXPathNodeIterator_Empty(DocumentXPathNavigator nav)
	{
		_nav = nav.Clone();
	}

	internal DocumentXPathNodeIterator_Empty(DocumentXPathNodeIterator_Empty other)
	{
		_nav = other._nav.Clone();
	}

	public override XPathNodeIterator Clone()
	{
		return new DocumentXPathNodeIterator_Empty(this);
	}

	public override bool MoveNext()
	{
		return false;
	}
}
