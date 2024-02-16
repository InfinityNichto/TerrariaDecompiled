using System.Xml.Linq;

namespace System.Xml.XPath;

public static class XDocumentExtensions
{
	private sealed class XDocumentNavigable : IXPathNavigable
	{
		private readonly XNode _node;

		public XDocumentNavigable(XNode n)
		{
			_node = n;
		}

		public XPathNavigator CreateNavigator()
		{
			return _node.CreateNavigator();
		}
	}

	public static IXPathNavigable ToXPathNavigable(this XNode node)
	{
		return new XDocumentNavigable(node);
	}
}
