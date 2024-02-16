using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Threading;

namespace System.Xml.Schema;

internal sealed class Preprocessor : BaseProcessor
{
	private string _xmlns;

	private string _nsXsi;

	private string _targetNamespace;

	private XmlSchema _rootSchema;

	private XmlSchema _currentSchema;

	private XmlSchemaForm _elementFormDefault;

	private XmlSchemaForm _attributeFormDefault;

	private XmlSchemaDerivationMethod _blockDefault;

	private XmlSchemaDerivationMethod _finalDefault;

	private Hashtable _schemaLocations;

	private Hashtable _chameleonSchemas;

	private readonly Hashtable _referenceNamespaces;

	private readonly Hashtable _processedExternals;

	private readonly SortedList _lockList;

	private XmlReaderSettings _readerSettings;

	private XmlSchema _rootSchemaForRedefine;

	private ArrayList _redefinedList;

	private static XmlSchema s_builtInSchemaForXmlNS;

	private XmlResolver _xmlResolver;

	internal XmlResolver XmlResolver
	{
		set
		{
			_xmlResolver = value;
		}
	}

	internal XmlReaderSettings ReaderSettings
	{
		set
		{
			_readerSettings = value;
		}
	}

	internal Hashtable SchemaLocations
	{
		set
		{
			_schemaLocations = value;
		}
	}

	internal Hashtable ChameleonSchemas
	{
		set
		{
			_chameleonSchemas = value;
		}
	}

	internal XmlSchema RootSchema => _rootSchema;

	public Preprocessor(XmlNameTable nameTable, SchemaNames schemaNames, ValidationEventHandler eventHandler)
		: this(nameTable, schemaNames, eventHandler, new XmlSchemaCompilationSettings())
	{
	}

	public Preprocessor(XmlNameTable nameTable, SchemaNames schemaNames, ValidationEventHandler eventHandler, XmlSchemaCompilationSettings compilationSettings)
		: base(nameTable, schemaNames, eventHandler, compilationSettings)
	{
		_referenceNamespaces = new Hashtable();
		_processedExternals = new Hashtable();
		_lockList = new SortedList();
	}

	public bool Execute(XmlSchema schema, string targetNamespace, bool loadExternals)
	{
		_rootSchema = schema;
		_xmlns = base.NameTable.Add("xmlns");
		_nsXsi = base.NameTable.Add("http://www.w3.org/2001/XMLSchema-instance");
		_rootSchema.ImportedSchemas.Clear();
		_rootSchema.ImportedNamespaces.Clear();
		if (_rootSchema.BaseUri != null && _schemaLocations[_rootSchema.BaseUri] == null)
		{
			_schemaLocations.Add(_rootSchema.BaseUri, _rootSchema);
		}
		if (_rootSchema.TargetNamespace != null)
		{
			if (targetNamespace == null)
			{
				targetNamespace = _rootSchema.TargetNamespace;
			}
			else if (targetNamespace != _rootSchema.TargetNamespace)
			{
				SendValidationEvent(System.SR.Sch_MismatchTargetNamespaceEx, targetNamespace, _rootSchema.TargetNamespace, _rootSchema);
			}
		}
		else if (targetNamespace != null && targetNamespace.Length != 0)
		{
			_rootSchema = GetChameleonSchema(targetNamespace, _rootSchema);
		}
		if (loadExternals && _xmlResolver != null)
		{
			LoadExternals(_rootSchema);
		}
		BuildSchemaList(_rootSchema);
		int i = 0;
		try
		{
			for (i = 0; i < _lockList.Count; i++)
			{
				XmlSchema xmlSchema = (XmlSchema)_lockList.GetByIndex(i);
				Monitor.Enter(xmlSchema);
				xmlSchema.IsProcessing = false;
			}
			_rootSchemaForRedefine = _rootSchema;
			Preprocess(_rootSchema, targetNamespace, _rootSchema.ImportedSchemas);
			if (_redefinedList != null)
			{
				for (int j = 0; j < _redefinedList.Count; j++)
				{
					PreprocessRedefine((RedefineEntry)_redefinedList[j]);
				}
			}
		}
		finally
		{
			if (i == _lockList.Count)
			{
				i--;
			}
			int num = i;
			while (i >= 0)
			{
				XmlSchema xmlSchema = (XmlSchema)_lockList.GetByIndex(i);
				xmlSchema.IsProcessing = false;
				if (xmlSchema == GetBuildInSchema())
				{
					Monitor.Exit(xmlSchema);
				}
				else
				{
					xmlSchema.IsCompiledBySet = false;
					xmlSchema.IsPreprocessed = !base.HasErrors;
					Monitor.Exit(xmlSchema);
				}
				i--;
			}
		}
		_rootSchema.IsPreprocessed = !base.HasErrors;
		return !base.HasErrors;
	}

	private void Cleanup(XmlSchema schema)
	{
		if (schema != GetBuildInSchema())
		{
			schema.Attributes.Clear();
			schema.AttributeGroups.Clear();
			schema.SchemaTypes.Clear();
			schema.Elements.Clear();
			schema.Groups.Clear();
			schema.Notations.Clear();
			schema.Ids.Clear();
			schema.IdentityConstraints.Clear();
			schema.IsRedefined = false;
			schema.IsCompiledBySet = false;
		}
	}

	private void CleanupRedefine(XmlSchemaExternal include)
	{
		XmlSchemaRedefine xmlSchemaRedefine = include as XmlSchemaRedefine;
		xmlSchemaRedefine.AttributeGroups.Clear();
		xmlSchemaRedefine.Groups.Clear();
		xmlSchemaRedefine.SchemaTypes.Clear();
	}

	private void BuildSchemaList(XmlSchema schema)
	{
		if (_lockList.Contains(schema.SchemaId))
		{
			return;
		}
		_lockList.Add(schema.SchemaId, schema);
		for (int i = 0; i < schema.Includes.Count; i++)
		{
			XmlSchemaExternal xmlSchemaExternal = (XmlSchemaExternal)schema.Includes[i];
			if (xmlSchemaExternal.Schema != null)
			{
				BuildSchemaList(xmlSchemaExternal.Schema);
			}
		}
	}

	private void LoadExternals(XmlSchema schema)
	{
		if (schema.IsProcessing)
		{
			return;
		}
		schema.IsProcessing = true;
		for (int i = 0; i < schema.Includes.Count; i++)
		{
			Uri uri = null;
			XmlSchemaExternal xmlSchemaExternal = (XmlSchemaExternal)schema.Includes[i];
			XmlSchema schema2 = xmlSchemaExternal.Schema;
			if (schema2 != null)
			{
				uri = schema2.BaseUri;
				if (uri != null && _schemaLocations[uri] == null)
				{
					_schemaLocations.Add(uri, schema2);
				}
				LoadExternals(schema2);
				continue;
			}
			string schemaLocation = xmlSchemaExternal.SchemaLocation;
			Uri uri2 = null;
			Exception innerException = null;
			if (schemaLocation != null)
			{
				try
				{
					uri2 = ResolveSchemaLocationUri(schema, schemaLocation);
				}
				catch (Exception ex)
				{
					uri2 = null;
					innerException = ex;
				}
			}
			if (xmlSchemaExternal.Compositor == Compositor.Import)
			{
				XmlSchemaImport xmlSchemaImport = xmlSchemaExternal as XmlSchemaImport;
				string text = ((xmlSchemaImport.Namespace != null) ? xmlSchemaImport.Namespace : string.Empty);
				if (!schema.ImportedNamespaces.Contains(text))
				{
					schema.ImportedNamespaces.Add(text);
				}
				if (text == "http://www.w3.org/XML/1998/namespace" && uri2 == null)
				{
					xmlSchemaExternal.Schema = GetBuildInSchema();
					continue;
				}
			}
			if (uri2 == null)
			{
				if (schemaLocation != null)
				{
					SendValidationEvent(new XmlSchemaException(System.SR.Sch_InvalidIncludeLocation, null, innerException, xmlSchemaExternal.SourceUri, xmlSchemaExternal.LineNumber, xmlSchemaExternal.LinePosition, xmlSchemaExternal), XmlSeverityType.Warning);
				}
			}
			else if (_schemaLocations[uri2] == null)
			{
				object obj = null;
				try
				{
					obj = GetSchemaEntity(uri2);
				}
				catch (Exception ex2)
				{
					innerException = ex2;
					obj = null;
				}
				if (obj != null)
				{
					xmlSchemaExternal.BaseUri = uri2;
					Type type = obj.GetType();
					if (typeof(XmlSchema).IsAssignableFrom(type))
					{
						xmlSchemaExternal.Schema = (XmlSchema)obj;
						_schemaLocations.Add(uri2, xmlSchemaExternal.Schema);
						LoadExternals(xmlSchemaExternal.Schema);
						continue;
					}
					XmlReader xmlReader = null;
					if (type.IsSubclassOf(typeof(Stream)))
					{
						_readerSettings.CloseInput = true;
						_readerSettings.XmlResolver = _xmlResolver;
						xmlReader = XmlReader.Create((Stream)obj, _readerSettings, uri2.ToString());
					}
					else if (type.IsSubclassOf(typeof(XmlReader)))
					{
						xmlReader = (XmlReader)obj;
					}
					else if (type.IsSubclassOf(typeof(TextReader)))
					{
						_readerSettings.CloseInput = true;
						_readerSettings.XmlResolver = _xmlResolver;
						xmlReader = XmlReader.Create((TextReader)obj, _readerSettings, uri2.ToString());
					}
					if (xmlReader == null)
					{
						SendValidationEvent(System.SR.Sch_InvalidIncludeLocation, xmlSchemaExternal, XmlSeverityType.Warning);
						continue;
					}
					try
					{
						Parser parser = new Parser(SchemaType.XSD, base.NameTable, base.SchemaNames, base.EventHandler);
						parser.Parse(xmlReader, null);
						while (xmlReader.Read())
						{
						}
						schema2 = (xmlSchemaExternal.Schema = parser.XmlSchema);
						_schemaLocations.Add(uri2, schema2);
						LoadExternals(schema2);
					}
					catch (XmlSchemaException ex3)
					{
						SendValidationEvent(System.SR.Sch_CannotLoadSchemaLocation, schemaLocation, ex3.Message, ex3.SourceUri, ex3.LineNumber, ex3.LinePosition);
					}
					catch (Exception innerException2)
					{
						SendValidationEvent(new XmlSchemaException(System.SR.Sch_InvalidIncludeLocation, null, innerException2, xmlSchemaExternal.SourceUri, xmlSchemaExternal.LineNumber, xmlSchemaExternal.LinePosition, xmlSchemaExternal), XmlSeverityType.Warning);
					}
					finally
					{
						xmlReader.Close();
					}
				}
				else
				{
					SendValidationEvent(new XmlSchemaException(System.SR.Sch_InvalidIncludeLocation, null, innerException, xmlSchemaExternal.SourceUri, xmlSchemaExternal.LineNumber, xmlSchemaExternal.LinePosition, xmlSchemaExternal), XmlSeverityType.Warning);
				}
			}
			else
			{
				xmlSchemaExternal.Schema = (XmlSchema)_schemaLocations[uri2];
			}
		}
	}

