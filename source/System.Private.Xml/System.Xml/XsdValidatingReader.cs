using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using System.Xml.Schema;

namespace System.Xml;

internal sealed class XsdValidatingReader : XmlReader, IXmlSchemaInfo, IXmlLineInfo, IXmlNamespaceResolver
{
	private enum ValidatingReaderState
	{
		None = 0,
		Init = 1,
		Read = 2,
		OnDefaultAttribute = -1,
		OnReadAttributeValue = -2,
		OnAttribute = 3,
		ClearAttributes = 4,
		ParseInlineSchema = 5,
		ReadAhead = 6,
		OnReadBinaryContent = 7,
		ReaderClosed = 8,
		EOF = 9,
		Error = 10
	}

	private XmlReader _coreReader;

	private readonly IXmlNamespaceResolver _coreReaderNSResolver;

	private readonly IXmlNamespaceResolver _thisNSResolver;

	private XmlSchemaValidator _validator;

	private readonly XmlResolver _xmlResolver;

	private readonly ValidationEventHandler _validationEvent;

	private ValidatingReaderState _validationState;

	private XmlValueGetter _valueGetter;

	private readonly XmlNamespaceManager _nsManager;

	private readonly bool _manageNamespaces;

	private readonly bool _processInlineSchema;

	private bool _replayCache;

	private ValidatingReaderNodeData _cachedNode;

	private AttributePSVIInfo _attributePSVI;

	private int _attributeCount;

	private int _coreReaderAttributeCount;

	private int _currentAttrIndex;

	private AttributePSVIInfo[] _attributePSVINodes;

	private ArrayList _defaultAttributes;

	private Parser _inlineSchemaParser;

	private object _atomicValue;

	private XmlSchemaInfo _xmlSchemaInfo;

	private string _originalAtomicValueString;

	private readonly XmlNameTable _coreReaderNameTable;

	private XsdCachingReader _cachingReader;

	private ValidatingReaderNodeData _textNode;

	private string _nsXmlNs;

	private string _nsXs;

	private string _nsXsi;

	private string _xsiType;

	private string _xsiNil;

	private string _xsdSchema;

	private string _xsiSchemaLocation;

	private string _xsiNoNamespaceSchemaLocation;

	private IXmlLineInfo _lineInfo;

	private ReadContentAsBinaryHelper _readBinaryHelper;

	private ValidatingReaderState _savedState;

	private static volatile Type s_typeOfString;

	public override XmlReaderSettings Settings
	{
		get
		{
			XmlReaderSettings settings = _coreReader.Settings;
			settings = ((settings != null) ? settings.Clone() : new XmlReaderSettings());
			settings.Schemas = _validator.SchemaSet;
			settings.ValidationType = ValidationType.Schema;
			settings.ValidationFlags = _validator.ValidationFlags;
			settings.ReadOnly = true;
			return settings;
		}
	}

	public override XmlNodeType NodeType
	{
		get
		{
			if (_validationState < ValidatingReaderState.None)
			{
				return _cachedNode.NodeType;
			}
			XmlNodeType nodeType = _coreReader.NodeType;
			if (nodeType == XmlNodeType.Whitespace && (_validator.CurrentContentType == XmlSchemaContentType.TextOnly || _validator.CurrentContentType == XmlSchemaContentType.Mixed))
			{
				return XmlNodeType.SignificantWhitespace;
			}
			return nodeType;
		}
	}

	public override string Name
	{
		get
		{
			if (_validationState == ValidatingReaderState.OnDefaultAttribute)
			{
				string defaultAttributePrefix = _validator.GetDefaultAttributePrefix(_cachedNode.Namespace);
				if (defaultAttributePrefix != null && defaultAttributePrefix.Length != 0)
				{
					return defaultAttributePrefix + ":" + _cachedNode.LocalName;
				}
				return _cachedNode.LocalName;
			}
			return _coreReader.Name;
		}
	}

	public override string LocalName
	{
		get
		{
			if (_validationState < ValidatingReaderState.None)
			{
				return _cachedNode.LocalName;
			}
			return _coreReader.LocalName;
		}
	}

	public override string NamespaceURI
	{
		get
		{
			if (_validationState < ValidatingReaderState.None)
			{
				return _cachedNode.Namespace;
			}
			return _coreReader.NamespaceURI;
		}
	}

	public override string Prefix
	{
		get
		{
			if (_validationState < ValidatingReaderState.None)
			{
				return _cachedNode.Prefix;
			}
			return _coreReader.Prefix;
		}
	}

	public override bool HasValue
	{
		get
		{
			if (_validationState < ValidatingReaderState.None)
			{
				return true;
			}
			return _coreReader.HasValue;
		}
	}

	public override string Value
	{
		get
		{
			if (_validationState < ValidatingReaderState.None)
			{
				return _cachedNode.RawValue;
			}
			return _coreReader.Value;
		}
	}

	public override int Depth
	{
		get
		{
			if (_validationState < ValidatingReaderState.None)
			{
				return _cachedNode.Depth;
			}
			return _coreReader.Depth;
		}
	}

	public override string BaseURI => _coreReader.BaseURI;

	public override bool IsEmptyElement => _coreReader.IsEmptyElement;

	public override bool IsDefault
	{
		get
		{
			if (_validationState == ValidatingReaderState.OnDefaultAttribute)
			{
				return true;
			}
			return _coreReader.IsDefault;
		}
	}

	public override char QuoteChar => _coreReader.QuoteChar;

	public override XmlSpace XmlSpace => _coreReader.XmlSpace;

	public override string XmlLang => _coreReader.XmlLang;

	public override IXmlSchemaInfo SchemaInfo => this;

	public override Type ValueType
	{
		get
		{
			switch (NodeType)
			{
			case XmlNodeType.Element:
			case XmlNodeType.EndElement:
				if (_xmlSchemaInfo.ContentType == XmlSchemaContentType.TextOnly)
				{
					return _xmlSchemaInfo.SchemaType.Datatype.ValueType;
				}
				break;
			case XmlNodeType.Attribute:
				if (_attributePSVI != null && AttributeSchemaInfo.ContentType == XmlSchemaContentType.TextOnly)
				{
					return AttributeSchemaInfo.SchemaType.Datatype.ValueType;
				}
				break;
			}
			return s_typeOfString;
		}
	}

	public override int AttributeCount => _attributeCount;

	public override bool EOF => _coreReader.EOF;

	public override ReadState ReadState
	{
		get
		{
			if (_validationState != ValidatingReaderState.Init)
			{
				return _coreReader.ReadState;
			}
			return ReadState.Initial;
		}
	}

	public override XmlNameTable NameTable => _coreReaderNameTable;

	public override bool CanReadBinaryContent => true;

	bool IXmlSchemaInfo.IsDefault
	{
		get
		{
			switch (NodeType)
			{
			case XmlNodeType.Element:
				if (!_coreReader.IsEmptyElement)
				{
					GetIsDefault();
				}
				return _xmlSchemaInfo.IsDefault;
			case XmlNodeType.EndElement:
				return _xmlSchemaInfo.IsDefault;
			case XmlNodeType.Attribute:
				if (_attributePSVI != null)
				{
					return AttributeSchemaInfo.IsDefault;
				}
				break;
			}
			return false;
		}
	}

	bool IXmlSchemaInfo.IsNil
	{
		get
		{
			XmlNodeType nodeType = NodeType;
			if (nodeType == XmlNodeType.Element || nodeType == XmlNodeType.EndElement)
			{
				return _xmlSchemaInfo.IsNil;
			}
			return false;
		}
	}

	XmlSchemaValidity IXmlSchemaInfo.Validity
	{
		get
		{
			switch (NodeType)
			{
			case XmlNodeType.Element:
				if (_coreReader.IsEmptyElement)
				{
					return _xmlSchemaInfo.Validity;
				}
				if (_xmlSchemaInfo.Validity == XmlSchemaValidity.Valid)
				{
					return XmlSchemaValidity.NotKnown;
				}
				return _xmlSchemaInfo.Validity;
			case XmlNodeType.EndElement:
				return _xmlSchemaInfo.Validity;
			case XmlNodeType.Attribute:
				if (_attributePSVI != null)
				{
					return AttributeSchemaInfo.Validity;
				}
				break;
			}
			return XmlSchemaValidity.NotKnown;
		}
	}

	XmlSchemaSimpleType IXmlSchemaInfo.MemberType
	{
		get
		{
			switch (NodeType)
			{
			case XmlNodeType.Element:
				if (!_coreReader.IsEmptyElement)
				{
					GetMemberType();
				}
				return _xmlSchemaInfo.MemberType;
			case XmlNodeType.EndElement:
				return _xmlSchemaInfo.MemberType;
			case XmlNodeType.Attribute:
				if (_attributePSVI != null)
				{
					return AttributeSchemaInfo.MemberType;
				}
				return null;
			default:
				return null;
			}
		}
	}

	XmlSchemaType IXmlSchemaInfo.SchemaType
	{
		get
		{
			switch (NodeType)
			{
			case XmlNodeType.Element:
			case XmlNodeType.EndElement:
				return _xmlSchemaInfo.SchemaType;
			case XmlNodeType.Attribute:
				if (_attributePSVI != null)
				{
					return AttributeSchemaInfo.SchemaType;
				}
				return null;
			default:
				return null;
			}
		}
	}

	XmlSchemaElement IXmlSchemaInfo.SchemaElement
	{
		get
		{
			if (NodeType == XmlNodeType.Element || NodeType == XmlNodeType.EndElement)
			{
				return _xmlSchemaInfo.SchemaElement;
			}
			return null;
		}
	}

	XmlSchemaAttribute IXmlSchemaInfo.SchemaAttribute
	{
		get
		{
			if (NodeType == XmlNodeType.Attribute && _attributePSVI != null)
			{
				return AttributeSchemaInfo.SchemaAttribute;
			}
			return null;
		}
	}

	public int LineNumber
	{
		get
		{
			if (_lineInfo != null)
			{
				return _lineInfo.LineNumber;
			}
			return 0;
		}
	}

	public int LinePosition
	{
		get
		{
			if (_lineInfo != null)
			{
				return _lineInfo.LinePosition;
			}
			return 0;
		}
	}

	private XmlSchemaType ElementXmlType => _xmlSchemaInfo.XmlType;

	private XmlSchemaType AttributeXmlType
	{
		get
		{
			if (_attributePSVI != null)
			{
				return AttributeSchemaInfo.XmlType;
			}
			return null;
		}
	}

	private XmlSchemaInfo AttributeSchemaInfo => _attributePSVI.attributeSchemaInfo;

