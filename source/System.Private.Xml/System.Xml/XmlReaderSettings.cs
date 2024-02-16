using System.IO;
using System.Xml.Schema;

namespace System.Xml;

public sealed class XmlReaderSettings
{
	private delegate XmlReader AddValidationFunc(XmlReader reader, XmlResolver resolver, bool addConformanceWrapper);

	internal static readonly XmlReaderSettings s_defaultReaderSettings = new XmlReaderSettings
	{
		ReadOnly = true
	};

	private bool _useAsync;

	private XmlNameTable _nameTable;

	private XmlResolver _xmlResolver;

	private int _lineNumberOffset;

	private int _linePositionOffset;

	private ConformanceLevel _conformanceLevel;

	private bool _checkCharacters;

	private long _maxCharactersInDocument;

	private long _maxCharactersFromEntities;

	private bool _ignoreWhitespace;

	private bool _ignorePIs;

	private bool _ignoreComments;

	private DtdProcessing _dtdProcessing;

	private ValidationType _validationType;

	private XmlSchemaValidationFlags _validationFlags;

	private XmlSchemaSet _schemas;

	private ValidationEventHandler _valEventHandler;

	private bool _closeInput;

	private bool _isReadOnly;

	private AddValidationFunc _addValidationFunc;

	public bool Async
	{
		get
		{
			return _useAsync;
		}
		set
		{
			CheckReadOnly("Async");
			_useAsync = value;
		}
	}

	public XmlNameTable? NameTable
	{
		get
		{
			return _nameTable;
		}
		set
		{
			CheckReadOnly("NameTable");
			_nameTable = value;
		}
	}

	internal bool IsXmlResolverSet { get; set; }

	public XmlResolver? XmlResolver
	{
		set
		{
			CheckReadOnly("XmlResolver");
			_xmlResolver = value;
			IsXmlResolverSet = true;
		}
	}

	public int LineNumberOffset
	{
		get
		{
			return _lineNumberOffset;
		}
		set
		{
			CheckReadOnly("LineNumberOffset");
			_lineNumberOffset = value;
		}
	}

	public int LinePositionOffset
	{
		get
		{
			return _linePositionOffset;
		}
		set
		{
			CheckReadOnly("LinePositionOffset");
			_linePositionOffset = value;
		}
	}

	public ConformanceLevel ConformanceLevel
	{
		get
		{
			return _conformanceLevel;
		}
		set
		{
			CheckReadOnly("ConformanceLevel");
			if ((uint)value > 2u)
			{
				throw new ArgumentOutOfRangeException("value");
			}
			_conformanceLevel = value;
		}
	}

	public bool CheckCharacters
	{
		get
		{
			return _checkCharacters;
		}
		set
		{
			CheckReadOnly("CheckCharacters");
			_checkCharacters = value;
		}
	}

	public long MaxCharactersInDocument
	{
		get
		{
			return _maxCharactersInDocument;
		}
		set
		{
			CheckReadOnly("MaxCharactersInDocument");
			if (value < 0)
			{
				throw new ArgumentOutOfRangeException("value");
			}
			_maxCharactersInDocument = value;
		}
	}

	public long MaxCharactersFromEntities
	{
		get
		{
			return _maxCharactersFromEntities;
		}
		set
		{
			CheckReadOnly("MaxCharactersFromEntities");
			if (value < 0)
			{
				throw new ArgumentOutOfRangeException("value");
			}
			_maxCharactersFromEntities = value;
		}
	}

	public bool IgnoreWhitespace
	{
		get
		{
			return _ignoreWhitespace;
		}
		set
		{
			CheckReadOnly("IgnoreWhitespace");
			_ignoreWhitespace = value;
		}
	}

	public bool IgnoreProcessingInstructions
	{
		get
		{
			return _ignorePIs;
		}
		set
		{
			CheckReadOnly("IgnoreProcessingInstructions");
			_ignorePIs = value;
		}
	}

	public bool IgnoreComments
	{
		get
		{
			return _ignoreComments;
		}
		set
		{
			CheckReadOnly("IgnoreComments");
			_ignoreComments = value;
		}
	}

