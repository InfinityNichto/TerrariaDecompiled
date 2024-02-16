using System.Collections.Generic;
using System.Text;
using System.Xml.Schema;

namespace System.Xml;

internal sealed class XmlNodeReaderNavigator
{
	internal struct VirtualAttribute
	{
		internal string name;

		internal string value;

		internal VirtualAttribute(string name, string value)
		{
			this.name = name;
			this.value = value;
		}
	}

	private XmlNode _curNode;

	private XmlNode _elemNode;

	private XmlNode _logNode;

	private int _attrIndex;

	private int _logAttrIndex;

	private readonly XmlNameTable _nameTable;

	private readonly XmlDocument _doc;

	private int _nAttrInd;

	private int _nDeclarationAttrCount;

	private int _nDocTypeAttrCount;

	private int _nLogLevel;

	private int _nLogAttrInd;

	private bool _bLogOnAttrVal;

	private readonly bool _bCreatedOnAttribute;

	internal VirtualAttribute[] decNodeAttributes = new VirtualAttribute[3]
	{
		new VirtualAttribute(null, null),
		new VirtualAttribute(null, null),
		new VirtualAttribute(null, null)
	};

	internal VirtualAttribute[] docTypeNodeAttributes = new VirtualAttribute[2]
	{
		new VirtualAttribute(null, null),
		new VirtualAttribute(null, null)
	};

	private bool _bOnAttrVal;

	public XmlNodeType NodeType
	{
		get
		{
			XmlNodeType nodeType = _curNode.NodeType;
			if (_nAttrInd != -1)
			{
				if (_bOnAttrVal)
				{
					return XmlNodeType.Text;
				}
				return XmlNodeType.Attribute;
			}
			return nodeType;
		}
	}

	public string NamespaceURI => _curNode.NamespaceURI;

	public string Name
	{
		get
		{
			if (_nAttrInd != -1)
			{
				if (_bOnAttrVal)
				{
					return string.Empty;
				}
				if (_curNode.NodeType == XmlNodeType.XmlDeclaration)
				{
					return decNodeAttributes[_nAttrInd].name;
				}
				return docTypeNodeAttributes[_nAttrInd].name;
			}
			if (IsLocalNameEmpty(_curNode.NodeType))
			{
				return string.Empty;
			}
			return _curNode.Name;
		}
	}

	public string LocalName
	{
		get
		{
			if (_nAttrInd != -1)
			{
				return Name;
			}
			if (IsLocalNameEmpty(_curNode.NodeType))
			{
				return string.Empty;
			}
			return _curNode.LocalName;
		}
	}

	internal bool CreatedOnAttribute => _bCreatedOnAttribute;

	public string Prefix => _curNode.Prefix;

	public bool HasValue
	{
		get
		{
			if (_nAttrInd != -1)
			{
				return true;
			}
			if (_curNode.Value != null || _curNode.NodeType == XmlNodeType.DocumentType)
			{
				return true;
			}
			return false;
		}
	}

	public string Value
	{
		get
		{
			string text = null;
			XmlNodeType nodeType = _curNode.NodeType;
			if (_nAttrInd != -1)
			{
				if (_curNode.NodeType == XmlNodeType.XmlDeclaration)
				{
					return decNodeAttributes[_nAttrInd].value;
				}
				return docTypeNodeAttributes[_nAttrInd].value;
			}
			switch (nodeType)
			{
			case XmlNodeType.DocumentType:
				text = ((XmlDocumentType)_curNode).InternalSubset;
				break;
			case XmlNodeType.XmlDeclaration:
			{
				StringBuilder stringBuilder = new StringBuilder(string.Empty);
				if (_nDeclarationAttrCount == -1)
				{
					InitDecAttr();
				}
				for (int i = 0; i < _nDeclarationAttrCount; i++)
				{
					stringBuilder.Append(decNodeAttributes[i].name + "=\"" + decNodeAttributes[i].value + "\"");
					if (i != _nDeclarationAttrCount - 1)
					{
						stringBuilder.Append(' ');
					}
				}
				text = stringBuilder.ToString();
				break;
			}
			default:
				text = _curNode.Value;
				break;
			}
			if (text != null)
			{
				return text;
			}
			return string.Empty;
		}
	}