	internal XsdValidatingReader(XmlReader reader, XmlResolver xmlResolver, XmlReaderSettings readerSettings, XmlSchemaObject partialValidationType)
	{
		_coreReader = reader;
		_coreReaderNSResolver = reader as IXmlNamespaceResolver;
		_lineInfo = reader as IXmlLineInfo;
		_coreReaderNameTable = _coreReader.NameTable;
		if (_coreReaderNSResolver == null)
		{
			_nsManager = new XmlNamespaceManager(_coreReaderNameTable);
			_manageNamespaces = true;
		}
		_thisNSResolver = this;
		_xmlResolver = xmlResolver;
		_processInlineSchema = (readerSettings.ValidationFlags & XmlSchemaValidationFlags.ProcessInlineSchema) != 0;
		_validationState = ValidatingReaderState.Init;
		_defaultAttributes = new ArrayList();
		_currentAttrIndex = -1;
		_attributePSVINodes = new AttributePSVIInfo[8];
		_valueGetter = GetStringValue;
		s_typeOfString = typeof(string);
		_xmlSchemaInfo = new XmlSchemaInfo();
		_nsXmlNs = _coreReaderNameTable.Add("http://www.w3.org/2000/xmlns/");
		_nsXs = _coreReaderNameTable.Add("http://www.w3.org/2001/XMLSchema");
		_nsXsi = _coreReaderNameTable.Add("http://www.w3.org/2001/XMLSchema-instance");
		_xsiType = _coreReaderNameTable.Add("type");
		_xsiNil = _coreReaderNameTable.Add("nil");
		_xsiSchemaLocation = _coreReaderNameTable.Add("schemaLocation");
		_xsiNoNamespaceSchemaLocation = _coreReaderNameTable.Add("noNamespaceSchemaLocation");
		_xsdSchema = _coreReaderNameTable.Add("schema");
		SetupValidator(readerSettings, reader, partialValidationType);
		_validationEvent = readerSettings.GetEventHandler();
	}

	internal XsdValidatingReader(XmlReader reader, XmlResolver xmlResolver, XmlReaderSettings readerSettings)
		: this(reader, xmlResolver, readerSettings, null)
	{
	}

	[MemberNotNull("_validator")]
	private void SetupValidator(XmlReaderSettings readerSettings, XmlReader reader, XmlSchemaObject partialValidationType)
	{
		_validator = new XmlSchemaValidator(_coreReaderNameTable, readerSettings.Schemas, _thisNSResolver, readerSettings.ValidationFlags);
		_validator.XmlResolver = _xmlResolver;
		_validator.SourceUri = XmlConvert.ToUri(reader.BaseURI);
		_validator.ValidationEventSender = this;
		_validator.ValidationEventHandler += readerSettings.GetEventHandler();
		_validator.LineInfoProvider = _lineInfo;
		if (_validator.ProcessSchemaHints)
		{
			_validator.SchemaSet.ReaderSettings.DtdProcessing = readerSettings.DtdProcessing;
		}
		_validator.SetDtdSchemaInfo(reader.DtdInfo);
		if (partialValidationType != null)
		{
			_validator.Initialize(partialValidationType);
		}
		else
		{
			_validator.Initialize();
		}
	}

	public override object ReadContentAsObject()
	{
		if (!XmlReader.CanReadContentAs(NodeType))
		{
			throw CreateReadContentAsException("ReadContentAsObject");
		}
		return InternalReadContentAsObject(unwrapTypedValue: true);
	}

	public override bool ReadContentAsBoolean()
	{
		if (!XmlReader.CanReadContentAs(NodeType))
		{
			throw CreateReadContentAsException("ReadContentAsBoolean");
		}
		object value = InternalReadContentAsObject();
		XmlSchemaType xmlSchemaType = ((NodeType == XmlNodeType.Attribute) ? AttributeXmlType : ElementXmlType);
		try
		{
			return xmlSchemaType?.ValueConverter.ToBoolean(value) ?? XmlUntypedConverter.Untyped.ToBoolean(value);
		}
		catch (InvalidCastException innerException)
		{
			throw new XmlException(System.SR.Xml_ReadContentAsFormatException, "Boolean", innerException, this);
		}
		catch (FormatException innerException2)
		{
			throw new XmlException(System.SR.Xml_ReadContentAsFormatException, "Boolean", innerException2, this);
		}
		catch (OverflowException innerException3)
		{
			throw new XmlException(System.SR.Xml_ReadContentAsFormatException, "Boolean", innerException3, this);
		}
	}

	public override DateTime ReadContentAsDateTime()
	{
		if (!XmlReader.CanReadContentAs(NodeType))
		{
			throw CreateReadContentAsException("ReadContentAsDateTime");
		}
		object value = InternalReadContentAsObject();
		XmlSchemaType xmlSchemaType = ((NodeType == XmlNodeType.Attribute) ? AttributeXmlType : ElementXmlType);
		try
		{
			return xmlSchemaType?.ValueConverter.ToDateTime(value) ?? XmlUntypedConverter.Untyped.ToDateTime(value);
		}
		catch (InvalidCastException innerException)
		{
			throw new XmlException(System.SR.Xml_ReadContentAsFormatException, "DateTime", innerException, this);
		}
		catch (FormatException innerException2)
		{
			throw new XmlException(System.SR.Xml_ReadContentAsFormatException, "DateTime", innerException2, this);
		}
		catch (OverflowException innerException3)
		{
			throw new XmlException(System.SR.Xml_ReadContentAsFormatException, "DateTime", innerException3, this);
		}
	}

	public override double ReadContentAsDouble()
	{
		if (!XmlReader.CanReadContentAs(NodeType))
		{
			throw CreateReadContentAsException("ReadContentAsDouble");
		}
		object value = InternalReadContentAsObject();
		XmlSchemaType xmlSchemaType = ((NodeType == XmlNodeType.Attribute) ? AttributeXmlType : ElementXmlType);
		try
		{
			return xmlSchemaType?.ValueConverter.ToDouble(value) ?? XmlUntypedConverter.Untyped.ToDouble(value);
		}
		catch (InvalidCastException innerException)
		{
			throw new XmlException(System.SR.Xml_ReadContentAsFormatException, "Double", innerException, this);
		}
		catch (FormatException innerException2)
		{
			throw new XmlException(System.SR.Xml_ReadContentAsFormatException, "Double", innerException2, this);
		}
		catch (OverflowException innerException3)
		{
			throw new XmlException(System.SR.Xml_ReadContentAsFormatException, "Double", innerException3, this);
		}
	}

	public override float ReadContentAsFloat()
	{
		if (!XmlReader.CanReadContentAs(NodeType))
		{
			throw CreateReadContentAsException("ReadContentAsFloat");
		}
		object value = InternalReadContentAsObject();
		XmlSchemaType xmlSchemaType = ((NodeType == XmlNodeType.Attribute) ? AttributeXmlType : ElementXmlType);
		try
		{
			return xmlSchemaType?.ValueConverter.ToSingle(value) ?? XmlUntypedConverter.Untyped.ToSingle(value);
		}
		catch (InvalidCastException innerException)
		{
			throw new XmlException(System.SR.Xml_ReadContentAsFormatException, "Float", innerException, this);
		}
		catch (FormatException innerException2)
		{
			throw new XmlException(System.SR.Xml_ReadContentAsFormatException, "Float", innerException2, this);
		}
		catch (OverflowException innerException3)
		{
			throw new XmlException(System.SR.Xml_ReadContentAsFormatException, "Float", innerException3, this);
		}
	}

	public override decimal ReadContentAsDecimal()
	{
		if (!XmlReader.CanReadContentAs(NodeType))
		{
			throw CreateReadContentAsException("ReadContentAsDecimal");
		}
		object value = InternalReadContentAsObject();
		XmlSchemaType xmlSchemaType = ((NodeType == XmlNodeType.Attribute) ? AttributeXmlType : ElementXmlType);
		try
		{
			return xmlSchemaType?.ValueConverter.ToDecimal(value) ?? XmlUntypedConverter.Untyped.ToDecimal(value);
		}
		catch (InvalidCastException innerException)
		{
			throw new XmlException(System.SR.Xml_ReadContentAsFormatException, "Decimal", innerException, this);
		}
		catch (FormatException innerException2)
		{
			throw new XmlException(System.SR.Xml_ReadContentAsFormatException, "Decimal", innerException2, this);
		}
		catch (OverflowException innerException3)
		{
			throw new XmlException(System.SR.Xml_ReadContentAsFormatException, "Decimal", innerException3, this);
		}
	}

	public override int ReadContentAsInt()
	{
		if (!XmlReader.CanReadContentAs(NodeType))
		{
			throw CreateReadContentAsException("ReadContentAsInt");
		}
		object value = InternalReadContentAsObject();
		XmlSchemaType xmlSchemaType = ((NodeType == XmlNodeType.Attribute) ? AttributeXmlType : ElementXmlType);
		try
		{
			return xmlSchemaType?.ValueConverter.ToInt32(value) ?? XmlUntypedConverter.Untyped.ToInt32(value);
		}
		catch (InvalidCastException innerException)
		{
			throw new XmlException(System.SR.Xml_ReadContentAsFormatException, "Int", innerException, this);
		}
		catch (FormatException innerException2)
		{
			throw new XmlException(System.SR.Xml_ReadContentAsFormatException, "Int", innerException2, this);
		}
		catch (OverflowException innerException3)
		{
			throw new XmlException(System.SR.Xml_ReadContentAsFormatException, "Int", innerException3, this);
		}
	}

	public override long ReadContentAsLong()
	{
		if (!XmlReader.CanReadContentAs(NodeType))
		{
			throw CreateReadContentAsException("ReadContentAsLong");
		}
		object value = InternalReadContentAsObject();
		XmlSchemaType xmlSchemaType = ((NodeType == XmlNodeType.Attribute) ? AttributeXmlType : ElementXmlType);
		try
		{
			return xmlSchemaType?.ValueConverter.ToInt64(value) ?? XmlUntypedConverter.Untyped.ToInt64(value);
		}
		catch (InvalidCastException innerException)
		{
			throw new XmlException(System.SR.Xml_ReadContentAsFormatException, "Long", innerException, this);
		}
		catch (FormatException innerException2)
		{
			throw new XmlException(System.SR.Xml_ReadContentAsFormatException, "Long", innerException2, this);
		}
		catch (OverflowException innerException3)
		{
			throw new XmlException(System.SR.Xml_ReadContentAsFormatException, "Long", innerException3, this);
		}
	}

	public override string ReadContentAsString()
	{
		if (!XmlReader.CanReadContentAs(NodeType))
		{
			throw CreateReadContentAsException("ReadContentAsString");
		}
		object obj = InternalReadContentAsObject();
		XmlSchemaType xmlSchemaType = ((NodeType == XmlNodeType.Attribute) ? AttributeXmlType : ElementXmlType);
		try
		{
			if (xmlSchemaType != null)
			{
				return xmlSchemaType.ValueConverter.ToString(obj);
			}
			return obj as string;
		}
		catch (InvalidCastException innerException)
		{
			throw new XmlException(System.SR.Xml_ReadContentAsFormatException, "String", innerException, this);
		}
		catch (FormatException innerException2)
		{
			throw new XmlException(System.SR.Xml_ReadContentAsFormatException, "String", innerException2, this);
		}
		catch (OverflowException innerException3)
		{
			throw new XmlException(System.SR.Xml_ReadContentAsFormatException, "String", innerException3, this);
		}
	}

