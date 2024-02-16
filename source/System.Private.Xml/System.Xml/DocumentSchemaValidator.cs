using System.Collections;
using System.Collections.Generic;
using System.Xml.Schema;
using System.Xml.XPath;

namespace System.Xml;

internal sealed class DocumentSchemaValidator : IXmlNamespaceResolver
{
	private XmlSchemaValidator _validator;

	private readonly XmlSchemaSet _schemas;

	private readonly XmlNamespaceManager _nsManager;

	private readonly XmlNameTable _nameTable;

	private ArrayList _defaultAttributes;

	private readonly XmlValueGetter _nodeValueGetter;

	private XmlSchemaInfo _attributeSchemaInfo;

	private XmlSchemaInfo _schemaInfo;

	private readonly ValidationEventHandler _eventHandler;

	private readonly ValidationEventHandler _internalEventHandler;

	private XmlNode _startNode;

	private XmlNode _currentNode;

	private readonly XmlDocument _document;

	private XmlNode[] _nodeSequenceToValidate;

	private bool _isPartialTreeValid;

	private bool _psviAugmentation;

	private bool _isValid;

	private readonly string _nsXmlNs;

	private readonly string _nsXsi;

	private readonly string _xsiType;

	private readonly string _xsiNil;

	public bool PsviAugmentation
	{
		set
		{
			_psviAugmentation = value;
		}
	}

	private IXmlNamespaceResolver NamespaceResolver
	{
		get
		{
			if (_startNode == _document)
			{
				return _nsManager;
			}
			return this;
		}
	}

	public DocumentSchemaValidator(XmlDocument ownerDocument, XmlSchemaSet schemas, ValidationEventHandler eventHandler)
	{
		_schemas = schemas;
		_eventHandler = eventHandler;
		_document = ownerDocument;
		_internalEventHandler = InternalValidationCallBack;
		_nameTable = _document.NameTable;
		_nsManager = new XmlNamespaceManager(_nameTable);
		_nodeValueGetter = GetNodeValue;
		_psviAugmentation = true;
		_nsXmlNs = _nameTable.Add("http://www.w3.org/2000/xmlns/");
		_nsXsi = _nameTable.Add("http://www.w3.org/2001/XMLSchema-instance");
		_xsiType = _nameTable.Add("type");
		_xsiNil = _nameTable.Add("nil");
	}

	public bool Validate(XmlNode nodeToValidate)
	{
		XmlSchemaObject xmlSchemaObject = null;
		XmlSchemaValidationFlags xmlSchemaValidationFlags = XmlSchemaValidationFlags.AllowXmlAttributes;
		_startNode = nodeToValidate;
		XmlNodeType nodeType = nodeToValidate.NodeType;
		if (nodeType <= XmlNodeType.Attribute)
		{
			if (nodeType != XmlNodeType.Element)
			{
				if (nodeType != XmlNodeType.Attribute || nodeToValidate.XPNodeType == XPathNodeType.Namespace)
				{
					goto IL_00fe;
				}
				xmlSchemaObject = nodeToValidate.SchemaInfo.SchemaAttribute;
				if (xmlSchemaObject == null)
				{
					xmlSchemaObject = FindSchemaInfo(nodeToValidate as XmlAttribute);
					if (xmlSchemaObject == null)
					{
						throw new XmlSchemaValidationException(System.SR.XmlDocument_NoNodeSchemaInfo, null, nodeToValidate);
					}
				}
			}
			else
			{
				IXmlSchemaInfo schemaInfo = nodeToValidate.SchemaInfo;
				XmlSchemaElement schemaElement = schemaInfo.SchemaElement;
				if (schemaElement != null)
				{
					xmlSchemaObject = (schemaElement.RefName.IsEmpty ? schemaElement : _schemas.GlobalElements[schemaElement.QualifiedName]);
				}
				else
				{
					xmlSchemaObject = schemaInfo.SchemaType;
					if (xmlSchemaObject == null)
					{
						if (nodeToValidate.ParentNode.NodeType == XmlNodeType.Document)
						{
							nodeToValidate = nodeToValidate.ParentNode;
						}
						else
						{
							xmlSchemaObject = FindSchemaInfo(nodeToValidate as XmlElement);
							if (xmlSchemaObject == null)
							{
								throw new XmlSchemaValidationException(System.SR.XmlDocument_NoNodeSchemaInfo, null, nodeToValidate);
							}
						}
					}
				}
			}
		}
		else if (nodeType != XmlNodeType.Document)
		{
			if (nodeType != XmlNodeType.DocumentFragment)
			{
				goto IL_00fe;
			}
		}
		else
		{
			xmlSchemaValidationFlags |= XmlSchemaValidationFlags.ProcessIdentityConstraints;
		}
		_isValid = true;
		CreateValidator(xmlSchemaObject, xmlSchemaValidationFlags);
		if (_psviAugmentation)
		{
			if (_schemaInfo == null)
			{
				_schemaInfo = new XmlSchemaInfo();
			}
			_attributeSchemaInfo = new XmlSchemaInfo();
		}
		ValidateNode(nodeToValidate);
		_validator.EndValidation();
		return _isValid;
		IL_00fe:
		throw new InvalidOperationException(System.SR.Format(System.SR.XmlDocument_ValidateInvalidNodeType, null));
	}

