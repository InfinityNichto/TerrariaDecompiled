using System.Xml.XPath;

namespace System.Xml.Xsl.Runtime;

internal sealed class XmlNavNeverFilter : XmlNavigatorFilter
{
	private static readonly XmlNavigatorFilter s_singleton = new XmlNavNeverFilter();

	public static XmlNavigatorFilter Create()
	{
		return s_singleton;
	}

	private XmlNavNeverFilter()
	{
	}

	public override bool MoveToContent(XPathNavigator navigator)
	{
		return MoveToFirstAttributeContent(navigator);
	}

	public override bool MoveToNextContent(XPathNavigator navigator)
	{
		return MoveToNextAttributeContent(navigator);
	}

	public override bool MoveToFollowingSibling(XPathNavigator navigator)
	{
		return navigator.MoveToNext();
	}

	public override bool MoveToPreviousSibling(XPathNavigator navigator)
	{
		return navigator.MoveToPrevious();
	}

	public override bool MoveToFollowing(XPathNavigator navigator, XPathNavigator navEnd)
	{
		return navigator.MoveToFollowing(XPathNodeType.All, navEnd);
	}

	public override bool IsFiltered(XPathNavigator navigator)
	{
		return false;
	}

	public static bool MoveToFirstAttributeContent(XPathNavigator navigator)
	{
		if (!navigator.MoveToFirstAttribute())
		{
			return navigator.MoveToFirstChild();
		}
		return true;
	}

	public static bool MoveToNextAttributeContent(XPathNavigator navigator)
	{
		if (navigator.NodeType == XPathNodeType.Attribute)
		{
			if (!navigator.MoveToNextAttribute())
			{
				navigator.MoveToParent();
				if (!navigator.MoveToFirstChild())
				{
					navigator.MoveToFirstAttribute();
					while (navigator.MoveToNextAttribute())
					{
					}
					return false;
				}
			}
			return true;
		}
		return navigator.MoveToNext();
	}
}