	public string BaseURI => _curNode.BaseURI;

	public XmlSpace XmlSpace => _curNode.XmlSpace;

	public string XmlLang => _curNode.XmlLang;

	public bool IsEmptyElement
	{
		get
		{
			if (_curNode.NodeType == XmlNodeType.Element)
			{
				return ((XmlElement)_curNode).IsEmpty;
			}
			return false;
		}
	}

	public bool IsDefault
	{
		get
		{
			if (_curNode.NodeType == XmlNodeType.Attribute)
			{
				return !((XmlAttribute)_curNode).Specified;
			}
			return false;
		}
	}

	public IXmlSchemaInfo SchemaInfo => _curNode.SchemaInfo;

	public XmlNameTable NameTable => _nameTable;

	public int AttributeCount
	{
		get
		{
			if (_bCreatedOnAttribute)
			{
				return 0;
			}
			XmlNodeType nodeType = _curNode.NodeType;
			switch (nodeType)
			{
			case XmlNodeType.Element:
				return ((XmlElement)_curNode).Attributes.Count;
			default:
				if (!_bOnAttrVal || nodeType == XmlNodeType.XmlDeclaration || nodeType == XmlNodeType.DocumentType)
				{
					break;
				}
				goto case XmlNodeType.Attribute;
			case XmlNodeType.Attribute:
				return _elemNode.Attributes.Count;
			}
			switch (nodeType)
			{
			case XmlNodeType.XmlDeclaration:
				if (_nDeclarationAttrCount != -1)
				{
					return _nDeclarationAttrCount;
				}
				InitDecAttr();
				return _nDeclarationAttrCount;
			case XmlNodeType.DocumentType:
				if (_nDocTypeAttrCount != -1)
				{
					return _nDocTypeAttrCount;
				}
				InitDocTypeAttr();
				return _nDocTypeAttrCount;
			default:
				return 0;
			}
		}
	}

	private bool IsOnDeclOrDocType
	{
		get
		{
			XmlNodeType nodeType = _curNode.NodeType;
			if (nodeType != XmlNodeType.XmlDeclaration)
			{
				return nodeType == XmlNodeType.DocumentType;
			}
			return true;
		}
	}

	public XmlDocument Document => _doc;

	public XmlNodeReaderNavigator(XmlNode node)
	{
		_curNode = node;
		_logNode = node;
		XmlNodeType nodeType = _curNode.NodeType;
		if (nodeType == XmlNodeType.Attribute)
		{
			_elemNode = null;
			_attrIndex = -1;
			_bCreatedOnAttribute = true;
		}
		else
		{
			_elemNode = node;
			_attrIndex = -1;
			_bCreatedOnAttribute = false;
		}
		if (nodeType == XmlNodeType.Document)
		{
			_doc = (XmlDocument)_curNode;
		}
		else
		{
			_doc = node.OwnerDocument;
		}
		_nameTable = _doc.NameTable;
		_nAttrInd = -1;
		_nDeclarationAttrCount = -1;
		_nDocTypeAttrCount = -1;
		_bOnAttrVal = false;
		_bLogOnAttrVal = false;
	}

	private bool IsLocalNameEmpty(XmlNodeType nt)
	{
		switch (nt)
		{
		case XmlNodeType.None:
		case XmlNodeType.Text:
		case XmlNodeType.CDATA:
		case XmlNodeType.Comment:
		case XmlNodeType.Document:
		case XmlNodeType.DocumentFragment:
		case XmlNodeType.Whitespace:
		case XmlNodeType.SignificantWhitespace:
		case XmlNodeType.EndElement:
		case XmlNodeType.EndEntity:
			return true;
		case XmlNodeType.Element:
		case XmlNodeType.Attribute:
		case XmlNodeType.EntityReference:
		case XmlNodeType.Entity:
		case XmlNodeType.ProcessingInstruction:
		case XmlNodeType.DocumentType:
		case XmlNodeType.Notation:
		case XmlNodeType.XmlDeclaration:
			return false;
		default:
			return true;
		}
	}