	public override object ReadContentAs(Type returnType, IXmlNamespaceResolver namespaceResolver)
	{
		if (!XmlReader.CanReadContentAs(NodeType))
		{
			throw CreateReadContentAsException("ReadContentAs");
		}
		string originalStringValue;
		object value = InternalReadContentAsObject(unwrapTypedValue: false, out originalStringValue);
		XmlSchemaType xmlSchemaType = ((NodeType == XmlNodeType.Attribute) ? AttributeXmlType : ElementXmlType);
		try
		{
			if (xmlSchemaType != null)
			{
				if (returnType == typeof(DateTimeOffset) && xmlSchemaType.Datatype is Datatype_dateTimeBase)
				{
					value = originalStringValue;
				}
				return xmlSchemaType.ValueConverter.ChangeType(value, returnType);
			}
			return XmlUntypedConverter.Untyped.ChangeType(value, returnType, namespaceResolver);
		}
		catch (FormatException innerException)
		{
			throw new XmlException(System.SR.Xml_ReadContentAsFormatException, returnType.ToString(), innerException, this);
		}
		catch (InvalidCastException innerException2)
		{
			throw new XmlException(System.SR.Xml_ReadContentAsFormatException, returnType.ToString(), innerException2, this);
		}
		catch (OverflowException innerException3)
		{
			throw new XmlException(System.SR.Xml_ReadContentAsFormatException, returnType.ToString(), innerException3, this);
		}
	}

	public override object ReadElementContentAsObject()
	{
		if (NodeType != XmlNodeType.Element)
		{
			throw CreateReadElementContentAsException("ReadElementContentAsObject");
		}
		XmlSchemaType xmlType;
		return InternalReadElementContentAsObject(out xmlType, unwrapTypedValue: true);
	}

	public override bool ReadElementContentAsBoolean()
	{
		if (NodeType != XmlNodeType.Element)
		{
			throw CreateReadElementContentAsException("ReadElementContentAsBoolean");
		}
		XmlSchemaType xmlType;
		object value = InternalReadElementContentAsObject(out xmlType);
		try
		{
			return xmlType?.ValueConverter.ToBoolean(value) ?? XmlUntypedConverter.Untyped.ToBoolean(value);
		}
		catch (FormatException innerException)
		{
			throw new XmlException(System.SR.Xml_ReadContentAsFormatException, "Boolean", innerException, this);
		}
		catch (InvalidCastException innerException2)
		{
			throw new XmlException(System.SR.Xml_ReadContentAsFormatException, "Boolean", innerException2, this);
		}
		catch (OverflowException innerException3)
		{
			throw new XmlException(System.SR.Xml_ReadContentAsFormatException, "Boolean", innerException3, this);
		}
	}

	public override DateTime ReadElementContentAsDateTime()
	{
		if (NodeType != XmlNodeType.Element)
		{
			throw CreateReadElementContentAsException("ReadElementContentAsDateTime");
		}
		XmlSchemaType xmlType;
		object value = InternalReadElementContentAsObject(out xmlType);
		try
		{
			return xmlType?.ValueConverter.ToDateTime(value) ?? XmlUntypedConverter.Untyped.ToDateTime(value);
		}
		catch (FormatException innerException)
		{
			throw new XmlException(System.SR.Xml_ReadContentAsFormatException, "DateTime", innerException, this);
		}
		catch (InvalidCastException innerException2)
		{
			throw new XmlException(System.SR.Xml_ReadContentAsFormatException, "DateTime", innerException2, this);
		}
		catch (OverflowException innerException3)
		{
			throw new XmlException(System.SR.Xml_ReadContentAsFormatException, "DateTime", innerException3, this);
		}
	}

	public override double ReadElementContentAsDouble()
	{
		if (NodeType != XmlNodeType.Element)
		{
			throw CreateReadElementContentAsException("ReadElementContentAsDouble");
		}
		XmlSchemaType xmlType;
		object value = InternalReadElementContentAsObject(out xmlType);
		try
		{
			return xmlType?.ValueConverter.ToDouble(value) ?? XmlUntypedConverter.Untyped.ToDouble(value);
		}
		catch (FormatException innerException)
		{
			throw new XmlException(System.SR.Xml_ReadContentAsFormatException, "Double", innerException, this);
		}
		catch (InvalidCastException innerException2)
		{
			throw new XmlException(System.SR.Xml_ReadContentAsFormatException, "Double", innerException2, this);
		}
		catch (OverflowException innerException3)
		{
			throw new XmlException(System.SR.Xml_ReadContentAsFormatException, "Double", innerException3, this);
		}
	}

	public override float ReadElementContentAsFloat()
	{
		if (NodeType != XmlNodeType.Element)
		{
			throw CreateReadElementContentAsException("ReadElementContentAsFloat");
		}
		XmlSchemaType xmlType;
		object value = InternalReadElementContentAsObject(out xmlType);
		try
		{
			return xmlType?.ValueConverter.ToSingle(value) ?? XmlUntypedConverter.Untyped.ToSingle(value);
		}
		catch (FormatException innerException)
		{
			throw new XmlException(System.SR.Xml_ReadContentAsFormatException, "Float", innerException, this);
		}
		catch (InvalidCastException innerException2)
		{
			throw new XmlException(System.SR.Xml_ReadContentAsFormatException, "Float", innerException2, this);
		}
		catch (OverflowException innerException3)
		{
			throw new XmlException(System.SR.Xml_ReadContentAsFormatException, "Float", innerException3, this);
		}
	}

	public override decimal ReadElementContentAsDecimal()
	{
		if (NodeType != XmlNodeType.Element)
		{
			throw CreateReadElementContentAsException("ReadElementContentAsDecimal");
		}
		XmlSchemaType xmlType;
		object value = InternalReadElementContentAsObject(out xmlType);
		try
		{
			return xmlType?.ValueConverter.ToDecimal(value) ?? XmlUntypedConverter.Untyped.ToDecimal(value);
		}
		catch (FormatException innerException)
		{
			throw new XmlException(System.SR.Xml_ReadContentAsFormatException, "Decimal", innerException, this);
		}
		catch (InvalidCastException innerException2)
		{
			throw new XmlException(System.SR.Xml_ReadContentAsFormatException, "Decimal", innerException2, this);
		}
		catch (OverflowException innerException3)
		{
			throw new XmlException(System.SR.Xml_ReadContentAsFormatException, "Decimal", innerException3, this);
		}
	}

	public override int ReadElementContentAsInt()
	{
		if (NodeType != XmlNodeType.Element)
		{
			throw CreateReadElementContentAsException("ReadElementContentAsInt");
		}
		XmlSchemaType xmlType;
		object value = InternalReadElementContentAsObject(out xmlType);
		try
		{
			return xmlType?.ValueConverter.ToInt32(value) ?? XmlUntypedConverter.Untyped.ToInt32(value);
		}
		catch (FormatException innerException)
		{
			throw new XmlException(System.SR.Xml_ReadContentAsFormatException, "Int", innerException, this);
		}
		catch (InvalidCastException innerException2)
		{
			throw new XmlException(System.SR.Xml_ReadContentAsFormatException, "Int", innerException2, this);
		}
		catch (OverflowException innerException3)
		{
			throw new XmlException(System.SR.Xml_ReadContentAsFormatException, "Int", innerException3, this);
		}
	}

	public override long ReadElementContentAsLong()
	{
		if (NodeType != XmlNodeType.Element)
		{
			throw CreateReadElementContentAsException("ReadElementContentAsLong");
		}
		XmlSchemaType xmlType;
		object value = InternalReadElementContentAsObject(out xmlType);
		try
		{
			return xmlType?.ValueConverter.ToInt64(value) ?? XmlUntypedConverter.Untyped.ToInt64(value);
		}
		catch (FormatException innerException)
		{
			throw new XmlException(System.SR.Xml_ReadContentAsFormatException, "Long", innerException, this);
		}
		catch (InvalidCastException innerException2)
		{
			throw new XmlException(System.SR.Xml_ReadContentAsFormatException, "Long", innerException2, this);
		}
		catch (OverflowException innerException3)
		{
			throw new XmlException(System.SR.Xml_ReadContentAsFormatException, "Long", innerException3, this);
		}
	}

	public override string ReadElementContentAsString()
	{
		if (NodeType != XmlNodeType.Element)
		{
			throw CreateReadElementContentAsException("ReadElementContentAsString");
		}
		XmlSchemaType xmlType;
		object obj = InternalReadElementContentAsObject(out xmlType);
		try
		{
			if (xmlType != null && obj != null)
			{
				return xmlType.ValueConverter.ToString(obj);
			}
			return (obj as string) ?? string.Empty;
		}
		catch (InvalidCastException innerException)
		{
			throw new XmlException(System.SR.Xml_ReadContentAsFormatException, "String", innerException, this);
		}
		catch (FormatException innerException2)
		{
			throw new XmlException(System.SR.Xml_ReadContentAsFormatException, "String", innerException2, this);
		}
		catch (OverflowException innerException3)
		{
			throw new XmlException(System.SR.Xml_ReadContentAsFormatException, "String", innerException3, this);
		}
	}

	public override object ReadElementContentAs(Type returnType, IXmlNamespaceResolver namespaceResolver)
	{
		if (NodeType != XmlNodeType.Element)
		{
			throw CreateReadElementContentAsException("ReadElementContentAs");
		}
		XmlSchemaType xmlType;
		string originalString;
		object value = InternalReadElementContentAsObject(out xmlType, unwrapTypedValue: false, out originalString);
		try
		{
			if (xmlType != null)
			{
				if (returnType == typeof(DateTimeOffset) && xmlType.Datatype is Datatype_dateTimeBase)
				{
					value = originalString;
				}
				return xmlType.ValueConverter.ChangeType(value, returnType, namespaceResolver);
			}
			return XmlUntypedConverter.Untyped.ChangeType(value, returnType, namespaceResolver);
		}
		catch (FormatException innerException)
		{
			throw new XmlException(System.SR.Xml_ReadContentAsFormatException, returnType.ToString(), innerException, this);
		}
		catch (InvalidCastException innerException2)
		{
			throw new XmlException(System.SR.Xml_ReadContentAsFormatException, returnType.ToString(), innerException2, this);
		}
		catch (OverflowException innerException3)
		{
			throw new XmlException(System.SR.Xml_ReadContentAsFormatException, returnType.ToString(), innerException3, this);
		}
	}

	public override string GetAttribute(string name)
	{
		string text = _coreReader.GetAttribute(name);
		if (text == null && _attributeCount > 0)
		{
			ValidatingReaderNodeData defaultAttribute = GetDefaultAttribute(name, updatePosition: false);
			if (defaultAttribute != null)
			{
				text = defaultAttribute.RawValue;
			}
		}
		return text;
	}

	public override string GetAttribute(string name, string namespaceURI)
	{
		string attribute = _coreReader.GetAttribute(name, namespaceURI);
		if (attribute == null && _attributeCount > 0)
		{
			string text = ((namespaceURI == null) ? string.Empty : _coreReaderNameTable.Get(namespaceURI));
			string text2 = _coreReaderNameTable.Get(name);
			if (text2 == null || text == null)
			{
				return null;
			}
			ValidatingReaderNodeData defaultAttribute = GetDefaultAttribute(text2, text, updatePosition: false);
			if (defaultAttribute != null)
			{
				return defaultAttribute.RawValue;
			}
		}
		return attribute;
	}

	public override string GetAttribute(int i)
	{
		if (i < 0 || i >= _attributeCount)
		{
			throw new ArgumentOutOfRangeException("i");
		}
		if (i < _coreReaderAttributeCount)
		{
			return _coreReader.GetAttribute(i);
		}
		int index = i - _coreReaderAttributeCount;
		ValidatingReaderNodeData validatingReaderNodeData = (ValidatingReaderNodeData)_defaultAttributes[index];
		return validatingReaderNodeData.RawValue;
	}