	[Obsolete("XmlReaderSettings.ProhibitDtd has been deprecated. Use DtdProcessing instead.")]
	public bool ProhibitDtd
	{
		get
		{
			return _dtdProcessing == DtdProcessing.Prohibit;
		}
		set
		{
			CheckReadOnly("ProhibitDtd");
			_dtdProcessing = ((!value) ? DtdProcessing.Parse : DtdProcessing.Prohibit);
		}
	}

	public DtdProcessing DtdProcessing
	{
		get
		{
			return _dtdProcessing;
		}
		set
		{
			CheckReadOnly("DtdProcessing");
			if ((uint)value > 2u)
			{
				throw new ArgumentOutOfRangeException("value");
			}
			_dtdProcessing = value;
		}
	}

	public bool CloseInput
	{
		get
		{
			return _closeInput;
		}
		set
		{
			CheckReadOnly("CloseInput");
			_closeInput = value;
		}
	}

	public ValidationType ValidationType
	{
		get
		{
			return _validationType;
		}
		set
		{
			CheckReadOnly("ValidationType");
			_addValidationFunc = AddValidationInternal;
			if ((uint)value > 4u)
			{
				throw new ArgumentOutOfRangeException("value");
			}
			_validationType = value;
		}
	}

	public XmlSchemaValidationFlags ValidationFlags
	{
		get
		{
			return _validationFlags;
		}
		set
		{
			CheckReadOnly("ValidationFlags");
			if ((uint)value > 31u)
			{
				throw new ArgumentOutOfRangeException("value");
			}
			_validationFlags = value;
		}
	}

	public XmlSchemaSet Schemas
	{
		get
		{
			if (_schemas == null)
			{
				_schemas = new XmlSchemaSet();
			}
			return _schemas;
		}
		set
		{
			CheckReadOnly("Schemas");
			_schemas = value;
		}
	}

	internal bool ReadOnly
	{
		set
		{
			_isReadOnly = value;
		}
	}

	public event ValidationEventHandler ValidationEventHandler
	{
		add
		{
			CheckReadOnly("ValidationEventHandler");
			_valEventHandler = (ValidationEventHandler)Delegate.Combine(_valEventHandler, value);
		}
		remove
		{
			CheckReadOnly("ValidationEventHandler");
			_valEventHandler = (ValidationEventHandler)Delegate.Remove(_valEventHandler, value);
		}
	}

	public XmlReaderSettings()
	{
		Initialize();
	}

	internal XmlResolver GetXmlResolver()
	{
		return _xmlResolver;
	}

	internal XmlResolver GetXmlResolver_CheckConfig()
	{
		if (!System.LocalAppContextSwitches.AllowDefaultResolver && !IsXmlResolverSet)
		{
			return null;
		}
		return _xmlResolver;
	}

	public void Reset()
	{
		CheckReadOnly("Reset");
		Initialize();
	}

	public XmlReaderSettings Clone()
	{
		XmlReaderSettings xmlReaderSettings = MemberwiseClone() as XmlReaderSettings;
		xmlReaderSettings.ReadOnly = false;
		return xmlReaderSettings;
	}

	internal ValidationEventHandler GetEventHandler()
	{
		return _valEventHandler;
	}

	internal XmlReader CreateReader(string inputUri, XmlParserContext inputContext)
	{
		if (inputUri == null)
		{
			throw new ArgumentNullException("inputUri");
		}
		if (inputUri.Length == 0)
		{
			throw new ArgumentException(System.SR.XmlConvert_BadUri, "inputUri");
		}
		XmlResolver uriResolver = GetXmlResolver() ?? new XmlUrlResolver();
		XmlReader xmlReader = new XmlTextReaderImpl(inputUri, this, inputContext, uriResolver);
		if (ValidationType != 0)
		{
			xmlReader = AddValidation(xmlReader);
		}
		if (_useAsync)
		{
			xmlReader = XmlAsyncCheckReader.CreateAsyncCheckWrapper(xmlReader);
		}
		return xmlReader;
	}

