namespace System.Xml.Linq;

internal sealed class XNodeReader : XmlReader, IXmlLineInfo
{
	private static readonly char[] s_WhitespaceChars = new char[4] { ' ', '\t', '\n', '\r' };

	private object _source;

	private object _parent;

	private ReadState _state;

	private XNode _root;

	private readonly XmlNameTable _nameTable;

	private readonly bool _omitDuplicateNamespaces;

	public override int AttributeCount
	{
		get
		{
			if (!IsInteractive)
			{
				return 0;
			}
			int num = 0;
			XElement elementInAttributeScope = GetElementInAttributeScope();
			if (elementInAttributeScope != null)
			{
				XAttribute xAttribute = elementInAttributeScope.lastAttr;
				if (xAttribute != null)
				{
					do
					{
						xAttribute = xAttribute.next;
						if (!_omitDuplicateNamespaces || !IsDuplicateNamespaceAttribute(xAttribute))
						{
							num++;
						}
					}
					while (xAttribute != elementInAttributeScope.lastAttr);
				}
			}
			return num;
		}
	}

	public override string BaseURI
	{
		get
		{
			if (_source is XObject xObject)
			{
				return xObject.BaseUri;
			}
			if (_parent is XObject xObject2)
			{
				return xObject2.BaseUri;
			}
			return string.Empty;
		}
	}

	public override int Depth
	{
		get
		{
			if (!IsInteractive)
			{
				return 0;
			}
			if (_source is XObject o)
			{
				return GetDepth(o);
			}
			if (_parent is XObject o2)
			{
				return GetDepth(o2) + 1;
			}
			return 0;
		}
	}

	public override bool EOF => _state == ReadState.EndOfFile;

	public override bool HasAttributes
	{
		get
		{
			if (!IsInteractive)
			{
				return false;
			}
			XElement elementInAttributeScope = GetElementInAttributeScope();
			if (elementInAttributeScope != null && elementInAttributeScope.lastAttr != null)
			{
				if (_omitDuplicateNamespaces)
				{
					return GetFirstNonDuplicateNamespaceAttribute(elementInAttributeScope.lastAttr.next) != null;
				}
				return true;
			}
			return false;
		}
	}

	public override bool HasValue
	{
		get
		{
			if (!IsInteractive)
			{
				return false;
			}
			if (_source is XObject xObject)
			{
				switch (xObject.NodeType)
				{
				case XmlNodeType.Attribute:
				case XmlNodeType.Text:
				case XmlNodeType.CDATA:
				case XmlNodeType.ProcessingInstruction:
				case XmlNodeType.Comment:
				case XmlNodeType.DocumentType:
					return true;
				default:
					return false;
				}
			}
			return true;
		}
	}