	public override bool MoveToAttribute(string name)
	{
		if (_coreReader.MoveToAttribute(name))
		{
			_validationState = ValidatingReaderState.OnAttribute;
			_attributePSVI = GetAttributePSVI(name);
			goto IL_0057;
		}
		if (_attributeCount > 0)
		{
			ValidatingReaderNodeData defaultAttribute = GetDefaultAttribute(name, updatePosition: true);
			if (defaultAttribute != null)
			{
				_validationState = ValidatingReaderState.OnDefaultAttribute;
				_attributePSVI = defaultAttribute.AttInfo;
				_cachedNode = defaultAttribute;
				goto IL_0057;
			}
		}
		return false;
		IL_0057:
		if (_validationState == ValidatingReaderState.OnReadBinaryContent)
		{
			_readBinaryHelper.Finish();
			_validationState = _savedState;
		}
		return true;
	}

	public override bool MoveToAttribute(string name, string ns)
	{
		string text = _coreReaderNameTable.Get(name);
		ns = ((ns != null) ? _coreReaderNameTable.Get(ns) : string.Empty);
		if (text == null || ns == null)
		{
			return false;
		}
		if (_coreReader.MoveToAttribute(text, ns))
		{
			_validationState = ValidatingReaderState.OnAttribute;
			if (_inlineSchemaParser == null)
			{
				_attributePSVI = GetAttributePSVI(text, ns);
			}
			else
			{
				_attributePSVI = null;
			}
		}
		else
		{
			ValidatingReaderNodeData defaultAttribute = GetDefaultAttribute(text, ns, updatePosition: true);
			if (defaultAttribute == null)
			{
				return false;
			}
			_attributePSVI = defaultAttribute.AttInfo;
			_cachedNode = defaultAttribute;
			_validationState = ValidatingReaderState.OnDefaultAttribute;
		}
		if (_validationState == ValidatingReaderState.OnReadBinaryContent)
		{
			_readBinaryHelper.Finish();
			_validationState = _savedState;
		}
		return true;
	}

	public override void MoveToAttribute(int i)
	{
		if (i < 0 || i >= _attributeCount)
		{
			throw new ArgumentOutOfRangeException("i");
		}
		_currentAttrIndex = i;
		if (i < _coreReaderAttributeCount)
		{
			_coreReader.MoveToAttribute(i);
			if (_inlineSchemaParser == null)
			{
				_attributePSVI = _attributePSVINodes[i];
			}
			else
			{
				_attributePSVI = null;
			}
			_validationState = ValidatingReaderState.OnAttribute;
		}
		else
		{
			int index = i - _coreReaderAttributeCount;
			_cachedNode = (ValidatingReaderNodeData)_defaultAttributes[index];
			_attributePSVI = _cachedNode.AttInfo;
			_validationState = ValidatingReaderState.OnDefaultAttribute;
		}
		if (_validationState == ValidatingReaderState.OnReadBinaryContent)
		{
			_readBinaryHelper.Finish();
			_validationState = _savedState;
		}
	}

	public override bool MoveToFirstAttribute()
	{
		if (_coreReader.MoveToFirstAttribute())
		{
			_currentAttrIndex = 0;
			if (_inlineSchemaParser == null)
			{
				_attributePSVI = _attributePSVINodes[0];
			}
			else
			{
				_attributePSVI = null;
			}
			_validationState = ValidatingReaderState.OnAttribute;
		}
		else
		{
			if (_defaultAttributes.Count <= 0)
			{
				return false;
			}
			_cachedNode = (ValidatingReaderNodeData)_defaultAttributes[0];
			_attributePSVI = _cachedNode.AttInfo;
			_currentAttrIndex = 0;
			_validationState = ValidatingReaderState.OnDefaultAttribute;
		}
		if (_validationState == ValidatingReaderState.OnReadBinaryContent)
		{
			_readBinaryHelper.Finish();
			_validationState = _savedState;
		}
		return true;
	}

	public override bool MoveToNextAttribute()
	{
		if (_currentAttrIndex + 1 < _coreReaderAttributeCount)
		{
			bool flag = _coreReader.MoveToNextAttribute();
			_currentAttrIndex++;
			if (_inlineSchemaParser == null)
			{
				_attributePSVI = _attributePSVINodes[_currentAttrIndex];
			}
			else
			{
				_attributePSVI = null;
			}
			_validationState = ValidatingReaderState.OnAttribute;
		}
		else
		{
			if (_currentAttrIndex + 1 >= _attributeCount)
			{
				return false;
			}
			int index = ++_currentAttrIndex - _coreReaderAttributeCount;
			_cachedNode = (ValidatingReaderNodeData)_defaultAttributes[index];
			_attributePSVI = _cachedNode.AttInfo;
			_validationState = ValidatingReaderState.OnDefaultAttribute;
		}
		if (_validationState == ValidatingReaderState.OnReadBinaryContent)
		{
			_readBinaryHelper.Finish();
			_validationState = _savedState;
		}
		return true;
	}

	public override bool MoveToElement()
	{
		if (_coreReader.MoveToElement() || _validationState < ValidatingReaderState.None)
		{
			_currentAttrIndex = -1;
			_validationState = ValidatingReaderState.ClearAttributes;
			return true;
		}
		return false;
	}

	public override bool Read()
	{
		switch (_validationState)
		{
		case ValidatingReaderState.Read:
			if (_coreReader.Read())
			{
				ProcessReaderEvent();
				return true;
			}
			_validator.EndValidation();
			if (_coreReader.EOF)
			{
				_validationState = ValidatingReaderState.EOF;
			}
			return false;
		case ValidatingReaderState.ParseInlineSchema:
			ProcessInlineSchema();
			return true;
		case ValidatingReaderState.OnReadAttributeValue:
		case ValidatingReaderState.OnDefaultAttribute:
		case ValidatingReaderState.OnAttribute:
		case ValidatingReaderState.ClearAttributes:
			ClearAttributesInfo();
			if (_inlineSchemaParser != null)
			{
				_validationState = ValidatingReaderState.ParseInlineSchema;
				goto case ValidatingReaderState.ParseInlineSchema;
			}
			_validationState = ValidatingReaderState.Read;
			goto case ValidatingReaderState.Read;
		case ValidatingReaderState.ReadAhead:
			ClearAttributesInfo();
			ProcessReaderEvent();
			_validationState = ValidatingReaderState.Read;
			return true;
		case ValidatingReaderState.OnReadBinaryContent:
			_validationState = _savedState;
			_readBinaryHelper.Finish();
			return Read();
		case ValidatingReaderState.Init:
			_validationState = ValidatingReaderState.Read;
			if (_coreReader.ReadState == ReadState.Interactive)
			{
				ProcessReaderEvent();
				return true;
			}
			goto case ValidatingReaderState.Read;
		case ValidatingReaderState.ReaderClosed:
		case ValidatingReaderState.EOF:
			return false;
		default:
			return false;
		}
	}

	public override void Close()
	{
		_coreReader.Close();
		_validationState = ValidatingReaderState.ReaderClosed;
	}

	public override void Skip()
	{
		XmlNodeType nodeType = NodeType;
		if (nodeType != XmlNodeType.Element)
		{
			if (nodeType != XmlNodeType.Attribute)
			{
				goto IL_007a;
			}
			MoveToElement();
		}
		if (!_coreReader.IsEmptyElement)
		{
			bool flag = true;
			if ((_xmlSchemaInfo.IsUnionType || _xmlSchemaInfo.IsDefault) && _coreReader is XsdCachingReader)
			{
				flag = false;
			}
			_coreReader.Skip();
			_validationState = ValidatingReaderState.ReadAhead;
			if (flag)
			{
				_validator.SkipToEndElement(_xmlSchemaInfo);
			}
		}
		goto IL_007a;
		IL_007a:
		Read();
	}

	public override string LookupNamespace(string prefix)
	{
		return _thisNSResolver.LookupNamespace(prefix);
	}

	public override void ResolveEntity()
	{
		throw new InvalidOperationException();
	}

	public override bool ReadAttributeValue()
	{
		if (_validationState == ValidatingReaderState.OnReadBinaryContent)
		{
			_readBinaryHelper.Finish();
			_validationState = _savedState;
		}
		if (NodeType == XmlNodeType.Attribute)
		{
			if (_validationState == ValidatingReaderState.OnDefaultAttribute)
			{
				_cachedNode = CreateDummyTextNode(_cachedNode.RawValue, _cachedNode.Depth + 1);
				_validationState = ValidatingReaderState.OnReadAttributeValue;
				return true;
			}
			return _coreReader.ReadAttributeValue();
		}
		return false;
	}

	public override int ReadContentAsBase64(byte[] buffer, int index, int count)
	{
		if (ReadState != ReadState.Interactive)
		{
			return 0;
		}
		if (_validationState != ValidatingReaderState.OnReadBinaryContent)
		{
			_readBinaryHelper = ReadContentAsBinaryHelper.CreateOrReset(_readBinaryHelper, this);
			_savedState = _validationState;
		}
		_validationState = _savedState;
		int result = _readBinaryHelper.ReadContentAsBase64(buffer, index, count);
		_savedState = _validationState;
		_validationState = ValidatingReaderState.OnReadBinaryContent;
		return result;
	}

	public override int ReadContentAsBinHex(byte[] buffer, int index, int count)
	{
		if (ReadState != ReadState.Interactive)
		{
			return 0;
		}
		if (_validationState != ValidatingReaderState.OnReadBinaryContent)
		{
			_readBinaryHelper = ReadContentAsBinaryHelper.CreateOrReset(_readBinaryHelper, this);
			_savedState = _validationState;
		}
		_validationState = _savedState;
		int result = _readBinaryHelper.ReadContentAsBinHex(buffer, index, count);
		_savedState = _validationState;
		_validationState = ValidatingReaderState.OnReadBinaryContent;
		return result;
	}

	public override int ReadElementContentAsBase64(byte[] buffer, int index, int count)
	{
		if (ReadState != ReadState.Interactive)
		{
			return 0;
		}
		if (_validationState != ValidatingReaderState.OnReadBinaryContent)
		{
			_readBinaryHelper = ReadContentAsBinaryHelper.CreateOrReset(_readBinaryHelper, this);
			_savedState = _validationState;
		}
		_validationState = _savedState;
		int result = _readBinaryHelper.ReadElementContentAsBase64(buffer, index, count);
		_savedState = _validationState;
		_validationState = ValidatingReaderState.OnReadBinaryContent;
		return result;
	}

	public override int ReadElementContentAsBinHex(byte[] buffer, int index, int count)
	{
		if (ReadState != ReadState.Interactive)
		{
			return 0;
		}
		if (_validationState != ValidatingReaderState.OnReadBinaryContent)
		{
			_readBinaryHelper = ReadContentAsBinaryHelper.CreateOrReset(_readBinaryHelper, this);
			_savedState = _validationState;
		}
		_validationState = _savedState;
		int result = _readBinaryHelper.ReadElementContentAsBinHex(buffer, index, count);
		_savedState = _validationState;
		_validationState = ValidatingReaderState.OnReadBinaryContent;
		return result;
	}

	public bool HasLineInfo()
	{
		return true;
	}

	IDictionary<string, string> IXmlNamespaceResolver.GetNamespacesInScope(XmlNamespaceScope scope)
	{
		if (_coreReaderNSResolver != null)
		{
			return _coreReaderNSResolver.GetNamespacesInScope(scope);
		}
		return _nsManager.GetNamespacesInScope(scope);
	}

