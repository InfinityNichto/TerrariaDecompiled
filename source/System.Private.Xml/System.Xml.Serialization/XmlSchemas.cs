using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Xml.Schema;

namespace System.Xml.Serialization;

public class XmlSchemas : CollectionBase, IEnumerable<XmlSchema>, IEnumerable
{
	private XmlSchemaSet _schemaSet;

	private Hashtable _references;

	private SchemaObjectCache _cache;

	private bool _shareTypes;

	private Hashtable _mergedSchemas;

	internal Hashtable delayedSchemas = new Hashtable();

	private bool _isCompiled;

	private static volatile XmlSchema s_xsd;

	private static volatile XmlSchema s_xml;

	public XmlSchema this[int index]
	{
		get
		{
			return (XmlSchema)base.List[index];
		}
		set
		{
			base.List[index] = value;
		}
	}

	public XmlSchema? this[string? ns]
	{
		get
		{
			IList list = (IList)SchemaSet.Schemas(ns);
			if (list.Count == 0)
			{
				return null;
			}
			if (list.Count == 1)
			{
				return (XmlSchema)list[0];
			}
			throw new InvalidOperationException(System.SR.Format(System.SR.XmlSchemaDuplicateNamespace, ns));
		}
	}

	internal SchemaObjectCache Cache
	{
		get
		{
			if (_cache == null)
			{
				_cache = new SchemaObjectCache();
			}
			return _cache;
		}
	}

	internal Hashtable MergedSchemas
	{
		get
		{
			if (_mergedSchemas == null)
			{
				_mergedSchemas = new Hashtable();
			}
			return _mergedSchemas;
		}
	}

	internal Hashtable References
	{
		get
		{
			if (_references == null)
			{
				_references = new Hashtable();
			}
			return _references;
		}
	}

	internal XmlSchemaSet SchemaSet
	{
		get
		{
			if (_schemaSet == null)
			{
				_schemaSet = new XmlSchemaSet();
				_schemaSet.XmlResolver = null;
				_schemaSet.ValidationEventHandler += IgnoreCompileErrors;
			}
			return _schemaSet;
		}
	}

	public bool IsCompiled => _isCompiled;

	internal static XmlSchema XsdSchema
	{
		get
		{
			if (s_xsd == null)
			{
				s_xsd = CreateFakeXsdSchema("http://www.w3.org/2001/XMLSchema", "schema");
			}
			return s_xsd;
		}
	}

	internal static XmlSchema XmlSchema
	{
		get
		{
			if (s_xml == null)
			{
				s_xml = System.Xml.Schema.XmlSchema.Read(new StringReader("<?xml version='1.0' encoding='UTF-8' ?>\r\n<xs:schema targetNamespace='http://www.w3.org/XML/1998/namespace' xmlns:xs='http://www.w3.org/2001/XMLSchema' xml:lang='en'>\r\n <xs:attribute name='lang' type='xs:language'/>\r\n <xs:attribute name='space'>\r\n  <xs:simpleType>\r\n   <xs:restriction base='xs:NCName'>\r\n    <xs:enumeration value='default'/>\r\n    <xs:enumeration value='preserve'/>\r\n   </xs:restriction>\r\n  </xs:simpleType>\r\n </xs:attribute>\r\n <xs:attribute name='base' type='xs:anyURI'/>\r\n <xs:attribute name='id' type='xs:ID' />\r\n <xs:attributeGroup name='specialAttrs'>\r\n  <xs:attribute ref='xml:base'/>\r\n  <xs:attribute ref='xml:lang'/>\r\n  <xs:attribute ref='xml:space'/>\r\n </xs:attributeGroup>\r\n</xs:schema>"), null);
			}
			return s_xml;
		}
	}

	public IList GetSchemas(string? ns)
	{
		return (IList)SchemaSet.Schemas(ns);
	}

	internal int Add(XmlSchema schema, bool delay)
	{
		if (delay)
		{
			if (delayedSchemas[schema] == null)
			{
				delayedSchemas.Add(schema, schema);
			}
			return -1;
		}
		return Add(schema);
	}

