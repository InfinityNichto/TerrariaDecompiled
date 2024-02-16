using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data.Common;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Xml;
using System.Xml.Schema;

namespace System.Data;

internal sealed class XSDSchema : XMLSchema
{
	private sealed class NameType : IComparable
	{
		public readonly string name;

		[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicFields | DynamicallyAccessedMemberTypes.PublicProperties)]
		public readonly Type type;

		public NameType(string n, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicFields | DynamicallyAccessedMemberTypes.PublicProperties)] Type t)
		{
			name = n;
			type = t;
		}

		public int CompareTo(object obj)
		{
			return string.Compare(name, (string)obj, StringComparison.Ordinal);
		}
	}

	private XmlSchemaSet _schemaSet;

	private XmlSchemaElement _dsElement;

	private DataSet _ds;

	private string _schemaName;

	private ArrayList _columnExpressions;

	private Hashtable _constraintNodes;

	private ArrayList _refTables;

	private ArrayList _complexTypes;

	private XmlSchemaObjectCollection _annotations;

	private XmlSchemaObjectCollection _elements;

	private Hashtable _attributes;

	private Hashtable _elementsTable;

	private Hashtable _attributeGroups;

	private Hashtable _schemaTypes;

	private Hashtable _expressions;

	private Dictionary<DataTable, List<DataTable>> _tableDictionary;

	private Hashtable _udSimpleTypes;

	private Hashtable _existingSimpleTypeMap;

	private bool _fromInference;

	private static readonly NameType[] s_mapNameTypeXsd = new NameType[44]
	{
		new NameType("ENTITIES", typeof(string)),
		new NameType("ENTITY", typeof(string)),
		new NameType("ID", typeof(string)),
		new NameType("IDREF", typeof(string)),
		new NameType("IDREFS", typeof(string)),
		new NameType("NCName", typeof(string)),
		new NameType("NMTOKEN", typeof(string)),
		new NameType("NMTOKENS", typeof(string)),
		new NameType("NOTATION", typeof(string)),
		new NameType("Name", typeof(string)),
		new NameType("QName", typeof(string)),
		new NameType("anyType", typeof(object)),
		new NameType("anyURI", typeof(Uri)),
		new NameType("base64Binary", typeof(byte[])),
		new NameType("boolean", typeof(bool)),
		new NameType("byte", typeof(sbyte)),
		new NameType("date", typeof(DateTime)),
		new NameType("dateTime", typeof(DateTime)),
		new NameType("decimal", typeof(decimal)),
		new NameType("double", typeof(double)),
		new NameType("duration", typeof(TimeSpan)),
		new NameType("float", typeof(float)),
		new NameType("gDay", typeof(DateTime)),
		new NameType("gMonth", typeof(DateTime)),
		new NameType("gMonthDay", typeof(DateTime)),
		new NameType("gYear", typeof(DateTime)),
		new NameType("gYearMonth", typeof(DateTime)),
		new NameType("hexBinary", typeof(byte[])),
		new NameType("int", typeof(int)),
		new NameType("integer", typeof(long)),
		new NameType("language", typeof(string)),
		new NameType("long", typeof(long)),
		new NameType("negativeInteger", typeof(long)),
		new NameType("nonNegativeInteger", typeof(ulong)),
		new NameType("nonPositiveInteger", typeof(long)),
		new NameType("normalizedString", typeof(string)),
		new NameType("positiveInteger", typeof(ulong)),
		new NameType("short", typeof(short)),
		new NameType("string", typeof(string)),
		new NameType("time", typeof(DateTime)),
		new NameType("unsignedByte", typeof(byte)),
		new NameType("unsignedInt", typeof(uint)),
		new NameType("unsignedLong", typeof(ulong)),
		new NameType("unsignedShort", typeof(ushort))
	};

	internal bool FromInference
	{
		get
		{
			return _fromInference;
		}
		set
		{
			_fromInference = value;
		}
	}

	private void CollectElementsAnnotations(XmlSchema schema)
	{
		ArrayList arrayList = new ArrayList();
		CollectElementsAnnotations(schema, arrayList);
		arrayList.Clear();
	}

	private void CollectElementsAnnotations(XmlSchema schema, ArrayList schemaList)
	{
		if (schemaList.Contains(schema))
		{
			return;
		}
		schemaList.Add(schema);
		foreach (XmlSchemaObject item in schema.Items)
		{
			if (item is XmlSchemaAnnotation)
			{
				_annotations.Add((XmlSchemaAnnotation)item);
			}
			if (item is XmlSchemaElement)
			{
				XmlSchemaElement xmlSchemaElement = (XmlSchemaElement)item;
				_elements.Add(xmlSchemaElement);
				_elementsTable[xmlSchemaElement.QualifiedName] = xmlSchemaElement;
			}
			if (item is XmlSchemaAttribute)
			{
				XmlSchemaAttribute xmlSchemaAttribute = (XmlSchemaAttribute)item;
				_attributes[xmlSchemaAttribute.QualifiedName] = xmlSchemaAttribute;
			}
			if (item is XmlSchemaAttributeGroup)
			{
				XmlSchemaAttributeGroup xmlSchemaAttributeGroup = (XmlSchemaAttributeGroup)item;
				_attributeGroups[xmlSchemaAttributeGroup.QualifiedName] = xmlSchemaAttributeGroup;
			}
			if (!(item is XmlSchemaType))
			{
				continue;
			}
			string text = null;
			if (item is XmlSchemaSimpleType)
			{
				text = GetMsdataAttribute((XmlSchemaType)item, "targetNamespace");
			}
			XmlSchemaType xmlSchemaType = (XmlSchemaType)item;
			_schemaTypes[xmlSchemaType.QualifiedName] = xmlSchemaType;
			if (!(item is XmlSchemaSimpleType xmlSchemaSimpleType))
			{
				continue;
			}
			if (_udSimpleTypes == null)
			{
				_udSimpleTypes = new Hashtable();
			}
			_udSimpleTypes[xmlSchemaType.QualifiedName.ToString()] = xmlSchemaSimpleType;
			SimpleType simpleType = ((DataColumn)_existingSimpleTypeMap[xmlSchemaType.QualifiedName.ToString()])?.SimpleType;
			if (simpleType != null)
			{
				SimpleType simpleType2 = new SimpleType(xmlSchemaSimpleType);
				string text2 = simpleType.HasConflictingDefinition(simpleType2);
				if (text2.Length != 0)
				{
					throw ExceptionBuilder.InvalidDuplicateNamedSimpleTypeDelaration(simpleType2.SimpleTypeQualifiedName, text2);
				}
			}
		}
		foreach (XmlSchemaExternal include in schema.Includes)
		{
			if (!(include is XmlSchemaImport) && include.Schema != null)
			{
				CollectElementsAnnotations(include.Schema, schemaList);
			}
		}
	}

	internal static string QualifiedName(string name)
	{
		if (!name.Contains(':'))
		{
			return "xs:" + name;
		}
		return name;
	}

	[RequiresUnreferencedCode("Members from serialized types may be trimmed if not referenced directly.")]
	internal static void SetProperties(object instance, XmlAttribute[] attrs)
	{
		if (attrs == null)
		{
			return;
		}
		for (int i = 0; i < attrs.Length; i++)
		{
			if (!(attrs[i].NamespaceURI == "urn:schemas-microsoft-com:xml-msdata"))
			{
				continue;
			}
			string localName = attrs[i].LocalName;
			string value = attrs[i].Value;
			switch (localName)
			{
			case "Expression":
				if (instance is DataColumn)
				{
					continue;
				}
				break;
			case "DefaultValue":
			case "Ordinal":
			case "Locale":
			case "RemotingFormat":
				continue;
			}
			if (localName == "DataType")
			{
				if (instance is DataColumn dataColumn)
				{
					dataColumn.DataType = DataStorage.GetType(value);
				}
				continue;
			}
			PropertyDescriptor propertyDescriptor = TypeDescriptor.GetProperties(instance)[localName];
			if (propertyDescriptor == null)
			{
				continue;
			}
			Type propertyType = propertyDescriptor.PropertyType;
			TypeConverter converter = XMLSchema.GetConverter(propertyType);
			object value2;
			if (converter.CanConvertFrom(typeof(string)))
			{
				value2 = converter.ConvertFromInvariantString(value);
			}
			else if (propertyType == typeof(Type))
			{
				value2 = Type.GetType(value);
			}
			else
			{
				if (!(propertyType == typeof(CultureInfo)))
				{
					throw ExceptionBuilder.CannotConvert(value, propertyType.FullName);
				}
				value2 = new CultureInfo(value);
			}
			propertyDescriptor.SetValue(instance, value2);
		}
	}

	[RequiresUnreferencedCode("Members from serialized types may be trimmed if not referenced directly.")]
	private static void SetExtProperties(object instance, XmlAttribute[] attrs)
	{
		PropertyCollection propertyCollection = null;
		if (attrs == null)
		{
			return;
		}
		for (int i = 0; i < attrs.Length; i++)
		{
			if (!(attrs[i].NamespaceURI == "urn:schemas-microsoft-com:xml-msprop"))
			{
				continue;
			}
			if (propertyCollection == null)
			{
				object value = TypeDescriptor.GetProperties(instance)["ExtendedProperties"].GetValue(instance);
				propertyCollection = (PropertyCollection)value;
			}
			string text = XmlConvert.DecodeName(attrs[i].LocalName);
			if (instance is ForeignKeyConstraint)
			{
				if (!text.StartsWith("fk_", StringComparison.Ordinal))
				{
					continue;
				}
				text = text.Substring(3);
			}
			if (instance is DataRelation && text.StartsWith("rel_", StringComparison.Ordinal))
			{
				text = text.Substring(4);
			}
			else if (instance is DataRelation && text.StartsWith("fk_", StringComparison.Ordinal))
			{
				continue;
			}
			propertyCollection.Add(text, attrs[i].Value);
		}
	}

	private void HandleColumnExpression(object instance, XmlAttribute[] attrs)
	{
		if (attrs == null || !(instance is DataColumn dataColumn))
		{
			return;
		}
		for (int i = 0; i < attrs.Length; i++)
		{
			if (attrs[i].NamespaceURI == "urn:schemas-microsoft-com:xml-msdata" && attrs[i].LocalName == "Expression")
			{
				if (_expressions == null)
				{
					_expressions = new Hashtable();
				}
				_expressions[dataColumn] = attrs[i].Value;
				_columnExpressions.Add(dataColumn);
				break;
			}
		}
	}

	internal static string GetMsdataAttribute(XmlSchemaAnnotated node, string ln)
	{
		XmlAttribute[] unhandledAttributes = node.UnhandledAttributes;
		if (unhandledAttributes != null)
		{
			for (int i = 0; i < unhandledAttributes.Length; i++)
			{
				if (unhandledAttributes[i].LocalName == ln && unhandledAttributes[i].NamespaceURI == "urn:schemas-microsoft-com:xml-msdata")
				{
					return unhandledAttributes[i].Value;
				}
			}
		}
		return null;
	}

	[RequiresUnreferencedCode("Members from serialized types may be trimmed if not referenced directly.")]
	private static void SetExtProperties(object instance, XmlAttributeCollection attrs)
	{
		PropertyCollection propertyCollection = null;
		for (int i = 0; i < attrs.Count; i++)
		{
			if (attrs[i].NamespaceURI == "urn:schemas-microsoft-com:xml-msprop")
			{
				if (propertyCollection == null)
				{
					object value = TypeDescriptor.GetProperties(instance)["ExtendedProperties"].GetValue(instance);
					propertyCollection = (PropertyCollection)value;
				}
				string key = XmlConvert.DecodeName(attrs[i].LocalName);
				propertyCollection.Add(key, attrs[i].Value);
			}
		}
	}

	[RequiresUnreferencedCode("Members from serialized types may be trimmed if not referenced directly.")]
	internal void HandleRefTableProperties(ArrayList RefTables, XmlSchemaElement element)
	{
		string instanceName = GetInstanceName(element);
		DataTable table = _ds.Tables.GetTable(XmlConvert.DecodeName(instanceName), element.QualifiedName.Namespace);
		SetProperties(table, element.UnhandledAttributes);
		SetExtProperties(table, element.UnhandledAttributes);
	}

	[RequiresUnreferencedCode("Members from serialized types may be trimmed if not referenced directly.")]
	internal void HandleRelation(XmlElement node, bool fNested)
	{
		bool createConstraints = false;
		DataRelationCollection relations = _ds.Relations;
		string text = XmlConvert.DecodeName(node.GetAttribute("name"));
		for (int i = 0; i < relations.Count; i++)
		{
			if (string.Equals(relations[i].RelationName, text, StringComparison.Ordinal))
			{
				return;
			}
		}
		string attribute = node.GetAttribute("parent", "urn:schemas-microsoft-com:xml-msdata");
		if (attribute == null || attribute.Length == 0)
		{
			throw ExceptionBuilder.RelationParentNameMissing(text);
		}
		attribute = XmlConvert.DecodeName(attribute);
		string attribute2 = node.GetAttribute("child", "urn:schemas-microsoft-com:xml-msdata");
		if (attribute2 == null || attribute2.Length == 0)
		{
			throw ExceptionBuilder.RelationChildNameMissing(text);
		}
		attribute2 = XmlConvert.DecodeName(attribute2);
		string attribute3 = node.GetAttribute("parentkey", "urn:schemas-microsoft-com:xml-msdata");
		if (attribute3 == null || attribute3.Length == 0)
		{
			throw ExceptionBuilder.RelationTableKeyMissing(text);
		}
		string[] array = attribute3.TrimEnd(null).Split(' ', '+');
		attribute3 = node.GetAttribute("childkey", "urn:schemas-microsoft-com:xml-msdata");
		if (attribute3 == null || attribute3.Length == 0)
		{
			throw ExceptionBuilder.RelationChildKeyMissing(text);
		}
		string[] array2 = attribute3.TrimEnd(null).Split(' ', '+');
		int num = array.Length;
		if (num != array2.Length)
		{
			throw ExceptionBuilder.MismatchKeyLength();
		}
		DataColumn[] array3 = new DataColumn[num];
		DataColumn[] array4 = new DataColumn[num];
		string attribute4 = node.GetAttribute("ParentTableNamespace", "urn:schemas-microsoft-com:xml-msdata");
		string attribute5 = node.GetAttribute("ChildTableNamespace", "urn:schemas-microsoft-com:xml-msdata");
		DataTable tableSmart = _ds.Tables.GetTableSmart(attribute, attribute4);
		if (tableSmart == null)
		{
			throw ExceptionBuilder.ElementTypeNotFound(attribute);
		}
		DataTable tableSmart2 = _ds.Tables.GetTableSmart(attribute2, attribute5);
		if (tableSmart2 == null)
		{
			throw ExceptionBuilder.ElementTypeNotFound(attribute2);
		}
		for (int j = 0; j < num; j++)
		{
			array3[j] = tableSmart.Columns[XmlConvert.DecodeName(array[j])];
			if (array3[j] == null)
			{
				throw ExceptionBuilder.ElementTypeNotFound(array[j]);
			}
			array4[j] = tableSmart2.Columns[XmlConvert.DecodeName(array2[j])];
			if (array4[j] == null)
			{
				throw ExceptionBuilder.ElementTypeNotFound(array2[j]);
			}
		}
		DataRelation dataRelation = new DataRelation(text, array3, array4, createConstraints);
		dataRelation.Nested = fNested;
		SetExtProperties(dataRelation, node.Attributes);
		_ds.Relations.Add(dataRelation);
		if (FromInference && dataRelation.Nested)
		{
			_tableDictionary[dataRelation.ParentTable].Add(dataRelation.ChildTable);
		}
	}

	private bool HasAttributes(XmlSchemaObjectCollection attributes)
	{
		foreach (XmlSchemaObject attribute in attributes)
		{
			if (attribute is XmlSchemaAttribute)
			{
				return true;
			}
			if (attribute is XmlSchemaAttributeGroup)
			{
				return true;
			}
			if (attribute is XmlSchemaAttributeGroupRef)
			{
				return true;
			}
		}
		return false;
	}

	private bool IsDatasetParticle(XmlSchemaParticle pt)
	{
		XmlSchemaObjectCollection particleItems = GetParticleItems(pt);
		if (particleItems == null)
		{
			return false;
		}
		bool flag = FromInference && pt is XmlSchemaChoice;
		foreach (XmlSchemaAnnotated item in particleItems)
		{
			if (item is XmlSchemaElement)
			{
				if (flag && pt.MaxOccurs > 1m && ((XmlSchemaElement)item).SchemaType is XmlSchemaComplexType)
				{
					((XmlSchemaElement)item).MaxOccurs = pt.MaxOccurs;
				}
				if ((((XmlSchemaElement)item).RefName.Name.Length == 0 || (FromInference && (!(((XmlSchemaElement)item).MaxOccurs != 1m) || ((XmlSchemaElement)item).SchemaType is XmlSchemaComplexType))) && !IsTable((XmlSchemaElement)item))
				{
					return false;
				}
			}
			else if (item is XmlSchemaParticle && !IsDatasetParticle((XmlSchemaParticle)item))
			{
				return false;
			}
		}
		return true;
	}

	private int DatasetElementCount(XmlSchemaObjectCollection elements)
	{
		int num = 0;
		foreach (XmlSchemaElement element in elements)
		{
			if (GetBooleanAttribute(element, "IsDataSet", defVal: false))
			{
				num++;
			}
		}
		return num;
	}

	private XmlSchemaElement FindDatasetElement(XmlSchemaObjectCollection elements)
	{
		foreach (XmlSchemaElement element in elements)
		{
			if (GetBooleanAttribute(element, "IsDataSet", defVal: false))
			{
				return element;
			}
		}
		if (elements.Count == 1 || (FromInference && elements.Count > 0))
		{
			XmlSchemaElement xmlSchemaElement2 = (XmlSchemaElement)elements[0];
			if (!GetBooleanAttribute(xmlSchemaElement2, "IsDataSet", defVal: true))
			{
				return null;
			}
			XmlSchemaComplexType xmlSchemaComplexType = xmlSchemaElement2.SchemaType as XmlSchemaComplexType;
			if (xmlSchemaComplexType == null)
			{
				return null;
			}
			while (xmlSchemaComplexType != null)
			{
				if (HasAttributes(xmlSchemaComplexType.Attributes))
				{
					return null;
				}
				if (xmlSchemaComplexType.ContentModel is XmlSchemaSimpleContent)
				{
					XmlSchemaAnnotated content = ((XmlSchemaSimpleContent)xmlSchemaComplexType.ContentModel).Content;
					if (content is XmlSchemaSimpleContentExtension)
					{
						XmlSchemaSimpleContentExtension xmlSchemaSimpleContentExtension = (XmlSchemaSimpleContentExtension)content;
						if (HasAttributes(xmlSchemaSimpleContentExtension.Attributes))
						{
							return null;
						}
					}
					else
					{
						XmlSchemaSimpleContentRestriction xmlSchemaSimpleContentRestriction = (XmlSchemaSimpleContentRestriction)content;
						if (HasAttributes(xmlSchemaSimpleContentRestriction.Attributes))
						{
							return null;
						}
					}
				}
				XmlSchemaParticle particle = GetParticle(xmlSchemaComplexType);
				if (particle != null && !IsDatasetParticle(particle))
				{
					return null;
				}
				if (!(xmlSchemaComplexType.BaseXmlSchemaType is XmlSchemaComplexType))
				{
					break;
				}
				xmlSchemaComplexType = (XmlSchemaComplexType)xmlSchemaComplexType.BaseXmlSchemaType;
			}
			return xmlSchemaElement2;
		}
		return null;
	}

	[RequiresUnreferencedCode("Members from serialized types may be trimmed if not referenced directly.")]
	public void LoadSchema(XmlSchemaSet schemaSet, DataTable dt)
	{
		if (dt.DataSet != null)
		{
			LoadSchema(schemaSet, dt.DataSet);
		}
	}

	[RequiresUnreferencedCode("Members from serialized types may be trimmed if not referenced directly.")]
	public void LoadSchema(XmlSchemaSet schemaSet, DataSet ds)
	{
		_constraintNodes = new Hashtable();
		_refTables = new ArrayList();
		_columnExpressions = new ArrayList();
		_complexTypes = new ArrayList();
		bool flag = false;
		bool isNewDataSet = ds.Tables.Count == 0;
		if (schemaSet == null)
		{
			return;
		}
		_schemaSet = schemaSet;
		_ds = ds;
		ds._fIsSchemaLoading = true;
		IEnumerator enumerator = schemaSet.Schemas().GetEnumerator();
		try
		{
			if (enumerator.MoveNext())
			{
				XmlSchema xmlSchema = (XmlSchema)enumerator.Current;
				_schemaName = xmlSchema.Id;
				if (_schemaName == null || _schemaName.Length == 0)
				{
					_schemaName = "NewDataSet";
				}
				ds.DataSetName = XmlConvert.DecodeName(_schemaName);
				string targetNamespace = xmlSchema.TargetNamespace;
				if (ds._namespaceURI == null || ds._namespaceURI.Length == 0)
				{
					ds._namespaceURI = ((targetNamespace == null) ? string.Empty : targetNamespace);
				}
			}
		}
		finally
		{
			IDisposable disposable = enumerator as IDisposable;
			if (disposable != null)
			{
				disposable.Dispose();
			}
		}
		_annotations = new XmlSchemaObjectCollection();
		_elements = new XmlSchemaObjectCollection();
		_elementsTable = new Hashtable();
		_attributes = new Hashtable();
		_attributeGroups = new Hashtable();
		_schemaTypes = new Hashtable();
		_tableDictionary = new Dictionary<DataTable, List<DataTable>>();
		_existingSimpleTypeMap = new Hashtable();
		foreach (DataTable table in ds.Tables)
		{
			foreach (DataColumn column in table.Columns)
			{
				if (column.SimpleType != null && column.SimpleType.Name != null && column.SimpleType.Name.Length != 0)
				{
					_existingSimpleTypeMap[column.SimpleType.SimpleTypeQualifiedName] = column;
				}
			}
		}
		foreach (XmlSchema item in schemaSet.Schemas())
		{
			CollectElementsAnnotations(item);
		}
		_dsElement = FindDatasetElement(_elements);
		if (_dsElement != null)
		{
			string stringAttribute = GetStringAttribute(_dsElement, "MainDataTable", "");
			if (stringAttribute != null)
			{
				ds.MainTableName = XmlConvert.DecodeName(stringAttribute);
			}
		}
		else
		{
			if (FromInference)
			{
				ds._fTopLevelTable = true;
			}
			flag = true;
		}
		List<XmlQualifiedName> list = new List<XmlQualifiedName>();
		if (ds != null && ds._useDataSetSchemaOnly)
		{
			int num = DatasetElementCount(_elements);
			if (num == 0)
			{
				throw ExceptionBuilder.IsDataSetAttributeMissingInSchema();
			}
			if (num > 1)
			{
				throw ExceptionBuilder.TooManyIsDataSetAttributesInSchema();
			}
			XmlSchemaComplexType xmlSchemaComplexType = (XmlSchemaComplexType)FindTypeNode(_dsElement);
			if (xmlSchemaComplexType.Particle != null)
			{
				XmlSchemaObjectCollection particleItems = GetParticleItems(xmlSchemaComplexType.Particle);
				if (particleItems != null)
				{
					foreach (XmlSchemaAnnotated item2 in particleItems)
					{
						if (item2 is XmlSchemaElement xmlSchemaElement && xmlSchemaElement.RefName.Name.Length != 0)
						{
							list.Add(xmlSchemaElement.QualifiedName);
						}
					}
				}
			}
		}
		foreach (XmlSchemaElement element in _elements)
		{
			if (element != _dsElement && (ds == null || !ds._useDataSetSchemaOnly || _dsElement == null || _dsElement.Parent == element.Parent || list.Contains(element.QualifiedName)))
			{
				string instanceName = GetInstanceName(element);
				if (_refTables.Contains(element.QualifiedName.Namespace + ":" + instanceName))
				{
					HandleRefTableProperties(_refTables, element);
				}
				else
				{
					HandleTable(element);
				}
			}
		}
		if (_dsElement != null)
		{
			HandleDataSet(_dsElement, isNewDataSet);
		}
		foreach (XmlSchemaAnnotation annotation in _annotations)
		{
			HandleRelations(annotation, fNested: false);
		}
		for (int i = 0; i < _columnExpressions.Count; i++)
		{
			DataColumn dataColumn2 = (DataColumn)_columnExpressions[i];
			dataColumn2.Expression = (string)_expressions[dataColumn2];
		}
		foreach (DataTable table2 in ds.Tables)
		{
			if (table2.NestedParentRelations.Length != 0 || !(table2.Namespace == ds.Namespace))
			{
				continue;
			}
			DataRelationCollection childRelations = table2.ChildRelations;
			for (int j = 0; j < childRelations.Count; j++)
			{
				if (childRelations[j].Nested && table2.Namespace == childRelations[j].ChildTable.Namespace)
				{
					childRelations[j].ChildTable._tableNamespace = null;
				}
			}
			table2._tableNamespace = null;
		}
		DataTable dataTable3 = ds.Tables[ds.DataSetName, ds.Namespace];
		if (dataTable3 != null)
		{
			dataTable3._fNestedInDataset = true;
		}
		if (FromInference && ds.Tables.Count == 0 && string.Equals(ds.DataSetName, "NewDataSet", StringComparison.Ordinal))
		{
			ds.DataSetName = XmlConvert.DecodeName(((XmlSchemaElement)_elements[0]).Name);
		}
		ds._fIsSchemaLoading = false;
		if (!flag)
		{
			return;
		}
		if (ds.Tables.Count > 0)
		{
			ds.Namespace = ds.Tables[0].Namespace;
			ds.Prefix = ds.Tables[0].Prefix;
			return;
		}
		foreach (XmlSchema item3 in schemaSet.Schemas())
		{
			ds.Namespace = item3.TargetNamespace;
		}
	}

	[RequiresUnreferencedCode("Members from serialized types may be trimmed if not referenced directly.")]
	private void HandleRelations(XmlSchemaAnnotation ann, bool fNested)
	{
		foreach (XmlSchemaObject item in ann.Items)
		{
			if (!(item is XmlSchemaAppInfo))
			{
				continue;
			}
			XmlNode[] markup = ((XmlSchemaAppInfo)item).Markup;
			for (int i = 0; i < markup.Length; i++)
			{
				if (XMLSchema.FEqualIdentity(markup[i], "Relationship", "urn:schemas-microsoft-com:xml-msdata"))
				{
					HandleRelation((XmlElement)markup[i], fNested);
				}
			}
		}
	}

	internal XmlSchemaObjectCollection GetParticleItems(XmlSchemaParticle pt)
	{
		if (pt is XmlSchemaSequence)
		{
			return ((XmlSchemaSequence)pt).Items;
		}
		if (pt is XmlSchemaAll)
		{
			return ((XmlSchemaAll)pt).Items;
		}
		if (pt is XmlSchemaChoice)
		{
			return ((XmlSchemaChoice)pt).Items;
		}
		if (pt is XmlSchemaAny)
		{
			return null;
		}
		if (pt is XmlSchemaElement)
		{
			XmlSchemaObjectCollection xmlSchemaObjectCollection = new XmlSchemaObjectCollection();
			xmlSchemaObjectCollection.Add(pt);
			return xmlSchemaObjectCollection;
		}
		if (pt is XmlSchemaGroupRef)
		{
			return GetParticleItems(((XmlSchemaGroupRef)pt).Particle);
		}
		return null;
	}

	[RequiresUnreferencedCode("Members from serialized types may be trimmed if not referenced directly.")]
	internal void HandleParticle(XmlSchemaParticle pt, DataTable table, ArrayList tableChildren, bool isBase)
	{
		XmlSchemaObjectCollection particleItems = GetParticleItems(pt);
		if (particleItems == null)
		{
			return;
		}
		foreach (XmlSchemaAnnotated item in particleItems)
		{
			if (item is XmlSchemaElement xmlSchemaElement)
			{
				if (FromInference && pt is XmlSchemaChoice && pt.MaxOccurs > 1m && xmlSchemaElement.SchemaType is XmlSchemaComplexType)
				{
					xmlSchemaElement.MaxOccurs = pt.MaxOccurs;
				}
				DataTable dataTable = null;
				if ((xmlSchemaElement.Name == null && xmlSchemaElement.RefName.Name == table.EncodedTableName && xmlSchemaElement.RefName.Namespace == table.Namespace) || (IsTable(xmlSchemaElement) && xmlSchemaElement.Name == table.TableName))
				{
					dataTable = ((!FromInference) ? table : HandleTable(xmlSchemaElement));
				}
				else
				{
					dataTable = HandleTable(xmlSchemaElement);
					if (dataTable == null && FromInference && xmlSchemaElement.Name == table.TableName)
					{
						dataTable = table;
					}
				}
				if (dataTable == null)
				{
					if (!FromInference || xmlSchemaElement.Name != table.TableName)
					{
						HandleElementColumn(xmlSchemaElement, table, isBase);
					}
					continue;
				}
				DataRelation dataRelation = null;
				if (xmlSchemaElement.Annotation != null)
				{
					HandleRelations(xmlSchemaElement.Annotation, fNested: true);
				}
				DataRelationCollection childRelations = table.ChildRelations;
				for (int i = 0; i < childRelations.Count; i++)
				{
					if (childRelations[i].Nested && dataTable == childRelations[i].ChildTable)
					{
						dataRelation = childRelations[i];
					}
				}
				if (dataRelation != null)
				{
					continue;
				}
				tableChildren.Add(dataTable);
				if (!FromInference || table.UKColumnPositionForInference != -1)
				{
					continue;
				}
				int num = -1;
				foreach (DataColumn column in table.Columns)
				{
					if (column.ColumnMapping == MappingType.Element)
					{
						num++;
					}
				}
				table.UKColumnPositionForInference = num + 1;
			}
			else
			{
				HandleParticle((XmlSchemaParticle)item, table, tableChildren, isBase);
			}
		}
	}

	[RequiresUnreferencedCode("Members from serialized types may be trimmed if not referenced directly.")]
	internal void HandleAttributes(XmlSchemaObjectCollection attributes, DataTable table, bool isBase)
	{
		foreach (XmlSchemaObject attribute in attributes)
		{
			if (attribute is XmlSchemaAttribute)
			{
				HandleAttributeColumn((XmlSchemaAttribute)attribute, table, isBase);
				continue;
			}
			XmlSchemaAttributeGroupRef xmlSchemaAttributeGroupRef = attribute as XmlSchemaAttributeGroupRef;
			if (_attributeGroups[xmlSchemaAttributeGroupRef.RefName] is XmlSchemaAttributeGroup attributeGroup)
			{
				HandleAttributeGroup(attributeGroup, table, isBase);
			}
		}
	}

	[RequiresUnreferencedCode("Members from serialized types may be trimmed if not referenced directly.")]
	private void HandleAttributeGroup(XmlSchemaAttributeGroup attributeGroup, DataTable table, bool isBase)
	{
		foreach (XmlSchemaObject attribute in attributeGroup.Attributes)
		{
			if (attribute is XmlSchemaAttribute)
			{
				HandleAttributeColumn((XmlSchemaAttribute)attribute, table, isBase);
				continue;
			}
			XmlSchemaAttributeGroupRef xmlSchemaAttributeGroupRef = (XmlSchemaAttributeGroupRef)attribute;
			XmlSchemaAttributeGroup xmlSchemaAttributeGroup = ((attributeGroup.RedefinedAttributeGroup == null || !(xmlSchemaAttributeGroupRef.RefName == new XmlQualifiedName(attributeGroup.Name, xmlSchemaAttributeGroupRef.RefName.Namespace))) ? ((XmlSchemaAttributeGroup)_attributeGroups[xmlSchemaAttributeGroupRef.RefName]) : attributeGroup.RedefinedAttributeGroup);
			if (xmlSchemaAttributeGroup != null)
			{
				HandleAttributeGroup(xmlSchemaAttributeGroup, table, isBase);
			}
		}
	}

	[RequiresUnreferencedCode("Members from serialized types may be trimmed if not referenced directly.")]
	internal void HandleComplexType(XmlSchemaComplexType ct, DataTable table, ArrayList tableChildren, bool isNillable)
	{
		if (_complexTypes.Contains(ct))
		{
			throw ExceptionBuilder.CircularComplexType(ct.Name);
		}
		bool isBase = false;
		_complexTypes.Add(ct);
		if (ct.ContentModel != null)
		{
			if (ct.ContentModel is XmlSchemaComplexContent)
			{
				XmlSchemaAnnotated content = ((XmlSchemaComplexContent)ct.ContentModel).Content;
				if (content is XmlSchemaComplexContentExtension)
				{
					XmlSchemaComplexContentExtension xmlSchemaComplexContentExtension = (XmlSchemaComplexContentExtension)content;
					if (!(ct.BaseXmlSchemaType is XmlSchemaComplexType) || !FromInference)
					{
						HandleAttributes(xmlSchemaComplexContentExtension.Attributes, table, isBase);
					}
					if (ct.BaseXmlSchemaType is XmlSchemaComplexType)
					{
						HandleComplexType((XmlSchemaComplexType)ct.BaseXmlSchemaType, table, tableChildren, isNillable);
					}
					else if (xmlSchemaComplexContentExtension.BaseTypeName.Namespace != "http://www.w3.org/2001/XMLSchema")
					{
						HandleSimpleContentColumn(xmlSchemaComplexContentExtension.BaseTypeName.ToString(), table, isBase, ct.ContentModel.UnhandledAttributes, isNillable);
					}
					else
					{
						HandleSimpleContentColumn(xmlSchemaComplexContentExtension.BaseTypeName.Name, table, isBase, ct.ContentModel.UnhandledAttributes, isNillable);
					}
					if (xmlSchemaComplexContentExtension.Particle != null)
					{
						HandleParticle(xmlSchemaComplexContentExtension.Particle, table, tableChildren, isBase);
					}
					if (ct.BaseXmlSchemaType is XmlSchemaComplexType && FromInference)
					{
						HandleAttributes(xmlSchemaComplexContentExtension.Attributes, table, isBase);
					}
				}
				else
				{
					XmlSchemaComplexContentRestriction xmlSchemaComplexContentRestriction = (XmlSchemaComplexContentRestriction)content;
					if (!FromInference)
					{
						HandleAttributes(xmlSchemaComplexContentRestriction.Attributes, table, isBase);
					}
					if (xmlSchemaComplexContentRestriction.Particle != null)
					{
						HandleParticle(xmlSchemaComplexContentRestriction.Particle, table, tableChildren, isBase);
					}
					if (FromInference)
					{
						HandleAttributes(xmlSchemaComplexContentRestriction.Attributes, table, isBase);
					}
				}
			}
			else
			{
				XmlSchemaAnnotated content2 = ((XmlSchemaSimpleContent)ct.ContentModel).Content;
				if (content2 is XmlSchemaSimpleContentExtension)
				{
					XmlSchemaSimpleContentExtension xmlSchemaSimpleContentExtension = (XmlSchemaSimpleContentExtension)content2;
					HandleAttributes(xmlSchemaSimpleContentExtension.Attributes, table, isBase);
					if (ct.BaseXmlSchemaType is XmlSchemaComplexType)
					{
						HandleComplexType((XmlSchemaComplexType)ct.BaseXmlSchemaType, table, tableChildren, isNillable);
					}
					else
					{
						HandleSimpleTypeSimpleContentColumn((XmlSchemaSimpleType)ct.BaseXmlSchemaType, xmlSchemaSimpleContentExtension.BaseTypeName.Name, table, isBase, ct.ContentModel.UnhandledAttributes, isNillable);
					}
				}
				else
				{
					XmlSchemaSimpleContentRestriction xmlSchemaSimpleContentRestriction = (XmlSchemaSimpleContentRestriction)content2;
					HandleAttributes(xmlSchemaSimpleContentRestriction.Attributes, table, isBase);
				}
			}
		}
		else
		{
			isBase = true;
			if (!FromInference)
			{
				HandleAttributes(ct.Attributes, table, isBase);
			}
			if (ct.Particle != null)
			{
				HandleParticle(ct.Particle, table, tableChildren, isBase);
			}
			if (FromInference)
			{
				HandleAttributes(ct.Attributes, table, isBase);
				if (isNillable)
				{
					HandleSimpleContentColumn("string", table, isBase, null, isNillable);
				}
			}
		}
		_complexTypes.Remove(ct);
	}

	internal XmlSchemaParticle GetParticle(XmlSchemaComplexType ct)
	{
		if (ct.ContentModel != null)
		{
			if (ct.ContentModel is XmlSchemaComplexContent)
			{
				XmlSchemaAnnotated content = ((XmlSchemaComplexContent)ct.ContentModel).Content;
				if (content is XmlSchemaComplexContentExtension)
				{
					return ((XmlSchemaComplexContentExtension)content).Particle;
				}
				return ((XmlSchemaComplexContentRestriction)content).Particle;
			}
			return null;
		}
		return ct.Particle;
	}

	internal DataColumn FindField(DataTable table, string field)
	{
		bool flag = false;
		string text = field;
		if (field.StartsWith('@'))
		{
			flag = true;
			text = field.Substring(1);
		}
		text = text.Split(':')[^1];
		text = XmlConvert.DecodeName(text);
		DataColumn dataColumn = table.Columns[text];
		if (dataColumn == null)
		{
			throw ExceptionBuilder.InvalidField(field);
		}
		bool flag2 = dataColumn.ColumnMapping == MappingType.Attribute || dataColumn.ColumnMapping == MappingType.Hidden;
		if (flag2 != flag)
		{
			throw ExceptionBuilder.InvalidField(field);
		}
		return dataColumn;
	}

	internal DataColumn[] BuildKey(XmlSchemaIdentityConstraint keyNode, DataTable table)
	{
		ArrayList arrayList = new ArrayList();
		foreach (XmlSchemaXPath field in keyNode.Fields)
		{
			arrayList.Add(FindField(table, field.XPath));
		}
		DataColumn[] array = new DataColumn[arrayList.Count];
		arrayList.CopyTo(array, 0);
		return array;
	}

	internal bool GetBooleanAttribute(XmlSchemaAnnotated element, string attrName, bool defVal)
	{
		string msdataAttribute = GetMsdataAttribute(element, attrName);
		if (msdataAttribute == null || msdataAttribute.Length == 0)
		{
			return defVal;
		}
		switch (msdataAttribute)
		{
		case "true":
		case "1":
			return true;
		case "false":
		case "0":
			return false;
		default:
			throw ExceptionBuilder.InvalidAttributeValue(attrName, msdataAttribute);
		}
	}

	internal string GetStringAttribute(XmlSchemaAnnotated element, string attrName, string defVal)
	{
		string msdataAttribute = GetMsdataAttribute(element, attrName);
		if (msdataAttribute == null || msdataAttribute.Length == 0)
		{
			return defVal;
		}
		return msdataAttribute;
	}

	internal static AcceptRejectRule TranslateAcceptRejectRule(string strRule)
	{
		if (strRule == "Cascade")
		{
			return AcceptRejectRule.Cascade;
		}
		_ = strRule == "None";
		return AcceptRejectRule.None;
	}

	internal static Rule TranslateRule(string strRule)
	{
		return strRule switch
		{
			"Cascade" => Rule.Cascade, 
			"None" => Rule.None, 
			"SetDefault" => Rule.SetDefault, 
			"SetNull" => Rule.SetNull, 
			_ => Rule.Cascade, 
		};
	}

	[RequiresUnreferencedCode("Members from serialized types may be trimmed if not referenced directly.")]
	internal void HandleKeyref(XmlSchemaKeyref keyref)
	{
		string text = XmlConvert.DecodeName(keyref.Refer.Name);
		string defVal = XmlConvert.DecodeName(keyref.Name);
		defVal = GetStringAttribute(keyref, "ConstraintName", defVal);
		string tableName = GetTableName(keyref);
		string msdataAttribute = GetMsdataAttribute(keyref, "TableNamespace");
		DataTable tableSmart = _ds.Tables.GetTableSmart(tableName, msdataAttribute);
		if (tableSmart == null)
		{
			return;
		}
		if (text == null || text.Length == 0)
		{
			throw ExceptionBuilder.MissingRefer(defVal);
		}
		ConstraintTable constraintTable = (ConstraintTable)_constraintNodes[text];
		if (constraintTable == null)
		{
			throw ExceptionBuilder.InvalidKey(defVal);
		}
		DataColumn[] array = BuildKey(constraintTable.constraint, constraintTable.table);
		DataColumn[] array2 = BuildKey(keyref, tableSmart);
		ForeignKeyConstraint foreignKeyConstraint = null;
		if (GetBooleanAttribute(keyref, "ConstraintOnly", defVal: false))
		{
			int num = array2[0].Table.Constraints.InternalIndexOf(defVal);
			if (num > -1 && array2[0].Table.Constraints[num].ConstraintName != defVal)
			{
				num = -1;
			}
			if (num < 0)
			{
				foreignKeyConstraint = new ForeignKeyConstraint(defVal, array, array2);
				array2[0].Table.Constraints.Add(foreignKeyConstraint);
			}
		}
		else
		{
			string text2 = XmlConvert.DecodeName(GetStringAttribute(keyref, "RelationName", keyref.Name));
			if (text2 == null || text2.Length == 0)
			{
				text2 = defVal;
			}
			int num2 = array2[0].Table.DataSet.Relations.InternalIndexOf(text2);
			if (num2 > -1 && array2[0].Table.DataSet.Relations[num2].RelationName != text2)
			{
				num2 = -1;
			}
			DataRelation dataRelation = null;
			if (num2 < 0)
			{
				dataRelation = new DataRelation(text2, array, array2);
				SetExtProperties(dataRelation, keyref.UnhandledAttributes);
				array[0].Table.DataSet.Relations.Add(dataRelation);
				if (FromInference && dataRelation.Nested && _tableDictionary.ContainsKey(dataRelation.ParentTable))
				{
					_tableDictionary[dataRelation.ParentTable].Add(dataRelation.ChildTable);
				}
				foreignKeyConstraint = dataRelation.ChildKeyConstraint;
				foreignKeyConstraint.ConstraintName = defVal;
			}
			else
			{
				dataRelation = array2[0].Table.DataSet.Relations[num2];
			}
			if (GetBooleanAttribute(keyref, "IsNested", defVal: false))
			{
				dataRelation.Nested = true;
			}
		}
		string msdataAttribute2 = GetMsdataAttribute(keyref, "AcceptRejectRule");
		string msdataAttribute3 = GetMsdataAttribute(keyref, "UpdateRule");
		string msdataAttribute4 = GetMsdataAttribute(keyref, "DeleteRule");
		if (foreignKeyConstraint != null)
		{
			if (msdataAttribute2 != null)
			{
				foreignKeyConstraint.AcceptRejectRule = TranslateAcceptRejectRule(msdataAttribute2);
			}
			if (msdataAttribute3 != null)
			{
				foreignKeyConstraint.UpdateRule = TranslateRule(msdataAttribute3);
			}
			if (msdataAttribute4 != null)
			{
				foreignKeyConstraint.DeleteRule = TranslateRule(msdataAttribute4);
			}
			SetExtProperties(foreignKeyConstraint, keyref.UnhandledAttributes);
		}
	}

	[RequiresUnreferencedCode("Members from serialized types may be trimmed if not referenced directly.")]
	internal void HandleConstraint(XmlSchemaIdentityConstraint keyNode)
	{
		string text = null;
		text = XmlConvert.DecodeName(keyNode.Name);
		if (text == null || text.Length == 0)
		{
			throw ExceptionBuilder.MissingAttribute("name");
		}
		if (_constraintNodes.ContainsKey(text))
		{
			throw ExceptionBuilder.DuplicateConstraintRead(text);
		}
		string tableName = GetTableName(keyNode);
		string msdataAttribute = GetMsdataAttribute(keyNode, "TableNamespace");
		DataTable tableSmart = _ds.Tables.GetTableSmart(tableName, msdataAttribute);
		if (tableSmart == null)
		{
			return;
		}
		_constraintNodes.Add(text, new ConstraintTable(tableSmart, keyNode));
		bool booleanAttribute = GetBooleanAttribute(keyNode, "PrimaryKey", defVal: false);
		text = GetStringAttribute(keyNode, "ConstraintName", text);
		DataColumn[] array = BuildKey(keyNode, tableSmart);
		if (array.Length == 0)
		{
			return;
		}
		UniqueConstraint uniqueConstraint = (UniqueConstraint)array[0].Table.Constraints.FindConstraint(new UniqueConstraint(text, array));
		if (uniqueConstraint == null)
		{
			array[0].Table.Constraints.Add(text, array, booleanAttribute);
			SetExtProperties(array[0].Table.Constraints[text], keyNode.UnhandledAttributes);
		}
		else
		{
			array = uniqueConstraint.ColumnsReference;
			SetExtProperties(uniqueConstraint, keyNode.UnhandledAttributes);
			if (booleanAttribute)
			{
				array[0].Table.PrimaryKey = array;
			}
		}
		if (keyNode is XmlSchemaKey)
		{
			for (int i = 0; i < array.Length; i++)
			{
				array[i].AllowDBNull = false;
			}
		}
	}

	[RequiresUnreferencedCode("Members from serialized types may be trimmed if not referenced directly.")]
	internal DataTable InstantiateSimpleTable(XmlSchemaElement node)
	{
		string text = XmlConvert.DecodeName(GetInstanceName(node));
		string @namespace = node.QualifiedName.Namespace;
		DataTable dataTable = _ds.Tables.GetTable(text, @namespace);
		if (!FromInference && dataTable != null)
		{
			throw ExceptionBuilder.DuplicateDeclaration(text);
		}
		if (dataTable == null)
		{
			dataTable = new DataTable(text);
			dataTable.Namespace = @namespace;
			dataTable.Namespace = GetStringAttribute(node, "targetNamespace", @namespace);
			if (!FromInference)
			{
				dataTable.MinOccurs = node.MinOccurs;
				dataTable.MaxOccurs = node.MaxOccurs;
			}
			else
			{
				string prefix = GetPrefix(@namespace);
				if (prefix != null)
				{
					dataTable.Prefix = prefix;
				}
			}
			SetProperties(dataTable, node.UnhandledAttributes);
			SetExtProperties(dataTable, node.UnhandledAttributes);
		}
		XmlSchemaComplexType xmlSchemaComplexType = node.SchemaType as XmlSchemaComplexType;
		bool flag = node.ElementSchemaType.BaseXmlSchemaType != null || (xmlSchemaComplexType != null && xmlSchemaComplexType.ContentModel is XmlSchemaSimpleContent);
		if (!FromInference || (flag && dataTable.Columns.Count == 0))
		{
			HandleElementColumn(node, dataTable, isBase: false);
			string text2;
			if (FromInference)
			{
				int num = 0;
				text2 = text + "_Text";
				while (dataTable.Columns[text2] != null)
				{
					text2 += num++;
				}
			}
			else
			{
				text2 = text + "_Column";
			}
			dataTable.Columns[0].ColumnName = text2;
			dataTable.Columns[0].ColumnMapping = MappingType.SimpleContent;
		}
		if (!FromInference || _ds.Tables.GetTable(text, @namespace) == null)
		{
			_ds.Tables.Add(dataTable);
			if (FromInference)
			{
				_tableDictionary.Add(dataTable, new List<DataTable>());
			}
		}
		if (_dsElement != null && _dsElement.Constraints != null)
		{
			foreach (XmlSchemaIdentityConstraint constraint in _dsElement.Constraints)
			{
				if (!(constraint is XmlSchemaKeyref) && GetTableName(constraint) == dataTable.TableName)
				{
					HandleConstraint(constraint);
				}
			}
		}
		dataTable._fNestedInDataset = false;
		return dataTable;
	}

	internal string GetInstanceName(XmlSchemaAnnotated node)
	{
		string result = null;
		if (node is XmlSchemaElement)
		{
			XmlSchemaElement xmlSchemaElement = (XmlSchemaElement)node;
			result = ((xmlSchemaElement.Name != null) ? xmlSchemaElement.Name : xmlSchemaElement.RefName.Name);
		}
		else if (node is XmlSchemaAttribute)
		{
			XmlSchemaAttribute xmlSchemaAttribute = (XmlSchemaAttribute)node;
			result = ((xmlSchemaAttribute.Name != null) ? xmlSchemaAttribute.Name : xmlSchemaAttribute.RefName.Name);
		}
		return result;
	}

	[RequiresUnreferencedCode("Members from serialized types may be trimmed if not referenced directly.")]
	internal DataTable InstantiateTable(XmlSchemaElement node, XmlSchemaComplexType typeNode, bool isRef)
	{
		string instanceName = GetInstanceName(node);
		ArrayList arrayList = new ArrayList();
		string @namespace = node.QualifiedName.Namespace;
		DataTable dataTable = _ds.Tables.GetTable(XmlConvert.DecodeName(instanceName), @namespace);
		if (!FromInference || (FromInference && dataTable == null))
		{
			if (dataTable != null)
			{
				if (isRef)
				{
					return dataTable;
				}
				throw ExceptionBuilder.DuplicateDeclaration(instanceName);
			}
			if (isRef)
			{
				_refTables.Add(@namespace + ":" + instanceName);
			}
			dataTable = new DataTable(XmlConvert.DecodeName(instanceName));
			dataTable.TypeName = node.SchemaTypeName;
			dataTable.Namespace = @namespace;
			dataTable.Namespace = GetStringAttribute(node, "targetNamespace", @namespace);
			string stringAttribute = GetStringAttribute(typeNode, "CaseSensitive", "");
			if (stringAttribute.Length == 0)
			{
				stringAttribute = GetStringAttribute(node, "CaseSensitive", "");
			}
			if (0 < stringAttribute.Length)
			{
				if (stringAttribute == "true" || stringAttribute == "True")
				{
					dataTable.CaseSensitive = true;
				}
				if (stringAttribute == "false" || stringAttribute == "False")
				{
					dataTable.CaseSensitive = false;
				}
			}
			stringAttribute = GetMsdataAttribute(node, "Locale");
			if (stringAttribute != null)
			{
				if (0 < stringAttribute.Length)
				{
					dataTable.Locale = new CultureInfo(stringAttribute);
				}
				else
				{
					dataTable.Locale = CultureInfo.InvariantCulture;
				}
			}
			if (!FromInference)
			{
				dataTable.MinOccurs = node.MinOccurs;
				dataTable.MaxOccurs = node.MaxOccurs;
			}
			else
			{
				string prefix = GetPrefix(@namespace);
				if (prefix != null)
				{
					dataTable.Prefix = prefix;
				}
			}
			_ds.Tables.Add(dataTable);
			if (FromInference)
			{
				_tableDictionary.Add(dataTable, new List<DataTable>());
			}
		}
		HandleComplexType(typeNode, dataTable, arrayList, node.IsNillable);
		for (int i = 0; i < dataTable.Columns.Count; i++)
		{
			dataTable.Columns[i].SetOrdinalInternal(i);
		}
		SetProperties(dataTable, node.UnhandledAttributes);
		SetExtProperties(dataTable, node.UnhandledAttributes);
		if (_dsElement != null && _dsElement.Constraints != null)
		{
			foreach (XmlSchemaIdentityConstraint constraint in _dsElement.Constraints)
			{
				if (!(constraint is XmlSchemaKeyref) && GetTableName(constraint) == dataTable.TableName && (GetTableNamespace(constraint) == dataTable.Namespace || GetTableNamespace(constraint) == null))
				{
					HandleConstraint(constraint);
				}
			}
		}
		foreach (DataTable item in arrayList)
		{
			if (item != dataTable && dataTable.Namespace == item.Namespace)
			{
				item._tableNamespace = null;
			}
			if (_dsElement != null && _dsElement.Constraints != null)
			{
				foreach (XmlSchemaIdentityConstraint constraint2 in _dsElement.Constraints)
				{
					if (!(constraint2 is XmlSchemaKeyref xmlSchemaKeyref) || !GetBooleanAttribute(xmlSchemaKeyref, "IsNested", defVal: false) || !(GetTableName(xmlSchemaKeyref) == item.TableName))
					{
						continue;
					}
					if (item.DataSet.Tables.InternalIndexOf(item.TableName) < -1)
					{
						if (GetTableNamespace(xmlSchemaKeyref) == item.Namespace)
						{
							HandleKeyref(xmlSchemaKeyref);
						}
					}
					else
					{
						HandleKeyref(xmlSchemaKeyref);
					}
				}
			}
			DataRelation dataRelation = null;
			DataRelationCollection childRelations = dataTable.ChildRelations;
			for (int j = 0; j < childRelations.Count; j++)
			{
				if (childRelations[j].Nested && item == childRelations[j].ChildTable)
				{
					dataRelation = childRelations[j];
				}
			}
			if (dataRelation != null)
			{
				continue;
			}
			DataColumn dataColumn2;
			if (FromInference)
			{
				int num = dataTable.UKColumnPositionForInference;
				if (num == -1)
				{
					foreach (DataColumn column in dataTable.Columns)
					{
						if (column.ColumnMapping == MappingType.Attribute)
						{
							num = column.Ordinal;
							break;
						}
					}
				}
				dataColumn2 = dataTable.AddUniqueKey(num);
			}
			else
			{
				dataColumn2 = dataTable.AddUniqueKey();
			}
			DataColumn dataColumn3 = item.AddForeignKey(dataColumn2);
			if (FromInference)
			{
				dataColumn3.Prefix = item.Prefix;
			}
			dataRelation = new DataRelation(dataTable.TableName + "_" + item.TableName, dataColumn2, dataColumn3, createConstraints: true);
			dataRelation.Nested = true;
			item.DataSet.Relations.Add(dataRelation);
			if (FromInference && dataRelation.Nested && _tableDictionary.ContainsKey(dataRelation.ParentTable))
			{
				_tableDictionary[dataRelation.ParentTable].Add(dataRelation.ChildTable);
			}
		}
		return dataTable;
	}

	public static Type XsdtoClr(string xsdTypeName)
	{
		int num = Array.BinarySearch(s_mapNameTypeXsd, xsdTypeName);
		if (num < 0)
		{
			throw ExceptionBuilder.UndefinedDatatype(xsdTypeName);
		}
		return s_mapNameTypeXsd[num].type;
	}

	private static NameType FindNameType(string name)
	{
		int num = Array.BinarySearch(s_mapNameTypeXsd, name);
		if (num < 0)
		{
			throw ExceptionBuilder.UndefinedDatatype(name);
		}
		return s_mapNameTypeXsd[num];
	}

	[return: DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)]
	private Type ParseDataType(string dt)
	{
		if (!IsXsdType(dt) && _udSimpleTypes != null)
		{
			XmlSchemaSimpleType xmlSchemaSimpleType = (XmlSchemaSimpleType)_udSimpleTypes[dt];
			if (xmlSchemaSimpleType == null)
			{
				throw ExceptionBuilder.UndefinedDatatype(dt);
			}
			SimpleType simpleType = new SimpleType(xmlSchemaSimpleType);
			while (simpleType.BaseSimpleType != null)
			{
				simpleType = simpleType.BaseSimpleType;
			}
			return ParseDataType(simpleType.BaseType);
		}
		NameType nameType = FindNameType(dt);
		return nameType.type;
	}

	internal static bool IsXsdType(string name)
	{
		int num = Array.BinarySearch(s_mapNameTypeXsd, name);
		if (num < 0)
		{
			return false;
		}
		return true;
	}

	internal XmlSchemaAnnotated FindTypeNode(XmlSchemaAnnotated node)
	{
		XmlSchemaAttribute xmlSchemaAttribute = node as XmlSchemaAttribute;
		XmlSchemaElement xmlSchemaElement = node as XmlSchemaElement;
		bool flag = false;
		if (xmlSchemaAttribute != null)
		{
			flag = true;
		}
		string text = (flag ? xmlSchemaAttribute.SchemaTypeName.Name : xmlSchemaElement.SchemaTypeName.Name);
		string text2 = (flag ? xmlSchemaAttribute.SchemaTypeName.Namespace : xmlSchemaElement.SchemaTypeName.Namespace);
		if (text2 == "http://www.w3.org/2001/XMLSchema")
		{
			return null;
		}
		if (text == null || text.Length == 0)
		{
			text = (flag ? xmlSchemaAttribute.RefName.Name : xmlSchemaElement.RefName.Name);
			if (text == null || text.Length == 0)
			{
				return flag ? xmlSchemaAttribute.SchemaType : xmlSchemaElement.SchemaType;
			}
			return flag ? FindTypeNode((XmlSchemaAnnotated)_attributes[xmlSchemaAttribute.RefName]) : FindTypeNode((XmlSchemaAnnotated)_elementsTable[xmlSchemaElement.RefName]);
		}
		return (XmlSchemaAnnotated)_schemaTypes[flag ? ((XmlSchemaAttribute)node).SchemaTypeName : ((XmlSchemaElement)node).SchemaTypeName];
	}

	[RequiresUnreferencedCode("Members from serialized types may be trimmed if not referenced directly.")]
	internal void HandleSimpleTypeSimpleContentColumn(XmlSchemaSimpleType typeNode, string strType, DataTable table, bool isBase, XmlAttribute[] attrs, bool isNillable)
	{
		if (FromInference && table.XmlText != null)
		{
			return;
		}
		Type type = null;
		SimpleType simpleType = null;
		if (typeNode.QualifiedName.Name != null && typeNode.QualifiedName.Name.Length != 0 && typeNode.QualifiedName.Namespace != "http://www.w3.org/2001/XMLSchema")
		{
			simpleType = new SimpleType(typeNode);
			strType = typeNode.QualifiedName.ToString();
			type = ParseDataType(typeNode.QualifiedName.ToString());
		}
		else if (typeNode.BaseXmlSchemaType is XmlSchemaSimpleType xmlSchemaSimpleType && xmlSchemaSimpleType.QualifiedName.Namespace != "http://www.w3.org/2001/XMLSchema")
		{
			simpleType = new SimpleType(typeNode);
			SimpleType simpleType2 = simpleType;
			while (simpleType2.BaseSimpleType != null)
			{
				simpleType2 = simpleType2.BaseSimpleType;
			}
			type = ParseDataType(simpleType2.BaseType);
			strType = simpleType.Name;
		}
		else
		{
			type = ParseDataType(strType);
		}
		string text;
		if (FromInference)
		{
			int num = 0;
			text = table.TableName + "_Text";
			while (table.Columns[text] != null)
			{
				text += num++;
			}
		}
		else
		{
			text = table.TableName + "_text";
		}
		string text2 = text;
		bool flag = true;
		DataColumn dataColumn;
		if (!isBase && table.Columns.Contains(text2, caseSensitive: true))
		{
			dataColumn = table.Columns[text2];
			flag = false;
		}
		else
		{
			dataColumn = new DataColumn(text2, type, null, MappingType.SimpleContent);
		}
		SetProperties(dataColumn, attrs);
		HandleColumnExpression(dataColumn, attrs);
		SetExtProperties(dataColumn, attrs);
		string value = (-1).ToString(CultureInfo.CurrentCulture);
		string text3 = null;
		dataColumn.AllowDBNull = isNillable;
		if (attrs != null)
		{
			for (int i = 0; i < attrs.Length; i++)
			{
				if (attrs[i].LocalName == "AllowDBNull" && attrs[i].NamespaceURI == "urn:schemas-microsoft-com:xml-msdata" && attrs[i].Value == "false")
				{
					dataColumn.AllowDBNull = false;
				}
				if (attrs[i].LocalName == "Ordinal" && attrs[i].NamespaceURI == "urn:schemas-microsoft-com:xml-msdata")
				{
					value = attrs[i].Value;
				}
				if (attrs[i].LocalName == "DefaultValue" && attrs[i].NamespaceURI == "urn:schemas-microsoft-com:xml-msdata")
				{
					text3 = attrs[i].Value;
				}
			}
		}
		int num2 = (int)Convert.ChangeType(value, typeof(int), null);
		if (dataColumn.Expression != null && dataColumn.Expression.Length != 0)
		{
			_columnExpressions.Add(dataColumn);
		}
		if (simpleType != null && simpleType.Name != null && simpleType.Name.Length > 0)
		{
			if (GetMsdataAttribute(typeNode, "targetNamespace") != null)
			{
				dataColumn.XmlDataType = simpleType.SimpleTypeQualifiedName;
			}
		}
		else
		{
			dataColumn.XmlDataType = strType;
		}
		dataColumn.SimpleType = simpleType;
		if (flag)
		{
			if (FromInference)
			{
				dataColumn.Prefix = GetPrefix(table.Namespace);
				dataColumn.AllowDBNull = true;
			}
			if (num2 > -1 && num2 < table.Columns.Count)
			{
				table.Columns.AddAt(num2, dataColumn);
			}
			else
			{
				table.Columns.Add(dataColumn);
			}
		}
		if (text3 != null)
		{
			try
			{
				dataColumn.DefaultValue = dataColumn.ConvertXmlToObject(text3);
			}
			catch (FormatException)
			{
				throw ExceptionBuilder.CannotConvert(text3, type.FullName);
			}
		}
	}

	[RequiresUnreferencedCode("Members from serialized types may be trimmed if not referenced directly.")]
	internal void HandleSimpleContentColumn(string strType, DataTable table, bool isBase, XmlAttribute[] attrs, bool isNillable)
	{
		if (FromInference && table.XmlText != null)
		{
			return;
		}
		Type type = null;
		if (strType == null)
		{
			return;
		}
		type = ParseDataType(strType);
		string text;
		if (FromInference)
		{
			int num = 0;
			text = table.TableName + "_Text";
			while (table.Columns[text] != null)
			{
				text += num++;
			}
		}
		else
		{
			text = table.TableName + "_text";
		}
		string text2 = text;
		bool flag = true;
		DataColumn dataColumn;
		if (!isBase && table.Columns.Contains(text2, caseSensitive: true))
		{
			dataColumn = table.Columns[text2];
			flag = false;
		}
		else
		{
			dataColumn = new DataColumn(text2, type, null, MappingType.SimpleContent);
		}
		SetProperties(dataColumn, attrs);
		HandleColumnExpression(dataColumn, attrs);
		SetExtProperties(dataColumn, attrs);
		string value = (-1).ToString(CultureInfo.CurrentCulture);
		string text3 = null;
		dataColumn.AllowDBNull = isNillable;
		if (attrs != null)
		{
			for (int i = 0; i < attrs.Length; i++)
			{
				if (attrs[i].LocalName == "AllowDBNull" && attrs[i].NamespaceURI == "urn:schemas-microsoft-com:xml-msdata" && attrs[i].Value == "false")
				{
					dataColumn.AllowDBNull = false;
				}
				if (attrs[i].LocalName == "Ordinal" && attrs[i].NamespaceURI == "urn:schemas-microsoft-com:xml-msdata")
				{
					value = attrs[i].Value;
				}
				if (attrs[i].LocalName == "DefaultValue" && attrs[i].NamespaceURI == "urn:schemas-microsoft-com:xml-msdata")
				{
					text3 = attrs[i].Value;
				}
			}
		}
		int num2 = (int)Convert.ChangeType(value, typeof(int), null);
		if (dataColumn.Expression != null && dataColumn.Expression.Length != 0)
		{
			_columnExpressions.Add(dataColumn);
		}
		dataColumn.XmlDataType = strType;
		dataColumn.SimpleType = null;
		if (FromInference)
		{
			dataColumn.Prefix = GetPrefix(dataColumn.Namespace);
		}
		if (flag)
		{
			if (FromInference)
			{
				dataColumn.AllowDBNull = true;
			}
			if (num2 > -1 && num2 < table.Columns.Count)
			{
				table.Columns.AddAt(num2, dataColumn);
			}
			else
			{
				table.Columns.Add(dataColumn);
			}
		}
		if (text3 == null)
		{
			return;
		}
		try
		{
			dataColumn.DefaultValue = dataColumn.ConvertXmlToObject(text3);
		}
		catch (FormatException)
		{
			throw ExceptionBuilder.CannotConvert(text3, type.FullName);
		}
	}

	[RequiresUnreferencedCode("Members from serialized types may be trimmed if not referenced directly.")]
	internal void HandleAttributeColumn(XmlSchemaAttribute attrib, DataTable table, bool isBase)
	{
		Type type = null;
		XmlSchemaAttribute xmlSchemaAttribute = ((attrib.Name != null) ? attrib : ((XmlSchemaAttribute)_attributes[attrib.RefName]));
		XmlSchemaAnnotated xmlSchemaAnnotated = FindTypeNode(xmlSchemaAttribute);
		string text = null;
		SimpleType simpleType = null;
		if (xmlSchemaAnnotated == null)
		{
			text = xmlSchemaAttribute.SchemaTypeName.Name;
			if (!string.IsNullOrEmpty(text))
			{
				type = ((!(xmlSchemaAttribute.SchemaTypeName.Namespace != "http://www.w3.org/2001/XMLSchema")) ? ParseDataType(xmlSchemaAttribute.SchemaTypeName.Name) : ParseDataType(xmlSchemaAttribute.SchemaTypeName.ToString()));
			}
			else
			{
				text = string.Empty;
				type = typeof(string);
			}
		}
		else if (xmlSchemaAnnotated is XmlSchemaSimpleType)
		{
			XmlSchemaSimpleType xmlSchemaSimpleType = xmlSchemaAnnotated as XmlSchemaSimpleType;
			simpleType = new SimpleType(xmlSchemaSimpleType);
			if (xmlSchemaSimpleType.QualifiedName.Name != null && xmlSchemaSimpleType.QualifiedName.Name.Length != 0 && xmlSchemaSimpleType.QualifiedName.Namespace != "http://www.w3.org/2001/XMLSchema")
			{
				text = xmlSchemaSimpleType.QualifiedName.ToString();
				type = ParseDataType(xmlSchemaSimpleType.QualifiedName.ToString());
			}
			else
			{
				type = ParseDataType(simpleType.BaseType);
				text = simpleType.Name;
				if (simpleType.Length == 1 && type == typeof(string))
				{
					type = typeof(char);
				}
			}
		}
		else
		{
			if (!(xmlSchemaAnnotated is XmlSchemaElement))
			{
				if (xmlSchemaAnnotated.Id == null)
				{
					throw ExceptionBuilder.DatatypeNotDefined();
				}
				throw ExceptionBuilder.UndefinedDatatype(xmlSchemaAnnotated.Id);
			}
			text = ((XmlSchemaElement)xmlSchemaAnnotated).SchemaTypeName.Name;
			type = ParseDataType(text);
		}
		string text2 = XmlConvert.DecodeName(GetInstanceName(xmlSchemaAttribute));
		bool flag = true;
		DataColumn dataColumn;
		if ((!isBase || FromInference) && table.Columns.Contains(text2, caseSensitive: true))
		{
			dataColumn = table.Columns[text2];
			flag = false;
			if (FromInference)
			{
				if (dataColumn.ColumnMapping != MappingType.Attribute)
				{
					throw ExceptionBuilder.ColumnTypeConflict(dataColumn.ColumnName);
				}
				if ((string.IsNullOrEmpty(attrib.QualifiedName.Namespace) && string.IsNullOrEmpty(dataColumn._columnUri)) || string.Equals(attrib.QualifiedName.Namespace, dataColumn.Namespace, StringComparison.Ordinal))
				{
					return;
				}
				dataColumn = new DataColumn(text2, type, null, MappingType.Attribute);
				flag = true;
			}
		}
		else
		{
			dataColumn = new DataColumn(text2, type, null, MappingType.Attribute);
		}
		SetProperties(dataColumn, xmlSchemaAttribute.UnhandledAttributes);
		HandleColumnExpression(dataColumn, xmlSchemaAttribute.UnhandledAttributes);
		SetExtProperties(dataColumn, xmlSchemaAttribute.UnhandledAttributes);
		if (dataColumn.Expression != null && dataColumn.Expression.Length != 0)
		{
			_columnExpressions.Add(dataColumn);
		}
		if (simpleType != null && simpleType.Name != null && simpleType.Name.Length > 0)
		{
			if (GetMsdataAttribute(xmlSchemaAnnotated, "targetNamespace") != null)
			{
				dataColumn.XmlDataType = simpleType.SimpleTypeQualifiedName;
			}
		}
		else
		{
			dataColumn.XmlDataType = text;
		}
		dataColumn.SimpleType = simpleType;
		dataColumn.AllowDBNull = attrib.Use != XmlSchemaUse.Required;
		dataColumn.Namespace = attrib.QualifiedName.Namespace;
		dataColumn.Namespace = GetStringAttribute(attrib, "targetNamespace", dataColumn.Namespace);
		if (flag)
		{
			if (FromInference)
			{
				dataColumn.AllowDBNull = true;
				dataColumn.Prefix = GetPrefix(dataColumn.Namespace);
			}
			table.Columns.Add(dataColumn);
		}
		if (attrib.Use == XmlSchemaUse.Prohibited)
		{
			dataColumn.ColumnMapping = MappingType.Hidden;
			dataColumn.AllowDBNull = GetBooleanAttribute(xmlSchemaAttribute, "AllowDBNull", defVal: true);
			string msdataAttribute = GetMsdataAttribute(xmlSchemaAttribute, "DefaultValue");
			if (msdataAttribute != null)
			{
				try
				{
					dataColumn.DefaultValue = dataColumn.ConvertXmlToObject(msdataAttribute);
				}
				catch (FormatException)
				{
					throw ExceptionBuilder.CannotConvert(msdataAttribute, type.FullName);
				}
			}
		}
		string text3 = ((attrib.Use == XmlSchemaUse.Required) ? GetMsdataAttribute(xmlSchemaAttribute, "DefaultValue") : xmlSchemaAttribute.DefaultValue);
		if (xmlSchemaAttribute.Use == XmlSchemaUse.Optional && text3 == null)
		{
			text3 = xmlSchemaAttribute.FixedValue;
		}
		if (text3 != null)
		{
			try
			{
				dataColumn.DefaultValue = dataColumn.ConvertXmlToObject(text3);
			}
			catch (FormatException)
			{
				throw ExceptionBuilder.CannotConvert(text3, type.FullName);
			}
		}
	}

	[RequiresUnreferencedCode("Members from serialized types may be trimmed if not referenced directly.")]
	internal void HandleElementColumn(XmlSchemaElement elem, DataTable table, bool isBase)
	{
		Type type = null;
		XmlSchemaElement xmlSchemaElement = ((elem.Name != null) ? elem : ((XmlSchemaElement)_elementsTable[elem.RefName]));
		if (xmlSchemaElement == null)
		{
			return;
		}
		XmlSchemaAnnotated xmlSchemaAnnotated = FindTypeNode(xmlSchemaElement);
		string text = null;
		SimpleType simpleType = null;
		if (xmlSchemaAnnotated == null)
		{
			text = xmlSchemaElement.SchemaTypeName.Name;
			if (string.IsNullOrEmpty(text))
			{
				text = string.Empty;
				type = typeof(string);
			}
			else
			{
				type = ParseDataType(xmlSchemaElement.SchemaTypeName.Name);
			}
		}
		else if (xmlSchemaAnnotated is XmlSchemaSimpleType)
		{
			XmlSchemaSimpleType node = xmlSchemaAnnotated as XmlSchemaSimpleType;
			simpleType = new SimpleType(node);
			if (((XmlSchemaSimpleType)xmlSchemaAnnotated).Name != null && ((XmlSchemaSimpleType)xmlSchemaAnnotated).Name.Length != 0 && ((XmlSchemaSimpleType)xmlSchemaAnnotated).QualifiedName.Namespace != "http://www.w3.org/2001/XMLSchema")
			{
				text = ((XmlSchemaSimpleType)xmlSchemaAnnotated).QualifiedName.ToString();
				type = ParseDataType(text);
			}
			else
			{
				for (node = ((simpleType.XmlBaseType != null && simpleType.XmlBaseType.Namespace != "http://www.w3.org/2001/XMLSchema") ? (_schemaTypes[simpleType.XmlBaseType] as XmlSchemaSimpleType) : null); node != null; node = ((simpleType.XmlBaseType != null && simpleType.XmlBaseType.Namespace != "http://www.w3.org/2001/XMLSchema") ? (_schemaTypes[simpleType.XmlBaseType] as XmlSchemaSimpleType) : null))
				{
					simpleType.LoadTypeValues(node);
				}
				type = ParseDataType(simpleType.BaseType);
				text = simpleType.Name;
				if (simpleType.Length == 1 && type == typeof(string))
				{
					type = typeof(char);
				}
			}
		}
		else if (xmlSchemaAnnotated is XmlSchemaElement)
		{
			text = ((XmlSchemaElement)xmlSchemaAnnotated).SchemaTypeName.Name;
			type = ParseDataType(text);
		}
		else
		{
			if (!(xmlSchemaAnnotated is XmlSchemaComplexType))
			{
				if (xmlSchemaAnnotated.Id == null)
				{
					throw ExceptionBuilder.DatatypeNotDefined();
				}
				throw ExceptionBuilder.UndefinedDatatype(xmlSchemaAnnotated.Id);
			}
			if (string.IsNullOrEmpty(GetMsdataAttribute(elem, "DataType")))
			{
				throw ExceptionBuilder.DatatypeNotDefined();
			}
			type = typeof(object);
		}
		string text2 = XmlConvert.DecodeName(GetInstanceName(xmlSchemaElement));
		bool flag = true;
		DataColumn dataColumn;
		if ((!isBase || FromInference) && table.Columns.Contains(text2, caseSensitive: true))
		{
			dataColumn = table.Columns[text2];
			flag = false;
			if (FromInference)
			{
				if (dataColumn.ColumnMapping != MappingType.Element)
				{
					throw ExceptionBuilder.ColumnTypeConflict(dataColumn.ColumnName);
				}
				if ((string.IsNullOrEmpty(elem.QualifiedName.Namespace) && string.IsNullOrEmpty(dataColumn._columnUri)) || string.Equals(elem.QualifiedName.Namespace, dataColumn.Namespace, StringComparison.Ordinal))
				{
					return;
				}
				dataColumn = new DataColumn(text2, type, null, MappingType.Element);
				flag = true;
			}
		}
		else
		{
			dataColumn = new DataColumn(text2, type, null, MappingType.Element);
		}
		SetProperties(dataColumn, xmlSchemaElement.UnhandledAttributes);
		HandleColumnExpression(dataColumn, xmlSchemaElement.UnhandledAttributes);
		SetExtProperties(dataColumn, xmlSchemaElement.UnhandledAttributes);
		if (!string.IsNullOrEmpty(dataColumn.Expression))
		{
			_columnExpressions.Add(dataColumn);
		}
		if (simpleType != null && simpleType.Name != null && simpleType.Name.Length > 0)
		{
			if (GetMsdataAttribute(xmlSchemaAnnotated, "targetNamespace") != null)
			{
				dataColumn.XmlDataType = simpleType.SimpleTypeQualifiedName;
			}
		}
		else
		{
			dataColumn.XmlDataType = text;
		}
		dataColumn.SimpleType = simpleType;
		dataColumn.AllowDBNull = FromInference || elem.MinOccurs == 0m || elem.IsNillable;
		if (!elem.RefName.IsEmpty || elem.QualifiedName.Namespace != table.Namespace)
		{
			dataColumn.Namespace = elem.QualifiedName.Namespace;
			dataColumn.Namespace = GetStringAttribute(xmlSchemaElement, "targetNamespace", dataColumn.Namespace);
		}
		else if (elem.Form == XmlSchemaForm.Unqualified)
		{
			dataColumn.Namespace = string.Empty;
		}
		else if (elem.Form == XmlSchemaForm.None)
		{
			XmlSchemaObject parent = elem.Parent;
			while (parent.Parent != null)
			{
				parent = parent.Parent;
			}
			if (((XmlSchema)parent).ElementFormDefault == XmlSchemaForm.Unqualified)
			{
				dataColumn.Namespace = string.Empty;
			}
		}
		else
		{
			dataColumn.Namespace = elem.QualifiedName.Namespace;
			dataColumn.Namespace = GetStringAttribute(xmlSchemaElement, "targetNamespace", dataColumn.Namespace);
		}
		string stringAttribute = GetStringAttribute(elem, "Ordinal", (-1).ToString(CultureInfo.CurrentCulture));
		int num = (int)Convert.ChangeType(stringAttribute, typeof(int), null);
		if (flag)
		{
			if (num > -1 && num < table.Columns.Count)
			{
				table.Columns.AddAt(num, dataColumn);
			}
			else
			{
				table.Columns.Add(dataColumn);
			}
		}
		if (dataColumn.Namespace == table.Namespace)
		{
			dataColumn._columnUri = null;
		}
		if (FromInference)
		{
			dataColumn.Prefix = GetPrefix(dataColumn.Namespace);
		}
		string defaultValue = xmlSchemaElement.DefaultValue;
		if (defaultValue == null)
		{
			return;
		}
		try
		{
			dataColumn.DefaultValue = dataColumn.ConvertXmlToObject(defaultValue);
		}
		catch (FormatException)
		{
			throw ExceptionBuilder.CannotConvert(defaultValue, type.FullName);
		}
	}

	[RequiresUnreferencedCode("Members from serialized types may be trimmed if not referenced directly.")]
	internal void HandleDataSet(XmlSchemaElement node, bool isNewDataSet)
	{
		string text = node.Name;
		string @namespace = node.QualifiedName.Namespace;
		List<DataTable> list = new List<DataTable>();
		string msdataAttribute = GetMsdataAttribute(node, "Locale");
		if (msdataAttribute != null)
		{
			if (msdataAttribute.Length != 0)
			{
				_ds.Locale = new CultureInfo(msdataAttribute);
			}
			else
			{
				_ds.Locale = CultureInfo.InvariantCulture;
			}
		}
		else if (GetBooleanAttribute(node, "UseCurrentLocale", defVal: false))
		{
			_ds.SetLocaleValue(CultureInfo.CurrentCulture, userSet: false);
		}
		else
		{
			_ds.SetLocaleValue(new CultureInfo(1033), userSet: false);
		}
		msdataAttribute = GetMsdataAttribute(node, "DataSetName");
		if (msdataAttribute != null && msdataAttribute.Length != 0)
		{
			text = msdataAttribute;
		}
		msdataAttribute = GetMsdataAttribute(node, "DataSetNamespace");
		if (msdataAttribute != null && msdataAttribute.Length != 0)
		{
			@namespace = msdataAttribute;
		}
		SetProperties(_ds, node.UnhandledAttributes);
		SetExtProperties(_ds, node.UnhandledAttributes);
		if (text != null && text.Length != 0)
		{
			_ds.DataSetName = XmlConvert.DecodeName(text);
		}
		_ds.Namespace = @namespace;
		if (FromInference)
		{
			_ds.Prefix = GetPrefix(_ds.Namespace);
		}
		XmlSchemaComplexType xmlSchemaComplexType = (XmlSchemaComplexType)FindTypeNode(node);
		if (xmlSchemaComplexType.Particle != null)
		{
			XmlSchemaObjectCollection particleItems = GetParticleItems(xmlSchemaComplexType.Particle);
			if (particleItems == null)
			{
				return;
			}
			foreach (XmlSchemaAnnotated item in particleItems)
			{
				if (item is XmlSchemaElement)
				{
					if (((XmlSchemaElement)item).RefName.Name.Length != 0)
					{
						if (!FromInference)
						{
							continue;
						}
						DataTable table = _ds.Tables.GetTable(XmlConvert.DecodeName(GetInstanceName((XmlSchemaElement)item)), node.QualifiedName.Namespace);
						if (table != null)
						{
							list.Add(table);
						}
						bool flag = false;
						if (node.ElementSchemaType != null || !(((XmlSchemaElement)item).SchemaType is XmlSchemaComplexType))
						{
							flag = true;
						}
						if (((XmlSchemaElement)item).MaxOccurs != 1m && !flag)
						{
							continue;
						}
					}
					DataTable dataTable = HandleTable((XmlSchemaElement)item);
					if (dataTable != null)
					{
						dataTable._fNestedInDataset = true;
					}
					if (FromInference)
					{
						list.Add(dataTable);
					}
				}
				else
				{
					if (!(item is XmlSchemaChoice))
					{
						continue;
					}
					XmlSchemaObjectCollection items = ((XmlSchemaChoice)item).Items;
					if (items == null)
					{
						continue;
					}
					foreach (XmlSchemaAnnotated item2 in items)
					{
						if (!(item2 is XmlSchemaElement))
						{
							continue;
						}
						if (((XmlSchemaParticle)item).MaxOccurs > 1m && ((XmlSchemaElement)item2).SchemaType is XmlSchemaComplexType)
						{
							((XmlSchemaElement)item2).MaxOccurs = ((XmlSchemaParticle)item).MaxOccurs;
						}
						if (((XmlSchemaElement)item2).RefName.Name.Length == 0 || FromInference || !(((XmlSchemaElement)item2).MaxOccurs != 1m) || ((XmlSchemaElement)item2).SchemaType is XmlSchemaComplexType)
						{
							DataTable dataTable2 = HandleTable((XmlSchemaElement)item2);
							if (FromInference)
							{
								list.Add(dataTable2);
							}
							if (dataTable2 != null)
							{
								dataTable2._fNestedInDataset = true;
							}
						}
					}
				}
			}
		}
		if (node.Constraints != null)
		{
			foreach (XmlSchemaIdentityConstraint constraint in node.Constraints)
			{
				if (constraint is XmlSchemaKeyref xmlSchemaKeyref && !GetBooleanAttribute(xmlSchemaKeyref, "IsNested", defVal: false))
				{
					HandleKeyref(xmlSchemaKeyref);
				}
			}
		}
		if (!(FromInference && isNewDataSet))
		{
			return;
		}
		List<DataTable> tableList = new List<DataTable>(_ds.Tables.Count);
		foreach (DataTable item3 in list)
		{
			AddTablesToList(tableList, item3);
		}
		_ds.Tables.ReplaceFromInference(tableList);
	}

	private void AddTablesToList(List<DataTable> tableList, DataTable dt)
	{
		if (tableList.Contains(dt))
		{
			return;
		}
		tableList.Add(dt);
		foreach (DataTable item in _tableDictionary[dt])
		{
			AddTablesToList(tableList, item);
		}
	}

	private string GetPrefix(string ns)
	{
		if (ns == null)
		{
			return null;
		}
		foreach (XmlSchema item in _schemaSet.Schemas())
		{
			XmlQualifiedName[] array = item.Namespaces.ToArray();
			for (int i = 0; i < array.Length; i++)
			{
				if (array[i].Namespace == ns)
				{
					return array[i].Name;
				}
			}
		}
		return null;
	}

	private string GetNamespaceFromPrefix(string prefix)
	{
		if (prefix == null || prefix.Length == 0)
		{
			return null;
		}
		foreach (XmlSchema item in _schemaSet.Schemas())
		{
			XmlQualifiedName[] array = item.Namespaces.ToArray();
			for (int i = 0; i < array.Length; i++)
			{
				if (array[i].Name == prefix)
				{
					return array[i].Namespace;
				}
			}
		}
		return null;
	}

	private string GetTableNamespace(XmlSchemaIdentityConstraint key)
	{
		string xPath = key.Selector.XPath;
		string[] array = xPath.Split('/');
		string empty = string.Empty;
		string text = array[^1];
		if (text == null || text.Length == 0)
		{
			throw ExceptionBuilder.InvalidSelector(xPath);
		}
		if (text.Contains(':'))
		{
			empty = text.Substring(0, text.IndexOf(':'));
			empty = XmlConvert.DecodeName(empty);
			return GetNamespaceFromPrefix(empty);
		}
		return GetMsdataAttribute(key, "TableNamespace");
	}

	private string GetTableName(XmlSchemaIdentityConstraint key)
	{
		string xPath = key.Selector.XPath;
		string text = xPath.Split('/', ':')[^1];
		if (text == null || text.Length == 0)
		{
			throw ExceptionBuilder.InvalidSelector(xPath);
		}
		return XmlConvert.DecodeName(text);
	}

	internal bool IsTable(XmlSchemaElement node)
	{
		if (node.MaxOccurs == 0m)
		{
			return false;
		}
		XmlAttribute[] unhandledAttributes = node.UnhandledAttributes;
		if (unhandledAttributes != null)
		{
			foreach (XmlAttribute xmlAttribute in unhandledAttributes)
			{
				if (xmlAttribute.LocalName == "DataType" && xmlAttribute.Prefix == "msdata" && xmlAttribute.NamespaceURI == "urn:schemas-microsoft-com:xml-msdata")
				{
					return false;
				}
			}
		}
		object obj = FindTypeNode(node);
		if (node.MaxOccurs > 1m && obj == null)
		{
			return true;
		}
		if (obj == null || !(obj is XmlSchemaComplexType))
		{
			return false;
		}
		XmlSchemaComplexType xmlSchemaComplexType = (XmlSchemaComplexType)obj;
		if (xmlSchemaComplexType.IsAbstract)
		{
			throw ExceptionBuilder.CannotInstantiateAbstract(node.Name);
		}
		return true;
	}

	[RequiresUnreferencedCode("Members from serialized types may be trimmed if not referenced directly.")]
	internal DataTable HandleTable(XmlSchemaElement node)
	{
		if (!IsTable(node))
		{
			return null;
		}
		object obj = FindTypeNode(node);
		if (node.MaxOccurs > 1m && obj == null)
		{
			return InstantiateSimpleTable(node);
		}
		DataTable dataTable = InstantiateTable(node, (XmlSchemaComplexType)obj, node.RefName != null);
		dataTable._fNestedInDataset = false;
		return dataTable;
	}
}
