using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Schema;

namespace System.Xml;

internal sealed class XmlValidatingReaderImpl : XmlReader, IXmlLineInfo, IXmlNamespaceResolver
{
	private enum ParsingFunction
	{
		Read,
		Init,
		ParseDtdFromContext,
		ResolveEntityInternally,
		InReadBinaryContent,
		ReaderClosed,
		Error,
		None
	}

	internal sealed class ValidationEventHandling : IValidationEventHandling
	{
		private readonly XmlValidatingReaderImpl _reader;

		private ValidationEventHandler _eventHandler;

		object IValidationEventHandling.EventHandler => _eventHandler;

		internal ValidationEventHandling(XmlValidatingReaderImpl reader)
		{
			_reader = reader;
		}

		void IValidationEventHandling.SendEvent(Exception exception, XmlSeverityType severity)
		{
			if (_eventHandler != null)
			{
				_eventHandler(_reader, new ValidationEventArgs((XmlSchemaException)exception, severity));
			}
			else if (_reader._validationType != 0 && severity == XmlSeverityType.Error)
			{
				throw exception;
			}
		}

		internal void AddHandler(ValidationEventHandler handler)
		{
			_eventHandler = (ValidationEventHandler)Delegate.Combine(_eventHandler, handler);
		}

		internal void RemoveHandler(ValidationEventHandler handler)
		{
			_eventHandler = (ValidationEventHandler)Delegate.Remove(_eventHandler, handler);
		}
	}

	private readonly XmlReader _coreReader;

	private readonly XmlTextReaderImpl _coreReaderImpl;

	private readonly IXmlNamespaceResolver _coreReaderNSResolver;

	private ValidationType _validationType;

	private BaseValidator _validator;

	private readonly XmlSchemaCollection _schemaCollection;

	private readonly bool _processIdentityConstraints;

	private ParsingFunction _parsingFunction = ParsingFunction.Init;

	private readonly ValidationEventHandling _eventHandling;

	private readonly XmlParserContext _parserContext;

	private ReadContentAsBinaryHelper _readBinaryHelper;

	private XmlReader _outerReader;

	private static XmlResolver s_tempResolver;

	public override XmlReaderSettings Settings
	{
		get
		{
			XmlReaderSettings xmlReaderSettings = ((!_coreReaderImpl.V1Compat) ? _coreReader.Settings : null);
			xmlReaderSettings = ((xmlReaderSettings == null) ? new XmlReaderSettings() : xmlReaderSettings.Clone());
			xmlReaderSettings.ValidationType = ValidationType.DTD;
			if (!_processIdentityConstraints)
			{
				xmlReaderSettings.ValidationFlags &= ~XmlSchemaValidationFlags.ProcessIdentityConstraints;
			}
			xmlReaderSettings.ReadOnly = true;
			return xmlReaderSettings;
		}
	}

	public override XmlNodeType NodeType => _coreReader.NodeType;

	public override string Name => _coreReader.Name;

	public override string LocalName => _coreReader.LocalName;

	public override string NamespaceURI => _coreReader.NamespaceURI;

	public override string Prefix => _coreReader.Prefix;

	public override bool HasValue => _coreReader.HasValue;

	public override string Value => _coreReader.Value;

	public override int Depth => _coreReader.Depth;

	public override string BaseURI => _coreReader.BaseURI;

	public override bool IsEmptyElement => _coreReader.IsEmptyElement;

	public override bool IsDefault => _coreReader.IsDefault;

	public override char QuoteChar => _coreReader.QuoteChar;

	public override XmlSpace XmlSpace => _coreReader.XmlSpace;

	public override string XmlLang => _coreReader.XmlLang;

	public override ReadState ReadState
	{
		get
		{
			if (_parsingFunction != ParsingFunction.Init)
			{
				return _coreReader.ReadState;
			}
			return ReadState.Initial;
		}
	}

	public override bool EOF => _coreReader.EOF;

	public override XmlNameTable NameTable => _coreReader.NameTable;

	internal Encoding Encoding => _coreReaderImpl.Encoding;

	public override int AttributeCount => _coreReader.AttributeCount;

	public override bool CanReadBinaryContent => true;

	public override bool CanResolveEntity => true;