	public IDictionary<string, string> GetNamespacesInScope(XmlNamespaceScope scope)
	{
		IDictionary<string, string> namespacesInScope = _nsManager.GetNamespacesInScope(scope);
		if (scope != XmlNamespaceScope.Local)
		{
			XmlNode xmlNode = _startNode;
			while (xmlNode != null)
			{
				switch (xmlNode.NodeType)
				{
				case XmlNodeType.Element:
				{
					XmlElement xmlElement = (XmlElement)xmlNode;
					if (xmlElement.HasAttributes)
					{
						XmlAttributeCollection attributes = xmlElement.Attributes;
						for (int i = 0; i < attributes.Count; i++)
						{
							XmlAttribute xmlAttribute = attributes[i];
							if (!Ref.Equal(xmlAttribute.NamespaceURI, _document.strReservedXmlns))
							{
								continue;
							}
							if (xmlAttribute.Prefix.Length == 0)
							{
								if (!namespacesInScope.ContainsKey(string.Empty))
								{
									namespacesInScope.Add(string.Empty, xmlAttribute.Value);
								}
							}
							else if (!namespacesInScope.ContainsKey(xmlAttribute.LocalName))
							{
								namespacesInScope.Add(xmlAttribute.LocalName, xmlAttribute.Value);
							}
						}
					}
					xmlNode = xmlNode.ParentNode;
					break;
				}
				case XmlNodeType.Attribute:
					xmlNode = ((XmlAttribute)xmlNode).OwnerElement;
					break;
				default:
					xmlNode = xmlNode.ParentNode;
					break;
				}
			}
		}
		return namespacesInScope;
	}

	public string LookupNamespace(string prefix)
	{
		string text = _nsManager.LookupNamespace(prefix);
		if (text == null)
		{
			text = _startNode.GetNamespaceOfPrefixStrict(prefix);
		}
		return text;
	}

	public string LookupPrefix(string namespaceName)
	{
		string text = _nsManager.LookupPrefix(namespaceName);
		if (text == null)
		{
			text = _startNode.GetPrefixOfNamespaceStrict(namespaceName);
		}
		return text;
	}

	private void CreateValidator(XmlSchemaObject partialValidationType, XmlSchemaValidationFlags validationFlags)
	{
		_validator = new XmlSchemaValidator(_nameTable, _schemas, NamespaceResolver, validationFlags);
		_validator.SourceUri = XmlConvert.ToUri(_document.BaseURI);
		_validator.XmlResolver = null;
		_validator.ValidationEventHandler += _internalEventHandler;
		_validator.ValidationEventSender = this;
		if (partialValidationType != null)
		{
			_validator.Initialize(partialValidationType);
		}
		else
		{
			_validator.Initialize();
		}
	}