	string IXmlNamespaceResolver.LookupNamespace(string prefix)
	{
		if (_coreReaderNSResolver != null)
		{
			return _coreReaderNSResolver.LookupNamespace(prefix);
		}
		return _nsManager.LookupNamespace(prefix);
	}

	string IXmlNamespaceResolver.LookupPrefix(string namespaceName)
	{
		if (_coreReaderNSResolver != null)
		{
			return _coreReaderNSResolver.LookupPrefix(namespaceName);
		}
		return _nsManager.LookupPrefix(namespaceName);
	}

	private object GetStringValue()
	{
		return _coreReader.Value;
	}

	private void ProcessReaderEvent()
	{
		if (!_replayCache)
		{
			switch (_coreReader.NodeType)
			{
			case XmlNodeType.Element:
				ProcessElementEvent();
				break;
			case XmlNodeType.Whitespace:
			case XmlNodeType.SignificantWhitespace:
				_validator.ValidateWhitespace(_valueGetter);
				break;
			case XmlNodeType.Text:
			case XmlNodeType.CDATA:
				_validator.ValidateText(_valueGetter);
				break;
			case XmlNodeType.EndElement:
				ProcessEndElementEvent();
				break;
			case XmlNodeType.EntityReference:
				throw new InvalidOperationException();
			case XmlNodeType.DocumentType:
				_validator.SetDtdSchemaInfo(_coreReader.DtdInfo);
				break;
			case XmlNodeType.Attribute:
			case XmlNodeType.Entity:
			case XmlNodeType.ProcessingInstruction:
			case XmlNodeType.Comment:
			case XmlNodeType.Document:
			case XmlNodeType.DocumentFragment:
			case XmlNodeType.Notation:
				break;
			}
		}
	}

	private void ProcessElementEvent()
	{
		if (_processInlineSchema && IsXSDRoot(_coreReader.LocalName, _coreReader.NamespaceURI) && _coreReader.Depth > 0)
		{
			_xmlSchemaInfo.Clear();
			_attributeCount = (_coreReaderAttributeCount = _coreReader.AttributeCount);
			if (!_coreReader.IsEmptyElement)
			{
				_inlineSchemaParser = new Parser(SchemaType.XSD, _coreReaderNameTable, _validator.SchemaSet.GetSchemaNames(_coreReaderNameTable), _validationEvent);
				_inlineSchemaParser.StartParsing(_coreReader, null);
				_inlineSchemaParser.ParseReaderNode();
				_validationState = ValidatingReaderState.ParseInlineSchema;
			}
			else
			{
				_validationState = ValidatingReaderState.ClearAttributes;
			}
			return;
		}
		_atomicValue = null;
		_originalAtomicValueString = null;
		_xmlSchemaInfo.Clear();
		if (_manageNamespaces)
		{
			_nsManager.PushScope();
		}
		string xsiSchemaLocation = null;
		string xsiNoNamespaceSchemaLocation = null;
		string xsiNil = null;
		string xsiType = null;
		if (_coreReader.MoveToFirstAttribute())
		{
			do
			{
				string namespaceURI = _coreReader.NamespaceURI;
				string localName = _coreReader.LocalName;
				if (Ref.Equal(namespaceURI, _nsXsi))
				{
					if (Ref.Equal(localName, _xsiSchemaLocation))
					{
						xsiSchemaLocation = _coreReader.Value;
					}
					else if (Ref.Equal(localName, _xsiNoNamespaceSchemaLocation))
					{
						xsiNoNamespaceSchemaLocation = _coreReader.Value;
					}
					else if (Ref.Equal(localName, _xsiType))
					{
						xsiType = _coreReader.Value;
					}
					else if (Ref.Equal(localName, _xsiNil))
					{
						xsiNil = _coreReader.Value;
					}
				}
				if (_manageNamespaces && Ref.Equal(_coreReader.NamespaceURI, _nsXmlNs))
				{
					_nsManager.AddNamespace((_coreReader.Prefix.Length == 0) ? string.Empty : _coreReader.LocalName, _coreReader.Value);
				}
			}
			while (_coreReader.MoveToNextAttribute());
			_coreReader.MoveToElement();
		}
		_validator.ValidateElement(_coreReader.LocalName, _coreReader.NamespaceURI, _xmlSchemaInfo, xsiType, xsiNil, xsiSchemaLocation, xsiNoNamespaceSchemaLocation);
		ValidateAttributes();
		_validator.ValidateEndOfAttributes(_xmlSchemaInfo);
		if (_coreReader.IsEmptyElement)
		{
			ProcessEndElementEvent();
		}
		_validationState = ValidatingReaderState.ClearAttributes;
	}

	private void ProcessEndElementEvent()
	{
		_atomicValue = _validator.ValidateEndElement(_xmlSchemaInfo);
		_originalAtomicValueString = GetOriginalAtomicValueStringOfElement();
		if (_xmlSchemaInfo.IsDefault)
		{
			int depth = _coreReader.Depth;
			_coreReader = GetCachingReader();
			_cachingReader.RecordTextNode(_xmlSchemaInfo.XmlType.ValueConverter.ToString(_atomicValue), _originalAtomicValueString, depth + 1, 0, 0);
			_cachingReader.RecordEndElementNode();
			_cachingReader.SetToReplayMode();
			_replayCache = true;
		}
		else if (_manageNamespaces)
		{
			_nsManager.PopScope();
		}
	}

	private void ValidateAttributes()
	{
		_attributeCount = (_coreReaderAttributeCount = _coreReader.AttributeCount);
		int num = 0;
		bool flag = false;
		if (_coreReader.MoveToFirstAttribute())
		{
			do
			{
				string localName = _coreReader.LocalName;
				string namespaceURI = _coreReader.NamespaceURI;
				AttributePSVIInfo attributePSVIInfo = AddAttributePSVI(num);
				attributePSVIInfo.localName = localName;
				attributePSVIInfo.namespaceUri = namespaceURI;
				if ((object)namespaceURI == _nsXmlNs)
				{
					num++;
					continue;
				}
				attributePSVIInfo.typedAttributeValue = _validator.ValidateAttribute(localName, namespaceURI, _valueGetter, attributePSVIInfo.attributeSchemaInfo);
				if (!flag)
				{
					flag = attributePSVIInfo.attributeSchemaInfo.Validity == XmlSchemaValidity.Invalid;
				}
				num++;
			}
			while (_coreReader.MoveToNextAttribute());
		}
		_coreReader.MoveToElement();
		if (flag)
		{
			_xmlSchemaInfo.Validity = XmlSchemaValidity.Invalid;
		}
		_validator.GetUnspecifiedDefaultAttributes(_defaultAttributes, createNodeData: true);
		_attributeCount += _defaultAttributes.Count;
	}

	private void ClearAttributesInfo()
	{
		_attributeCount = 0;
		_coreReaderAttributeCount = 0;
		_currentAttrIndex = -1;
		_defaultAttributes.Clear();
		_attributePSVI = null;
	}

	private AttributePSVIInfo GetAttributePSVI(string name)
	{
		if (_inlineSchemaParser != null)
		{
			return null;
		}
		ValidateNames.SplitQName(name, out var prefix, out var lname);
		prefix = _coreReaderNameTable.Add(prefix);
		lname = _coreReaderNameTable.Add(lname);
		string ns = ((prefix.Length != 0) ? _thisNSResolver.LookupNamespace(prefix) : string.Empty);
		return GetAttributePSVI(lname, ns);
	}

	private AttributePSVIInfo GetAttributePSVI(string localName, string ns)
	{
		AttributePSVIInfo attributePSVIInfo = null;
		for (int i = 0; i < _coreReaderAttributeCount; i++)
		{
			attributePSVIInfo = _attributePSVINodes[i];
			if (attributePSVIInfo != null && Ref.Equal(localName, attributePSVIInfo.localName) && Ref.Equal(ns, attributePSVIInfo.namespaceUri))
			{
				_currentAttrIndex = i;
				return attributePSVIInfo;
			}
		}
		return null;
	}

	private ValidatingReaderNodeData GetDefaultAttribute(string name, bool updatePosition)
	{
		ValidateNames.SplitQName(name, out var prefix, out var lname);
		prefix = _coreReaderNameTable.Add(prefix);
		lname = _coreReaderNameTable.Add(lname);
		string ns = ((prefix.Length != 0) ? _thisNSResolver.LookupNamespace(prefix) : string.Empty);
		return GetDefaultAttribute(lname, ns, updatePosition);
	}

	private ValidatingReaderNodeData GetDefaultAttribute(string attrLocalName, string ns, bool updatePosition)
	{
		ValidatingReaderNodeData validatingReaderNodeData = null;
		for (int i = 0; i < _defaultAttributes.Count; i++)
		{
			validatingReaderNodeData = (ValidatingReaderNodeData)_defaultAttributes[i];
			if (Ref.Equal(validatingReaderNodeData.LocalName, attrLocalName) && Ref.Equal(validatingReaderNodeData.Namespace, ns))
			{
				if (updatePosition)
				{
					_currentAttrIndex = _coreReader.AttributeCount + i;
				}
				return validatingReaderNodeData;
			}
		}
		return null;
	}

	private AttributePSVIInfo AddAttributePSVI(int attIndex)
	{
		AttributePSVIInfo attributePSVIInfo = _attributePSVINodes[attIndex];
		if (attributePSVIInfo != null)
		{
			attributePSVIInfo.Reset();
			return attributePSVIInfo;
		}
		if (attIndex >= _attributePSVINodes.Length - 1)
		{
			AttributePSVIInfo[] array = new AttributePSVIInfo[_attributePSVINodes.Length * 2];
			Array.Copy(_attributePSVINodes, array, _attributePSVINodes.Length);
			_attributePSVINodes = array;
		}
		attributePSVIInfo = _attributePSVINodes[attIndex];
		if (attributePSVIInfo == null)
		{
			attributePSVIInfo = new AttributePSVIInfo();
			_attributePSVINodes[attIndex] = attributePSVIInfo;
		}
		return attributePSVIInfo;
	}

	private bool IsXSDRoot(string localName, string ns)
	{
		if (Ref.Equal(ns, _nsXs))
		{
			return Ref.Equal(localName, _xsdSchema);
		}
		return false;
	}

	private void ProcessInlineSchema()
	{
		if (_coreReader.Read())
		{
			if (_coreReader.NodeType == XmlNodeType.Element)
			{
				_attributeCount = (_coreReaderAttributeCount = _coreReader.AttributeCount);
			}
			else
			{
				ClearAttributesInfo();
			}
			if (!_inlineSchemaParser.ParseReaderNode())
			{
				_inlineSchemaParser.FinishParsing();
				XmlSchema xmlSchema = _inlineSchemaParser.XmlSchema;
				_validator.AddSchema(xmlSchema);
				_inlineSchemaParser = null;
				_validationState = ValidatingReaderState.Read;
			}
		}
	}

	private object InternalReadContentAsObject()
	{
		return InternalReadContentAsObject(unwrapTypedValue: false);
	}

	private object InternalReadContentAsObject(bool unwrapTypedValue)
	{
		string originalStringValue;
		return InternalReadContentAsObject(unwrapTypedValue, out originalStringValue);
	}

