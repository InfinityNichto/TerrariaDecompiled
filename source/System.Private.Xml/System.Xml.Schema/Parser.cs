using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;

namespace System.Xml.Schema;

internal sealed class Parser
{
	private SchemaType _schemaType;

	private readonly XmlNameTable _nameTable;

	private readonly SchemaNames _schemaNames;

	private readonly ValidationEventHandler _eventHandler;

	private XmlNamespaceManager _namespaceManager;

	private XmlReader _reader;

	private PositionInfo _positionInfo;

	private bool _isProcessNamespaces;

	private int _schemaXmlDepth;

	private int _markupDepth;

	private SchemaBuilder _builder;

	private XmlSchema _schema;

	private SchemaInfo _xdrSchema;

	private XmlResolver _xmlResolver;

	private readonly XmlDocument _dummyDocument;

	private bool _processMarkup;

	private XmlNode _parentNode;

	private XmlNamespaceManager _annotationNSManager;

	private string _xmlns;

	public XmlSchema XmlSchema => _schema;

	internal XmlResolver XmlResolver
	{
		set
		{
			_xmlResolver = value;
		}
	}

	public SchemaInfo XdrSchema => _xdrSchema;

	public Parser(SchemaType schemaType, XmlNameTable nameTable, SchemaNames schemaNames, ValidationEventHandler eventHandler)
	{
		_schemaType = schemaType;
		_nameTable = nameTable;
		_schemaNames = schemaNames;
		_eventHandler = eventHandler;
		_xmlResolver = null;
		_processMarkup = true;
		_dummyDocument = new XmlDocument();
	}

	public SchemaType Parse(XmlReader reader, string targetNamespace)
	{
		StartParsing(reader, targetNamespace);
		while (ParseReaderNode() && reader.Read())
		{
		}
		return FinishParsing();
	}

	public void StartParsing(XmlReader reader, string targetNamespace)
	{
		_reader = reader;
		_positionInfo = PositionInfo.GetPositionInfo(reader);
		_namespaceManager = reader.NamespaceManager;
		if (_namespaceManager == null)
		{
			_namespaceManager = new XmlNamespaceManager(_nameTable);
			_isProcessNamespaces = true;
		}
		else
		{
			_isProcessNamespaces = false;
		}
		while (reader.NodeType != XmlNodeType.Element && reader.Read())
		{
		}
		_markupDepth = int.MaxValue;
		_schemaXmlDepth = reader.Depth;
		SchemaType rootType = _schemaNames.SchemaTypeFromRoot(reader.LocalName, reader.NamespaceURI);
		if (!CheckSchemaRoot(rootType, out var code))
		{
			throw new XmlSchemaException(code, reader.BaseURI, _positionInfo.LineNumber, _positionInfo.LinePosition);
		}
		if (_schemaType == SchemaType.XSD)
		{
			_schema = new XmlSchema();
			_schema.BaseUri = new Uri(reader.BaseURI, UriKind.RelativeOrAbsolute);
			_builder = new XsdBuilder(reader, _namespaceManager, _schema, _nameTable, _schemaNames, _eventHandler);
		}
		else
		{
			_xdrSchema = new SchemaInfo();
			_xdrSchema.SchemaType = SchemaType.XDR;
			_builder = new XdrBuilder(reader, _namespaceManager, _xdrSchema, targetNamespace, _nameTable, _schemaNames, _eventHandler);
			((XdrBuilder)_builder).XmlResolver = _xmlResolver;
		}
	}

