using System.Threading;
using System.Xml.Linq;

namespace System.Xml.XPath;

internal sealed class XNodeNavigator : XPathNavigator, IXmlLineInfo
{
	internal static readonly string xmlPrefixNamespace = XNamespace.Xml.NamespaceName;

	internal static readonly string xmlnsPrefixNamespace = XNamespace.Xmlns.NamespaceName;

	private static readonly int[] s_ElementContentMasks = new int[10] { 0, 2, 0, 0, 24, 0, 0, 128, 256, 410 };

	private static XAttribute s_XmlNamespaceDeclaration;

	private XObject _source;

	private XElement _parent;

	private readonly XmlNameTable _nameTable;

	public override string BaseURI
	{
		get
		{
			if (_source != null)
			{
				return _source.BaseUri;
			}
			if (_parent != null)
			{
				return _parent.BaseUri;
			}
			return string.Empty;
		}
	}

	public override bool HasAttributes
	{
		get
		{
			if (_source is XElement xElement)
			{
				foreach (XAttribute item in xElement.Attributes())
				{
					if (!item.IsNamespaceDeclaration)
					{
						return true;
					}
				}
			}
			return false;
		}
	}

	public override bool HasChildren
	{
		get
		{
			if (_source is XContainer xContainer)
			{
				foreach (XNode item in xContainer.Nodes())
				{
					if (IsContent(xContainer, item))
					{
						return true;
					}
				}
			}
			return false;
		}
	}

	public override bool IsEmptyElement
	{
		get
		{
			if (_source is XElement xElement)
			{
				return xElement.IsEmpty;
			}
			return false;
		}
	}

	public override string LocalName => _nameTable.Add(GetLocalName());

	public override string Name
	{
		get
		{
			string prefix = GetPrefix();
			if (prefix.Length == 0)
			{
				return _nameTable.Add(GetLocalName());
			}
			return _nameTable.Add(prefix + ":" + GetLocalName());
		}
	}

	public override string NamespaceURI => _nameTable.Add(GetNamespaceURI());

	public override XmlNameTable NameTable => _nameTable;

	public override XPathNodeType NodeType
	{
		get
		{
			if (_source != null)
			{
				switch (_source.NodeType)
				{
				case XmlNodeType.Element:
					return XPathNodeType.Element;
				case XmlNodeType.Attribute:
				{
					XAttribute xAttribute = (XAttribute)_source;
					if (!xAttribute.IsNamespaceDeclaration)
					{
						return XPathNodeType.Attribute;
					}
					return XPathNodeType.Namespace;
				}
				case XmlNodeType.Document:
					return XPathNodeType.Root;
				case XmlNodeType.Comment:
					return XPathNodeType.Comment;
				case XmlNodeType.ProcessingInstruction:
					return XPathNodeType.ProcessingInstruction;
				default:
					return XPathNodeType.Text;
				}
			}
			return XPathNodeType.Text;
		}
	}

	public override string Prefix => _nameTable.Add(GetPrefix());

	public override object UnderlyingObject => _source;

	public override string Value
	{
		get
		{
			if (_source != null)
			{
				switch (_source.NodeType)
				{
				case XmlNodeType.Element:
					return ((XElement)_source).Value;
				case XmlNodeType.Attribute:
					return ((XAttribute)_source).Value;
				case XmlNodeType.Document:
				{
					XElement root = ((XDocument)_source).Root;
					if (root == null)
					{
						return string.Empty;
					}
					return root.Value;
				}
				case XmlNodeType.Text:
				case XmlNodeType.CDATA:
					return CollectText((XText)_source);
				case XmlNodeType.Comment:
					return ((XComment)_source).Value;
				case XmlNodeType.ProcessingInstruction:
					return ((XProcessingInstruction)_source).Data;
				default:
					return string.Empty;
				}
			}
			return string.Empty;
		}
	}

	int IXmlLineInfo.LineNumber => ((IXmlLineInfo)_source)?.LineNumber ?? 0;

	int IXmlLineInfo.LinePosition => ((IXmlLineInfo)_source)?.LinePosition ?? 0;

	public XNodeNavigator(XNode node, XmlNameTable nameTable)
	{
		_source = node;
		_nameTable = ((nameTable != null) ? nameTable : CreateNameTable());
	}

	public XNodeNavigator(XNodeNavigator other)
	{
		_source = other._source;
		_parent = other._parent;
		_nameTable = other._nameTable;
	}

	private string GetLocalName()
	{
		if (_source is XElement xElement)
		{
			return xElement.Name.LocalName;
		}
		if (_source is XAttribute xAttribute)
		{
			if (_parent != null && xAttribute.Name.NamespaceName.Length == 0)
			{
				return string.Empty;
			}
			return xAttribute.Name.LocalName;
		}
		if (_source is XProcessingInstruction xProcessingInstruction)
		{
			return xProcessingInstruction.Target;
		}
		return string.Empty;
	}

