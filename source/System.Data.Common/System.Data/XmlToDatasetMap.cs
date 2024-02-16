using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Xml;

namespace System.Data;

internal sealed class XmlToDatasetMap
{
	private sealed class XmlNodeIdentety
	{
		public string LocalName;

		public string NamespaceURI;

		public XmlNodeIdentety(string localName, string namespaceURI)
		{
			LocalName = localName;
			NamespaceURI = namespaceURI;
		}

		public override int GetHashCode()
		{
			return LocalName.GetHashCode();
		}

		public override bool Equals([NotNullWhen(true)] object obj)
		{
			XmlNodeIdentety xmlNodeIdentety = (XmlNodeIdentety)obj;
			if (string.Equals(LocalName, xmlNodeIdentety.LocalName, StringComparison.OrdinalIgnoreCase))
			{
				return string.Equals(NamespaceURI, xmlNodeIdentety.NamespaceURI, StringComparison.OrdinalIgnoreCase);
			}
			return false;
		}
	}

	internal sealed class XmlNodeIdHashtable : Hashtable
	{
		private readonly XmlNodeIdentety _id = new XmlNodeIdentety(string.Empty, string.Empty);

		public object this[XmlNode node]
		{
			get
			{
				_id.LocalName = node.LocalName;
				_id.NamespaceURI = node.NamespaceURI;
				return this[_id];
			}
		}

		public object this[XmlReader dataReader]
		{
			get
			{
				_id.LocalName = dataReader.LocalName;
				_id.NamespaceURI = dataReader.NamespaceURI;
				return this[_id];
			}
		}

		public object this[DataTable table]
		{
			get
			{
				_id.LocalName = table.EncodedTableName;
				_id.NamespaceURI = table.Namespace;
				return this[_id];
			}
		}

		public object this[string name]
		{
			get
			{
				_id.LocalName = name;
				_id.NamespaceURI = string.Empty;
				return this[_id];
			}
		}

		public XmlNodeIdHashtable(int capacity)
			: base(capacity)
		{
		}
	}

	private sealed class TableSchemaInfo
	{
		public DataTable TableSchema;

		public XmlNodeIdHashtable ColumnsSchemaMap;

		public TableSchemaInfo(DataTable tableSchema)
		{
			TableSchema = tableSchema;
			ColumnsSchemaMap = new XmlNodeIdHashtable(tableSchema.Columns.Count);
		}
	}

	private XmlNodeIdHashtable _tableSchemaMap;

	private TableSchemaInfo _lastTableSchemaInfo;

	public XmlToDatasetMap(DataSet dataSet, XmlNameTable nameTable)
	{
		BuildIdentityMap(dataSet, nameTable);
	}

	public XmlToDatasetMap(XmlNameTable nameTable, DataSet dataSet)
	{
		BuildIdentityMap(nameTable, dataSet);
	}

	public XmlToDatasetMap(DataTable dataTable, XmlNameTable nameTable)
	{
		BuildIdentityMap(dataTable, nameTable);
	}

	public XmlToDatasetMap(XmlNameTable nameTable, DataTable dataTable)
	{
		BuildIdentityMap(nameTable, dataTable);
	}

	internal static bool IsMappedColumn(DataColumn c)
	{
		return c.ColumnMapping != MappingType.Hidden;
	}

	private TableSchemaInfo AddTableSchema(DataTable table, XmlNameTable nameTable)
	{
		string text = nameTable.Get(table.EncodedTableName);
		string namespaceURI = nameTable.Get(table.Namespace);
		if (text == null)
		{
			return null;
		}
		TableSchemaInfo tableSchemaInfo = new TableSchemaInfo(table);
		_tableSchemaMap[new XmlNodeIdentety(text, namespaceURI)] = tableSchemaInfo;
		return tableSchemaInfo;
	}

	private TableSchemaInfo AddTableSchema(XmlNameTable nameTable, DataTable table)
	{
		string encodedTableName = table.EncodedTableName;
		string text = nameTable.Get(encodedTableName);
		if (text == null)
		{
			text = nameTable.Add(encodedTableName);
		}
		table._encodedTableName = text;
		string text2 = nameTable.Get(table.Namespace);
		if (text2 == null)
		{
			text2 = nameTable.Add(table.Namespace);
		}
		else if (table._tableNamespace != null)
		{
			table._tableNamespace = text2;
		}
		TableSchemaInfo tableSchemaInfo = new TableSchemaInfo(table);
		_tableSchemaMap[new XmlNodeIdentety(text, text2)] = tableSchemaInfo;
		return tableSchemaInfo;
	}

