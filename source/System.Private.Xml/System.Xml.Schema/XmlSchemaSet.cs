using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading;

namespace System.Xml.Schema;

public class XmlSchemaSet
{
	private readonly XmlNameTable _nameTable;

	private SchemaNames _schemaNames;

	private readonly SortedList _schemas;

	private readonly ValidationEventHandler _internalEventHandler;

	private ValidationEventHandler _eventHandler;

	private bool _isCompiled;

	private readonly Hashtable _schemaLocations;

	private readonly Hashtable _chameleonSchemas;

	private readonly Hashtable _targetNamespaces;

	private bool _compileAll;

	private SchemaInfo _cachedCompiledInfo;

	private readonly XmlReaderSettings _readerSettings;

	private XmlSchema _schemaForSchema;

	private XmlSchemaCompilationSettings _compilationSettings;

	internal XmlSchemaObjectTable elements;

	internal XmlSchemaObjectTable attributes;

	internal XmlSchemaObjectTable schemaTypes;

	internal XmlSchemaObjectTable substitutionGroups;

	private XmlSchemaObjectTable _typeExtensions;

	private object _internalSyncObject;

	internal object InternalSyncObject
	{
		get
		{
			if (_internalSyncObject == null)
			{
				object value = new object();
				Interlocked.CompareExchange<object>(ref _internalSyncObject, value, (object)null);
			}
			return _internalSyncObject;
		}
	}

	public XmlNameTable NameTable => _nameTable;

	public bool IsCompiled => _isCompiled;

	public XmlResolver? XmlResolver
	{
		set
		{
			_readerSettings.XmlResolver = value;
		}
	}

	public XmlSchemaCompilationSettings CompilationSettings
	{
		get
		{
			return _compilationSettings;
		}
		set
		{
			_compilationSettings = value;
		}
	}

	public int Count => _schemas.Count;

	public XmlSchemaObjectTable GlobalElements
	{
		get
		{
			if (elements == null)
			{
				elements = new XmlSchemaObjectTable();
			}
			return elements;
		}
	}

	public XmlSchemaObjectTable GlobalAttributes
	{
		get
		{
			if (attributes == null)
			{
				attributes = new XmlSchemaObjectTable();
			}
			return attributes;
		}
	}

	public XmlSchemaObjectTable GlobalTypes
	{
		get
		{
			if (schemaTypes == null)
			{
				schemaTypes = new XmlSchemaObjectTable();
			}
			return schemaTypes;
		}
	}

	internal XmlSchemaObjectTable SubstitutionGroups
	{
		get
		{
			if (substitutionGroups == null)
			{
				substitutionGroups = new XmlSchemaObjectTable();
			}
			return substitutionGroups;
		}
	}

	internal Hashtable SchemaLocations => _schemaLocations;

	internal XmlSchemaObjectTable TypeExtensions
	{
		get
		{
			if (_typeExtensions == null)
			{
				_typeExtensions = new XmlSchemaObjectTable();
			}
			return _typeExtensions;
		}
	}

	internal SchemaInfo CompiledInfo => _cachedCompiledInfo;

	internal XmlReaderSettings ReaderSettings => _readerSettings;

	internal SortedList SortedSchemas => _schemas;

	public event ValidationEventHandler ValidationEventHandler
	{
		add
		{
			_eventHandler = (ValidationEventHandler)Delegate.Remove(_eventHandler, _internalEventHandler);
			_eventHandler = (ValidationEventHandler)Delegate.Combine(_eventHandler, value);
			if (_eventHandler == null)
			{
				_eventHandler = _internalEventHandler;
			}
		}
		remove
		{
			_eventHandler = (ValidationEventHandler)Delegate.Remove(_eventHandler, value);
			if (_eventHandler == null)
			{
				_eventHandler = _internalEventHandler;
			}
		}
	}

	public XmlSchemaSet()
		: this(new NameTable())
	{
	}

	public XmlSchemaSet(XmlNameTable nameTable)
	{
		if (nameTable == null)
		{
			throw new ArgumentNullException("nameTable");
		}
		_nameTable = nameTable;
		_schemas = new SortedList();
		_schemaLocations = new Hashtable();
		_chameleonSchemas = new Hashtable();
		_targetNamespaces = new Hashtable();
		_internalEventHandler = InternalValidationCallback;
		_eventHandler = _internalEventHandler;
		_readerSettings = new XmlReaderSettings();
		if (_readerSettings.GetXmlResolver() == null)
		{
			_readerSettings.XmlResolver = new XmlUrlResolver();
			_readerSettings.IsXmlResolverSet = false;
		}
		_readerSettings.NameTable = nameTable;
		_readerSettings.DtdProcessing = DtdProcessing.Prohibit;
		_compilationSettings = new XmlSchemaCompilationSettings();
		_cachedCompiledInfo = new SchemaInfo();
		_compileAll = true;
	}