	internal XmlReader CreateReader(Stream input, Uri baseUri, string baseUriString, XmlParserContext inputContext)
	{
		if (input == null)
		{
			throw new ArgumentNullException("input");
		}
		if (baseUriString == null)
		{
			baseUriString = ((!(baseUri == null)) ? baseUri.ToString() : string.Empty);
		}
		XmlReader xmlReader = new XmlTextReaderImpl(input, null, 0, this, baseUri, baseUriString, inputContext, _closeInput);
		if (ValidationType != 0)
		{
			xmlReader = AddValidation(xmlReader);
		}
		if (_useAsync)
		{
			xmlReader = XmlAsyncCheckReader.CreateAsyncCheckWrapper(xmlReader);
		}
		return xmlReader;
	}

	internal XmlReader CreateReader(TextReader input, string baseUriString, XmlParserContext inputContext)
	{
		if (input == null)
		{
			throw new ArgumentNullException("input");
		}
		if (baseUriString == null)
		{
			baseUriString = string.Empty;
		}
		XmlReader xmlReader = new XmlTextReaderImpl(input, this, baseUriString, inputContext);
		if (ValidationType != 0)
		{
			xmlReader = AddValidation(xmlReader);
		}
		if (_useAsync)
		{
			xmlReader = XmlAsyncCheckReader.CreateAsyncCheckWrapper(xmlReader);
		}
		return xmlReader;
	}

	internal XmlReader CreateReader(XmlReader reader)
	{
		if (reader == null)
		{
			throw new ArgumentNullException("reader");
		}
		return AddValidationAndConformanceWrapper(reader);
	}

	private void CheckReadOnly(string propertyName)
	{
		if (_isReadOnly)
		{
			throw new XmlException(System.SR.Xml_ReadOnlyProperty, GetType().Name + "." + propertyName);
		}
	}

	private void Initialize()
	{
		Initialize(null);
	}

	private void Initialize(XmlResolver resolver)
	{
		_nameTable = null;
		_xmlResolver = resolver;
		_maxCharactersFromEntities = 10000000L;
		_lineNumberOffset = 0;
		_linePositionOffset = 0;
		_checkCharacters = true;
		_conformanceLevel = ConformanceLevel.Document;
		_ignoreWhitespace = false;
		_ignorePIs = false;
		_ignoreComments = false;
		_dtdProcessing = DtdProcessing.Prohibit;
		_closeInput = false;
		_maxCharactersInDocument = 0L;
		_schemas = null;
		_validationType = ValidationType.None;
		_validationFlags = XmlSchemaValidationFlags.ProcessIdentityConstraints;
		_validationFlags |= XmlSchemaValidationFlags.AllowXmlAttributes;
		_useAsync = false;
		_isReadOnly = false;
		IsXmlResolverSet = false;
	}

	internal XmlReader AddValidation(XmlReader reader)
	{
		XmlResolver xmlResolver = null;
		if (_validationType == ValidationType.Schema)
		{
			xmlResolver = GetXmlResolver_CheckConfig();
			if (xmlResolver == null && !IsXmlResolverSet)
			{
				xmlResolver = new XmlUrlResolver();
			}
		}
		return AddValidationAndConformanceInternal(reader, xmlResolver, addConformanceWrapper: false);
	}

	private XmlReader AddValidationAndConformanceWrapper(XmlReader reader)
	{
		XmlResolver resolver = null;
		if (_validationType == ValidationType.Schema)
		{
			resolver = GetXmlResolver_CheckConfig();
		}
		return AddValidationAndConformanceInternal(reader, resolver, addConformanceWrapper: true);
	}

	private XmlReader AddValidationAndConformanceInternal(XmlReader reader, XmlResolver resolver, bool addConformanceWrapper)
	{
		if (_validationType == ValidationType.None)
		{
			if (addConformanceWrapper)
			{
				reader = AddConformanceWrapper(reader);
			}
		}
		else
		{
			reader = _addValidationFunc(reader, resolver, addConformanceWrapper);
		}
		return reader;
	}

	private XmlReader AddValidationInternal(XmlReader reader, XmlResolver resolver, bool addConformanceWrapper)
	{
		if (_validationType == ValidationType.DTD)
		{
			reader = CreateDtdValidatingReader(reader);
		}
		if (addConformanceWrapper)
		{
			reader = AddConformanceWrapper(reader);
		}
		if (_validationType == ValidationType.Schema)
		{
			reader = new XsdValidatingReader(reader, GetXmlResolver_CheckConfig(), this);
		}
		return reader;
	}

