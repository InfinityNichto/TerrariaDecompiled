using System.Xml.Linq;

namespace System.Xml.XPath;

internal static class XObjectExtensions
{
	public static XContainer GetParent(this XObject obj)
	{
		XContainer xContainer = obj.Parent;
		if (xContainer == null)
		{
			xContainer = obj.Document;
		}
		if (xContainer == obj)
		{
			return null;
		}
		return xContainer;
	}
}
