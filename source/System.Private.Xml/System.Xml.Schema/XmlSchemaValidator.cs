using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Text;
using System.Threading;

namespace System.Xml.Schema;

public sealed class XmlSchemaValidator
{
	private readonly XmlSchemaSet _schemaSet;

	private readonly XmlSchemaValidationFlags _validationFlags;

	private int _startIDConstraint = -1;

	private bool _isRoot;

	private bool _rootHasSchema;

	private bool _attrValid;

	private bool _checkEntity;

	private SchemaInfo _compiledSchemaInfo;

	private IDtdInfo _dtdSchemaInfo;

	private readonly Hashtable _validatedNamespaces;

	private HWStack _validationStack;

	private ValidationState _context;

	private ValidatorState _currentState;

	private Hashtable _attPresence;

	private SchemaAttDef _wildID;

	private Hashtable _IDs;

	private IdRefNode _idRefListHead;

	private XmlQualifiedName _contextQName;

	private string _nsXs;

	private string _nsXsi;

	private string _nsXmlNs;

	private string _nsXml;

	private XmlSchemaObject _partialValidationType;

	private StringBuilder _textValue;

	private ValidationEventHandler _eventHandler;

	private object _validationEventSender;

	private readonly XmlNameTable _nameTable;

	private IXmlLineInfo _positionInfo;

	private static readonly IXmlLineInfo s_dummyPositionInfo = new PositionInfo();

	private XmlResolver _xmlResolver;

	private Uri _sourceUri;

	private string _sourceUriString;

	private readonly IXmlNamespaceResolver _nsResolver;

	private XmlSchemaContentProcessing _processContents = XmlSchemaContentProcessing.Strict;

	private static XmlSchemaAttribute s_xsiTypeSO;

	private static XmlSchemaAttribute s_xsiNilSO;

	private static XmlSchemaAttribute s_xsiSLSO;

	private static XmlSchemaAttribute s_xsiNoNsSLSO;

	private string _xsiTypeString;

	private string _xsiNilString;

	private string _xsiSchemaLocationString;

	private string _xsiNoNamespaceSchemaLocationString;

	private static readonly XmlSchemaDatatype s_dtQName = XmlSchemaDatatype.FromXmlTokenizedTypeXsd(XmlTokenizedType.QName);

	private static readonly XmlSchemaDatatype s_dtCDATA = XmlSchemaDatatype.FromXmlTokenizedType(XmlTokenizedType.CDATA);

	private static readonly XmlSchemaDatatype s_dtStringArray = s_dtCDATA.DeriveByList(null);

	internal static bool[,] ValidStates = new bool[12, 12]
	{
		{
			true, true, false, false, false, false, false, false, false, false,
			false, false
		},
		{
			false, true, true, true, true, false, false, false, false, false,
			false, true
		},
		{
			false, false, false, false, false, false, false, false, false, false,
			false, true
		},
		{
			false, false, false, true, true, false, false, false, false, false,
			false, true
		},
		{
			false, false, false, true, false, true, true, false, false, true,
			true, false
		},
		{
			false, false, false, false, false, true, true, false, false, true,
			true, false
		},
		{
			false, false, false, false, true, false, false, true, true, true,
			true, false
		},
		{
			false, false, false, false, true, false, false, true, true, true,
			true, false
		},
		{
			false, false, false, false, true, false, false, true, true, true,
			true, false
		},
		{
			false, false, false, true, true, false, false, true, true, true,
			true, true
		},
		{
			false, false, false, true, true, false, false, true, true, true,
			true, true
		},
		{
			false, true, false, false, false, false, false, false, false, false,
			false, false
		}
	};

	private static readonly string[] s_methodNames = new string[12]
	{
		"None", "Initialize", "top-level ValidateAttribute", "top-level ValidateText or ValidateWhitespace", "ValidateElement", "ValidateAttribute", "ValidateEndOfAttributes", "ValidateText", "ValidateWhitespace", "ValidateEndElement",
		"SkipToEndElement", "EndValidation"
	};

	public XmlResolver? XmlResolver
	{
		set
		{
			_xmlResolver = value;
		}
	}

	public IXmlLineInfo LineInfoProvider
	{
		get
		{
			return _positionInfo;
		}
		[param: AllowNull]
		set
		{
			if (value == null)
			{
				_positionInfo = s_dummyPositionInfo;
			}
			else
			{
				_positionInfo = value;
			}
		}
	}

	public Uri? SourceUri
	{
		get
		{
			return _sourceUri;
		}
		[param: DisallowNull]
		set
		{
			_sourceUri = value;
			_sourceUriString = _sourceUri.ToString();
		}
	}

	public object ValidationEventSender
	{
		get
		{
			return _validationEventSender;
		}
		set
		{
			_validationEventSender = value;
		}
	}

	internal XmlSchemaSet SchemaSet => _schemaSet;

	internal XmlSchemaValidationFlags ValidationFlags => _validationFlags;

	internal XmlSchemaContentType CurrentContentType
	{
		get
		{
			if (_context.ElementDecl == null)
			{
				return XmlSchemaContentType.Empty;
			}
			return _context.ElementDecl.ContentValidator.ContentType;
		}
	}

	internal XmlSchemaContentProcessing CurrentProcessContents => _processContents;

	private bool StrictlyAssessed
	{
		get
		{
			if ((_processContents == XmlSchemaContentProcessing.Strict || _processContents == XmlSchemaContentProcessing.Lax) && _context.ElementDecl != null)
			{
				return !_context.ValidationSkipped;
			}
			return false;
		}
	}

	private bool HasSchema
	{
		get
		{
			if (_isRoot)
			{
				_isRoot = false;
				if (!_compiledSchemaInfo.Contains(_context.Namespace))
				{
					_rootHasSchema = false;
				}
			}
			return _rootHasSchema;
		}
	}

	private bool HasIdentityConstraints
	{
		get
		{
			if (ProcessIdentityConstraints)
			{
				return _startIDConstraint != -1;
			}
			return false;
		}
	}

	internal bool ProcessIdentityConstraints => (_validationFlags & XmlSchemaValidationFlags.ProcessIdentityConstraints) != 0;

	internal bool ReportValidationWarnings => (_validationFlags & XmlSchemaValidationFlags.ReportValidationWarnings) != 0;

	internal bool ProcessSchemaHints
	{
		get
		{
			if ((_validationFlags & XmlSchemaValidationFlags.ProcessInlineSchema) == 0)
			{
				return (_validationFlags & XmlSchemaValidationFlags.ProcessSchemaLocation) != 0;
			}
			return true;
		}
	}

	public event ValidationEventHandler? ValidationEventHandler
	{
		add
		{
			_eventHandler = (ValidationEventHandler)Delegate.Combine(_eventHandler, value);
		}
		remove
		{
			_eventHandler = (ValidationEventHandler)Delegate.Remove(_eventHandler, value);
		}
	}

	public XmlSchemaValidator(XmlNameTable nameTable, XmlSchemaSet schemas, IXmlNamespaceResolver namespaceResolver, XmlSchemaValidationFlags validationFlags)
	{
		if (nameTable == null)
		{
			throw new ArgumentNullException("nameTable");
		}
		if (schemas == null)
		{
			throw new ArgumentNullException("schemas");
		}
		if (namespaceResolver == null)
		{
			throw new ArgumentNullException("namespaceResolver");
		}
		_nameTable = nameTable;
		_nsResolver = namespaceResolver;
		_validationFlags = validationFlags;
		if ((validationFlags & XmlSchemaValidationFlags.ProcessInlineSchema) != 0 || (validationFlags & XmlSchemaValidationFlags.ProcessSchemaLocation) != 0)
		{
			_schemaSet = new XmlSchemaSet(nameTable);
			_schemaSet.ValidationEventHandler += schemas.GetEventHandler();
			_schemaSet.CompilationSettings = schemas.CompilationSettings;
			_schemaSet.XmlResolver = schemas.GetResolver();
			_schemaSet.Add(schemas);
			_validatedNamespaces = new Hashtable();
		}
		else
		{
			_schemaSet = schemas;
		}
		Init();
	}

	[MemberNotNull("_validationStack")]
	[MemberNotNull("_attPresence")]
	[MemberNotNull("_positionInfo")]
	[MemberNotNull("_validationEventSender")]
	[MemberNotNull("_currentState")]
	[MemberNotNull("_textValue")]
	[MemberNotNull("_context")]
	[MemberNotNull("_contextQName")]
	[MemberNotNull("_nsXs")]
	[MemberNotNull("_nsXsi")]
	[MemberNotNull("_nsXmlNs")]
	[MemberNotNull("_nsXml")]
	[MemberNotNull("_xsiTypeString")]
	[MemberNotNull("_xsiNilString")]
	[MemberNotNull("_xsiSchemaLocationString")]
	[MemberNotNull("_xsiNoNamespaceSchemaLocationString")]
	[MemberNotNull("_compiledSchemaInfo")]
	private void Init()
	{
		_validationStack = new HWStack(10);
		_attPresence = new Hashtable();
		Push(XmlQualifiedName.Empty);
		_positionInfo = s_dummyPositionInfo;
		_validationEventSender = this;
		_currentState = ValidatorState.None;
		_textValue = new StringBuilder(100);
		_xmlResolver = null;
		_contextQName = new XmlQualifiedName();
		Reset();
		RecompileSchemaSet();
		_nsXs = _nameTable.Add("http://www.w3.org/2001/XMLSchema");
		_nsXsi = _nameTable.Add("http://www.w3.org/2001/XMLSchema-instance");
		_nsXmlNs = _nameTable.Add("http://www.w3.org/2000/xmlns/");
		_nsXml = _nameTable.Add("http://www.w3.org/XML/1998/namespace");
		_xsiTypeString = _nameTable.Add("type");
		_xsiNilString = _nameTable.Add("nil");
		_xsiSchemaLocationString = _nameTable.Add("schemaLocation");
		_xsiNoNamespaceSchemaLocationString = _nameTable.Add("noNamespaceSchemaLocation");
	}

	private void Reset()
	{
		_isRoot = true;
		_rootHasSchema = true;
		while (_validationStack.Length > 1)
		{
			_validationStack.Pop();
		}
		_startIDConstraint = -1;
		_partialValidationType = null;
		if (_IDs != null)
		{
			_IDs.Clear();
		}
		if (ProcessSchemaHints)
		{
			_validatedNamespaces.Clear();
		}
	}

	public void AddSchema(XmlSchema schema)
	{
		if (schema == null)
		{
			throw new ArgumentNullException("schema");
		}
		if ((_validationFlags & XmlSchemaValidationFlags.ProcessInlineSchema) == 0)
		{
			return;
		}
		string text = schema.TargetNamespace;
		if (text == null)
		{
			text = string.Empty;
		}
		Hashtable schemaLocations = _schemaSet.SchemaLocations;
		DictionaryEntry[] array = new DictionaryEntry[schemaLocations.Count];
		schemaLocations.CopyTo(array, 0);
		if (_validatedNamespaces[text] != null && _schemaSet.FindSchemaByNSAndUrl(schema.BaseUri, text, array) == null)
		{
			SendValidationEvent(System.SR.Sch_ComponentAlreadySeenForNS, text, XmlSeverityType.Error);
		}
		if (schema.ErrorCount != 0)
		{
			return;
		}
		try
		{
			_schemaSet.Add(schema);
			RecompileSchemaSet();
		}
		catch (XmlSchemaException ex)
		{
			SendValidationEvent(System.SR.Sch_CannotLoadSchema, new string[2]
			{
				schema.BaseUri.ToString(),
				ex.Message
			}, ex);
		}
		for (int i = 0; i < schema.ImportedSchemas.Count; i++)
		{
			XmlSchema xmlSchema = (XmlSchema)schema.ImportedSchemas[i];
			text = xmlSchema.TargetNamespace;
			if (text == null)
			{
				text = string.Empty;
			}
			if (_validatedNamespaces[text] != null && _schemaSet.FindSchemaByNSAndUrl(xmlSchema.BaseUri, text, array) == null)
			{
				SendValidationEvent(System.SR.Sch_ComponentAlreadySeenForNS, text, XmlSeverityType.Error);
				_schemaSet.RemoveRecursive(schema);
				break;
			}
		}
	}