	internal static XmlSchema GetBuildInSchema()
	{
		if (s_builtInSchemaForXmlNS == null)
		{
			XmlSchema xmlSchema = new XmlSchema();
			xmlSchema.TargetNamespace = "http://www.w3.org/XML/1998/namespace";
			xmlSchema.Namespaces.Add("xml", "http://www.w3.org/XML/1998/namespace");
			XmlSchemaAttribute xmlSchemaAttribute = new XmlSchemaAttribute();
			xmlSchemaAttribute.Name = "lang";
			xmlSchemaAttribute.SchemaTypeName = new XmlQualifiedName("language", "http://www.w3.org/2001/XMLSchema");
			xmlSchema.Items.Add(xmlSchemaAttribute);
			XmlSchemaAttribute xmlSchemaAttribute2 = new XmlSchemaAttribute();
			xmlSchemaAttribute2.Name = "base";
			xmlSchemaAttribute2.SchemaTypeName = new XmlQualifiedName("anyURI", "http://www.w3.org/2001/XMLSchema");
			xmlSchema.Items.Add(xmlSchemaAttribute2);
			XmlSchemaAttribute xmlSchemaAttribute3 = new XmlSchemaAttribute();
			xmlSchemaAttribute3.Name = "space";
			XmlSchemaSimpleType xmlSchemaSimpleType = new XmlSchemaSimpleType();
			XmlSchemaSimpleTypeRestriction xmlSchemaSimpleTypeRestriction = new XmlSchemaSimpleTypeRestriction();
			xmlSchemaSimpleTypeRestriction.BaseTypeName = new XmlQualifiedName("NCName", "http://www.w3.org/2001/XMLSchema");
			XmlSchemaEnumerationFacet xmlSchemaEnumerationFacet = new XmlSchemaEnumerationFacet();
			xmlSchemaEnumerationFacet.Value = "default";
			xmlSchemaSimpleTypeRestriction.Facets.Add(xmlSchemaEnumerationFacet);
			XmlSchemaEnumerationFacet xmlSchemaEnumerationFacet2 = new XmlSchemaEnumerationFacet();
			xmlSchemaEnumerationFacet2.Value = "preserve";
			xmlSchemaSimpleTypeRestriction.Facets.Add(xmlSchemaEnumerationFacet2);
			xmlSchemaSimpleType.Content = xmlSchemaSimpleTypeRestriction;
			xmlSchemaAttribute3.SchemaType = xmlSchemaSimpleType;
			xmlSchemaAttribute3.DefaultValue = "preserve";
			xmlSchema.Items.Add(xmlSchemaAttribute3);
			XmlSchemaAttributeGroup xmlSchemaAttributeGroup = new XmlSchemaAttributeGroup();
			xmlSchemaAttributeGroup.Name = "specialAttrs";
			XmlSchemaAttribute xmlSchemaAttribute4 = new XmlSchemaAttribute();
			xmlSchemaAttribute4.RefName = new XmlQualifiedName("lang", "http://www.w3.org/XML/1998/namespace");
			xmlSchemaAttributeGroup.Attributes.Add(xmlSchemaAttribute4);
			XmlSchemaAttribute xmlSchemaAttribute5 = new XmlSchemaAttribute();
			xmlSchemaAttribute5.RefName = new XmlQualifiedName("space", "http://www.w3.org/XML/1998/namespace");
			xmlSchemaAttributeGroup.Attributes.Add(xmlSchemaAttribute5);
			XmlSchemaAttribute xmlSchemaAttribute6 = new XmlSchemaAttribute();
			xmlSchemaAttribute6.RefName = new XmlQualifiedName("base", "http://www.w3.org/XML/1998/namespace");
			xmlSchemaAttributeGroup.Attributes.Add(xmlSchemaAttribute6);
			xmlSchema.Items.Add(xmlSchemaAttributeGroup);
			xmlSchema.IsPreprocessed = true;
			xmlSchema.CompileSchemaInSet(new NameTable(), null, null);
			Interlocked.CompareExchange(ref s_builtInSchemaForXmlNS, xmlSchema, null);
		}
		return s_builtInSchemaForXmlNS;
	}

	private void BuildRefNamespaces(XmlSchema schema)
	{
		_referenceNamespaces.Clear();
		_referenceNamespaces.Add("http://www.w3.org/2001/XMLSchema", "http://www.w3.org/2001/XMLSchema");
		for (int i = 0; i < schema.Includes.Count; i++)
		{
			XmlSchemaExternal xmlSchemaExternal = (XmlSchemaExternal)schema.Includes[i];
			if (xmlSchemaExternal is XmlSchemaImport)
			{
				XmlSchemaImport xmlSchemaImport = xmlSchemaExternal as XmlSchemaImport;
				string text = xmlSchemaImport.Namespace;
				if (text == null)
				{
					text = string.Empty;
				}
				if (_referenceNamespaces[text] == null)
				{
					_referenceNamespaces.Add(text, text);
				}
			}
		}
		string text2 = schema.TargetNamespace;
		if (text2 == null)
		{
			text2 = string.Empty;
		}
		if (_referenceNamespaces[text2] == null)
		{
			_referenceNamespaces.Add(text2, text2);
		}
	}

	private void ParseUri(string uri, string code, XmlSchemaObject sourceSchemaObject)
	{
		try
		{
			XmlConvert.ToUri(uri);
		}
		catch (FormatException innerException)
		{
			SendValidationEvent(code, new string[1] { uri }, innerException, sourceSchemaObject);
		}
	}