	private void ValidateNode(XmlNode node)
	{
		_currentNode = node;
		switch (_currentNode.NodeType)
		{
		case XmlNodeType.Document:
		{
			XmlElement documentElement = ((XmlDocument)node).DocumentElement;
			if (documentElement == null)
			{
				throw new InvalidOperationException(System.SR.Format(System.SR.Xml_InvalidXmlDocument, System.SR.Xdom_NoRootEle));
			}
			ValidateNode(documentElement);
			break;
		}
		case XmlNodeType.EntityReference:
		case XmlNodeType.DocumentFragment:
		{
			for (XmlNode xmlNode = node.FirstChild; xmlNode != null; xmlNode = xmlNode.NextSibling)
			{
				ValidateNode(xmlNode);
			}
			break;
		}
		case XmlNodeType.Element:
			ValidateElement();
			break;
		case XmlNodeType.Attribute:
		{
			XmlAttribute xmlAttribute = _currentNode as XmlAttribute;
			_validator.ValidateAttribute(xmlAttribute.LocalName, xmlAttribute.NamespaceURI, _nodeValueGetter, _attributeSchemaInfo);
			if (_psviAugmentation)
			{
				xmlAttribute.XmlName = _document.AddAttrXmlName(xmlAttribute.Prefix, xmlAttribute.LocalName, xmlAttribute.NamespaceURI, _attributeSchemaInfo);
			}
			break;
		}
		case XmlNodeType.Text:
			_validator.ValidateText(_nodeValueGetter);
			break;
		case XmlNodeType.CDATA:
			_validator.ValidateText(_nodeValueGetter);
			break;
		case XmlNodeType.Whitespace:
		case XmlNodeType.SignificantWhitespace:
			_validator.ValidateWhitespace(_nodeValueGetter);
			break;
		default:
			throw new InvalidOperationException(System.SR.Format(System.SR.Xml_UnexpectedNodeType, _currentNode.NodeType));
		case XmlNodeType.ProcessingInstruction:
		case XmlNodeType.Comment:
			break;
		}
	}

	private void ValidateElement()
	{
		_nsManager.PushScope();
		XmlElement xmlElement = _currentNode as XmlElement;
		XmlAttributeCollection attributes = xmlElement.Attributes;
		XmlAttribute xmlAttribute = null;
		string xsiNil = null;
		string xsiType = null;
		for (int i = 0; i < attributes.Count; i++)
		{
			xmlAttribute = attributes[i];
			string namespaceURI = xmlAttribute.NamespaceURI;
			string localName = xmlAttribute.LocalName;
			if (Ref.Equal(namespaceURI, _nsXsi))
			{
				if (Ref.Equal(localName, _xsiType))
				{
					xsiType = xmlAttribute.Value;
				}
				else if (Ref.Equal(localName, _xsiNil))
				{
					xsiNil = xmlAttribute.Value;
				}
			}
			else if (Ref.Equal(namespaceURI, _nsXmlNs))
			{
				_nsManager.AddNamespace((xmlAttribute.Prefix.Length == 0) ? string.Empty : xmlAttribute.LocalName, xmlAttribute.Value);
			}
		}
		_validator.ValidateElement(xmlElement.LocalName, xmlElement.NamespaceURI, _schemaInfo, xsiType, xsiNil, null, null);
		ValidateAttributes(xmlElement);
		_validator.ValidateEndOfAttributes(_schemaInfo);
		for (XmlNode xmlNode = xmlElement.FirstChild; xmlNode != null; xmlNode = xmlNode.NextSibling)
		{
			ValidateNode(xmlNode);
		}
		_currentNode = xmlElement;
		_validator.ValidateEndElement(_schemaInfo);
		if (_psviAugmentation)
		{
			xmlElement.XmlName = _document.AddXmlName(xmlElement.Prefix, xmlElement.LocalName, xmlElement.NamespaceURI, _schemaInfo);
			if (_schemaInfo.IsDefault)
			{
				XmlText newChild = _document.CreateTextNode(_schemaInfo.SchemaElement.ElementDecl.DefaultValueRaw);
				xmlElement.AppendChild(newChild);
			}
		}
		_nsManager.PopScope();
	}