	public void Initialize()
	{
		if (_currentState != 0 && _currentState != ValidatorState.Finish)
		{
			string sch_InvalidStateTransition = System.SR.Sch_InvalidStateTransition;
			object[] args = new string[2]
			{
				s_methodNames[(int)_currentState],
				s_methodNames[1]
			};
			throw new InvalidOperationException(System.SR.Format(sch_InvalidStateTransition, args));
		}
		_currentState = ValidatorState.Start;
		Reset();
	}

	public void Initialize(XmlSchemaObject partialValidationType)
	{
		if (_currentState != 0 && _currentState != ValidatorState.Finish)
		{
			string sch_InvalidStateTransition = System.SR.Sch_InvalidStateTransition;
			object[] args = new string[2]
			{
				s_methodNames[(int)_currentState],
				s_methodNames[1]
			};
			throw new InvalidOperationException(System.SR.Format(sch_InvalidStateTransition, args));
		}
		if (partialValidationType == null)
		{
			throw new ArgumentNullException("partialValidationType");
		}
		if (!(partialValidationType is XmlSchemaElement) && !(partialValidationType is XmlSchemaAttribute) && !(partialValidationType is XmlSchemaType))
		{
			throw new ArgumentException(System.SR.Sch_InvalidPartialValidationType);
		}
		_currentState = ValidatorState.Start;
		Reset();
		_partialValidationType = partialValidationType;
	}

	public void ValidateElement(string localName, string namespaceUri, XmlSchemaInfo? schemaInfo)
	{
		ValidateElement(localName, namespaceUri, schemaInfo, null, null, null, null);
	}

	public void ValidateElement(string localName, string namespaceUri, XmlSchemaInfo? schemaInfo, string? xsiType, string? xsiNil, string? xsiSchemaLocation, string? xsiNoNamespaceSchemaLocation)
	{
		if (localName == null)
		{
			throw new ArgumentNullException("localName");
		}
		if (namespaceUri == null)
		{
			throw new ArgumentNullException("namespaceUri");
		}
		CheckStateTransition(ValidatorState.Element, s_methodNames[4]);
		ClearPSVI();
		_contextQName.Init(localName, namespaceUri);
		XmlQualifiedName contextQName = _contextQName;
		bool invalidElementInContext;
		object particle = ValidateElementContext(contextQName, out invalidElementInContext);
		SchemaElementDecl schemaElementDecl = FastGetElementDecl(contextQName, particle);
		Push(contextQName);
		if (invalidElementInContext)
		{
			_context.Validity = XmlSchemaValidity.Invalid;
		}
		if ((_validationFlags & XmlSchemaValidationFlags.ProcessSchemaLocation) != 0 && _xmlResolver != null)
		{
			ProcessSchemaLocations(xsiSchemaLocation, xsiNoNamespaceSchemaLocation);
		}
		if (_processContents != XmlSchemaContentProcessing.Skip)
		{
			if (schemaElementDecl == null && _partialValidationType == null)
			{
				schemaElementDecl = _compiledSchemaInfo.GetElementDecl(contextQName);
			}
			bool declFound = schemaElementDecl != null;
			if (xsiType != null || xsiNil != null)
			{
				schemaElementDecl = CheckXsiTypeAndNil(schemaElementDecl, xsiType, xsiNil, ref declFound);
			}
			if (schemaElementDecl == null)
			{
				ThrowDeclNotFoundWarningOrError(declFound);
			}
		}
		_context.ElementDecl = schemaElementDecl;
		XmlSchemaElement schemaElement = null;
		XmlSchemaType schemaType = null;
		if (schemaElementDecl != null)
		{
			CheckElementProperties();
			_attPresence.Clear();
			_context.NeedValidateChildren = _processContents != XmlSchemaContentProcessing.Skip;
			ValidateStartElementIdentityConstraints();
			schemaElementDecl.ContentValidator.InitValidation(_context);
			schemaType = schemaElementDecl.SchemaType;
			schemaElement = GetSchemaElement();
		}
		if (schemaInfo != null)
		{
			schemaInfo.SchemaType = schemaType;
			schemaInfo.SchemaElement = schemaElement;
			schemaInfo.IsNil = _context.IsNill;
			schemaInfo.Validity = _context.Validity;
		}
		if (ProcessSchemaHints && _validatedNamespaces[namespaceUri] == null)
		{
			_validatedNamespaces.Add(namespaceUri, namespaceUri);
		}
		if (_isRoot)
		{
			_isRoot = false;
		}
	}

	public object? ValidateAttribute(string localName, string namespaceUri, string attributeValue, XmlSchemaInfo? schemaInfo)
	{
		if (attributeValue == null)
		{
			throw new ArgumentNullException("attributeValue");
		}
		return ValidateAttribute(localName, namespaceUri, null, attributeValue, schemaInfo);
	}

	public object? ValidateAttribute(string localName, string namespaceUri, XmlValueGetter attributeValue, XmlSchemaInfo? schemaInfo)
	{
		if (attributeValue == null)
		{
			throw new ArgumentNullException("attributeValue");
		}
		return ValidateAttribute(localName, namespaceUri, attributeValue, null, schemaInfo);
	}

	private object ValidateAttribute(string localName, string namespaceUri, XmlValueGetter attributeValueGetter, string attributeStringValue, XmlSchemaInfo schemaInfo)
	{
		if (localName == null)
		{
			throw new ArgumentNullException("localName");
		}
		if (namespaceUri == null)
		{
			throw new ArgumentNullException("namespaceUri");
		}
		ValidatorState validatorState = ((_validationStack.Length > 1) ? ValidatorState.Attribute : ValidatorState.TopLevelAttribute);
		CheckStateTransition(validatorState, s_methodNames[(int)validatorState]);
		object obj = null;
		_attrValid = true;
		XmlSchemaValidity validity = XmlSchemaValidity.NotKnown;
		XmlSchemaAttribute xmlSchemaAttribute = null;
		XmlSchemaSimpleType memberType = null;
		namespaceUri = _nameTable.Add(namespaceUri);
		if (Ref.Equal(namespaceUri, _nsXmlNs))
		{
			return null;
		}
		SchemaAttDef schemaAttDef = null;
		SchemaElementDecl elementDecl = _context.ElementDecl;
		XmlQualifiedName xmlQualifiedName = new XmlQualifiedName(localName, namespaceUri);
		if (_attPresence[xmlQualifiedName] != null)
		{
			SendValidationEvent(System.SR.Sch_DuplicateAttribute, xmlQualifiedName.ToString());
			schemaInfo?.Clear();
			return null;
		}
		if (!Ref.Equal(namespaceUri, _nsXsi))
		{
			XmlSchemaObject xmlSchemaObject = ((_currentState == ValidatorState.TopLevelAttribute) ? _partialValidationType : null);
			schemaAttDef = _compiledSchemaInfo.GetAttributeXsd(elementDecl, xmlQualifiedName, xmlSchemaObject, out var attributeMatchState);
			switch (attributeMatchState)
			{
			case AttributeMatchState.UndeclaredElementAndAttribute:
				if ((schemaAttDef = CheckIsXmlAttribute(xmlQualifiedName)) == null)
				{
					if (elementDecl == null && _processContents == XmlSchemaContentProcessing.Strict && xmlQualifiedName.Namespace.Length != 0 && _compiledSchemaInfo.Contains(xmlQualifiedName.Namespace))
					{
						_attrValid = false;
						SendValidationEvent(System.SR.Sch_UndeclaredAttribute, xmlQualifiedName.ToString());
					}
					else if (_processContents != XmlSchemaContentProcessing.Skip)
					{
						SendValidationEvent(System.SR.Sch_NoAttributeSchemaFound, xmlQualifiedName.ToString(), XmlSeverityType.Warning);
					}
					break;
				}
				goto case AttributeMatchState.AttributeFound;
			case AttributeMatchState.UndeclaredAttribute:
				if ((schemaAttDef = CheckIsXmlAttribute(xmlQualifiedName)) == null)
				{
					_attrValid = false;
					SendValidationEvent(System.SR.Sch_UndeclaredAttribute, xmlQualifiedName.ToString());
					break;
				}
				goto case AttributeMatchState.AttributeFound;
			case AttributeMatchState.ProhibitedAnyAttribute:
				if ((schemaAttDef = CheckIsXmlAttribute(xmlQualifiedName)) == null)
				{
					_attrValid = false;
					SendValidationEvent(System.SR.Sch_ProhibitedAttribute, xmlQualifiedName.ToString());
					break;
				}
				goto case AttributeMatchState.AttributeFound;
			case AttributeMatchState.ProhibitedAttribute:
				_attrValid = false;
				SendValidationEvent(System.SR.Sch_ProhibitedAttribute, xmlQualifiedName.ToString());
				break;
			case AttributeMatchState.AttributeNameMismatch:
				_attrValid = false;
				SendValidationEvent(System.SR.Sch_SchemaAttributeNameMismatch, new string[2]
				{
					xmlQualifiedName.ToString(),
					((XmlSchemaAttribute)xmlSchemaObject).QualifiedName.ToString()
				});
				break;
			case AttributeMatchState.ValidateAttributeInvalidCall:
				_currentState = ValidatorState.Start;
				_attrValid = false;
				SendValidationEvent(System.SR.Sch_ValidateAttributeInvalidCall, string.Empty);
				break;
			case AttributeMatchState.AnyIdAttributeFound:
				if (_wildID == null)
				{
					_wildID = schemaAttDef;
					XmlSchemaComplexType xmlSchemaComplexType = elementDecl.SchemaType as XmlSchemaComplexType;
					if (xmlSchemaComplexType.ContainsIdAttribute(findAll: false))
					{
						SendValidationEvent(System.SR.Sch_AttrUseAndWildId, string.Empty);
						break;
					}
					goto case AttributeMatchState.AttributeFound;
				}
				SendValidationEvent(System.SR.Sch_MoreThanOneWildId, string.Empty);
				break;
			case AttributeMatchState.AttributeFound:
			{
				xmlSchemaAttribute = schemaAttDef.SchemaAttribute;
				if (elementDecl != null)
				{
					_attPresence.Add(xmlQualifiedName, schemaAttDef);
				}
				object obj2 = ((attributeValueGetter == null) ? attributeStringValue : attributeValueGetter());
				obj = CheckAttributeValue(obj2, schemaAttDef);
				XmlSchemaDatatype datatype = schemaAttDef.Datatype;
				if (datatype.Variety == XmlSchemaDatatypeVariety.Union && obj != null)
				{
					XsdSimpleValue xsdSimpleValue = obj as XsdSimpleValue;
					memberType = xsdSimpleValue.XmlType;
					datatype = xsdSimpleValue.XmlType.Datatype;
					obj = xsdSimpleValue.TypedValue;
				}
				CheckTokenizedTypes(datatype, obj, attrValue: true);
				if (HasIdentityConstraints)
				{
					AttributeIdentityConstraints(xmlQualifiedName.Name, xmlQualifiedName.Namespace, obj, obj2.ToString(), datatype);
				}
				break;
			}
			case AttributeMatchState.AnyAttributeLax:
				SendValidationEvent(System.SR.Sch_NoAttributeSchemaFound, xmlQualifiedName.ToString(), XmlSeverityType.Warning);
				break;
			}
		}
		else
		{
			localName = _nameTable.Add(localName);
			if (Ref.Equal(localName, _xsiTypeString) || Ref.Equal(localName, _xsiNilString) || Ref.Equal(localName, _xsiSchemaLocationString) || Ref.Equal(localName, _xsiNoNamespaceSchemaLocationString))
			{
				_attPresence.Add(xmlQualifiedName, SchemaAttDef.Empty);
			}
			else
			{
				_attrValid = false;
				SendValidationEvent(System.SR.Sch_NotXsiAttribute, xmlQualifiedName.ToString());
			}
		}
		if (!_attrValid)
		{
			validity = XmlSchemaValidity.Invalid;
		}
		else if (schemaAttDef != null)
		{
			validity = XmlSchemaValidity.Valid;
		}
		if (schemaInfo != null)
		{
			schemaInfo.SchemaAttribute = xmlSchemaAttribute;
			schemaInfo.SchemaType = xmlSchemaAttribute?.AttributeSchemaType;
			schemaInfo.MemberType = memberType;
			schemaInfo.IsDefault = false;
			schemaInfo.Validity = validity;
		}
		if (ProcessSchemaHints && _validatedNamespaces[namespaceUri] == null)
		{
			_validatedNamespaces.Add(namespaceUri, namespaceUri);
		}
		return obj;
	}