	public int Add(XmlSchema schema)
	{
		if (base.List.Contains(schema))
		{
			return base.List.IndexOf(schema);
		}
		return base.List.Add(schema);
	}

	public int Add(XmlSchema schema, Uri? baseUri)
	{
		if (base.List.Contains(schema))
		{
			return base.List.IndexOf(schema);
		}
		if (baseUri != null)
		{
			schema.BaseUri = baseUri;
		}
		return base.List.Add(schema);
	}

	public void Add(XmlSchemas schemas)
	{
		foreach (XmlSchema schema in schemas)
		{
			Add(schema);
		}
	}

	public void AddReference(XmlSchema schema)
	{
		References[schema] = schema;
	}

	public void Insert(int index, XmlSchema schema)
	{
		base.List.Insert(index, schema);
	}

	public int IndexOf(XmlSchema schema)
	{
		return base.List.IndexOf(schema);
	}

	public bool Contains(XmlSchema schema)
	{
		return base.List.Contains(schema);
	}

	public bool Contains(string? targetNamespace)
	{
		return SchemaSet.Contains(targetNamespace);
	}

	public void Remove(XmlSchema schema)
	{
		base.List.Remove(schema);
	}

	public void CopyTo(XmlSchema[] array, int index)
	{
		base.List.CopyTo(array, index);
	}

	protected override void OnInsert(int index, object? value)
	{
		AddName((XmlSchema)value);
	}

	protected override void OnRemove(int index, object? value)
	{
		RemoveName((XmlSchema)value);
	}

	protected override void OnClear()
	{
		_schemaSet = null;
	}

	protected override void OnSet(int index, object? oldValue, object? newValue)
	{
		RemoveName((XmlSchema)oldValue);
		AddName((XmlSchema)newValue);
	}

	private void AddName(XmlSchema schema)
	{
		if (_isCompiled)
		{
			throw new InvalidOperationException(System.SR.XmlSchemaCompiled);
		}
		if (SchemaSet.Contains(schema))
		{
			SchemaSet.Reprocess(schema);
			return;
		}
		Prepare(schema);
		SchemaSet.Add(schema);
	}

	private void Prepare(XmlSchema schema)
	{
		ArrayList arrayList = new ArrayList();
		string targetNamespace = schema.TargetNamespace;
		foreach (XmlSchemaExternal include in schema.Includes)
		{
			if (include is XmlSchemaImport && targetNamespace == ((XmlSchemaImport)include).Namespace)
			{
				arrayList.Add(include);
			}
		}
		foreach (XmlSchemaObject item in arrayList)
		{
			schema.Includes.Remove(item);
		}
	}

	private void RemoveName(XmlSchema schema)
	{
		SchemaSet.Remove(schema);
	}

	public object? Find(XmlQualifiedName name, Type type)
	{
		return Find(name, type, checkCache: true);
	}

	internal object Find(XmlQualifiedName name, Type type, bool checkCache)
	{
		if (!IsCompiled)
		{
			foreach (XmlSchema item in base.List)
			{
				Preprocess(item);
			}
		}
		IList list = (IList)SchemaSet.Schemas(name.Namespace);
		if (list == null)
		{
			return null;
		}
		foreach (XmlSchema item2 in list)
		{
			Preprocess(item2);
			XmlSchemaObject xmlSchemaObject = null;
			if (typeof(XmlSchemaType).IsAssignableFrom(type))
			{
				xmlSchemaObject = item2.SchemaTypes[name];
				if (xmlSchemaObject == null || !type.IsAssignableFrom(xmlSchemaObject.GetType()))
				{
					continue;
				}
			}
			else if (type == typeof(XmlSchemaGroup))
			{
				xmlSchemaObject = item2.Groups[name];
			}
			else if (type == typeof(XmlSchemaAttributeGroup))
			{
				xmlSchemaObject = item2.AttributeGroups[name];
			}
			else if (type == typeof(XmlSchemaElement))
			{
				xmlSchemaObject = item2.Elements[name];
			}
			else if (type == typeof(XmlSchemaAttribute))
			{
				xmlSchemaObject = item2.Attributes[name];
			}
			else if (type == typeof(XmlSchemaNotation))
			{
				xmlSchemaObject = item2.Notations[name];
			}
			if (xmlSchemaObject != null && _shareTypes && checkCache && !IsReference(xmlSchemaObject))
			{
				xmlSchemaObject = Cache.AddItem(xmlSchemaObject, name, this);
			}
			if (xmlSchemaObject != null)
			{
				return xmlSchemaObject;
			}
		}
		return null;
	}