	internal XmlReader OuterReader
	{
		set
		{
			_outerReader = value;
		}
	}

	public int LineNumber => ((IXmlLineInfo)_coreReader).LineNumber;

	public int LinePosition => ((IXmlLineInfo)_coreReader).LinePosition;

	internal object SchemaType
	{
		get
		{
			if (_validationType != 0)
			{
				if (_coreReaderImpl.InternalSchemaType is XmlSchemaType xmlSchemaType && xmlSchemaType.QualifiedName.Namespace == "http://www.w3.org/2001/XMLSchema")
				{
					return xmlSchemaType.Datatype;
				}
				return _coreReaderImpl.InternalSchemaType;
			}
			return null;
		}
	}

	internal XmlReader Reader => _coreReader;

	internal XmlTextReaderImpl ReaderImpl => _coreReaderImpl;

	internal ValidationType ValidationType
	{
		get
		{
			return _validationType;
		}
		set
		{
			if (ReadState != 0)
			{
				throw new InvalidOperationException(System.SR.Xml_InvalidOperation);
			}
			_validationType = value;
			SetupValidation(value);
		}
	}

	internal XmlSchemaCollection Schemas => _schemaCollection;

	internal EntityHandling EntityHandling
	{
		get
		{
			return _coreReaderImpl.EntityHandling;
		}
		set
		{
			_coreReaderImpl.EntityHandling = value;
		}
	}

	internal XmlResolver XmlResolver
	{
		set
		{
			_coreReaderImpl.XmlResolver = value;
			_validator.XmlResolver = value;
			_schemaCollection.XmlResolver = value;
		}
	}

	internal bool Namespaces
	{
		get
		{
			return _coreReaderImpl.Namespaces;
		}
		set
		{
			_coreReaderImpl.Namespaces = value;
		}
	}

	internal BaseValidator Validator
	{
		get
		{
			return _validator;
		}
		set
		{
			_validator = value;
		}
	}

	internal override XmlNamespaceManager NamespaceManager => _coreReaderImpl.NamespaceManager;

	internal bool StandAlone => _coreReaderImpl.StandAlone;

	internal object SchemaTypeObject
	{
		set
		{
			_coreReaderImpl.InternalSchemaType = value;
		}
	}

	internal object TypedValueObject
	{
		get
		{
			return _coreReaderImpl.InternalTypedValue;
		}
		set
		{
			_coreReaderImpl.InternalTypedValue = value;
		}
	}

	internal override IDtdInfo DtdInfo => _coreReaderImpl.DtdInfo;

	internal event ValidationEventHandler ValidationEventHandler
	{
		add
		{
			_eventHandling.AddHandler(value);
		}
		remove
		{
			_eventHandling.RemoveHandler(value);
		}
	}

	internal XmlValidatingReaderImpl(XmlReader reader)
	{
		if (reader is XmlAsyncCheckReader xmlAsyncCheckReader)
		{
			reader = xmlAsyncCheckReader.CoreReader;
		}
		_outerReader = this;
		_coreReader = reader;
		_coreReaderNSResolver = reader as IXmlNamespaceResolver;
		_coreReaderImpl = reader as XmlTextReaderImpl;
		if (_coreReaderImpl == null && reader is XmlTextReader xmlTextReader)
		{
			_coreReaderImpl = xmlTextReader.Impl;
		}
		if (_coreReaderImpl == null)
		{
			throw new ArgumentException(System.SR.Arg_ExpectingXmlTextReader, "reader");
		}
		_coreReaderImpl.EntityHandling = EntityHandling.ExpandEntities;
		_coreReaderImpl.XmlValidatingReaderCompatibilityMode = true;
		_processIdentityConstraints = true;
		_schemaCollection = new XmlSchemaCollection(_coreReader.NameTable);
		_schemaCollection.XmlResolver = GetResolver();
		_eventHandling = new ValidationEventHandling(this);
		_coreReaderImpl.ValidationEventHandling = _eventHandling;
		_coreReaderImpl.OnDefaultAttributeUse = ValidateDefaultAttributeOnUse;
		_validationType = ValidationType.Auto;
		SetupValidation(ValidationType.Auto);
	}