	public void GetUnspecifiedDefaultAttributes(ArrayList defaultAttributes)
	{
		if (defaultAttributes == null)
		{
			throw new ArgumentNullException("defaultAttributes");
		}
		CheckStateTransition(ValidatorState.Attribute, "GetUnspecifiedDefaultAttributes");
		GetUnspecifiedDefaultAttributes(defaultAttributes, createNodeData: false);
	}

	public void ValidateEndOfAttributes(XmlSchemaInfo? schemaInfo)
	{
		CheckStateTransition(ValidatorState.EndOfAttributes, s_methodNames[6]);
		SchemaElementDecl elementDecl = _context.ElementDecl;
		if (elementDecl != null && elementDecl.HasRequiredAttribute)
		{
			_context.CheckRequiredAttribute = false;
			CheckRequiredAttributes(elementDecl);
		}
		if (schemaInfo != null)
		{
			schemaInfo.Validity = _context.Validity;
		}
	}

	public void ValidateText(string elementValue)
	{
		if (elementValue == null)
		{
			throw new ArgumentNullException("elementValue");
		}
		ValidateText(elementValue, null);
	}

	public void ValidateText(XmlValueGetter elementValue)
	{
		if (elementValue == null)
		{
			throw new ArgumentNullException("elementValue");
		}
		ValidateText(null, elementValue);
	}

	private void ValidateText(string elementStringValue, XmlValueGetter elementValueGetter)
	{
		ValidatorState validatorState = ((_validationStack.Length > 1) ? ValidatorState.Text : ValidatorState.TopLevelTextOrWS);
		CheckStateTransition(validatorState, s_methodNames[(int)validatorState]);
		if (!_context.NeedValidateChildren)
		{
			return;
		}
		if (_context.IsNill)
		{
			SendValidationEvent(System.SR.Sch_ContentInNill, QNameString(_context.LocalName, _context.Namespace));
			return;
		}
		switch (_context.ElementDecl.ContentValidator.ContentType)
		{
		case XmlSchemaContentType.Empty:
			SendValidationEvent(System.SR.Sch_InvalidTextInEmpty, string.Empty);
			break;
		case XmlSchemaContentType.TextOnly:
			if (elementValueGetter != null)
			{
				SaveTextValue(elementValueGetter());
			}
			else
			{
				SaveTextValue(elementStringValue);
			}
			break;
		case XmlSchemaContentType.ElementOnly:
		{
			string str = ((elementValueGetter != null) ? elementValueGetter().ToString() : elementStringValue);
			if (!XmlCharType.IsOnlyWhitespace(str))
			{
				ArrayList arrayList = _context.ElementDecl.ContentValidator.ExpectedParticles(_context, isRequiredOnly: false, _schemaSet);
				if (arrayList == null || arrayList.Count == 0)
				{
					SendValidationEvent(System.SR.Sch_InvalidTextInElement, BuildElementName(_context.LocalName, _context.Namespace));
					break;
				}
				SendValidationEvent(System.SR.Sch_InvalidTextInElementExpecting, new string[2]
				{
					BuildElementName(_context.LocalName, _context.Namespace),
					PrintExpectedElements(arrayList, getParticles: true)
				});
			}
			break;
		}
		case XmlSchemaContentType.Mixed:
			if (_context.ElementDecl.DefaultValueTyped != null)
			{
				if (elementValueGetter != null)
				{
					SaveTextValue(elementValueGetter());
				}
				else
				{
					SaveTextValue(elementStringValue);
				}
			}
			break;
		}
	}

	public void ValidateWhitespace(string elementValue)
	{
		if (elementValue == null)
		{
			throw new ArgumentNullException("elementValue");
		}
		ValidateWhitespace(elementValue, null);
	}

	public void ValidateWhitespace(XmlValueGetter elementValue)
	{
		if (elementValue == null)
		{
			throw new ArgumentNullException("elementValue");
		}
		ValidateWhitespace(null, elementValue);
	}

	private void ValidateWhitespace(string elementStringValue, XmlValueGetter elementValueGetter)
	{
		ValidatorState validatorState = ((_validationStack.Length > 1) ? ValidatorState.Whitespace : ValidatorState.TopLevelTextOrWS);
		CheckStateTransition(validatorState, s_methodNames[(int)validatorState]);
		if (!_context.NeedValidateChildren)
		{
			return;
		}
		if (_context.IsNill)
		{
			SendValidationEvent(System.SR.Sch_ContentInNill, QNameString(_context.LocalName, _context.Namespace));
		}
		switch (_context.ElementDecl.ContentValidator.ContentType)
		{
		case XmlSchemaContentType.Empty:
			SendValidationEvent(System.SR.Sch_InvalidWhitespaceInEmpty, string.Empty);
			break;
		case XmlSchemaContentType.TextOnly:
			if (elementValueGetter != null)
			{
				SaveTextValue(elementValueGetter());
			}
			else
			{
				SaveTextValue(elementStringValue);
			}
			break;
		case XmlSchemaContentType.Mixed:
			if (_context.ElementDecl.DefaultValueTyped != null)
			{
				if (elementValueGetter != null)
				{
					SaveTextValue(elementValueGetter());
				}
				else
				{
					SaveTextValue(elementStringValue);
				}
			}
			break;
		case XmlSchemaContentType.ElementOnly:
			break;
		}
	}

	public object? ValidateEndElement(XmlSchemaInfo? schemaInfo)
	{
		return InternalValidateEndElement(schemaInfo, null);
	}

	public object? ValidateEndElement(XmlSchemaInfo? schemaInfo, object typedValue)
	{
		if (typedValue == null)
		{
			throw new ArgumentNullException("typedValue");
		}
		if (_textValue.Length > 0)
		{
			throw new InvalidOperationException(System.SR.Sch_InvalidEndElementCall);
		}
		return InternalValidateEndElement(schemaInfo, typedValue);
	}

	public void SkipToEndElement(XmlSchemaInfo? schemaInfo)
	{
		if (_validationStack.Length <= 1)
		{
			throw new InvalidOperationException(System.SR.Format(System.SR.Sch_InvalidEndElementMultiple, s_methodNames[10]));
		}
		CheckStateTransition(ValidatorState.SkipToEndElement, s_methodNames[10]);
		if (schemaInfo != null)
		{
			SchemaElementDecl elementDecl = _context.ElementDecl;
			if (elementDecl != null)
			{
				schemaInfo.SchemaType = elementDecl.SchemaType;
				schemaInfo.SchemaElement = GetSchemaElement();
			}
			else
			{
				schemaInfo.SchemaType = null;
				schemaInfo.SchemaElement = null;
			}
			schemaInfo.MemberType = null;
			schemaInfo.IsNil = _context.IsNill;
			schemaInfo.IsDefault = _context.IsDefault;
			schemaInfo.Validity = _context.Validity;
		}
		_context.ValidationSkipped = true;
		_currentState = ValidatorState.SkipToEndElement;
		Pop();
	}

	public void EndValidation()
	{
		if (_validationStack.Length > 1)
		{
			throw new InvalidOperationException(System.SR.Sch_InvalidEndValidation);
		}
		CheckStateTransition(ValidatorState.Finish, s_methodNames[11]);
		CheckForwardRefs();
	}

	public XmlSchemaParticle[] GetExpectedParticles()
	{
		if (_currentState == ValidatorState.Start || _currentState == ValidatorState.TopLevelTextOrWS)
		{
			if (_partialValidationType != null)
			{
				if (_partialValidationType is XmlSchemaElement xmlSchemaElement)
				{
					return new XmlSchemaParticle[1] { xmlSchemaElement };
				}
				return Array.Empty<XmlSchemaParticle>();
			}
			ICollection values = _schemaSet.GlobalElements.Values;
			ArrayList arrayList = new ArrayList(values.Count);
			foreach (XmlSchemaElement item in values)
			{
				ContentValidator.AddParticleToExpected(item, _schemaSet, arrayList, global: true);
			}
			return arrayList.ToArray(typeof(XmlSchemaParticle)) as XmlSchemaParticle[];
		}
		if (_context.ElementDecl != null)
		{
			ArrayList arrayList2 = _context.ElementDecl.ContentValidator.ExpectedParticles(_context, isRequiredOnly: false, _schemaSet);
			if (arrayList2 != null)
			{
				return arrayList2.ToArray(typeof(XmlSchemaParticle)) as XmlSchemaParticle[];
			}
		}
		return Array.Empty<XmlSchemaParticle>();
	}

	public XmlSchemaAttribute[] GetExpectedAttributes()
	{
		if (_currentState == ValidatorState.Element || _currentState == ValidatorState.Attribute)
		{
			SchemaElementDecl elementDecl = _context.ElementDecl;
			ArrayList arrayList = new ArrayList();
			if (elementDecl != null)
			{
				foreach (SchemaAttDef value in elementDecl.AttDefs.Values)
				{
					if (_attPresence[value.Name] == null)
					{
						arrayList.Add(value.SchemaAttribute);
					}
				}
			}
			if (_nsResolver.LookupPrefix(_nsXsi) != null)
			{
				AddXsiAttributes(arrayList);
			}
			return arrayList.ToArray(typeof(XmlSchemaAttribute)) as XmlSchemaAttribute[];
		}
		if (_currentState == ValidatorState.Start && _partialValidationType != null && _partialValidationType is XmlSchemaAttribute xmlSchemaAttribute)
		{
			return new XmlSchemaAttribute[1] { xmlSchemaAttribute };
		}
		return Array.Empty<XmlSchemaAttribute>();
	}