	private void CheckIndexCondition(int attributeIndex)
	{
		if (attributeIndex < 0 || attributeIndex >= AttributeCount)
		{
			throw new ArgumentOutOfRangeException("attributeIndex");
		}
	}

	private void InitDecAttr()
	{
		int num = 0;
		string version = _doc.Version;
		if (version != null && version.Length != 0)
		{
			decNodeAttributes[num].name = "version";
			decNodeAttributes[num].value = version;
			num++;
		}
		version = _doc.Encoding;
		if (version != null && version.Length != 0)
		{
			decNodeAttributes[num].name = "encoding";
			decNodeAttributes[num].value = version;
			num++;
		}
		version = _doc.Standalone;
		if (version != null && version.Length != 0)
		{
			decNodeAttributes[num].name = "standalone";
			decNodeAttributes[num].value = version;
			num++;
		}
		_nDeclarationAttrCount = num;
	}

	public string GetDeclarationAttr(XmlDeclaration decl, string name)
	{
		return name switch
		{
			"version" => decl.Version, 
			"encoding" => decl.Encoding, 
			"standalone" => decl.Standalone, 
			_ => null, 
		};
	}

	public string GetDeclarationAttr(int i)
	{
		if (_nDeclarationAttrCount == -1)
		{
			InitDecAttr();
		}
		return decNodeAttributes[i].value;
	}

	public int GetDecAttrInd(string name)
	{
		if (_nDeclarationAttrCount == -1)
		{
			InitDecAttr();
		}
		for (int i = 0; i < _nDeclarationAttrCount; i++)
		{
			if (decNodeAttributes[i].name == name)
			{
				return i;
			}
		}
		return -1;
	}

	private void InitDocTypeAttr()
	{
		int num = 0;
		XmlDocumentType documentType = _doc.DocumentType;
		if (documentType == null)
		{
			_nDocTypeAttrCount = 0;
			return;
		}
		string publicId = documentType.PublicId;
		if (publicId != null)
		{
			docTypeNodeAttributes[num].name = "PUBLIC";
			docTypeNodeAttributes[num].value = publicId;
			num++;
		}
		publicId = documentType.SystemId;
		if (publicId != null)
		{
			docTypeNodeAttributes[num].name = "SYSTEM";
			docTypeNodeAttributes[num].value = publicId;
			num++;
		}
		_nDocTypeAttrCount = num;
	}

	public string GetDocumentTypeAttr(XmlDocumentType docType, string name)
	{
		if (name == "PUBLIC")
		{
			return docType.PublicId;
		}
		if (name == "SYSTEM")
		{
			return docType.SystemId;
		}
		return null;
	}

	public string GetDocumentTypeAttr(int i)
	{
		if (_nDocTypeAttrCount == -1)
		{
			InitDocTypeAttr();
		}
		return docTypeNodeAttributes[i].value;
	}

	public int GetDocTypeAttrInd(string name)
	{
		if (_nDocTypeAttrCount == -1)
		{
			InitDocTypeAttr();
		}
		for (int i = 0; i < _nDocTypeAttrCount; i++)
		{
			if (docTypeNodeAttributes[i].name == name)
			{
				return i;
			}
		}
		return -1;
	}

	private string GetAttributeFromElement(XmlElement elem, string name)
	{
		return elem.GetAttributeNode(name)?.Value;
	}

	public string GetAttribute(string name)
	{
		if (_bCreatedOnAttribute)
		{
			return null;
		}
		return _curNode.NodeType switch
		{
			XmlNodeType.Element => GetAttributeFromElement((XmlElement)_curNode, name), 
			XmlNodeType.Attribute => GetAttributeFromElement((XmlElement)_elemNode, name), 
			XmlNodeType.XmlDeclaration => GetDeclarationAttr((XmlDeclaration)_curNode, name), 
			XmlNodeType.DocumentType => GetDocumentTypeAttr((XmlDocumentType)_curNode, name), 
			_ => null, 
		};
	}

	private string GetAttributeFromElement(XmlElement elem, string name, string ns)
	{
		return elem.GetAttributeNode(name, ns)?.Value;
	}