	internal XmlValidatingReaderImpl(string xmlFragment, XmlNodeType fragType, XmlParserContext context)
		: this(new XmlTextReader(xmlFragment, fragType, context))
	{
		if (_coreReader.BaseURI.Length > 0)
		{
			_validator.BaseUri = GetResolver().ResolveUri(null, _coreReader.BaseURI);
		}
		if (context != null)
		{
			_parsingFunction = ParsingFunction.ParseDtdFromContext;
			_parserContext = context;
		}
	}

	internal XmlValidatingReaderImpl(Stream xmlFragment, XmlNodeType fragType, XmlParserContext context)
		: this(new XmlTextReader(xmlFragment, fragType, context))
	{
		if (_coreReader.BaseURI.Length > 0)
		{
			_validator.BaseUri = GetResolver().ResolveUri(null, _coreReader.BaseURI);
		}
		if (context != null)
		{
			_parsingFunction = ParsingFunction.ParseDtdFromContext;
			_parserContext = context;
		}
	}

	internal XmlValidatingReaderImpl(XmlReader reader, ValidationEventHandler settingsEventHandler, bool processIdentityConstraints)
	{
		if (reader is XmlAsyncCheckReader xmlAsyncCheckReader)
		{
			reader = xmlAsyncCheckReader.CoreReader;
		}
		_outerReader = this;
		_coreReader = reader;
		_coreReaderImpl = reader as XmlTextReaderImpl;
		if (_coreReaderImpl == null && reader is XmlTextReader xmlTextReader)
		{
			_coreReaderImpl = xmlTextReader.Impl;
		}
		if (_coreReaderImpl == null)
		{
			throw new ArgumentException(System.SR.Arg_ExpectingXmlTextReader, "reader");
		}
		_coreReaderImpl.XmlValidatingReaderCompatibilityMode = true;
		_coreReaderNSResolver = reader as IXmlNamespaceResolver;
		_processIdentityConstraints = processIdentityConstraints;
		_schemaCollection = new XmlSchemaCollection(_coreReader.NameTable);
		_schemaCollection.XmlResolver = GetResolver();
		_eventHandling = new ValidationEventHandling(this);
		if (settingsEventHandler != null)
		{
			_eventHandling.AddHandler(settingsEventHandler);
		}
		_coreReaderImpl.ValidationEventHandling = _eventHandling;
		_coreReaderImpl.OnDefaultAttributeUse = ValidateDefaultAttributeOnUse;
		_validationType = ValidationType.DTD;
		SetupValidation(ValidationType.DTD);
	}

	public override string GetAttribute(string name)
	{
		return _coreReader.GetAttribute(name);
	}

	public override string GetAttribute(string localName, string namespaceURI)
	{
		return _coreReader.GetAttribute(localName, namespaceURI);
	}

	public override string GetAttribute(int i)
	{
		return _coreReader.GetAttribute(i);
	}

	public override bool MoveToAttribute(string name)
	{
		if (!_coreReader.MoveToAttribute(name))
		{
			return false;
		}
		_parsingFunction = ParsingFunction.Read;
		return true;
	}

	public override bool MoveToAttribute(string localName, string namespaceURI)
	{
		if (!_coreReader.MoveToAttribute(localName, namespaceURI))
		{
			return false;
		}
		_parsingFunction = ParsingFunction.Read;
		return true;
	}

	public override void MoveToAttribute(int i)
	{
		_coreReader.MoveToAttribute(i);
		_parsingFunction = ParsingFunction.Read;
	}

	public override bool MoveToFirstAttribute()
	{
		if (!_coreReader.MoveToFirstAttribute())
		{
			return false;
		}
		_parsingFunction = ParsingFunction.Read;
		return true;
	}

	public override bool MoveToNextAttribute()
	{
		if (!_coreReader.MoveToNextAttribute())
		{
			return false;
		}
		_parsingFunction = ParsingFunction.Read;
		return true;
	}

	public override bool MoveToElement()
	{
		if (!_coreReader.MoveToElement())
		{
			return false;
		}
		_parsingFunction = ParsingFunction.Read;
		return true;
	}

