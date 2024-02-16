using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Xml.Schema;
using System.Xml.XPath;

namespace System.Xml;

internal sealed class DocumentXPathNavigator : XPathNavigator, IHasXmlNode
{
	private readonly XmlDocument _document;

	private XmlNode _source;

	private int _attributeIndex;

	private XmlElement _namespaceParent;

	public override XmlNameTable NameTable => _document.NameTable;

	public override XPathNodeType NodeType
	{
		get
		{
			CalibrateText();
			return _source.XPNodeType;
		}
	}

	public override string LocalName => _source.XPLocalName;

	public override string NamespaceURI
	{
		get
		{
			if (_source is XmlAttribute { IsNamespace: not false })
			{
				return string.Empty;
			}
			return _source.NamespaceURI;
		}
	}

	public override string Name
	{
		get
		{
			switch (_source.NodeType)
			{
			case XmlNodeType.Element:
			case XmlNodeType.ProcessingInstruction:
				return _source.Name;
			case XmlNodeType.Attribute:
				if (((XmlAttribute)_source).IsNamespace)
				{
					string localName = _source.LocalName;
					if (Ref.Equal(localName, _document.strXmlns))
					{
						return string.Empty;
					}
					return localName;
				}
				return _source.Name;
			default:
				return string.Empty;
			}
		}
	}

	public override string Prefix
	{
		get
		{
			if (_source is XmlAttribute { IsNamespace: not false })
			{
				return string.Empty;
			}
			return _source.Prefix;
		}
	}

	public override string Value
	{
		get
		{
			switch (_source.NodeType)
			{
			case XmlNodeType.Element:
			case XmlNodeType.DocumentFragment:
				return _source.InnerText;
			case XmlNodeType.Document:
				return ValueDocument;
			case XmlNodeType.Text:
			case XmlNodeType.CDATA:
			case XmlNodeType.Whitespace:
			case XmlNodeType.SignificantWhitespace:
				return ValueText;
			default:
				return _source.Value;
			}
		}
	}

	private string ValueDocument
	{
		get
		{
			XmlElement documentElement = _document.DocumentElement;
			if (documentElement != null)
			{
				return documentElement.InnerText;
			}
			return string.Empty;
		}
	}

	private string ValueText
	{
		get
		{
			CalibrateText();
			string text = _source.Value;
			XmlNode xmlNode = NextSibling(_source);
			if (xmlNode != null && xmlNode.IsText)
			{
				StringBuilder stringBuilder = new StringBuilder(text);
				do
				{
					stringBuilder.Append(xmlNode.Value);
					xmlNode = NextSibling(xmlNode);
				}
				while (xmlNode != null && xmlNode.IsText);
				text = stringBuilder.ToString();
			}
			return text;
		}
	}

	public override string BaseURI => _source.BaseURI;

	public override bool IsEmptyElement
	{
		get
		{
			if (_source is XmlElement xmlElement)
			{
				return xmlElement.IsEmpty;
			}
			return false;
		}
	}

	public override string XmlLang => _source.XmlLang;

	public override object UnderlyingObject
	{
		get
		{
			CalibrateText();
			return _source;
		}
	}

