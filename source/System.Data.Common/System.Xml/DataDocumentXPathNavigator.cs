using System.Diagnostics.CodeAnalysis;
using System.Xml.XPath;

namespace System.Xml;

internal sealed class DataDocumentXPathNavigator : XPathNavigator, IHasXmlNode
{
	private readonly XPathNodePointer _curNode;

	private XmlDataDocument _doc;

	private readonly XPathNodePointer _temp;

	internal XPathNodePointer CurNode => _curNode;

	internal XmlDataDocument Document => _doc;

	public override XPathNodeType NodeType => _curNode.NodeType;

	public override string LocalName => _curNode.LocalName;

	public override string NamespaceURI => _curNode.NamespaceURI;

	public override string Name => _curNode.Name;

	public override string Prefix => _curNode.Prefix;

	public override string Value
	{
		[UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:RequiresUnreferencedCode", Justification = "This whole class is unsafe. Constructors are marked as such.")]
		get
		{
			XPathNodeType nodeType = _curNode.NodeType;
			if (nodeType != XPathNodeType.Element && nodeType != 0)
			{
				return _curNode.Value;
			}
			return _curNode.InnerText;
		}
	}

	public override string BaseURI => _curNode.BaseURI;

	public override string XmlLang => _curNode.XmlLang;

	public override bool IsEmptyElement => _curNode.IsEmptyElement;

	public override XmlNameTable NameTable => _doc.NameTable;

	public override bool HasAttributes => _curNode.AttributeCount > 0;

	public override bool HasChildren => _curNode.HasChildren;

	[RequiresUnreferencedCode("Members from serialized types may be trimmed if not referenced directly.")]
	internal DataDocumentXPathNavigator(XmlDataDocument doc, XmlNode node)
	{
		_curNode = new XPathNodePointer(this, doc, node);
		_temp = new XPathNodePointer(this, doc, node);
		_doc = doc;
	}

	[RequiresUnreferencedCode("Members from serialized types may be trimmed if not referenced directly.")]
	private DataDocumentXPathNavigator(DataDocumentXPathNavigator other)
	{
		_curNode = other._curNode.Clone(this);
		_temp = other._temp.Clone(this);
		_doc = other._doc;
	}

	[UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:RequiresUnreferencedCode", Justification = "This whole class is unsafe. Constructors are marked as such.")]
	public override XPathNavigator Clone()
	{
		return new DataDocumentXPathNavigator(this);
	}

	[UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:RequiresUnreferencedCode", Justification = "This whole class is unsafe. Constructors are marked as such.")]
	public override string GetAttribute(string localName, string namespaceURI)
	{
		if (_curNode.NodeType != XPathNodeType.Element)
		{
			return string.Empty;
		}
		_temp.MoveTo(_curNode);
		if (!_temp.MoveToAttribute(localName, namespaceURI))
		{
			return string.Empty;
		}
		return _temp.Value;
	}

	[UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:RequiresUnreferencedCode", Justification = "This whole class is unsafe. Constructors are marked as such.")]
	public override string GetNamespace(string name)
	{
		return _curNode.GetNamespace(name);
	}

	public override bool MoveToNamespace(string name)
	{
		if (_curNode.NodeType == XPathNodeType.Element)
		{
			return _curNode.MoveToNamespace(name);
		}
		return false;
	}

	[UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:RequiresUnreferencedCode", Justification = "This whole class is unsafe. Constructors are marked as such.")]
	public override bool MoveToFirstNamespace(XPathNamespaceScope namespaceScope)
	{
		if (_curNode.NodeType == XPathNodeType.Element)
		{
			return _curNode.MoveToFirstNamespace(namespaceScope);
		}
		return false;
	}

	[UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:RequiresUnreferencedCode", Justification = "This whole class is unsafe. Constructors are marked as such.")]
	public override bool MoveToNextNamespace(XPathNamespaceScope namespaceScope)
	{
		if (_curNode.NodeType == XPathNodeType.Namespace)
		{
			return _curNode.MoveToNextNamespace(namespaceScope);
		}
		return false;
	}

	public override bool MoveToAttribute(string localName, string namespaceURI)
	{
		if (_curNode.NodeType == XPathNodeType.Element)
		{
			return _curNode.MoveToAttribute(localName, namespaceURI);
		}
		return false;
	}

	public override bool MoveToFirstAttribute()
	{
		if (_curNode.NodeType == XPathNodeType.Element)
		{
			return _curNode.MoveToNextAttribute(bFirst: true);
		}
		return false;
	}

	public override bool MoveToNextAttribute()
	{
		if (_curNode.NodeType == XPathNodeType.Attribute)
		{
			return _curNode.MoveToNextAttribute(bFirst: false);
		}
		return false;
	}

	public override bool MoveToNext()
	{
		if (_curNode.NodeType != XPathNodeType.Attribute)
		{
			return _curNode.MoveToNextSibling();
		}
		return false;
	}

	public override bool MoveToPrevious()
	{
		if (_curNode.NodeType != XPathNodeType.Attribute)
		{
			return _curNode.MoveToPreviousSibling();
		}
		return false;
	}

	public override bool MoveToFirst()
	{
		if (_curNode.NodeType != XPathNodeType.Attribute)
		{
			return _curNode.MoveToFirst();
		}
		return false;
	}

	public override bool MoveToFirstChild()
	{
		return _curNode.MoveToFirstChild();
	}

	public override bool MoveToParent()
	{
		return _curNode.MoveToParent();
	}

	public override void MoveToRoot()
	{
		_curNode.MoveToRoot();
	}

	public override bool MoveTo(XPathNavigator other)
	{
		if (other != null && other is DataDocumentXPathNavigator dataDocumentXPathNavigator && _curNode.MoveTo(dataDocumentXPathNavigator.CurNode))
		{
			_doc = _curNode.Document;
			return true;
		}
		return false;
	}

	public override bool MoveToId(string id)
	{
		return false;
	}

	public override bool IsSamePosition(XPathNavigator other)
	{
		if (other != null && other is DataDocumentXPathNavigator dataDocumentXPathNavigator && _doc == dataDocumentXPathNavigator.Document && _curNode.IsSamePosition(dataDocumentXPathNavigator.CurNode))
		{
			return true;
		}
		return false;
	}

	[UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:RequiresUnreferencedCode", Justification = "This whole class is unsafe. Constructors are marked as such.")]
	XmlNode IHasXmlNode.GetNode()
	{
		return _curNode.Node;
	}

	[UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:RequiresUnreferencedCode", Justification = "This whole class is unsafe. Constructors are marked as such.")]
	public override XmlNodeOrder ComparePosition(XPathNavigator other)
	{
		if (other == null)
		{
			return XmlNodeOrder.Unknown;
		}
		if (other is DataDocumentXPathNavigator dataDocumentXPathNavigator && dataDocumentXPathNavigator.Document == _doc)
		{
			return _curNode.ComparePosition(dataDocumentXPathNavigator.CurNode);
		}
		return XmlNodeOrder.Unknown;
	}
}