	private bool CheckSchemaRoot(SchemaType rootType, [NotNullWhen(false)] out string code)
	{
		code = null;
		if (_schemaType == SchemaType.None)
		{
			_schemaType = rootType;
		}
		switch (rootType)
		{
		case SchemaType.XSD:
			if (_schemaType != SchemaType.XSD)
			{
				code = System.SR.Sch_MixSchemaTypes;
				return false;
			}
			break;
		case SchemaType.XDR:
			if (_schemaType == SchemaType.XSD)
			{
				code = System.SR.Sch_XSDSchemaOnly;
				return false;
			}
			if (_schemaType != SchemaType.XDR)
			{
				code = System.SR.Sch_MixSchemaTypes;
				return false;
			}
			break;
		case SchemaType.None:
		case SchemaType.DTD:
			code = System.SR.Sch_SchemaRootExpected;
			if (_schemaType == SchemaType.XSD)
			{
				code = System.SR.Sch_XSDSchemaRootExpected;
			}
			return false;
		}
		return true;
	}

	public SchemaType FinishParsing()
	{
		return _schemaType;
	}

	public bool ParseReaderNode()
	{
		if (_reader.Depth > _markupDepth)
		{
			if (_processMarkup)
			{
				ProcessAppInfoDocMarkup(root: false);
			}
			return true;
		}
		if (_reader.NodeType == XmlNodeType.Element)
		{
			if (_builder.ProcessElement(_reader.Prefix, _reader.LocalName, _reader.NamespaceURI))
			{
				_namespaceManager.PushScope();
				if (_reader.MoveToFirstAttribute())
				{
					do
					{
						_builder.ProcessAttribute(_reader.Prefix, _reader.LocalName, _reader.NamespaceURI, _reader.Value);
						if (Ref.Equal(_reader.NamespaceURI, _schemaNames.NsXmlNs) && _isProcessNamespaces)
						{
							_namespaceManager.AddNamespace((_reader.Prefix.Length == 0) ? string.Empty : _reader.LocalName, _reader.Value);
						}
					}
					while (_reader.MoveToNextAttribute());
					_reader.MoveToElement();
				}
				_builder.StartChildren();
				if (_reader.IsEmptyElement)
				{
					_namespaceManager.PopScope();
					_builder.EndChildren();
					if (_reader.Depth == _schemaXmlDepth)
					{
						return false;
					}
				}
				else if (!_builder.IsContentParsed())
				{
					_markupDepth = _reader.Depth;
					_processMarkup = true;
					if (_annotationNSManager == null)
					{
						_annotationNSManager = new XmlNamespaceManager(_nameTable);
						_xmlns = _nameTable.Add("xmlns");
					}
					ProcessAppInfoDocMarkup(root: true);
				}
			}
			else if (!_reader.IsEmptyElement)
			{
				_markupDepth = _reader.Depth;
				_processMarkup = false;
			}
		}
		else if (_reader.NodeType == XmlNodeType.Text)
		{
			if (!XmlCharType.IsOnlyWhitespace(_reader.Value))
			{
				_builder.ProcessCData(_reader.Value);
			}
		}
		else if (_reader.NodeType == XmlNodeType.EntityReference || _reader.NodeType == XmlNodeType.SignificantWhitespace || _reader.NodeType == XmlNodeType.CDATA)
		{
			_builder.ProcessCData(_reader.Value);
		}
		else if (_reader.NodeType == XmlNodeType.EndElement)
		{
			if (_reader.Depth == _markupDepth)
			{
				if (_processMarkup)
				{
					XmlNodeList childNodes = _parentNode.ChildNodes;
					XmlNode[] array = new XmlNode[childNodes.Count];
					for (int i = 0; i < childNodes.Count; i++)
					{
						array[i] = childNodes[i];
					}
					_builder.ProcessMarkup(array);
					_namespaceManager.PopScope();
					_builder.EndChildren();
				}
				_markupDepth = int.MaxValue;
			}
			else
			{
				_namespaceManager.PopScope();
				_builder.EndChildren();
			}
			if (_reader.Depth == _schemaXmlDepth)
			{
				return false;
			}
		}
		return true;
	}