	private object InternalReadContentAsObject(bool unwrapTypedValue, out string originalStringValue)
	{
		switch (NodeType)
		{
		case XmlNodeType.Attribute:
			originalStringValue = Value;
			if (_attributePSVI != null && _attributePSVI.typedAttributeValue != null)
			{
				if (_validationState == ValidatingReaderState.OnDefaultAttribute)
				{
					XmlSchemaAttribute schemaAttribute = _attributePSVI.attributeSchemaInfo.SchemaAttribute;
					originalStringValue = ((schemaAttribute.DefaultValue != null) ? schemaAttribute.DefaultValue : schemaAttribute.FixedValue);
				}
				return ReturnBoxedValue(_attributePSVI.typedAttributeValue, AttributeSchemaInfo.XmlType, unwrapTypedValue);
			}
			return Value;
		case XmlNodeType.EndElement:
			if (_atomicValue != null)
			{
				originalStringValue = _originalAtomicValueString;
				return _atomicValue;
			}
			originalStringValue = string.Empty;
			return string.Empty;
		default:
			if (_validator.CurrentContentType == XmlSchemaContentType.TextOnly)
			{
				object result = ReturnBoxedValue(ReadTillEndElement(), _xmlSchemaInfo.XmlType, unwrapTypedValue);
				originalStringValue = _originalAtomicValueString;
				return result;
			}
			if (_coreReader is XsdCachingReader xsdCachingReader)
			{
				originalStringValue = xsdCachingReader.ReadOriginalContentAsString();
			}
			else
			{
				originalStringValue = InternalReadContentAsString();
			}
			return originalStringValue;
		}
	}

	private object InternalReadElementContentAsObject(out XmlSchemaType xmlType)
	{
		return InternalReadElementContentAsObject(out xmlType, unwrapTypedValue: false);
	}

	private object InternalReadElementContentAsObject(out XmlSchemaType xmlType, bool unwrapTypedValue)
	{
		string originalString;
		return InternalReadElementContentAsObject(out xmlType, unwrapTypedValue, out originalString);
	}

	private object InternalReadElementContentAsObject(out XmlSchemaType xmlType, bool unwrapTypedValue, out string originalString)
	{
		object obj = null;
		xmlType = null;
		if (IsEmptyElement)
		{
			obj = ((_xmlSchemaInfo.ContentType != 0) ? _atomicValue : ReturnBoxedValue(_atomicValue, _xmlSchemaInfo.XmlType, unwrapTypedValue));
			originalString = _originalAtomicValueString;
			xmlType = ElementXmlType;
			Read();
			return obj;
		}
		Read();
		if (NodeType == XmlNodeType.EndElement)
		{
			if (_xmlSchemaInfo.IsDefault)
			{
				obj = ((_xmlSchemaInfo.ContentType != 0) ? _atomicValue : ReturnBoxedValue(_atomicValue, _xmlSchemaInfo.XmlType, unwrapTypedValue));
				originalString = _originalAtomicValueString;
			}
			else
			{
				obj = string.Empty;
				originalString = string.Empty;
			}
		}
		else
		{
			if (NodeType == XmlNodeType.Element)
			{
				throw new XmlException(System.SR.Xml_MixedReadElementContentAs, string.Empty, this);
			}
			obj = InternalReadContentAsObject(unwrapTypedValue, out originalString);
			if (NodeType != XmlNodeType.EndElement)
			{
				throw new XmlException(System.SR.Xml_MixedReadElementContentAs, string.Empty, this);
			}
		}
		xmlType = ElementXmlType;
		Read();
		return obj;
	}

	private object ReadTillEndElement()
	{
		if (_atomicValue == null)
		{
			while (_coreReader.Read())
			{
				if (_replayCache)
				{
					continue;
				}
				switch (_coreReader.NodeType)
				{
				case XmlNodeType.Element:
					ProcessReaderEvent();
					break;
				case XmlNodeType.Text:
				case XmlNodeType.CDATA:
					_validator.ValidateText(_valueGetter);
					continue;
				case XmlNodeType.Whitespace:
				case XmlNodeType.SignificantWhitespace:
					_validator.ValidateWhitespace(_valueGetter);
					continue;
				case XmlNodeType.EndElement:
					_atomicValue = _validator.ValidateEndElement(_xmlSchemaInfo);
					_originalAtomicValueString = GetOriginalAtomicValueStringOfElement();
					if (_manageNamespaces)
					{
						_nsManager.PopScope();
					}
					break;
				default:
					continue;
				}
				break;
			}
		}
		else
		{
			if (_atomicValue == this)
			{
				_atomicValue = null;
			}
			SwitchReader();
		}
		return _atomicValue;
	}

	private void SwitchReader()
	{
		if (_coreReader is XsdCachingReader xsdCachingReader)
		{
			_coreReader = xsdCachingReader.GetCoreReader();
		}
		_replayCache = false;
	}

	private void ReadAheadForMemberType()
	{
		while (_coreReader.Read())
		{
			switch (_coreReader.NodeType)
			{
			case XmlNodeType.Text:
			case XmlNodeType.CDATA:
				_validator.ValidateText(_valueGetter);
				break;
			case XmlNodeType.Whitespace:
			case XmlNodeType.SignificantWhitespace:
				_validator.ValidateWhitespace(_valueGetter);
				break;
			case XmlNodeType.EndElement:
				_atomicValue = _validator.ValidateEndElement(_xmlSchemaInfo);
				_originalAtomicValueString = GetOriginalAtomicValueStringOfElement();
				if (_atomicValue == null)
				{
					_atomicValue = this;
				}
				else if (_xmlSchemaInfo.IsDefault)
				{
					_cachingReader.SwitchTextNodeAndEndElement(_xmlSchemaInfo.XmlType.ValueConverter.ToString(_atomicValue), _originalAtomicValueString);
				}
				return;
			}
		}
	}

	private void GetIsDefault()
	{
		XsdCachingReader xsdCachingReader = _coreReader as XsdCachingReader;
		if (xsdCachingReader != null || !_xmlSchemaInfo.HasDefaultValue)
		{
			return;
		}
		_coreReader = GetCachingReader();
		if (_xmlSchemaInfo.IsUnionType && !_xmlSchemaInfo.IsNil)
		{
			ReadAheadForMemberType();
		}
		else if (_coreReader.Read())
		{
			switch (_coreReader.NodeType)
			{
			case XmlNodeType.Text:
			case XmlNodeType.CDATA:
				_validator.ValidateText(_valueGetter);
				break;
			case XmlNodeType.Whitespace:
			case XmlNodeType.SignificantWhitespace:
				_validator.ValidateWhitespace(_valueGetter);
				break;
			case XmlNodeType.EndElement:
				_atomicValue = _validator.ValidateEndElement(_xmlSchemaInfo);
				_originalAtomicValueString = GetOriginalAtomicValueStringOfElement();
				if (_xmlSchemaInfo.IsDefault)
				{
					_cachingReader.SwitchTextNodeAndEndElement(_xmlSchemaInfo.XmlType.ValueConverter.ToString(_atomicValue), _originalAtomicValueString);
				}
				break;
			}
		}
		_cachingReader.SetToReplayMode();
		_replayCache = true;
	}

	private void GetMemberType()
	{
		if (_xmlSchemaInfo.MemberType == null && _atomicValue != this)
		{
			XsdCachingReader xsdCachingReader = _coreReader as XsdCachingReader;
			if (xsdCachingReader == null && _xmlSchemaInfo.IsUnionType && !_xmlSchemaInfo.IsNil)
			{
				_coreReader = GetCachingReader();
				ReadAheadForMemberType();
				_cachingReader.SetToReplayMode();
				_replayCache = true;
			}
		}
	}

	private object ReturnBoxedValue(object typedValue, XmlSchemaType xmlType, bool unWrap)
	{
		if (typedValue != null)
		{
			if (unWrap && xmlType.Datatype.Variety == XmlSchemaDatatypeVariety.List)
			{
				Datatype_List datatype_List = xmlType.Datatype as Datatype_List;
				if (datatype_List.ItemType.Variety == XmlSchemaDatatypeVariety.Union)
				{
					typedValue = xmlType.ValueConverter.ChangeType(typedValue, xmlType.Datatype.ValueType, _thisNSResolver);
				}
			}
			return typedValue;
		}
		typedValue = _validator.GetConcatenatedValue();
		return typedValue;
	}

	private XsdCachingReader GetCachingReader()
	{
		if (_cachingReader == null)
		{
			_cachingReader = new XsdCachingReader(_coreReader, _lineInfo, CachingCallBack);
		}
		else
		{
			_cachingReader.Reset(_coreReader);
		}
		_lineInfo = _cachingReader;
		return _cachingReader;
	}

	internal ValidatingReaderNodeData CreateDummyTextNode(string attributeValue, int depth)
	{
		if (_textNode == null)
		{
			_textNode = new ValidatingReaderNodeData(XmlNodeType.Text);
		}
		_textNode.Depth = depth;
		_textNode.RawValue = attributeValue;
		return _textNode;
	}

	internal void CachingCallBack(XsdCachingReader cachingReader)
	{
		_coreReader = cachingReader.GetCoreReader();
		_lineInfo = cachingReader.GetLineInfo();
		_replayCache = false;
	}

	private string GetOriginalAtomicValueStringOfElement()
	{
		if (_xmlSchemaInfo.IsDefault)
		{
			XmlSchemaElement schemaElement = _xmlSchemaInfo.SchemaElement;
			if (schemaElement != null)
			{
				if (schemaElement.DefaultValue == null)
				{
					return schemaElement.FixedValue;
				}
				return schemaElement.DefaultValue;
			}
			return string.Empty;
		}
		return _validator.GetConcatenatedValue();
	}

	public override Task<string> GetValueAsync()
	{
		if (_validationState < ValidatingReaderState.None)
		{
			return Task.FromResult(_cachedNode.RawValue);
		}
		return _coreReader.GetValueAsync();
	}

	public override Task<object> ReadContentAsObjectAsync()
	{
		if (!XmlReader.CanReadContentAs(NodeType))
		{
			throw CreateReadContentAsException("ReadContentAsObject");
		}
		return InternalReadContentAsObjectAsync(unwrapTypedValue: true);
	}

	public override async Task<string> ReadContentAsStringAsync()
	{
		if (!XmlReader.CanReadContentAs(NodeType))
		{
			throw CreateReadContentAsException("ReadContentAsString");
		}
		object obj = await InternalReadContentAsObjectAsync().ConfigureAwait(continueOnCapturedContext: false);
		XmlSchemaType xmlSchemaType = ((NodeType == XmlNodeType.Attribute) ? AttributeXmlType : ElementXmlType);
		try
		{
			if (xmlSchemaType != null)
			{
				return xmlSchemaType.ValueConverter.ToString(obj);
			}
			return obj as string;
		}
		catch (InvalidCastException innerException)
		{
			throw new XmlException(System.SR.Xml_ReadContentAsFormatException, "String", innerException, this);
		}
		catch (FormatException innerException2)
		{
			throw new XmlException(System.SR.Xml_ReadContentAsFormatException, "String", innerException2, this);
		}
		catch (OverflowException innerException3)
		{
			throw new XmlException(System.SR.Xml_ReadContentAsFormatException, "String", innerException3, this);
		}
	}