	private void ValidateAttributes(XmlElement elementNode)
	{
		XmlAttributeCollection attributes = elementNode.Attributes;
		XmlAttribute xmlAttribute = null;
		for (int i = 0; i < attributes.Count; i++)
		{
			xmlAttribute = (XmlAttribute)(_currentNode = attributes[i]);
			if (!Ref.Equal(xmlAttribute.NamespaceURI, _nsXmlNs))
			{
				_validator.ValidateAttribute(xmlAttribute.LocalName, xmlAttribute.NamespaceURI, _nodeValueGetter, _attributeSchemaInfo);
				if (_psviAugmentation)
				{
					xmlAttribute.XmlName = _document.AddAttrXmlName(xmlAttribute.Prefix, xmlAttribute.LocalName, xmlAttribute.NamespaceURI, _attributeSchemaInfo);
				}
			}
		}
		if (!_psviAugmentation)
		{
			return;
		}
		if (_defaultAttributes == null)
		{
			_defaultAttributes = new ArrayList();
		}
		else
		{
			_defaultAttributes.Clear();
		}
		_validator.GetUnspecifiedDefaultAttributes(_defaultAttributes);
		xmlAttribute = null;
		for (int j = 0; j < _defaultAttributes.Count; j++)
		{
			XmlSchemaAttribute xmlSchemaAttribute = _defaultAttributes[j] as XmlSchemaAttribute;
			XmlQualifiedName qualifiedName = xmlSchemaAttribute.QualifiedName;
			xmlAttribute = _document.CreateDefaultAttribute(GetDefaultPrefix(qualifiedName.Namespace), qualifiedName.Name, qualifiedName.Namespace);
			SetDefaultAttributeSchemaInfo(xmlSchemaAttribute);
			xmlAttribute.XmlName = _document.AddAttrXmlName(xmlAttribute.Prefix, xmlAttribute.LocalName, xmlAttribute.NamespaceURI, _attributeSchemaInfo);
			xmlAttribute.AppendChild(_document.CreateTextNode(xmlSchemaAttribute.AttDef.DefaultValueRaw));
			attributes.Append(xmlAttribute);
			if (xmlAttribute is XmlUnspecifiedAttribute xmlUnspecifiedAttribute)
			{
				xmlUnspecifiedAttribute.SetSpecified(f: false);
			}
		}
	}

	private void SetDefaultAttributeSchemaInfo(XmlSchemaAttribute schemaAttribute)
	{
		_attributeSchemaInfo.Clear();
		_attributeSchemaInfo.IsDefault = true;
		_attributeSchemaInfo.IsNil = false;
		_attributeSchemaInfo.SchemaType = schemaAttribute.AttributeSchemaType;
		_attributeSchemaInfo.SchemaAttribute = schemaAttribute;
		SchemaAttDef attDef = schemaAttribute.AttDef;
		if (attDef.Datatype.Variety == XmlSchemaDatatypeVariety.Union)
		{
			XsdSimpleValue xsdSimpleValue = attDef.DefaultValueTyped as XsdSimpleValue;
			_attributeSchemaInfo.MemberType = xsdSimpleValue.XmlType;
		}
		_attributeSchemaInfo.Validity = XmlSchemaValidity.Valid;
	}

	private string GetDefaultPrefix(string attributeNS)
	{
		IDictionary<string, string> namespacesInScope = NamespaceResolver.GetNamespacesInScope(XmlNamespaceScope.All);
		string text = null;
		attributeNS = _nameTable.Add(attributeNS);
		foreach (KeyValuePair<string, string> item in namespacesInScope)
		{
			string text2 = _nameTable.Add(item.Value);
			if ((object)text2 == attributeNS)
			{
				text = item.Key;
				if (text.Length != 0)
				{
					return text;
				}
			}
		}
		return text;
	}

	private object GetNodeValue()
	{
		return _currentNode.Value;
	}

	private XmlSchemaObject FindSchemaInfo(XmlElement elementToValidate)
	{
		_isPartialTreeValid = true;
		IXmlSchemaInfo xmlSchemaInfo = null;
		int num = 0;
		XmlNode parentNode = elementToValidate.ParentNode;
		do
		{
			xmlSchemaInfo = parentNode.SchemaInfo;
			if (xmlSchemaInfo.SchemaElement != null || xmlSchemaInfo.SchemaType != null)
			{
				break;
			}
			CheckNodeSequenceCapacity(num);
			_nodeSequenceToValidate[num++] = parentNode;
			parentNode = parentNode.ParentNode;
		}
		while (parentNode != null);
		if (parentNode == null)
		{
			num--;
			_nodeSequenceToValidate[num] = null;
			return GetTypeFromAncestors(elementToValidate, null, num);
		}
		CheckNodeSequenceCapacity(num);
		_nodeSequenceToValidate[num++] = parentNode;
		XmlSchemaObject xmlSchemaObject = xmlSchemaInfo.SchemaElement;
		if (xmlSchemaObject == null)
		{
			xmlSchemaObject = xmlSchemaInfo.SchemaType;
		}
		return GetTypeFromAncestors(elementToValidate, xmlSchemaObject, num);
	}