	public string GetAttribute(string name, string ns)
	{
		if (_bCreatedOnAttribute)
		{
			return null;
		}
		return _curNode.NodeType switch
		{
			XmlNodeType.Element => GetAttributeFromElement((XmlElement)_curNode, name, ns), 
			XmlNodeType.Attribute => GetAttributeFromElement((XmlElement)_elemNode, name, ns), 
			XmlNodeType.XmlDeclaration => (ns.Length == 0) ? GetDeclarationAttr((XmlDeclaration)_curNode, name) : null, 
			XmlNodeType.DocumentType => (ns.Length == 0) ? GetDocumentTypeAttr((XmlDocumentType)_curNode, name) : null, 
			_ => null, 
		};
	}

	public string GetAttribute(int attributeIndex)
	{
		if (_bCreatedOnAttribute)
		{
			return null;
		}
		switch (_curNode.NodeType)
		{
		case XmlNodeType.Element:
			CheckIndexCondition(attributeIndex);
			return ((XmlElement)_curNode).Attributes[attributeIndex].Value;
		case XmlNodeType.Attribute:
			CheckIndexCondition(attributeIndex);
			return ((XmlElement)_elemNode).Attributes[attributeIndex].Value;
		case XmlNodeType.XmlDeclaration:
			CheckIndexCondition(attributeIndex);
			return GetDeclarationAttr(attributeIndex);
		case XmlNodeType.DocumentType:
			CheckIndexCondition(attributeIndex);
			return GetDocumentTypeAttr(attributeIndex);
		default:
			throw new ArgumentOutOfRangeException("attributeIndex");
		}
	}

	public void LogMove(int level)
	{
		_logNode = _curNode;
		_nLogLevel = level;
		_nLogAttrInd = _nAttrInd;
		_logAttrIndex = _attrIndex;
		_bLogOnAttrVal = _bOnAttrVal;
	}

	public void RollBackMove(ref int level)
	{
		_curNode = _logNode;
		level = _nLogLevel;
		_nAttrInd = _nLogAttrInd;
		_attrIndex = _logAttrIndex;
		_bOnAttrVal = _bLogOnAttrVal;
	}

	public void ResetToAttribute(ref int level)
	{
		if (_bCreatedOnAttribute || !_bOnAttrVal)
		{
			return;
		}
		if (IsOnDeclOrDocType)
		{
			level -= 2;
		}
		else
		{
			while (_curNode.NodeType != XmlNodeType.Attribute && (_curNode = _curNode.ParentNode) != null)
			{
				level--;
			}
		}
		_bOnAttrVal = false;
	}

	public void ResetMove(ref int level, ref XmlNodeType nt)
	{
		LogMove(level);
		if (_bCreatedOnAttribute)
		{
			return;
		}
		if (_nAttrInd != -1)
		{
			if (_bOnAttrVal)
			{
				level--;
				_bOnAttrVal = false;
			}
			_nLogAttrInd = _nAttrInd;
			level--;
			_nAttrInd = -1;
			nt = _curNode.NodeType;
			return;
		}
		if (_bOnAttrVal && _curNode.NodeType != XmlNodeType.Attribute)
		{
			ResetToAttribute(ref level);
		}
		if (_curNode.NodeType == XmlNodeType.Attribute)
		{
			_curNode = ((XmlAttribute)_curNode).OwnerElement;
			_attrIndex = -1;
			level--;
			nt = XmlNodeType.Element;
		}
		if (_curNode.NodeType == XmlNodeType.Element)
		{
			_elemNode = _curNode;
		}
	}

	public bool MoveToAttribute(string name)
	{
		return MoveToAttribute(name, string.Empty);
	}

	private bool MoveToAttributeFromElement(XmlElement elem, string name, string ns)
	{
		XmlAttribute xmlAttribute = null;
		xmlAttribute = ((ns.Length != 0) ? elem.GetAttributeNode(name, ns) : elem.GetAttributeNode(name));
		if (xmlAttribute != null)
		{
			_bOnAttrVal = false;
			_elemNode = elem;
			_curNode = xmlAttribute;
			_attrIndex = elem.Attributes.FindNodeOffsetNS(xmlAttribute);
			if (_attrIndex != -1)
			{
				return true;
			}
		}
		return false;
	}

