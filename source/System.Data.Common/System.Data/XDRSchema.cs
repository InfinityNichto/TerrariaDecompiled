using System.Collections;
using System.Data.Common;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Xml;

namespace System.Data;

internal sealed class XDRSchema : XMLSchema
{
	private sealed class NameType : IComparable
	{
		public string name;

		[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicFields | DynamicallyAccessedMemberTypes.PublicProperties)]
		public Type type;

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

	internal string _schemaName;

	internal string _schemaUri;

	internal XmlElement _schemaRoot;

	internal DataSet _ds;

	private static readonly NameType[] s_mapNameTypeXdr = new NameType[36]
	{
		new NameType("bin.base64", typeof(byte[])),
		new NameType("bin.hex", typeof(byte[])),
		new NameType("boolean", typeof(bool)),
		new NameType("byte", typeof(sbyte)),
		new NameType("char", typeof(char)),
		new NameType("date", typeof(DateTime)),
		new NameType("dateTime", typeof(DateTime)),
		new NameType("dateTime.tz", typeof(DateTime)),
		new NameType("entities", typeof(string)),
		new NameType("entity", typeof(string)),
		new NameType("enumeration", typeof(string)),
		new NameType("fixed.14.4", typeof(decimal)),
		new NameType("float", typeof(double)),
		new NameType("i1", typeof(sbyte)),
		new NameType("i2", typeof(short)),
		new NameType("i4", typeof(int)),
		new NameType("i8", typeof(long)),
		new NameType("id", typeof(string)),
		new NameType("idref", typeof(string)),
		new NameType("idrefs", typeof(string)),
		new NameType("int", typeof(int)),
		new NameType("nmtoken", typeof(string)),
		new NameType("nmtokens", typeof(string)),
		new NameType("notation", typeof(string)),
		new NameType("number", typeof(decimal)),
		new NameType("r4", typeof(float)),
		new NameType("r8", typeof(double)),
		new NameType("string", typeof(string)),
		new NameType("time", typeof(DateTime)),
		new NameType("time.tz", typeof(DateTime)),
		new NameType("ui1", typeof(byte)),
		new NameType("ui2", typeof(ushort)),
		new NameType("ui4", typeof(uint)),
		new NameType("ui8", typeof(ulong)),
		new NameType("uri", typeof(string)),
		new NameType("uuid", typeof(Guid))
	};

	private static readonly NameType s_enumerationNameType = FindNameType("enumeration");

	internal XDRSchema(DataSet ds, bool fInline)
	{
		_schemaUri = string.Empty;
		_schemaName = string.Empty;
		_schemaRoot = null;
		_ds = ds;
	}

	[RequiresUnreferencedCode("Members from serialized types may be trimmed if not referenced directly.")]
	internal void LoadSchema(XmlElement schemaRoot, DataSet ds)
	{
		if (schemaRoot == null)
		{
			return;
		}
		_schemaRoot = schemaRoot;
		_ds = ds;
		_schemaName = schemaRoot.GetAttribute("name");
		_schemaUri = string.Empty;
		if (_schemaName == null || _schemaName.Length == 0)
		{
			_schemaName = "NewDataSet";
		}
		ds.Namespace = _schemaUri;
		for (XmlNode xmlNode = schemaRoot.FirstChild; xmlNode != null; xmlNode = xmlNode.NextSibling)
		{
			if (xmlNode is XmlElement)
			{
				XmlElement node = (XmlElement)xmlNode;
				if (XMLSchema.FEqualIdentity(node, "ElementType", "urn:schemas-microsoft-com:xml-data"))
				{
					HandleTable(node);
				}
			}
		}
		_schemaName = XmlConvert.DecodeName(_schemaName);
		if (ds.Tables[_schemaName] == null)
		{
			ds.DataSetName = _schemaName;
		}
	}