	public XmlSchema? Add(string? targetNamespace, string schemaUri)
	{
		if (schemaUri == null || schemaUri.Length == 0)
		{
			throw new ArgumentNullException("schemaUri");
		}
		if (targetNamespace != null)
		{
			targetNamespace = XmlComplianceUtil.CDataNormalize(targetNamespace);
		}
		XmlSchema schema = null;
		lock (InternalSyncObject)
		{
			XmlResolver xmlResolver = _readerSettings.GetXmlResolver();
			if (xmlResolver == null)
			{
				xmlResolver = new XmlUrlResolver();
			}
			Uri schemaUri2 = xmlResolver.ResolveUri(null, schemaUri);
			if (IsSchemaLoaded(schemaUri2, targetNamespace, out schema))
			{
				return schema;
			}
			XmlReader xmlReader = XmlReader.Create(schemaUri, _readerSettings);
			try
			{
				schema = Add(targetNamespace, ParseSchema(targetNamespace, xmlReader));
				while (xmlReader.Read())
				{
				}
				return schema;
			}
			finally
			{
				xmlReader.Close();
			}
		}
	}

	public XmlSchema? Add(string? targetNamespace, XmlReader schemaDocument)
	{
		if (schemaDocument == null)
		{
			throw new ArgumentNullException("schemaDocument");
		}
		if (targetNamespace != null)
		{
			targetNamespace = XmlComplianceUtil.CDataNormalize(targetNamespace);
		}
		lock (InternalSyncObject)
		{
			XmlSchema schema = null;
			Uri schemaUri = new Uri(schemaDocument.BaseURI, UriKind.RelativeOrAbsolute);
			if (IsSchemaLoaded(schemaUri, targetNamespace, out schema))
			{
				return schema;
			}
			DtdProcessing dtdProcessing = _readerSettings.DtdProcessing;
			SetDtdProcessing(schemaDocument);
			schema = Add(targetNamespace, ParseSchema(targetNamespace, schemaDocument));
			_readerSettings.DtdProcessing = dtdProcessing;
			return schema;
		}
	}

	public void Add(XmlSchemaSet schemas)
	{
		if (schemas == null)
		{
			throw new ArgumentNullException("schemas");
		}
		if (this == schemas)
		{
			return;
		}
		bool lockTaken = false;
		bool lockTaken2 = false;
		try
		{
			SpinWait spinWait = default(SpinWait);
			while (true)
			{
				Monitor.TryEnter(InternalSyncObject, ref lockTaken);
				if (lockTaken)
				{
					Monitor.TryEnter(schemas.InternalSyncObject, ref lockTaken2);
					if (lockTaken2)
					{
						break;
					}
					Monitor.Exit(InternalSyncObject);
					lockTaken = false;
					spinWait.SpinOnce();
				}
			}
			if (schemas.IsCompiled)
			{
				CopyFromCompiledSet(schemas);
				return;
			}
			bool flag = false;
			string text = null;
			foreach (XmlSchema value in schemas.SortedSchemas.Values)
			{
				text = value.TargetNamespace;
				if (text == null)
				{
					text = string.Empty;
				}
				if (!_schemas.ContainsKey(value.SchemaId) && FindSchemaByNSAndUrl(value.BaseUri, text, null) == null)
				{
					XmlSchema xmlSchema2 = Add(value.TargetNamespace, value);
					if (xmlSchema2 == null)
					{
						flag = true;
						break;
					}
				}
			}
			if (!flag)
			{
				return;
			}
			foreach (XmlSchema value2 in schemas.SortedSchemas.Values)
			{
				_schemas.Remove(value2.SchemaId);
				_schemaLocations.Remove(value2.BaseUri);
			}
		}
		finally
		{
			if (lockTaken)
			{
				Monitor.Exit(InternalSyncObject);
			}
			if (lockTaken2)
			{
				Monitor.Exit(schemas.InternalSyncObject);
			}
		}
	}

	public XmlSchema? Add(XmlSchema schema)
	{
		if (schema == null)
		{
			throw new ArgumentNullException("schema");
		}
		lock (InternalSyncObject)
		{
			if (_schemas.ContainsKey(schema.SchemaId))
			{
				return schema;
			}
			return Add(schema.TargetNamespace, schema);
		}
	}

	public XmlSchema? Remove(XmlSchema schema)
	{
		return Remove(schema, forceCompile: true);
	}

