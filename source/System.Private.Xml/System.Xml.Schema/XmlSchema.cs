using System.Collections;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Threading;
using System.Xml.Serialization;

namespace System.Xml.Schema;

[XmlRoot("schema", Namespace = "http://www.w3.org/2001/XMLSchema")]
public class XmlSchema : XmlSchemaObject
{
	public const string Namespace = "http://www.w3.org/2001/XMLSchema";

	public const string InstanceNamespace = "http://www.w3.org/2001/XMLSchema-instance";

	private XmlSchemaForm _attributeFormDefault;

	private XmlSchemaForm _elementFormDefault;

	private XmlSchemaDerivationMethod _blockDefault = XmlSchemaDerivationMethod.None;

	private XmlSchemaDerivationMethod _finalDefault = XmlSchemaDerivationMethod.None;

	private string _targetNs;

	private string _version;

	private XmlSchemaObjectCollection _includes = new XmlSchemaObjectCollection();

	private XmlSchemaObjectCollection _items = new XmlSchemaObjectCollection();

	private string _id;

	private XmlAttribute[] _moreAttributes;

	private bool _isCompiled;

	private bool _isCompiledBySet;

	private bool _isPreprocessed;

	private bool _isRedefined;

	private int _errorCount;

	private XmlSchemaObjectTable _attributes;

	private XmlSchemaObjectTable _attributeGroups = new XmlSchemaObjectTable();

	private XmlSchemaObjectTable _elements = new XmlSchemaObjectTable();

	private XmlSchemaObjectTable _types = new XmlSchemaObjectTable();

	private readonly XmlSchemaObjectTable _groups = new XmlSchemaObjectTable();

	private readonly XmlSchemaObjectTable _notations = new XmlSchemaObjectTable();

	private readonly XmlSchemaObjectTable _identityConstraints = new XmlSchemaObjectTable();

	private static int s_globalIdCounter = -1;

	private ArrayList _importedSchemas;

	private ArrayList _importedNamespaces;

	private int _schemaId = -1;

	private Uri _baseUri;

	private bool _isChameleon;

	private readonly Hashtable _ids = new Hashtable();

	private XmlDocument _document;

	private XmlNameTable _nameTable;

	[XmlAttribute("attributeFormDefault")]
	[DefaultValue(XmlSchemaForm.None)]
	public XmlSchemaForm AttributeFormDefault
	{
		get
		{
			return _attributeFormDefault;
		}
		set
		{
			_attributeFormDefault = value;
		}
	}

	[XmlAttribute("blockDefault")]
	[DefaultValue(XmlSchemaDerivationMethod.None)]
	public XmlSchemaDerivationMethod BlockDefault
	{
		get
		{
			return _blockDefault;
		}
		set
		{
			_blockDefault = value;
		}
	}

	[XmlAttribute("finalDefault")]
	[DefaultValue(XmlSchemaDerivationMethod.None)]
	public XmlSchemaDerivationMethod FinalDefault
	{
		get
		{
			return _finalDefault;
		}
		set
		{
			_finalDefault = value;
		}
	}

	[XmlAttribute("elementFormDefault")]
	[DefaultValue(XmlSchemaForm.None)]
	public XmlSchemaForm ElementFormDefault
	{
		get
		{
			return _elementFormDefault;
		}
		set
		{
			_elementFormDefault = value;
		}
	}

	[XmlAttribute("targetNamespace", DataType = "anyURI")]
	public string? TargetNamespace
	{
		get
		{
			return _targetNs;
		}
		set
		{
			_targetNs = value;
		}
	}

	[XmlAttribute("version", DataType = "token")]
	public string? Version
	{
		get
		{
			return _version;
		}
		set
		{
			_version = value;
		}
	}

	[XmlElement("include", typeof(XmlSchemaInclude))]
	[XmlElement("import", typeof(XmlSchemaImport))]
	[XmlElement("redefine", typeof(XmlSchemaRedefine))]
	public XmlSchemaObjectCollection Includes => _includes;

	[XmlElement("annotation", typeof(XmlSchemaAnnotation))]
	[XmlElement("attribute", typeof(XmlSchemaAttribute))]
	[XmlElement("attributeGroup", typeof(XmlSchemaAttributeGroup))]
	[XmlElement("complexType", typeof(XmlSchemaComplexType))]
	[XmlElement("simpleType", typeof(XmlSchemaSimpleType))]
	[XmlElement("element", typeof(XmlSchemaElement))]
	[XmlElement("group", typeof(XmlSchemaGroup))]
	[XmlElement("notation", typeof(XmlSchemaNotation))]
	public XmlSchemaObjectCollection Items => _items;