	IEnumerator<XmlSchema> IEnumerable<XmlSchema>.GetEnumerator()
	{
		return new XmlSchemaEnumerator(this);
	}

	internal static void Preprocess(XmlSchema schema)
	{
		if (!schema.IsPreprocessed)
		{
			try
			{
				XmlNameTable nameTable = new System.Xml.NameTable();
				Preprocessor preprocessor = new Preprocessor(nameTable, new SchemaNames(nameTable), null);
				preprocessor.SchemaLocations = new Hashtable();
				preprocessor.Execute(schema, schema.TargetNamespace, loadExternals: false);
			}
			catch (XmlSchemaException ex)
			{
				throw CreateValidationException(ex, ex.Message);
			}
		}
	}

	public static bool IsDataSet(XmlSchema schema)
	{
		foreach (XmlSchemaObject item in schema.Items)
		{
			if (!(item is XmlSchemaElement))
			{
				continue;
			}
			XmlSchemaElement xmlSchemaElement = (XmlSchemaElement)item;
			if (xmlSchemaElement.UnhandledAttributes == null)
			{
				continue;
			}
			XmlAttribute[] unhandledAttributes = xmlSchemaElement.UnhandledAttributes;
			foreach (XmlAttribute xmlAttribute in unhandledAttributes)
			{
				if (xmlAttribute.LocalName == "IsDataSet" && xmlAttribute.NamespaceURI == "urn:schemas-microsoft-com:xml-msdata" && (xmlAttribute.Value == "True" || xmlAttribute.Value == "true" || xmlAttribute.Value == "1"))
				{
					return true;
				}
			}
		}
		return false;
	}

	[RequiresUnreferencedCode("calls Merge")]
	private void Merge(XmlSchema schema)
	{
		if (MergedSchemas[schema] == null)
		{
			IList list = (IList)SchemaSet.Schemas(schema.TargetNamespace);
			if (list != null && list.Count > 0)
			{
				MergedSchemas.Add(schema, schema);
				Merge(list, schema);
			}
			else
			{
				Add(schema);
				MergedSchemas.Add(schema, schema);
			}
		}
	}

	private void AddImport(IList schemas, string ns)
	{
		foreach (XmlSchema schema in schemas)
		{
			bool flag = true;
			foreach (XmlSchemaExternal include in schema.Includes)
			{
				if (include is XmlSchemaImport && ((XmlSchemaImport)include).Namespace == ns)
				{
					flag = false;
					break;
				}
			}
			if (flag)
			{
				XmlSchemaImport xmlSchemaImport = new XmlSchemaImport();
				xmlSchemaImport.Namespace = ns;
				schema.Includes.Add(xmlSchemaImport);
			}
		}
	}