	public bool RemoveRecursive(XmlSchema schemaToRemove)
	{
		if (schemaToRemove == null)
		{
			throw new ArgumentNullException("schemaToRemove");
		}
		if (!_schemas.ContainsKey(schemaToRemove.SchemaId))
		{
			return false;
		}
		lock (InternalSyncObject)
		{
			if (_schemas.ContainsKey(schemaToRemove.SchemaId))
			{
				Hashtable hashtable = new Hashtable();
				hashtable.Add(GetTargetNamespace(schemaToRemove), schemaToRemove);
				for (int i = 0; i < schemaToRemove.ImportedNamespaces.Count; i++)
				{
					string text = (string)schemaToRemove.ImportedNamespaces[i];
					if (hashtable[text] == null)
					{
						hashtable.Add(text, text);
					}
				}
				ArrayList arrayList = new ArrayList();
				XmlSchema xmlSchema;
				for (int j = 0; j < _schemas.Count; j++)
				{
					xmlSchema = (XmlSchema)_schemas.GetByIndex(j);
					if (xmlSchema != schemaToRemove && !schemaToRemove.ImportedSchemas.Contains(xmlSchema))
					{
						arrayList.Add(xmlSchema);
					}
				}
				xmlSchema = null;
				for (int k = 0; k < arrayList.Count; k++)
				{
					xmlSchema = (XmlSchema)arrayList[k];
					if (xmlSchema.ImportedNamespaces.Count <= 0)
					{
						continue;
					}
					foreach (string key in hashtable.Keys)
					{
						if (xmlSchema.ImportedNamespaces.Contains(key))
						{
							SendValidationEvent(new XmlSchemaException(System.SR.Sch_SchemaNotRemoved, string.Empty), XmlSeverityType.Warning);
							return false;
						}
					}
				}
				Remove(schemaToRemove, forceCompile: true);
				for (int l = 0; l < schemaToRemove.ImportedSchemas.Count; l++)
				{
					XmlSchema schema = (XmlSchema)schemaToRemove.ImportedSchemas[l];
					Remove(schema, forceCompile: true);
				}
				return true;
			}
		}
		return false;
	}

	public bool Contains(string? targetNamespace)
	{
		if (targetNamespace == null)
		{
			targetNamespace = string.Empty;
		}
		return _targetNamespaces[targetNamespace] != null;
	}

	public bool Contains(XmlSchema schema)
	{
		if (schema == null)
		{
			throw new ArgumentNullException("schema");
		}
		return _schemas.ContainsValue(schema);
	}

	public void Compile()
	{
		if (_isCompiled)
		{
			return;
		}
		if (_schemas.Count == 0)
		{
			ClearTables();
			_cachedCompiledInfo = new SchemaInfo();
			_isCompiled = true;
			_compileAll = false;
			return;
		}
		lock (InternalSyncObject)
		{
			if (_isCompiled)
			{
				return;
			}
			Compiler compiler = new Compiler(_nameTable, _eventHandler, _schemaForSchema, _compilationSettings);
			SchemaInfo schemaInfo = new SchemaInfo();
			int i = 0;
			if (!_compileAll)
			{
				compiler.ImportAllCompiledSchemas(this);
			}
			try
			{
				XmlSchema buildInSchema = Preprocessor.GetBuildInSchema();
				for (i = 0; i < _schemas.Count; i++)
				{
					XmlSchema xmlSchema = (XmlSchema)_schemas.GetByIndex(i);
					Monitor.Enter(xmlSchema);
					if (!xmlSchema.IsPreprocessed)
					{
						SendValidationEvent(new XmlSchemaException(System.SR.Sch_SchemaNotPreprocessed, string.Empty), XmlSeverityType.Error);
						_isCompiled = false;
						return;
					}
					if (xmlSchema.IsCompiledBySet)
					{
						if (!_compileAll)
						{
							continue;
						}
						if (xmlSchema == buildInSchema)
						{
							compiler.Prepare(xmlSchema, cleanup: false);
							continue;
						}
					}
					compiler.Prepare(xmlSchema, cleanup: true);
				}
				_isCompiled = compiler.Execute(this, schemaInfo);
				if (_isCompiled)
				{
					if (!_compileAll)
					{
						schemaInfo.Add(_cachedCompiledInfo, _eventHandler);
					}
					_compileAll = false;
					_cachedCompiledInfo = schemaInfo;
				}
			}
			finally
			{
				if (i == _schemas.Count)
				{
					i--;
				}
				for (int num = i; num >= 0; num--)
				{
					XmlSchema xmlSchema2 = (XmlSchema)_schemas.GetByIndex(num);
					if (xmlSchema2 == Preprocessor.GetBuildInSchema())
					{
						Monitor.Exit(xmlSchema2);
					}
					else
					{
						xmlSchema2.IsCompiledBySet = _isCompiled;
						Monitor.Exit(xmlSchema2);
					}
				}
			}
		}
	}

