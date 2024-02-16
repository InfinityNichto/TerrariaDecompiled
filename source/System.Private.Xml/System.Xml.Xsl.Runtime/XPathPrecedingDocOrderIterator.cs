using System.ComponentModel;
using System.Xml.XPath;

namespace System.Xml.Xsl.Runtime;

[EditorBrowsable(EditorBrowsableState.Never)]
public struct XPathPrecedingDocOrderIterator
{
	private XmlNavigatorFilter _filter;

	private XPathNavigator _navCurrent;

	private XmlNavigatorStack _navStack;

	public XPathNavigator Current => _navCurrent;

	public void Create(XPathNavigator input, XmlNavigatorFilter filter)
	{
		_navCurrent = XmlQueryRuntime.SyncToNavigator(_navCurrent, input);
		_filter = filter;
		PushAncestors();
	}

	public bool MoveNext()
	{
		if (!_navStack.IsEmpty)
		{
			do
			{
				if (_filter.MoveToFollowing(_navCurrent, _navStack.Peek()))
				{
					return true;
				}
				_navCurrent.MoveTo(_navStack.Pop());
			}
			while (!_navStack.IsEmpty);
		}
		return false;
	}

	private void PushAncestors()
	{
		_navStack.Reset();
		do
		{
			_navStack.Push(_navCurrent.Clone());
		}
		while (_navCurrent.MoveToParent());
		_navStack.Pop();
	}
}
