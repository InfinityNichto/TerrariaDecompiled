using System.ComponentModel;
using System.Xml.XPath;

namespace System.Xml.Xsl.Runtime;

[EditorBrowsable(EditorBrowsableState.Never)]
public struct PrecedingIterator
{
	private XmlNavigatorStack _stack;

	private XPathNavigator _navCurrent;

	public XPathNavigator Current => _navCurrent;

	public void Create(XPathNavigator context, XmlNavigatorFilter filter)
	{
		_navCurrent = XmlQueryRuntime.SyncToNavigator(_navCurrent, context);
		_navCurrent.MoveToRoot();
		_stack.Reset();
		if (!_navCurrent.IsSamePosition(context))
		{
			if (!filter.IsFiltered(_navCurrent))
			{
				_stack.Push(_navCurrent.Clone());
			}
			while (filter.MoveToFollowing(_navCurrent, context))
			{
				_stack.Push(_navCurrent.Clone());
			}
		}
	}

	public bool MoveNext()
	{
		if (_stack.IsEmpty)
		{
			return false;
		}
		_navCurrent = _stack.Pop();
		return true;
	}
}