	public XmlSchema Reprocess(XmlSchema schema)
	{
		if (schema == null)
		{
			throw new ArgumentNullException("schema");
		}
		if (!_schemas.ContainsKey(schema.SchemaId))
		{
			throw new ArgumentException(System.SR.Sch_SchemaDoesNotExist, "schema");
		}
		XmlSchema result = schema;
		lock (InternalSyncObject)
		{
			RemoveSchemaFromGlobalTables(schema);
			RemoveSchemaFromCaches(schema);
			if (schema.BaseUri != null)
			{
				_schemaLocations.Remove(schema.BaseUri);
			}
			string targetNamespace = GetTargetNamespace(schema);
			if (Schemas(targetNamespace).Count == 0)
			{
				_targetNamespaces.Remove(targetNamespace);
			}
			_isCompiled = false;
			_compileAll = true;
			if (schema.ErrorCount != 0)
			{
				return result;
			}
			if (PreprocessSchema(ref schema, schema.TargetNamespace))
			{
				if (_targetNamespaces[targetNamespace] == null)
				{
					_targetNamespaces.Add(targetNamespace, targetNamespace);
				}
				if (_schemaForSchema == null && targetNamespace == "http://www.w3.org/2001/XMLSchema" && schema.SchemaTypes[DatatypeImplementation.QnAnyType] != null)
				{
					_schemaForSchema = schema;
				}
				for (int i = 0; i < schema.ImportedSchemas.Count; i++)
				{
					XmlSchema xmlSchema = (XmlSchema)schema.ImportedSchemas[i];
					if (!_schemas.ContainsKey(xmlSchema.SchemaId))
					{
						_schemas.Add(xmlSchema.SchemaId, xmlSchema);
					}
					targetNamespace = GetTargetNamespace(xmlSchema);
					if (_targetNamespaces[targetNamespace] == null)
					{
						_targetNamespaces.Add(targetNamespace, targetNamespace);
					}
					if (_schemaForSchema == null && targetNamespace == "http://www.w3.org/2001/XMLSchema" && schema.SchemaTypes[DatatypeImplementation.QnAnyType] != null)
					{
						_schemaForSchema = schema;
					}
				}
				return schema;
			}
			return result;
		}
	}

	public void CopyTo(XmlSchema[] schemas, int index)
	{
		if (schemas == null)
		{
			throw new ArgumentNullException("schemas");
		}
		if (index < 0 || index > schemas.Length - 1)
		{
			throw new ArgumentOutOfRangeException("index");
		}
		_schemas.Values.CopyTo(schemas, index);
	}

	public ICollection Schemas()
	{
		return _schemas.Values;
	}

	public ICollection Schemas(string? targetNamespace)
	{
		ArrayList arrayList = new ArrayList();
		if (targetNamespace == null)
		{
			targetNamespace = string.Empty;
		}
		for (int i = 0; i < _schemas.Count; i++)
		{
			XmlSchema xmlSchema = (XmlSchema)_schemas.GetByIndex(i);
			if (GetTargetNamespace(xmlSchema) == targetNamespace)
			{
				arrayList.Add(xmlSchema);
			}
		}
		return arrayList;
	}

	private XmlSchema Add(string targetNamespace, XmlSchema schema)
	{
		if (schema == null || schema.ErrorCount != 0)
		{
			return null;
		}
		if (PreprocessSchema(ref schema, targetNamespace))
		{
			AddSchemaToSet(schema);
			_isCompiled = false;
			return schema;
		}
		return null;
	}

	internal void Add(string targetNamespace, XmlReader reader, Hashtable validatedNamespaces)
	{
		if (reader == null)
		{
			throw new ArgumentNullException("reader");
		}
		if (targetNamespace == null)
		{
			targetNamespace = string.Empty;
		}
		if (validatedNamespaces[targetNamespace] != null)
		{
			if (FindSchemaByNSAndUrl(new Uri(reader.BaseURI, UriKind.RelativeOrAbsolute), targetNamespace, null) == null)
			{
				throw new XmlSchemaException(System.SR.Sch_ComponentAlreadySeenForNS, targetNamespace);
			}
		}
		else
		{
			if (IsSchemaLoaded(new Uri(reader.BaseURI, UriKind.RelativeOrAbsolute), targetNamespace, out var schema))
			{
				return;
			}
			schema = ParseSchema(targetNamespace, reader);
			DictionaryEntry[] array = new DictionaryEntry[_schemaLocations.Count];
			_schemaLocations.CopyTo(array, 0);
			Add(targetNamespace, schema);
			if (schema.ImportedSchemas.Count <= 0)
			{
				return;
			}
			for (int i = 0; i < schema.ImportedSchemas.Count; i++)
			{
				XmlSchema xmlSchema = (XmlSchema)schema.ImportedSchemas[i];
				string text = xmlSchema.TargetNamespace;
				if (text == null)
				{
					text = string.Empty;
				}
				if (validatedNamespaces[text] != null && FindSchemaByNSAndUrl(xmlSchema.BaseUri, text, array) == null)
				{
					RemoveRecursive(schema);
					throw new XmlSchemaException(System.SR.Sch_ComponentAlreadySeenForNS, text);
				}
			}
		}
	}