	private void Preprocess(XmlSchema schema, string targetNamespace, ArrayList imports)
	{
		XmlSchema xmlSchema = null;
		if (schema.IsProcessing)
		{
			return;
		}
		schema.IsProcessing = true;
		string targetNamespace2 = schema.TargetNamespace;
		if (targetNamespace2 != null)
		{
			targetNamespace2 = (schema.TargetNamespace = base.NameTable.Add(targetNamespace2));
			if (targetNamespace2.Length == 0)
			{
				SendValidationEvent(System.SR.Sch_InvalidTargetNamespaceAttribute, schema);
			}
			else
			{
				ParseUri(targetNamespace2, System.SR.Sch_InvalidNamespace, schema);
			}
		}
		if (schema.Version != null)
		{
			XmlSchemaDatatype datatype = DatatypeImplementation.GetSimpleTypeFromTypeCode(XmlTypeCode.Token).Datatype;
			object typedValue;
			Exception ex = datatype.TryParseValue(schema.Version, null, null, out typedValue);
			if (ex != null)
			{
				SendValidationEvent(System.SR.Sch_AttributeValueDataTypeDetailed, new string[4] { "version", schema.Version, datatype.TypeCodeString, ex.Message }, ex, schema);
			}
			else
			{
				schema.Version = (string)typedValue;
			}
		}
		Cleanup(schema);
		for (int i = 0; i < schema.Includes.Count; i++)
		{
			XmlSchemaExternal xmlSchemaExternal = (XmlSchemaExternal)schema.Includes[i];
			XmlSchema xmlSchema2 = xmlSchemaExternal.Schema;
			SetParent(xmlSchemaExternal, schema);
			PreprocessAnnotation(xmlSchemaExternal);
			string schemaLocation = xmlSchemaExternal.SchemaLocation;
			if (schemaLocation != null)
			{
				ParseUri(schemaLocation, System.SR.Sch_InvalidSchemaLocation, xmlSchemaExternal);
			}
			else if ((xmlSchemaExternal.Compositor == Compositor.Include || xmlSchemaExternal.Compositor == Compositor.Redefine) && xmlSchema2 == null)
			{
				SendValidationEvent(System.SR.Sch_MissRequiredAttribute, "schemaLocation", xmlSchemaExternal);
			}
			switch (xmlSchemaExternal.Compositor)
			{
			case Compositor.Import:
			{
				XmlSchemaImport xmlSchemaImport = xmlSchemaExternal as XmlSchemaImport;
				string @namespace = xmlSchemaImport.Namespace;
				if (@namespace == schema.TargetNamespace)
				{
					SendValidationEvent(System.SR.Sch_ImportTargetNamespace, xmlSchemaExternal);
				}
				if (xmlSchema2 != null)
				{
					if (@namespace != xmlSchema2.TargetNamespace)
					{
						SendValidationEvent(System.SR.Sch_MismatchTargetNamespaceImport, @namespace, xmlSchema2.TargetNamespace, xmlSchemaImport);
					}
					xmlSchema = _rootSchemaForRedefine;
					_rootSchemaForRedefine = xmlSchema2;
					Preprocess(xmlSchema2, @namespace, imports);
					_rootSchemaForRedefine = xmlSchema;
				}
				else if (@namespace != null)
				{
					if (@namespace.Length == 0)
					{
						SendValidationEvent(System.SR.Sch_InvalidNamespaceAttribute, @namespace, xmlSchemaExternal);
					}
					else
					{
						ParseUri(@namespace, System.SR.Sch_InvalidNamespace, xmlSchemaExternal);
					}
				}
				continue;
			}
			case Compositor.Include:
			{
				XmlSchema schema2 = xmlSchemaExternal.Schema;
				if (schema2 == null)
				{
					continue;
				}
				break;
			}
			case Compositor.Redefine:
				if (xmlSchema2 == null)
				{
					continue;
				}
				CleanupRedefine(xmlSchemaExternal);
				break;
			}
			if (xmlSchema2.TargetNamespace != null)
			{
				if (schema.TargetNamespace != xmlSchema2.TargetNamespace)
				{
					SendValidationEvent(System.SR.Sch_MismatchTargetNamespaceInclude, xmlSchema2.TargetNamespace, schema.TargetNamespace, xmlSchemaExternal);
				}
			}
			else if (targetNamespace != null && targetNamespace.Length != 0)
			{
				xmlSchema2 = (xmlSchemaExternal.Schema = GetChameleonSchema(targetNamespace, xmlSchema2));
			}
			Preprocess(xmlSchema2, schema.TargetNamespace, imports);
		}
		_currentSchema = schema;
		BuildRefNamespaces(schema);
		ValidateIdAttribute(schema);
		_targetNamespace = ((targetNamespace == null) ? string.Empty : targetNamespace);
		SetSchemaDefaults(schema);
		_processedExternals.Clear();
		for (int j = 0; j < schema.Includes.Count; j++)
		{
			XmlSchemaExternal xmlSchemaExternal2 = (XmlSchemaExternal)schema.Includes[j];
			XmlSchema schema3 = xmlSchemaExternal2.Schema;
			if (schema3 != null)
			{
				switch (xmlSchemaExternal2.Compositor)
				{
				case Compositor.Include:
					if (_processedExternals[schema3] != null)
					{
						continue;
					}
					_processedExternals.Add(schema3, xmlSchemaExternal2);
					CopyIncludedComponents(schema3, schema);
					break;
				case Compositor.Redefine:
					if (_redefinedList == null)
					{
						_redefinedList = new ArrayList();
					}
					_redefinedList.Add(new RedefineEntry(xmlSchemaExternal2 as XmlSchemaRedefine, _rootSchemaForRedefine));
					if (_processedExternals[schema3] != null)
					{
						continue;
					}
					_processedExternals.Add(schema3, xmlSchemaExternal2);
					CopyIncludedComponents(schema3, schema);
					break;
				case Compositor.Import:
					if (schema3 != _rootSchema)
					{
						XmlSchemaImport xmlSchemaImport2 = xmlSchemaExternal2 as XmlSchemaImport;
						string text2 = ((xmlSchemaImport2.Namespace != null) ? xmlSchemaImport2.Namespace : string.Empty);
						if (!imports.Contains(schema3))
						{
							imports.Add(schema3);
						}
						if (!_rootSchema.ImportedNamespaces.Contains(text2))
						{
							_rootSchema.ImportedNamespaces.Add(text2);
						}
					}
					break;
				}
			}
			else if (xmlSchemaExternal2.Compositor == Compositor.Redefine)
			{
				XmlSchemaRedefine xmlSchemaRedefine = xmlSchemaExternal2 as XmlSchemaRedefine;
				if (xmlSchemaRedefine.BaseUri == null)
				{
					for (int k = 0; k < xmlSchemaRedefine.Items.Count; k++)
					{
						if (!(xmlSchemaRedefine.Items[k] is XmlSchemaAnnotation))
						{
							SendValidationEvent(System.SR.Sch_RedefineNoSchema, xmlSchemaRedefine);
							break;
						}
					}
				}
			}
			ValidateIdAttribute(xmlSchemaExternal2);
		}
		List<XmlSchemaObject> list = new List<XmlSchemaObject>();
		XmlSchemaObjectCollection items = schema.Items;
		for (int l = 0; l < items.Count; l++)
		{
			SetParent(items[l], schema);
			if (items[l] is XmlSchemaAttribute xmlSchemaAttribute)
			{
				PreprocessAttribute(xmlSchemaAttribute);
				AddToTable(schema.Attributes, xmlSchemaAttribute.QualifiedName, xmlSchemaAttribute);
			}
			else if (items[l] is XmlSchemaAttributeGroup xmlSchemaAttributeGroup)
			{
				PreprocessAttributeGroup(xmlSchemaAttributeGroup);
				AddToTable(schema.AttributeGroups, xmlSchemaAttributeGroup.QualifiedName, xmlSchemaAttributeGroup);
			}
			else if (items[l] is XmlSchemaComplexType xmlSchemaComplexType)
			{
				PreprocessComplexType(xmlSchemaComplexType, local: false);
				AddToTable(schema.SchemaTypes, xmlSchemaComplexType.QualifiedName, xmlSchemaComplexType);
			}
			else if (items[l] is XmlSchemaSimpleType xmlSchemaSimpleType)
			{
				PreprocessSimpleType(xmlSchemaSimpleType, local: false);
				AddToTable(schema.SchemaTypes, xmlSchemaSimpleType.QualifiedName, xmlSchemaSimpleType);
			}
			else if (items[l] is XmlSchemaElement xmlSchemaElement)
			{
				PreprocessElement(xmlSchemaElement);
				AddToTable(schema.Elements, xmlSchemaElement.QualifiedName, xmlSchemaElement);
			}
			else if (items[l] is XmlSchemaGroup xmlSchemaGroup)
			{
				PreprocessGroup(xmlSchemaGroup);
				AddToTable(schema.Groups, xmlSchemaGroup.QualifiedName, xmlSchemaGroup);
			}
			else if (items[l] is XmlSchemaNotation xmlSchemaNotation)
			{
				PreprocessNotation(xmlSchemaNotation);
				AddToTable(schema.Notations, xmlSchemaNotation.QualifiedName, xmlSchemaNotation);
			}
			else if (items[l] is XmlSchemaAnnotation annotation)
			{
				PreprocessAnnotation(annotation);
			}
			else
			{
				SendValidationEvent(System.SR.Sch_InvalidCollection, items[l]);
				list.Add(items[l]);
			}
		}
		for (int m = 0; m < list.Count; m++)
		{
			schema.Items.Remove(list[m]);
		}
	}

	private void CopyIncludedComponents(XmlSchema includedSchema, XmlSchema schema)
	{
		foreach (XmlSchemaElement value in includedSchema.Elements.Values)
		{
			AddToTable(schema.Elements, value.QualifiedName, value);
		}
		foreach (XmlSchemaAttribute value2 in includedSchema.Attributes.Values)
		{
			AddToTable(schema.Attributes, value2.QualifiedName, value2);
		}
		foreach (XmlSchemaGroup value3 in includedSchema.Groups.Values)
		{
			AddToTable(schema.Groups, value3.QualifiedName, value3);
		}
		foreach (XmlSchemaAttributeGroup value4 in includedSchema.AttributeGroups.Values)
		{
			AddToTable(schema.AttributeGroups, value4.QualifiedName, value4);
		}
		foreach (XmlSchemaType value5 in includedSchema.SchemaTypes.Values)
		{
			AddToTable(schema.SchemaTypes, value5.QualifiedName, value5);
		}
		foreach (XmlSchemaNotation value6 in includedSchema.Notations.Values)
		{
			AddToTable(schema.Notations, value6.QualifiedName, value6);
		}
	}

