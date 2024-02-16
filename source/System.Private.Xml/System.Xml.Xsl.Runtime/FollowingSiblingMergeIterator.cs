using System.ComponentModel;
using System.Xml.XPath;

namespace System.Xml.Xsl.Runtime;

[EditorBrowsable(EditorBrowsableState.Never)]
public struct FollowingSiblingMergeIterator
{
	private ContentMergeIterator _wrapped;

	public XPathNavigator Current => _wrapped.Current;

	public void Create(XmlNavigatorFilter filter)
	{
		_wrapped.Create(filter);
	}

	public IteratorResult MoveNext(XPathNavigator navigator)
	{
		return _wrapped.MoveNext(navigator, isContent: false);
	}
}