	internal XmlSchema FindSchemaByNSAndUrl(Uri schemaUri, string ns, DictionaryEntry[] locationsTable)
	{
		if (schemaUri == null || schemaUri.OriginalString.Length == 0)
		{
			return null;
		}
		XmlSchema xmlSchema = null;
		if (locationsTable == null)
		{
			xmlSchema = (XmlSchema)_schemaLocations[schemaUri];
		}
		else
		{
			for (int i = 0; i < locationsTable.Length; i++)
			{
				if (schemaUri.Equals(locationsTable[i].Key))
				{
					xmlSchema = (XmlSchema)locationsTable[i].Value;
					break;
				}
			}
		}
		if (xmlSchema != null)
		{
			string text = ((xmlSchema.TargetNamespace == null) ? string.Empty : xmlSchema.TargetNamespace);
			if (text == ns)
			{
				return xmlSchema;
			}
			if (text.Length == 0)
			{
				ChameleonKey key = new ChameleonKey(ns, xmlSchema);
				xmlSchema = (XmlSchema)_chameleonSchemas[key];
			}
			else
			{
				xmlSchema = null;
			}
		}
		return xmlSchema;
	}

	private void SetDtdProcessing(XmlReader reader)
	{
		if (reader.Settings != null)
		{
			_readerSettings.DtdProcessing = reader.Settings.DtdProcessing;
		}
		else if (reader is XmlTextReader xmlTextReader)
		{
			_readerSettings.DtdProcessing = xmlTextReader.DtdProcessing;
		}
	}

	private void AddSchemaToSet(XmlSchema schema)
	{
		_schemas.Add(schema.SchemaId, schema);
		string targetNamespace = GetTargetNamespace(schema);
		if (_targetNamespaces[targetNamespace] == null)
		{
			_targetNamespaces.Add(targetNamespace, targetNamespace);
		}
		if (_schemaForSchema == null && targetNamespace == "http://www.w3.org/2001/XMLSchema" && schema.SchemaTypes[DatatypeImplementation.QnAnyType] != null)
		{
			_schemaForSchema = schema;
		}
		for (int i = 0; i < schema.ImportedSchemas.Count; i++)
		{
			XmlSchema xmlSchema = (XmlSchema)schema.ImportedSchemas[i];
			if (!_schemas.ContainsKey(xmlSchema.SchemaId))
			{
				_schemas.Add(xmlSchema.SchemaId, xmlSchema);
			}
			targetNamespace = GetTargetNamespace(xmlSchema);
			if (_targetNamespaces[targetNamespace] == null)
			{
				_targetNamespaces.Add(targetNamespace, targetNamespace);
			}
			if (_schemaForSchema == null && targetNamespace == "http://www.w3.org/2001/XMLSchema" && schema.SchemaTypes[DatatypeImplementation.QnAnyType] != null)
			{
				_schemaForSchema = schema;
			}
		}
	}

	private void ProcessNewSubstitutionGroups(XmlSchemaObjectTable substitutionGroupsTable, bool resolve)
	{
		foreach (XmlSchemaSubstitutionGroup value in substitutionGroupsTable.Values)
		{
			if (resolve)
			{
				ResolveSubstitutionGroup(value, substitutionGroupsTable);
			}
			XmlQualifiedName examplar = value.Examplar;
			XmlSchemaSubstitutionGroup xmlSchemaSubstitutionGroup2 = (XmlSchemaSubstitutionGroup)substitutionGroups[examplar];
			if (xmlSchemaSubstitutionGroup2 != null)
			{
				for (int i = 0; i < value.Members.Count; i++)
				{
					if (!xmlSchemaSubstitutionGroup2.Members.Contains(value.Members[i]))
					{
						xmlSchemaSubstitutionGroup2.Members.Add(value.Members[i]);
					}
				}
			}
			else
			{
				AddToTable(substitutionGroups, examplar, value);
			}
		}
	}