	private void PreprocessRedefine(RedefineEntry redefineEntry)
	{
		XmlSchemaRedefine redefine = redefineEntry.redefine;
		XmlSchema schema = redefine.Schema;
		_currentSchema = GetParentSchema(redefine);
		SetSchemaDefaults(_currentSchema);
		if (schema.IsRedefined)
		{
			SendValidationEvent(System.SR.Sch_MultipleRedefine, redefine, XmlSeverityType.Warning);
			return;
		}
		schema.IsRedefined = true;
		XmlSchema schemaToUpdate = redefineEntry.schemaToUpdate;
		ArrayList arrayList = new ArrayList();
		GetIncludedSet(schema, arrayList);
		string @namespace = ((schemaToUpdate.TargetNamespace == null) ? string.Empty : schemaToUpdate.TargetNamespace);
		XmlSchemaObjectCollection items = redefine.Items;
		for (int i = 0; i < items.Count; i++)
		{
			SetParent(items[i], redefine);
			if (items[i] is XmlSchemaGroup xmlSchemaGroup)
			{
				PreprocessGroup(xmlSchemaGroup);
				xmlSchemaGroup.QualifiedName.SetNamespace(@namespace);
				if (redefine.Groups[xmlSchemaGroup.QualifiedName] != null)
				{
					SendValidationEvent(System.SR.Sch_GroupDoubleRedefine, xmlSchemaGroup);
					continue;
				}
				AddToTable(redefine.Groups, xmlSchemaGroup.QualifiedName, xmlSchemaGroup);
				XmlSchemaGroup xmlSchemaGroup2 = (XmlSchemaGroup)schemaToUpdate.Groups[xmlSchemaGroup.QualifiedName];
				XmlSchema parentSchema = GetParentSchema(xmlSchemaGroup2);
				if (xmlSchemaGroup2 == null || (parentSchema != schema && !arrayList.Contains(parentSchema)))
				{
					SendValidationEvent(System.SR.Sch_ComponentRedefineNotFound, "<group>", xmlSchemaGroup.QualifiedName.ToString(), xmlSchemaGroup);
					continue;
				}
				xmlSchemaGroup.Redefined = xmlSchemaGroup2;
				schemaToUpdate.Groups.Insert(xmlSchemaGroup.QualifiedName, xmlSchemaGroup);
				CheckRefinedGroup(xmlSchemaGroup);
			}
			else if (items[i] is XmlSchemaAttributeGroup)
			{
				XmlSchemaAttributeGroup xmlSchemaAttributeGroup = (XmlSchemaAttributeGroup)items[i];
				PreprocessAttributeGroup(xmlSchemaAttributeGroup);
				xmlSchemaAttributeGroup.QualifiedName.SetNamespace(@namespace);
				if (redefine.AttributeGroups[xmlSchemaAttributeGroup.QualifiedName] != null)
				{
					SendValidationEvent(System.SR.Sch_AttrGroupDoubleRedefine, xmlSchemaAttributeGroup);
					continue;
				}
				AddToTable(redefine.AttributeGroups, xmlSchemaAttributeGroup.QualifiedName, xmlSchemaAttributeGroup);
				XmlSchemaAttributeGroup xmlSchemaAttributeGroup2 = (XmlSchemaAttributeGroup)schemaToUpdate.AttributeGroups[xmlSchemaAttributeGroup.QualifiedName];
				XmlSchema parentSchema2 = GetParentSchema(xmlSchemaAttributeGroup2);
				if (xmlSchemaAttributeGroup2 == null || (parentSchema2 != schema && !arrayList.Contains(parentSchema2)))
				{
					SendValidationEvent(System.SR.Sch_ComponentRedefineNotFound, "<attributeGroup>", xmlSchemaAttributeGroup.QualifiedName.ToString(), xmlSchemaAttributeGroup);
					continue;
				}
				xmlSchemaAttributeGroup.Redefined = xmlSchemaAttributeGroup2;
				schemaToUpdate.AttributeGroups.Insert(xmlSchemaAttributeGroup.QualifiedName, xmlSchemaAttributeGroup);
				CheckRefinedAttributeGroup(xmlSchemaAttributeGroup);
			}
			else if (items[i] is XmlSchemaComplexType)
			{
				XmlSchemaComplexType xmlSchemaComplexType = (XmlSchemaComplexType)items[i];
				PreprocessComplexType(xmlSchemaComplexType, local: false);
				xmlSchemaComplexType.QualifiedName.SetNamespace(@namespace);
				if (redefine.SchemaTypes[xmlSchemaComplexType.QualifiedName] != null)
				{
					SendValidationEvent(System.SR.Sch_ComplexTypeDoubleRedefine, xmlSchemaComplexType);
					continue;
				}
				AddToTable(redefine.SchemaTypes, xmlSchemaComplexType.QualifiedName, xmlSchemaComplexType);
				XmlSchemaType xmlSchemaType = (XmlSchemaType)schemaToUpdate.SchemaTypes[xmlSchemaComplexType.QualifiedName];
				XmlSchema parentSchema3 = GetParentSchema(xmlSchemaType);
				if (xmlSchemaType == null || (parentSchema3 != schema && !arrayList.Contains(parentSchema3)))
				{
					SendValidationEvent(System.SR.Sch_ComponentRedefineNotFound, "<complexType>", xmlSchemaComplexType.QualifiedName.ToString(), xmlSchemaComplexType);
				}
				else if (xmlSchemaType is XmlSchemaComplexType)
				{
					xmlSchemaComplexType.Redefined = xmlSchemaType;
					schemaToUpdate.SchemaTypes.Insert(xmlSchemaComplexType.QualifiedName, xmlSchemaComplexType);
					CheckRefinedComplexType(xmlSchemaComplexType);
				}
				else
				{
					SendValidationEvent(System.SR.Sch_SimpleToComplexTypeRedefine, xmlSchemaComplexType);
				}
			}
			else
			{
				if (!(items[i] is XmlSchemaSimpleType))
				{
					continue;
				}
				XmlSchemaSimpleType xmlSchemaSimpleType = (XmlSchemaSimpleType)items[i];
				PreprocessSimpleType(xmlSchemaSimpleType, local: false);
				xmlSchemaSimpleType.QualifiedName.SetNamespace(@namespace);
				if (redefine.SchemaTypes[xmlSchemaSimpleType.QualifiedName] != null)
				{
					SendValidationEvent(System.SR.Sch_SimpleTypeDoubleRedefine, xmlSchemaSimpleType);
					continue;
				}
				AddToTable(redefine.SchemaTypes, xmlSchemaSimpleType.QualifiedName, xmlSchemaSimpleType);
				XmlSchemaType xmlSchemaType2 = (XmlSchemaType)schemaToUpdate.SchemaTypes[xmlSchemaSimpleType.QualifiedName];
				XmlSchema parentSchema4 = GetParentSchema(xmlSchemaType2);
				if (xmlSchemaType2 == null || (parentSchema4 != schema && !arrayList.Contains(parentSchema4)))
				{
					SendValidationEvent(System.SR.Sch_ComponentRedefineNotFound, "<simpleType>", xmlSchemaSimpleType.QualifiedName.ToString(), xmlSchemaSimpleType);
				}
				else if (xmlSchemaType2 is XmlSchemaSimpleType)
				{
					xmlSchemaSimpleType.Redefined = xmlSchemaType2;
					schemaToUpdate.SchemaTypes.Insert(xmlSchemaSimpleType.QualifiedName, xmlSchemaSimpleType);
					CheckRefinedSimpleType(xmlSchemaSimpleType);
				}
				else
				{
					SendValidationEvent(System.SR.Sch_ComplexToSimpleTypeRedefine, xmlSchemaSimpleType);
				}
			}
		}
	}

	private void GetIncludedSet(XmlSchema schema, ArrayList includesList)
	{
		if (includesList.Contains(schema))
		{
			return;
		}
		includesList.Add(schema);
		for (int i = 0; i < schema.Includes.Count; i++)
		{
			XmlSchemaExternal xmlSchemaExternal = (XmlSchemaExternal)schema.Includes[i];
			if ((xmlSchemaExternal.Compositor == Compositor.Include || xmlSchemaExternal.Compositor == Compositor.Redefine) && xmlSchemaExternal.Schema != null)
			{
				GetIncludedSet(xmlSchemaExternal.Schema, includesList);
			}
		}
	}

	internal static XmlSchema GetParentSchema(XmlSchemaObject currentSchemaObject)
	{
		XmlSchema xmlSchema = null;
		while (xmlSchema == null && currentSchemaObject != null)
		{
			currentSchemaObject = currentSchemaObject.Parent;
			xmlSchema = currentSchemaObject as XmlSchema;
		}
		return xmlSchema;
	}

	private void SetSchemaDefaults(XmlSchema schema)
	{
		if (schema.BlockDefault == XmlSchemaDerivationMethod.All)
		{
			_blockDefault = XmlSchemaDerivationMethod.All;
		}
		else if (schema.BlockDefault == XmlSchemaDerivationMethod.None)
		{
			_blockDefault = XmlSchemaDerivationMethod.Empty;
		}
		else
		{
			if (((uint)schema.BlockDefault & 0xFFFFFFF8u) != 0)
			{
				SendValidationEvent(System.SR.Sch_InvalidBlockDefaultValue, schema);
			}
			_blockDefault = schema.BlockDefault & (XmlSchemaDerivationMethod.Substitution | XmlSchemaDerivationMethod.Extension | XmlSchemaDerivationMethod.Restriction);
		}
		if (schema.FinalDefault == XmlSchemaDerivationMethod.All)
		{
			_finalDefault = XmlSchemaDerivationMethod.All;
		}
		else if (schema.FinalDefault == XmlSchemaDerivationMethod.None)
		{
			_finalDefault = XmlSchemaDerivationMethod.Empty;
		}
		else
		{
			if (((uint)schema.FinalDefault & 0xFFFFFFE1u) != 0)
			{
				SendValidationEvent(System.SR.Sch_InvalidFinalDefaultValue, schema);
			}
			_finalDefault = schema.FinalDefault & (XmlSchemaDerivationMethod.Extension | XmlSchemaDerivationMethod.Restriction | XmlSchemaDerivationMethod.List | XmlSchemaDerivationMethod.Union);
		}
		_elementFormDefault = schema.ElementFormDefault;
		if (_elementFormDefault == XmlSchemaForm.None)
		{
			_elementFormDefault = XmlSchemaForm.Unqualified;
		}
		_attributeFormDefault = schema.AttributeFormDefault;
		if (_attributeFormDefault == XmlSchemaForm.None)
		{
			_attributeFormDefault = XmlSchemaForm.Unqualified;
		}
	}

	private int CountGroupSelfReference(XmlSchemaObjectCollection items, XmlQualifiedName name, XmlSchemaGroup redefined)
	{
		int num = 0;
		for (int i = 0; i < items.Count; i++)
		{
			if (items[i] is XmlSchemaGroupRef xmlSchemaGroupRef)
			{
				if (xmlSchemaGroupRef.RefName == name)
				{
					xmlSchemaGroupRef.Redefined = redefined;
					if (xmlSchemaGroupRef.MinOccurs != 1m || xmlSchemaGroupRef.MaxOccurs != 1m)
					{
						SendValidationEvent(System.SR.Sch_MinMaxGroupRedefine, xmlSchemaGroupRef);
					}
					num++;
				}
			}
			else if (items[i] is XmlSchemaGroupBase)
			{
				num += CountGroupSelfReference(((XmlSchemaGroupBase)items[i]).Items, name, redefined);
			}
			if (num > 1)
			{
				break;
			}
		}
		return num;
	}

	private void CheckRefinedGroup(XmlSchemaGroup group)
	{
		int num = 0;
		if (group.Particle != null)
		{
			num = CountGroupSelfReference(group.Particle.Items, group.QualifiedName, group.Redefined);
		}
		if (num > 1)
		{
			SendValidationEvent(System.SR.Sch_MultipleGroupSelfRef, group);
		}
		group.SelfReferenceCount = num;
	}

	private void CheckRefinedAttributeGroup(XmlSchemaAttributeGroup attributeGroup)
	{
		int num = 0;
		for (int i = 0; i < attributeGroup.Attributes.Count; i++)
		{
			if (attributeGroup.Attributes[i] is XmlSchemaAttributeGroupRef xmlSchemaAttributeGroupRef && xmlSchemaAttributeGroupRef.RefName == attributeGroup.QualifiedName)
			{
				num++;
			}
		}
		if (num > 1)
		{
			SendValidationEvent(System.SR.Sch_MultipleAttrGroupSelfRef, attributeGroup);
		}
		attributeGroup.SelfReferenceCount = num;
	}

	private void CheckRefinedSimpleType(XmlSchemaSimpleType stype)
	{
		if (stype.Content != null && stype.Content is XmlSchemaSimpleTypeRestriction)
		{
			XmlSchemaSimpleTypeRestriction xmlSchemaSimpleTypeRestriction = (XmlSchemaSimpleTypeRestriction)stype.Content;
			if (xmlSchemaSimpleTypeRestriction.BaseTypeName == stype.QualifiedName)
			{
				return;
			}
		}
		SendValidationEvent(System.SR.Sch_InvalidTypeRedefine, stype);
	}