	internal XmlElement FindTypeNode(XmlElement node)
	{
		if (XMLSchema.FEqualIdentity(node, "ElementType", "urn:schemas-microsoft-com:xml-data"))
		{
			return node;
		}
		string attribute = node.GetAttribute("type");
		if (XMLSchema.FEqualIdentity(node, "element", "urn:schemas-microsoft-com:xml-data") || XMLSchema.FEqualIdentity(node, "attribute", "urn:schemas-microsoft-com:xml-data"))
		{
			if (attribute == null || attribute.Length == 0)
			{
				return null;
			}
			XmlNode xmlNode = node.OwnerDocument.FirstChild;
			XmlNode ownerDocument = node.OwnerDocument;
			while (xmlNode != ownerDocument)
			{
				if (((XMLSchema.FEqualIdentity(xmlNode, "ElementType", "urn:schemas-microsoft-com:xml-data") && XMLSchema.FEqualIdentity(node, "element", "urn:schemas-microsoft-com:xml-data")) || (XMLSchema.FEqualIdentity(xmlNode, "AttributeType", "urn:schemas-microsoft-com:xml-data") && XMLSchema.FEqualIdentity(node, "attribute", "urn:schemas-microsoft-com:xml-data"))) && xmlNode is XmlElement && ((XmlElement)xmlNode).GetAttribute("name") == attribute)
				{
					return (XmlElement)xmlNode;
				}
				if (xmlNode.FirstChild != null)
				{
					xmlNode = xmlNode.FirstChild;
					continue;
				}
				if (xmlNode.NextSibling != null)
				{
					xmlNode = xmlNode.NextSibling;
					continue;
				}
				while (xmlNode != ownerDocument)
				{
					xmlNode = xmlNode.ParentNode;
					if (xmlNode.NextSibling != null)
					{
						xmlNode = xmlNode.NextSibling;
						break;
					}
				}
			}
			return null;
		}
		return null;
	}

	internal bool IsTextOnlyContent(XmlElement node)
	{
		string attribute = node.GetAttribute("content");
		if (attribute == null || attribute.Length == 0)
		{
			string attribute2 = node.GetAttribute("type", "urn:schemas-microsoft-com:datatypes");
			return !string.IsNullOrEmpty(attribute2);
		}
		switch (attribute)
		{
		case "empty":
		case "eltOnly":
		case "elementOnly":
		case "mixed":
			return false;
		case "textOnly":
			return true;
		default:
			throw ExceptionBuilder.InvalidAttributeValue("content", attribute);
		}
	}

	internal bool IsXDRField(XmlElement node, XmlElement typeNode)
	{
		int minOccurs = 1;
		int maxOccurs = 1;
		if (!IsTextOnlyContent(typeNode))
		{
			return false;
		}
		for (XmlNode xmlNode = typeNode.FirstChild; xmlNode != null; xmlNode = xmlNode.NextSibling)
		{
			if (XMLSchema.FEqualIdentity(xmlNode, "element", "urn:schemas-microsoft-com:xml-data") || XMLSchema.FEqualIdentity(xmlNode, "attribute", "urn:schemas-microsoft-com:xml-data"))
			{
				return false;
			}
		}
		if (XMLSchema.FEqualIdentity(node, "element", "urn:schemas-microsoft-com:xml-data"))
		{
			GetMinMax(node, ref minOccurs, ref maxOccurs);
			if (maxOccurs == -1 || maxOccurs > 1)
			{
				return false;
			}
		}
		return true;
	}

	[RequiresUnreferencedCode("Members from serialized types may be trimmed if not referenced directly.")]
	internal DataTable HandleTable(XmlElement node)
	{
		XmlElement xmlElement = FindTypeNode(node);
		string attribute = node.GetAttribute("minOccurs");
		if (attribute != null && attribute.Length > 0 && Convert.ToInt32(attribute, CultureInfo.InvariantCulture) > 1 && xmlElement == null)
		{
			return InstantiateSimpleTable(_ds, node);
		}
		attribute = node.GetAttribute("maxOccurs");
		if (attribute != null && attribute.Length > 0 && !string.Equals(attribute, "1", StringComparison.Ordinal) && xmlElement == null)
		{
			return InstantiateSimpleTable(_ds, node);
		}
		if (xmlElement == null)
		{
			return null;
		}
		if (IsXDRField(node, xmlElement))
		{
			return null;
		}
		return InstantiateTable(_ds, node, xmlElement);
	}

