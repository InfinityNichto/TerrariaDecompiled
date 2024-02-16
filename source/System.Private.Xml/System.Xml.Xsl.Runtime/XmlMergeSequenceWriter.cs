using System.Xml.XPath;

namespace System.Xml.Xsl.Runtime;

internal sealed class XmlMergeSequenceWriter : XmlSequenceWriter
{
	private readonly XmlRawWriter _xwrt;

	private bool _lastItemWasAtomic;

	public XmlMergeSequenceWriter(XmlRawWriter xwrt)
	{
		_xwrt = xwrt;
		_lastItemWasAtomic = false;
	}

	public override XmlRawWriter StartTree(XPathNodeType rootType, IXmlNamespaceResolver nsResolver, XmlNameTable nameTable)
	{
		if (rootType == XPathNodeType.Attribute || rootType == XPathNodeType.Namespace)
		{
			throw new XslTransformException(System.SR.XmlIl_TopLevelAttrNmsp, string.Empty);
		}
		_xwrt.NamespaceResolver = nsResolver;
		return _xwrt;
	}

	public override void EndTree()
	{
		_lastItemWasAtomic = false;
	}

	public override void WriteItem(XPathItem item)
	{
		if (item.IsNode)
		{
			XPathNavigator xPathNavigator = item as XPathNavigator;
			if (xPathNavigator.NodeType == XPathNodeType.Attribute || xPathNavigator.NodeType == XPathNodeType.Namespace)
			{
				throw new XslTransformException(System.SR.XmlIl_TopLevelAttrNmsp, string.Empty);
			}
			CopyNode(xPathNavigator);
			_lastItemWasAtomic = false;
		}
		else
		{
			WriteString(item.Value);
		}
	}

	private void WriteString(string value)
	{
		if (_lastItemWasAtomic)
		{
			_xwrt.WriteWhitespace(" ");
		}
		else
		{
			_lastItemWasAtomic = true;
		}
		_xwrt.WriteString(value);
	}

	private void CopyNode(XPathNavigator nav)
	{
		int num = 0;
		while (true)
		{
			if (CopyShallowNode(nav))
			{
				XPathNodeType nodeType = nav.NodeType;
				if (nodeType == XPathNodeType.Element)
				{
					if (nav.MoveToFirstAttribute())
					{
						do
						{
							CopyShallowNode(nav);
						}
						while (nav.MoveToNextAttribute());
						nav.MoveToParent();
					}
					XPathNamespaceScope xPathNamespaceScope = ((num == 0) ? XPathNamespaceScope.ExcludeXml : XPathNamespaceScope.Local);
					if (nav.MoveToFirstNamespace(xPathNamespaceScope))
					{
						CopyNamespaces(nav, xPathNamespaceScope);
						nav.MoveToParent();
					}
					_xwrt.StartElementContent();
				}
				if (nav.MoveToFirstChild())
				{
					num++;
					continue;
				}
				if (nav.NodeType == XPathNodeType.Element)
				{
					_xwrt.WriteEndElement(nav.Prefix, nav.LocalName, nav.NamespaceURI);
				}
			}
			while (true)
			{
				if (num == 0)
				{
					return;
				}
				if (nav.MoveToNext())
				{
					break;
				}
				num--;
				nav.MoveToParent();
				if (nav.NodeType == XPathNodeType.Element)
				{
					_xwrt.WriteFullEndElement(nav.Prefix, nav.LocalName, nav.NamespaceURI);
				}
			}
		}
	}

	private bool CopyShallowNode(XPathNavigator nav)
	{
		bool result = false;
		switch (nav.NodeType)
		{
		case XPathNodeType.Element:
			_xwrt.WriteStartElement(nav.Prefix, nav.LocalName, nav.NamespaceURI);
			result = true;
			break;
		case XPathNodeType.Attribute:
			_xwrt.WriteStartAttribute(nav.Prefix, nav.LocalName, nav.NamespaceURI);
			_xwrt.WriteString(nav.Value);
			_xwrt.WriteEndAttribute();
			break;
		case XPathNodeType.Text:
			_xwrt.WriteString(nav.Value);
			break;
		case XPathNodeType.SignificantWhitespace:
		case XPathNodeType.Whitespace:
			_xwrt.WriteWhitespace(nav.Value);
			break;
		case XPathNodeType.Root:
			result = true;
			break;
		case XPathNodeType.Comment:
			_xwrt.WriteComment(nav.Value);
			break;
		case XPathNodeType.ProcessingInstruction:
			_xwrt.WriteProcessingInstruction(nav.LocalName, nav.Value);
			break;
		case XPathNodeType.Namespace:
			_xwrt.WriteNamespaceDeclaration(nav.LocalName, nav.Value);
			break;
		}
		return result;
	}

	private void CopyNamespaces(XPathNavigator nav, XPathNamespaceScope nsScope)
	{
		string localName = nav.LocalName;
		string value = nav.Value;
		if (nav.MoveToNextNamespace(nsScope))
		{
			CopyNamespaces(nav, nsScope);
		}
		_xwrt.WriteNamespaceDeclaration(localName, value);
	}
}