	private XmlValidatingReaderImpl CreateDtdValidatingReader(XmlReader baseReader)
	{
		return new XmlValidatingReaderImpl(baseReader, GetEventHandler(), (ValidationFlags & XmlSchemaValidationFlags.ProcessIdentityConstraints) != 0);
	}

	internal XmlReader AddConformanceWrapper(XmlReader baseReader)
	{
		XmlReaderSettings settings = baseReader.Settings;
		bool checkCharacters = false;
		bool ignoreWhitespace = false;
		bool ignoreComments = false;
		bool ignorePis = false;
		DtdProcessing dtdProcessing = (DtdProcessing)(-1);
		bool flag = false;
		if (settings == null)
		{
			if (_conformanceLevel != 0 && _conformanceLevel != XmlReader.GetV1ConformanceLevel(baseReader))
			{
				throw new InvalidOperationException(System.SR.Format(System.SR.Xml_IncompatibleConformanceLevel, _conformanceLevel.ToString()));
			}
			XmlTextReader xmlTextReader = baseReader as XmlTextReader;
			if (xmlTextReader == null && baseReader is XmlValidatingReader xmlValidatingReader)
			{
				xmlTextReader = (XmlTextReader)xmlValidatingReader.Reader;
			}
			if (_ignoreWhitespace)
			{
				WhitespaceHandling whitespaceHandling = WhitespaceHandling.All;
				if (xmlTextReader != null)
				{
					whitespaceHandling = xmlTextReader.WhitespaceHandling;
				}
				if (whitespaceHandling == WhitespaceHandling.All)
				{
					ignoreWhitespace = true;
					flag = true;
				}
			}
			if (_ignoreComments)
			{
				ignoreComments = true;
				flag = true;
			}
			if (_ignorePIs)
			{
				ignorePis = true;
				flag = true;
			}
			DtdProcessing dtdProcessing2 = DtdProcessing.Parse;
			if (xmlTextReader != null)
			{
				dtdProcessing2 = xmlTextReader.DtdProcessing;
			}
			if ((_dtdProcessing == DtdProcessing.Prohibit && dtdProcessing2 != 0) || (_dtdProcessing == DtdProcessing.Ignore && dtdProcessing2 == DtdProcessing.Parse))
			{
				dtdProcessing = _dtdProcessing;
				flag = true;
			}
		}
		else
		{
			if (_conformanceLevel != settings.ConformanceLevel && _conformanceLevel != 0)
			{
				throw new InvalidOperationException(System.SR.Format(System.SR.Xml_IncompatibleConformanceLevel, _conformanceLevel.ToString()));
			}
			if (_checkCharacters && !settings.CheckCharacters)
			{
				checkCharacters = true;
				flag = true;
			}
			if (_ignoreWhitespace && !settings.IgnoreWhitespace)
			{
				ignoreWhitespace = true;
				flag = true;
			}
			if (_ignoreComments && !settings.IgnoreComments)
			{
				ignoreComments = true;
				flag = true;
			}
			if (_ignorePIs && !settings.IgnoreProcessingInstructions)
			{
				ignorePis = true;
				flag = true;
			}
			if ((_dtdProcessing == DtdProcessing.Prohibit && settings.DtdProcessing != 0) || (_dtdProcessing == DtdProcessing.Ignore && settings.DtdProcessing == DtdProcessing.Parse))
			{
				dtdProcessing = _dtdProcessing;
				flag = true;
			}
		}
		if (flag)
		{
			if (baseReader is IXmlNamespaceResolver readerAsNSResolver)
			{
				return new XmlCharCheckingReaderWithNS(baseReader, readerAsNSResolver, checkCharacters, ignoreWhitespace, ignoreComments, ignorePis, dtdProcessing);
			}
			return new XmlCharCheckingReader(baseReader, checkCharacters, ignoreWhitespace, ignoreComments, ignorePis, dtdProcessing);
		}
		return baseReader;
	}
}