	private static NameType FindNameType(string name)
	{
		int num = Array.BinarySearch(s_mapNameTypeXdr, name);
		if (num < 0)
		{
			throw ExceptionBuilder.UndefinedDatatype(name);
		}
		return s_mapNameTypeXdr[num];
	}

	[return: DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)]
	private Type ParseDataType(string dt, string dtValues)
	{
		string name = dt;
		string[] array = dt.Split(':');
		if (array.Length > 2)
		{
			throw ExceptionBuilder.InvalidAttributeValue("type", dt);
		}
		if (array.Length == 2)
		{
			name = array[1];
		}
		NameType nameType = FindNameType(name);
		if (nameType == s_enumerationNameType && (dtValues == null || dtValues.Length == 0))
		{
			throw ExceptionBuilder.MissingAttribute("type", "values");
		}
		return nameType.type;
	}

	internal string GetInstanceName(XmlElement node)
	{
		string attribute;
		if (XMLSchema.FEqualIdentity(node, "ElementType", "urn:schemas-microsoft-com:xml-data") || XMLSchema.FEqualIdentity(node, "AttributeType", "urn:schemas-microsoft-com:xml-data"))
		{
			attribute = node.GetAttribute("name");
			if (attribute == null || attribute.Length == 0)
			{
				throw ExceptionBuilder.MissingAttribute("Element", "name");
			}
		}
		else
		{
			attribute = node.GetAttribute("type");
			if (attribute == null || attribute.Length == 0)
			{
				throw ExceptionBuilder.MissingAttribute("Element", "type");
			}
		}
		return attribute;
	}

	[RequiresUnreferencedCode("Members from serialized types may be trimmed if not referenced directly.")]
	internal void HandleColumn(XmlElement node, DataTable table)
	{
		int minOccurs = 0;
		int maxOccurs = 1;
		string name;
		DataColumn dataColumn;
		if (node.Attributes.Count > 0)
		{
			string attribute = node.GetAttribute("ref");
			if (attribute != null && attribute.Length > 0)
			{
				return;
			}
			string text = (name = GetInstanceName(node));
			dataColumn = table.Columns[name, _schemaUri];
			if (dataColumn != null)
			{
				if (dataColumn.ColumnMapping == MappingType.Attribute)
				{
					if (XMLSchema.FEqualIdentity(node, "attribute", "urn:schemas-microsoft-com:xml-data"))
					{
						throw ExceptionBuilder.DuplicateDeclaration(text);
					}
				}
				else if (XMLSchema.FEqualIdentity(node, "element", "urn:schemas-microsoft-com:xml-data"))
				{
					throw ExceptionBuilder.DuplicateDeclaration(text);
				}
				name = XMLSchema.GenUniqueColumnName(text, table);
			}
		}
		else
		{
			name = string.Empty;
		}
		XmlElement xmlElement = FindTypeNode(node);
		SimpleType simpleType = null;
		string attribute2;
		if (xmlElement == null)
		{
			attribute2 = node.GetAttribute("type");
			throw ExceptionBuilder.UndefinedDatatype(attribute2);
		}
		attribute2 = xmlElement.GetAttribute("type", "urn:schemas-microsoft-com:datatypes");
		string attribute3 = xmlElement.GetAttribute("values", "urn:schemas-microsoft-com:datatypes");
		Type type;
		if (attribute2 == null || attribute2.Length == 0)
		{
			attribute2 = string.Empty;
			type = typeof(string);
		}
		else
		{
			type = ParseDataType(attribute2, attribute3);
			if (attribute2 == "float")
			{
				attribute2 = string.Empty;
			}
			if (attribute2 == "char")
			{
				attribute2 = string.Empty;
				simpleType = SimpleType.CreateSimpleType(StorageType.Char, type);
			}
			if (attribute2 == "enumeration")
			{
				attribute2 = string.Empty;
				simpleType = SimpleType.CreateEnumeratedType(attribute3);
			}
			if (attribute2 == "bin.base64")
			{
				attribute2 = string.Empty;
				simpleType = SimpleType.CreateByteArrayType("base64");
			}
			if (attribute2 == "bin.hex")
			{
				attribute2 = string.Empty;
				simpleType = SimpleType.CreateByteArrayType("hex");
			}
		}
		bool flag = XMLSchema.FEqualIdentity(node, "attribute", "urn:schemas-microsoft-com:xml-data");
		GetMinMax(node, flag, ref minOccurs, ref maxOccurs);
		string text2 = null;
		text2 = node.GetAttribute("default");
		bool flag2 = false;
		dataColumn = new DataColumn(XmlConvert.DecodeName(name), type, null, (!flag) ? MappingType.Element : MappingType.Attribute);
		XMLSchema.SetProperties(dataColumn, node.Attributes);
		dataColumn.XmlDataType = attribute2;
		dataColumn.SimpleType = simpleType;
		dataColumn.AllowDBNull = minOccurs == 0 || flag2;
		dataColumn.Namespace = (flag ? string.Empty : _schemaUri);
		if (node.Attributes != null)
		{
			for (int i = 0; i < node.Attributes.Count; i++)
			{
				if (node.Attributes[i].NamespaceURI == "urn:schemas-microsoft-com:xml-msdata" && node.Attributes[i].LocalName == "Expression")
				{
					dataColumn.Expression = node.Attributes[i].Value;
					break;
				}
			}
		}
		string attribute4 = node.GetAttribute("targetNamespace");
		if (attribute4 != null && attribute4.Length > 0)
		{
			dataColumn.Namespace = attribute4;
		}
		table.Columns.Add(dataColumn);
		if (text2 != null && text2.Length != 0)
		{
			try
			{
				dataColumn.DefaultValue = SqlConvert.ChangeTypeForXML(text2, type);
			}
			catch (FormatException)
			{
				throw ExceptionBuilder.CannotConvert(text2, type.FullName);
			}
		}
	}

	internal void GetMinMax(XmlElement elNode, ref int minOccurs, ref int maxOccurs)
	{
		GetMinMax(elNode, isAttribute: false, ref minOccurs, ref maxOccurs);
	}

	internal void GetMinMax(XmlElement elNode, bool isAttribute, ref int minOccurs, ref int maxOccurs)
	{
		string attribute = elNode.GetAttribute("minOccurs");
		if (attribute != null && attribute.Length > 0)
		{
			try
			{
				minOccurs = int.Parse(attribute, CultureInfo.InvariantCulture);
			}
			catch (Exception e) when (ADP.IsCatchableExceptionType(e))
			{
				throw ExceptionBuilder.AttributeValues("minOccurs", "0", "1");
			}
		}
		attribute = elNode.GetAttribute("maxOccurs");
		if (attribute == null || attribute.Length <= 0)
		{
			return;
		}
		if (string.Compare(attribute, "*", StringComparison.Ordinal) == 0)
		{
			maxOccurs = -1;
			return;
		}
		try
		{
			maxOccurs = int.Parse(attribute, CultureInfo.InvariantCulture);
		}
		catch (Exception e2) when (ADP.IsCatchableExceptionType(e2))
		{
			throw ExceptionBuilder.AttributeValues("maxOccurs", "1", "*");
		}
		if (maxOccurs == 1)
		{
			return;
		}
		throw ExceptionBuilder.AttributeValues("maxOccurs", "1", "*");
	}

	[RequiresUnreferencedCode("Members from serialized types may be trimmed if not referenced directly.")]
	internal void HandleTypeNode(XmlElement typeNode, DataTable table, ArrayList tableChildren)
	{
		for (XmlNode xmlNode = typeNode.FirstChild; xmlNode != null; xmlNode = xmlNode.NextSibling)
		{
			if (!(xmlNode is XmlElement))
			{
				continue;
			}
			if (XMLSchema.FEqualIdentity(xmlNode, "element", "urn:schemas-microsoft-com:xml-data"))
			{
				DataTable dataTable = HandleTable((XmlElement)xmlNode);
				if (dataTable != null)
				{
					tableChildren.Add(dataTable);
					continue;
				}
			}
			if (XMLSchema.FEqualIdentity(xmlNode, "attribute", "urn:schemas-microsoft-com:xml-data") || XMLSchema.FEqualIdentity(xmlNode, "element", "urn:schemas-microsoft-com:xml-data"))
			{
				HandleColumn((XmlElement)xmlNode, table);
			}
		}
	}

	[RequiresUnreferencedCode("Members from serialized types may be trimmed if not referenced directly.")]
	internal DataTable InstantiateTable(DataSet dataSet, XmlElement node, XmlElement typeNode)
	{
		string name = string.Empty;
		XmlAttributeCollection attributes = node.Attributes;
		int minOccurs = 1;
		int maxOccurs = 1;
		string text = null;
		ArrayList arrayList = new ArrayList();
		DataTable table;
		if (attributes.Count > 0)
		{
			name = GetInstanceName(node);
			table = dataSet.Tables.GetTable(name, _schemaUri);
			if (table != null)
			{
				return table;
			}
		}
		table = new DataTable(XmlConvert.DecodeName(name));
		table.Namespace = _schemaUri;
		GetMinMax(node, ref minOccurs, ref maxOccurs);
		table.MinOccurs = minOccurs;
		table.MaxOccurs = maxOccurs;
		_ds.Tables.Add(table);
		HandleTypeNode(typeNode, table, arrayList);
		XMLSchema.SetProperties(table, attributes);
		if (text != null)
		{
			string[] array = text.TrimEnd(null).Split((char[]?)null);
			int num = array.Length;
			DataColumn[] array2 = new DataColumn[num];
			for (int i = 0; i < num; i++)
			{
				DataColumn dataColumn = table.Columns[array[i], _schemaUri];
				if (dataColumn == null)
				{
					throw ExceptionBuilder.ElementTypeNotFound(array[i]);
				}
				array2[i] = dataColumn;
			}
			table.PrimaryKey = array2;
		}
		foreach (DataTable item in arrayList)
		{
			DataRelation dataRelation = null;
			DataRelationCollection childRelations = table.ChildRelations;
			for (int j = 0; j < childRelations.Count; j++)
			{
				if (childRelations[j].Nested && item == childRelations[j].ChildTable)
				{
					dataRelation = childRelations[j];
				}
			}
			if (dataRelation == null)
			{
				DataColumn dataColumn2 = table.AddUniqueKey();
				DataColumn childColumn = item.AddForeignKey(dataColumn2);
				dataRelation = new DataRelation(table.TableName + "_" + item.TableName, dataColumn2, childColumn, createConstraints: true);
				dataRelation.CheckMultipleNested = false;
				dataRelation.Nested = true;
				item.DataSet.Relations.Add(dataRelation);
				dataRelation.CheckMultipleNested = true;
			}
		}
		return table;
	}

	[RequiresUnreferencedCode("Members from serialized types may be trimmed if not referenced directly.")]
	internal DataTable InstantiateSimpleTable(DataSet dataSet, XmlElement node)
	{
		XmlAttributeCollection attributes = node.Attributes;
		int minOccurs = 1;
		int maxOccurs = 1;
		string instanceName = GetInstanceName(node);
		DataTable table = dataSet.Tables.GetTable(instanceName, _schemaUri);
		if (table != null)
		{
			throw ExceptionBuilder.DuplicateDeclaration(instanceName);
		}
		string text = XmlConvert.DecodeName(instanceName);
		table = new DataTable(text);
		table.Namespace = _schemaUri;
		GetMinMax(node, ref minOccurs, ref maxOccurs);
		table.MinOccurs = minOccurs;
		table.MaxOccurs = maxOccurs;
		XMLSchema.SetProperties(table, attributes);
		table._repeatableElement = true;
		HandleColumn(node, table);
		table.Columns[0].ColumnName = text + "_Column";
		_ds.Tables.Add(table);
		return table;
	}
}
