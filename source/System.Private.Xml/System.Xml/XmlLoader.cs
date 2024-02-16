using System.Collections.Generic;
using System.Globalization;
using System.Xml.Schema;

namespace System.Xml;

internal sealed class XmlLoader
{
	private XmlDocument _doc;

	private XmlReader _reader;

	private bool _preserveWhitespace;

	internal void Load(XmlDocument doc, XmlReader reader, bool preserveWhitespace)
	{
		_doc = doc;
		if (reader.GetType() == typeof(XmlTextReader))
		{
			_reader = ((XmlTextReader)reader).Impl;
		}
		else
		{
			_reader = reader;
		}
		_preserveWhitespace = preserveWhitespace;
		if (doc == null)
		{
			throw new ArgumentException(System.SR.Xdom_Load_NoDocument);
		}
		if (reader == null)
		{
			throw new ArgumentException(System.SR.Xdom_Load_NoReader);
		}
		doc.SetBaseURI(reader.BaseURI);
		if (reader.Settings != null && reader.Settings.ValidationType == ValidationType.Schema)
		{
			doc.Schemas = reader.Settings.Schemas;
		}
		if (_reader.ReadState == ReadState.Interactive || _reader.Read())
		{
			LoadDocSequence(doc);
		}
	}

	private void LoadDocSequence(XmlDocument parentDoc)
	{
		XmlNode xmlNode = null;
		while ((xmlNode = LoadNode(skipOverWhitespace: true)) != null)
		{
			parentDoc.AppendChildForLoad(xmlNode, parentDoc);
			if (!_reader.Read())
			{
				break;
			}
		}
	}

	internal XmlNode ReadCurrentNode(XmlDocument doc, XmlReader reader)
	{
		_doc = doc;
		_reader = reader;
		_preserveWhitespace = true;
		if (doc == null)
		{
			throw new ArgumentException(System.SR.Xdom_Load_NoDocument);
		}
		if (reader == null)
		{
			throw new ArgumentException(System.SR.Xdom_Load_NoReader);
		}
		if (reader.ReadState == ReadState.Initial)
		{
			reader.Read();
		}
		if (reader.ReadState == ReadState.Interactive)
		{
			XmlNode xmlNode = LoadNode(skipOverWhitespace: true);
			if (xmlNode.NodeType != XmlNodeType.Attribute)
			{
				reader.Read();
			}
			return xmlNode;
		}
		return null;
	}

