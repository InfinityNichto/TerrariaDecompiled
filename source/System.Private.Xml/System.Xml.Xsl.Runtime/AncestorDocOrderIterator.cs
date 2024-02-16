using System.ComponentModel;
using System.Xml.XPath;

namespace System.Xml.Xsl.Runtime;

[EditorBrowsable(EditorBrowsableState.Never)]
public struct AncestorDocOrderIterator
{
	private XmlNavigatorStack _stack;

	private XPathNavigator _navCurrent;

	public XPathNavigator Current => _navCurrent;

	public void Create(XPathNavigator context, XmlNavigatorFilter filter, bool orSelf)
	{
		AncestorIterator ancestorIterator = default(AncestorIterator);
		ancestorIterator.Create(context, filter, orSelf);
		_stack.Reset();
		while (ancestorIterator.MoveNext())
		{
			_stack.Push(ancestorIterator.Current.Clone());
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