	internal void GetUnspecifiedDefaultAttributes(ArrayList defaultAttributes, bool createNodeData)
	{
		_currentState = ValidatorState.Attribute;
		SchemaElementDecl elementDecl = _context.ElementDecl;
		if (elementDecl == null || !elementDecl.HasDefaultAttribute)
		{
			return;
		}
		for (int i = 0; i < elementDecl.DefaultAttDefs.Count; i++)
		{
			SchemaAttDef schemaAttDef = (SchemaAttDef)elementDecl.DefaultAttDefs[i];
			if (_attPresence.Contains(schemaAttDef.Name) || schemaAttDef.DefaultValueTyped == null)
			{
				continue;
			}
			string text = _nameTable.Add(schemaAttDef.Name.Namespace);
			string text2 = string.Empty;
			if (text.Length > 0)
			{
				text2 = GetDefaultAttributePrefix(text);
				if (text2 == null || text2.Length == 0)
				{
					SendValidationEvent(System.SR.Sch_DefaultAttributeNotApplied, new string[2]
					{
						schemaAttDef.Name.ToString(),
						QNameString(_context.LocalName, _context.Namespace)
					});
					continue;
				}
			}
			XmlSchemaDatatype datatype = schemaAttDef.Datatype;
			if (createNodeData)
			{
				ValidatingReaderNodeData validatingReaderNodeData = new ValidatingReaderNodeData();
				validatingReaderNodeData.LocalName = _nameTable.Add(schemaAttDef.Name.Name);
				validatingReaderNodeData.Namespace = text;
				validatingReaderNodeData.Prefix = _nameTable.Add(text2);
				validatingReaderNodeData.NodeType = XmlNodeType.Attribute;
				AttributePSVIInfo attributePSVIInfo = new AttributePSVIInfo();
				XmlSchemaInfo attributeSchemaInfo = attributePSVIInfo.attributeSchemaInfo;
				if (schemaAttDef.Datatype.Variety == XmlSchemaDatatypeVariety.Union)
				{
					XsdSimpleValue xsdSimpleValue = schemaAttDef.DefaultValueTyped as XsdSimpleValue;
					attributeSchemaInfo.MemberType = xsdSimpleValue.XmlType;
					datatype = xsdSimpleValue.XmlType.Datatype;
					attributePSVIInfo.typedAttributeValue = xsdSimpleValue.TypedValue;
				}
				else
				{
					attributePSVIInfo.typedAttributeValue = schemaAttDef.DefaultValueTyped;
				}
				attributeSchemaInfo.IsDefault = true;
				attributeSchemaInfo.Validity = XmlSchemaValidity.Valid;
				attributeSchemaInfo.SchemaType = schemaAttDef.SchemaType;
				attributeSchemaInfo.SchemaAttribute = schemaAttDef.SchemaAttribute;
				validatingReaderNodeData.RawValue = attributeSchemaInfo.XmlType.ValueConverter.ToString(attributePSVIInfo.typedAttributeValue);
				validatingReaderNodeData.AttInfo = attributePSVIInfo;
				defaultAttributes.Add(validatingReaderNodeData);
			}
			else
			{
				defaultAttributes.Add(schemaAttDef.SchemaAttribute);
			}
			CheckTokenizedTypes(datatype, schemaAttDef.DefaultValueTyped, attrValue: true);
			if (HasIdentityConstraints)
			{
				AttributeIdentityConstraints(schemaAttDef.Name.Name, schemaAttDef.Name.Namespace, schemaAttDef.DefaultValueTyped, schemaAttDef.DefaultValueRaw, datatype);
			}
		}
	}

	internal void SetDtdSchemaInfo(IDtdInfo dtdSchemaInfo)
	{
		_dtdSchemaInfo = dtdSchemaInfo;
		_checkEntity = true;
	}

	internal string GetConcatenatedValue()
	{
		return _textValue.ToString();
	}

	private object InternalValidateEndElement(XmlSchemaInfo schemaInfo, object typedValue)
	{
		if (_validationStack.Length <= 1)
		{
			throw new InvalidOperationException(System.SR.Format(System.SR.Sch_InvalidEndElementMultiple, s_methodNames[9]));
		}
		CheckStateTransition(ValidatorState.EndElement, s_methodNames[9]);
		SchemaElementDecl elementDecl = _context.ElementDecl;
		XmlSchemaSimpleType memberType = null;
		XmlSchemaType schemaType = null;
		XmlSchemaElement schemaElement = null;
		string text = string.Empty;
		if (elementDecl != null)
		{
			if (_context.CheckRequiredAttribute && elementDecl.HasRequiredAttribute)
			{
				CheckRequiredAttributes(elementDecl);
			}
			if (!_context.IsNill && _context.NeedValidateChildren)
			{
				switch (elementDecl.ContentValidator.ContentType)
				{
				case XmlSchemaContentType.TextOnly:
					if (typedValue == null)
					{
						text = _textValue.ToString();
						typedValue = ValidateAtomicValue(text, out memberType);
					}
					else
					{
						typedValue = ValidateAtomicValue(typedValue, out memberType);
					}
					break;
				case XmlSchemaContentType.Mixed:
					if (elementDecl.DefaultValueTyped != null && typedValue == null)
					{
						text = _textValue.ToString();
						typedValue = CheckMixedValueConstraint(text);
					}
					break;
				case XmlSchemaContentType.ElementOnly:
					if (typedValue != null)
					{
						throw new InvalidOperationException(System.SR.Sch_InvalidEndElementCallTyped);
					}
					break;
				}
				if (!elementDecl.ContentValidator.CompleteValidation(_context))
				{
					CompleteValidationError(_context, _eventHandler, _nsResolver, _sourceUriString, _positionInfo.LineNumber, _positionInfo.LinePosition, _schemaSet);
					_context.Validity = XmlSchemaValidity.Invalid;
				}
			}
			if (HasIdentityConstraints)
			{
				XmlSchemaType xmlSchemaType = ((memberType == null) ? elementDecl.SchemaType : memberType);
				EndElementIdentityConstraints(typedValue, text, xmlSchemaType.Datatype);
			}
			schemaType = elementDecl.SchemaType;
			schemaElement = GetSchemaElement();
		}
		if (schemaInfo != null)
		{
			schemaInfo.SchemaType = schemaType;
			schemaInfo.SchemaElement = schemaElement;
			schemaInfo.MemberType = memberType;
			schemaInfo.IsNil = _context.IsNill;
			schemaInfo.IsDefault = _context.IsDefault;
			if (_context.Validity == XmlSchemaValidity.NotKnown && StrictlyAssessed)
			{
				_context.Validity = XmlSchemaValidity.Valid;
			}
			schemaInfo.Validity = _context.Validity;
		}
		Pop();
		return typedValue;
	}

	private void ProcessSchemaLocations(string xsiSchemaLocation, string xsiNoNamespaceSchemaLocation)
	{
		bool flag = false;
		if (xsiNoNamespaceSchemaLocation != null)
		{
			flag = true;
			LoadSchema(string.Empty, xsiNoNamespaceSchemaLocation);
		}
		if (xsiSchemaLocation != null)
		{
			object typedValue;
			Exception ex = s_dtStringArray.TryParseValue(xsiSchemaLocation, _nameTable, _nsResolver, out typedValue);
			if (ex != null)
			{
				SendValidationEvent(System.SR.Sch_InvalidValueDetailedAttribute, new string[4] { "schemaLocation", xsiSchemaLocation, s_dtStringArray.TypeCodeString, ex.Message }, ex);
				return;
			}
			string[] array = (string[])typedValue;
			flag = true;
			try
			{
				for (int i = 0; i < array.Length - 1; i += 2)
				{
					LoadSchema(array[i], array[i + 1]);
				}
			}
			catch (XmlSchemaException e)
			{
				SendValidationEvent(e);
			}
		}
		if (flag)
		{
			RecompileSchemaSet();
		}
	}

	private object ValidateElementContext(XmlQualifiedName elementName, out bool invalidElementInContext)
	{
		object obj = null;
		int errorCode = 0;
		XmlSchemaElement xmlSchemaElement = null;
		invalidElementInContext = false;
		if (_context.NeedValidateChildren)
		{
			if (_context.IsNill)
			{
				SendValidationEvent(System.SR.Sch_ContentInNill, QNameString(_context.LocalName, _context.Namespace));
				return null;
			}
			ContentValidator contentValidator = _context.ElementDecl.ContentValidator;
			if (contentValidator.ContentType == XmlSchemaContentType.Mixed && _context.ElementDecl.Presence == SchemaDeclBase.Use.Fixed)
			{
				SendValidationEvent(System.SR.Sch_ElementInMixedWithFixed, QNameString(_context.LocalName, _context.Namespace));
				return null;
			}
			XmlQualifiedName xmlQualifiedName = elementName;
			bool flag = false;
			while (true)
			{
				obj = _context.ElementDecl.ContentValidator.ValidateElement(xmlQualifiedName, _context, out errorCode);
				if (obj != null)
				{
					break;
				}
				if (errorCode == -2)
				{
					SendValidationEvent(System.SR.Sch_AllElement, elementName.ToString());
					invalidElementInContext = true;
					_processContents = (_context.ProcessContents = XmlSchemaContentProcessing.Skip);
					return null;
				}
				flag = true;
				xmlSchemaElement = GetSubstitutionGroupHead(xmlQualifiedName);
				if (xmlSchemaElement == null)
				{
					break;
				}
				xmlQualifiedName = xmlSchemaElement.QualifiedName;
			}
			if (flag)
			{
				if (!(obj is XmlSchemaElement xmlSchemaElement2))
				{
					obj = null;
				}
				else if (xmlSchemaElement2.RefName.IsEmpty)
				{
					SendValidationEvent(System.SR.Sch_InvalidElementSubstitution, BuildElementName(elementName), BuildElementName(xmlSchemaElement2.QualifiedName));
					invalidElementInContext = true;
					_processContents = (_context.ProcessContents = XmlSchemaContentProcessing.Skip);
				}
				else
				{
					obj = _compiledSchemaInfo.GetElement(elementName);
					_context.NeedValidateChildren = true;
				}
			}
			if (obj == null)
			{
				ElementValidationError(elementName, _context, _eventHandler, _nsResolver, _sourceUriString, _positionInfo.LineNumber, _positionInfo.LinePosition, _schemaSet);
				invalidElementInContext = true;
				_processContents = (_context.ProcessContents = XmlSchemaContentProcessing.Skip);
			}
		}
		return obj;
	}

	private XmlSchemaElement GetSubstitutionGroupHead(XmlQualifiedName member)
	{
		XmlSchemaElement element = _compiledSchemaInfo.GetElement(member);
		if (element != null)
		{
			XmlQualifiedName substitutionGroup = element.SubstitutionGroup;
			if (!substitutionGroup.IsEmpty)
			{
				XmlSchemaElement element2 = _compiledSchemaInfo.GetElement(substitutionGroup);
				if (element2 != null)
				{
					if ((element2.BlockResolved & XmlSchemaDerivationMethod.Substitution) != 0)
					{
						SendValidationEvent(System.SR.Sch_SubstitutionNotAllowed, new string[2]
						{
							member.ToString(),
							substitutionGroup.ToString()
						});
						return null;
					}
					if (!XmlSchemaType.IsDerivedFrom(element.ElementSchemaType, element2.ElementSchemaType, element2.BlockResolved))
					{
						SendValidationEvent(System.SR.Sch_SubstitutionBlocked, new string[2]
						{
							member.ToString(),
							substitutionGroup.ToString()
						});
						return null;
					}
					return element2;
				}
			}
		}
		return null;
	}

	private object ValidateAtomicValue(string stringValue, out XmlSchemaSimpleType memberType)
	{
		object typedValue = null;
		memberType = null;
		if (!_context.IsNill)
		{
			SchemaElementDecl elementDecl = _context.ElementDecl;
			if (stringValue.Length == 0 && elementDecl.DefaultValueTyped != null)
			{
				SchemaElementDecl elementDeclBeforeXsi = _context.ElementDeclBeforeXsi;
				if (elementDeclBeforeXsi != null && elementDeclBeforeXsi != elementDecl)
				{
					Exception ex = elementDecl.Datatype.TryParseValue(elementDecl.DefaultValueRaw, _nameTable, _nsResolver, out typedValue);
					if (ex != null)
					{
						SendValidationEvent(System.SR.Sch_InvalidElementDefaultValue, new string[2]
						{
							elementDecl.DefaultValueRaw,
							QNameString(_context.LocalName, _context.Namespace)
						});
					}
					else
					{
						_context.IsDefault = true;
					}
				}
				else
				{
					_context.IsDefault = true;
					typedValue = elementDecl.DefaultValueTyped;
				}
			}
			else
			{
				typedValue = CheckElementValue(stringValue);
			}
			XsdSimpleValue xsdSimpleValue = typedValue as XsdSimpleValue;
			XmlSchemaDatatype datatype = elementDecl.Datatype;
			if (xsdSimpleValue != null)
			{
				memberType = xsdSimpleValue.XmlType;
				typedValue = xsdSimpleValue.TypedValue;
				datatype = memberType.Datatype;
			}
			CheckTokenizedTypes(datatype, typedValue, attrValue: false);
		}
		return typedValue;
	}