	public override async Task<object> ReadContentAsAsync(Type returnType, IXmlNamespaceResolver namespaceResolver)
	{
		if (!XmlReader.CanReadContentAs(NodeType))
		{
			throw CreateReadContentAsException("ReadContentAs");
		}
		(string, object) tuple = await InternalReadContentAsObjectTupleAsync(unwrapTypedValue: false).ConfigureAwait(continueOnCapturedContext: false);
		string item = tuple.Item1;
		object value = tuple.Item2;
		XmlSchemaType xmlSchemaType = ((NodeType == XmlNodeType.Attribute) ? AttributeXmlType : ElementXmlType);
		try
		{
			if (xmlSchemaType != null)
			{
				if (returnType == typeof(DateTimeOffset) && xmlSchemaType.Datatype is Datatype_dateTimeBase)
				{
					value = item;
				}
				return xmlSchemaType.ValueConverter.ChangeType(value, returnType);
			}
			return XmlUntypedConverter.Untyped.ChangeType(value, returnType, namespaceResolver);
		}
		catch (FormatException innerException)
		{
			throw new XmlException(System.SR.Xml_ReadContentAsFormatException, returnType.ToString(), innerException, this);
		}
		catch (InvalidCastException innerException2)
		{
			throw new XmlException(System.SR.Xml_ReadContentAsFormatException, returnType.ToString(), innerException2, this);
		}
		catch (OverflowException innerException3)
		{
			throw new XmlException(System.SR.Xml_ReadContentAsFormatException, returnType.ToString(), innerException3, this);
		}
	}

	public override async Task<object> ReadElementContentAsObjectAsync()
	{
		if (NodeType != XmlNodeType.Element)
		{
			throw CreateReadElementContentAsException("ReadElementContentAsObject");
		}
		return (await InternalReadElementContentAsObjectAsync(unwrapTypedValue: true).ConfigureAwait(continueOnCapturedContext: false)).Item2;
	}

	public override async Task<string> ReadElementContentAsStringAsync()
	{
		if (NodeType != XmlNodeType.Element)
		{
			throw CreateReadElementContentAsException("ReadElementContentAsString");
		}
		var (xmlSchemaType, obj) = await InternalReadElementContentAsObjectAsync().ConfigureAwait(continueOnCapturedContext: false);
		try
		{
			if (xmlSchemaType != null)
			{
				return xmlSchemaType.ValueConverter.ToString(obj);
			}
			return obj as string;
		}
		catch (InvalidCastException innerException)
		{
			throw new XmlException(System.SR.Xml_ReadContentAsFormatException, "String", innerException, this);
		}
		catch (FormatException innerException2)
		{
			throw new XmlException(System.SR.Xml_ReadContentAsFormatException, "String", innerException2, this);
		}
		catch (OverflowException innerException3)
		{
			throw new XmlException(System.SR.Xml_ReadContentAsFormatException, "String", innerException3, this);
		}
	}

	public override async Task<object> ReadElementContentAsAsync(Type returnType, IXmlNamespaceResolver namespaceResolver)
	{
		if (NodeType != XmlNodeType.Element)
		{
			throw CreateReadElementContentAsException("ReadElementContentAs");
		}
		var (xmlSchemaType, text, value) = await InternalReadElementContentAsObjectTupleAsync(unwrapTypedValue: false).ConfigureAwait(continueOnCapturedContext: false);
		try
		{
			if (xmlSchemaType != null)
			{
				if (returnType == typeof(DateTimeOffset) && xmlSchemaType.Datatype is Datatype_dateTimeBase)
				{
					value = text;
				}
				return xmlSchemaType.ValueConverter.ChangeType(value, returnType, namespaceResolver);
			}
			return XmlUntypedConverter.Untyped.ChangeType(value, returnType, namespaceResolver);
		}
		catch (FormatException innerException)
		{
			throw new XmlException(System.SR.Xml_ReadContentAsFormatException, returnType.ToString(), innerException, this);
		}
		catch (InvalidCastException innerException2)
		{
			throw new XmlException(System.SR.Xml_ReadContentAsFormatException, returnType.ToString(), innerException2, this);
		}
		catch (OverflowException innerException3)
		{
			throw new XmlException(System.SR.Xml_ReadContentAsFormatException, returnType.ToString(), innerException3, this);
		}
	}

	private Task<bool> ReadAsync_Read(Task<bool> task)
	{
		if (task.IsSuccess())
		{
			if (task.Result)
			{
				return ProcessReaderEventAsync().ReturnTrueTaskWhenFinishAsync();
			}
			_validator.EndValidation();
			if (_coreReader.EOF)
			{
				_validationState = ValidatingReaderState.EOF;
			}
			return AsyncHelper.DoneTaskFalse;
		}
		return _ReadAsync_Read(task);
	}

	private async Task<bool> _ReadAsync_Read(Task<bool> task)
	{
		if (await task.ConfigureAwait(continueOnCapturedContext: false))
		{
			await ProcessReaderEventAsync().ConfigureAwait(continueOnCapturedContext: false);
			return true;
		}
		_validator.EndValidation();
		if (_coreReader.EOF)
		{
			_validationState = ValidatingReaderState.EOF;
		}
		return false;
	}

	private Task<bool> ReadAsync_ReadAhead(Task task)
	{
		if (task.IsSuccess())
		{
			_validationState = ValidatingReaderState.Read;
			return AsyncHelper.DoneTaskTrue;
		}
		return _ReadAsync_ReadAhead(task);
	}

	private async Task<bool> _ReadAsync_ReadAhead(Task task)
	{
		await task.ConfigureAwait(continueOnCapturedContext: false);
		_validationState = ValidatingReaderState.Read;
		return true;
	}

	public override Task<bool> ReadAsync()
	{
		switch (_validationState)
		{
		case ValidatingReaderState.Read:
		{
			Task<bool> task = _coreReader.ReadAsync();
			return ReadAsync_Read(task);
		}
		case ValidatingReaderState.ParseInlineSchema:
			return ProcessInlineSchemaAsync().ReturnTrueTaskWhenFinishAsync();
		case ValidatingReaderState.OnReadAttributeValue:
		case ValidatingReaderState.OnDefaultAttribute:
		case ValidatingReaderState.OnAttribute:
		case ValidatingReaderState.ClearAttributes:
			ClearAttributesInfo();
			if (_inlineSchemaParser != null)
			{
				_validationState = ValidatingReaderState.ParseInlineSchema;
				goto case ValidatingReaderState.ParseInlineSchema;
			}
			_validationState = ValidatingReaderState.Read;
			goto case ValidatingReaderState.Read;
		case ValidatingReaderState.ReadAhead:
		{
			ClearAttributesInfo();
			Task task2 = ProcessReaderEventAsync();
			return ReadAsync_ReadAhead(task2);
		}
		case ValidatingReaderState.OnReadBinaryContent:
			_validationState = _savedState;
			return _readBinaryHelper.FinishAsync().CallBoolTaskFuncWhenFinishAsync((XsdValidatingReader thisRef) => thisRef.ReadAsync(), this);
		case ValidatingReaderState.Init:
			_validationState = ValidatingReaderState.Read;
			if (_coreReader.ReadState == ReadState.Interactive)
			{
				return ProcessReaderEventAsync().ReturnTrueTaskWhenFinishAsync();
			}
			goto case ValidatingReaderState.Read;
		case ValidatingReaderState.ReaderClosed:
		case ValidatingReaderState.EOF:
			return AsyncHelper.DoneTaskFalse;
		default:
			return AsyncHelper.DoneTaskFalse;
		}
	}

	public override async Task SkipAsync()
	{
		XmlNodeType nodeType = NodeType;
		if (nodeType != XmlNodeType.Element)
		{
			if (nodeType != XmlNodeType.Attribute)
			{
				goto IL_010f;
			}
			MoveToElement();
		}
		if (!_coreReader.IsEmptyElement)
		{
			bool callSkipToEndElem = true;
			if ((_xmlSchemaInfo.IsUnionType || _xmlSchemaInfo.IsDefault) && _coreReader is XsdCachingReader)
			{
				callSkipToEndElem = false;
			}
			await _coreReader.SkipAsync().ConfigureAwait(continueOnCapturedContext: false);
			_validationState = ValidatingReaderState.ReadAhead;
			if (callSkipToEndElem)
			{
				_validator.SkipToEndElement(_xmlSchemaInfo);
			}
		}
		goto IL_010f;
		IL_010f:
		await ReadAsync().ConfigureAwait(continueOnCapturedContext: false);
	}

	public override async Task<int> ReadContentAsBase64Async(byte[] buffer, int index, int count)
	{
		if (ReadState != ReadState.Interactive)
		{
			return 0;
		}
		if (_validationState != ValidatingReaderState.OnReadBinaryContent)
		{
			_readBinaryHelper = ReadContentAsBinaryHelper.CreateOrReset(_readBinaryHelper, this);
			_savedState = _validationState;
		}
		_validationState = _savedState;
		int result = await _readBinaryHelper.ReadContentAsBase64Async(buffer, index, count).ConfigureAwait(continueOnCapturedContext: false);
		_savedState = _validationState;
		_validationState = ValidatingReaderState.OnReadBinaryContent;
		return result;
	}

	public override async Task<int> ReadContentAsBinHexAsync(byte[] buffer, int index, int count)
	{
		if (ReadState != ReadState.Interactive)
		{
			return 0;
		}
		if (_validationState != ValidatingReaderState.OnReadBinaryContent)
		{
			_readBinaryHelper = ReadContentAsBinaryHelper.CreateOrReset(_readBinaryHelper, this);
			_savedState = _validationState;
		}
		_validationState = _savedState;
		int result = await _readBinaryHelper.ReadContentAsBinHexAsync(buffer, index, count).ConfigureAwait(continueOnCapturedContext: false);
		_savedState = _validationState;
		_validationState = ValidatingReaderState.OnReadBinaryContent;
		return result;
	}

	public override async Task<int> ReadElementContentAsBase64Async(byte[] buffer, int index, int count)
	{
		if (ReadState != ReadState.Interactive)
		{
			return 0;
		}
		if (_validationState != ValidatingReaderState.OnReadBinaryContent)
		{
			_readBinaryHelper = ReadContentAsBinaryHelper.CreateOrReset(_readBinaryHelper, this);
			_savedState = _validationState;
		}
		_validationState = _savedState;
		int result = await _readBinaryHelper.ReadElementContentAsBase64Async(buffer, index, count).ConfigureAwait(continueOnCapturedContext: false);
		_savedState = _validationState;
		_validationState = ValidatingReaderState.OnReadBinaryContent;
		return result;
	}

	public override async Task<int> ReadElementContentAsBinHexAsync(byte[] buffer, int index, int count)
	{
		if (ReadState != ReadState.Interactive)
		{
			return 0;
		}
		if (_validationState != ValidatingReaderState.OnReadBinaryContent)
		{
			_readBinaryHelper = ReadContentAsBinaryHelper.CreateOrReset(_readBinaryHelper, this);
			_savedState = _validationState;
		}
		_validationState = _savedState;
		int result = await _readBinaryHelper.ReadElementContentAsBinHexAsync(buffer, index, count).ConfigureAwait(continueOnCapturedContext: false);
		_savedState = _validationState;
		_validationState = ValidatingReaderState.OnReadBinaryContent;
		return result;
	}