	private XmlNode LoadNode(bool skipOverWhitespace)
	{
		XmlReader reader = _reader;
		XmlNode xmlNode = null;
		do
		{
			XmlNode xmlNode2 = null;
			switch (reader.NodeType)
			{
			case XmlNodeType.Element:
			{
				bool isEmptyElement = reader.IsEmptyElement;
				XmlElement xmlElement2 = _doc.CreateElement(reader.Prefix, reader.LocalName, reader.NamespaceURI);
				xmlElement2.IsEmpty = isEmptyElement;
				if (reader.MoveToFirstAttribute())
				{
					XmlAttributeCollection attributes = xmlElement2.Attributes;
					do
					{
						XmlAttribute node = LoadAttributeNode();
						attributes.Append(node);
					}
					while (reader.MoveToNextAttribute());
					reader.MoveToElement();
				}
				if (!isEmptyElement)
				{
					xmlNode?.AppendChildForLoad(xmlElement2, _doc);
					xmlNode = xmlElement2;
					continue;
				}
				IXmlSchemaInfo schemaInfo = reader.SchemaInfo;
				if (schemaInfo != null)
				{
					xmlElement2.XmlName = _doc.AddXmlName(xmlElement2.Prefix, xmlElement2.LocalName, xmlElement2.NamespaceURI, schemaInfo);
				}
				xmlNode2 = xmlElement2;
				break;
			}
			case XmlNodeType.EndElement:
			{
				if (xmlNode == null)
				{
					return null;
				}
				IXmlSchemaInfo schemaInfo = reader.SchemaInfo;
				if (schemaInfo != null && xmlNode is XmlElement xmlElement)
				{
					xmlElement.XmlName = _doc.AddXmlName(xmlElement.Prefix, xmlElement.LocalName, xmlElement.NamespaceURI, schemaInfo);
				}
				if (xmlNode.ParentNode == null)
				{
					return xmlNode;
				}
				xmlNode = xmlNode.ParentNode;
				continue;
			}
			case XmlNodeType.EntityReference:
				xmlNode2 = LoadEntityReferenceNode(direct: false);
				break;
			case XmlNodeType.EndEntity:
				return null;
			case XmlNodeType.Attribute:
				xmlNode2 = LoadAttributeNode();
				break;
			case XmlNodeType.Text:
				xmlNode2 = _doc.CreateTextNode(reader.Value);
				break;
			case XmlNodeType.SignificantWhitespace:
				xmlNode2 = _doc.CreateSignificantWhitespace(reader.Value);
				break;
			case XmlNodeType.Whitespace:
				if (_preserveWhitespace)
				{
					xmlNode2 = _doc.CreateWhitespace(reader.Value);
					break;
				}
				if (xmlNode == null && !skipOverWhitespace)
				{
					return null;
				}
				continue;
			case XmlNodeType.CDATA:
				xmlNode2 = _doc.CreateCDataSection(reader.Value);
				break;
			case XmlNodeType.XmlDeclaration:
				xmlNode2 = LoadDeclarationNode();
				break;
			case XmlNodeType.ProcessingInstruction:
				xmlNode2 = _doc.CreateProcessingInstruction(reader.Name, reader.Value);
				break;
			case XmlNodeType.Comment:
				xmlNode2 = _doc.CreateComment(reader.Value);
				break;
			case XmlNodeType.DocumentType:
				xmlNode2 = LoadDocumentTypeNode();
				break;
			default:
				throw UnexpectedNodeType(reader.NodeType);
			}
			if (xmlNode != null)
			{
				xmlNode.AppendChildForLoad(xmlNode2, _doc);
				continue;
			}
			return xmlNode2;
		}
		while (reader.Read());
		if (xmlNode != null)
		{
			while (xmlNode.ParentNode != null)
			{
				xmlNode = xmlNode.ParentNode;
			}
		}
		return xmlNode;
	}

	private XmlAttribute LoadAttributeNode()
	{
		XmlReader reader = _reader;
		if (reader.IsDefault)
		{
			return LoadDefaultAttribute();
		}
		XmlAttribute xmlAttribute = _doc.CreateAttribute(reader.Prefix, reader.LocalName, reader.NamespaceURI);
		IXmlSchemaInfo schemaInfo = reader.SchemaInfo;
		if (schemaInfo != null)
		{
			xmlAttribute.XmlName = _doc.AddAttrXmlName(xmlAttribute.Prefix, xmlAttribute.LocalName, xmlAttribute.NamespaceURI, schemaInfo);
		}
		while (reader.ReadAttributeValue())
		{
			XmlNode xmlNode;
			switch (reader.NodeType)
			{
			case XmlNodeType.Text:
				xmlNode = _doc.CreateTextNode(reader.Value);
				break;
			case XmlNodeType.EntityReference:
				xmlNode = _doc.CreateEntityReference(reader.LocalName);
				if (reader.CanResolveEntity)
				{
					reader.ResolveEntity();
					LoadAttributeValue(xmlNode, direct: false);
					if (xmlNode.FirstChild == null)
					{
						xmlNode.AppendChildForLoad(_doc.CreateTextNode(string.Empty), _doc);
					}
				}
				break;
			default:
				throw UnexpectedNodeType(reader.NodeType);
			}
			xmlAttribute.AppendChildForLoad(xmlNode, _doc);
		}
		return xmlAttribute;
	}

	private XmlAttribute LoadDefaultAttribute()
	{
		XmlReader reader = _reader;
		XmlAttribute xmlAttribute = _doc.CreateDefaultAttribute(reader.Prefix, reader.LocalName, reader.NamespaceURI);
		IXmlSchemaInfo schemaInfo = reader.SchemaInfo;
		if (schemaInfo != null)
		{
			xmlAttribute.XmlName = _doc.AddAttrXmlName(xmlAttribute.Prefix, xmlAttribute.LocalName, xmlAttribute.NamespaceURI, schemaInfo);
		}
		LoadAttributeValue(xmlAttribute, direct: false);
		if (xmlAttribute is XmlUnspecifiedAttribute xmlUnspecifiedAttribute)
		{
			xmlUnspecifiedAttribute.SetSpecified(f: false);
		}
		return xmlAttribute;
	}