	private void ResolveSubstitutionGroup(XmlSchemaSubstitutionGroup substitutionGroup, XmlSchemaObjectTable substTable)
	{
		List<XmlSchemaElement> list = null;
		XmlSchemaElement xmlSchemaElement = (XmlSchemaElement)elements[substitutionGroup.Examplar];
		if (substitutionGroup.Members.Contains(xmlSchemaElement))
		{
			return;
		}
		for (int i = 0; i < substitutionGroup.Members.Count; i++)
		{
			XmlSchemaElement xmlSchemaElement2 = (XmlSchemaElement)substitutionGroup.Members[i];
			XmlSchemaSubstitutionGroup xmlSchemaSubstitutionGroup = (XmlSchemaSubstitutionGroup)substTable[xmlSchemaElement2.QualifiedName];
			if (xmlSchemaSubstitutionGroup == null)
			{
				continue;
			}
			ResolveSubstitutionGroup(xmlSchemaSubstitutionGroup, substTable);
			for (int j = 0; j < xmlSchemaSubstitutionGroup.Members.Count; j++)
			{
				XmlSchemaElement xmlSchemaElement3 = (XmlSchemaElement)xmlSchemaSubstitutionGroup.Members[j];
				if (xmlSchemaElement3 != xmlSchemaElement2)
				{
					if (list == null)
					{
						list = new List<XmlSchemaElement>();
					}
					list.Add(xmlSchemaElement3);
				}
			}
		}
		if (list != null)
		{
			for (int k = 0; k < list.Count; k++)
			{
				substitutionGroup.Members.Add(list[k]);
			}
		}
		substitutionGroup.Members.Add(xmlSchemaElement);
	}

	internal XmlSchema Remove(XmlSchema schema, bool forceCompile)
	{
		if (schema == null)
		{
			throw new ArgumentNullException("schema");
		}
		lock (InternalSyncObject)
		{
			if (_schemas.ContainsKey(schema.SchemaId))
			{
				if (forceCompile)
				{
					RemoveSchemaFromGlobalTables(schema);
					RemoveSchemaFromCaches(schema);
				}
				_schemas.Remove(schema.SchemaId);
				if (schema.BaseUri != null)
				{
					_schemaLocations.Remove(schema.BaseUri);
				}
				string targetNamespace = GetTargetNamespace(schema);
				if (Schemas(targetNamespace).Count == 0)
				{
					_targetNamespaces.Remove(targetNamespace);
				}
				if (forceCompile)
				{
					_isCompiled = false;
					_compileAll = true;
				}
				return schema;
			}
		}
		return null;
	}

	private void ClearTables()
	{
		GlobalElements.Clear();
		GlobalAttributes.Clear();
		GlobalTypes.Clear();
		SubstitutionGroups.Clear();
		TypeExtensions.Clear();
	}

	internal bool PreprocessSchema(ref XmlSchema schema, string targetNamespace)
	{
		Preprocessor preprocessor = new Preprocessor(_nameTable, GetSchemaNames(_nameTable), _eventHandler, _compilationSettings);
		preprocessor.XmlResolver = _readerSettings.GetXmlResolver_CheckConfig();
		preprocessor.ReaderSettings = _readerSettings;
		preprocessor.SchemaLocations = _schemaLocations;
		preprocessor.ChameleonSchemas = _chameleonSchemas;
		bool result = preprocessor.Execute(schema, targetNamespace, loadExternals: true);
		schema = preprocessor.RootSchema;
		return result;
	}

	internal XmlSchema ParseSchema(string targetNamespace, XmlReader reader)
	{
		XmlNameTable nameTable = reader.NameTable;
		SchemaNames schemaNames = GetSchemaNames(nameTable);
		Parser parser = new Parser(SchemaType.XSD, nameTable, schemaNames, _eventHandler);
		parser.XmlResolver = _readerSettings.GetXmlResolver_CheckConfig();
		try
		{
			SchemaType schemaType = parser.Parse(reader, targetNamespace);
		}
		catch (XmlSchemaException e)
		{
			SendValidationEvent(e, XmlSeverityType.Error);
			return null;
		}
		return parser.XmlSchema;
	}