	[RequiresUnreferencedCode("Calls MergeFailedMessage")]
	private void Merge(IList originals, XmlSchema schema)
	{
		foreach (XmlSchema original in originals)
		{
			if (schema == original)
			{
				return;
			}
		}
		foreach (XmlSchemaExternal include in schema.Includes)
		{
			if (include is XmlSchemaImport)
			{
				include.SchemaLocation = null;
				if (include.Schema != null)
				{
					Merge(include.Schema);
				}
				else
				{
					AddImport(originals, ((XmlSchemaImport)include).Namespace);
				}
			}
			else if (include.Schema == null)
			{
				if (include.SchemaLocation != null)
				{
					throw new InvalidOperationException(System.SR.Format(System.SR.XmlSchemaIncludeLocation, GetType().Name, include.SchemaLocation));
				}
			}
			else
			{
				include.SchemaLocation = null;
				Merge(originals, include.Schema);
			}
		}
		bool[] array = new bool[schema.Items.Count];
		int num = 0;
		for (int i = 0; i < schema.Items.Count; i++)
		{
			XmlSchemaObject xmlSchemaObject = schema.Items[i];
			XmlSchemaObject xmlSchemaObject2 = Find(xmlSchemaObject, originals);
			if (xmlSchemaObject2 != null)
			{
				if (!Cache.Match(xmlSchemaObject2, xmlSchemaObject, _shareTypes))
				{
					throw new InvalidOperationException(MergeFailedMessage(xmlSchemaObject, xmlSchemaObject2, schema.TargetNamespace));
				}
				array[i] = true;
				num++;
			}
		}
		if (num == schema.Items.Count)
		{
			return;
		}
		XmlSchema xmlSchema2 = (XmlSchema)originals[0];
		for (int j = 0; j < schema.Items.Count; j++)
		{
			if (!array[j])
			{
				xmlSchema2.Items.Add(schema.Items[j]);
			}
		}
		xmlSchema2.IsPreprocessed = false;
		Preprocess(xmlSchema2);
	}

	private static string ItemName(XmlSchemaObject o)
	{
		if (o is XmlSchemaNotation)
		{
			return ((XmlSchemaNotation)o).Name;
		}
		if (o is XmlSchemaGroup)
		{
			return ((XmlSchemaGroup)o).Name;
		}
		if (o is XmlSchemaElement)
		{
			return ((XmlSchemaElement)o).Name;
		}
		if (o is XmlSchemaType)
		{
			return ((XmlSchemaType)o).Name;
		}
		if (o is XmlSchemaAttributeGroup)
		{
			return ((XmlSchemaAttributeGroup)o).Name;
		}
		if (o is XmlSchemaAttribute)
		{
			return ((XmlSchemaAttribute)o).Name;
		}
		return null;
	}

	internal static XmlQualifiedName GetParentName(XmlSchemaObject item)
	{
		while (item.Parent != null)
		{
			if (item.Parent is XmlSchemaType)
			{
				XmlSchemaType xmlSchemaType = (XmlSchemaType)item.Parent;
				if (xmlSchemaType.Name != null && xmlSchemaType.Name.Length != 0)
				{
					return xmlSchemaType.QualifiedName;
				}
			}
			item = item.Parent;
		}
		return XmlQualifiedName.Empty;
	}