	private void LoadAttributeValue(XmlNode parent, bool direct)
	{
		XmlReader reader = _reader;
		while (reader.ReadAttributeValue())
		{
			XmlNode xmlNode;
			switch (reader.NodeType)
			{
			case XmlNodeType.Text:
				xmlNode = (direct ? new XmlText(reader.Value, _doc) : _doc.CreateTextNode(reader.Value));
				break;
			case XmlNodeType.EndEntity:
				return;
			case XmlNodeType.EntityReference:
				xmlNode = (direct ? new XmlEntityReference(_reader.LocalName, _doc) : _doc.CreateEntityReference(_reader.LocalName));
				if (reader.CanResolveEntity)
				{
					reader.ResolveEntity();
					LoadAttributeValue(xmlNode, direct);
					if (xmlNode.FirstChild == null)
					{
						xmlNode.AppendChildForLoad(direct ? new XmlText(string.Empty) : _doc.CreateTextNode(string.Empty), _doc);
					}
				}
				break;
			default:
				throw UnexpectedNodeType(reader.NodeType);
			}
			parent.AppendChildForLoad(xmlNode, _doc);
		}
	}

	private XmlEntityReference LoadEntityReferenceNode(bool direct)
	{
		XmlEntityReference xmlEntityReference = (direct ? new XmlEntityReference(_reader.Name, _doc) : _doc.CreateEntityReference(_reader.Name));
		if (_reader.CanResolveEntity)
		{
			_reader.ResolveEntity();
			while (_reader.Read() && _reader.NodeType != XmlNodeType.EndEntity)
			{
				XmlNode xmlNode = (direct ? LoadNodeDirect() : LoadNode(skipOverWhitespace: false));
				if (xmlNode != null)
				{
					xmlEntityReference.AppendChildForLoad(xmlNode, _doc);
				}
			}
			if (xmlEntityReference.LastChild == null)
			{
				xmlEntityReference.AppendChildForLoad(_doc.CreateTextNode(string.Empty), _doc);
			}
		}
		return xmlEntityReference;
	}

	private XmlDeclaration LoadDeclarationNode()
	{
		string version = null;
		string encoding = null;
		string standalone = null;
		while (_reader.MoveToNextAttribute())
		{
			switch (_reader.Name)
			{
			case "version":
				version = _reader.Value;
				break;
			case "encoding":
				encoding = _reader.Value;
				break;
			case "standalone":
				standalone = _reader.Value;
				break;
			}
		}
		if (version == null)
		{
			ParseXmlDeclarationValue(_reader.Value, out version, out encoding, out standalone);
		}
		return _doc.CreateXmlDeclaration(version, encoding, standalone);
	}

	private XmlDocumentType LoadDocumentTypeNode()
	{
		string publicId = null;
		string systemId = null;
		string value = _reader.Value;
		string localName = _reader.LocalName;
		while (_reader.MoveToNextAttribute())
		{
			string name = _reader.Name;
			if (!(name == "PUBLIC"))
			{
				if (name == "SYSTEM")
				{
					systemId = _reader.Value;
				}
			}
			else
			{
				publicId = _reader.Value;
			}
		}
		XmlDocumentType xmlDocumentType = _doc.CreateDocumentType(localName, publicId, systemId, value);
		IDtdInfo dtdInfo = _reader.DtdInfo;
		if (dtdInfo != null)
		{
			LoadDocumentType(dtdInfo, xmlDocumentType);
		}
		else
		{
			ParseDocumentType(xmlDocumentType);
		}
		return xmlDocumentType;
	}