	public bool MoveToAttribute(string name, string namespaceURI)
	{
		if (_bCreatedOnAttribute)
		{
			return false;
		}
		XmlNodeType nodeType = _curNode.NodeType;
		if (nodeType == XmlNodeType.Element)
		{
			return MoveToAttributeFromElement((XmlElement)_curNode, name, namespaceURI);
		}
		if (nodeType == XmlNodeType.Attribute)
		{
			return MoveToAttributeFromElement((XmlElement)_elemNode, name, namespaceURI);
		}
		if (nodeType == XmlNodeType.XmlDeclaration && namespaceURI.Length == 0)
		{
			if ((_nAttrInd = GetDecAttrInd(name)) != -1)
			{
				_bOnAttrVal = false;
				return true;
			}
		}
		else if (nodeType == XmlNodeType.DocumentType && namespaceURI.Length == 0 && (_nAttrInd = GetDocTypeAttrInd(name)) != -1)
		{
			_bOnAttrVal = false;
			return true;
		}
		return false;
	}

	public void MoveToAttribute(int attributeIndex)
	{
		if (_bCreatedOnAttribute)
		{
			return;
		}
		XmlAttribute xmlAttribute = null;
		switch (_curNode.NodeType)
		{
		case XmlNodeType.Element:
			CheckIndexCondition(attributeIndex);
			xmlAttribute = ((XmlElement)_curNode).Attributes[attributeIndex];
			if (xmlAttribute != null)
			{
				_elemNode = _curNode;
				_curNode = xmlAttribute;
				_attrIndex = attributeIndex;
			}
			break;
		case XmlNodeType.Attribute:
			CheckIndexCondition(attributeIndex);
			xmlAttribute = ((XmlElement)_elemNode).Attributes[attributeIndex];
			if (xmlAttribute != null)
			{
				_curNode = xmlAttribute;
				_attrIndex = attributeIndex;
			}
			break;
		case XmlNodeType.DocumentType:
		case XmlNodeType.XmlDeclaration:
			CheckIndexCondition(attributeIndex);
			_nAttrInd = attributeIndex;
			break;
		}
	}

	public bool MoveToNextAttribute(ref int level)
	{
		if (_bCreatedOnAttribute)
		{
			return false;
		}
		switch (_curNode.NodeType)
		{
		case XmlNodeType.Attribute:
			if (_attrIndex >= _elemNode.Attributes.Count - 1)
			{
				return false;
			}
			_curNode = _elemNode.Attributes[++_attrIndex];
			return true;
		case XmlNodeType.Element:
			if (_curNode.Attributes.Count > 0)
			{
				level++;
				_elemNode = _curNode;
				_curNode = _curNode.Attributes[0];
				_attrIndex = 0;
				return true;
			}
			break;
		case XmlNodeType.XmlDeclaration:
			if (_nDeclarationAttrCount == -1)
			{
				InitDecAttr();
			}
			_nAttrInd++;
			if (_nAttrInd < _nDeclarationAttrCount)
			{
				if (_nAttrInd == 0)
				{
					level++;
				}
				_bOnAttrVal = false;
				return true;
			}
			_nAttrInd--;
			break;
		case XmlNodeType.DocumentType:
			if (_nDocTypeAttrCount == -1)
			{
				InitDocTypeAttr();
			}
			_nAttrInd++;
			if (_nAttrInd < _nDocTypeAttrCount)
			{
				if (_nAttrInd == 0)
				{
					level++;
				}
				_bOnAttrVal = false;
				return true;
			}
			_nAttrInd--;
			break;
		}
		return false;
	}

	public bool MoveToParent()
	{
		XmlNode parentNode = _curNode.ParentNode;
		if (parentNode != null)
		{
			_curNode = parentNode;
			if (!_bOnAttrVal)
			{
				_attrIndex = 0;
			}
			return true;
		}
		return false;
	}

	public bool MoveToFirstChild()
	{
		XmlNode firstChild = _curNode.FirstChild;
		if (firstChild != null)
		{
			_curNode = firstChild;
			if (!_bOnAttrVal)
			{
				_attrIndex = -1;
			}
			return true;
		}
		return false;
	}