	private object ValidateAtomicValue(object parsedValue, out XmlSchemaSimpleType memberType)
	{
		object typedValue = null;
		memberType = null;
		if (!_context.IsNill)
		{
			SchemaElementDecl elementDecl = _context.ElementDecl;
			SchemaDeclBase schemaDeclBase = elementDecl;
			XmlSchemaDatatype datatype = elementDecl.Datatype;
			Exception ex = datatype.TryParseValue(parsedValue, _nameTable, _nsResolver, out typedValue);
			if (ex != null)
			{
				string text = parsedValue as string;
				if (text == null)
				{
					text = XmlSchemaDatatype.ConcatenatedToString(parsedValue);
				}
				SendValidationEvent(System.SR.Sch_ElementValueDataTypeDetailed, new string[4]
				{
					QNameString(_context.LocalName, _context.Namespace),
					text,
					GetTypeName(schemaDeclBase),
					ex.Message
				}, ex);
				return null;
			}
			if (!schemaDeclBase.CheckValue(typedValue))
			{
				SendValidationEvent(System.SR.Sch_FixedElementValue, QNameString(_context.LocalName, _context.Namespace));
			}
			if (datatype.Variety == XmlSchemaDatatypeVariety.Union)
			{
				XsdSimpleValue xsdSimpleValue = typedValue as XsdSimpleValue;
				memberType = xsdSimpleValue.XmlType;
				typedValue = xsdSimpleValue.TypedValue;
				datatype = memberType.Datatype;
			}
			CheckTokenizedTypes(datatype, typedValue, attrValue: false);
		}
		return typedValue;
	}

	private string GetTypeName(SchemaDeclBase decl)
	{
		string text = decl.SchemaType.QualifiedName.ToString();
		if (text.Length == 0)
		{
			text = decl.Datatype.TypeCodeString;
		}
		return text;
	}

	private void SaveTextValue(object value)
	{
		string value2 = value.ToString();
		_textValue.Append(value2);
	}

	[MemberNotNull("_context")]
	private void Push(XmlQualifiedName elementName)
	{
		_context = (ValidationState)_validationStack.Push();
		if (_context == null)
		{
			_context = new ValidationState();
			_validationStack.AddToTop(_context);
		}
		_context.LocalName = elementName.Name;
		_context.Namespace = elementName.Namespace;
		_context.HasMatched = false;
		_context.IsNill = false;
		_context.IsDefault = false;
		_context.CheckRequiredAttribute = true;
		_context.ValidationSkipped = false;
		_context.Validity = XmlSchemaValidity.NotKnown;
		_context.NeedValidateChildren = false;
		_context.ProcessContents = _processContents;
		_context.ElementDeclBeforeXsi = null;
		_context.Constr = null;
	}

	private void Pop()
	{
		ValidationState validationState = (ValidationState)_validationStack.Pop();
		if (_startIDConstraint == _validationStack.Length)
		{
			_startIDConstraint = -1;
		}
		_context = (ValidationState)_validationStack.Peek();
		if (validationState.Validity == XmlSchemaValidity.Invalid)
		{
			_context.Validity = XmlSchemaValidity.Invalid;
		}
		if (validationState.ValidationSkipped)
		{
			_context.ValidationSkipped = true;
		}
		_processContents = _context.ProcessContents;
	}

	private void AddXsiAttributes(ArrayList attList)
	{
		BuildXsiAttributes();
		if (_attPresence[s_xsiTypeSO.QualifiedName] == null)
		{
			attList.Add(s_xsiTypeSO);
		}
		if (_attPresence[s_xsiNilSO.QualifiedName] == null)
		{
			attList.Add(s_xsiNilSO);
		}
		if (_attPresence[s_xsiSLSO.QualifiedName] == null)
		{
			attList.Add(s_xsiSLSO);
		}
		if (_attPresence[s_xsiNoNsSLSO.QualifiedName] == null)
		{
			attList.Add(s_xsiNoNsSLSO);
		}
	}

	private SchemaElementDecl FastGetElementDecl(XmlQualifiedName elementName, object particle)
	{
		SchemaElementDecl schemaElementDecl = null;
		if (particle != null)
		{
			if (particle is XmlSchemaElement xmlSchemaElement)
			{
				schemaElementDecl = xmlSchemaElement.ElementDecl;
			}
			else
			{
				XmlSchemaAny xmlSchemaAny = (XmlSchemaAny)particle;
				_processContents = xmlSchemaAny.ProcessContentsCorrect;
			}
		}
		if (schemaElementDecl == null && _processContents != XmlSchemaContentProcessing.Skip)
		{
			if (_isRoot && _partialValidationType != null)
			{
				if (_partialValidationType is XmlSchemaElement)
				{
					XmlSchemaElement xmlSchemaElement2 = (XmlSchemaElement)_partialValidationType;
					if (elementName.Equals(xmlSchemaElement2.QualifiedName))
					{
						schemaElementDecl = xmlSchemaElement2.ElementDecl;
					}
					else
					{
						SendValidationEvent(System.SR.Sch_SchemaElementNameMismatch, elementName.ToString(), xmlSchemaElement2.QualifiedName.ToString());
					}
				}
				else if (_partialValidationType is XmlSchemaType)
				{
					XmlSchemaType xmlSchemaType = (XmlSchemaType)_partialValidationType;
					schemaElementDecl = xmlSchemaType.ElementDecl;
				}
				else
				{
					SendValidationEvent(System.SR.Sch_ValidateElementInvalidCall, string.Empty);
				}
			}
			else
			{
				schemaElementDecl = _compiledSchemaInfo.GetElementDecl(elementName);
			}
		}
		return schemaElementDecl;
	}

	private SchemaElementDecl CheckXsiTypeAndNil(SchemaElementDecl elementDecl, string xsiType, string xsiNil, ref bool declFound)
	{
		XmlQualifiedName xmlQualifiedName = XmlQualifiedName.Empty;
		if (xsiType != null)
		{
			object typedValue = null;
			Exception ex = s_dtQName.TryParseValue(xsiType, _nameTable, _nsResolver, out typedValue);
			if (ex != null)
			{
				SendValidationEvent(System.SR.Sch_InvalidValueDetailedAttribute, new string[4] { "type", xsiType, s_dtQName.TypeCodeString, ex.Message }, ex);
			}
			else
			{
				xmlQualifiedName = typedValue as XmlQualifiedName;
			}
		}
		if (elementDecl != null)
		{
			if (elementDecl.IsNillable)
			{
				if (xsiNil != null)
				{
					_context.IsNill = XmlConvert.ToBoolean(xsiNil);
					if (_context.IsNill && elementDecl.Presence == SchemaDeclBase.Use.Fixed)
					{
						SendValidationEvent(System.SR.Sch_XsiNilAndFixed);
					}
				}
			}
			else if (xsiNil != null)
			{
				SendValidationEvent(System.SR.Sch_InvalidXsiNill);
			}
		}
		if (xmlQualifiedName.IsEmpty)
		{
			if (elementDecl != null && elementDecl.IsAbstract)
			{
				SendValidationEvent(System.SR.Sch_AbstractElement, QNameString(_context.LocalName, _context.Namespace));
				elementDecl = null;
			}
		}
		else
		{
			SchemaElementDecl schemaElementDecl = _compiledSchemaInfo.GetTypeDecl(xmlQualifiedName);
			XmlSeverityType severity = XmlSeverityType.Warning;
			if (HasSchema && _processContents == XmlSchemaContentProcessing.Strict)
			{
				severity = XmlSeverityType.Error;
			}
			if (schemaElementDecl == null && xmlQualifiedName.Namespace == _nsXs)
			{
				XmlSchemaType xmlSchemaType = DatatypeImplementation.GetSimpleTypeFromXsdType(xmlQualifiedName);
				if (xmlSchemaType == null)
				{
					xmlSchemaType = XmlSchemaType.GetBuiltInComplexType(xmlQualifiedName);
				}
				if (xmlSchemaType != null)
				{
					schemaElementDecl = xmlSchemaType.ElementDecl;
				}
			}
			if (schemaElementDecl == null)
			{
				SendValidationEvent(System.SR.Sch_XsiTypeNotFound, xmlQualifiedName.ToString(), severity);
				elementDecl = null;
			}
			else
			{
				declFound = true;
				if (schemaElementDecl.IsAbstract)
				{
					SendValidationEvent(System.SR.Sch_XsiTypeAbstract, xmlQualifiedName.ToString(), severity);
					elementDecl = null;
				}
				else if (elementDecl != null && !XmlSchemaType.IsDerivedFrom(schemaElementDecl.SchemaType, elementDecl.SchemaType, elementDecl.Block))
				{
					SendValidationEvent(System.SR.Sch_XsiTypeBlockedEx, new string[2]
					{
						xmlQualifiedName.ToString(),
						QNameString(_context.LocalName, _context.Namespace)
					});
					elementDecl = null;
				}
				else
				{
					if (elementDecl != null)
					{
						schemaElementDecl = schemaElementDecl.Clone();
						schemaElementDecl.Constraints = elementDecl.Constraints;
						schemaElementDecl.DefaultValueRaw = elementDecl.DefaultValueRaw;
						schemaElementDecl.DefaultValueTyped = elementDecl.DefaultValueTyped;
						schemaElementDecl.Block = elementDecl.Block;
					}
					_context.ElementDeclBeforeXsi = elementDecl;
					elementDecl = schemaElementDecl;
				}
			}
		}
		return elementDecl;
	}

	private void ThrowDeclNotFoundWarningOrError(bool declFound)
	{
		if (declFound)
		{
			_processContents = (_context.ProcessContents = XmlSchemaContentProcessing.Skip);
			_context.NeedValidateChildren = false;
		}
		else if (HasSchema && _processContents == XmlSchemaContentProcessing.Strict)
		{
			_processContents = (_context.ProcessContents = XmlSchemaContentProcessing.Skip);
			_context.NeedValidateChildren = false;
			SendValidationEvent(System.SR.Sch_UndeclaredElement, QNameString(_context.LocalName, _context.Namespace));
		}
		else
		{
			SendValidationEvent(System.SR.Sch_NoElementSchemaFound, QNameString(_context.LocalName, _context.Namespace), XmlSeverityType.Warning);
		}
	}

	private void CheckElementProperties()
	{
		if (_context.ElementDecl.IsAbstract)
		{
			SendValidationEvent(System.SR.Sch_AbstractElement, QNameString(_context.LocalName, _context.Namespace));
		}
	}

	private void ValidateStartElementIdentityConstraints()
	{
		if (ProcessIdentityConstraints && _context.ElementDecl.Constraints != null)
		{
			AddIdentityConstraints();
		}
		if (HasIdentityConstraints)
		{
			ElementIdentityConstraints();
		}
	}