	private XmlNode LoadNodeDirect()
	{
		XmlReader reader = _reader;
		XmlNode xmlNode = null;
		do
		{
			XmlNode xmlNode2 = null;
			switch (reader.NodeType)
			{
			case XmlNodeType.Element:
			{
				bool isEmptyElement = _reader.IsEmptyElement;
				XmlElement xmlElement = new XmlElement(_reader.Prefix, _reader.LocalName, _reader.NamespaceURI, _doc);
				xmlElement.IsEmpty = isEmptyElement;
				if (_reader.MoveToFirstAttribute())
				{
					XmlAttributeCollection attributes = xmlElement.Attributes;
					do
					{
						XmlAttribute node = LoadAttributeNodeDirect();
						attributes.Append(node);
					}
					while (reader.MoveToNextAttribute());
				}
				if (!isEmptyElement)
				{
					xmlNode.AppendChildForLoad(xmlElement, _doc);
					xmlNode = xmlElement;
					continue;
				}
				xmlNode2 = xmlElement;
				break;
			}
			case XmlNodeType.EndElement:
				if (xmlNode.ParentNode == null)
				{
					return xmlNode;
				}
				xmlNode = xmlNode.ParentNode;
				continue;
			case XmlNodeType.EntityReference:
				xmlNode2 = LoadEntityReferenceNode(direct: true);
				break;
			case XmlNodeType.Attribute:
				xmlNode2 = LoadAttributeNodeDirect();
				break;
			case XmlNodeType.SignificantWhitespace:
				xmlNode2 = new XmlSignificantWhitespace(_reader.Value, _doc);
				break;
			case XmlNodeType.Whitespace:
				if (_preserveWhitespace)
				{
					xmlNode2 = new XmlWhitespace(_reader.Value, _doc);
					break;
				}
				continue;
			case XmlNodeType.Text:
				xmlNode2 = new XmlText(_reader.Value, _doc);
				break;
			case XmlNodeType.CDATA:
				xmlNode2 = new XmlCDataSection(_reader.Value, _doc);
				break;
			case XmlNodeType.ProcessingInstruction:
				xmlNode2 = new XmlProcessingInstruction(_reader.Name, _reader.Value, _doc);
				break;
			case XmlNodeType.Comment:
				xmlNode2 = new XmlComment(_reader.Value, _doc);
				break;
			default:
				throw UnexpectedNodeType(_reader.NodeType);
			case XmlNodeType.EndEntity:
				continue;
			}
			if (xmlNode != null)
			{
				xmlNode.AppendChildForLoad(xmlNode2, _doc);
				continue;
			}
			return xmlNode2;
		}
		while (reader.Read());
		return null;
	}

	private XmlAttribute LoadAttributeNodeDirect()
	{
		XmlReader reader = _reader;
		if (reader.IsDefault)
		{
			XmlUnspecifiedAttribute xmlUnspecifiedAttribute = new XmlUnspecifiedAttribute(reader.Prefix, reader.LocalName, reader.NamespaceURI, _doc);
			LoadAttributeValue(xmlUnspecifiedAttribute, direct: true);
			xmlUnspecifiedAttribute.SetSpecified(f: false);
			return xmlUnspecifiedAttribute;
		}
		XmlAttribute xmlAttribute = new XmlAttribute(reader.Prefix, reader.LocalName, reader.NamespaceURI, _doc);
		LoadAttributeValue(xmlAttribute, direct: true);
		return xmlAttribute;
	}

	internal void ParseDocumentType(XmlDocumentType dtNode)
	{
		XmlDocument ownerDocument = dtNode.OwnerDocument;
		if (ownerDocument.HasSetResolver)
		{
			ParseDocumentType(dtNode, bUseResolver: true, ownerDocument.GetResolver());
		}
		else
		{
			ParseDocumentType(dtNode, bUseResolver: false, null);
		}
	}

	private void ParseDocumentType(XmlDocumentType dtNode, bool bUseResolver, XmlResolver resolver)
	{
		_doc = dtNode.OwnerDocument;
		XmlParserContext context = new XmlParserContext(null, new XmlNamespaceManager(_doc.NameTable), null, null, null, null, _doc.BaseURI, string.Empty, XmlSpace.None);
		XmlTextReaderImpl xmlTextReaderImpl = new XmlTextReaderImpl("", XmlNodeType.Element, context);
		xmlTextReaderImpl.Namespaces = dtNode.ParseWithNamespaces;
		if (bUseResolver)
		{
			xmlTextReaderImpl.XmlResolver = resolver;
		}
		IDtdParser dtdParser = DtdParser.Create();
		XmlTextReaderImpl.DtdParserProxy adapter = new XmlTextReaderImpl.DtdParserProxy(xmlTextReaderImpl);
		IDtdInfo dtdInfo = dtdParser.ParseFreeFloatingDtd(_doc.BaseURI, dtNode.Name, dtNode.PublicId, dtNode.SystemId, dtNode.InternalSubset, adapter);
		LoadDocumentType(dtdInfo, dtNode);
	}

