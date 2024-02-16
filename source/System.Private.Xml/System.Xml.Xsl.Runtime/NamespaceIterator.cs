using System.ComponentModel;
using System.Xml.XPath;

namespace System.Xml.Xsl.Runtime;

[EditorBrowsable(EditorBrowsableState.Never)]
public struct NamespaceIterator
{
	private XPathNavigator _navCurrent;

	private XmlNavigatorStack _navStack;

	public XPathNavigator Current => _navCurrent;

	public void Create(XPathNavigator context)
	{
		_navStack.Reset();
		if (!context.MoveToFirstNamespace(XPathNamespaceScope.All))
		{
			return;
		}
		do
		{
			if (context.LocalName.Length != 0 || context.Value.Length != 0)
			{
				_navStack.Push(context.Clone());
			}
		}
		while (context.MoveToNextNamespace(XPathNamespaceScope.All));
		context.MoveToParent();
	}

	public bool MoveNext()
	{
		if (_navStack.IsEmpty)
		{
			return false;
		}
		_navCurrent = _navStack.Pop();
		return true;
	}
}