	private SchemaAttDef CheckIsXmlAttribute(XmlQualifiedName attQName)
	{
		SchemaAttDef value = null;
		if (Ref.Equal(attQName.Namespace, _nsXml) && (_validationFlags & XmlSchemaValidationFlags.AllowXmlAttributes) != 0)
		{
			if (!_compiledSchemaInfo.Contains(_nsXml))
			{
				AddXmlNamespaceSchema();
			}
			_compiledSchemaInfo.AttributeDecls.TryGetValue(attQName, out value);
		}
		return value;
	}

	private void AddXmlNamespaceSchema()
	{
		XmlSchemaSet xmlSchemaSet = new XmlSchemaSet();
		xmlSchemaSet.Add(Preprocessor.GetBuildInSchema());
		xmlSchemaSet.Compile();
		_schemaSet.Add(xmlSchemaSet);
		RecompileSchemaSet();
	}

	internal object CheckMixedValueConstraint(string elementValue)
	{
		SchemaElementDecl elementDecl = _context.ElementDecl;
		if (_context.IsNill)
		{
			return null;
		}
		if (elementValue.Length == 0)
		{
			_context.IsDefault = true;
			return elementDecl.DefaultValueTyped;
		}
		SchemaDeclBase schemaDeclBase = elementDecl;
		if (schemaDeclBase.Presence == SchemaDeclBase.Use.Fixed && !elementValue.Equals(elementDecl.DefaultValueRaw))
		{
			SendValidationEvent(System.SR.Sch_FixedElementValue, elementDecl.Name.ToString());
		}
		return elementValue;
	}

	private void LoadSchema(string uri, string url)
	{
		XmlReader xmlReader = null;
		try
		{
			Uri uri2 = _xmlResolver.ResolveUri(_sourceUri, url);
			Stream input = (Stream)_xmlResolver.GetEntity(uri2, null, null);
			XmlReaderSettings readerSettings = _schemaSet.ReaderSettings;
			readerSettings.CloseInput = true;
			readerSettings.XmlResolver = _xmlResolver;
			xmlReader = XmlReader.Create(input, readerSettings, uri2.ToString());
			_schemaSet.Add(uri, xmlReader, _validatedNamespaces);
			while (xmlReader.Read())
			{
			}
		}
		catch (XmlSchemaException ex)
		{
			SendValidationEvent(System.SR.Sch_CannotLoadSchema, new string[2] { uri, ex.Message }, ex);
		}
		catch (Exception ex2)
		{
			SendValidationEvent(System.SR.Sch_CannotLoadSchema, new string[2] { uri, ex2.Message }, ex2, XmlSeverityType.Warning);
		}
		finally
		{
			xmlReader?.Close();
		}
	}

	[MemberNotNull("_compiledSchemaInfo")]
	internal void RecompileSchemaSet()
	{
		if (!_schemaSet.IsCompiled)
		{
			try
			{
				_schemaSet.Compile();
			}
			catch (XmlSchemaException e)
			{
				SendValidationEvent(e);
			}
		}
		_compiledSchemaInfo = _schemaSet.CompiledInfo;
	}

	private void ProcessTokenizedType(XmlTokenizedType ttype, string name, bool attrValue)
	{
		switch (ttype)
		{
		case XmlTokenizedType.ID:
			if (!ProcessIdentityConstraints)
			{
				break;
			}
			if (FindId(name) != null)
			{
				if (attrValue)
				{
					_attrValid = false;
				}
				SendValidationEvent(System.SR.Sch_DupId, name);
			}
			else
			{
				if (_IDs == null)
				{
					_IDs = new Hashtable();
				}
				_IDs.Add(name, _context.LocalName);
			}
			break;
		case XmlTokenizedType.IDREF:
			if (ProcessIdentityConstraints)
			{
				object obj = FindId(name);
				if (obj == null)
				{
					_idRefListHead = new IdRefNode(_idRefListHead, name, _positionInfo.LineNumber, _positionInfo.LinePosition);
				}
			}
			break;
		case XmlTokenizedType.ENTITY:
			ProcessEntity(name);
			break;
		case XmlTokenizedType.IDREFS:
			break;
		}
	}

	private object CheckAttributeValue(object value, SchemaAttDef attdef)
	{
		object typedValue = null;
		XmlSchemaDatatype datatype = attdef.Datatype;
		string text = value as string;
		Exception ex = null;
		if (text != null)
		{
			ex = datatype.TryParseValue(text, _nameTable, _nsResolver, out typedValue);
			if (ex == null)
			{
				goto IL_0050;
			}
		}
		else
		{
			ex = datatype.TryParseValue(value, _nameTable, _nsResolver, out typedValue);
			if (ex == null)
			{
				goto IL_0050;
			}
		}
		_attrValid = false;
		if (text == null)
		{
			text = XmlSchemaDatatype.ConcatenatedToString(value);
		}
		SendValidationEvent(System.SR.Sch_AttributeValueDataTypeDetailed, new string[4]
		{
			attdef.Name.ToString(),
			text,
			GetTypeName(attdef),
			ex.Message
		}, ex);
		return null;
		IL_0050:
		if (!attdef.CheckValue(typedValue))
		{
			_attrValid = false;
			SendValidationEvent(System.SR.Sch_FixedAttributeValue, attdef.Name.ToString());
		}
		return typedValue;
	}

	private object CheckElementValue(string stringValue)
	{
		object typedValue = null;
		SchemaDeclBase elementDecl = _context.ElementDecl;
		XmlSchemaDatatype datatype = elementDecl.Datatype;
		Exception ex = datatype.TryParseValue(stringValue, _nameTable, _nsResolver, out typedValue);
		if (ex != null)
		{
			SendValidationEvent(System.SR.Sch_ElementValueDataTypeDetailed, new string[4]
			{
				QNameString(_context.LocalName, _context.Namespace),
				stringValue,
				GetTypeName(elementDecl),
				ex.Message
			}, ex);
			return null;
		}
		if (!elementDecl.CheckValue(typedValue))
		{
			SendValidationEvent(System.SR.Sch_FixedElementValue, QNameString(_context.LocalName, _context.Namespace));
		}
		return typedValue;
	}

	private void CheckTokenizedTypes(XmlSchemaDatatype dtype, object typedValue, bool attrValue)
	{
		if (typedValue == null)
		{
			return;
		}
		XmlTokenizedType tokenizedType = dtype.TokenizedType;
		if (tokenizedType != XmlTokenizedType.ENTITY && tokenizedType != XmlTokenizedType.ID && tokenizedType != XmlTokenizedType.IDREF)
		{
			return;
		}
		if (dtype.Variety == XmlSchemaDatatypeVariety.List)
		{
			string[] array = (string[])typedValue;
			for (int i = 0; i < array.Length; i++)
			{
				ProcessTokenizedType(dtype.TokenizedType, array[i], attrValue);
			}
		}
		else
		{
			ProcessTokenizedType(dtype.TokenizedType, (string)typedValue, attrValue);
		}
	}

	private object FindId(string name)
	{
		if (_IDs != null)
		{
			return _IDs[name];
		}
		return null;
	}

	private void CheckForwardRefs()
	{
		IdRefNode idRefNode = _idRefListHead;
		while (idRefNode != null)
		{
			if (FindId(idRefNode.Id) == null)
			{
				SendValidationEvent(new XmlSchemaValidationException(System.SR.Sch_UndeclaredId, idRefNode.Id, _sourceUriString, idRefNode.LineNo, idRefNode.LinePos), XmlSeverityType.Error);
			}
			IdRefNode next = idRefNode.Next;
			idRefNode.Next = null;
			idRefNode = next;
		}
		_idRefListHead = null;
	}

	private void CheckStateTransition(ValidatorState toState, string methodName)
	{
		if (!ValidStates[(int)_currentState, (int)toState])
		{
			object[] args;
			if (_currentState == ValidatorState.None)
			{
				string sch_InvalidStartTransition = System.SR.Sch_InvalidStartTransition;
				args = new string[2]
				{
					methodName,
					s_methodNames[1]
				};
				throw new InvalidOperationException(System.SR.Format(sch_InvalidStartTransition, args));
			}
			string sch_InvalidStateTransition = System.SR.Sch_InvalidStateTransition;
			args = new string[2]
			{
				s_methodNames[(int)_currentState],
				methodName
			};
			throw new InvalidOperationException(System.SR.Format(sch_InvalidStateTransition, args));
		}
		_currentState = toState;
	}

	private void ClearPSVI()
	{
		if (_textValue != null)
		{
			_textValue.Length = 0;
		}
		_attPresence.Clear();
		_wildID = null;
	}

	private void CheckRequiredAttributes(SchemaElementDecl currentElementDecl)
	{
		Dictionary<XmlQualifiedName, SchemaAttDef> attDefs = currentElementDecl.AttDefs;
		foreach (SchemaAttDef value in attDefs.Values)
		{
			if (_attPresence[value.Name] == null && (value.Presence == SchemaDeclBase.Use.Required || value.Presence == SchemaDeclBase.Use.RequiredFixed))
			{
				SendValidationEvent(System.SR.Sch_MissRequiredAttribute, value.Name.ToString());
			}
		}
	}

	private XmlSchemaElement GetSchemaElement()
	{
		SchemaElementDecl elementDeclBeforeXsi = _context.ElementDeclBeforeXsi;
		SchemaElementDecl elementDecl = _context.ElementDecl;
		if (elementDeclBeforeXsi != null && elementDeclBeforeXsi.SchemaElement != null)
		{
			XmlSchemaElement xmlSchemaElement = (XmlSchemaElement)elementDeclBeforeXsi.SchemaElement.Clone(null);
			xmlSchemaElement.SchemaTypeName = XmlQualifiedName.Empty;
			xmlSchemaElement.SchemaType = elementDecl.SchemaType;
			xmlSchemaElement.SetElementType(elementDecl.SchemaType);
			xmlSchemaElement.ElementDecl = elementDecl;
			return xmlSchemaElement;
		}
		return elementDecl.SchemaElement;
	}

