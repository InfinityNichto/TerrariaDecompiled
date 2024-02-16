using System.Collections;
using System.Collections.Generic;

namespace System.Xml.Linq;

public sealed class XNodeDocumentOrderComparer : IComparer, IComparer<XNode?>
{
	public int Compare(XNode? x, XNode? y)
	{
		return XNode.CompareDocumentOrder(x, y);
	}

	int IComparer.Compare(object x, object y)
	{
		XNode xNode = x as XNode;
		if (xNode == null && x != null)
		{
			throw new ArgumentException(System.SR.Format(System.SR.Argument_MustBeDerivedFrom, typeof(XNode)), "x");
		}
		XNode xNode2 = y as XNode;
		if (xNode2 == null && y != null)
		{
			throw new ArgumentException(System.SR.Format(System.SR.Argument_MustBeDerivedFrom, typeof(XNode)), "y");
		}
		return Compare(xNode, xNode2);
	}
}