	private void ProcessAppInfoDocMarkup(bool root)
	{
		XmlNode newChild = null;
		switch (_reader.NodeType)
		{
		case XmlNodeType.Element:
			_annotationNSManager.PushScope();
			newChild = LoadElementNode(root);
			return;
		case XmlNodeType.Text:
			newChild = _dummyDocument.CreateTextNode(_reader.Value);
			break;
		case XmlNodeType.SignificantWhitespace:
			newChild = _dummyDocument.CreateSignificantWhitespace(_reader.Value);
			break;
		case XmlNodeType.CDATA:
			newChild = _dummyDocument.CreateCDataSection(_reader.Value);
			break;
		case XmlNodeType.EntityReference:
			newChild = _dummyDocument.CreateEntityReference(_reader.Name);
			break;
		case XmlNodeType.Comment:
			newChild = _dummyDocument.CreateComment(_reader.Value);
			break;
		case XmlNodeType.ProcessingInstruction:
			newChild = _dummyDocument.CreateProcessingInstruction(_reader.Name, _reader.Value);
			break;
		case XmlNodeType.EndElement:
			_annotationNSManager.PopScope();
			_parentNode = _parentNode.ParentNode;
			return;
		case XmlNodeType.Whitespace:
		case XmlNodeType.EndEntity:
			return;
		}
		_parentNode.AppendChild(newChild);
	}

	private XmlElement LoadElementNode(bool root)
	{
		XmlReader reader = _reader;
		bool isEmptyElement = reader.IsEmptyElement;
		XmlElement xmlElement = _dummyDocument.CreateElement(reader.Prefix, reader.LocalName, reader.NamespaceURI);
		xmlElement.IsEmpty = isEmptyElement;
		if (root)
		{
			_parentNode = xmlElement;
		}
		else
		{
			XmlAttributeCollection attributes = xmlElement.Attributes;
			if (reader.MoveToFirstAttribute())
			{
				do
				{
					if (Ref.Equal(reader.NamespaceURI, _schemaNames.NsXmlNs))
					{
						_annotationNSManager.AddNamespace((reader.Prefix.Length == 0) ? string.Empty : _reader.LocalName, _reader.Value);
					}
					XmlAttribute node = LoadAttributeNode();
					attributes.Append(node);
				}
				while (reader.MoveToNextAttribute());
			}
			reader.MoveToElement();
			string text = _annotationNSManager.LookupNamespace(reader.Prefix);
			if (text == null)
			{
				XmlAttribute node2 = CreateXmlNsAttribute(reader.Prefix, _namespaceManager.LookupNamespace(reader.Prefix));
				attributes.Append(node2);
			}
			else if (text.Length == 0)
			{
				string text2 = _namespaceManager.LookupNamespace(reader.Prefix);
				if (text2 != string.Empty)
				{
					XmlAttribute node3 = CreateXmlNsAttribute(reader.Prefix, text2);
					attributes.Append(node3);
				}
			}
			while (reader.MoveToNextAttribute())
			{
				if (reader.Prefix.Length != 0)
				{
					string text3 = _annotationNSManager.LookupNamespace(reader.Prefix);
					if (text3 == null)
					{
						XmlAttribute node4 = CreateXmlNsAttribute(reader.Prefix, _namespaceManager.LookupNamespace(reader.Prefix));
						attributes.Append(node4);
					}
				}
			}
			reader.MoveToElement();
			_parentNode.AppendChild(xmlElement);
			if (!reader.IsEmptyElement)
			{
				_parentNode = xmlElement;
			}
		}
		return xmlElement;
	}

	private XmlAttribute CreateXmlNsAttribute(string prefix, string value)
	{
		XmlAttribute xmlAttribute = ((prefix.Length != 0) ? _dummyDocument.CreateAttribute(_xmlns, prefix, "http://www.w3.org/2000/xmlns/") : _dummyDocument.CreateAttribute(string.Empty, _xmlns, "http://www.w3.org/2000/xmlns/"));
		xmlAttribute.AppendChild(_dummyDocument.CreateTextNode(value));
		_annotationNSManager.AddNamespace(prefix, value);
		return xmlAttribute;
	}

