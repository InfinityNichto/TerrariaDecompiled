using System.Collections;
using System.Collections.Generic;

namespace System.Xml.Linq;

public sealed class XNodeEqualityComparer : IEqualityComparer, IEqualityComparer<XNode>
{
	public bool Equals(XNode? x, XNode? y)
	{
		return XNode.DeepEquals(x, y);
	}

	public int GetHashCode(XNode obj)
	{
		return obj?.GetDeepHashCode() ?? 0;
	}

	bool IEqualityComparer.Equals(object x, object y)
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
		return Equals(xNode, xNode2);
	}

	int IEqualityComparer.GetHashCode(object obj)
	{
		XNode xNode = obj as XNode;
		if (xNode == null && obj != null)
		{
			throw new ArgumentException(System.SR.Format(System.SR.Argument_MustBeDerivedFrom, typeof(XNode)), "obj");
		}
		return GetHashCode(xNode);
	}
}