	public override bool IsEmptyElement
	{
		get
		{
			if (!IsInteractive)
			{
				return false;
			}
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

	public override XmlNodeType NodeType
	{
		get
		{
			if (!IsInteractive)
			{
				return XmlNodeType.None;
			}
			if (_source is XObject xObject)
			{
				if (IsEndElement)
				{
					return XmlNodeType.EndElement;
				}
				XmlNodeType nodeType = xObject.NodeType;
				if (nodeType != XmlNodeType.Text)
				{
					return nodeType;
				}
				if (xObject.parent != null && xObject.parent.parent == null && xObject.parent is XDocument)
				{
					return XmlNodeType.Whitespace;
				}
				return XmlNodeType.Text;
			}
			if (_parent is XDocument)
			{
				return XmlNodeType.Whitespace;
			}
			return XmlNodeType.Text;
		}
	}

	public override string Prefix => _nameTable.Add(GetPrefix());

	public override ReadState ReadState => _state;

	public override XmlReaderSettings Settings
	{
		get
		{
			XmlReaderSettings xmlReaderSettings = new XmlReaderSettings();
			xmlReaderSettings.CheckCharacters = false;
			return xmlReaderSettings;
		}
	}

	public override string Value
	{
		get
		{
			if (!IsInteractive)
			{
				return string.Empty;
			}
			if (_source is XObject xObject)
			{
				switch (xObject.NodeType)
				{
				case XmlNodeType.Attribute:
					return ((XAttribute)xObject).Value;
				case XmlNodeType.Text:
				case XmlNodeType.CDATA:
					return ((XText)xObject).Value;
				case XmlNodeType.Comment:
					return ((XComment)xObject).Value;
				case XmlNodeType.ProcessingInstruction:
					return ((XProcessingInstruction)xObject).Data;
				case XmlNodeType.DocumentType:
					return ((XDocumentType)xObject).InternalSubset ?? string.Empty;
				default:
					return string.Empty;
				}
			}
			return (string)_source;
		}
	}

	public override string XmlLang
	{
		get
		{
			if (!IsInteractive)
			{
				return string.Empty;
			}
			XElement xElement = GetElementInScope();
			if (xElement != null)
			{
				XName name = XNamespace.Xml.GetName("lang");
				do
				{
					XAttribute xAttribute = xElement.Attribute(name);
					if (xAttribute != null)
					{
						return xAttribute.Value;
					}
					xElement = xElement.parent as XElement;
				}
				while (xElement != null);
			}
			return string.Empty;
		}
	}

	public override XmlSpace XmlSpace
	{
		get
		{
			if (!IsInteractive)
			{
				return XmlSpace.None;
			}
			XElement xElement = GetElementInScope();
			if (xElement != null)
			{
				XName name = XNamespace.Xml.GetName("space");
				do
				{
					XAttribute xAttribute = xElement.Attribute(name);
					if (xAttribute != null)
					{
						string text = xAttribute.Value.Trim(s_WhitespaceChars);
						if (text == "preserve")
						{
							return XmlSpace.Preserve;
						}
						if (text == "default")
						{
							return XmlSpace.Default;
						}
					}
					xElement = xElement.parent as XElement;
				}
				while (xElement != null);
			}
			return XmlSpace.None;
		}
	}

	int IXmlLineInfo.LineNumber
	{
		get
		{
			if (IsEndElement)
			{
				if (_source is XElement xElement)
				{
					LineInfoEndElementAnnotation lineInfoEndElementAnnotation = xElement.Annotation<LineInfoEndElementAnnotation>();
					if (lineInfoEndElementAnnotation != null)
					{
						return lineInfoEndElementAnnotation.lineNumber;
					}
				}
			}
			else if (_source is IXmlLineInfo xmlLineInfo)
			{
				return xmlLineInfo.LineNumber;
			}
			return 0;
		}
	}

	int IXmlLineInfo.LinePosition
	{
		get
		{
			if (IsEndElement)
			{
				if (_source is XElement xElement)
				{
					LineInfoEndElementAnnotation lineInfoEndElementAnnotation = xElement.Annotation<LineInfoEndElementAnnotation>();
					if (lineInfoEndElementAnnotation != null)
					{
						return lineInfoEndElementAnnotation.linePosition;
					}
				}
			}
			else if (_source is IXmlLineInfo xmlLineInfo)
			{
				return xmlLineInfo.LinePosition;
			}
			return 0;
		}
	}

	private bool IsEndElement
	{
		get
		{
			return _parent == _source;
		}
		set
		{
			_parent = (value ? _source : null);
		}
	}

	private bool IsInteractive => _state == ReadState.Interactive;

	internal XNodeReader(XNode node, XmlNameTable nameTable, ReaderOptions options)
	{
		_source = node;
		_root = node;
		_nameTable = ((nameTable != null) ? nameTable : CreateNameTable());
		_omitDuplicateNamespaces = (((options & ReaderOptions.OmitDuplicateNamespaces) != 0) ? true : false);
	}

	internal XNodeReader(XNode node, XmlNameTable nameTable)
		: this(node, nameTable, ((node.GetSaveOptionsFromAnnotations() & SaveOptions.OmitDuplicateNamespaces) != 0) ? ReaderOptions.OmitDuplicateNamespaces : ReaderOptions.None)
	{
	}

	private static int GetDepth(XObject o)
	{
		int num = 0;
		while (o.parent != null)
		{
			num++;
			o = o.parent;
		}
		if (o is XDocument)
		{
			num--;
		}
		return num;
	}

	private string GetLocalName()
	{
		if (!IsInteractive)
		{
			return string.Empty;
		}
		if (_source is XElement xElement)
		{
			return xElement.Name.LocalName;
		}
		if (_source is XAttribute xAttribute)
		{
			return xAttribute.Name.LocalName;
		}
		if (_source is XProcessingInstruction xProcessingInstruction)
		{
			return xProcessingInstruction.Target;
		}
		if (_source is XDocumentType xDocumentType)
		{
			return xDocumentType.Name;
		}
		return string.Empty;
	}

	private string GetNamespaceURI()
	{
		if (!IsInteractive)
		{
			return string.Empty;
		}
		if (_source is XElement xElement)
		{
			return xElement.Name.NamespaceName;
		}
		if (_source is XAttribute xAttribute)
		{
			string namespaceName = xAttribute.Name.NamespaceName;
			if (namespaceName.Length == 0 && xAttribute.Name.LocalName == "xmlns")
			{
				return "http://www.w3.org/2000/xmlns/";
			}
			return namespaceName;
		}
		return string.Empty;
	}

	private string GetPrefix()
	{
		if (!IsInteractive)
		{
			return string.Empty;
		}
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
			string prefixOfNamespace2 = xAttribute.GetPrefixOfNamespace(xAttribute.Name.Namespace);
			if (prefixOfNamespace2 != null)
			{
				return prefixOfNamespace2;
			}
		}
		return string.Empty;
	}

	protected override void Dispose(bool disposing)
	{
		if (disposing && ReadState != ReadState.Closed)
		{
			Close();
		}
	}

	public override void Close()
	{
		_source = null;
		_parent = null;
		_root = null;
		_state = ReadState.Closed;
	}

	public override string GetAttribute(string name)
	{
		if (!IsInteractive)
		{
			return null;
		}
		XElement elementInAttributeScope = GetElementInAttributeScope();
		if (elementInAttributeScope != null)
		{
			GetNameInAttributeScope(name, elementInAttributeScope, out var localName, out var namespaceName);
			XAttribute xAttribute = elementInAttributeScope.lastAttr;
			if (xAttribute != null)
			{
				do
				{
					xAttribute = xAttribute.next;
					if (xAttribute.Name.LocalName == localName && xAttribute.Name.NamespaceName == namespaceName)
					{
						if (_omitDuplicateNamespaces && IsDuplicateNamespaceAttribute(xAttribute))
						{
							return null;
						}
						return xAttribute.Value;
					}
				}
				while (xAttribute != elementInAttributeScope.lastAttr);
			}
			return null;
		}
		if (_source is XDocumentType xDocumentType)
		{
			if (name == "PUBLIC")
			{
				return xDocumentType.PublicId;
			}
			if (name == "SYSTEM")
			{
				return xDocumentType.SystemId;
			}
		}
		return null;
	}

	public override string GetAttribute(string localName, string namespaceName)
	{
		if (!IsInteractive)
		{
			return null;
		}
		XElement elementInAttributeScope = GetElementInAttributeScope();
		if (elementInAttributeScope != null)
		{
			if (localName == "xmlns")
			{
				if (namespaceName != null && namespaceName.Length == 0)
				{
					return null;
				}
				if (namespaceName == "http://www.w3.org/2000/xmlns/")
				{
					namespaceName = string.Empty;
				}
			}
			XAttribute xAttribute = elementInAttributeScope.lastAttr;
			if (xAttribute != null)
			{
				do
				{
					xAttribute = xAttribute.next;
					if (xAttribute.Name.LocalName == localName && xAttribute.Name.NamespaceName == namespaceName)
					{
						if (_omitDuplicateNamespaces && IsDuplicateNamespaceAttribute(xAttribute))
						{
							return null;
						}
						return xAttribute.Value;
					}
				}
				while (xAttribute != elementInAttributeScope.lastAttr);
			}
		}
		return null;
	}

	public override string GetAttribute(int index)
	{
		if (!IsInteractive)
		{
			throw new InvalidOperationException(System.SR.InvalidOperation_ExpectedInteractive);
		}
		if (index < 0)
		{
			throw new ArgumentOutOfRangeException("index");
		}
		XElement elementInAttributeScope = GetElementInAttributeScope();
		if (elementInAttributeScope != null)
		{
			XAttribute xAttribute = elementInAttributeScope.lastAttr;
			if (xAttribute != null)
			{
				do
				{
					xAttribute = xAttribute.next;
					if ((!_omitDuplicateNamespaces || !IsDuplicateNamespaceAttribute(xAttribute)) && index-- == 0)
					{
						return xAttribute.Value;
					}
				}
				while (xAttribute != elementInAttributeScope.lastAttr);
			}
		}
		throw new ArgumentOutOfRangeException("index");
	}

	public override string LookupNamespace(string prefix)
	{
		if (!IsInteractive)
		{
			return null;
		}
		if (prefix == null)
		{
			return null;
		}
		XElement elementInScope = GetElementInScope();
		if (elementInScope != null)
		{
			XNamespace xNamespace = ((prefix.Length == 0) ? elementInScope.GetDefaultNamespace() : elementInScope.GetNamespaceOfPrefix(prefix));
			if (xNamespace != null)
			{
				return _nameTable.Add(xNamespace.NamespaceName);
			}
		}
		return null;
	}

	public override bool MoveToAttribute(string name)
	{
		if (!IsInteractive)
		{
			return false;
		}
		XElement elementInAttributeScope = GetElementInAttributeScope();
		if (elementInAttributeScope != null)
		{
			GetNameInAttributeScope(name, elementInAttributeScope, out var localName, out var namespaceName);
			XAttribute xAttribute = elementInAttributeScope.lastAttr;
			if (xAttribute != null)
			{
				do
				{
					xAttribute = xAttribute.next;
					if (xAttribute.Name.LocalName == localName && xAttribute.Name.NamespaceName == namespaceName)
					{
						if (_omitDuplicateNamespaces && IsDuplicateNamespaceAttribute(xAttribute))
						{
							return false;
						}
						_source = xAttribute;
						_parent = null;
						return true;
					}
				}
				while (xAttribute != elementInAttributeScope.lastAttr);
			}
		}
		return false;
	}

	public override bool MoveToAttribute(string localName, string namespaceName)
	{
		if (!IsInteractive)
		{
			return false;
		}
		XElement elementInAttributeScope = GetElementInAttributeScope();
		if (elementInAttributeScope != null)
		{
			if (localName == "xmlns")
			{
				if (namespaceName != null && namespaceName.Length == 0)
				{
					return false;
				}
				if (namespaceName == "http://www.w3.org/2000/xmlns/")
				{
					namespaceName = string.Empty;
				}
			}
			XAttribute xAttribute = elementInAttributeScope.lastAttr;
			if (xAttribute != null)
			{
				do
				{
					xAttribute = xAttribute.next;
					if (xAttribute.Name.LocalName == localName && xAttribute.Name.NamespaceName == namespaceName)
					{
						if (_omitDuplicateNamespaces && IsDuplicateNamespaceAttribute(xAttribute))
						{
							return false;
						}
						_source = xAttribute;
						_parent = null;
						return true;
					}
				}
				while (xAttribute != elementInAttributeScope.lastAttr);
			}
		}
		return false;
	}

	public override void MoveToAttribute(int index)
	{
		if (!IsInteractive)
		{
			return;
		}
		if (index < 0)
		{
			throw new ArgumentOutOfRangeException("index");
		}
		XElement elementInAttributeScope = GetElementInAttributeScope();
		if (elementInAttributeScope != null)
		{
			XAttribute xAttribute = elementInAttributeScope.lastAttr;
			if (xAttribute != null)
			{
				do
				{
					xAttribute = xAttribute.next;
					if ((!_omitDuplicateNamespaces || !IsDuplicateNamespaceAttribute(xAttribute)) && index-- == 0)
					{
						_source = xAttribute;
						_parent = null;
						return;
					}
				}
				while (xAttribute != elementInAttributeScope.lastAttr);
			}
		}
		throw new ArgumentOutOfRangeException("index");
	}

	public override bool MoveToElement()
	{
		if (!IsInteractive)
		{
			return false;
		}
		XAttribute xAttribute = _source as XAttribute;
		if (xAttribute == null)
		{
			xAttribute = _parent as XAttribute;
		}
		if (xAttribute != null && xAttribute.parent != null)
		{
			_source = xAttribute.parent;
			_parent = null;
			return true;
		}
		return false;
	}

	public override bool MoveToFirstAttribute()
	{
		if (!IsInteractive)
		{
			return false;
		}
		XElement elementInAttributeScope = GetElementInAttributeScope();
		if (elementInAttributeScope != null && elementInAttributeScope.lastAttr != null)
		{
			if (_omitDuplicateNamespaces)
			{
				object firstNonDuplicateNamespaceAttribute = GetFirstNonDuplicateNamespaceAttribute(elementInAttributeScope.lastAttr.next);
				if (firstNonDuplicateNamespaceAttribute == null)
				{
					return false;
				}
				_source = firstNonDuplicateNamespaceAttribute;
			}
			else
			{
				_source = elementInAttributeScope.lastAttr.next;
			}
			return true;
		}
		return false;
	}

	public override bool MoveToNextAttribute()
	{
		if (!IsInteractive)
		{
			return false;
		}
		if (_source is XElement xElement)
		{
			if (IsEndElement)
			{
				return false;
			}
			if (xElement.lastAttr != null)
			{
				if (_omitDuplicateNamespaces)
				{
					object firstNonDuplicateNamespaceAttribute = GetFirstNonDuplicateNamespaceAttribute(xElement.lastAttr.next);
					if (firstNonDuplicateNamespaceAttribute == null)
					{
						return false;
					}
					_source = firstNonDuplicateNamespaceAttribute;
				}
				else
				{
					_source = xElement.lastAttr.next;
				}
				return true;
			}
			return false;
		}
		XAttribute xAttribute = _source as XAttribute;
		if (xAttribute == null)
		{
			xAttribute = _parent as XAttribute;
		}
		if (xAttribute != null && xAttribute.parent != null && ((XElement)xAttribute.parent).lastAttr != xAttribute)
		{
			if (_omitDuplicateNamespaces)
			{
				object firstNonDuplicateNamespaceAttribute2 = GetFirstNonDuplicateNamespaceAttribute(xAttribute.next);
				if (firstNonDuplicateNamespaceAttribute2 == null)
				{
					return false;
				}
				_source = firstNonDuplicateNamespaceAttribute2;
			}
			else
			{
				_source = xAttribute.next;
			}
			_parent = null;
			return true;
		}
		return false;
	}

	public override bool Read()
	{
		switch (_state)
		{
		case ReadState.Initial:
			_state = ReadState.Interactive;
			if (_source is XDocument d)
			{
				return ReadIntoDocument(d);
			}
			return true;
		case ReadState.Interactive:
			return Read(skipContent: false);
		default:
			return false;
		}
	}

	public override bool ReadAttributeValue()
	{
		if (!IsInteractive)
		{
			return false;
		}
		if (_source is XAttribute a)
		{
			return ReadIntoAttribute(a);
		}
		return false;
	}

	public override bool ReadToDescendant(string localName, string namespaceName)
	{
		if (!IsInteractive)
		{
			return false;
		}
		MoveToElement();
		if (_source is XElement { IsEmpty: false } xElement)
		{
			if (IsEndElement)
			{
				return false;
			}
			foreach (XElement item in xElement.Descendants())
			{
				if (item.Name.LocalName == localName && item.Name.NamespaceName == namespaceName)
				{
					_source = item;
					return true;
				}
			}
			IsEndElement = true;
		}
		return false;
	}

	public override bool ReadToFollowing(string localName, string namespaceName)
	{
		while (Read())
		{
			if (_source is XElement xElement && !IsEndElement && xElement.Name.LocalName == localName && xElement.Name.NamespaceName == namespaceName)
			{
				return true;
			}
		}
		return false;
	}

	public override bool ReadToNextSibling(string localName, string namespaceName)
	{
		if (!IsInteractive)
		{
			return false;
		}
		MoveToElement();
		if (_source != _root)
		{
			if (_source is XNode xNode)
			{
				foreach (XElement item in xNode.ElementsAfterSelf())
				{
					if (item.Name.LocalName == localName && item.Name.NamespaceName == namespaceName)
					{
						_source = item;
						IsEndElement = false;
						return true;
					}
				}
				if (xNode.parent is XElement)
				{
					_source = xNode.parent;
					IsEndElement = true;
					return false;
				}
			}
			else if (_parent is XElement)
			{
				_source = _parent;
				_parent = null;
				IsEndElement = true;
				return false;
			}
		}
		return ReadToEnd();
	}

	public override void ResolveEntity()
	{
	}

	public override void Skip()
	{
		if (IsInteractive)
		{
			Read(skipContent: true);
		}
	}

	bool IXmlLineInfo.HasLineInfo()
	{
		if (IsEndElement)
		{
			if (_source is XElement xElement)
			{
				return xElement.Annotation<LineInfoEndElementAnnotation>() != null;
			}
		}
		else if (_source is IXmlLineInfo xmlLineInfo)
		{
			return xmlLineInfo.HasLineInfo();
		}
		return false;
	}

	private static XmlNameTable CreateNameTable()
	{
		XmlNameTable xmlNameTable = new NameTable();
		xmlNameTable.Add(string.Empty);
		xmlNameTable.Add("http://www.w3.org/2000/xmlns/");
		xmlNameTable.Add("http://www.w3.org/XML/1998/namespace");
		return xmlNameTable;
	}

	private XElement GetElementInAttributeScope()
	{
		if (_source is XElement result)
		{
			if (IsEndElement)
			{
				return null;
			}
			return result;
		}
		if (_source is XAttribute xAttribute)
		{
			return (XElement)xAttribute.parent;
		}
		if (_parent is XAttribute xAttribute2)
		{
			return (XElement)xAttribute2.parent;
		}
		return null;
	}

	private XElement GetElementInScope()
	{
		if (_source is XElement result)
		{
			return result;
		}
		if (_source is XNode xNode)
		{
			return xNode.parent as XElement;
		}
		if (_source is XAttribute xAttribute)
		{
			return (XElement)xAttribute.parent;
		}
		if (_parent is XElement result2)
		{
			return result2;
		}
		if (_parent is XAttribute xAttribute2)
		{
			return (XElement)xAttribute2.parent;
		}
		return null;
	}

	private static void GetNameInAttributeScope(string qualifiedName, XElement e, out string localName, out string namespaceName)
	{
		if (!string.IsNullOrEmpty(qualifiedName))
		{
			int num = qualifiedName.IndexOf(':');
			if (num != 0 && num != qualifiedName.Length - 1)
			{
				if (num == -1)
				{
					localName = qualifiedName;
					namespaceName = string.Empty;
					return;
				}
				XNamespace namespaceOfPrefix = e.GetNamespaceOfPrefix(qualifiedName.Substring(0, num));
				if (namespaceOfPrefix != null)
				{
					localName = qualifiedName.Substring(num + 1, qualifiedName.Length - num - 1);
					namespaceName = namespaceOfPrefix.NamespaceName;
					return;
				}
			}
		}
		localName = null;
		namespaceName = null;
	}

	private bool Read(bool skipContent)
	{
		if (_source is XElement xElement)
		{
			if (xElement.IsEmpty || IsEndElement || skipContent)
			{
				return ReadOverNode(xElement);
			}
			return ReadIntoElement(xElement);
		}
		if (_source is XNode n)
		{
			return ReadOverNode(n);
		}
		if (_source is XAttribute a)
		{
			return ReadOverAttribute(a, skipContent);
		}
		return ReadOverText(skipContent);
	}

	private bool ReadIntoDocument(XDocument d)
	{
		if (d.content is XNode xNode)
		{
			_source = xNode.next;
			return true;
		}
		if (d.content is string { Length: >0 } text)
		{
			_source = text;
			_parent = d;
			return true;
		}
		return ReadToEnd();
	}

	private bool ReadIntoElement(XElement e)
	{
		if (e.content is XNode xNode)
		{
			_source = xNode.next;
			return true;
		}
		if (e.content is string text)
		{
			if (text.Length > 0)
			{
				_source = text;
				_parent = e;
			}
			else
			{
				_source = e;
				IsEndElement = true;
			}
			return true;
		}
		return ReadToEnd();
	}

	private bool ReadIntoAttribute(XAttribute a)
	{
		_source = a.value;
		_parent = a;
		return true;
	}

	private bool ReadOverAttribute(XAttribute a, bool skipContent)
	{
		XElement xElement = (XElement)a.parent;
		if (xElement != null)
		{
			if (xElement.IsEmpty || skipContent)
			{
				return ReadOverNode(xElement);
			}
			return ReadIntoElement(xElement);
		}
		return ReadToEnd();
	}

	private bool ReadOverNode(XNode n)
	{
		if (n == _root)
		{
			return ReadToEnd();
		}
		XNode next = n.next;
		if (next == null || next == n || n == n.parent.content)
		{
			if (n.parent == null || (n.parent.parent == null && n.parent is XDocument))
			{
				return ReadToEnd();
			}
			_source = n.parent;
			IsEndElement = true;
		}
		else
		{
			_source = next;
			IsEndElement = false;
		}
		return true;
	}

	private bool ReadOverText(bool skipContent)
	{
		if (_parent is XElement)
		{
			_source = _parent;
			_parent = null;
			IsEndElement = true;
			return true;
		}
		if (_parent is XAttribute a)
		{
			_parent = null;
			return ReadOverAttribute(a, skipContent);
		}
		return ReadToEnd();
	}

	private bool ReadToEnd()
	{
		_state = ReadState.EndOfFile;
		return false;
	}

	private bool IsDuplicateNamespaceAttribute(XAttribute candidateAttribute)
	{
		if (!candidateAttribute.IsNamespaceDeclaration)
		{
			return false;
		}
		return IsDuplicateNamespaceAttributeInner(candidateAttribute);
	}

	private bool IsDuplicateNamespaceAttributeInner(XAttribute candidateAttribute)
	{
		if (candidateAttribute.Name.LocalName == "xml")
		{
			return true;
		}
		XElement xElement = candidateAttribute.parent as XElement;
		if (xElement == _root || xElement == null)
		{
			return false;
		}
		for (xElement = xElement.parent as XElement; xElement != null; xElement = xElement.parent as XElement)
		{
			XAttribute xAttribute = xElement.lastAttr;
			if (xAttribute != null)
			{
				do
				{
					if (xAttribute.name == candidateAttribute.name)
					{
						if (xAttribute.Value == candidateAttribute.Value)
						{
							return true;
						}
						return false;
					}
					xAttribute = xAttribute.next;
				}
				while (xAttribute != xElement.lastAttr);
			}
			if (xElement == _root)
			{
				return false;
			}
		}
		return false;
	}

	private XAttribute GetFirstNonDuplicateNamespaceAttribute(XAttribute candidate)
	{
		if (!IsDuplicateNamespaceAttribute(candidate))
		{
			return candidate;
		}
		if (candidate.parent is XElement xElement && candidate != xElement.lastAttr)
		{
			do
			{
				candidate = candidate.next;
				if (!IsDuplicateNamespaceAttribute(candidate))
				{
					return candidate;
				}
			}
			while (candidate != xElement.lastAttr);
		}
		return null;
	}
}
