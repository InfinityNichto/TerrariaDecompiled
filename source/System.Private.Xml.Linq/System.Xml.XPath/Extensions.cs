using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

namespace System.Xml.XPath;

public static class Extensions
{
	public static XPathNavigator CreateNavigator(this XNode node)
	{
		return node.CreateNavigator(null);
	}

	public static XPathNavigator CreateNavigator(this XNode node, XmlNameTable? nameTable)
	{
		if (node == null)
		{
			throw new ArgumentNullException("node");
		}
		if (node is XDocumentType)
		{
			throw new ArgumentException(System.SR.Format(System.SR.Argument_CreateNavigator, XmlNodeType.DocumentType));
		}
		if (node is XText xText)
		{
			if (xText.GetParent() is XDocument)
			{
				throw new ArgumentException(System.SR.Format(System.SR.Argument_CreateNavigator, XmlNodeType.Whitespace));
			}
			node = CalibrateText(xText);
		}
		return new XNodeNavigator(node, nameTable);
	}

	public static object XPathEvaluate(this XNode node, string expression)
	{
		return node.XPathEvaluate(expression, null);
	}

	public static object XPathEvaluate(this XNode node, string expression, IXmlNamespaceResolver? resolver)
	{
		if (node == null)
		{
			throw new ArgumentNullException("node");
		}
		return default(XPathEvaluator).Evaluate<object>(node, expression, resolver);
	}

	public static XElement? XPathSelectElement(this XNode node, string expression)
	{
		return node.XPathSelectElement(expression, null);
	}

	public static XElement? XPathSelectElement(this XNode node, string expression, IXmlNamespaceResolver? resolver)
	{
		return node.XPathSelectElements(expression, resolver).FirstOrDefault();
	}

	public static IEnumerable<XElement> XPathSelectElements(this XNode node, string expression)
	{
		return node.XPathSelectElements(expression, null);
	}

	public static IEnumerable<XElement> XPathSelectElements(this XNode node, string expression, IXmlNamespaceResolver? resolver)
	{
		if (node == null)
		{
			throw new ArgumentNullException("node");
		}
		return (IEnumerable<XElement>)default(XPathEvaluator).Evaluate<XElement>(node, expression, resolver);
	}

	private static XText CalibrateText(XText n)
	{
		XContainer parent = n.GetParent();
		if (parent == null)
		{
			return n;
		}
		foreach (XNode item in parent.Nodes())
		{
			if (item is XText result && item == n)
			{
				return result;
			}
		}
		return null;
	}
}
