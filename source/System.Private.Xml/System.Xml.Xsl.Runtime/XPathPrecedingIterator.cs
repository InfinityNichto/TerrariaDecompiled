using System.ComponentModel;
using System.Xml.XPath;

namespace System.Xml.Xsl.Runtime;

[EditorBrowsable(EditorBrowsableState.Never)]
public struct XPathPrecedingIterator
{
	private XmlNavigatorStack _stack;

	private XPathNavigator _navCurrent;

	public XPathNavigator Current => _navCurrent;

	public void Create(XPathNavigator context, XmlNavigatorFilter filter)
	{
		XPathPrecedingDocOrderIterator xPathPrecedingDocOrderIterator = default(XPathPrecedingDocOrderIterator);
		xPathPrecedingDocOrderIterator.Create(context, filter);
		_stack.Reset();
		while (xPathPrecedingDocOrderIterator.MoveNext())
		{
			_stack.Push(xPathPrecedingDocOrderIterator.Current.Clone());
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