	internal void CopyFromCompiledSet(XmlSchemaSet otherSet)
	{
		SortedList sortedSchemas = otherSet.SortedSchemas;
		bool flag = _schemas.Count == 0;
		ArrayList arrayList = new ArrayList();
		SchemaInfo schemaInfo = new SchemaInfo();
		for (int i = 0; i < sortedSchemas.Count; i++)
		{
			XmlSchema xmlSchema = (XmlSchema)sortedSchemas.GetByIndex(i);
			Uri baseUri = xmlSchema.BaseUri;
			if (_schemas.ContainsKey(xmlSchema.SchemaId) || (baseUri != null && baseUri.OriginalString.Length != 0 && _schemaLocations[baseUri] != null))
			{
				arrayList.Add(xmlSchema);
				continue;
			}
			_schemas.Add(xmlSchema.SchemaId, xmlSchema);
			if (baseUri != null && baseUri.OriginalString.Length != 0)
			{
				_schemaLocations.Add(baseUri, xmlSchema);
			}
			string targetNamespace = GetTargetNamespace(xmlSchema);
			if (_targetNamespaces[targetNamespace] == null)
			{
				_targetNamespaces.Add(targetNamespace, targetNamespace);
			}
		}
		VerifyTables();
		foreach (XmlSchemaElement value in otherSet.GlobalElements.Values)
		{
			if (AddToTable(elements, value.QualifiedName, value))
			{
				continue;
			}
			goto IL_026e;
		}
		foreach (XmlSchemaAttribute value2 in otherSet.GlobalAttributes.Values)
		{
			if (AddToTable(attributes, value2.QualifiedName, value2))
			{
				continue;
			}
			goto IL_026e;
		}
		foreach (XmlSchemaType value3 in otherSet.GlobalTypes.Values)
		{
			if (AddToTable(schemaTypes, value3.QualifiedName, value3))
			{
				continue;
			}
			goto IL_026e;
		}
		ProcessNewSubstitutionGroups(otherSet.SubstitutionGroups, resolve: false);
		schemaInfo.Add(_cachedCompiledInfo, _eventHandler);
		schemaInfo.Add(otherSet.CompiledInfo, _eventHandler);
		_cachedCompiledInfo = schemaInfo;
		if (flag)
		{
			_isCompiled = true;
			_compileAll = false;
		}
		return;
		IL_026e:
		foreach (XmlSchema value4 in sortedSchemas.Values)
		{
			if (!arrayList.Contains(value4))
			{
				Remove(value4, forceCompile: false);
			}
		}
		foreach (XmlSchemaElement value5 in otherSet.GlobalElements.Values)
		{
			if (!arrayList.Contains((XmlSchema)value5.Parent))
			{
				elements.Remove(value5.QualifiedName);
			}
		}
		foreach (XmlSchemaAttribute value6 in otherSet.GlobalAttributes.Values)
		{
			if (!arrayList.Contains((XmlSchema)value6.Parent))
			{
				attributes.Remove(value6.QualifiedName);
			}
		}
		foreach (XmlSchemaType value7 in otherSet.GlobalTypes.Values)
		{
			if (!arrayList.Contains((XmlSchema)value7.Parent))
			{
				schemaTypes.Remove(value7.QualifiedName);
			}
		}
	}

	internal XmlResolver GetResolver()
	{
		return _readerSettings.GetXmlResolver_CheckConfig();
	}

	internal ValidationEventHandler GetEventHandler()
	{
		return _eventHandler;
	}

	internal SchemaNames GetSchemaNames(XmlNameTable nt)
	{
		if (_nameTable != nt)
		{
			return new SchemaNames(nt);
		}
		if (_schemaNames == null)
		{
			_schemaNames = new SchemaNames(_nameTable);
		}
		return _schemaNames;
	}

	internal bool IsSchemaLoaded(Uri schemaUri, string targetNamespace, out XmlSchema schema)
	{
		schema = null;
		if (targetNamespace == null)
		{
			targetNamespace = string.Empty;
		}
		if (GetSchemaByUri(schemaUri, out schema))
		{
			if (!_schemas.ContainsKey(schema.SchemaId) || (targetNamespace.Length != 0 && !(targetNamespace == schema.TargetNamespace)))
			{
				if (schema.TargetNamespace == null)
				{
					XmlSchema xmlSchema = FindSchemaByNSAndUrl(schemaUri, targetNamespace, null);
					if (xmlSchema != null && _schemas.ContainsKey(xmlSchema.SchemaId))
					{
						schema = xmlSchema;
					}
					else
					{
						schema = Add(targetNamespace, schema);
					}
				}
				else if (targetNamespace.Length != 0 && targetNamespace != schema.TargetNamespace)
				{
					SendValidationEvent(new XmlSchemaException(System.SR.Sch_MismatchTargetNamespaceEx, new string[2] { targetNamespace, schema.TargetNamespace }), XmlSeverityType.Error);
					schema = null;
				}
				else
				{
					AddSchemaToSet(schema);
				}
			}
			return true;
		}
		return false;
	}

	internal bool GetSchemaByUri(Uri schemaUri, [NotNullWhen(true)] out XmlSchema schema)
	{
		schema = null;
		if (schemaUri == null || schemaUri.OriginalString.Length == 0)
		{
			return false;
		}
		schema = (XmlSchema)_schemaLocations[schemaUri];
		if (schema != null)
		{
			return true;
		}
		return false;
	}