	private bool AddColumnSchema(DataColumn col, XmlNameTable nameTable, XmlNodeIdHashtable columns)
	{
		string text = nameTable.Get(col.EncodedColumnName);
		string namespaceURI = nameTable.Get(col.Namespace);
		if (text == null)
		{
			return false;
		}
		XmlNodeIdentety key = new XmlNodeIdentety(text, namespaceURI);
		columns[key] = col;
		if (col.ColumnName.StartsWith("xml", StringComparison.OrdinalIgnoreCase))
		{
			HandleSpecialColumn(col, nameTable, columns);
		}
		return true;
	}

	private bool AddColumnSchema(XmlNameTable nameTable, DataColumn col, XmlNodeIdHashtable columns)
	{
		string array = XmlConvert.EncodeLocalName(col.ColumnName);
		string text = nameTable.Get(array);
		if (text == null)
		{
			text = nameTable.Add(array);
		}
		col._encodedColumnName = text;
		string text2 = nameTable.Get(col.Namespace);
		if (text2 == null)
		{
			text2 = nameTable.Add(col.Namespace);
		}
		else if (col._columnUri != null)
		{
			col._columnUri = text2;
		}
		XmlNodeIdentety key = new XmlNodeIdentety(text, text2);
		columns[key] = col;
		if (col.ColumnName.StartsWith("xml", StringComparison.OrdinalIgnoreCase))
		{
			HandleSpecialColumn(col, nameTable, columns);
		}
		return true;
	}

	[MemberNotNull("_tableSchemaMap")]
	private void BuildIdentityMap(DataSet dataSet, XmlNameTable nameTable)
	{
		_tableSchemaMap = new XmlNodeIdHashtable(dataSet.Tables.Count);
		foreach (DataTable table in dataSet.Tables)
		{
			TableSchemaInfo tableSchemaInfo = AddTableSchema(table, nameTable);
			if (tableSchemaInfo == null)
			{
				continue;
			}
			foreach (DataColumn column in table.Columns)
			{
				if (IsMappedColumn(column))
				{
					AddColumnSchema(column, nameTable, tableSchemaInfo.ColumnsSchemaMap);
				}
			}
		}
	}

	[MemberNotNull("_tableSchemaMap")]
	private void BuildIdentityMap(XmlNameTable nameTable, DataSet dataSet)
	{
		_tableSchemaMap = new XmlNodeIdHashtable(dataSet.Tables.Count);
		string text = nameTable.Get(dataSet.Namespace);
		if (text == null)
		{
			text = nameTable.Add(dataSet.Namespace);
		}
		dataSet._namespaceURI = text;
		foreach (DataTable table in dataSet.Tables)
		{
			TableSchemaInfo tableSchemaInfo = AddTableSchema(nameTable, table);
			if (tableSchemaInfo == null)
			{
				continue;
			}
			foreach (DataColumn column in table.Columns)
			{
				if (IsMappedColumn(column))
				{
					AddColumnSchema(nameTable, column, tableSchemaInfo.ColumnsSchemaMap);
				}
			}
			foreach (DataRelation childRelation in table.ChildRelations)
			{
				if (childRelation.Nested)
				{
					string array = XmlConvert.EncodeLocalName(childRelation.ChildTable.TableName);
					string text2 = nameTable.Get(array);
					if (text2 == null)
					{
						text2 = nameTable.Add(array);
					}
					string text3 = nameTable.Get(childRelation.ChildTable.Namespace);
					if (text3 == null)
					{
						text3 = nameTable.Add(childRelation.ChildTable.Namespace);
					}
					XmlNodeIdentety key = new XmlNodeIdentety(text2, text3);
					tableSchemaInfo.ColumnsSchemaMap[key] = childRelation.ChildTable;
				}
			}
		}
	}

	[MemberNotNull("_tableSchemaMap")]
	private void BuildIdentityMap(DataTable dataTable, XmlNameTable nameTable)
	{
		_tableSchemaMap = new XmlNodeIdHashtable(1);
		TableSchemaInfo tableSchemaInfo = AddTableSchema(dataTable, nameTable);
		if (tableSchemaInfo == null)
		{
			return;
		}
		foreach (DataColumn column in dataTable.Columns)
		{
			if (IsMappedColumn(column))
			{
				AddColumnSchema(column, nameTable, tableSchemaInfo.ColumnsSchemaMap);
			}
		}
	}