	public override bool HasAttributes
	{
		get
		{
			if (_source is XmlElement { HasAttributes: not false } xmlElement)
			{
				XmlAttributeCollection attributes = xmlElement.Attributes;
				for (int i = 0; i < attributes.Count; i++)
				{
					XmlAttribute xmlAttribute = attributes[i];
					if (!xmlAttribute.IsNamespace)
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
			switch (_source.NodeType)
			{
			case XmlNodeType.Element:
			{
				XmlNode xmlNode = FirstChild(_source);
				if (xmlNode == null)
				{
					return false;
				}
				return true;
			}
			case XmlNodeType.Document:
			case XmlNodeType.DocumentFragment:
			{
				XmlNode xmlNode = FirstChild(_source);
				if (xmlNode == null)
				{
					return false;
				}
				while (!IsValidChild(_source, xmlNode))
				{
					xmlNode = NextSibling(xmlNode);
					if (xmlNode == null)
					{
						return false;
					}
				}
				return true;
			}
			default:
				return false;
			}
		}
	}

	public override IXmlSchemaInfo SchemaInfo => _source.SchemaInfo;

	public override bool CanEdit => true;

	public DocumentXPathNavigator(XmlDocument document, XmlNode node)
	{
		_document = document;
		ResetPosition(node);
	}

	public DocumentXPathNavigator(DocumentXPathNavigator other)
	{
		_document = other._document;
		_source = other._source;
		_attributeIndex = other._attributeIndex;
		_namespaceParent = other._namespaceParent;
	}

	public override XPathNavigator Clone()
	{
		return new DocumentXPathNavigator(this);
	}

	public override void SetValue(string value)
	{
		if (value == null)
		{
			throw new ArgumentNullException("value");
		}
		XmlNode source = _source;
		switch (source.NodeType)
		{
		case XmlNodeType.Attribute:
			if (!((XmlAttribute)source).IsNamespace)
			{
				source.InnerText = value;
				return;
			}
			break;
		case XmlNodeType.Text:
		case XmlNodeType.CDATA:
		case XmlNodeType.Whitespace:
		case XmlNodeType.SignificantWhitespace:
		{
			CalibrateText();
			source = _source;
			XmlNode xmlNode = TextEnd(source);
			if (source != xmlNode)
			{
				if (source.IsReadOnly)
				{
					throw new InvalidOperationException(System.SR.Xdom_Node_Modify_ReadOnly);
				}
				DeleteToFollowingSibling(source.NextSibling, xmlNode);
			}
			goto case XmlNodeType.Element;
		}
		case XmlNodeType.Element:
		case XmlNodeType.ProcessingInstruction:
		case XmlNodeType.Comment:
			source.InnerText = value;
			return;
		}
		throw new InvalidOperationException(System.SR.Xpn_BadPosition);
	}

	public override string GetAttribute(string localName, string namespaceURI)
	{
		return _source.GetXPAttribute(localName, namespaceURI);
	}

	public override bool MoveToAttribute(string localName, string namespaceURI)
	{
		if (_source is XmlElement { HasAttributes: not false } xmlElement)
		{
			XmlAttributeCollection attributes = xmlElement.Attributes;
			for (int i = 0; i < attributes.Count; i++)
			{
				XmlAttribute xmlAttribute = attributes[i];
				if (xmlAttribute.LocalName == localName && xmlAttribute.NamespaceURI == namespaceURI)
				{
					if (!xmlAttribute.IsNamespace)
					{
						_source = xmlAttribute;
						_attributeIndex = i;
						return true;
					}
					return false;
				}
			}
		}
		return false;
	}

	public override bool MoveToFirstAttribute()
	{
		if (_source is XmlElement { HasAttributes: not false } xmlElement)
		{
			XmlAttributeCollection attributes = xmlElement.Attributes;
			for (int i = 0; i < attributes.Count; i++)
			{
				XmlAttribute xmlAttribute = attributes[i];
				if (!xmlAttribute.IsNamespace)
				{
					_source = xmlAttribute;
					_attributeIndex = i;
					return true;
				}
			}
		}
		return false;
	}

	public override bool MoveToNextAttribute()
	{
		if (!(_source is XmlAttribute { IsNamespace: false } xmlAttribute))
		{
			return false;
		}
		if (!CheckAttributePosition(xmlAttribute, out var attributes, _attributeIndex) && !ResetAttributePosition(xmlAttribute, attributes, out _attributeIndex))
		{
			return false;
		}
		for (int i = _attributeIndex + 1; i < attributes.Count; i++)
		{
			XmlAttribute xmlAttribute2 = attributes[i];
			if (!xmlAttribute2.IsNamespace)
			{
				_source = xmlAttribute2;
				_attributeIndex = i;
				return true;
			}
		}
		return false;
	}

	public override string GetNamespace(string name)
	{
		XmlNode xmlNode = _source;
		while (xmlNode != null && xmlNode.NodeType != XmlNodeType.Element)
		{
			xmlNode = ((!(xmlNode is XmlAttribute xmlAttribute)) ? xmlNode.ParentNode : xmlAttribute.OwnerElement);
		}
		XmlElement xmlElement = xmlNode as XmlElement;
		if (xmlElement != null)
		{
			string localName = ((name == null || name.Length == 0) ? _document.strXmlns : name);
			string strReservedXmlns = _document.strReservedXmlns;
			do
			{
				XmlAttribute attributeNode = xmlElement.GetAttributeNode(localName, strReservedXmlns);
				if (attributeNode != null)
				{
					return attributeNode.Value;
				}
				xmlElement = xmlElement.ParentNode as XmlElement;
			}
			while (xmlElement != null);
		}
		if (name == _document.strXml)
		{
			return _document.strReservedXml;
		}
		if (name == _document.strXmlns)
		{
			return _document.strReservedXmlns;
		}
		return string.Empty;
	}

	public override bool MoveToNamespace(string name)
	{
		if (name == _document.strXmlns)
		{
			return false;
		}
		XmlElement xmlElement = _source as XmlElement;
		if (xmlElement != null)
		{
			string localName = ((name == null || name.Length == 0) ? _document.strXmlns : name);
			string strReservedXmlns = _document.strReservedXmlns;
			do
			{
				XmlAttribute attributeNode = xmlElement.GetAttributeNode(localName, strReservedXmlns);
				if (attributeNode != null)
				{
					_namespaceParent = (XmlElement)_source;
					_source = attributeNode;
					return true;
				}
				xmlElement = xmlElement.ParentNode as XmlElement;
			}
			while (xmlElement != null);
			if (name == _document.strXml)
			{
				_namespaceParent = (XmlElement)_source;
				_source = _document.NamespaceXml;
				return true;
			}
		}
		return false;
	}

	public override bool MoveToFirstNamespace(XPathNamespaceScope scope)
	{
		if (!(_source is XmlElement xmlElement))
		{
			return false;
		}
		int index = int.MaxValue;
		switch (scope)
		{
		case XPathNamespaceScope.Local:
		{
			if (!xmlElement.HasAttributes)
			{
				return false;
			}
			XmlAttributeCollection attributes = xmlElement.Attributes;
			if (!MoveToFirstNamespaceLocal(attributes, ref index))
			{
				return false;
			}
			_source = attributes[index];
			_attributeIndex = index;
			_namespaceParent = xmlElement;
			break;
		}
		case XPathNamespaceScope.ExcludeXml:
		{
			XmlAttributeCollection attributes = xmlElement.Attributes;
			if (!MoveToFirstNamespaceGlobal(ref attributes, ref index))
			{
				return false;
			}
			XmlAttribute xmlAttribute = attributes[index];
			while (Ref.Equal(xmlAttribute.LocalName, _document.strXml))
			{
				if (!MoveToNextNamespaceGlobal(ref attributes, ref index))
				{
					return false;
				}
				xmlAttribute = attributes[index];
			}
			_source = xmlAttribute;
			_attributeIndex = index;
			_namespaceParent = xmlElement;
			break;
		}
		case XPathNamespaceScope.All:
		{
			XmlAttributeCollection attributes = xmlElement.Attributes;
			if (!MoveToFirstNamespaceGlobal(ref attributes, ref index))
			{
				_source = _document.NamespaceXml;
			}
			else
			{
				_source = attributes[index];
				_attributeIndex = index;
			}
			_namespaceParent = xmlElement;
			break;
		}
		default:
			return false;
		}
		return true;
	}

	private static bool MoveToFirstNamespaceLocal(XmlAttributeCollection attributes, ref int index)
	{
		for (int num = attributes.Count - 1; num >= 0; num--)
		{
			XmlAttribute xmlAttribute = attributes[num];
			if (xmlAttribute.IsNamespace)
			{
				index = num;
				return true;
			}
		}
		return false;
	}

	private static bool MoveToFirstNamespaceGlobal(ref XmlAttributeCollection attributes, ref int index)
	{
		if (MoveToFirstNamespaceLocal(attributes, ref index))
		{
			return true;
		}
		for (XmlElement xmlElement = attributes.parent.ParentNode as XmlElement; xmlElement != null; xmlElement = xmlElement.ParentNode as XmlElement)
		{
			if (xmlElement.HasAttributes)
			{
				attributes = xmlElement.Attributes;
				if (MoveToFirstNamespaceLocal(attributes, ref index))
				{
					return true;
				}
			}
		}
		return false;
	}

	public override bool MoveToNextNamespace(XPathNamespaceScope scope)
	{
		if (!(_source is XmlAttribute { IsNamespace: not false } xmlAttribute))
		{
			return false;
		}
		int index = _attributeIndex;
		if (!CheckAttributePosition(xmlAttribute, out var attributes, index) && !ResetAttributePosition(xmlAttribute, attributes, out index))
		{
			return false;
		}
		switch (scope)
		{
		case XPathNamespaceScope.Local:
			if (xmlAttribute.OwnerElement != _namespaceParent)
			{
				return false;
			}
			if (!MoveToNextNamespaceLocal(attributes, ref index))
			{
				return false;
			}
			_source = attributes[index];
			_attributeIndex = index;
			break;
		case XPathNamespaceScope.ExcludeXml:
		{
			XmlAttribute xmlAttribute2;
			string localName;
			do
			{
				if (!MoveToNextNamespaceGlobal(ref attributes, ref index))
				{
					return false;
				}
				xmlAttribute2 = attributes[index];
				localName = xmlAttribute2.LocalName;
			}
			while (PathHasDuplicateNamespace(xmlAttribute2.OwnerElement, _namespaceParent, localName) || Ref.Equal(localName, _document.strXml));
			_source = xmlAttribute2;
			_attributeIndex = index;
			break;
		}
		case XPathNamespaceScope.All:
		{
			XmlAttribute xmlAttribute2;
			do
			{
				if (!MoveToNextNamespaceGlobal(ref attributes, ref index))
				{
					if (PathHasDuplicateNamespace(null, _namespaceParent, _document.strXml))
					{
						return false;
					}
					_source = _document.NamespaceXml;
					return true;
				}
				xmlAttribute2 = attributes[index];
			}
			while (PathHasDuplicateNamespace(xmlAttribute2.OwnerElement, _namespaceParent, xmlAttribute2.LocalName));
			_source = xmlAttribute2;
			_attributeIndex = index;
			break;
		}
		default:
			return false;
		}
		return true;
	}

	private static bool MoveToNextNamespaceLocal(XmlAttributeCollection attributes, ref int index)
	{
		for (int num = index - 1; num >= 0; num--)
		{
			XmlAttribute xmlAttribute = attributes[num];
			if (xmlAttribute.IsNamespace)
			{
				index = num;
				return true;
			}
		}
		return false;
	}

	private static bool MoveToNextNamespaceGlobal(ref XmlAttributeCollection attributes, ref int index)
	{
		if (MoveToNextNamespaceLocal(attributes, ref index))
		{
			return true;
		}
		for (XmlElement xmlElement = attributes.parent.ParentNode as XmlElement; xmlElement != null; xmlElement = xmlElement.ParentNode as XmlElement)
		{
			if (xmlElement.HasAttributes)
			{
				attributes = xmlElement.Attributes;
				if (MoveToFirstNamespaceLocal(attributes, ref index))
				{
					return true;
				}
			}
		}
		return false;
	}

	private bool PathHasDuplicateNamespace(XmlElement top, XmlElement bottom, string localName)
	{
		XmlElement xmlElement = bottom;
		string strReservedXmlns = _document.strReservedXmlns;
		while (xmlElement != null && xmlElement != top)
		{
			XmlAttribute attributeNode = xmlElement.GetAttributeNode(localName, strReservedXmlns);
			if (attributeNode != null)
			{
				return true;
			}
			xmlElement = xmlElement.ParentNode as XmlElement;
		}
		return false;
	}

	public override string LookupNamespace(string prefix)
	{
		string text = base.LookupNamespace(prefix);
		if (text != null)
		{
			text = NameTable.Add(text);
		}
		return text;
	}

	public override bool MoveToNext()
	{
		XmlNode xmlNode = NextSibling(_source);
		if (xmlNode == null)
		{
			return false;
		}
		if (xmlNode.IsText && _source.IsText)
		{
			xmlNode = NextSibling(TextEnd(xmlNode));
			if (xmlNode == null)
			{
				return false;
			}
		}
		XmlNode parent = ParentNode(xmlNode);
		while (!IsValidChild(parent, xmlNode))
		{
			xmlNode = NextSibling(xmlNode);
			if (xmlNode == null)
			{
				return false;
			}
		}
		_source = xmlNode;
		return true;
	}

	public override bool MoveToPrevious()
	{
		XmlNode xmlNode = PreviousSibling(_source);
		if (xmlNode == null)
		{
			return false;
		}
		if (xmlNode.IsText)
		{
			if (_source.IsText)
			{
				xmlNode = PreviousSibling(TextStart(xmlNode));
				if (xmlNode == null)
				{
					return false;
				}
			}
			else
			{
				xmlNode = TextStart(xmlNode);
			}
		}
		XmlNode parent = ParentNode(xmlNode);
		while (!IsValidChild(parent, xmlNode))
		{
			xmlNode = PreviousSibling(xmlNode);
			if (xmlNode == null)
			{
				return false;
			}
		}
		_source = xmlNode;
		return true;
	}

	public override bool MoveToFirst()
	{
		if (_source.NodeType == XmlNodeType.Attribute)
		{
			return false;
		}
		XmlNode xmlNode = ParentNode(_source);
		if (xmlNode == null)
		{
			return false;
		}
		XmlNode xmlNode2 = FirstChild(xmlNode);
		while (!IsValidChild(xmlNode, xmlNode2))
		{
			xmlNode2 = NextSibling(xmlNode2);
			if (xmlNode2 == null)
			{
				return false;
			}
		}
		_source = xmlNode2;
		return true;
	}

	public override bool MoveToFirstChild()
	{
		XmlNode xmlNode;
		switch (_source.NodeType)
		{
		case XmlNodeType.Element:
			xmlNode = FirstChild(_source);
			if (xmlNode == null)
			{
				return false;
			}
			break;
		case XmlNodeType.Document:
		case XmlNodeType.DocumentFragment:
			xmlNode = FirstChild(_source);
			if (xmlNode == null)
			{
				return false;
			}
			while (!IsValidChild(_source, xmlNode))
			{
				xmlNode = NextSibling(xmlNode);
				if (xmlNode == null)
				{
					return false;
				}
			}
			break;
		default:
			return false;
		}
		_source = xmlNode;
		return true;
	}

	public override bool MoveToParent()
	{
		XmlNode xmlNode = ParentNode(_source);
		if (xmlNode != null)
		{
			_source = xmlNode;
			return true;
		}
		if (_source is XmlAttribute xmlAttribute)
		{
			xmlNode = (xmlAttribute.IsNamespace ? _namespaceParent : xmlAttribute.OwnerElement);
			if (xmlNode != null)
			{
				_source = xmlNode;
				_namespaceParent = null;
				return true;
			}
		}
		return false;
	}

	public override void MoveToRoot()
	{
		while (true)
		{
			XmlNode xmlNode = _source.ParentNode;
			if (xmlNode == null)
			{
				if (!(_source is XmlAttribute xmlAttribute))
				{
					break;
				}
				xmlNode = (xmlAttribute.IsNamespace ? _namespaceParent : xmlAttribute.OwnerElement);
				if (xmlNode == null)
				{
					break;
				}
			}
			_source = xmlNode;
		}
		_namespaceParent = null;
	}

	public override bool MoveTo(XPathNavigator other)
	{
		if (other is DocumentXPathNavigator documentXPathNavigator && _document == documentXPathNavigator._document)
		{
			_source = documentXPathNavigator._source;
			_attributeIndex = documentXPathNavigator._attributeIndex;
			_namespaceParent = documentXPathNavigator._namespaceParent;
			return true;
		}
		return false;
	}

	public override bool MoveToId(string id)
	{
		XmlElement elementById = _document.GetElementById(id);
		if (elementById != null)
		{
			_source = elementById;
			_namespaceParent = null;
			return true;
		}
		return false;
	}

	public override bool MoveToChild(string localName, string namespaceUri)
	{
		if (_source.NodeType == XmlNodeType.Attribute)
		{
			return false;
		}
		XmlNode xmlNode = FirstChild(_source);
		if (xmlNode != null)
		{
			do
			{
				if (xmlNode.NodeType == XmlNodeType.Element && xmlNode.LocalName == localName && xmlNode.NamespaceURI == namespaceUri)
				{
					_source = xmlNode;
					return true;
				}
				xmlNode = NextSibling(xmlNode);
			}
			while (xmlNode != null);
		}
		return false;
	}

	public override bool MoveToChild(XPathNodeType type)
	{
		if (_source.NodeType == XmlNodeType.Attribute)
		{
			return false;
		}
		XmlNode xmlNode = FirstChild(_source);
		if (xmlNode != null)
		{
			int contentKindMask = XPathNavigator.GetContentKindMask(type);
			if (contentKindMask == 0)
			{
				return false;
			}
			do
			{
				if (((1 << (int)xmlNode.XPNodeType) & contentKindMask) != 0)
				{
					_source = xmlNode;
					return true;
				}
				xmlNode = NextSibling(xmlNode);
			}
			while (xmlNode != null);
		}
		return false;
	}

	public override bool MoveToFollowing(string localName, string namespaceUri, XPathNavigator end)
	{
		XmlNode xmlNode = null;
		DocumentXPathNavigator documentXPathNavigator = end as DocumentXPathNavigator;
		if (documentXPathNavigator != null)
		{
			if (_document != documentXPathNavigator._document)
			{
				return false;
			}
			XmlNodeType nodeType = documentXPathNavigator._source.NodeType;
			if (nodeType == XmlNodeType.Attribute)
			{
				documentXPathNavigator = (DocumentXPathNavigator)documentXPathNavigator.Clone();
				if (!documentXPathNavigator.MoveToNonDescendant())
				{
					return false;
				}
			}
			xmlNode = documentXPathNavigator._source;
		}
		XmlNode xmlNode2 = _source;
		if (xmlNode2.NodeType == XmlNodeType.Attribute)
		{
			xmlNode2 = ((XmlAttribute)xmlNode2).OwnerElement;
			if (xmlNode2 == null)
			{
				return false;
			}
		}
		do
		{
			XmlNode firstChild = xmlNode2.FirstChild;
			if (firstChild != null)
			{
				xmlNode2 = firstChild;
			}
			else
			{
				XmlNode nextSibling;
				while (true)
				{
					nextSibling = xmlNode2.NextSibling;
					if (nextSibling != null)
					{
						break;
					}
					XmlNode parentNode = xmlNode2.ParentNode;
					if (parentNode != null)
					{
						xmlNode2 = parentNode;
						continue;
					}
					return false;
				}
				xmlNode2 = nextSibling;
			}
			if (xmlNode2 == xmlNode)
			{
				return false;
			}
		}
		while (xmlNode2.NodeType != XmlNodeType.Element || xmlNode2.LocalName != localName || xmlNode2.NamespaceURI != namespaceUri);
		_source = xmlNode2;
		return true;
	}

	public override bool MoveToFollowing(XPathNodeType type, XPathNavigator end)
	{
		XmlNode xmlNode = null;
		DocumentXPathNavigator documentXPathNavigator = end as DocumentXPathNavigator;
		if (documentXPathNavigator != null)
		{
			if (_document != documentXPathNavigator._document)
			{
				return false;
			}
			XmlNodeType nodeType = documentXPathNavigator._source.NodeType;
			if (nodeType == XmlNodeType.Attribute)
			{
				documentXPathNavigator = (DocumentXPathNavigator)documentXPathNavigator.Clone();
				if (!documentXPathNavigator.MoveToNonDescendant())
				{
					return false;
				}
			}
			xmlNode = documentXPathNavigator._source;
		}
		int contentKindMask = XPathNavigator.GetContentKindMask(type);
		if (contentKindMask == 0)
		{
			return false;
		}
		XmlNode xmlNode2 = _source;
		switch (xmlNode2.NodeType)
		{
		case XmlNodeType.Attribute:
			xmlNode2 = ((XmlAttribute)xmlNode2).OwnerElement;
			if (xmlNode2 == null)
			{
				return false;
			}
			break;
		case XmlNodeType.Text:
		case XmlNodeType.CDATA:
		case XmlNodeType.Whitespace:
		case XmlNodeType.SignificantWhitespace:
			xmlNode2 = TextEnd(xmlNode2);
			break;
		}
		do
		{
			XmlNode firstChild = xmlNode2.FirstChild;
			if (firstChild != null)
			{
				xmlNode2 = firstChild;
			}
			else
			{
				XmlNode nextSibling;
				while (true)
				{
					nextSibling = xmlNode2.NextSibling;
					if (nextSibling != null)
					{
						break;
					}
					XmlNode parentNode = xmlNode2.ParentNode;
					if (parentNode != null)
					{
						xmlNode2 = parentNode;
						continue;
					}
					return false;
				}
				xmlNode2 = nextSibling;
			}
			if (xmlNode2 == xmlNode)
			{
				return false;
			}
		}
		while (((1 << (int)xmlNode2.XPNodeType) & contentKindMask) == 0);
		_source = xmlNode2;
		return true;
	}

	public override bool MoveToNext(string localName, string namespaceUri)
	{
		XmlNode xmlNode = NextSibling(_source);
		if (xmlNode == null)
		{
			return false;
		}
		do
		{
			if (xmlNode.NodeType == XmlNodeType.Element && xmlNode.LocalName == localName && xmlNode.NamespaceURI == namespaceUri)
			{
				_source = xmlNode;
				return true;
			}
			xmlNode = NextSibling(xmlNode);
		}
		while (xmlNode != null);
		return false;
	}

	public override bool MoveToNext(XPathNodeType type)
	{
		XmlNode xmlNode = NextSibling(_source);
		if (xmlNode == null)
		{
			return false;
		}
		if (xmlNode.IsText && _source.IsText)
		{
			xmlNode = NextSibling(TextEnd(xmlNode));
			if (xmlNode == null)
			{
				return false;
			}
		}
		int contentKindMask = XPathNavigator.GetContentKindMask(type);
		if (contentKindMask == 0)
		{
			return false;
		}
		do
		{
			if (((1 << (int)xmlNode.XPNodeType) & contentKindMask) != 0)
			{
				_source = xmlNode;
				return true;
			}
			xmlNode = NextSibling(xmlNode);
		}
		while (xmlNode != null);
		return false;
	}

	public override bool IsSamePosition(XPathNavigator other)
	{
		if (other is DocumentXPathNavigator documentXPathNavigator)
		{
			CalibrateText();
			documentXPathNavigator.CalibrateText();
			if (_source == documentXPathNavigator._source)
			{
				return _namespaceParent == documentXPathNavigator._namespaceParent;
			}
			return false;
		}
		return false;
	}

	public override bool IsDescendant([NotNullWhen(true)] XPathNavigator other)
	{
		if (other is DocumentXPathNavigator documentXPathNavigator)
		{
			return IsDescendant(_source, documentXPathNavigator._source);
		}
		return false;
	}

	public override bool CheckValidity(XmlSchemaSet schemas, ValidationEventHandler validationEventHandler)
	{
		XmlDocument xmlDocument;
		if (_source.NodeType == XmlNodeType.Document)
		{
			xmlDocument = (XmlDocument)_source;
		}
		else
		{
			xmlDocument = _source.OwnerDocument;
			if (schemas != null)
			{
				throw new ArgumentException(System.SR.Format(System.SR.XPathDocument_SchemaSetNotAllowed, null));
			}
		}
		if (schemas == null && xmlDocument != null)
		{
			schemas = xmlDocument.Schemas;
		}
		if (schemas == null || schemas.Count == 0)
		{
			throw new InvalidOperationException(System.SR.XmlDocument_NoSchemaInfo);
		}
		DocumentSchemaValidator documentSchemaValidator = new DocumentSchemaValidator(xmlDocument, schemas, validationEventHandler);
		documentSchemaValidator.PsviAugmentation = false;
		return documentSchemaValidator.Validate(_source);
	}

	private static XmlNode OwnerNode(XmlNode node)
	{
		XmlNode parentNode = node.ParentNode;
		if (parentNode != null)
		{
			return parentNode;
		}
		if (node is XmlAttribute xmlAttribute)
		{
			return xmlAttribute.OwnerElement;
		}
		return null;
	}

	private static int GetDepth(XmlNode node)
	{
		int num = 0;
		for (XmlNode xmlNode = OwnerNode(node); xmlNode != null; xmlNode = OwnerNode(xmlNode))
		{
			num++;
		}
		return num;
	}

	private XmlNodeOrder Compare(XmlNode node1, XmlNode node2)
	{
		if (node1.XPNodeType == XPathNodeType.Attribute)
		{
			if (node2.XPNodeType == XPathNodeType.Attribute)
			{
				XmlElement ownerElement = ((XmlAttribute)node1).OwnerElement;
				if (ownerElement.HasAttributes)
				{
					XmlAttributeCollection attributes = ownerElement.Attributes;
					for (int i = 0; i < attributes.Count; i++)
					{
						XmlAttribute xmlAttribute = attributes[i];
						if (xmlAttribute == node1)
						{
							return XmlNodeOrder.Before;
						}
						if (xmlAttribute == node2)
						{
							return XmlNodeOrder.After;
						}
					}
				}
				return XmlNodeOrder.Unknown;
			}
			return XmlNodeOrder.Before;
		}
		if (node2.XPNodeType == XPathNodeType.Attribute)
		{
			return XmlNodeOrder.After;
		}
		XmlNode nextSibling = node1.NextSibling;
		while (nextSibling != null && nextSibling != node2)
		{
			nextSibling = nextSibling.NextSibling;
		}
		if (nextSibling == null)
		{
			return XmlNodeOrder.After;
		}
		return XmlNodeOrder.Before;
	}

	public override XmlNodeOrder ComparePosition(XPathNavigator other)
	{
		if (!(other is DocumentXPathNavigator documentXPathNavigator))
		{
			return XmlNodeOrder.Unknown;
		}
		CalibrateText();
		documentXPathNavigator.CalibrateText();
		if (_source == documentXPathNavigator._source && _namespaceParent == documentXPathNavigator._namespaceParent)
		{
			return XmlNodeOrder.Same;
		}
		if (_namespaceParent != null || documentXPathNavigator._namespaceParent != null)
		{
			return base.ComparePosition(other);
		}
		XmlNode xmlNode = _source;
		XmlNode xmlNode2 = documentXPathNavigator._source;
		XmlNode xmlNode3 = OwnerNode(xmlNode);
		XmlNode xmlNode4 = OwnerNode(xmlNode2);
		if (xmlNode3 == xmlNode4)
		{
			if (xmlNode3 == null)
			{
				return XmlNodeOrder.Unknown;
			}
			return Compare(xmlNode, xmlNode2);
		}
		int num = GetDepth(xmlNode);
		int num2 = GetDepth(xmlNode2);
		if (num2 > num)
		{
			while (xmlNode2 != null && num2 > num)
			{
				xmlNode2 = OwnerNode(xmlNode2);
				num2--;
			}
			if (xmlNode == xmlNode2)
			{
				return XmlNodeOrder.Before;
			}
			xmlNode4 = OwnerNode(xmlNode2);
		}
		else if (num > num2)
		{
			while (xmlNode != null && num > num2)
			{
				xmlNode = OwnerNode(xmlNode);
				num--;
			}
			if (xmlNode == xmlNode2)
			{
				return XmlNodeOrder.After;
			}
			xmlNode3 = OwnerNode(xmlNode);
		}
		while (xmlNode3 != null && xmlNode4 != null)
		{
			if (xmlNode3 == xmlNode4)
			{
				return Compare(xmlNode, xmlNode2);
			}
			xmlNode = xmlNode3;
			xmlNode2 = xmlNode4;
			xmlNode3 = OwnerNode(xmlNode);
			xmlNode4 = OwnerNode(xmlNode2);
		}
		return XmlNodeOrder.Unknown;
	}

	XmlNode IHasXmlNode.GetNode()
	{
		return _source;
	}

	public override XPathNodeIterator SelectDescendants(string localName, string namespaceURI, bool matchSelf)
	{
		string text = _document.NameTable.Get(namespaceURI);
		if (text == null || _source.NodeType == XmlNodeType.Attribute)
		{
			return new DocumentXPathNodeIterator_Empty(this);
		}
		string text2 = _document.NameTable.Get(localName);
		if (text2 == null)
		{
			return new DocumentXPathNodeIterator_Empty(this);
		}
		if (text2.Length == 0)
		{
			if (matchSelf)
			{
				return new DocumentXPathNodeIterator_ElemChildren_AndSelf_NoLocalName(this, text);
			}
			return new DocumentXPathNodeIterator_ElemChildren_NoLocalName(this, text);
		}
		if (matchSelf)
		{
			return new DocumentXPathNodeIterator_ElemChildren_AndSelf(this, text2, text);
		}
		return new DocumentXPathNodeIterator_ElemChildren(this, text2, text);
	}

	public override XPathNodeIterator SelectDescendants(XPathNodeType nt, bool includeSelf)
	{
		if (nt == XPathNodeType.Element)
		{
			XmlNodeType nodeType = _source.NodeType;
			if (nodeType != XmlNodeType.Document && nodeType != XmlNodeType.Element)
			{
				return new DocumentXPathNodeIterator_Empty(this);
			}
			if (includeSelf)
			{
				return new DocumentXPathNodeIterator_AllElemChildren_AndSelf(this);
			}
			return new DocumentXPathNodeIterator_AllElemChildren(this);
		}
		return base.SelectDescendants(nt, includeSelf);
	}

	public override XmlWriter PrependChild()
	{
		XmlNodeType nodeType = _source.NodeType;
		if (nodeType != XmlNodeType.Element && nodeType != XmlNodeType.Document && nodeType != XmlNodeType.DocumentFragment)
		{
			throw new InvalidOperationException(System.SR.Xpn_BadPosition);
		}
		DocumentXmlWriter documentXmlWriter = new DocumentXmlWriter(DocumentXmlWriterType.PrependChild, _source, _document);
		documentXmlWriter.NamespaceManager = GetNamespaceManager(_source, _document);
		return new XmlWellFormedWriter(documentXmlWriter, documentXmlWriter.Settings);
	}

	public override XmlWriter AppendChild()
	{
		XmlNodeType nodeType = _source.NodeType;
		if (nodeType != XmlNodeType.Element && nodeType != XmlNodeType.Document && nodeType != XmlNodeType.DocumentFragment)
		{
			throw new InvalidOperationException(System.SR.Xpn_BadPosition);
		}
		DocumentXmlWriter documentXmlWriter = new DocumentXmlWriter(DocumentXmlWriterType.AppendChild, _source, _document);
		documentXmlWriter.NamespaceManager = GetNamespaceManager(_source, _document);
		return new XmlWellFormedWriter(documentXmlWriter, documentXmlWriter.Settings);
	}

	public override XmlWriter InsertAfter()
	{
		XmlNode xmlNode = _source;
		switch (xmlNode.NodeType)
		{
		case XmlNodeType.Attribute:
		case XmlNodeType.Document:
		case XmlNodeType.DocumentFragment:
			throw new InvalidOperationException(System.SR.Xpn_BadPosition);
		case XmlNodeType.Text:
		case XmlNodeType.CDATA:
		case XmlNodeType.Whitespace:
		case XmlNodeType.SignificantWhitespace:
			xmlNode = TextEnd(xmlNode);
			break;
		}
		DocumentXmlWriter documentXmlWriter = new DocumentXmlWriter(DocumentXmlWriterType.InsertSiblingAfter, xmlNode, _document);
		documentXmlWriter.NamespaceManager = GetNamespaceManager(xmlNode.ParentNode, _document);
		return new XmlWellFormedWriter(documentXmlWriter, documentXmlWriter.Settings);
	}

	public override XmlWriter InsertBefore()
	{
		switch (_source.NodeType)
		{
		case XmlNodeType.Attribute:
		case XmlNodeType.Document:
		case XmlNodeType.DocumentFragment:
			throw new InvalidOperationException(System.SR.Xpn_BadPosition);
		case XmlNodeType.Text:
		case XmlNodeType.CDATA:
		case XmlNodeType.Whitespace:
		case XmlNodeType.SignificantWhitespace:
			CalibrateText();
			break;
		}
		DocumentXmlWriter documentXmlWriter = new DocumentXmlWriter(DocumentXmlWriterType.InsertSiblingBefore, _source, _document);
		documentXmlWriter.NamespaceManager = GetNamespaceManager(_source.ParentNode, _document);
		return new XmlWellFormedWriter(documentXmlWriter, documentXmlWriter.Settings);
	}

	public override XmlWriter CreateAttributes()
	{
		if (_source.NodeType != XmlNodeType.Element)
		{
			throw new InvalidOperationException(System.SR.Xpn_BadPosition);
		}
		DocumentXmlWriter documentXmlWriter = new DocumentXmlWriter(DocumentXmlWriterType.AppendAttribute, _source, _document);
		documentXmlWriter.NamespaceManager = GetNamespaceManager(_source, _document);
		return new XmlWellFormedWriter(documentXmlWriter, documentXmlWriter.Settings);
	}

	public override XmlWriter ReplaceRange(XPathNavigator lastSiblingToReplace)
	{
		if (!(lastSiblingToReplace is DocumentXPathNavigator documentXPathNavigator))
		{
			if (lastSiblingToReplace == null)
			{
				throw new ArgumentNullException("lastSiblingToReplace");
			}
			throw new NotSupportedException();
		}
		CalibrateText();
		documentXPathNavigator.CalibrateText();
		XmlNode source = _source;
		XmlNode xmlNode = documentXPathNavigator._source;
		if (source == xmlNode)
		{
			switch (source.NodeType)
			{
			case XmlNodeType.Attribute:
			case XmlNodeType.Document:
			case XmlNodeType.DocumentFragment:
				throw new InvalidOperationException(System.SR.Xpn_BadPosition);
			case XmlNodeType.Text:
			case XmlNodeType.CDATA:
			case XmlNodeType.Whitespace:
			case XmlNodeType.SignificantWhitespace:
				xmlNode = documentXPathNavigator.TextEnd(xmlNode);
				break;
			}
		}
		else
		{
			if (xmlNode.IsText)
			{
				xmlNode = documentXPathNavigator.TextEnd(xmlNode);
			}
			if (!IsFollowingSibling(source, xmlNode))
			{
				throw new InvalidOperationException(System.SR.Xpn_BadPosition);
			}
		}
		DocumentXmlWriter documentXmlWriter = new DocumentXmlWriter(DocumentXmlWriterType.ReplaceToFollowingSibling, source, _document);
		documentXmlWriter.NamespaceManager = GetNamespaceManager(source.ParentNode, _document);
		documentXmlWriter.Navigator = this;
		documentXmlWriter.EndNode = xmlNode;
		return new XmlWellFormedWriter(documentXmlWriter, documentXmlWriter.Settings);
	}

	public override void DeleteRange(XPathNavigator lastSiblingToDelete)
	{
		if (!(lastSiblingToDelete is DocumentXPathNavigator documentXPathNavigator))
		{
			if (lastSiblingToDelete == null)
			{
				throw new ArgumentNullException("lastSiblingToDelete");
			}
			throw new NotSupportedException();
		}
		CalibrateText();
		documentXPathNavigator.CalibrateText();
		XmlNode source = _source;
		XmlNode xmlNode = documentXPathNavigator._source;
		if (source == xmlNode)
		{
			switch (source.NodeType)
			{
			case XmlNodeType.Attribute:
			{
				XmlAttribute xmlAttribute = (XmlAttribute)source;
				if (!xmlAttribute.IsNamespace)
				{
					XmlNode xmlNode2 = OwnerNode(xmlAttribute);
					DeleteAttribute(xmlAttribute, _attributeIndex);
					if (xmlNode2 != null)
					{
						ResetPosition(xmlNode2);
					}
					break;
				}
				goto default;
			}
			case XmlNodeType.Text:
			case XmlNodeType.CDATA:
			case XmlNodeType.Whitespace:
			case XmlNodeType.SignificantWhitespace:
				xmlNode = documentXPathNavigator.TextEnd(xmlNode);
				goto case XmlNodeType.Element;
			case XmlNodeType.Element:
			case XmlNodeType.ProcessingInstruction:
			case XmlNodeType.Comment:
			{
				XmlNode xmlNode2 = OwnerNode(source);
				DeleteToFollowingSibling(source, xmlNode);
				if (xmlNode2 != null)
				{
					ResetPosition(xmlNode2);
				}
				break;
			}
			default:
				throw new InvalidOperationException(System.SR.Xpn_BadPosition);
			}
		}
		else
		{
			if (xmlNode.IsText)
			{
				xmlNode = documentXPathNavigator.TextEnd(xmlNode);
			}
			if (!IsFollowingSibling(source, xmlNode))
			{
				throw new InvalidOperationException(System.SR.Xpn_BadPosition);
			}
			XmlNode xmlNode3 = OwnerNode(source);
			DeleteToFollowingSibling(source, xmlNode);
			if (xmlNode3 != null)
			{
				ResetPosition(xmlNode3);
			}
		}
	}

	public override void DeleteSelf()
	{
		XmlNode source = _source;
		XmlNode end = source;
		switch (source.NodeType)
		{
		case XmlNodeType.Attribute:
		{
			XmlAttribute xmlAttribute = (XmlAttribute)source;
			if (!xmlAttribute.IsNamespace)
			{
				XmlNode xmlNode = OwnerNode(xmlAttribute);
				DeleteAttribute(xmlAttribute, _attributeIndex);
				if (xmlNode != null)
				{
					ResetPosition(xmlNode);
				}
				break;
			}
			goto default;
		}
		case XmlNodeType.Text:
		case XmlNodeType.CDATA:
		case XmlNodeType.Whitespace:
		case XmlNodeType.SignificantWhitespace:
			CalibrateText();
			source = _source;
			end = TextEnd(source);
			goto case XmlNodeType.Element;
		case XmlNodeType.Element:
		case XmlNodeType.ProcessingInstruction:
		case XmlNodeType.Comment:
		{
			XmlNode xmlNode = OwnerNode(source);
			DeleteToFollowingSibling(source, end);
			if (xmlNode != null)
			{
				ResetPosition(xmlNode);
			}
			break;
		}
		default:
			throw new InvalidOperationException(System.SR.Xpn_BadPosition);
		}
	}

	private static void DeleteAttribute(XmlAttribute attribute, int index)
	{
		if (!CheckAttributePosition(attribute, out var attributes, index) && !ResetAttributePosition(attribute, attributes, out index))
		{
			throw new InvalidOperationException(System.SR.Xpn_MissingParent);
		}
		if (attribute.IsReadOnly)
		{
			throw new InvalidOperationException(System.SR.Xdom_Node_Modify_ReadOnly);
		}
		attributes.RemoveAt(index);
	}

	internal static void DeleteToFollowingSibling(XmlNode node, XmlNode end)
	{
		XmlNode parentNode = node.ParentNode;
		if (parentNode == null)
		{
			throw new InvalidOperationException(System.SR.Xpn_MissingParent);
		}
		if (node.IsReadOnly || end.IsReadOnly)
		{
			throw new InvalidOperationException(System.SR.Xdom_Node_Modify_ReadOnly);
		}
		while (node != end)
		{
			XmlNode oldChild = node;
			node = node.NextSibling;
			parentNode.RemoveChild(oldChild);
		}
		parentNode.RemoveChild(node);
	}

	private static XmlNamespaceManager GetNamespaceManager(XmlNode node, XmlDocument document)
	{
		XmlNamespaceManager xmlNamespaceManager = new XmlNamespaceManager(document.NameTable);
		List<XmlElement> list = new List<XmlElement>();
		while (node != null)
		{
			if (node is XmlElement { HasAttributes: not false } xmlElement)
			{
				list.Add(xmlElement);
			}
			node = node.ParentNode;
		}
		for (int num = list.Count - 1; num >= 0; num--)
		{
			xmlNamespaceManager.PushScope();
			XmlAttributeCollection attributes = list[num].Attributes;
			for (int i = 0; i < attributes.Count; i++)
			{
				XmlAttribute xmlAttribute = attributes[i];
				if (xmlAttribute.IsNamespace)
				{
					string prefix = ((xmlAttribute.Prefix.Length == 0) ? string.Empty : xmlAttribute.LocalName);
					xmlNamespaceManager.AddNamespace(prefix, xmlAttribute.Value);
				}
			}
		}
		return xmlNamespaceManager;
	}

	[MemberNotNull("_source")]
	internal void ResetPosition(XmlNode node)
	{
		_source = node;
		if (!(node is XmlAttribute xmlAttribute))
		{
			return;
		}
		XmlElement ownerElement = xmlAttribute.OwnerElement;
		if (ownerElement != null)
		{
			ResetAttributePosition(xmlAttribute, ownerElement.Attributes, out _attributeIndex);
			if (xmlAttribute.IsNamespace)
			{
				_namespaceParent = ownerElement;
			}
		}
	}

	private static bool ResetAttributePosition(XmlAttribute attribute, [NotNullWhen(true)] XmlAttributeCollection attributes, out int index)
	{
		if (attributes != null)
		{
			for (int i = 0; i < attributes.Count; i++)
			{
				if (attribute == attributes[i])
				{
					index = i;
					return true;
				}
			}
		}
		index = 0;
		return false;
	}

	private static bool CheckAttributePosition(XmlAttribute attribute, [NotNullWhen(true)] out XmlAttributeCollection attributes, int index)
	{
		XmlElement ownerElement = attribute.OwnerElement;
		if (ownerElement != null)
		{
			attributes = ownerElement.Attributes;
			if (index >= 0 && index < attributes.Count && attribute == attributes[index])
			{
				return true;
			}
		}
		else
		{
			attributes = null;
		}
		return false;
	}

	private void CalibrateText()
	{
		for (XmlNode xmlNode = PreviousText(_source); xmlNode != null; xmlNode = PreviousText(xmlNode))
		{
			ResetPosition(xmlNode);
		}
	}

	private XmlNode ParentNode(XmlNode node)
	{
		XmlNode parentNode = node.ParentNode;
		if (!_document.HasEntityReferences)
		{
			return parentNode;
		}
		return ParentNodeTail(parentNode);
	}

	private XmlNode ParentNodeTail(XmlNode parent)
	{
		while (parent != null && parent.NodeType == XmlNodeType.EntityReference)
		{
			parent = parent.ParentNode;
		}
		return parent;
	}

	private XmlNode FirstChild(XmlNode node)
	{
		XmlNode firstChild = node.FirstChild;
		if (!_document.HasEntityReferences)
		{
			return firstChild;
		}
		return FirstChildTail(firstChild);
	}

	private XmlNode FirstChildTail(XmlNode child)
	{
		while (child != null && child.NodeType == XmlNodeType.EntityReference)
		{
			child = child.FirstChild;
		}
		return child;
	}

	private XmlNode NextSibling(XmlNode node)
	{
		XmlNode nextSibling = node.NextSibling;
		if (!_document.HasEntityReferences)
		{
			return nextSibling;
		}
		return NextSiblingTail(node, nextSibling);
	}

	private XmlNode NextSiblingTail(XmlNode node, XmlNode sibling)
	{
		XmlNode xmlNode = node;
		while (sibling == null)
		{
			xmlNode = xmlNode.ParentNode;
			if (xmlNode == null || xmlNode.NodeType != XmlNodeType.EntityReference)
			{
				return null;
			}
			sibling = xmlNode.NextSibling;
		}
		while (sibling != null && sibling.NodeType == XmlNodeType.EntityReference)
		{
			sibling = sibling.FirstChild;
		}
		return sibling;
	}

	private XmlNode PreviousSibling(XmlNode node)
	{
		XmlNode previousSibling = node.PreviousSibling;
		if (!_document.HasEntityReferences)
		{
			return previousSibling;
		}
		return PreviousSiblingTail(node, previousSibling);
	}

	private XmlNode PreviousSiblingTail(XmlNode node, XmlNode sibling)
	{
		XmlNode xmlNode = node;
		while (sibling == null)
		{
			xmlNode = xmlNode.ParentNode;
			if (xmlNode == null || xmlNode.NodeType != XmlNodeType.EntityReference)
			{
				return null;
			}
			sibling = xmlNode.PreviousSibling;
		}
		while (sibling != null && sibling.NodeType == XmlNodeType.EntityReference)
		{
			sibling = sibling.LastChild;
		}
		return sibling;
	}

	private XmlNode PreviousText(XmlNode node)
	{
		XmlNode previousText = node.PreviousText;
		if (!_document.HasEntityReferences)
		{
			return previousText;
		}
		return PreviousTextTail(node, previousText);
	}

	private XmlNode PreviousTextTail(XmlNode node, XmlNode text)
	{
		if (text != null)
		{
			return text;
		}
		if (!node.IsText)
		{
			return null;
		}
		XmlNode xmlNode = node.PreviousSibling;
		XmlNode xmlNode2 = node;
		while (xmlNode == null)
		{
			xmlNode2 = xmlNode2.ParentNode;
			if (xmlNode2 == null || xmlNode2.NodeType != XmlNodeType.EntityReference)
			{
				return null;
			}
			xmlNode = xmlNode2.PreviousSibling;
		}
		while (xmlNode != null)
		{
			switch (xmlNode.NodeType)
			{
			case XmlNodeType.EntityReference:
				break;
			case XmlNodeType.Text:
			case XmlNodeType.CDATA:
			case XmlNodeType.Whitespace:
			case XmlNodeType.SignificantWhitespace:
				return xmlNode;
			default:
				return null;
			}
			xmlNode = xmlNode.LastChild;
		}
		return null;
	}

	internal static bool IsFollowingSibling(XmlNode left, [NotNullWhen(true)] XmlNode right)
	{
		XmlNode xmlNode = left;
		while (true)
		{
			xmlNode = xmlNode.NextSibling;
			if (xmlNode == null)
			{
				break;
			}
			if (xmlNode == right)
			{
				return true;
			}
		}
		return false;
	}

	private static bool IsDescendant(XmlNode top, XmlNode bottom)
	{
		while (true)
		{
			XmlNode xmlNode = bottom.ParentNode;
			if (xmlNode == null)
			{
				if (!(bottom is XmlAttribute xmlAttribute))
				{
					break;
				}
				xmlNode = xmlAttribute.OwnerElement;
				if (xmlNode == null)
				{
					break;
				}
			}
			bottom = xmlNode;
			if (top == bottom)
			{
				return true;
			}
		}
		return false;
	}

	private static bool IsValidChild(XmlNode parent, XmlNode child)
	{
		switch (parent.NodeType)
		{
		case XmlNodeType.Element:
			return true;
		case XmlNodeType.DocumentFragment:
			switch (child.NodeType)
			{
			case XmlNodeType.Element:
			case XmlNodeType.Text:
			case XmlNodeType.CDATA:
			case XmlNodeType.ProcessingInstruction:
			case XmlNodeType.Comment:
			case XmlNodeType.Whitespace:
			case XmlNodeType.SignificantWhitespace:
				return true;
			}
			break;
		case XmlNodeType.Document:
		{
			XmlNodeType nodeType = child.NodeType;
			if (nodeType == XmlNodeType.Element || (uint)(nodeType - 7) <= 1u)
			{
				return true;
			}
			break;
		}
		}
		return false;
	}

	private XmlNode TextStart(XmlNode node)
	{
		XmlNode xmlNode = node;
		XmlNode result;
		do
		{
			result = xmlNode;
			xmlNode = PreviousSibling(xmlNode);
		}
		while (xmlNode != null && xmlNode.IsText);
		return result;
	}

	private XmlNode TextEnd(XmlNode node)
	{
		XmlNode xmlNode = node;
		XmlNode result;
		do
		{
			result = xmlNode;
			xmlNode = NextSibling(xmlNode);
		}
		while (xmlNode != null && xmlNode.IsText);
		return result;
	}
}