	private void CheckRefinedComplexType(XmlSchemaComplexType ctype)
	{
		if (ctype.ContentModel != null)
		{
			XmlQualifiedName xmlQualifiedName;
			if (ctype.ContentModel is XmlSchemaComplexContent)
			{
				XmlSchemaComplexContent xmlSchemaComplexContent = (XmlSchemaComplexContent)ctype.ContentModel;
				xmlQualifiedName = ((!(xmlSchemaComplexContent.Content is XmlSchemaComplexContentRestriction)) ? ((XmlSchemaComplexContentExtension)xmlSchemaComplexContent.Content).BaseTypeName : ((XmlSchemaComplexContentRestriction)xmlSchemaComplexContent.Content).BaseTypeName);
			}
			else
			{
				XmlSchemaSimpleContent xmlSchemaSimpleContent = (XmlSchemaSimpleContent)ctype.ContentModel;
				xmlQualifiedName = ((!(xmlSchemaSimpleContent.Content is XmlSchemaSimpleContentRestriction)) ? ((XmlSchemaSimpleContentExtension)xmlSchemaSimpleContent.Content).BaseTypeName : ((XmlSchemaSimpleContentRestriction)xmlSchemaSimpleContent.Content).BaseTypeName);
			}
			if (xmlQualifiedName == ctype.QualifiedName)
			{
				return;
			}
		}
		SendValidationEvent(System.SR.Sch_InvalidTypeRedefine, ctype);
	}

	private void PreprocessAttribute(XmlSchemaAttribute attribute)
	{
		if (attribute.Name != null)
		{
			ValidateNameAttribute(attribute);
			attribute.SetQualifiedName(new XmlQualifiedName(attribute.Name, _targetNamespace));
		}
		else
		{
			SendValidationEvent(System.SR.Sch_MissRequiredAttribute, "name", attribute);
		}
		if (attribute.Use != 0)
		{
			SendValidationEvent(System.SR.Sch_ForbiddenAttribute, "use", attribute);
		}
		if (attribute.Form != 0)
		{
			SendValidationEvent(System.SR.Sch_ForbiddenAttribute, "form", attribute);
		}
		PreprocessAttributeContent(attribute);
		ValidateIdAttribute(attribute);
	}

	private void PreprocessLocalAttribute(XmlSchemaAttribute attribute)
	{
		if (attribute.Name != null)
		{
			ValidateNameAttribute(attribute);
			PreprocessAttributeContent(attribute);
			attribute.SetQualifiedName(new XmlQualifiedName(attribute.Name, (attribute.Form == XmlSchemaForm.Qualified || (attribute.Form == XmlSchemaForm.None && _attributeFormDefault == XmlSchemaForm.Qualified)) ? _targetNamespace : null));
		}
		else
		{
			PreprocessAnnotation(attribute);
			if (attribute.RefName.IsEmpty)
			{
				SendValidationEvent(System.SR.Sch_AttributeNameRef, "???", attribute);
			}
			else
			{
				ValidateQNameAttribute(attribute, "ref", attribute.RefName);
			}
			if (!attribute.SchemaTypeName.IsEmpty || attribute.SchemaType != null || attribute.Form != 0)
			{
				SendValidationEvent(System.SR.Sch_InvalidAttributeRef, attribute);
			}
			attribute.SetQualifiedName(attribute.RefName);
		}
		ValidateIdAttribute(attribute);
	}

	private void PreprocessAttributeContent(XmlSchemaAttribute attribute)
	{
		PreprocessAnnotation(attribute);
		if (Ref.Equal(_currentSchema.TargetNamespace, _nsXsi))
		{
			SendValidationEvent(System.SR.Sch_TargetNamespaceXsi, attribute);
		}
		if (!attribute.RefName.IsEmpty)
		{
			SendValidationEvent(System.SR.Sch_ForbiddenAttribute, "ref", attribute);
		}
		if (attribute.DefaultValue != null && attribute.FixedValue != null)
		{
			SendValidationEvent(System.SR.Sch_DefaultFixedAttributes, attribute);
		}
		if (attribute.DefaultValue != null && attribute.Use != XmlSchemaUse.Optional && attribute.Use != 0)
		{
			SendValidationEvent(System.SR.Sch_OptionalDefaultAttribute, attribute);
		}
		if (attribute.Name == _xmlns)
		{
			SendValidationEvent(System.SR.Sch_XmlNsAttribute, attribute);
		}
		if (attribute.SchemaType != null)
		{
			SetParent(attribute.SchemaType, attribute);
			if (!attribute.SchemaTypeName.IsEmpty)
			{
				SendValidationEvent(System.SR.Sch_TypeMutualExclusive, attribute);
			}
			PreprocessSimpleType(attribute.SchemaType, local: true);
		}
		if (!attribute.SchemaTypeName.IsEmpty)
		{
			ValidateQNameAttribute(attribute, "type", attribute.SchemaTypeName);
		}
	}

	private void PreprocessAttributeGroup(XmlSchemaAttributeGroup attributeGroup)
	{
		if (attributeGroup.Name != null)
		{
			ValidateNameAttribute(attributeGroup);
			attributeGroup.SetQualifiedName(new XmlQualifiedName(attributeGroup.Name, _targetNamespace));
		}
		else
		{
			SendValidationEvent(System.SR.Sch_MissRequiredAttribute, "name", attributeGroup);
		}
		PreprocessAttributes(attributeGroup.Attributes, attributeGroup.AnyAttribute, attributeGroup);
		PreprocessAnnotation(attributeGroup);
		ValidateIdAttribute(attributeGroup);
	}

	private void PreprocessElement(XmlSchemaElement element)
	{
		if (element.Name != null)
		{
			ValidateNameAttribute(element);
			element.SetQualifiedName(new XmlQualifiedName(element.Name, _targetNamespace));
		}
		else
		{
			SendValidationEvent(System.SR.Sch_MissRequiredAttribute, "name", element);
		}
		PreprocessElementContent(element);
		if (element.Final == XmlSchemaDerivationMethod.All)
		{
			element.SetFinalResolved(XmlSchemaDerivationMethod.All);
		}
		else if (element.Final == XmlSchemaDerivationMethod.None)
		{
			if (_finalDefault == XmlSchemaDerivationMethod.All)
			{
				element.SetFinalResolved(XmlSchemaDerivationMethod.All);
			}
			else
			{
				element.SetFinalResolved(_finalDefault & (XmlSchemaDerivationMethod.Extension | XmlSchemaDerivationMethod.Restriction));
			}
		}
		else
		{
			if (((uint)element.Final & 0xFFFFFFF9u) != 0)
			{
				SendValidationEvent(System.SR.Sch_InvalidElementFinalValue, element);
			}
			element.SetFinalResolved(element.Final & (XmlSchemaDerivationMethod.Extension | XmlSchemaDerivationMethod.Restriction));
		}
		if (element.Form != 0)
		{
			SendValidationEvent(System.SR.Sch_ForbiddenAttribute, "form", element);
		}
		if (element.MinOccursString != null)
		{
			SendValidationEvent(System.SR.Sch_ForbiddenAttribute, "minOccurs", element);
		}
		if (element.MaxOccursString != null)
		{
			SendValidationEvent(System.SR.Sch_ForbiddenAttribute, "maxOccurs", element);
		}
		if (!element.SubstitutionGroup.IsEmpty)
		{
			ValidateQNameAttribute(element, "type", element.SubstitutionGroup);
		}
		ValidateIdAttribute(element);
	}

	private void PreprocessLocalElement(XmlSchemaElement element)
	{
		if (element.Name != null)
		{
			ValidateNameAttribute(element);
			PreprocessElementContent(element);
			element.SetQualifiedName(new XmlQualifiedName(element.Name, (element.Form == XmlSchemaForm.Qualified || (element.Form == XmlSchemaForm.None && _elementFormDefault == XmlSchemaForm.Qualified)) ? _targetNamespace : null));
		}
		else
		{
			PreprocessAnnotation(element);
			if (element.RefName.IsEmpty)
			{
				SendValidationEvent(System.SR.Sch_ElementNameRef, element);
			}
			else
			{
				ValidateQNameAttribute(element, "ref", element.RefName);
			}
			if (!element.SchemaTypeName.IsEmpty || element.HasAbstractAttribute || element.Block != XmlSchemaDerivationMethod.None || element.SchemaType != null || element.HasConstraints || element.DefaultValue != null || element.Form != 0 || element.FixedValue != null || element.HasNillableAttribute)
			{
				SendValidationEvent(System.SR.Sch_InvalidElementRef, element);
			}
			if (element.DefaultValue != null && element.FixedValue != null)
			{
				SendValidationEvent(System.SR.Sch_DefaultFixedAttributes, element);
			}
			element.SetQualifiedName(element.RefName);
		}
		if (element.MinOccurs > element.MaxOccurs)
		{
			element.MinOccurs = 0m;
			SendValidationEvent(System.SR.Sch_MinGtMax, element);
		}
		if (element.HasAbstractAttribute)
		{
			SendValidationEvent(System.SR.Sch_ForbiddenAttribute, "abstract", element);
		}
		if (element.Final != XmlSchemaDerivationMethod.None)
		{
			SendValidationEvent(System.SR.Sch_ForbiddenAttribute, "final", element);
		}
		if (!element.SubstitutionGroup.IsEmpty)
		{
			SendValidationEvent(System.SR.Sch_ForbiddenAttribute, "substitutionGroup", element);
		}
		ValidateIdAttribute(element);
	}