	private void LoadDocumentType(IDtdInfo dtdInfo, XmlDocumentType dtNode)
	{
		if (!(dtdInfo is SchemaInfo schemaInfo))
		{
			throw new XmlException(System.SR.Xml_InternalError, string.Empty);
		}
		dtNode.DtdSchemaInfo = schemaInfo;
		if (schemaInfo == null)
		{
			return;
		}
		_doc.DtdSchemaInfo = schemaInfo;
		if (schemaInfo.Notations != null)
		{
			foreach (SchemaNotation value3 in schemaInfo.Notations.Values)
			{
				dtNode.Notations.SetNamedItem(new XmlNotation(value3.Name.Name, value3.Pubid, value3.SystemLiteral, _doc));
			}
		}
		if (schemaInfo.GeneralEntities != null)
		{
			foreach (SchemaEntity value4 in schemaInfo.GeneralEntities.Values)
			{
				XmlEntity xmlEntity = new XmlEntity(value4.Name.Name, value4.Text, value4.Pubid, value4.Url, value4.NData.IsEmpty ? null : value4.NData.Name, _doc);
				xmlEntity.SetBaseURI(value4.DeclaredURI);
				dtNode.Entities.SetNamedItem(xmlEntity);
			}
		}
		if (schemaInfo.ParameterEntities != null)
		{
			foreach (SchemaEntity value5 in schemaInfo.ParameterEntities.Values)
			{
				XmlEntity xmlEntity2 = new XmlEntity(value5.Name.Name, value5.Text, value5.Pubid, value5.Url, value5.NData.IsEmpty ? null : value5.NData.Name, _doc);
				xmlEntity2.SetBaseURI(value5.DeclaredURI);
				dtNode.Entities.SetNamedItem(xmlEntity2);
			}
		}
		_doc.Entities = dtNode.Entities;
		foreach (KeyValuePair<XmlQualifiedName, SchemaElementDecl> elementDecl in schemaInfo.ElementDecls)
		{
			SchemaElementDecl value = elementDecl.Value;
			if (value.AttDefs == null)
			{
				continue;
			}
			foreach (KeyValuePair<XmlQualifiedName, SchemaAttDef> attDef in value.AttDefs)
			{
				SchemaAttDef value2 = attDef.Value;
				if (value2.Datatype.TokenizedType == XmlTokenizedType.ID)
				{
					_doc.AddIdInfo(_doc.AddXmlName(value.Prefix, value.Name.Name, string.Empty, null), _doc.AddAttrXmlName(value2.Prefix, value2.Name.Name, string.Empty, null));
					break;
				}
			}
		}
	}

	private XmlParserContext GetContext(XmlNode node)
	{
		string text = null;
		XmlSpace xmlSpace = XmlSpace.None;
		XmlDocumentType documentType = _doc.DocumentType;
		string baseURI = _doc.BaseURI;
		HashSet<string> hashSet = new HashSet<string>();
		XmlNameTable nameTable = _doc.NameTable;
		XmlNamespaceManager xmlNamespaceManager = new XmlNamespaceManager(nameTable);
		bool flag = false;
		while (node != null && node != _doc)
		{
			if (node is XmlElement { HasAttributes: not false } xmlElement)
			{
				xmlNamespaceManager.PushScope();
				foreach (XmlAttribute attribute in xmlElement.Attributes)
				{
					if (attribute.Prefix == _doc.strXmlns && !hashSet.Contains(attribute.LocalName))
					{
						hashSet.Add(attribute.LocalName);
						xmlNamespaceManager.AddNamespace(attribute.LocalName, attribute.Value);
					}
					else if (!flag && attribute.Prefix.Length == 0 && attribute.LocalName == _doc.strXmlns)
					{
						xmlNamespaceManager.AddNamespace(string.Empty, attribute.Value);
						flag = true;
					}
					else if (xmlSpace == XmlSpace.None && attribute.Prefix == _doc.strXml && attribute.LocalName == _doc.strSpace)
					{
						if (attribute.Value == "default")
						{
							xmlSpace = XmlSpace.Default;
						}
						else if (attribute.Value == "preserve")
						{
							xmlSpace = XmlSpace.Preserve;
						}
					}
					else if (text == null && attribute.Prefix == _doc.strXml && attribute.LocalName == _doc.strLang)
					{
						text = attribute.Value;
					}
				}
			}
			node = node.ParentNode;
		}
		return new XmlParserContext(nameTable, xmlNamespaceManager, documentType?.Name, documentType?.PublicId, documentType?.SystemId, documentType?.InternalSubset, baseURI, text, xmlSpace);
	}

