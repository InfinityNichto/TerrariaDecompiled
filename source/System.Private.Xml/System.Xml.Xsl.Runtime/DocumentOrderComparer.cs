using System.Collections.Generic;
using System.Xml.XPath;

namespace System.Xml.Xsl.Runtime;

internal sealed class DocumentOrderComparer : IComparer<XPathNavigator>
{
	private List<XPathNavigator> _roots;

	public int Compare(XPathNavigator navThis, XPathNavigator navThat)
	{
		switch (navThis.ComparePosition(navThat))
		{
		case XmlNodeOrder.Before:
			return -1;
		case XmlNodeOrder.Same:
			return 0;
		case XmlNodeOrder.After:
			return 1;
		default:
			if (_roots == null)
			{
				_roots = new List<XPathNavigator>();
			}
			if (GetDocumentIndex(navThis) >= GetDocumentIndex(navThat))
			{
				return 1;
			}
			return -1;
		}
	}

	public int GetDocumentIndex(XPathNavigator nav)
	{
		if (_roots == null)
		{
			_roots = new List<XPathNavigator>();
		}
		XPathNavigator xPathNavigator = nav.Clone();
		xPathNavigator.MoveToRoot();
		for (int i = 0; i < _roots.Count; i++)
		{
			if (xPathNavigator.IsSamePosition(_roots[i]))
			{
				return i;
			}
		}
		_roots.Add(xPathNavigator);
		return _roots.Count - 1;
	}
}
