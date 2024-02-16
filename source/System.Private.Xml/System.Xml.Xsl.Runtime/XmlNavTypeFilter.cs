using System.Xml.XPath;

namespace System.Xml.Xsl.Runtime;

internal sealed class XmlNavTypeFilter : XmlNavigatorFilter
{
	private static readonly XmlNavigatorFilter[] s_typeFilters = CreateTypeFilters();

	private readonly XPathNodeType _nodeType;

	private readonly int _mask;

	private static XmlNavigatorFilter[] CreateTypeFilters()
	{
		return new XmlNavigatorFilter[9]
		{
			null,
			new XmlNavTypeFilter(XPathNodeType.Element),
			null,
			null,
			new XmlNavTypeFilter(XPathNodeType.Text),
			null,
			null,
			new XmlNavTypeFilter(XPathNodeType.ProcessingInstruction),
			new XmlNavTypeFilter(XPathNodeType.Comment)
		};
	}

	public static XmlNavigatorFilter Create(XPathNodeType nodeType)
	{
		return s_typeFilters[(int)nodeType];
	}

	private XmlNavTypeFilter(XPathNodeType nodeType)
	{
		_nodeType = nodeType;
		_mask = XPathNavigator.GetContentKindMask(nodeType);
	}

	public override bool MoveToContent(XPathNavigator navigator)
	{
		return navigator.MoveToChild(_nodeType);
	}

	public override bool MoveToNextContent(XPathNavigator navigator)
	{
		return navigator.MoveToNext(_nodeType);
	}

	public override bool MoveToFollowingSibling(XPathNavigator navigator)
	{
		return navigator.MoveToNext(_nodeType);
	}

	public override bool MoveToPreviousSibling(XPathNavigator navigator)
	{
		return navigator.MoveToPrevious(_nodeType);
	}

	public override bool MoveToFollowing(XPathNavigator navigator, XPathNavigator navEnd)
	{
		return navigator.MoveToFollowing(_nodeType, navEnd);
	}

	public override bool IsFiltered(XPathNavigator navigator)
	{
		return ((1 << (int)navigator.NodeType) & _mask) == 0;
	}
}