	private void CheckNodeSequenceCapacity(int currentIndex)
	{
		if (_nodeSequenceToValidate == null)
		{
			_nodeSequenceToValidate = new XmlNode[4];
		}
		else if (currentIndex >= _nodeSequenceToValidate.Length - 1)
		{
			XmlNode[] array = new XmlNode[_nodeSequenceToValidate.Length * 2];
			Array.Copy(_nodeSequenceToValidate, array, _nodeSequenceToValidate.Length);
			_nodeSequenceToValidate = array;
		}
	}

	private XmlSchemaAttribute FindSchemaInfo(XmlAttribute attributeToValidate)
	{
		XmlElement ownerElement = attributeToValidate.OwnerElement;
		XmlSchemaObject schemaObject = FindSchemaInfo(ownerElement);
		XmlSchemaComplexType complexType = GetComplexType(schemaObject);
		if (complexType == null)
		{
			return null;
		}
		XmlQualifiedName xmlQualifiedName = new XmlQualifiedName(attributeToValidate.LocalName, attributeToValidate.NamespaceURI);
		XmlSchemaAttribute xmlSchemaAttribute = complexType.AttributeUses[xmlQualifiedName] as XmlSchemaAttribute;
		if (xmlSchemaAttribute == null)
		{
			XmlSchemaAnyAttribute attributeWildcard = complexType.AttributeWildcard;
			if (attributeWildcard != null && attributeWildcard.NamespaceList.Allows(xmlQualifiedName))
			{
				xmlSchemaAttribute = _schemas.GlobalAttributes[xmlQualifiedName] as XmlSchemaAttribute;
			}
		}
		return xmlSchemaAttribute;
	}

	private XmlSchemaObject GetTypeFromAncestors(XmlElement elementToValidate, XmlSchemaObject ancestorType, int ancestorsCount)
	{
		_validator = CreateTypeFinderValidator(ancestorType);
		_schemaInfo = new XmlSchemaInfo();
		int num = ancestorsCount - 1;
		bool flag = AncestorTypeHasWildcard(ancestorType);
		for (int num2 = num; num2 >= 0; num2--)
		{
			XmlNode xmlNode = _nodeSequenceToValidate[num2];
			XmlElement xmlElement = xmlNode as XmlElement;
			ValidateSingleElement(xmlElement, skipToEnd: false, _schemaInfo);
			if (!flag)
			{
				xmlElement.XmlName = _document.AddXmlName(xmlElement.Prefix, xmlElement.LocalName, xmlElement.NamespaceURI, _schemaInfo);
				flag = AncestorTypeHasWildcard(_schemaInfo.SchemaElement);
			}
			_validator.ValidateEndOfAttributes(null);
			if (num2 > 0)
			{
				ValidateChildrenTillNextAncestor(xmlNode, _nodeSequenceToValidate[num2 - 1]);
			}
			else
			{
				ValidateChildrenTillNextAncestor(xmlNode, elementToValidate);
			}
		}
		ValidateSingleElement(elementToValidate, skipToEnd: false, _schemaInfo);
		XmlSchemaObject xmlSchemaObject = null;
		xmlSchemaObject = ((_schemaInfo.SchemaElement == null) ? ((XmlSchemaAnnotated)_schemaInfo.SchemaType) : ((XmlSchemaAnnotated)_schemaInfo.SchemaElement));
		if (xmlSchemaObject == null)
		{
			if (_validator.CurrentProcessContents == XmlSchemaContentProcessing.Skip)
			{
				if (_isPartialTreeValid)
				{
					return XmlSchemaComplexType.AnyTypeSkip;
				}
			}
			else if (_validator.CurrentProcessContents == XmlSchemaContentProcessing.Lax)
			{
				return XmlSchemaComplexType.AnyType;
			}
		}
		return xmlSchemaObject;
	}

	private bool AncestorTypeHasWildcard(XmlSchemaObject ancestorType)
	{
		XmlSchemaComplexType complexType = GetComplexType(ancestorType);
		if (ancestorType != null)
		{
			return complexType.HasWildCard;
		}
		return false;
	}