	[XmlIgnore]
	public bool IsCompiled
	{
		get
		{
			if (!_isCompiled)
			{
				return _isCompiledBySet;
			}
			return true;
		}
	}

	[XmlIgnore]
	internal bool IsCompiledBySet
	{
		get
		{
			return _isCompiledBySet;
		}
		set
		{
			_isCompiledBySet = value;
		}
	}

	[XmlIgnore]
	internal bool IsPreprocessed
	{
		get
		{
			return _isPreprocessed;
		}
		set
		{
			_isPreprocessed = value;
		}
	}

	[XmlIgnore]
	internal bool IsRedefined
	{
		get
		{
			return _isRedefined;
		}
		set
		{
			_isRedefined = value;
		}
	}

	[XmlIgnore]
	public XmlSchemaObjectTable Attributes
	{
		get
		{
			if (_attributes == null)
			{
				_attributes = new XmlSchemaObjectTable();
			}
			return _attributes;
		}
	}

	[XmlIgnore]
	public XmlSchemaObjectTable AttributeGroups
	{
		get
		{
			if (_attributeGroups == null)
			{
				_attributeGroups = new XmlSchemaObjectTable();
			}
			return _attributeGroups;
		}
	}

	[XmlIgnore]
	public XmlSchemaObjectTable SchemaTypes
	{
		get
		{
			if (_types == null)
			{
				_types = new XmlSchemaObjectTable();
			}
			return _types;
		}
	}

	[XmlIgnore]
	public XmlSchemaObjectTable Elements
	{
		get
		{
			if (_elements == null)
			{
				_elements = new XmlSchemaObjectTable();
			}
			return _elements;
		}
	}

	[XmlAttribute("id", DataType = "ID")]
	public string? Id
	{
		get
		{
			return _id;
		}
		set
		{
			_id = value;
		}
	}

	[XmlAnyAttribute]
	public XmlAttribute[]? UnhandledAttributes
	{
		get
		{
			return _moreAttributes;
		}
		set
		{
			_moreAttributes = value;
		}
	}

	[XmlIgnore]
	public XmlSchemaObjectTable Groups => _groups;

	[XmlIgnore]
	public XmlSchemaObjectTable Notations => _notations;

	[XmlIgnore]
	internal XmlSchemaObjectTable IdentityConstraints => _identityConstraints;

	[XmlIgnore]
	internal Uri? BaseUri
	{
		get
		{
			return _baseUri;
		}
		set
		{
			_baseUri = value;
		}
	}

	[XmlIgnore]
	internal int SchemaId
	{
		get
		{
			if (_schemaId == -1)
			{
				_schemaId = Interlocked.Increment(ref s_globalIdCounter);
			}
			return _schemaId;
		}
	}

	[XmlIgnore]
	internal bool IsChameleon
	{
		get
		{
			return _isChameleon;
		}
		set
		{
			_isChameleon = value;
		}
	}

	[XmlIgnore]
	internal Hashtable Ids => _ids;

	[XmlIgnore]
	internal XmlDocument Document
	{
		get
		{
			if (_document == null)
			{
				_document = new XmlDocument();
			}
			return _document;
		}
	}

	[XmlIgnore]
	internal int ErrorCount
	{
		get
		{
			return _errorCount;
		}
		set
		{
			_errorCount = value;
		}
	}

	[XmlIgnore]
	internal override string? IdAttribute
	{
		get
		{
			return Id;
		}
		set
		{
			Id = value;
		}
	}

	internal XmlNameTable NameTable
	{
		get
		{
			if (_nameTable == null)
			{
				_nameTable = new NameTable();
			}
			return _nameTable;
		}
	}

	internal ArrayList ImportedSchemas
	{
		get
		{
			if (_importedSchemas == null)
			{
				_importedSchemas = new ArrayList();
			}
			return _importedSchemas;
		}
	}

	internal ArrayList ImportedNamespaces
	{
		get
		{
			if (_importedNamespaces == null)
			{
				_importedNamespaces = new ArrayList();
			}
			return _importedNamespaces;
		}
	}

	public static XmlSchema? Read(TextReader reader, ValidationEventHandler? validationEventHandler)
	{
		return Read(new XmlTextReader(reader), validationEventHandler);
	}

	public static XmlSchema? Read(Stream stream, ValidationEventHandler? validationEventHandler)
	{
		return Read(new XmlTextReader(stream), validationEventHandler);
	}