	[return: NotNullIfNotNull("o")]
	private static string GetSchemaItem(XmlSchemaObject o, string ns, string details)
	{
		if (o == null)
		{
			return null;
		}
		while (o.Parent != null && !(o.Parent is XmlSchema))
		{
			o = o.Parent;
		}
		if (ns == null || ns.Length == 0)
		{
			XmlSchemaObject xmlSchemaObject = o;
			while (xmlSchemaObject.Parent != null)
			{
				xmlSchemaObject = xmlSchemaObject.Parent;
			}
			if (xmlSchemaObject is XmlSchema)
			{
				ns = ((XmlSchema)xmlSchemaObject).TargetNamespace;
			}
		}
		string text = null;
		if (o is XmlSchemaNotation)
		{
			return System.SR.Format(System.SR.XmlSchemaNamedItem, ns, "notation", ((XmlSchemaNotation)o).Name, details);
		}
		if (o is XmlSchemaGroup)
		{
			return System.SR.Format(System.SR.XmlSchemaNamedItem, ns, "group", ((XmlSchemaGroup)o).Name, details);
		}
		if (o is XmlSchemaElement)
		{
			XmlSchemaElement xmlSchemaElement = (XmlSchemaElement)o;
			if (xmlSchemaElement.Name == null || xmlSchemaElement.Name.Length == 0)
			{
				XmlQualifiedName parentName = GetParentName(o);
				return System.SR.Format(System.SR.XmlSchemaElementReference, xmlSchemaElement.RefName.ToString(), parentName.Name, parentName.Namespace);
			}
			return System.SR.Format(System.SR.XmlSchemaNamedItem, ns, "element", xmlSchemaElement.Name, details);
		}
		if (o is XmlSchemaType)
		{
			return System.SR.Format(System.SR.XmlSchemaNamedItem, ns, (o.GetType() == typeof(XmlSchemaSimpleType)) ? "simpleType" : "complexType", ((XmlSchemaType)o).Name, null);
		}
		if (o is XmlSchemaAttributeGroup)
		{
			return System.SR.Format(System.SR.XmlSchemaNamedItem, ns, "attributeGroup", ((XmlSchemaAttributeGroup)o).Name, details);
		}
		if (o is XmlSchemaAttribute)
		{
			XmlSchemaAttribute xmlSchemaAttribute = (XmlSchemaAttribute)o;
			if (xmlSchemaAttribute.Name == null || xmlSchemaAttribute.Name.Length == 0)
			{
				XmlQualifiedName parentName2 = GetParentName(o);
				return System.SR.Format(System.SR.XmlSchemaAttributeReference, xmlSchemaAttribute.RefName.ToString(), parentName2.Name, parentName2.Namespace);
			}
			return System.SR.Format(System.SR.XmlSchemaNamedItem, ns, "attribute", xmlSchemaAttribute.Name, details);
		}
		if (o is XmlSchemaContent)
		{
			XmlQualifiedName parentName3 = GetParentName(o);
			return System.SR.Format(System.SR.XmlSchemaContentDef, parentName3.Name, parentName3.Namespace, null);
		}
		if (o is XmlSchemaExternal)
		{
			string p = ((o is XmlSchemaImport) ? "import" : ((o is XmlSchemaInclude) ? "include" : ((o is XmlSchemaRedefine) ? "redefine" : o.GetType().Name)));
			return System.SR.Format(System.SR.XmlSchemaItem, ns, p, details);
		}
		if (o is XmlSchema)
		{
			return System.SR.Format(System.SR.XmlSchema, ns, details);
		}
		return System.SR.Format(System.SR.XmlSchemaNamedItem, ns, o.GetType().Name, null, details);
	}

	[RequiresUnreferencedCode("Creates XmlSerializer")]
	private static string Dump(XmlSchemaObject o)
	{
		XmlWriterSettings xmlWriterSettings = new XmlWriterSettings();
		xmlWriterSettings.OmitXmlDeclaration = true;
		xmlWriterSettings.Indent = true;
		XmlSerializer xmlSerializer = new XmlSerializer(o.GetType());
		StringWriter stringWriter = new StringWriter(CultureInfo.InvariantCulture);
		XmlWriter xmlWriter = XmlWriter.Create(stringWriter, xmlWriterSettings);
		XmlSerializerNamespaces xmlSerializerNamespaces = new XmlSerializerNamespaces();
		xmlSerializerNamespaces.Add("xs", "http://www.w3.org/2001/XMLSchema");
		xmlSerializer.Serialize(xmlWriter, o, xmlSerializerNamespaces);
		return stringWriter.ToString();
	}

	[RequiresUnreferencedCode("calls Dump")]
	private static string MergeFailedMessage(XmlSchemaObject src, XmlSchemaObject dest, string ns)
	{
		string text = System.SR.Format(System.SR.XmlSerializableMergeItem, ns, GetSchemaItem(src, ns, null));
		text = text + "\r\n" + Dump(src);
		return text + "\r\n" + Dump(dest);
	}

	internal XmlSchemaObject Find(XmlSchemaObject o, IList originals)
	{
		string text = ItemName(o);
		if (text == null)
		{
			return null;
		}
		Type type = o.GetType();
		foreach (XmlSchema original in originals)
		{
			foreach (XmlSchemaObject item in original.Items)
			{
				if (item.GetType() == type && text == ItemName(item))
				{
					return item;
				}
			}
		}
		return null;
	}