	private XmlSchemaComplexType GetComplexType(XmlSchemaObject schemaObject)
	{
		if (schemaObject == null)
		{
			return null;
		}
		XmlSchemaElement xmlSchemaElement = schemaObject as XmlSchemaElement;
		XmlSchemaComplexType xmlSchemaComplexType = null;
		if (xmlSchemaElement != null)
		{
			return xmlSchemaElement.ElementSchemaType as XmlSchemaComplexType;
		}
		return schemaObject as XmlSchemaComplexType;
	}

	private void ValidateSingleElement(XmlElement elementNode, bool skipToEnd, XmlSchemaInfo newSchemaInfo)
	{
		_nsManager.PushScope();
		XmlAttributeCollection attributes = elementNode.Attributes;
		XmlAttribute xmlAttribute = null;
		string xsiNil = null;
		string xsiType = null;
		for (int i = 0; i < attributes.Count; i++)
		{
			xmlAttribute = attributes[i];
			string namespaceURI = xmlAttribute.NamespaceURI;
			string localName = xmlAttribute.LocalName;
			if (Ref.Equal(namespaceURI, _nsXsi))
			{
				if (Ref.Equal(localName, _xsiType))
				{
					xsiType = xmlAttribute.Value;
				}
				else if (Ref.Equal(localName, _xsiNil))
				{
					xsiNil = xmlAttribute.Value;
				}
			}
			else if (Ref.Equal(namespaceURI, _nsXmlNs))
			{
				_nsManager.AddNamespace((xmlAttribute.Prefix.Length == 0) ? string.Empty : xmlAttribute.LocalName, xmlAttribute.Value);
			}
		}
		_validator.ValidateElement(elementNode.LocalName, elementNode.NamespaceURI, newSchemaInfo, xsiType, xsiNil, null, null);
		if (skipToEnd)
		{
			_validator.ValidateEndOfAttributes(newSchemaInfo);
			_validator.SkipToEndElement(newSchemaInfo);
			_nsManager.PopScope();
		}
	}

	private void ValidateChildrenTillNextAncestor(XmlNode parentNode, XmlNode childToStopAt)
	{
		XmlNode xmlNode = parentNode.FirstChild;
		while (xmlNode != null && xmlNode != childToStopAt)
		{
			switch (xmlNode.NodeType)
			{
			case XmlNodeType.EntityReference:
				ValidateChildrenTillNextAncestor(xmlNode, childToStopAt);
				break;
			case XmlNodeType.Element:
				ValidateSingleElement(xmlNode as XmlElement, skipToEnd: true, null);
				break;
			case XmlNodeType.Text:
			case XmlNodeType.CDATA:
				_validator.ValidateText(xmlNode.Value);
				break;
			case XmlNodeType.Whitespace:
			case XmlNodeType.SignificantWhitespace:
				_validator.ValidateWhitespace(xmlNode.Value);
				break;
			default:
				throw new InvalidOperationException(System.SR.Format(System.SR.Xml_UnexpectedNodeType, _currentNode.NodeType));
			case XmlNodeType.ProcessingInstruction:
			case XmlNodeType.Comment:
				break;
			}
			xmlNode = xmlNode.NextSibling;
		}
	}

	private XmlSchemaValidator CreateTypeFinderValidator(XmlSchemaObject partialValidationType)
	{
		XmlSchemaValidator xmlSchemaValidator = new XmlSchemaValidator(_document.NameTable, _document.Schemas, _nsManager, XmlSchemaValidationFlags.None);
		xmlSchemaValidator.ValidationEventHandler += TypeFinderCallBack;
		if (partialValidationType != null)
		{
			xmlSchemaValidator.Initialize(partialValidationType);
		}
		else
		{
			xmlSchemaValidator.Initialize();
		}
		return xmlSchemaValidator;
	}

	private void TypeFinderCallBack(object sender, ValidationEventArgs arg)
	{
		if (arg.Severity == XmlSeverityType.Error)
		{
			_isPartialTreeValid = false;
		}
	}

	private void InternalValidationCallBack(object sender, ValidationEventArgs arg)
	{
		if (arg.Severity == XmlSeverityType.Error)
		{
			_isValid = false;
		}
		XmlSchemaValidationException ex = arg.Exception as XmlSchemaValidationException;
		ex.SetSourceObject(_currentNode);
		if (_eventHandler != null)
		{
			_eventHandler(sender, arg);
		}
		else if (arg.Severity == XmlSeverityType.Error)
		{
			throw ex;
		}
	}
}