	public static XmlSchema? Read(XmlReader reader, ValidationEventHandler? validationEventHandler)
	{
		XmlNameTable nameTable = reader.NameTable;
		Parser parser = new Parser(SchemaType.XSD, nameTable, new SchemaNames(nameTable), validationEventHandler);
		try
		{
			parser.Parse(reader, null);
		}
		catch (XmlSchemaException ex)
		{
			if (validationEventHandler != null)
			{
				validationEventHandler(null, new ValidationEventArgs(ex));
				return null;
			}
			throw;
		}
		return parser.XmlSchema;
	}

	[RequiresUnreferencedCode("Members from serialized types may be trimmed if not referenced directly")]
	public void Write(Stream stream)
	{
		Write(stream, null);
	}

	[RequiresUnreferencedCode("Members from serialized types may be trimmed if not referenced directly")]
	public void Write(Stream stream, XmlNamespaceManager? namespaceManager)
	{
		XmlTextWriter xmlTextWriter = new XmlTextWriter(stream, null);
		xmlTextWriter.Formatting = Formatting.Indented;
		Write(xmlTextWriter, namespaceManager);
	}

	[RequiresUnreferencedCode("Members from serialized types may be trimmed if not referenced directly")]
	public void Write(TextWriter writer)
	{
		Write(writer, null);
	}

	[RequiresUnreferencedCode("Members from serialized types may be trimmed if not referenced directly")]
	public void Write(TextWriter writer, XmlNamespaceManager? namespaceManager)
	{
		XmlTextWriter xmlTextWriter = new XmlTextWriter(writer);
		xmlTextWriter.Formatting = Formatting.Indented;
		Write(xmlTextWriter, namespaceManager);
	}

	[RequiresUnreferencedCode("Members from serialized types may be trimmed if not referenced directly")]
	public void Write(XmlWriter writer)
	{
		Write(writer, null);
	}

	[DynamicDependency(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicMethods | DynamicallyAccessedMemberTypes.PublicFields | DynamicallyAccessedMemberTypes.PublicNestedTypes | DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicEvents, typeof(XmlSchema))]
	[RequiresUnreferencedCode("Members from serialized types may be trimmed if not referenced directly")]
	public void Write(XmlWriter writer, XmlNamespaceManager? namespaceManager)
	{
		XmlSerializer xmlSerializer = new XmlSerializer(typeof(XmlSchema));
		XmlSerializerNamespaces xmlSerializerNamespaces;
		if (namespaceManager != null)
		{
			xmlSerializerNamespaces = new XmlSerializerNamespaces();
			bool flag = false;
			if (base.Namespaces != null)
			{
				flag = base.Namespaces.TryLookupPrefix("xs", out var _) || base.Namespaces.TryLookupNamespace("http://www.w3.org/2001/XMLSchema", out var _);
			}
			if (!flag && namespaceManager.LookupPrefix("http://www.w3.org/2001/XMLSchema") == null && namespaceManager.LookupNamespace("xs") == null)
			{
				xmlSerializerNamespaces.Add("xs", "http://www.w3.org/2001/XMLSchema");
			}
			foreach (string item in namespaceManager)
			{
				if (item != "xml" && item != "xmlns")
				{
					xmlSerializerNamespaces.Add(item, namespaceManager.LookupNamespace(item));
				}
			}
		}
		else if (base.Namespaces != null && base.Namespaces.Count > 0)
		{
			if (!base.Namespaces.TryLookupPrefix("xs", out var _) && !base.Namespaces.TryLookupNamespace("http://www.w3.org/2001/XMLSchema", out var _))
			{
				base.Namespaces.Add("xs", "http://www.w3.org/2001/XMLSchema");
			}
			xmlSerializerNamespaces = base.Namespaces;
		}
		else
		{
			xmlSerializerNamespaces = new XmlSerializerNamespaces();
			xmlSerializerNamespaces.Add("xs", "http://www.w3.org/2001/XMLSchema");
			if (_targetNs != null && _targetNs.Length != 0)
			{
				xmlSerializerNamespaces.Add("tns", _targetNs);
			}
		}
		xmlSerializer.Serialize(writer, this, xmlSerializerNamespaces);
	}

	[Obsolete("XmlSchema.Compile has been deprecated. Use System.Xml.Schema.XmlSchemaSet for schema compilation and validation instead.")]
	public void Compile(ValidationEventHandler? validationEventHandler)
	{
		SchemaInfo schemaInfo = new SchemaInfo();
		schemaInfo.SchemaType = SchemaType.XSD;
		CompileSchema(null, null, schemaInfo, null, validationEventHandler, NameTable, CompileContentModel: false);
	}