	private string GetNamespaceURI()
	{
		if (_source is XElement xElement)
		{
			return xElement.Name.NamespaceName;
		}
		if (_source is XAttribute xAttribute)
		{
			if (_parent != null)
			{
				return string.Empty;
			}
			return xAttribute.Name.NamespaceName;
		}
		return string.Empty;
	}

	private string GetPrefix()
	{
		if (_source is XElement xElement)
		{
			string prefixOfNamespace = xElement.GetPrefixOfNamespace(xElement.Name.Namespace);
			if (prefixOfNamespace != null)
			{
				return prefixOfNamespace;
			}
			return string.Empty;
		}
		if (_source is XAttribute xAttribute)
		{
			if (_parent != null)
			{
				return string.Empty;
			}
			string prefixOfNamespace2 = xAttribute.GetPrefixOfNamespace(xAttribute.Name.Namespace);
			if (prefixOfNamespace2 != null)
			{
				return prefixOfNamespace2;
			}
		}
		return string.Empty;
	}

	public override XPathNavigator Clone()
	{
		return new XNodeNavigator(this);
	}

	public override bool IsSamePosition(XPathNavigator navigator)
	{
		if (!(navigator is XNodeNavigator n))
		{
			return false;
		}
		return IsSamePosition(this, n);
	}

	public override bool MoveTo(XPathNavigator navigator)
	{
		if (navigator is XNodeNavigator xNodeNavigator)
		{
			_source = xNodeNavigator._source;
			_parent = xNodeNavigator._parent;
			return true;
		}
		return false;
	}

	public override bool MoveToAttribute(string localName, string namespaceName)
	{
		if (_source is XElement xElement)
		{
			foreach (XAttribute item in xElement.Attributes())
			{
				if (item.Name.LocalName == localName && item.Name.NamespaceName == namespaceName && !item.IsNamespaceDeclaration)
				{
					_source = item;
					return true;
				}
			}
		}
		return false;
	}

	public override bool MoveToChild(string localName, string namespaceName)
	{
		if (_source is XContainer xContainer)
		{
			foreach (XElement item in xContainer.Elements())
			{
				if (item.Name.LocalName == localName && item.Name.NamespaceName == namespaceName)
				{
					_source = item;
					return true;
				}
			}
		}
		return false;
	}

	public override bool MoveToChild(XPathNodeType type)
	{
		if (_source is XContainer xContainer)
		{
			int num = GetElementContentMask(type);
			if ((0x18u & (uint)num) != 0 && xContainer.GetParent() == null && xContainer is XDocument)
			{
				num &= -25;
			}
			foreach (XNode item in xContainer.Nodes())
			{
				if (((1 << (int)item.NodeType) & num) != 0)
				{
					_source = item;
					return true;
				}
			}
		}
		return false;
	}

	public override bool MoveToFirstAttribute()
	{
		if (_source is XElement xElement)
		{
			foreach (XAttribute item in xElement.Attributes())
			{
				if (!item.IsNamespaceDeclaration)
				{
					_source = item;
					return true;
				}
			}
		}
		return false;
	}

	public override bool MoveToFirstChild()
	{
		if (_source is XContainer xContainer)
		{
			foreach (XNode item in xContainer.Nodes())
			{
				if (IsContent(xContainer, item))
				{
					_source = item;
					return true;
				}
			}
		}
		return false;
	}

	public override bool MoveToFirstNamespace(XPathNamespaceScope scope)
	{
		if (_source is XElement xElement)
		{
			XAttribute xAttribute = null;
			switch (scope)
			{
			case XPathNamespaceScope.Local:
				xAttribute = GetFirstNamespaceDeclarationLocal(xElement);
				break;
			case XPathNamespaceScope.ExcludeXml:
				xAttribute = GetFirstNamespaceDeclarationGlobal(xElement);
				while (xAttribute != null && xAttribute.Name.LocalName == "xml")
				{
					xAttribute = GetNextNamespaceDeclarationGlobal(xAttribute);
				}
				break;
			case XPathNamespaceScope.All:
				xAttribute = GetFirstNamespaceDeclarationGlobal(xElement);
				if (xAttribute == null)
				{
					xAttribute = GetXmlNamespaceDeclaration();
				}
				break;
			}
			if (xAttribute != null)
			{
				_source = xAttribute;
				_parent = xElement;
				return true;
			}
		}
		return false;
	}

	public override bool MoveToId(string id)
	{
		throw new NotSupportedException(System.SR.NotSupported_MoveToId);
	}