	internal XmlNamespaceManager ParsePartialContent(XmlNode parentNode, string innerxmltext, XmlNodeType nt)
	{
		_doc = parentNode.OwnerDocument;
		XmlParserContext context = GetContext(parentNode);
		_reader = CreateInnerXmlReader(innerxmltext, nt, context, _doc);
		try
		{
			_preserveWhitespace = true;
			bool isLoading = _doc.IsLoading;
			_doc.IsLoading = true;
			if (nt == XmlNodeType.Entity)
			{
				XmlNode xmlNode = null;
				while (_reader.Read() && (xmlNode = LoadNodeDirect()) != null)
				{
					parentNode.AppendChildForLoad(xmlNode, _doc);
				}
			}
			else
			{
				XmlNode xmlNode2 = null;
				while (_reader.Read() && (xmlNode2 = LoadNode(skipOverWhitespace: true)) != null)
				{
					parentNode.AppendChildForLoad(xmlNode2, _doc);
				}
			}
			_doc.IsLoading = isLoading;
		}
		finally
		{
			_reader.Close();
		}
		return context.NamespaceManager;
	}

	internal void LoadInnerXmlElement(XmlElement node, string innerxmltext)
	{
		XmlNamespaceManager mgr = ParsePartialContent(node, innerxmltext, XmlNodeType.Element);
		if (node.ChildNodes.Count > 0)
		{
			RemoveDuplicateNamespace(node, mgr, fCheckElemAttrs: false);
		}
	}

	internal void LoadInnerXmlAttribute(XmlAttribute node, string innerxmltext)
	{
		ParsePartialContent(node, innerxmltext, XmlNodeType.Attribute);
	}

	private void RemoveDuplicateNamespace(XmlElement elem, XmlNamespaceManager mgr, bool fCheckElemAttrs)
	{
		mgr.PushScope();
		XmlAttributeCollection attributes = elem.Attributes;
		int count = attributes.Count;
		if (fCheckElemAttrs && count > 0)
		{
			for (int num = count - 1; num >= 0; num--)
			{
				XmlAttribute xmlAttribute = attributes[num];
				if (xmlAttribute.Prefix == _doc.strXmlns)
				{
					string text = mgr.LookupNamespace(xmlAttribute.LocalName);
					if (text != null)
					{
						if (xmlAttribute.Value == text)
						{
							elem.Attributes.RemoveNodeAt(num);
						}
					}
					else
					{
						mgr.AddNamespace(xmlAttribute.LocalName, xmlAttribute.Value);
					}
				}
				else if (xmlAttribute.Prefix.Length == 0 && xmlAttribute.LocalName == _doc.strXmlns)
				{
					string defaultNamespace = mgr.DefaultNamespace;
					if (defaultNamespace != null)
					{
						if (xmlAttribute.Value == defaultNamespace)
						{
							elem.Attributes.RemoveNodeAt(num);
						}
					}
					else
					{
						mgr.AddNamespace(xmlAttribute.LocalName, xmlAttribute.Value);
					}
				}
			}
		}
		for (XmlNode xmlNode = elem.FirstChild; xmlNode != null; xmlNode = xmlNode.NextSibling)
		{
			if (xmlNode is XmlElement elem2)
			{
				RemoveDuplicateNamespace(elem2, mgr, fCheckElemAttrs: true);
			}
		}
		mgr.PopScope();
	}

	private string EntitizeName(string name)
	{
		return "&" + name + ";";
	}

	internal void ExpandEntity(XmlEntity ent)
	{
		ParsePartialContent(ent, EntitizeName(ent.Name), XmlNodeType.Entity);
	}