	public override bool Read()
	{
		switch (_parsingFunction)
		{
		case ParsingFunction.Read:
			if (_coreReader.Read())
			{
				ProcessCoreReaderEvent();
				return true;
			}
			_validator.CompleteValidation();
			return false;
		case ParsingFunction.ParseDtdFromContext:
			_parsingFunction = ParsingFunction.Read;
			ParseDtdFromParserContext();
			goto case ParsingFunction.Read;
		case ParsingFunction.ReaderClosed:
		case ParsingFunction.Error:
			return false;
		case ParsingFunction.Init:
			_parsingFunction = ParsingFunction.Read;
			if (_coreReader.ReadState == ReadState.Interactive)
			{
				ProcessCoreReaderEvent();
				return true;
			}
			goto case ParsingFunction.Read;
		case ParsingFunction.ResolveEntityInternally:
			_parsingFunction = ParsingFunction.Read;
			ResolveEntityInternally();
			goto case ParsingFunction.Read;
		case ParsingFunction.InReadBinaryContent:
			_parsingFunction = ParsingFunction.Read;
			_readBinaryHelper.Finish();
			goto case ParsingFunction.Read;
		default:
			return false;
		}
	}

	public override void Close()
	{
		_coreReader.Close();
		_parsingFunction = ParsingFunction.ReaderClosed;
	}

	public override string LookupNamespace(string prefix)
	{
		return _coreReaderImpl.LookupNamespace(prefix);
	}

	public override bool ReadAttributeValue()
	{
		if (_parsingFunction == ParsingFunction.InReadBinaryContent)
		{
			_parsingFunction = ParsingFunction.Read;
			_readBinaryHelper.Finish();
		}
		if (!_coreReader.ReadAttributeValue())
		{
			return false;
		}
		_parsingFunction = ParsingFunction.Read;
		return true;
	}

	public override int ReadContentAsBase64(byte[] buffer, int index, int count)
	{
		if (ReadState != ReadState.Interactive)
		{
			return 0;
		}
		if (_parsingFunction != ParsingFunction.InReadBinaryContent)
		{
			_readBinaryHelper = ReadContentAsBinaryHelper.CreateOrReset(_readBinaryHelper, _outerReader);
		}
		_parsingFunction = ParsingFunction.Read;
		int result = _readBinaryHelper.ReadContentAsBase64(buffer, index, count);
		_parsingFunction = ParsingFunction.InReadBinaryContent;
		return result;
	}

	public override int ReadContentAsBinHex(byte[] buffer, int index, int count)
	{
		if (ReadState != ReadState.Interactive)
		{
			return 0;
		}
		if (_parsingFunction != ParsingFunction.InReadBinaryContent)
		{
			_readBinaryHelper = ReadContentAsBinaryHelper.CreateOrReset(_readBinaryHelper, _outerReader);
		}
		_parsingFunction = ParsingFunction.Read;
		int result = _readBinaryHelper.ReadContentAsBinHex(buffer, index, count);
		_parsingFunction = ParsingFunction.InReadBinaryContent;
		return result;
	}

	public override int ReadElementContentAsBase64(byte[] buffer, int index, int count)
	{
		if (ReadState != ReadState.Interactive)
		{
			return 0;
		}
		if (_parsingFunction != ParsingFunction.InReadBinaryContent)
		{
			_readBinaryHelper = ReadContentAsBinaryHelper.CreateOrReset(_readBinaryHelper, _outerReader);
		}
		_parsingFunction = ParsingFunction.Read;
		int result = _readBinaryHelper.ReadElementContentAsBase64(buffer, index, count);
		_parsingFunction = ParsingFunction.InReadBinaryContent;
		return result;
	}

	public override int ReadElementContentAsBinHex(byte[] buffer, int index, int count)
	{
		if (ReadState != ReadState.Interactive)
		{
			return 0;
		}
		if (_parsingFunction != ParsingFunction.InReadBinaryContent)
		{
			_readBinaryHelper = ReadContentAsBinaryHelper.CreateOrReset(_readBinaryHelper, _outerReader);
		}
		_parsingFunction = ParsingFunction.Read;
		int result = _readBinaryHelper.ReadElementContentAsBinHex(buffer, index, count);
		_parsingFunction = ParsingFunction.InReadBinaryContent;
		return result;
	}

