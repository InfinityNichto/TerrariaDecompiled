using System.ComponentModel;
using System.Xml.XPath;

namespace System.Xml.Xsl.Runtime;

[EditorBrowsable(EditorBrowsableState.Never)]
public abstract class XmlNavigatorFilter
{
	public abstract bool MoveToContent(XPathNavigator navigator);

	public abstract bool MoveToNextContent(XPathNavigator navigator);

	public abstract bool MoveToFollowingSibling(XPathNavigator navigator);

	public abstract bool MoveToPreviousSibling(XPathNavigator navigator);

	public abstract bool MoveToFollowing(XPathNavigator navigator, XPathNavigator navigatorEnd);

	public abstract bool IsFiltered(XPathNavigator navigator);
}