	[MemberNotNull("_tableSchemaMap")]
	private void BuildIdentityMap(XmlNameTable nameTable, DataTable dataTable)
	{
		ArrayList selfAndDescendants = GetSelfAndDescendants(dataTable);
		_tableSchemaMap = new XmlNodeIdHashtable(selfAndDescendants.Count);
		foreach (DataTable item in selfAndDescendants)
		{
			TableSchemaInfo tableSchemaInfo = AddTableSchema(nameTable, item);
			if (tableSchemaInfo == null)
			{
				continue;
			}
			foreach (DataColumn column in item.Columns)
			{
				if (IsMappedColumn(column))
				{
					AddColumnSchema(nameTable, column, tableSchemaInfo.ColumnsSchemaMap);
				}
			}
			foreach (DataRelation childRelation in item.ChildRelations)
			{
				if (childRelation.Nested)
				{
					string array = XmlConvert.EncodeLocalName(childRelation.ChildTable.TableName);
					string text = nameTable.Get(array);
					if (text == null)
					{
						text = nameTable.Add(array);
					}
					string text2 = nameTable.Get(childRelation.ChildTable.Namespace);
					if (text2 == null)
					{
						text2 = nameTable.Add(childRelation.ChildTable.Namespace);
					}
					XmlNodeIdentety key = new XmlNodeIdentety(text, text2);
					tableSchemaInfo.ColumnsSchemaMap[key] = childRelation.ChildTable;
				}
			}
		}
	}

	private ArrayList GetSelfAndDescendants(DataTable dt)
	{
		ArrayList arrayList = new ArrayList();
		arrayList.Add(dt);
		for (int i = 0; i < arrayList.Count; i++)
		{
			foreach (DataRelation childRelation in ((DataTable)arrayList[i]).ChildRelations)
			{
				if (!arrayList.Contains(childRelation.ChildTable))
				{
					arrayList.Add(childRelation.ChildTable);
				}
			}
		}
		return arrayList;
	}

	public object GetColumnSchema(XmlNode node, bool fIgnoreNamespace)
	{
		TableSchemaInfo tableSchemaInfo = null;
		XmlNode xmlNode = ((node.NodeType == XmlNodeType.Attribute) ? ((XmlAttribute)node).OwnerElement : node.ParentNode);
		do
		{
			if (xmlNode == null || xmlNode.NodeType != XmlNodeType.Element)
			{
				return null;
			}
			tableSchemaInfo = (TableSchemaInfo)(fIgnoreNamespace ? _tableSchemaMap[xmlNode.LocalName] : _tableSchemaMap[xmlNode]);
			xmlNode = xmlNode.ParentNode;
		}
		while (tableSchemaInfo == null);
		if (fIgnoreNamespace)
		{
			return tableSchemaInfo.ColumnsSchemaMap[node.LocalName];
		}
		return tableSchemaInfo.ColumnsSchemaMap[node];
	}

	public object GetColumnSchema(DataTable table, XmlReader dataReader, bool fIgnoreNamespace)
	{
		if (_lastTableSchemaInfo == null || _lastTableSchemaInfo.TableSchema != table)
		{
			_lastTableSchemaInfo = (TableSchemaInfo)(fIgnoreNamespace ? _tableSchemaMap[table.EncodedTableName] : _tableSchemaMap[table]);
		}
		if (fIgnoreNamespace)
		{
			return _lastTableSchemaInfo.ColumnsSchemaMap[dataReader.LocalName];
		}
		return _lastTableSchemaInfo.ColumnsSchemaMap[dataReader];
	}

	public object GetSchemaForNode(XmlNode node, bool fIgnoreNamespace)
	{
		TableSchemaInfo tableSchemaInfo = null;
		if (node.NodeType == XmlNodeType.Element)
		{
			tableSchemaInfo = (TableSchemaInfo)(fIgnoreNamespace ? _tableSchemaMap[node.LocalName] : _tableSchemaMap[node]);
		}
		if (tableSchemaInfo != null)
		{
			return tableSchemaInfo.TableSchema;
		}
		return GetColumnSchema(node, fIgnoreNamespace);
	}

	public DataTable GetTableForNode(XmlReader node, bool fIgnoreNamespace)
	{
		TableSchemaInfo tableSchemaInfo = (TableSchemaInfo)(fIgnoreNamespace ? _tableSchemaMap[node.LocalName] : _tableSchemaMap[node]);
		if (tableSchemaInfo != null)
		{
			_lastTableSchemaInfo = tableSchemaInfo;
			return _lastTableSchemaInfo.TableSchema;
		}
		return null;
	}

	private void HandleSpecialColumn(DataColumn col, XmlNameTable nameTable, XmlNodeIdHashtable columns)
	{
		string text = (('x' != col.ColumnName[0]) ? "_x0058_" : "_x0078_");
		text += col.ColumnName.AsSpan(1);
		if (nameTable.Get(text) == null)
		{
			nameTable.Add(text);
		}
		string namespaceURI = nameTable.Get(col.Namespace);
		XmlNodeIdentety key = new XmlNodeIdentety(text, namespaceURI);
		columns[key] = col;
	}
}