	private bool MoveToNextSibling(XmlNode node)
	{
		XmlNode nextSibling = node.NextSibling;
		if (nextSibling != null)
		{
			_curNode = nextSibling;
			if (!_bOnAttrVal)
			{
				_attrIndex = -1;
			}
			return true;
		}
		return false;
	}

	public bool MoveToNext()
	{
		if (_curNode.NodeType != XmlNodeType.Attribute)
		{
			return MoveToNextSibling(_curNode);
		}
		return MoveToNextSibling(_elemNode);
	}

	public bool MoveToElement()
	{
		if (_bCreatedOnAttribute)
		{
			return false;
		}
		switch (_curNode.NodeType)
		{
		case XmlNodeType.Attribute:
			if (_elemNode != null)
			{
				_curNode = _elemNode;
				_attrIndex = -1;
				return true;
			}
			break;
		case XmlNodeType.DocumentType:
		case XmlNodeType.XmlDeclaration:
			if (_nAttrInd != -1)
			{
				_nAttrInd = -1;
				return true;
			}
			break;
		}
		return false;
	}

	public string LookupNamespace(string prefix)
	{
		if (_bCreatedOnAttribute)
		{
			return null;
		}
		if (prefix == "xmlns")
		{
			return _nameTable.Add("http://www.w3.org/2000/xmlns/");
		}
		if (prefix == "xml")
		{
			return _nameTable.Add("http://www.w3.org/XML/1998/namespace");
		}
		if (prefix == null)
		{
			prefix = string.Empty;
		}
		string name = ((prefix.Length != 0) ? ("xmlns:" + prefix) : "xmlns");
		XmlNode xmlNode = _curNode;
		while (xmlNode != null)
		{
			if (xmlNode.NodeType == XmlNodeType.Element)
			{
				XmlElement xmlElement = (XmlElement)xmlNode;
				if (xmlElement.HasAttributes)
				{
					XmlAttribute attributeNode = xmlElement.GetAttributeNode(name);
					if (attributeNode != null)
					{
						return attributeNode.Value;
					}
				}
			}
			else if (xmlNode.NodeType == XmlNodeType.Attribute)
			{
				xmlNode = ((XmlAttribute)xmlNode).OwnerElement;
				continue;
			}
			xmlNode = xmlNode.ParentNode;
		}
		if (prefix.Length == 0)
		{
			return string.Empty;
		}
		return null;
	}

	internal string DefaultLookupNamespace(string prefix)
	{
		if (!_bCreatedOnAttribute)
		{
			if (prefix == "xmlns")
			{
				return _nameTable.Add("http://www.w3.org/2000/xmlns/");
			}
			if (prefix == "xml")
			{
				return _nameTable.Add("http://www.w3.org/XML/1998/namespace");
			}
			if (prefix == string.Empty)
			{
				return _nameTable.Add(string.Empty);
			}
		}
		return null;
	}

	internal string LookupPrefix(string namespaceName)
	{
		if (_bCreatedOnAttribute || namespaceName == null)
		{
			return null;
		}
		if (namespaceName == "http://www.w3.org/2000/xmlns/")
		{
			return _nameTable.Add("xmlns");
		}
		if (namespaceName == "http://www.w3.org/XML/1998/namespace")
		{
			return _nameTable.Add("xml");
		}
		if (namespaceName.Length == 0)
		{
			return string.Empty;
		}
		XmlNode xmlNode = _curNode;
		while (xmlNode != null)
		{
			if (xmlNode.NodeType == XmlNodeType.Element)
			{
				XmlElement xmlElement = (XmlElement)xmlNode;
				if (xmlElement.HasAttributes)
				{
					XmlAttributeCollection attributes = xmlElement.Attributes;
					for (int i = 0; i < attributes.Count; i++)
					{
						XmlAttribute xmlAttribute = attributes[i];
						if (!(xmlAttribute.Value == namespaceName))
						{
							continue;
						}
						if (xmlAttribute.Prefix.Length == 0 && xmlAttribute.LocalName == "xmlns")
						{
							if (LookupNamespace(string.Empty) == namespaceName)
							{
								return string.Empty;
							}
						}
						else if (xmlAttribute.Prefix == "xmlns")
						{
							string localName = xmlAttribute.LocalName;
							if (LookupNamespace(localName) == namespaceName)
							{
								return _nameTable.Add(localName);
							}
						}
					}
				}
			}
			else if (xmlNode.NodeType == XmlNodeType.Attribute)
			{
				xmlNode = ((XmlAttribute)xmlNode).OwnerElement;
				continue;
			}
			xmlNode = xmlNode.ParentNode;
		}
		return null;
	}