	private Task ProcessReaderEventAsync()
	{
		if (_replayCache)
		{
			return Task.CompletedTask;
		}
		switch (_coreReader.NodeType)
		{
		case XmlNodeType.Element:
			return ProcessElementEventAsync();
		case XmlNodeType.Whitespace:
		case XmlNodeType.SignificantWhitespace:
			_validator.ValidateWhitespace(GetStringValue);
			break;
		case XmlNodeType.Text:
		case XmlNodeType.CDATA:
			_validator.ValidateText(GetStringValue);
			break;
		case XmlNodeType.EndElement:
			return ProcessEndElementEventAsync();
		case XmlNodeType.EntityReference:
			throw new InvalidOperationException();
		case XmlNodeType.DocumentType:
			_validator.SetDtdSchemaInfo(_coreReader.DtdInfo);
			break;
		}
		return Task.CompletedTask;
	}

	private async Task ProcessElementEventAsync()
	{
		if (_processInlineSchema && IsXSDRoot(_coreReader.LocalName, _coreReader.NamespaceURI) && _coreReader.Depth > 0)
		{
			_xmlSchemaInfo.Clear();
			_attributeCount = (_coreReaderAttributeCount = _coreReader.AttributeCount);
			if (!_coreReader.IsEmptyElement)
			{
				_inlineSchemaParser = new Parser(SchemaType.XSD, _coreReaderNameTable, _validator.SchemaSet.GetSchemaNames(_coreReaderNameTable), _validationEvent);
				await _inlineSchemaParser.StartParsingAsync(_coreReader, null).ConfigureAwait(continueOnCapturedContext: false);
				_inlineSchemaParser.ParseReaderNode();
				_validationState = ValidatingReaderState.ParseInlineSchema;
			}
			else
			{
				_validationState = ValidatingReaderState.ClearAttributes;
			}
			return;
		}
		_atomicValue = null;
		_originalAtomicValueString = null;
		_xmlSchemaInfo.Clear();
		if (_manageNamespaces)
		{
			_nsManager.PushScope();
		}
		string xsiSchemaLocation = null;
		string xsiNoNamespaceSchemaLocation = null;
		string xsiNil = null;
		string xsiType = null;
		if (_coreReader.MoveToFirstAttribute())
		{
			do
			{
				string namespaceURI = _coreReader.NamespaceURI;
				string localName = _coreReader.LocalName;
				if (Ref.Equal(namespaceURI, _nsXsi))
				{
					if (Ref.Equal(localName, _xsiSchemaLocation))
					{
						xsiSchemaLocation = _coreReader.Value;
					}
					else if (Ref.Equal(localName, _xsiNoNamespaceSchemaLocation))
					{
						xsiNoNamespaceSchemaLocation = _coreReader.Value;
					}
					else if (Ref.Equal(localName, _xsiType))
					{
						xsiType = _coreReader.Value;
					}
					else if (Ref.Equal(localName, _xsiNil))
					{
						xsiNil = _coreReader.Value;
					}
				}
				if (_manageNamespaces && Ref.Equal(_coreReader.NamespaceURI, _nsXmlNs))
				{
					_nsManager.AddNamespace((_coreReader.Prefix.Length == 0) ? string.Empty : _coreReader.LocalName, _coreReader.Value);
				}
			}
			while (_coreReader.MoveToNextAttribute());
			_coreReader.MoveToElement();
		}
		_validator.ValidateElement(_coreReader.LocalName, _coreReader.NamespaceURI, _xmlSchemaInfo, xsiType, xsiNil, xsiSchemaLocation, xsiNoNamespaceSchemaLocation);
		ValidateAttributes();
		_validator.ValidateEndOfAttributes(_xmlSchemaInfo);
		if (_coreReader.IsEmptyElement)
		{
			await ProcessEndElementEventAsync().ConfigureAwait(continueOnCapturedContext: false);
		}
		_validationState = ValidatingReaderState.ClearAttributes;
	}

	private async Task ProcessEndElementEventAsync()
	{
		_atomicValue = _validator.ValidateEndElement(_xmlSchemaInfo);
		_originalAtomicValueString = GetOriginalAtomicValueStringOfElement();
		if (_xmlSchemaInfo.IsDefault)
		{
			int depth = _coreReader.Depth;
			_coreReader = GetCachingReader();
			_cachingReader.RecordTextNode(_xmlSchemaInfo.XmlType.ValueConverter.ToString(_atomicValue), _originalAtomicValueString, depth + 1, 0, 0);
			_cachingReader.RecordEndElementNode();
			await _cachingReader.SetToReplayModeAsync().ConfigureAwait(continueOnCapturedContext: false);
			_replayCache = true;
		}
		else if (_manageNamespaces)
		{
			_nsManager.PopScope();
		}
	}

	private async Task ProcessInlineSchemaAsync()
	{
		if (await _coreReader.ReadAsync().ConfigureAwait(continueOnCapturedContext: false))
		{
			if (_coreReader.NodeType == XmlNodeType.Element)
			{
				_attributeCount = (_coreReaderAttributeCount = _coreReader.AttributeCount);
			}
			else
			{
				ClearAttributesInfo();
			}
			if (!_inlineSchemaParser.ParseReaderNode())
			{
				_inlineSchemaParser.FinishParsing();
				XmlSchema xmlSchema = _inlineSchemaParser.XmlSchema;
				_validator.AddSchema(xmlSchema);
				_inlineSchemaParser = null;
				_validationState = ValidatingReaderState.Read;
			}
		}
	}

	private Task<object> InternalReadContentAsObjectAsync()
	{
		return InternalReadContentAsObjectAsync(unwrapTypedValue: false);
	}

	private async Task<object> InternalReadContentAsObjectAsync(bool unwrapTypedValue)
	{
		return (await InternalReadContentAsObjectTupleAsync(unwrapTypedValue).ConfigureAwait(continueOnCapturedContext: false)).Item2;
	}

	private async Task<(string, object)> InternalReadContentAsObjectTupleAsync(bool unwrapTypedValue)
	{
		switch (NodeType)
		{
		case XmlNodeType.Attribute:
		{
			string originalAtomicValueString = Value;
			if (_attributePSVI != null && _attributePSVI.typedAttributeValue != null)
			{
				if (_validationState == ValidatingReaderState.OnDefaultAttribute)
				{
					XmlSchemaAttribute schemaAttribute = _attributePSVI.attributeSchemaInfo.SchemaAttribute;
					originalAtomicValueString = ((schemaAttribute.DefaultValue != null) ? schemaAttribute.DefaultValue : schemaAttribute.FixedValue);
				}
				return (originalAtomicValueString, ReturnBoxedValue(_attributePSVI.typedAttributeValue, AttributeSchemaInfo.XmlType, unwrapTypedValue));
			}
			return (originalAtomicValueString, Value);
		}
		case XmlNodeType.EndElement:
		{
			string originalAtomicValueString;
			if (_atomicValue != null)
			{
				originalAtomicValueString = _originalAtomicValueString;
				return (originalAtomicValueString, _atomicValue);
			}
			originalAtomicValueString = string.Empty;
			return (originalAtomicValueString, string.Empty);
		}
		default:
		{
			string originalAtomicValueString;
			if (_validator.CurrentContentType == XmlSchemaContentType.TextOnly)
			{
				object item = ReturnBoxedValue(await ReadTillEndElementAsync().ConfigureAwait(continueOnCapturedContext: false), _xmlSchemaInfo.XmlType, unwrapTypedValue);
				originalAtomicValueString = _originalAtomicValueString;
				return (originalAtomicValueString, item);
			}
			originalAtomicValueString = ((!(_coreReader is XsdCachingReader xsdCachingReader)) ? (await InternalReadContentAsStringAsync().ConfigureAwait(continueOnCapturedContext: false)) : xsdCachingReader.ReadOriginalContentAsString());
			return (originalAtomicValueString, originalAtomicValueString);
		}
		}
	}

	private Task<(XmlSchemaType, object)> InternalReadElementContentAsObjectAsync()
	{
		return InternalReadElementContentAsObjectAsync(unwrapTypedValue: false);
	}

	private async Task<(XmlSchemaType, object)> InternalReadElementContentAsObjectAsync(bool unwrapTypedValue)
	{
		(XmlSchemaType, string, object) tuple = await InternalReadElementContentAsObjectTupleAsync(unwrapTypedValue).ConfigureAwait(continueOnCapturedContext: false);
		return (tuple.Item1, tuple.Item3);
	}

	private async Task<(XmlSchemaType, string, object)> InternalReadElementContentAsObjectTupleAsync(bool unwrapTypedValue)
	{
		object typedValue;
		string originalString;
		XmlSchemaType xmlType;
		if (IsEmptyElement)
		{
			typedValue = ((_xmlSchemaInfo.ContentType != 0) ? _atomicValue : ReturnBoxedValue(_atomicValue, _xmlSchemaInfo.XmlType, unwrapTypedValue));
			originalString = _originalAtomicValueString;
			xmlType = ElementXmlType;
			await ReadAsync().ConfigureAwait(continueOnCapturedContext: false);
			return (xmlType, originalString, typedValue);
		}
		await ReadAsync().ConfigureAwait(continueOnCapturedContext: false);
		if (NodeType == XmlNodeType.EndElement)
		{
			if (_xmlSchemaInfo.IsDefault)
			{
				typedValue = ((_xmlSchemaInfo.ContentType != 0) ? _atomicValue : ReturnBoxedValue(_atomicValue, _xmlSchemaInfo.XmlType, unwrapTypedValue));
				originalString = _originalAtomicValueString;
			}
			else
			{
				typedValue = string.Empty;
				originalString = string.Empty;
			}
		}
		else
		{
			if (NodeType == XmlNodeType.Element)
			{
				throw new XmlException(System.SR.Xml_MixedReadElementContentAs, string.Empty, this);
			}
			(originalString, typedValue) = await InternalReadContentAsObjectTupleAsync(unwrapTypedValue).ConfigureAwait(continueOnCapturedContext: false);
			if (NodeType != XmlNodeType.EndElement)
			{
				throw new XmlException(System.SR.Xml_MixedReadElementContentAs, string.Empty, this);
			}
		}
		xmlType = ElementXmlType;
		await ReadAsync().ConfigureAwait(continueOnCapturedContext: false);
		return (xmlType, originalString, typedValue);
	}

	private async Task<object> ReadTillEndElementAsync()
	{
		if (_atomicValue == null)
		{
			while (await _coreReader.ReadAsync().ConfigureAwait(continueOnCapturedContext: false))
			{
				if (_replayCache)
				{
					continue;
				}
				switch (_coreReader.NodeType)
				{
				case XmlNodeType.Element:
					await ProcessReaderEventAsync().ConfigureAwait(continueOnCapturedContext: false);
					break;
				case XmlNodeType.Text:
				case XmlNodeType.CDATA:
					_validator.ValidateText(GetStringValue);
					continue;
				case XmlNodeType.Whitespace:
				case XmlNodeType.SignificantWhitespace:
					_validator.ValidateWhitespace(GetStringValue);
					continue;
				case XmlNodeType.EndElement:
					_atomicValue = _validator.ValidateEndElement(_xmlSchemaInfo);
					_originalAtomicValueString = GetOriginalAtomicValueStringOfElement();
					if (_manageNamespaces)
					{
						_nsManager.PopScope();
					}
					break;
				default:
					continue;
				}
				break;
			}
		}
		else
		{
			if (_atomicValue == this)
			{
				_atomicValue = null;
			}
			SwitchReader();
		}
		return _atomicValue;
	}
}