	private void PreprocessElementContent(XmlSchemaElement element)
	{
		PreprocessAnnotation(element);
		if (!element.RefName.IsEmpty)
		{
			SendValidationEvent(System.SR.Sch_ForbiddenAttribute, "ref", element);
		}
		if (element.Block == XmlSchemaDerivationMethod.All)
		{
			element.SetBlockResolved(XmlSchemaDerivationMethod.All);
		}
		else if (element.Block == XmlSchemaDerivationMethod.None)
		{
			if (_blockDefault == XmlSchemaDerivationMethod.All)
			{
				element.SetBlockResolved(XmlSchemaDerivationMethod.All);
			}
			else
			{
				element.SetBlockResolved(_blockDefault & (XmlSchemaDerivationMethod.Substitution | XmlSchemaDerivationMethod.Extension | XmlSchemaDerivationMethod.Restriction));
			}
		}
		else
		{
			if (((uint)element.Block & 0xFFFFFFF8u) != 0)
			{
				SendValidationEvent(System.SR.Sch_InvalidElementBlockValue, element);
			}
			element.SetBlockResolved(element.Block & (XmlSchemaDerivationMethod.Substitution | XmlSchemaDerivationMethod.Extension | XmlSchemaDerivationMethod.Restriction));
		}
		if (element.SchemaType != null)
		{
			SetParent(element.SchemaType, element);
			if (!element.SchemaTypeName.IsEmpty)
			{
				SendValidationEvent(System.SR.Sch_TypeMutualExclusive, element);
			}
			if (element.SchemaType is XmlSchemaComplexType)
			{
				PreprocessComplexType((XmlSchemaComplexType)element.SchemaType, local: true);
			}
			else
			{
				PreprocessSimpleType((XmlSchemaSimpleType)element.SchemaType, local: true);
			}
		}
		if (!element.SchemaTypeName.IsEmpty)
		{
			ValidateQNameAttribute(element, "type", element.SchemaTypeName);
		}
		if (element.DefaultValue != null && element.FixedValue != null)
		{
			SendValidationEvent(System.SR.Sch_DefaultFixedAttributes, element);
		}
		for (int i = 0; i < element.Constraints.Count; i++)
		{
			XmlSchemaIdentityConstraint xmlSchemaIdentityConstraint = (XmlSchemaIdentityConstraint)element.Constraints[i];
			SetParent(xmlSchemaIdentityConstraint, element);
			PreprocessIdentityConstraint(xmlSchemaIdentityConstraint);
		}
	}

	private void PreprocessIdentityConstraint(XmlSchemaIdentityConstraint constraint)
	{
		bool flag = true;
		PreprocessAnnotation(constraint);
		if (constraint.Name != null)
		{
			ValidateNameAttribute(constraint);
			constraint.SetQualifiedName(new XmlQualifiedName(constraint.Name, _targetNamespace));
		}
		else
		{
			SendValidationEvent(System.SR.Sch_MissRequiredAttribute, "name", constraint);
			flag = false;
		}
		if (_rootSchema.IdentityConstraints[constraint.QualifiedName] != null)
		{
			SendValidationEvent(System.SR.Sch_DupIdentityConstraint, constraint.QualifiedName.ToString(), constraint);
			flag = false;
		}
		else
		{
			_rootSchema.IdentityConstraints.Add(constraint.QualifiedName, constraint);
		}
		if (constraint.Selector == null)
		{
			SendValidationEvent(System.SR.Sch_IdConstraintNoSelector, constraint);
			flag = false;
		}
		if (constraint.Fields.Count == 0)
		{
			SendValidationEvent(System.SR.Sch_IdConstraintNoFields, constraint);
			flag = false;
		}
		if (constraint is XmlSchemaKeyref)
		{
			XmlSchemaKeyref xmlSchemaKeyref = (XmlSchemaKeyref)constraint;
			if (xmlSchemaKeyref.Refer.IsEmpty)
			{
				SendValidationEvent(System.SR.Sch_IdConstraintNoRefer, constraint);
				flag = false;
			}
			else
			{
				ValidateQNameAttribute(xmlSchemaKeyref, "refer", xmlSchemaKeyref.Refer);
			}
		}
		if (flag)
		{
			ValidateIdAttribute(constraint);
			ValidateIdAttribute(constraint.Selector);
			SetParent(constraint.Selector, constraint);
			for (int i = 0; i < constraint.Fields.Count; i++)
			{
				SetParent(constraint.Fields[i], constraint);
				ValidateIdAttribute(constraint.Fields[i]);
			}
		}
	}

	private void PreprocessSimpleType(XmlSchemaSimpleType simpleType, bool local)
	{
		if (local)
		{
			if (simpleType.Name != null)
			{
				SendValidationEvent(System.SR.Sch_ForbiddenAttribute, "name", simpleType);
			}
		}
		else
		{
			if (simpleType.Name != null)
			{
				ValidateNameAttribute(simpleType);
				simpleType.SetQualifiedName(new XmlQualifiedName(simpleType.Name, _targetNamespace));
			}
			else
			{
				SendValidationEvent(System.SR.Sch_MissRequiredAttribute, "name", simpleType);
			}
			if (simpleType.Final == XmlSchemaDerivationMethod.All)
			{
				simpleType.SetFinalResolved(XmlSchemaDerivationMethod.All);
			}
			else if (simpleType.Final == XmlSchemaDerivationMethod.None)
			{
				if (_finalDefault == XmlSchemaDerivationMethod.All)
				{
					simpleType.SetFinalResolved(XmlSchemaDerivationMethod.All);
				}
				else
				{
					simpleType.SetFinalResolved(_finalDefault & (XmlSchemaDerivationMethod.Extension | XmlSchemaDerivationMethod.Restriction | XmlSchemaDerivationMethod.List | XmlSchemaDerivationMethod.Union));
				}
			}
			else
			{
				if (((uint)simpleType.Final & 0xFFFFFFE1u) != 0)
				{
					SendValidationEvent(System.SR.Sch_InvalidSimpleTypeFinalValue, simpleType);
				}
				simpleType.SetFinalResolved(simpleType.Final & (XmlSchemaDerivationMethod.Extension | XmlSchemaDerivationMethod.Restriction | XmlSchemaDerivationMethod.List | XmlSchemaDerivationMethod.Union));
			}
		}
		if (simpleType.Content == null)
		{
			SendValidationEvent(System.SR.Sch_NoSimpleTypeContent, simpleType);
		}
		else if (simpleType.Content is XmlSchemaSimpleTypeRestriction)
		{
			XmlSchemaSimpleTypeRestriction xmlSchemaSimpleTypeRestriction = (XmlSchemaSimpleTypeRestriction)simpleType.Content;
			SetParent(xmlSchemaSimpleTypeRestriction, simpleType);
			for (int i = 0; i < xmlSchemaSimpleTypeRestriction.Facets.Count; i++)
			{
				SetParent(xmlSchemaSimpleTypeRestriction.Facets[i], xmlSchemaSimpleTypeRestriction);
			}
			if (xmlSchemaSimpleTypeRestriction.BaseType != null)
			{
				if (!xmlSchemaSimpleTypeRestriction.BaseTypeName.IsEmpty)
				{
					SendValidationEvent(System.SR.Sch_SimpleTypeRestRefBase, xmlSchemaSimpleTypeRestriction);
				}
				PreprocessSimpleType(xmlSchemaSimpleTypeRestriction.BaseType, local: true);
			}
			else if (xmlSchemaSimpleTypeRestriction.BaseTypeName.IsEmpty)
			{
				SendValidationEvent(System.SR.Sch_SimpleTypeRestRefBaseNone, xmlSchemaSimpleTypeRestriction);
			}
			else
			{
				ValidateQNameAttribute(xmlSchemaSimpleTypeRestriction, "base", xmlSchemaSimpleTypeRestriction.BaseTypeName);
			}
			PreprocessAnnotation(xmlSchemaSimpleTypeRestriction);
			ValidateIdAttribute(xmlSchemaSimpleTypeRestriction);
		}
		else if (simpleType.Content is XmlSchemaSimpleTypeList)
		{
			XmlSchemaSimpleTypeList xmlSchemaSimpleTypeList = (XmlSchemaSimpleTypeList)simpleType.Content;
			SetParent(xmlSchemaSimpleTypeList, simpleType);
			if (xmlSchemaSimpleTypeList.ItemType != null)
			{
				if (!xmlSchemaSimpleTypeList.ItemTypeName.IsEmpty)
				{
					SendValidationEvent(System.SR.Sch_SimpleTypeListRefBase, xmlSchemaSimpleTypeList);
				}
				SetParent(xmlSchemaSimpleTypeList.ItemType, xmlSchemaSimpleTypeList);
				PreprocessSimpleType(xmlSchemaSimpleTypeList.ItemType, local: true);
			}
			else if (xmlSchemaSimpleTypeList.ItemTypeName.IsEmpty)
			{
				SendValidationEvent(System.SR.Sch_SimpleTypeListRefBaseNone, xmlSchemaSimpleTypeList);
			}
			else
			{
				ValidateQNameAttribute(xmlSchemaSimpleTypeList, "itemType", xmlSchemaSimpleTypeList.ItemTypeName);
			}
			PreprocessAnnotation(xmlSchemaSimpleTypeList);
			ValidateIdAttribute(xmlSchemaSimpleTypeList);
		}
		else
		{
			XmlSchemaSimpleTypeUnion xmlSchemaSimpleTypeUnion = (XmlSchemaSimpleTypeUnion)simpleType.Content;
			SetParent(xmlSchemaSimpleTypeUnion, simpleType);
			int num = xmlSchemaSimpleTypeUnion.BaseTypes.Count;
			if (xmlSchemaSimpleTypeUnion.MemberTypes != null)
			{
				num += xmlSchemaSimpleTypeUnion.MemberTypes.Length;
				XmlQualifiedName[] memberTypes = xmlSchemaSimpleTypeUnion.MemberTypes;
				for (int j = 0; j < memberTypes.Length; j++)
				{
					ValidateQNameAttribute(xmlSchemaSimpleTypeUnion, "memberTypes", memberTypes[j]);
				}
			}
			if (num == 0)
			{
				SendValidationEvent(System.SR.Sch_SimpleTypeUnionNoBase, xmlSchemaSimpleTypeUnion);
			}
			for (int k = 0; k < xmlSchemaSimpleTypeUnion.BaseTypes.Count; k++)
			{
				XmlSchemaSimpleType xmlSchemaSimpleType = (XmlSchemaSimpleType)xmlSchemaSimpleTypeUnion.BaseTypes[k];
				SetParent(xmlSchemaSimpleType, xmlSchemaSimpleTypeUnion);
				PreprocessSimpleType(xmlSchemaSimpleType, local: true);
			}
			PreprocessAnnotation(xmlSchemaSimpleTypeUnion);
			ValidateIdAttribute(xmlSchemaSimpleTypeUnion);
		}
		ValidateIdAttribute(simpleType);
	}