	internal string GetTargetNamespace(XmlSchema schema)
	{
		if (schema.TargetNamespace != null)
		{
			return schema.TargetNamespace;
		}
		return string.Empty;
	}

	private void RemoveSchemaFromCaches(XmlSchema schema)
	{
		List<XmlSchema> list = new List<XmlSchema>();
		schema.GetExternalSchemasList(list, schema);
		for (int i = 0; i < list.Count; i++)
		{
			if (list[i].BaseUri != null && list[i].BaseUri.OriginalString.Length != 0)
			{
				_schemaLocations.Remove(list[i].BaseUri);
			}
			ICollection keys = _chameleonSchemas.Keys;
			ArrayList arrayList = new ArrayList();
			foreach (ChameleonKey item in keys)
			{
				if (item.chameleonLocation.Equals(list[i].BaseUri) && (item.originalSchema == null || item.originalSchema == list[i]))
				{
					arrayList.Add(item);
				}
			}
			for (int j = 0; j < arrayList.Count; j++)
			{
				_chameleonSchemas.Remove(arrayList[j]);
			}
		}
	}

	private void RemoveSchemaFromGlobalTables(XmlSchema schema)
	{
		if (_schemas.Count == 0)
		{
			return;
		}
		VerifyTables();
		foreach (XmlSchemaElement value in schema.Elements.Values)
		{
			XmlSchemaElement xmlSchemaElement2 = (XmlSchemaElement)elements[value.QualifiedName];
			if (xmlSchemaElement2 == value)
			{
				elements.Remove(value.QualifiedName);
			}
		}
		foreach (XmlSchemaAttribute value2 in schema.Attributes.Values)
		{
			XmlSchemaAttribute xmlSchemaAttribute2 = (XmlSchemaAttribute)attributes[value2.QualifiedName];
			if (xmlSchemaAttribute2 == value2)
			{
				attributes.Remove(value2.QualifiedName);
			}
		}
		foreach (XmlSchemaType value3 in schema.SchemaTypes.Values)
		{
			XmlSchemaType xmlSchemaType2 = (XmlSchemaType)schemaTypes[value3.QualifiedName];
			if (xmlSchemaType2 == value3)
			{
				schemaTypes.Remove(value3.QualifiedName);
			}
		}
	}

	private bool AddToTable(XmlSchemaObjectTable table, XmlQualifiedName qname, XmlSchemaObject item)
	{
		if (qname.Name.Length == 0)
		{
			return true;
		}
		XmlSchemaObject xmlSchemaObject = table[qname];
		if (xmlSchemaObject != null)
		{
			if (xmlSchemaObject == item || xmlSchemaObject.SourceUri == item.SourceUri)
			{
				return true;
			}
			string res = string.Empty;
			if (item is XmlSchemaComplexType)
			{
				res = System.SR.Sch_DupComplexType;
			}
			else if (item is XmlSchemaSimpleType)
			{
				res = System.SR.Sch_DupSimpleType;
			}
			else if (item is XmlSchemaElement)
			{
				res = System.SR.Sch_DupGlobalElement;
			}
			else if (item is XmlSchemaAttribute)
			{
				if (qname.Namespace == "http://www.w3.org/XML/1998/namespace")
				{
					XmlSchema buildInSchema = Preprocessor.GetBuildInSchema();
					XmlSchemaObject xmlSchemaObject2 = buildInSchema.Attributes[qname];
					if (xmlSchemaObject == xmlSchemaObject2)
					{
						table.Insert(qname, item);
						return true;
					}
					if (item == xmlSchemaObject2)
					{
						return true;
					}
				}
				res = System.SR.Sch_DupGlobalAttribute;
			}
			SendValidationEvent(new XmlSchemaException(res, qname.ToString()), XmlSeverityType.Error);
			return false;
		}
		table.Add(qname, item);
		return true;
	}

	private void VerifyTables()
	{
		if (elements == null)
		{
			elements = new XmlSchemaObjectTable();
		}
		if (attributes == null)
		{
			attributes = new XmlSchemaObjectTable();
		}
		if (schemaTypes == null)
		{
			schemaTypes = new XmlSchemaObjectTable();
		}
		if (substitutionGroups == null)
		{
			substitutionGroups = new XmlSchemaObjectTable();
		}
	}

	private void InternalValidationCallback(object sender, ValidationEventArgs e)
	{
		if (e.Severity == XmlSeverityType.Error)
		{
			throw e.Exception;
		}
	}

	private void SendValidationEvent(XmlSchemaException e, XmlSeverityType severity)
	{
		if (_eventHandler != null)
		{
			_eventHandler(this, new ValidationEventArgs(e, severity));
			return;
		}
		throw e;
	}
}