	public override bool MoveToNamespace(string localName)
	{
		if (_source is XElement xElement)
		{
			if (localName == "xmlns")
			{
				return false;
			}
			if (localName != null && localName.Length == 0)
			{
				localName = "xmlns";
			}
			for (XAttribute xAttribute = GetFirstNamespaceDeclarationGlobal(xElement); xAttribute != null; xAttribute = GetNextNamespaceDeclarationGlobal(xAttribute))
			{
				if (xAttribute.Name.LocalName == localName)
				{
					_source = xAttribute;
					_parent = xElement;
					return true;
				}
			}
			if (localName == "xml")
			{
				_source = GetXmlNamespaceDeclaration();
				_parent = xElement;
				return true;
			}
		}
		return false;
	}

	public override bool MoveToNext()
	{
		if (_source is XNode xNode)
		{
			XContainer parent = xNode.GetParent();
			if (parent != null)
			{
				XNode xNode2 = null;
				for (XNode xNode3 = xNode; xNode3 != null; xNode3 = xNode2)
				{
					xNode2 = xNode3.NextNode;
					if (xNode2 == null)
					{
						break;
					}
					if (IsContent(parent, xNode2) && (!(xNode3 is XText) || !(xNode2 is XText)))
					{
						_source = xNode2;
						return true;
					}
				}
			}
		}
		return false;
	}

	public override bool MoveToNext(string localName, string namespaceName)
	{
		if (_source is XNode xNode)
		{
			foreach (XElement item in xNode.ElementsAfterSelf())
			{
				if (item.Name.LocalName == localName && item.Name.NamespaceName == namespaceName)
				{
					_source = item;
					return true;
				}
			}
		}
		return false;
	}

	public override bool MoveToNext(XPathNodeType type)
	{
		if (_source is XNode xNode)
		{
			XContainer parent = xNode.GetParent();
			if (parent != null)
			{
				int num = GetElementContentMask(type);
				if ((0x18u & (uint)num) != 0 && parent.GetParent() == null && parent is XDocument)
				{
					num &= -25;
				}
				XNode xNode2 = null;
				XNode xNode3 = xNode;
				while (true)
				{
					xNode2 = xNode3.NextNode;
					if (xNode2 == null)
					{
						break;
					}
					if (((1 << (int)xNode2.NodeType) & num) != 0 && (!(xNode3 is XText) || !(xNode2 is XText)))
					{
						_source = xNode2;
						return true;
					}
					xNode3 = xNode2;
				}
			}
		}
		return false;
	}

	public override bool MoveToNextAttribute()
	{
		if (_source is XAttribute xAttribute && _parent == null)
		{
			XElement xElement = (XElement)xAttribute.GetParent();
			if (xElement != null)
			{
				for (XAttribute nextAttribute = xAttribute.NextAttribute; nextAttribute != null; nextAttribute = nextAttribute.NextAttribute)
				{
					if (!nextAttribute.IsNamespaceDeclaration)
					{
						_source = nextAttribute;
						return true;
					}
				}
			}
		}
		return false;
	}

	public override bool MoveToNextNamespace(XPathNamespaceScope scope)
	{
		XAttribute xAttribute = _source as XAttribute;
		if (xAttribute != null && _parent != null && !IsXmlNamespaceDeclaration(xAttribute))
		{
			switch (scope)
			{
			case XPathNamespaceScope.Local:
				if (xAttribute.GetParent() != _parent)
				{
					return false;
				}
				xAttribute = GetNextNamespaceDeclarationLocal(xAttribute);
				break;
			case XPathNamespaceScope.ExcludeXml:
				do
				{
					xAttribute = GetNextNamespaceDeclarationGlobal(xAttribute);
				}
				while (xAttribute != null && (xAttribute.Name.LocalName == "xml" || HasNamespaceDeclarationInScope(xAttribute, _parent)));
				break;
			case XPathNamespaceScope.All:
				do
				{
					xAttribute = GetNextNamespaceDeclarationGlobal(xAttribute);
				}
				while (xAttribute != null && HasNamespaceDeclarationInScope(xAttribute, _parent));
				if (xAttribute == null && !HasNamespaceDeclarationInScope(GetXmlNamespaceDeclaration(), _parent))
				{
					xAttribute = GetXmlNamespaceDeclaration();
				}
				break;
			}
			if (xAttribute != null)
			{
				_source = xAttribute;
				return true;
			}
		}
		return false;
	}

	public override bool MoveToParent()
	{
		if (_parent != null)
		{
			_source = _parent;
			_parent = null;
			return true;
		}
		XNode parent = _source.GetParent();
		if (parent != null)
		{
			_source = parent;
			return true;
		}
		return false;
	}