	private void PreprocessComplexType(XmlSchemaComplexType complexType, bool local)
	{
		if (local)
		{
			if (complexType.Name != null)
			{
				SendValidationEvent(System.SR.Sch_ForbiddenAttribute, "name", complexType);
			}
		}
		else
		{
			if (complexType.Name != null)
			{
				ValidateNameAttribute(complexType);
				complexType.SetQualifiedName(new XmlQualifiedName(complexType.Name, _targetNamespace));
			}
			else
			{
				SendValidationEvent(System.SR.Sch_MissRequiredAttribute, "name", complexType);
			}
			if (complexType.Block == XmlSchemaDerivationMethod.All)
			{
				complexType.SetBlockResolved(XmlSchemaDerivationMethod.All);
			}
			else if (complexType.Block == XmlSchemaDerivationMethod.None)
			{
				complexType.SetBlockResolved(_blockDefault & (XmlSchemaDerivationMethod.Extension | XmlSchemaDerivationMethod.Restriction));
			}
			else
			{
				if (((uint)complexType.Block & 0xFFFFFFF9u) != 0)
				{
					SendValidationEvent(System.SR.Sch_InvalidComplexTypeBlockValue, complexType);
				}
				complexType.SetBlockResolved(complexType.Block & (XmlSchemaDerivationMethod.Extension | XmlSchemaDerivationMethod.Restriction));
			}
			if (complexType.Final == XmlSchemaDerivationMethod.All)
			{
				complexType.SetFinalResolved(XmlSchemaDerivationMethod.All);
			}
			else if (complexType.Final == XmlSchemaDerivationMethod.None)
			{
				if (_finalDefault == XmlSchemaDerivationMethod.All)
				{
					complexType.SetFinalResolved(XmlSchemaDerivationMethod.All);
				}
				else
				{
					complexType.SetFinalResolved(_finalDefault & (XmlSchemaDerivationMethod.Extension | XmlSchemaDerivationMethod.Restriction));
				}
			}
			else
			{
				if (((uint)complexType.Final & 0xFFFFFFF9u) != 0)
				{
					SendValidationEvent(System.SR.Sch_InvalidComplexTypeFinalValue, complexType);
				}
				complexType.SetFinalResolved(complexType.Final & (XmlSchemaDerivationMethod.Extension | XmlSchemaDerivationMethod.Restriction));
			}
		}
		if (complexType.ContentModel != null)
		{
			SetParent(complexType.ContentModel, complexType);
			PreprocessAnnotation(complexType.ContentModel);
			if (complexType.Particle == null)
			{
				_ = complexType.Attributes;
			}
			if (complexType.ContentModel is XmlSchemaSimpleContent)
			{
				XmlSchemaSimpleContent xmlSchemaSimpleContent = (XmlSchemaSimpleContent)complexType.ContentModel;
				if (xmlSchemaSimpleContent.Content == null)
				{
					if (complexType.QualifiedName == XmlQualifiedName.Empty)
					{
						SendValidationEvent(System.SR.Sch_NoRestOrExt, complexType);
					}
					else
					{
						SendValidationEvent(System.SR.Sch_NoRestOrExtQName, complexType.QualifiedName.Name, complexType.QualifiedName.Namespace, complexType);
					}
				}
				else
				{
					SetParent(xmlSchemaSimpleContent.Content, xmlSchemaSimpleContent);
					PreprocessAnnotation(xmlSchemaSimpleContent.Content);
					if (xmlSchemaSimpleContent.Content is XmlSchemaSimpleContentExtension)
					{
						XmlSchemaSimpleContentExtension xmlSchemaSimpleContentExtension = (XmlSchemaSimpleContentExtension)xmlSchemaSimpleContent.Content;
						if (xmlSchemaSimpleContentExtension.BaseTypeName.IsEmpty)
						{
							SendValidationEvent(System.SR.Sch_MissAttribute, "base", xmlSchemaSimpleContentExtension);
						}
						else
						{
							ValidateQNameAttribute(xmlSchemaSimpleContentExtension, "base", xmlSchemaSimpleContentExtension.BaseTypeName);
						}
						PreprocessAttributes(xmlSchemaSimpleContentExtension.Attributes, xmlSchemaSimpleContentExtension.AnyAttribute, xmlSchemaSimpleContentExtension);
						ValidateIdAttribute(xmlSchemaSimpleContentExtension);
					}
					else
					{
						XmlSchemaSimpleContentRestriction xmlSchemaSimpleContentRestriction = (XmlSchemaSimpleContentRestriction)xmlSchemaSimpleContent.Content;
						if (xmlSchemaSimpleContentRestriction.BaseTypeName.IsEmpty)
						{
							SendValidationEvent(System.SR.Sch_MissAttribute, "base", xmlSchemaSimpleContentRestriction);
						}
						else
						{
							ValidateQNameAttribute(xmlSchemaSimpleContentRestriction, "base", xmlSchemaSimpleContentRestriction.BaseTypeName);
						}
						if (xmlSchemaSimpleContentRestriction.BaseType != null)
						{
							SetParent(xmlSchemaSimpleContentRestriction.BaseType, xmlSchemaSimpleContentRestriction);
							PreprocessSimpleType(xmlSchemaSimpleContentRestriction.BaseType, local: true);
						}
						PreprocessAttributes(xmlSchemaSimpleContentRestriction.Attributes, xmlSchemaSimpleContentRestriction.AnyAttribute, xmlSchemaSimpleContentRestriction);
						ValidateIdAttribute(xmlSchemaSimpleContentRestriction);
					}
				}
				ValidateIdAttribute(xmlSchemaSimpleContent);
			}
			else
			{
				XmlSchemaComplexContent xmlSchemaComplexContent = (XmlSchemaComplexContent)complexType.ContentModel;
				if (xmlSchemaComplexContent.Content == null)
				{
					if (complexType.QualifiedName == XmlQualifiedName.Empty)
					{
						SendValidationEvent(System.SR.Sch_NoRestOrExt, complexType);
					}
					else
					{
						SendValidationEvent(System.SR.Sch_NoRestOrExtQName, complexType.QualifiedName.Name, complexType.QualifiedName.Namespace, complexType);
					}
				}
				else
				{
					if (!xmlSchemaComplexContent.HasMixedAttribute && complexType.IsMixed)
					{
						xmlSchemaComplexContent.IsMixed = true;
					}
					SetParent(xmlSchemaComplexContent.Content, xmlSchemaComplexContent);
					PreprocessAnnotation(xmlSchemaComplexContent.Content);
					if (xmlSchemaComplexContent.Content is XmlSchemaComplexContentExtension)
					{
						XmlSchemaComplexContentExtension xmlSchemaComplexContentExtension = (XmlSchemaComplexContentExtension)xmlSchemaComplexContent.Content;
						if (xmlSchemaComplexContentExtension.BaseTypeName.IsEmpty)
						{
							SendValidationEvent(System.SR.Sch_MissAttribute, "base", xmlSchemaComplexContentExtension);
						}
						else
						{
							ValidateQNameAttribute(xmlSchemaComplexContentExtension, "base", xmlSchemaComplexContentExtension.BaseTypeName);
						}
						if (xmlSchemaComplexContentExtension.Particle != null)
						{
							SetParent(xmlSchemaComplexContentExtension.Particle, xmlSchemaComplexContentExtension);
							PreprocessParticle(xmlSchemaComplexContentExtension.Particle);
						}
						PreprocessAttributes(xmlSchemaComplexContentExtension.Attributes, xmlSchemaComplexContentExtension.AnyAttribute, xmlSchemaComplexContentExtension);
						ValidateIdAttribute(xmlSchemaComplexContentExtension);
					}
					else
					{
						XmlSchemaComplexContentRestriction xmlSchemaComplexContentRestriction = (XmlSchemaComplexContentRestriction)xmlSchemaComplexContent.Content;
						if (xmlSchemaComplexContentRestriction.BaseTypeName.IsEmpty)
						{
							SendValidationEvent(System.SR.Sch_MissAttribute, "base", xmlSchemaComplexContentRestriction);
						}
						else
						{
							ValidateQNameAttribute(xmlSchemaComplexContentRestriction, "base", xmlSchemaComplexContentRestriction.BaseTypeName);
						}
						if (xmlSchemaComplexContentRestriction.Particle != null)
						{
							SetParent(xmlSchemaComplexContentRestriction.Particle, xmlSchemaComplexContentRestriction);
							PreprocessParticle(xmlSchemaComplexContentRestriction.Particle);
						}
						PreprocessAttributes(xmlSchemaComplexContentRestriction.Attributes, xmlSchemaComplexContentRestriction.AnyAttribute, xmlSchemaComplexContentRestriction);
						ValidateIdAttribute(xmlSchemaComplexContentRestriction);
					}
					ValidateIdAttribute(xmlSchemaComplexContent);
				}
			}
		}
		else
		{
			if (complexType.Particle != null)
			{
				SetParent(complexType.Particle, complexType);
				PreprocessParticle(complexType.Particle);
			}
			PreprocessAttributes(complexType.Attributes, complexType.AnyAttribute, complexType);
		}
		ValidateIdAttribute(complexType);
	}

	private void PreprocessGroup(XmlSchemaGroup group)
	{
		if (group.Name != null)
		{
			ValidateNameAttribute(group);
			group.SetQualifiedName(new XmlQualifiedName(group.Name, _targetNamespace));
		}
		else
		{
			SendValidationEvent(System.SR.Sch_MissRequiredAttribute, "name", group);
		}
		if (group.Particle == null)
		{
			SendValidationEvent(System.SR.Sch_NoGroupParticle, group);
			return;
		}
		if (group.Particle.MinOccursString != null)
		{
			SendValidationEvent(System.SR.Sch_ForbiddenAttribute, "minOccurs", group.Particle);
		}
		if (group.Particle.MaxOccursString != null)
		{
			SendValidationEvent(System.SR.Sch_ForbiddenAttribute, "maxOccurs", group.Particle);
		}
		PreprocessParticle(group.Particle);
		PreprocessAnnotation(group);
		ValidateIdAttribute(group);
	}