	internal void ExpandEntityReference(XmlEntityReference eref)
	{
		_doc = eref.OwnerDocument;
		bool isLoading = _doc.IsLoading;
		_doc.IsLoading = true;
		switch (eref.Name)
		{
		case "lt":
			eref.AppendChildForLoad(_doc.CreateTextNode("<"), _doc);
			_doc.IsLoading = isLoading;
			return;
		case "gt":
			eref.AppendChildForLoad(_doc.CreateTextNode(">"), _doc);
			_doc.IsLoading = isLoading;
			return;
		case "amp":
			eref.AppendChildForLoad(_doc.CreateTextNode("&"), _doc);
			_doc.IsLoading = isLoading;
			return;
		case "apos":
			eref.AppendChildForLoad(_doc.CreateTextNode("'"), _doc);
			_doc.IsLoading = isLoading;
			return;
		case "quot":
			eref.AppendChildForLoad(_doc.CreateTextNode("\""), _doc);
			_doc.IsLoading = isLoading;
			return;
		}
		XmlNamedNodeMap entities = _doc.Entities;
		foreach (XmlEntity item in entities)
		{
			if (Ref.Equal(item.Name, eref.Name))
			{
				ParsePartialContent(eref, EntitizeName(eref.Name), XmlNodeType.EntityReference);
				return;
			}
		}
		if (!_doc.ActualLoadingStatus)
		{
			eref.AppendChildForLoad(_doc.CreateTextNode(""), _doc);
			_doc.IsLoading = isLoading;
			return;
		}
		_doc.IsLoading = isLoading;
		throw new XmlException(System.SR.Xml_UndeclaredParEntity, eref.Name);
	}

	private XmlReader CreateInnerXmlReader(string xmlFragment, XmlNodeType nt, XmlParserContext context, XmlDocument doc)
	{
		XmlNodeType xmlNodeType = nt;
		if (xmlNodeType == XmlNodeType.Entity || xmlNodeType == XmlNodeType.EntityReference)
		{
			xmlNodeType = XmlNodeType.Element;
		}
		XmlTextReaderImpl xmlTextReaderImpl = new XmlTextReaderImpl(xmlFragment, xmlNodeType, context);
		xmlTextReaderImpl.XmlValidatingReaderCompatibilityMode = true;
		if (doc.HasSetResolver)
		{
			xmlTextReaderImpl.XmlResolver = doc.GetResolver();
		}
		if (!doc.ActualLoadingStatus)
		{
			xmlTextReaderImpl.DisableUndeclaredEntityCheck = true;
		}
		XmlDocumentType documentType = doc.DocumentType;
		if (documentType != null)
		{
			xmlTextReaderImpl.Namespaces = documentType.ParseWithNamespaces;
			if (documentType.DtdSchemaInfo != null)
			{
				xmlTextReaderImpl.SetDtdInfo(documentType.DtdSchemaInfo);
			}
			else
			{
				IDtdParser dtdParser = DtdParser.Create();
				XmlTextReaderImpl.DtdParserProxy adapter = new XmlTextReaderImpl.DtdParserProxy(xmlTextReaderImpl);
				IDtdInfo dtdInfo = dtdParser.ParseFreeFloatingDtd(context.BaseURI, context.DocTypeName, context.PublicId, context.SystemId, context.InternalSubset, adapter);
				documentType.DtdSchemaInfo = dtdInfo as SchemaInfo;
				xmlTextReaderImpl.SetDtdInfo(dtdInfo);
			}
		}
		if (nt == XmlNodeType.Entity || nt == XmlNodeType.EntityReference)
		{
			xmlTextReaderImpl.Read();
			xmlTextReaderImpl.ResolveEntity();
		}
		return xmlTextReaderImpl;
	}

	internal static void ParseXmlDeclarationValue(string strValue, out string version, out string encoding, out string standalone)
	{
		version = null;
		encoding = null;
		standalone = null;
		XmlTextReaderImpl xmlTextReaderImpl = new XmlTextReaderImpl(strValue, (XmlParserContext)null);
		try
		{
			xmlTextReaderImpl.Read();
			if (xmlTextReaderImpl.MoveToAttribute("version"))
			{
				version = xmlTextReaderImpl.Value;
			}
			if (xmlTextReaderImpl.MoveToAttribute("encoding"))
			{
				encoding = xmlTextReaderImpl.Value;
			}
			if (xmlTextReaderImpl.MoveToAttribute("standalone"))
			{
				standalone = xmlTextReaderImpl.Value;
			}
		}
		finally
		{
			xmlTextReaderImpl.Close();
		}
	}

	internal static Exception UnexpectedNodeType(XmlNodeType nodetype)
	{
		return new InvalidOperationException(System.SR.Format(CultureInfo.InvariantCulture, System.SR.Xml_UnexpectedNodeType, nodetype.ToString()));
	}
}