	public override void ResolveEntity()
	{
		if (_parsingFunction == ParsingFunction.ResolveEntityInternally)
		{
			_parsingFunction = ParsingFunction.Read;
		}
		_coreReader.ResolveEntity();
	}

	internal void MoveOffEntityReference()
	{
		if (_outerReader.NodeType == XmlNodeType.EntityReference && _parsingFunction != ParsingFunction.ResolveEntityInternally && !_outerReader.Read())
		{
			throw new InvalidOperationException(System.SR.Xml_InvalidOperation);
		}
	}

	public override string ReadString()
	{
		MoveOffEntityReference();
		return base.ReadString();
	}

	public bool HasLineInfo()
	{
		return true;
	}

	IDictionary<string, string> IXmlNamespaceResolver.GetNamespacesInScope(XmlNamespaceScope scope)
	{
		return GetNamespacesInScope(scope);
	}

	string IXmlNamespaceResolver.LookupNamespace(string prefix)
	{
		return LookupNamespace(prefix);
	}

	string IXmlNamespaceResolver.LookupPrefix(string namespaceName)
	{
		return LookupPrefix(namespaceName);
	}

	internal IDictionary<string, string> GetNamespacesInScope(XmlNamespaceScope scope)
	{
		return _coreReaderNSResolver.GetNamespacesInScope(scope);
	}

	internal string LookupPrefix(string namespaceName)
	{
		return _coreReaderNSResolver.LookupPrefix(namespaceName);
	}

	public object ReadTypedValue()
	{
		if (_validationType == ValidationType.None)
		{
			return null;
		}
		switch (_outerReader.NodeType)
		{
		case XmlNodeType.Attribute:
			return _coreReaderImpl.InternalTypedValue;
		case XmlNodeType.Element:
		{
			if (SchemaType == null)
			{
				return null;
			}
			XmlSchemaDatatype xmlSchemaDatatype = ((SchemaType is XmlSchemaDatatype) ? ((XmlSchemaDatatype)SchemaType) : ((XmlSchemaType)SchemaType).Datatype);
			if (xmlSchemaDatatype != null)
			{
				if (!_outerReader.IsEmptyElement)
				{
					XmlNodeType nodeType;
					do
					{
						if (!_outerReader.Read())
						{
							throw new InvalidOperationException(System.SR.Xml_InvalidOperation);
						}
						nodeType = _outerReader.NodeType;
					}
					while (nodeType == XmlNodeType.CDATA || nodeType == XmlNodeType.Text || nodeType == XmlNodeType.Whitespace || nodeType == XmlNodeType.SignificantWhitespace || nodeType == XmlNodeType.Comment || nodeType == XmlNodeType.ProcessingInstruction);
					if (_outerReader.NodeType != XmlNodeType.EndElement)
					{
						throw new XmlException(System.SR.Xml_InvalidNodeType, _outerReader.NodeType.ToString());
					}
				}
				return _coreReaderImpl.InternalTypedValue;
			}
			return null;
		}
		case XmlNodeType.EndElement:
			return null;
		default:
			if (_coreReaderImpl.V1Compat)
			{
				return null;
			}
			return Value;
		}
	}

	private void ParseDtdFromParserContext()
	{
		if (_parserContext.DocTypeName != null && _parserContext.DocTypeName.Length != 0)
		{
			IDtdParser dtdParser = DtdParser.Create();
			XmlTextReaderImpl.DtdParserProxy adapter = new XmlTextReaderImpl.DtdParserProxy(_coreReaderImpl);
			IDtdInfo dtdInfo = dtdParser.ParseFreeFloatingDtd(_parserContext.BaseURI, _parserContext.DocTypeName, _parserContext.PublicId, _parserContext.SystemId, _parserContext.InternalSubset, adapter);
			_coreReaderImpl.SetDtdInfo(dtdInfo);
			ValidateDtd();
		}
	}

	private void ValidateDtd()
	{
		IDtdInfo dtdInfo = _coreReaderImpl.DtdInfo;
		if (dtdInfo != null)
		{
			switch (_validationType)
			{
			default:
				return;
			case ValidationType.Auto:
				SetupValidation(ValidationType.DTD);
				break;
			case ValidationType.None:
			case ValidationType.DTD:
				break;
			}
			_validator.DtdInfo = dtdInfo;
		}
	}