	[RequiresUnreferencedCode("Members from serialized types may be trimmed if not referenced directly")]
	public void Compile(ValidationEventHandler? handler, bool fullCompile)
	{
		if (_isCompiled)
		{
			return;
		}
		foreach (XmlSchema value in delayedSchemas.Values)
		{
			Merge(value);
		}
		delayedSchemas.Clear();
		if (fullCompile)
		{
			_schemaSet = new XmlSchemaSet();
			_schemaSet.XmlResolver = null;
			_schemaSet.ValidationEventHandler += handler;
			foreach (XmlSchema value2 in References.Values)
			{
				_schemaSet.Add(value2);
			}
			int num = _schemaSet.Count;
			foreach (XmlSchema item in base.List)
			{
				if (!SchemaSet.Contains(item))
				{
					_schemaSet.Add(item);
					num++;
				}
			}
			if (!SchemaSet.Contains("http://www.w3.org/2001/XMLSchema"))
			{
				AddReference(XsdSchema);
				_schemaSet.Add(XsdSchema);
				num++;
			}
			if (!SchemaSet.Contains("http://www.w3.org/XML/1998/namespace"))
			{
				AddReference(XmlSchema);
				_schemaSet.Add(XmlSchema);
				num++;
			}
			_schemaSet.Compile();
			_schemaSet.ValidationEventHandler -= handler;
			_isCompiled = _schemaSet.IsCompiled && num == _schemaSet.Count;
			return;
		}
		try
		{
			XmlNameTable nameTable = new System.Xml.NameTable();
			Preprocessor preprocessor = new Preprocessor(nameTable, new SchemaNames(nameTable), null);
			preprocessor.XmlResolver = null;
			preprocessor.SchemaLocations = new Hashtable();
			preprocessor.ChameleonSchemas = new Hashtable();
			foreach (XmlSchema item2 in SchemaSet.Schemas())
			{
				preprocessor.Execute(item2, item2.TargetNamespace, loadExternals: true);
			}
		}
		catch (XmlSchemaException ex)
		{
			throw CreateValidationException(ex, ex.Message);
		}
	}

	internal static Exception CreateValidationException(XmlSchemaException exception, string message)
	{
		XmlSchemaObject xmlSchemaObject = exception.SourceSchemaObject;
		if (exception.LineNumber == 0 && exception.LinePosition == 0)
		{
			throw new InvalidOperationException(GetSchemaItem(xmlSchemaObject, null, message), exception);
		}
		string text = null;
		if (xmlSchemaObject != null)
		{
			while (xmlSchemaObject.Parent != null)
			{
				xmlSchemaObject = xmlSchemaObject.Parent;
			}
			if (xmlSchemaObject is XmlSchema)
			{
				text = ((XmlSchema)xmlSchemaObject).TargetNamespace;
			}
		}
		throw new InvalidOperationException(System.SR.Format(System.SR.XmlSchemaSyntaxErrorDetails, text, message, exception.LineNumber, exception.LinePosition), exception);
	}

	internal static void IgnoreCompileErrors(object sender, ValidationEventArgs args)
	{
	}

	private static XmlSchema CreateFakeXsdSchema(string ns, string name)
	{
		XmlSchema xmlSchema = new XmlSchema();
		xmlSchema.TargetNamespace = ns;
		XmlSchemaElement xmlSchemaElement = new XmlSchemaElement();
		xmlSchemaElement.Name = name;
		XmlSchemaComplexType schemaType = new XmlSchemaComplexType();
		xmlSchemaElement.SchemaType = schemaType;
		xmlSchema.Items.Add(xmlSchemaElement);
		return xmlSchema;
	}

	[RequiresUnreferencedCode("calls GenerateSchemaGraph")]
	internal void SetCache(SchemaObjectCache cache, bool shareTypes)
	{
		_shareTypes = shareTypes;
		_cache = cache;
		if (shareTypes)
		{
			cache.GenerateSchemaGraph(this);
		}
	}

	internal bool IsReference(XmlSchemaObject type)
	{
		XmlSchemaObject xmlSchemaObject = type;
		while (xmlSchemaObject.Parent != null)
		{
			xmlSchemaObject = xmlSchemaObject.Parent;
		}
		return References.Contains(xmlSchemaObject);
	}
}