	internal string GetDefaultAttributePrefix(string attributeNS)
	{
		IDictionary<string, string> namespacesInScope = _nsResolver.GetNamespacesInScope(XmlNamespaceScope.All);
		string text = null;
		foreach (KeyValuePair<string, string> item in namespacesInScope)
		{
			string strA = _nameTable.Add(item.Value);
			if (Ref.Equal(strA, attributeNS))
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

	private void AddIdentityConstraints()
	{
		SchemaElementDecl elementDecl = _context.ElementDecl;
		_context.Constr = new ConstraintStruct[elementDecl.Constraints.Length];
		int num = 0;
		for (int i = 0; i < elementDecl.Constraints.Length; i++)
		{
			_context.Constr[num++] = new ConstraintStruct(elementDecl.Constraints[i]);
		}
		for (int j = 0; j < _context.Constr.Length; j++)
		{
			if (_context.Constr[j].constraint.Role != CompiledIdentityConstraint.ConstraintRole.Keyref)
			{
				continue;
			}
			bool flag = false;
			for (int num2 = _validationStack.Length - 1; num2 >= ((_startIDConstraint >= 0) ? _startIDConstraint : (_validationStack.Length - 1)); num2--)
			{
				if (((ValidationState)_validationStack[num2]).Constr != null)
				{
					ConstraintStruct[] constr = ((ValidationState)_validationStack[num2]).Constr;
					for (int k = 0; k < constr.Length; k++)
					{
						if (constr[k].constraint.name == _context.Constr[j].constraint.refer)
						{
							flag = true;
							if (constr[k].keyrefTable == null)
							{
								constr[k].keyrefTable = new Hashtable();
							}
							_context.Constr[j].qualifiedTable = constr[k].keyrefTable;
							break;
						}
					}
					if (flag)
					{
						break;
					}
				}
			}
			if (!flag)
			{
				SendValidationEvent(System.SR.Sch_RefNotInScope, QNameString(_context.LocalName, _context.Namespace));
			}
		}
		if (_startIDConstraint == -1)
		{
			_startIDConstraint = _validationStack.Length - 1;
		}
	}

	private void ElementIdentityConstraints()
	{
		SchemaElementDecl elementDecl = _context.ElementDecl;
		string localName = _context.LocalName;
		string @namespace = _context.Namespace;
		for (int i = _startIDConstraint; i < _validationStack.Length; i++)
		{
			if (((ValidationState)_validationStack[i]).Constr == null)
			{
				continue;
			}
			ConstraintStruct[] constr = ((ValidationState)_validationStack[i]).Constr;
			for (int j = 0; j < constr.Length; j++)
			{
				if (constr[j].axisSelector.MoveToStartElement(localName, @namespace))
				{
					constr[j].axisSelector.PushKS(_positionInfo.LineNumber, _positionInfo.LinePosition);
				}
				for (int k = 0; k < constr[j].axisFields.Count; k++)
				{
					LocatedActiveAxis locatedActiveAxis = (LocatedActiveAxis)constr[j].axisFields[k];
					if (locatedActiveAxis.MoveToStartElement(localName, @namespace) && elementDecl != null)
					{
						if (elementDecl.Datatype == null || elementDecl.ContentValidator.ContentType == XmlSchemaContentType.Mixed)
						{
							SendValidationEvent(System.SR.Sch_FieldSimpleTypeExpected, localName);
						}
						else
						{
							locatedActiveAxis.isMatched = true;
						}
					}
				}
			}
		}
	}

	private void AttributeIdentityConstraints(string name, string ns, object obj, string sobj, XmlSchemaDatatype datatype)
	{
		for (int i = _startIDConstraint; i < _validationStack.Length; i++)
		{
			if (((ValidationState)_validationStack[i]).Constr == null)
			{
				continue;
			}
			ConstraintStruct[] constr = ((ValidationState)_validationStack[i]).Constr;
			for (int j = 0; j < constr.Length; j++)
			{
				for (int k = 0; k < constr[j].axisFields.Count; k++)
				{
					LocatedActiveAxis locatedActiveAxis = (LocatedActiveAxis)constr[j].axisFields[k];
					if (locatedActiveAxis.MoveToAttribute(name, ns))
					{
						if (locatedActiveAxis.Ks[locatedActiveAxis.Column] != null)
						{
							SendValidationEvent(System.SR.Sch_FieldSingleValueExpected, name);
						}
						else
						{
							locatedActiveAxis.Ks[locatedActiveAxis.Column] = new TypedObject(obj, sobj, datatype);
						}
					}
				}
			}
		}
	}

	private void EndElementIdentityConstraints(object typedValue, string stringValue, XmlSchemaDatatype datatype)
	{
		string localName = _context.LocalName;
		string @namespace = _context.Namespace;
		for (int num = _validationStack.Length - 1; num >= _startIDConstraint; num--)
		{
			if (((ValidationState)_validationStack[num]).Constr != null)
			{
				ConstraintStruct[] constr = ((ValidationState)_validationStack[num]).Constr;
				for (int i = 0; i < constr.Length; i++)
				{
					for (int j = 0; j < constr[i].axisFields.Count; j++)
					{
						LocatedActiveAxis locatedActiveAxis = (LocatedActiveAxis)constr[i].axisFields[j];
						if (locatedActiveAxis.isMatched)
						{
							locatedActiveAxis.isMatched = false;
							if (locatedActiveAxis.Ks[locatedActiveAxis.Column] != null)
							{
								SendValidationEvent(System.SR.Sch_FieldSingleValueExpected, localName);
							}
							else if (System.LocalAppContextSwitches.IgnoreEmptyKeySequences)
							{
								if (typedValue != null && stringValue.Length != 0)
								{
									locatedActiveAxis.Ks[locatedActiveAxis.Column] = new TypedObject(typedValue, stringValue, datatype);
								}
							}
							else if (typedValue != null)
							{
								locatedActiveAxis.Ks[locatedActiveAxis.Column] = new TypedObject(typedValue, stringValue, datatype);
							}
						}
						locatedActiveAxis.EndElement(localName, @namespace);
					}
					if (!constr[i].axisSelector.EndElement(localName, @namespace))
					{
						continue;
					}
					KeySequence keySequence = constr[i].axisSelector.PopKS();
					switch (constr[i].constraint.Role)
					{
					case CompiledIdentityConstraint.ConstraintRole.Key:
						if (!keySequence.IsQualified())
						{
							SendValidationEvent(new XmlSchemaValidationException(System.SR.Sch_MissingKey, constr[i].constraint.name.ToString(), _sourceUriString, keySequence.PosLine, keySequence.PosCol));
						}
						else if (constr[i].qualifiedTable.Contains(keySequence))
						{
							SendValidationEvent(new XmlSchemaValidationException(System.SR.Sch_DuplicateKey, new string[2]
							{
								keySequence.ToString(),
								constr[i].constraint.name.ToString()
							}, _sourceUriString, keySequence.PosLine, keySequence.PosCol));
						}
						else
						{
							constr[i].qualifiedTable.Add(keySequence, keySequence);
						}
						break;
					case CompiledIdentityConstraint.ConstraintRole.Unique:
						if (keySequence.IsQualified())
						{
							if (constr[i].qualifiedTable.Contains(keySequence))
							{
								SendValidationEvent(new XmlSchemaValidationException(System.SR.Sch_DuplicateKey, new string[2]
								{
									keySequence.ToString(),
									constr[i].constraint.name.ToString()
								}, _sourceUriString, keySequence.PosLine, keySequence.PosCol));
							}
							else
							{
								constr[i].qualifiedTable.Add(keySequence, keySequence);
							}
						}
						break;
					case CompiledIdentityConstraint.ConstraintRole.Keyref:
						if (constr[i].qualifiedTable != null && keySequence.IsQualified() && !constr[i].qualifiedTable.Contains(keySequence))
						{
							constr[i].qualifiedTable.Add(keySequence, keySequence);
						}
						break;
					}
				}
			}
		}
		ConstraintStruct[] constr2 = ((ValidationState)_validationStack[_validationStack.Length - 1]).Constr;
		if (constr2 == null)
		{
			return;
		}
		for (int k = 0; k < constr2.Length; k++)
		{
			if (constr2[k].constraint.Role == CompiledIdentityConstraint.ConstraintRole.Keyref || constr2[k].keyrefTable == null)
			{
				continue;
			}
			foreach (KeySequence key in constr2[k].keyrefTable.Keys)
			{
				if (!constr2[k].qualifiedTable.Contains(key))
				{
					SendValidationEvent(new XmlSchemaValidationException(System.SR.Sch_UnresolvedKeyref, new string[2]
					{
						key.ToString(),
						constr2[k].constraint.name.ToString()
					}, _sourceUriString, key.PosLine, key.PosCol));
				}
			}
		}
	}

	private static void BuildXsiAttributes()
	{
		if (s_xsiTypeSO == null)
		{
			XmlSchemaAttribute xmlSchemaAttribute = new XmlSchemaAttribute();
			xmlSchemaAttribute.Name = "type";
			xmlSchemaAttribute.SetQualifiedName(new XmlQualifiedName("type", "http://www.w3.org/2001/XMLSchema-instance"));
			xmlSchemaAttribute.SetAttributeType(XmlSchemaType.GetBuiltInSimpleType(XmlTypeCode.QName));
			Interlocked.CompareExchange(ref s_xsiTypeSO, xmlSchemaAttribute, null);
		}
		if (s_xsiNilSO == null)
		{
			XmlSchemaAttribute xmlSchemaAttribute2 = new XmlSchemaAttribute();
			xmlSchemaAttribute2.Name = "nil";
			xmlSchemaAttribute2.SetQualifiedName(new XmlQualifiedName("nil", "http://www.w3.org/2001/XMLSchema-instance"));
			xmlSchemaAttribute2.SetAttributeType(XmlSchemaType.GetBuiltInSimpleType(XmlTypeCode.Boolean));
			Interlocked.CompareExchange(ref s_xsiNilSO, xmlSchemaAttribute2, null);
		}
		if (s_xsiSLSO == null)
		{
			XmlSchemaSimpleType builtInSimpleType = XmlSchemaType.GetBuiltInSimpleType(XmlTypeCode.String);
			XmlSchemaAttribute xmlSchemaAttribute3 = new XmlSchemaAttribute();
			xmlSchemaAttribute3.Name = "schemaLocation";
			xmlSchemaAttribute3.SetQualifiedName(new XmlQualifiedName("schemaLocation", "http://www.w3.org/2001/XMLSchema-instance"));
			xmlSchemaAttribute3.SetAttributeType(builtInSimpleType);
			Interlocked.CompareExchange(ref s_xsiSLSO, xmlSchemaAttribute3, null);
		}
		if (s_xsiNoNsSLSO == null)
		{
			XmlSchemaSimpleType builtInSimpleType2 = XmlSchemaType.GetBuiltInSimpleType(XmlTypeCode.String);
			XmlSchemaAttribute xmlSchemaAttribute4 = new XmlSchemaAttribute();
			xmlSchemaAttribute4.Name = "noNamespaceSchemaLocation";
			xmlSchemaAttribute4.SetQualifiedName(new XmlQualifiedName("noNamespaceSchemaLocation", "http://www.w3.org/2001/XMLSchema-instance"));
			xmlSchemaAttribute4.SetAttributeType(builtInSimpleType2);
			Interlocked.CompareExchange(ref s_xsiNoNsSLSO, xmlSchemaAttribute4, null);
		}
	}

	internal static void ElementValidationError(XmlQualifiedName name, ValidationState context, ValidationEventHandler eventHandler, object sender, string sourceUri, int lineNo, int linePos, XmlSchemaSet schemaSet)
	{
		ArrayList arrayList = null;
		if (context.ElementDecl == null)
		{
			return;
		}
		ContentValidator contentValidator = context.ElementDecl.ContentValidator;
		XmlSchemaContentType contentType = contentValidator.ContentType;
		if (contentType == XmlSchemaContentType.ElementOnly || (contentType == XmlSchemaContentType.Mixed && contentValidator != ContentValidator.Mixed && contentValidator != ContentValidator.Any))
		{
			bool flag = schemaSet != null;
			arrayList = ((!flag) ? contentValidator.ExpectedElements(context, isRequiredOnly: false) : contentValidator.ExpectedParticles(context, isRequiredOnly: false, schemaSet));
			if (arrayList == null || arrayList.Count == 0)
			{
				if (context.TooComplex)
				{
					SendValidationEvent(eventHandler, sender, new XmlSchemaValidationException(System.SR.Sch_InvalidElementContentComplex, new string[3]
					{
						BuildElementName(context.LocalName, context.Namespace),
						BuildElementName(name),
						System.SR.Sch_ComplexContentModel
					}, sourceUri, lineNo, linePos), XmlSeverityType.Error);
				}
				else
				{
					SendValidationEvent(eventHandler, sender, new XmlSchemaValidationException(System.SR.Sch_InvalidElementContent, new string[2]
					{
						BuildElementName(context.LocalName, context.Namespace),
						BuildElementName(name)
					}, sourceUri, lineNo, linePos), XmlSeverityType.Error);
				}
			}
			else if (context.TooComplex)
			{
				SendValidationEvent(eventHandler, sender, new XmlSchemaValidationException(System.SR.Sch_InvalidElementContentExpectingComplex, new string[4]
				{
					BuildElementName(context.LocalName, context.Namespace),
					BuildElementName(name),
					PrintExpectedElements(arrayList, flag),
					System.SR.Sch_ComplexContentModel
				}, sourceUri, lineNo, linePos), XmlSeverityType.Error);
			}
			else
			{
				SendValidationEvent(eventHandler, sender, new XmlSchemaValidationException(System.SR.Sch_InvalidElementContentExpecting, new string[3]
				{
					BuildElementName(context.LocalName, context.Namespace),
					BuildElementName(name),
					PrintExpectedElements(arrayList, flag)
				}, sourceUri, lineNo, linePos), XmlSeverityType.Error);
			}
		}
		else if (contentType == XmlSchemaContentType.Empty)
		{
			SendValidationEvent(eventHandler, sender, new XmlSchemaValidationException(System.SR.Sch_InvalidElementInEmptyEx, new string[2]
			{
				QNameString(context.LocalName, context.Namespace),
				name.ToString()
			}, sourceUri, lineNo, linePos), XmlSeverityType.Error);
		}
		else if (!contentValidator.IsOpen)
		{
			SendValidationEvent(eventHandler, sender, new XmlSchemaValidationException(System.SR.Sch_InvalidElementInTextOnlyEx, new string[2]
			{
				QNameString(context.LocalName, context.Namespace),
				name.ToString()
			}, sourceUri, lineNo, linePos), XmlSeverityType.Error);
		}
	}

	internal static void CompleteValidationError(ValidationState context, ValidationEventHandler eventHandler, object sender, string sourceUri, int lineNo, int linePos, XmlSchemaSet schemaSet)
	{
		ArrayList arrayList = null;
		bool flag = schemaSet != null;
		if (context.ElementDecl != null)
		{
			arrayList = ((!flag) ? context.ElementDecl.ContentValidator.ExpectedElements(context, isRequiredOnly: true) : context.ElementDecl.ContentValidator.ExpectedParticles(context, isRequiredOnly: true, schemaSet));
		}
		if (arrayList == null || arrayList.Count == 0)
		{
			if (context.TooComplex)
			{
				SendValidationEvent(eventHandler, sender, new XmlSchemaValidationException(System.SR.Sch_IncompleteContentComplex, new string[2]
				{
					BuildElementName(context.LocalName, context.Namespace),
					System.SR.Sch_ComplexContentModel
				}, sourceUri, lineNo, linePos), XmlSeverityType.Error);
			}
			SendValidationEvent(eventHandler, sender, new XmlSchemaValidationException(System.SR.Sch_IncompleteContent, BuildElementName(context.LocalName, context.Namespace), sourceUri, lineNo, linePos), XmlSeverityType.Error);
		}
		else if (context.TooComplex)
		{
			SendValidationEvent(eventHandler, sender, new XmlSchemaValidationException(System.SR.Sch_IncompleteContentExpectingComplex, new string[3]
			{
				BuildElementName(context.LocalName, context.Namespace),
				PrintExpectedElements(arrayList, flag),
				System.SR.Sch_ComplexContentModel
			}, sourceUri, lineNo, linePos), XmlSeverityType.Error);
		}
		else
		{
			SendValidationEvent(eventHandler, sender, new XmlSchemaValidationException(System.SR.Sch_IncompleteContentExpecting, new string[2]
			{
				BuildElementName(context.LocalName, context.Namespace),
				PrintExpectedElements(arrayList, flag)
			}, sourceUri, lineNo, linePos), XmlSeverityType.Error);
		}
	}

	internal static string PrintExpectedElements(ArrayList expected, bool getParticles)
	{
		if (getParticles)
		{
			string sch_ContinuationString = System.SR.Sch_ContinuationString;
			object[] args = new string[1] { " " };
			string value = System.SR.Format(sch_ContinuationString, args);
			XmlSchemaParticle xmlSchemaParticle = null;
			XmlSchemaParticle xmlSchemaParticle2 = null;
			ArrayList arrayList = new ArrayList();
			StringBuilder stringBuilder = new StringBuilder();
			if (expected.Count == 1)
			{
				xmlSchemaParticle2 = expected[0] as XmlSchemaParticle;
			}
			else
			{
				for (int i = 1; i < expected.Count; i++)
				{
					xmlSchemaParticle = expected[i - 1] as XmlSchemaParticle;
					xmlSchemaParticle2 = expected[i] as XmlSchemaParticle;
					XmlQualifiedName qualifiedName = xmlSchemaParticle.GetQualifiedName();
					if (qualifiedName.Namespace != xmlSchemaParticle2.GetQualifiedName().Namespace)
					{
						arrayList.Add(qualifiedName);
						PrintNamesWithNS(arrayList, stringBuilder);
						arrayList.Clear();
						stringBuilder.Append(value);
					}
					else
					{
						arrayList.Add(qualifiedName);
					}
				}
			}
			arrayList.Add(xmlSchemaParticle2.GetQualifiedName());
			PrintNamesWithNS(arrayList, stringBuilder);
			return stringBuilder.ToString();
		}
		return PrintNames(expected);
	}

	private static string PrintNames(ArrayList expected)
	{
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.Append('\'');
		stringBuilder.Append(expected[0].ToString());
		for (int i = 1; i < expected.Count; i++)
		{
			stringBuilder.Append(' ');
			stringBuilder.Append(expected[i].ToString());
		}
		stringBuilder.Append('\'');
		return stringBuilder.ToString();
	}

	private static void PrintNamesWithNS(ArrayList expected, StringBuilder builder)
	{
		XmlQualifiedName xmlQualifiedName = expected[0] as XmlQualifiedName;
		if (expected.Count == 1)
		{
			if (xmlQualifiedName.Name == "*")
			{
				EnumerateAny(builder, xmlQualifiedName.Namespace);
			}
			else if (xmlQualifiedName.Namespace.Length != 0)
			{
				builder.Append(System.SR.Format(System.SR.Sch_ElementNameAndNamespace, xmlQualifiedName.Name, xmlQualifiedName.Namespace));
			}
			else
			{
				builder.Append(System.SR.Format(System.SR.Sch_ElementName, xmlQualifiedName.Name));
			}
			return;
		}
		bool flag = false;
		bool flag2 = true;
		StringBuilder stringBuilder = new StringBuilder();
		for (int i = 0; i < expected.Count; i++)
		{
			xmlQualifiedName = expected[i] as XmlQualifiedName;
			if (xmlQualifiedName.Name == "*")
			{
				flag = true;
				continue;
			}
			if (flag2)
			{
				flag2 = false;
			}
			else
			{
				stringBuilder.Append(", ");
			}
			stringBuilder.Append(xmlQualifiedName.Name);
		}
		if (flag)
		{
			stringBuilder.Append(", ");
			stringBuilder.Append(System.SR.Sch_AnyElement);
		}
		else if (xmlQualifiedName.Namespace.Length != 0)
		{
			builder.Append(System.SR.Format(System.SR.Sch_ElementNameAndNamespace, stringBuilder, xmlQualifiedName.Namespace));
		}
		else
		{
			builder.Append(System.SR.Format(System.SR.Sch_ElementName, stringBuilder));
		}
	}

	private static void EnumerateAny(StringBuilder builder, string namespaces)
	{
		StringBuilder stringBuilder = new StringBuilder();
		if (namespaces == "##any" || namespaces == "##other")
		{
			stringBuilder.Append(namespaces);
		}
		else
		{
			string[] array = XmlConvert.SplitString(namespaces);
			stringBuilder.Append(array[0]);
			for (int i = 1; i < array.Length; i++)
			{
				stringBuilder.Append(", ");
				stringBuilder.Append(array[i]);
			}
		}
		builder.Append(System.SR.Format(System.SR.Sch_AnyElementNS, stringBuilder));
	}

	internal static string QNameString(string localName, string ns)
	{
		if (ns.Length == 0)
		{
			return localName;
		}
		return ns + ":" + localName;
	}

	internal static string BuildElementName(XmlQualifiedName qname)
	{
		return BuildElementName(qname.Name, qname.Namespace);
	}

	internal static string BuildElementName(string localName, string ns)
	{
		if (ns.Length != 0)
		{
			return System.SR.Format(System.SR.Sch_ElementNameAndNamespace, localName, ns);
		}
		return System.SR.Format(System.SR.Sch_ElementName, localName);
	}

	private void ProcessEntity(string name)
	{
		if (_checkEntity)
		{
			IDtdEntityInfo dtdEntityInfo = null;
			if (_dtdSchemaInfo != null)
			{
				dtdEntityInfo = _dtdSchemaInfo.LookupEntity(name);
			}
			if (dtdEntityInfo == null)
			{
				SendValidationEvent(System.SR.Sch_UndeclaredEntity, name);
			}
			else if (dtdEntityInfo.IsUnparsedEntity)
			{
				SendValidationEvent(System.SR.Sch_UnparsedEntityRef, name);
			}
		}
	}

	private void SendValidationEvent(string code)
	{
		SendValidationEvent(code, string.Empty);
	}

	private void SendValidationEvent(string code, string[] args)
	{
		SendValidationEvent(new XmlSchemaValidationException(code, args, _sourceUriString, _positionInfo.LineNumber, _positionInfo.LinePosition));
	}

	private void SendValidationEvent(string code, string arg)
	{
		SendValidationEvent(new XmlSchemaValidationException(code, arg, _sourceUriString, _positionInfo.LineNumber, _positionInfo.LinePosition));
	}

	private void SendValidationEvent(string code, string arg1, string arg2)
	{
		SendValidationEvent(new XmlSchemaValidationException(code, new string[2] { arg1, arg2 }, _sourceUriString, _positionInfo.LineNumber, _positionInfo.LinePosition));
	}

	private void SendValidationEvent(string code, string[] args, Exception innerException, XmlSeverityType severity)
	{
		if (severity != XmlSeverityType.Warning || ReportValidationWarnings)
		{
			SendValidationEvent(new XmlSchemaValidationException(code, args, innerException, _sourceUriString, _positionInfo.LineNumber, _positionInfo.LinePosition), severity);
		}
	}

	private void SendValidationEvent(string code, string[] args, Exception innerException)
	{
		SendValidationEvent(new XmlSchemaValidationException(code, args, innerException, _sourceUriString, _positionInfo.LineNumber, _positionInfo.LinePosition), XmlSeverityType.Error);
	}

	private void SendValidationEvent(XmlSchemaValidationException e)
	{
		SendValidationEvent(e, XmlSeverityType.Error);
	}

	private void SendValidationEvent(XmlSchemaException e)
	{
		SendValidationEvent(new XmlSchemaValidationException(e.GetRes, e.Args, e.SourceUri, e.LineNumber, e.LinePosition), XmlSeverityType.Error);
	}

	private void SendValidationEvent(string code, string msg, XmlSeverityType severity)
	{
		if (severity != XmlSeverityType.Warning || ReportValidationWarnings)
		{
			SendValidationEvent(new XmlSchemaValidationException(code, msg, _sourceUriString, _positionInfo.LineNumber, _positionInfo.LinePosition), severity);
		}
	}

	private void SendValidationEvent(XmlSchemaValidationException e, XmlSeverityType severity)
	{
		bool flag = false;
		if (severity == XmlSeverityType.Error)
		{
			flag = true;
			_context.Validity = XmlSchemaValidity.Invalid;
		}
		if (flag)
		{
			if (_eventHandler == null)
			{
				throw e;
			}
			_eventHandler(_validationEventSender, new ValidationEventArgs(e, severity));
		}
		else if (ReportValidationWarnings && _eventHandler != null)
		{
			_eventHandler(_validationEventSender, new ValidationEventArgs(e, severity));
		}
	}

	internal static void SendValidationEvent(ValidationEventHandler eventHandler, object sender, XmlSchemaValidationException e, XmlSeverityType severity)
	{
		if (eventHandler != null)
		{
			eventHandler(sender, new ValidationEventArgs(e, severity));
		}
		else if (severity == XmlSeverityType.Error)
		{
			throw e;
		}
	}
}