	private XmlAttribute LoadAttributeNode()
	{
		XmlReader reader = _reader;
		XmlAttribute xmlAttribute = _dummyDocument.CreateAttribute(reader.Prefix, reader.LocalName, reader.NamespaceURI);
		while (reader.ReadAttributeValue())
		{
			switch (reader.NodeType)
			{
			case XmlNodeType.Text:
				xmlAttribute.AppendChild(_dummyDocument.CreateTextNode(reader.Value));
				break;
			case XmlNodeType.EntityReference:
				xmlAttribute.AppendChild(LoadEntityReferenceInAttribute());
				break;
			default:
				throw XmlLoader.UnexpectedNodeType(reader.NodeType);
			}
		}
		return xmlAttribute;
	}

	private XmlEntityReference LoadEntityReferenceInAttribute()
	{
		XmlEntityReference xmlEntityReference = _dummyDocument.CreateEntityReference(_reader.LocalName);
		if (!_reader.CanResolveEntity)
		{
			return xmlEntityReference;
		}
		_reader.ResolveEntity();
		while (_reader.ReadAttributeValue())
		{
			switch (_reader.NodeType)
			{
			case XmlNodeType.Text:
				xmlEntityReference.AppendChild(_dummyDocument.CreateTextNode(_reader.Value));
				break;
			case XmlNodeType.EndEntity:
				if (xmlEntityReference.ChildNodes.Count == 0)
				{
					xmlEntityReference.AppendChild(_dummyDocument.CreateTextNode(string.Empty));
				}
				return xmlEntityReference;
			case XmlNodeType.EntityReference:
				xmlEntityReference.AppendChild(LoadEntityReferenceInAttribute());
				break;
			default:
				throw XmlLoader.UnexpectedNodeType(_reader.NodeType);
			}
		}
		return xmlEntityReference;
	}

	public async Task StartParsingAsync(XmlReader reader, string targetNamespace)
	{
		_reader = reader;
		_positionInfo = PositionInfo.GetPositionInfo(reader);
		_namespaceManager = reader.NamespaceManager;
		if (_namespaceManager == null)
		{
			_namespaceManager = new XmlNamespaceManager(_nameTable);
			_isProcessNamespaces = true;
		}
		else
		{
			_isProcessNamespaces = false;
		}
		bool flag2;
		do
		{
			bool flag = reader.NodeType != XmlNodeType.Element;
			flag2 = flag;
			if (flag2)
			{
				flag2 = await reader.ReadAsync().ConfigureAwait(continueOnCapturedContext: false);
			}
		}
		while (flag2);
		_markupDepth = int.MaxValue;
		_schemaXmlDepth = reader.Depth;
		SchemaType rootType = _schemaNames.SchemaTypeFromRoot(reader.LocalName, reader.NamespaceURI);
		if (!CheckSchemaRoot(rootType, out var code))
		{
			throw new XmlSchemaException(code, reader.BaseURI, _positionInfo.LineNumber, _positionInfo.LinePosition);
		}
		if (_schemaType == SchemaType.XSD)
		{
			_schema = new XmlSchema();
			_schema.BaseUri = new Uri(reader.BaseURI, UriKind.RelativeOrAbsolute);
			_builder = new XsdBuilder(reader, _namespaceManager, _schema, _nameTable, _schemaNames, _eventHandler);
		}
		else
		{
			_xdrSchema = new SchemaInfo();
			_xdrSchema.SchemaType = SchemaType.XDR;
			_builder = new XdrBuilder(reader, _namespaceManager, _xdrSchema, targetNamespace, _nameTable, _schemaNames, _eventHandler);
			((XdrBuilder)_builder).XmlResolver = _xmlResolver;
		}
	}
}