	internal IDictionary<string, string> GetNamespacesInScope(XmlNamespaceScope scope)
	{
		Dictionary<string, string> dictionary = new Dictionary<string, string>();
		if (_bCreatedOnAttribute)
		{
			return dictionary;
		}
		XmlNode xmlNode = _curNode;
		while (xmlNode != null)
		{
			if (xmlNode.NodeType == XmlNodeType.Element)
			{
				XmlElement xmlElement = (XmlElement)xmlNode;
				if (xmlElement.HasAttributes)
				{
					XmlAttributeCollection attributes = xmlElement.Attributes;
					for (int i = 0; i < attributes.Count; i++)
					{
						XmlAttribute xmlAttribute = attributes[i];
						if (xmlAttribute.LocalName == "xmlns" && xmlAttribute.Prefix.Length == 0)
						{
							if (!dictionary.ContainsKey(string.Empty))
							{
								dictionary.Add(_nameTable.Add(string.Empty), _nameTable.Add(xmlAttribute.Value));
							}
						}
						else if (xmlAttribute.Prefix == "xmlns")
						{
							string localName = xmlAttribute.LocalName;
							if (!dictionary.ContainsKey(localName))
							{
								dictionary.Add(_nameTable.Add(localName), _nameTable.Add(xmlAttribute.Value));
							}
						}
					}
				}
				if (scope == XmlNamespaceScope.Local)
				{
					break;
				}
			}
			else if (xmlNode.NodeType == XmlNodeType.Attribute)
			{
				xmlNode = ((XmlAttribute)xmlNode).OwnerElement;
				continue;
			}
			xmlNode = xmlNode.ParentNode;
		}
		if (scope != XmlNamespaceScope.Local)
		{
			if (dictionary.ContainsKey(string.Empty) && dictionary[string.Empty] == string.Empty)
			{
				dictionary.Remove(string.Empty);
			}
			if (scope == XmlNamespaceScope.All)
			{
				dictionary.Add(_nameTable.Add("xml"), _nameTable.Add("http://www.w3.org/XML/1998/namespace"));
			}
		}
		return dictionary;
	}

	public bool ReadAttributeValue(ref int level, ref bool bResolveEntity, ref XmlNodeType nt)
	{
		if (_nAttrInd != -1)
		{
			if (!_bOnAttrVal)
			{
				_bOnAttrVal = true;
				level++;
				nt = XmlNodeType.Text;
				return true;
			}
			return false;
		}
		if (_curNode.NodeType == XmlNodeType.Attribute)
		{
			XmlNode firstChild = _curNode.FirstChild;
			if (firstChild != null)
			{
				_curNode = firstChild;
				nt = _curNode.NodeType;
				level++;
				_bOnAttrVal = true;
				return true;
			}
		}
		else if (_bOnAttrVal)
		{
			XmlNode xmlNode = null;
			if ((_curNode.NodeType == XmlNodeType.EntityReference) & bResolveEntity)
			{
				_curNode = _curNode.FirstChild;
				nt = _curNode.NodeType;
				level++;
				bResolveEntity = false;
				return true;
			}
			xmlNode = _curNode.NextSibling;
			if (xmlNode == null)
			{
				XmlNode parentNode = _curNode.ParentNode;
				if (parentNode != null && parentNode.NodeType == XmlNodeType.EntityReference)
				{
					_curNode = parentNode;
					nt = XmlNodeType.EndEntity;
					level--;
					return true;
				}
			}
			if (xmlNode != null)
			{
				_curNode = xmlNode;
				nt = _curNode.NodeType;
				return true;
			}
			return false;
		}
		return false;
	}
}