	public override bool MoveToPrevious()
	{
		if (_source is XNode xNode)
		{
			XContainer parent = xNode.GetParent();
			if (parent != null)
			{
				XNode xNode2 = null;
				foreach (XNode item in parent.Nodes())
				{
					if (item == xNode)
					{
						if (xNode2 != null)
						{
							_source = xNode2;
							return true;
						}
						return false;
					}
					if (IsContent(parent, item))
					{
						xNode2 = item;
					}
				}
			}
		}
		return false;
	}

	public override XmlReader ReadSubtree()
	{
		if (!(_source is XContainer xContainer))
		{
			throw new InvalidOperationException(System.SR.Format(System.SR.InvalidOperation_BadNodeType, NodeType));
		}
		return xContainer.CreateReader();
	}

	bool IXmlLineInfo.HasLineInfo()
	{
		return ((IXmlLineInfo)_source)?.HasLineInfo() ?? false;
	}

	private static string CollectText(XText n)
	{
		string text = n.Value;
		if (n.GetParent() != null)
		{
			foreach (XNode item in n.NodesAfterSelf())
			{
				if (!(item is XText xText))
				{
					break;
				}
				text += xText.Value;
			}
		}
		return text;
	}

	private static XmlNameTable CreateNameTable()
	{
		XmlNameTable xmlNameTable = new NameTable();
		xmlNameTable.Add(string.Empty);
		xmlNameTable.Add(xmlnsPrefixNamespace);
		xmlNameTable.Add(xmlPrefixNamespace);
		return xmlNameTable;
	}

	private static bool IsContent(XContainer c, XNode n)
	{
		if (c.GetParent() != null || c is XElement)
		{
			return true;
		}
		return ((1 << (int)n.NodeType) & 0x182) != 0;
	}

	private static bool IsSamePosition(XNodeNavigator n1, XNodeNavigator n2)
	{
		if (n1._source == n2._source)
		{
			return n1._source.GetParent() == n2._source.GetParent();
		}
		return false;
	}

	private static bool IsXmlNamespaceDeclaration(XAttribute a)
	{
		return a == GetXmlNamespaceDeclaration();
	}

	private static int GetElementContentMask(XPathNodeType type)
	{
		return s_ElementContentMasks[(int)type];
	}

	private static XAttribute GetFirstNamespaceDeclarationGlobal(XElement e)
	{
		XElement xElement = e;
		do
		{
			XAttribute firstNamespaceDeclarationLocal = GetFirstNamespaceDeclarationLocal(xElement);
			if (firstNamespaceDeclarationLocal != null)
			{
				return firstNamespaceDeclarationLocal;
			}
			xElement = xElement.Parent;
		}
		while (xElement != null);
		return null;
	}

	private static XAttribute GetFirstNamespaceDeclarationLocal(XElement e)
	{
		foreach (XAttribute item in e.Attributes())
		{
			if (item.IsNamespaceDeclaration)
			{
				return item;
			}
		}
		return null;
	}

	private static XAttribute GetNextNamespaceDeclarationGlobal(XAttribute a)
	{
		XElement xElement = (XElement)a.GetParent();
		if (xElement == null)
		{
			return null;
		}
		XAttribute nextNamespaceDeclarationLocal = GetNextNamespaceDeclarationLocal(a);
		if (nextNamespaceDeclarationLocal != null)
		{
			return nextNamespaceDeclarationLocal;
		}
		xElement = xElement.Parent;
		if (xElement == null)
		{
			return null;
		}
		return GetFirstNamespaceDeclarationGlobal(xElement);
	}

	private static XAttribute GetNextNamespaceDeclarationLocal(XAttribute a)
	{
		XElement parent = a.Parent;
		if (parent == null)
		{
			return null;
		}
		XAttribute xAttribute = a;
		for (xAttribute = xAttribute.NextAttribute; xAttribute != null; xAttribute = xAttribute.NextAttribute)
		{
			if (xAttribute.IsNamespaceDeclaration)
			{
				return xAttribute;
			}
		}
		return null;
	}

	private static XAttribute GetXmlNamespaceDeclaration()
	{
		if (s_XmlNamespaceDeclaration == null)
		{
			Interlocked.CompareExchange(ref s_XmlNamespaceDeclaration, new XAttribute(XNamespace.Xmlns.GetName("xml"), xmlPrefixNamespace), null);
		}
		return s_XmlNamespaceDeclaration;
	}

	private static bool HasNamespaceDeclarationInScope(XAttribute a, XElement e)
	{
		XName name = a.Name;
		XElement xElement = e;
		while (xElement != null && xElement != a.GetParent())
		{
			if (xElement.Attribute(name) != null)
			{
				return true;
			}
			xElement = xElement.Parent;
		}
		return false;
	}
}