	private void ResolveEntityInternally()
	{
		int depth = _coreReader.Depth;
		_outerReader.ResolveEntity();
		while (_outerReader.Read() && _coreReader.Depth > depth)
		{
		}
	}

	[MemberNotNull("_validator")]
	private void SetupValidation(ValidationType valType)
	{
		_validator = BaseValidator.CreateInstance(valType, this, _schemaCollection, _eventHandling, _processIdentityConstraints);
		XmlResolver resolver = GetResolver();
		_validator.XmlResolver = resolver;
		if (_outerReader.BaseURI.Length > 0)
		{
			_validator.BaseUri = ((resolver == null) ? new Uri(_outerReader.BaseURI, UriKind.RelativeOrAbsolute) : resolver.ResolveUri(null, _outerReader.BaseURI));
		}
		_coreReaderImpl.ValidationEventHandling = ((_validationType == ValidationType.None) ? null : _eventHandling);
	}

	private XmlResolver GetResolver()
	{
		XmlResolver resolver = _coreReaderImpl.GetResolver();
		if (resolver == null && !_coreReaderImpl.IsResolverSet)
		{
			if (s_tempResolver == null)
			{
				s_tempResolver = new XmlUrlResolver();
			}
			return s_tempResolver;
		}
		return resolver;
	}

	private void ProcessCoreReaderEvent()
	{
		switch (_coreReader.NodeType)
		{
		case XmlNodeType.Whitespace:
			if ((_coreReader.Depth > 0 || _coreReaderImpl.FragmentType != XmlNodeType.Document) && _validator.PreserveWhitespace)
			{
				_coreReaderImpl.ChangeCurrentNodeType(XmlNodeType.SignificantWhitespace);
			}
			break;
		case XmlNodeType.DocumentType:
			ValidateDtd();
			return;
		case XmlNodeType.EntityReference:
			_parsingFunction = ParsingFunction.ResolveEntityInternally;
			break;
		}
		_coreReaderImpl.InternalSchemaType = null;
		_coreReaderImpl.InternalTypedValue = null;
		_validator.Validate();
	}

	internal bool AddDefaultAttribute(SchemaAttDef attdef)
	{
		return _coreReaderImpl.AddDefaultAttributeNonDtd(attdef);
	}

	internal void ValidateDefaultAttributeOnUse(IDtdDefaultAttributeInfo defaultAttribute, XmlTextReaderImpl coreReader)
	{
		if (defaultAttribute is SchemaAttDef attdef && coreReader.DtdInfo is SchemaInfo sinfo)
		{
			DtdValidator.CheckDefaultValue(attdef, sinfo, _eventHandling, coreReader.BaseURI);
		}
	}

	public override Task<string> GetValueAsync()
	{
		return _coreReader.GetValueAsync();
	}

	public override async Task<bool> ReadAsync()
	{
		switch (_parsingFunction)
		{
		case ParsingFunction.Read:
			if (await _coreReader.ReadAsync().ConfigureAwait(continueOnCapturedContext: false))
			{
				ProcessCoreReaderEvent();
				return true;
			}
			_validator.CompleteValidation();
			return false;
		case ParsingFunction.ParseDtdFromContext:
			_parsingFunction = ParsingFunction.Read;
			await ParseDtdFromParserContextAsync().ConfigureAwait(continueOnCapturedContext: false);
			goto case ParsingFunction.Read;
		case ParsingFunction.ReaderClosed:
		case ParsingFunction.Error:
			return false;
		case ParsingFunction.Init:
			_parsingFunction = ParsingFunction.Read;
			if (_coreReader.ReadState == ReadState.Interactive)
			{
				ProcessCoreReaderEvent();
				return true;
			}
			goto case ParsingFunction.Read;
		case ParsingFunction.ResolveEntityInternally:
			_parsingFunction = ParsingFunction.Read;
			await ResolveEntityInternallyAsync().ConfigureAwait(continueOnCapturedContext: false);
			goto case ParsingFunction.Read;
		case ParsingFunction.InReadBinaryContent:
			_parsingFunction = ParsingFunction.Read;
			await _readBinaryHelper.FinishAsync().ConfigureAwait(continueOnCapturedContext: false);
			goto case ParsingFunction.Read;
		default:
			return false;
		}
	}

