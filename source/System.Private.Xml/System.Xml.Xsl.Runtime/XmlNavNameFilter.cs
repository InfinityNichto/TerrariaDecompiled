using System.Xml.XPath;

namespace System.Xml.Xsl.Runtime;

internal sealed class XmlNavNameFilter : XmlNavigatorFilter
{
	private readonly string _localName;

	private readonly string _namespaceUri;

	public static XmlNavigatorFilter Create(string localName, string namespaceUri)
	{
		return new XmlNavNameFilter(localName, namespaceUri);
	}

	private XmlNavNameFilter(string localName, string namespaceUri)
	{
		_localName = localName;
		_namespaceUri = namespaceUri;
	}

	public override bool MoveToContent(XPathNavigator navigator)
	{
		return navigator.MoveToChild(_localName, _namespaceUri);
	}

	public override bool MoveToNextContent(XPathNavigator navigator)
	{
		return navigator.MoveToNext(_localName, _namespaceUri);
	}

	public override bool MoveToFollowingSibling(XPathNavigator navigator)
	{
		return navigator.MoveToNext(_localName, _namespaceUri);
	}

	public override bool MoveToPreviousSibling(XPathNavigator navigator)
	{
		return navigator.MoveToPrevious(_localName, _namespaceUri);
	}

	public override bool MoveToFollowing(XPathNavigator navigator, XPathNavigator navEnd)
	{
		return navigator.MoveToFollowing(_localName, _namespaceUri, navEnd);
	}

	public override bool IsFiltered(XPathNavigator navigator)
	{
		if (!(navigator.LocalName != _localName))
		{
			return navigator.NamespaceURI != _namespaceUri;
		}
		return true;
	}
}