	[Obsolete("XmlSchema.Compile has been deprecated. Use System.Xml.Schema.XmlSchemaSet for schema compilation and validation instead.")]
	public void Compile(ValidationEventHandler? validationEventHandler, XmlResolver? resolver)
	{
		SchemaInfo schemaInfo = new SchemaInfo();
		schemaInfo.SchemaType = SchemaType.XSD;
		CompileSchema(null, resolver, schemaInfo, null, validationEventHandler, NameTable, CompileContentModel: false);
	}

	internal bool CompileSchema(XmlSchemaCollection xsc, XmlResolver resolver, SchemaInfo schemaInfo, string ns, ValidationEventHandler validationEventHandler, XmlNameTable nameTable, bool CompileContentModel)
	{
		lock (this)
		{
			SchemaCollectionPreprocessor schemaCollectionPreprocessor = new SchemaCollectionPreprocessor(nameTable, null, validationEventHandler);
			schemaCollectionPreprocessor.XmlResolver = resolver;
			if (!schemaCollectionPreprocessor.Execute(this, ns, loadExternals: true, xsc))
			{
				return false;
			}
			SchemaCollectionCompiler schemaCollectionCompiler = new SchemaCollectionCompiler(nameTable, validationEventHandler);
			_isCompiled = schemaCollectionCompiler.Execute(this, schemaInfo, CompileContentModel);
			SetIsCompiled(_isCompiled);
			return _isCompiled;
		}
	}

	internal void CompileSchemaInSet(XmlNameTable nameTable, ValidationEventHandler eventHandler, XmlSchemaCompilationSettings compilationSettings)
	{
		Compiler compiler = new Compiler(nameTable, eventHandler, null, compilationSettings);
		compiler.Prepare(this, cleanup: true);
		_isCompiledBySet = compiler.Compile();
	}

	internal new XmlSchema Clone()
	{
		XmlSchema xmlSchema = new XmlSchema();
		xmlSchema._attributeFormDefault = _attributeFormDefault;
		xmlSchema._elementFormDefault = _elementFormDefault;
		xmlSchema._blockDefault = _blockDefault;
		xmlSchema._finalDefault = _finalDefault;
		xmlSchema._targetNs = _targetNs;
		xmlSchema._version = _version;
		xmlSchema._includes = _includes;
		xmlSchema.Namespaces = base.Namespaces;
		xmlSchema._items = _items;
		xmlSchema.BaseUri = BaseUri;
		SchemaCollectionCompiler.Cleanup(xmlSchema);
		return xmlSchema;
	}

	internal XmlSchema DeepClone()
	{
		XmlSchema xmlSchema = new XmlSchema();
		xmlSchema._attributeFormDefault = _attributeFormDefault;
		xmlSchema._elementFormDefault = _elementFormDefault;
		xmlSchema._blockDefault = _blockDefault;
		xmlSchema._finalDefault = _finalDefault;
		xmlSchema._targetNs = _targetNs;
		xmlSchema._version = _version;
		xmlSchema._isPreprocessed = _isPreprocessed;
		for (int i = 0; i < _items.Count; i++)
		{
			XmlSchemaObject item = ((_items[i] is XmlSchemaComplexType xmlSchemaComplexType) ? xmlSchemaComplexType.Clone(this) : ((_items[i] is XmlSchemaElement xmlSchemaElement) ? xmlSchemaElement.Clone(this) : ((!(_items[i] is XmlSchemaGroup xmlSchemaGroup)) ? _items[i].Clone() : xmlSchemaGroup.Clone(this))));
			xmlSchema.Items.Add(item);
		}
		for (int j = 0; j < _includes.Count; j++)
		{
			XmlSchemaExternal item2 = (XmlSchemaExternal)_includes[j].Clone();
			xmlSchema.Includes.Add(item2);
		}
		xmlSchema.Namespaces = base.Namespaces;
		xmlSchema.BaseUri = BaseUri;
		return xmlSchema;
	}

	internal void SetIsCompiled(bool isCompiled)
	{
		_isCompiled = isCompiled;
	}

	internal override void SetUnhandledAttributes(XmlAttribute[] moreAttributes)
	{
		_moreAttributes = moreAttributes;
	}

	internal override void AddAnnotation(XmlSchemaAnnotation annotation)
	{
		_items.Add(annotation);
	}

	internal void GetExternalSchemasList(IList extList, XmlSchema schema)
	{
		if (extList.Contains(schema))
		{
			return;
		}
		extList.Add(schema);
		for (int i = 0; i < schema.Includes.Count; i++)
		{
			XmlSchemaExternal xmlSchemaExternal = (XmlSchemaExternal)schema.Includes[i];
			if (xmlSchemaExternal.Schema != null)
			{
				GetExternalSchemasList(extList, xmlSchemaExternal.Schema);
			}
		}
	}
}