	public override async Task<int> ReadContentAsBase64Async(byte[] buffer, int index, int count)
	{
		if (ReadState != ReadState.Interactive)
		{
			return 0;
		}
		if (_parsingFunction != ParsingFunction.InReadBinaryContent)
		{
			_readBinaryHelper = ReadContentAsBinaryHelper.CreateOrReset(_readBinaryHelper, _outerReader);
		}
		_parsingFunction = ParsingFunction.Read;
		int result = await _readBinaryHelper.ReadContentAsBase64Async(buffer, index, count).ConfigureAwait(continueOnCapturedContext: false);
		_parsingFunction = ParsingFunction.InReadBinaryContent;
		return result;
	}

	public override async Task<int> ReadContentAsBinHexAsync(byte[] buffer, int index, int count)
	{
		if (ReadState != ReadState.Interactive)
		{
			return 0;
		}
		if (_parsingFunction != ParsingFunction.InReadBinaryContent)
		{
			_readBinaryHelper = ReadContentAsBinaryHelper.CreateOrReset(_readBinaryHelper, _outerReader);
		}
		_parsingFunction = ParsingFunction.Read;
		int result = await _readBinaryHelper.ReadContentAsBinHexAsync(buffer, index, count).ConfigureAwait(continueOnCapturedContext: false);
		_parsingFunction = ParsingFunction.InReadBinaryContent;
		return result;
	}

	public override async Task<int> ReadElementContentAsBase64Async(byte[] buffer, int index, int count)
	{
		if (ReadState != ReadState.Interactive)
		{
			return 0;
		}
		if (_parsingFunction != ParsingFunction.InReadBinaryContent)
		{
			_readBinaryHelper = ReadContentAsBinaryHelper.CreateOrReset(_readBinaryHelper, _outerReader);
		}
		_parsingFunction = ParsingFunction.Read;
		int result = await _readBinaryHelper.ReadElementContentAsBase64Async(buffer, index, count).ConfigureAwait(continueOnCapturedContext: false);
		_parsingFunction = ParsingFunction.InReadBinaryContent;
		return result;
	}

	public override async Task<int> ReadElementContentAsBinHexAsync(byte[] buffer, int index, int count)
	{
		if (ReadState != ReadState.Interactive)
		{
			return 0;
		}
		if (_parsingFunction != ParsingFunction.InReadBinaryContent)
		{
			_readBinaryHelper = ReadContentAsBinaryHelper.CreateOrReset(_readBinaryHelper, _outerReader);
		}
		_parsingFunction = ParsingFunction.Read;
		int result = await _readBinaryHelper.ReadElementContentAsBinHexAsync(buffer, index, count).ConfigureAwait(continueOnCapturedContext: false);
		_parsingFunction = ParsingFunction.InReadBinaryContent;
		return result;
	}

	private async Task ParseDtdFromParserContextAsync()
	{
		if (_parserContext.DocTypeName != null && _parserContext.DocTypeName.Length != 0)
		{
			IDtdParser dtdParser = DtdParser.Create();
			XmlTextReaderImpl.DtdParserProxy adapter = new XmlTextReaderImpl.DtdParserProxy(_coreReaderImpl);
			IDtdInfo dtdInfo = await dtdParser.ParseFreeFloatingDtdAsync(_parserContext.BaseURI, _parserContext.DocTypeName, _parserContext.PublicId, _parserContext.SystemId, _parserContext.InternalSubset, adapter).ConfigureAwait(continueOnCapturedContext: false);
			_coreReaderImpl.SetDtdInfo(dtdInfo);
			ValidateDtd();
		}
	}

	private async Task ResolveEntityInternallyAsync()
	{
		int initialDepth = _coreReader.Depth;
		_outerReader.ResolveEntity();
		while (await _outerReader.ReadAsync().ConfigureAwait(continueOnCapturedContext: false) && _coreReader.Depth > initialDepth)
		{
		}
	}
}