	private void PreprocessNotation(XmlSchemaNotation notation)
	{
		if (notation.Name != null)
		{
			ValidateNameAttribute(notation);
			notation.QualifiedName = new XmlQualifiedName(notation.Name, _targetNamespace);
		}
		else
		{
			SendValidationEvent(System.SR.Sch_MissRequiredAttribute, "name", notation);
		}
		if (notation.Public == null && notation.System == null)
		{
			SendValidationEvent(System.SR.Sch_MissingPublicSystemAttribute, notation);
		}
		else
		{
			if (notation.Public != null)
			{
				try
				{
					XmlConvert.VerifyTOKEN(notation.Public);
				}
				catch (XmlException innerException)
				{
					SendValidationEvent(System.SR.Sch_InvalidPublicAttribute, new string[1] { notation.Public }, innerException, notation);
				}
			}
			if (notation.System != null)
			{
				ParseUri(notation.System, System.SR.Sch_InvalidSystemAttribute, notation);
			}
		}
		PreprocessAnnotation(notation);
		ValidateIdAttribute(notation);
	}

	private void PreprocessParticle(XmlSchemaParticle particle)
	{
		if (particle is XmlSchemaAll)
		{
			if (particle.MinOccurs != 0m && particle.MinOccurs != 1m)
			{
				particle.MinOccurs = 1m;
				SendValidationEvent(System.SR.Sch_InvalidAllMin, particle);
			}
			if (particle.MaxOccurs != 1m)
			{
				particle.MaxOccurs = 1m;
				SendValidationEvent(System.SR.Sch_InvalidAllMax, particle);
			}
			XmlSchemaObjectCollection items = ((XmlSchemaAll)particle).Items;
			for (int i = 0; i < items.Count; i++)
			{
				XmlSchemaElement xmlSchemaElement = (XmlSchemaElement)items[i];
				if (xmlSchemaElement.MaxOccurs != 0m && xmlSchemaElement.MaxOccurs != 1m)
				{
					xmlSchemaElement.MaxOccurs = 1m;
					SendValidationEvent(System.SR.Sch_InvalidAllElementMax, xmlSchemaElement);
				}
				SetParent(xmlSchemaElement, particle);
				PreprocessLocalElement(xmlSchemaElement);
			}
		}
		else
		{
			if (particle.MinOccurs > particle.MaxOccurs)
			{
				particle.MinOccurs = particle.MaxOccurs;
				SendValidationEvent(System.SR.Sch_MinGtMax, particle);
			}
			if (particle is XmlSchemaChoice)
			{
				XmlSchemaObjectCollection items = ((XmlSchemaChoice)particle).Items;
				for (int j = 0; j < items.Count; j++)
				{
					SetParent(items[j], particle);
					if (items[j] is XmlSchemaElement element)
					{
						PreprocessLocalElement(element);
					}
					else
					{
						PreprocessParticle((XmlSchemaParticle)items[j]);
					}
				}
			}
			else if (particle is XmlSchemaSequence)
			{
				XmlSchemaObjectCollection items = ((XmlSchemaSequence)particle).Items;
				for (int k = 0; k < items.Count; k++)
				{
					SetParent(items[k], particle);
					if (items[k] is XmlSchemaElement element2)
					{
						PreprocessLocalElement(element2);
					}
					else
					{
						PreprocessParticle((XmlSchemaParticle)items[k]);
					}
				}
			}
			else if (particle is XmlSchemaGroupRef)
			{
				XmlSchemaGroupRef xmlSchemaGroupRef = (XmlSchemaGroupRef)particle;
				if (xmlSchemaGroupRef.RefName.IsEmpty)
				{
					SendValidationEvent(System.SR.Sch_MissAttribute, "ref", xmlSchemaGroupRef);
				}
				else
				{
					ValidateQNameAttribute(xmlSchemaGroupRef, "ref", xmlSchemaGroupRef.RefName);
				}
			}
			else if (particle is XmlSchemaAny)
			{
				try
				{
					((XmlSchemaAny)particle).BuildNamespaceList(_targetNamespace);
				}
				catch (FormatException ex)
				{
					SendValidationEvent(System.SR.Sch_InvalidAnyDetailed, new string[1] { ex.Message }, ex, particle);
				}
			}
		}
		PreprocessAnnotation(particle);
		ValidateIdAttribute(particle);
	}

	private void PreprocessAttributes(XmlSchemaObjectCollection attributes, XmlSchemaAnyAttribute anyAttribute, XmlSchemaObject parent)
	{
		for (int i = 0; i < attributes.Count; i++)
		{
			SetParent(attributes[i], parent);
			if (attributes[i] is XmlSchemaAttribute attribute)
			{
				PreprocessLocalAttribute(attribute);
				continue;
			}
			XmlSchemaAttributeGroupRef xmlSchemaAttributeGroupRef = (XmlSchemaAttributeGroupRef)attributes[i];
			if (xmlSchemaAttributeGroupRef.RefName.IsEmpty)
			{
				SendValidationEvent(System.SR.Sch_MissAttribute, "ref", xmlSchemaAttributeGroupRef);
			}
			else
			{
				ValidateQNameAttribute(xmlSchemaAttributeGroupRef, "ref", xmlSchemaAttributeGroupRef.RefName);
			}
			PreprocessAnnotation(attributes[i]);
			ValidateIdAttribute(attributes[i]);
		}
		if (anyAttribute != null)
		{
			try
			{
				SetParent(anyAttribute, parent);
				PreprocessAnnotation(anyAttribute);
				anyAttribute.BuildNamespaceList(_targetNamespace);
			}
			catch (FormatException ex)
			{
				SendValidationEvent(System.SR.Sch_InvalidAnyDetailed, new string[1] { ex.Message }, ex, anyAttribute);
			}
			ValidateIdAttribute(anyAttribute);
		}
	}

	private void ValidateIdAttribute(XmlSchemaObject xso)
	{
		if (xso.IdAttribute != null)
		{
			try
			{
				xso.IdAttribute = base.NameTable.Add(XmlConvert.VerifyNCName(xso.IdAttribute));
			}
			catch (XmlException ex)
			{
				SendValidationEvent(System.SR.Sch_InvalidIdAttribute, new string[1] { ex.Message }, ex, xso);
				return;
			}
			catch (ArgumentNullException)
			{
				SendValidationEvent(System.SR.Sch_InvalidIdAttribute, System.SR.Sch_NullValue, xso);
				return;
			}
			try
			{
				_currentSchema.Ids.Add(xso.IdAttribute, xso);
			}
			catch (ArgumentException)
			{
				SendValidationEvent(System.SR.Sch_DupIdAttribute, xso);
			}
		}
	}

	private void ValidateNameAttribute(XmlSchemaObject xso)
	{
		string nameAttribute = xso.NameAttribute;
		if (nameAttribute == null || nameAttribute.Length == 0)
		{
			SendValidationEvent(System.SR.Sch_InvalidNameAttributeEx, null, System.SR.Sch_NullValue, xso);
		}
		nameAttribute = XmlComplianceUtil.NonCDataNormalize(nameAttribute ?? string.Empty);
		int num = ValidateNames.ParseNCName(nameAttribute, 0);
		if (num != nameAttribute.Length)
		{
			string[] array = XmlException.BuildCharExceptionArgs(nameAttribute, num);
			string msg = System.SR.Format(System.SR.Xml_BadNameCharWithPos, array[0], array[1], num);
			SendValidationEvent(System.SR.Sch_InvalidNameAttributeEx, nameAttribute, msg, xso);
		}
		else
		{
			xso.NameAttribute = base.NameTable.Add(nameAttribute);
		}
	}

	private void ValidateQNameAttribute(XmlSchemaObject xso, string attributeName, XmlQualifiedName value)
	{
		try
		{
			value.Verify();
			value.Atomize(base.NameTable);
			if (_currentSchema.IsChameleon && value.Namespace.Length == 0)
			{
				value.SetNamespace(_currentSchema.TargetNamespace);
			}
			if (_referenceNamespaces[value.Namespace] == null)
			{
				SendValidationEvent(System.SR.Sch_UnrefNS, value.Namespace, xso, XmlSeverityType.Warning);
			}
		}
		catch (FormatException ex)
		{
			SendValidationEvent(System.SR.Sch_InvalidAttribute, new string[2] { attributeName, ex.Message }, ex, xso);
		}
		catch (XmlException ex2)
		{
			SendValidationEvent(System.SR.Sch_InvalidAttribute, new string[2] { attributeName, ex2.Message }, ex2, xso);
		}
	}

	private Uri ResolveSchemaLocationUri(XmlSchema enclosingSchema, string location)
	{
		if (location.Length == 0)
		{
			return null;
		}
		return _xmlResolver.ResolveUri(enclosingSchema.BaseUri, location);
	}

	private object GetSchemaEntity(Uri ruri)
	{
		return _xmlResolver.GetEntity(ruri, null, null);
	}

	private XmlSchema GetChameleonSchema(string targetNamespace, XmlSchema schema)
	{
		ChameleonKey key = new ChameleonKey(targetNamespace, schema);
		XmlSchema xmlSchema = (XmlSchema)_chameleonSchemas[key];
		if (xmlSchema == null)
		{
			xmlSchema = schema.DeepClone();
			xmlSchema.IsChameleon = true;
			xmlSchema.TargetNamespace = targetNamespace;
			_chameleonSchemas.Add(key, xmlSchema);
			xmlSchema.SourceUri = schema.SourceUri;
			schema.IsProcessing = false;
		}
		return xmlSchema;
	}

	private void SetParent(XmlSchemaObject child, XmlSchemaObject parent)
	{
		child.Parent = parent;
	}

	private void PreprocessAnnotation(XmlSchemaObject schemaObject)
	{
		if (schemaObject is XmlSchemaAnnotated xmlSchemaAnnotated)
		{
			XmlSchemaAnnotation annotation = xmlSchemaAnnotated.Annotation;
			if (annotation != null)
			{
				PreprocessAnnotation(annotation);
				annotation.Parent = schemaObject;
			}
		}
	}

	private void PreprocessAnnotation(XmlSchemaAnnotation annotation)
	{
		ValidateIdAttribute(annotation);
		for (int i = 0; i < annotation.Items.Count; i++)
		{
			annotation.Items[i].Parent = annotation;
		}
	}
}
