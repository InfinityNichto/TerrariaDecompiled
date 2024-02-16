using System.Collections;
using System.Data.Common;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Xml;
using System.Xml.Serialization;

namespace System.Data;

internal sealed class XmlDataTreeWriter
{
	private XmlWriter _xmlw;

	private readonly DataSet _ds;

	private readonly DataTable _dt;

	private readonly ArrayList _dTables = new ArrayList();

	private readonly DataTable[] _topLevelTables;

	private readonly bool _fFromTable;

	private bool _isDiffgram;

	private Hashtable _rowsOrder;

	private readonly bool _writeHierarchy;

	internal XmlDataTreeWriter(DataSet ds)
	{
		_ds = ds;
		_topLevelTables = ds.TopLevelTables();
		foreach (DataTable table in ds.Tables)
		{
			_dTables.Add(table);
		}
	}

	internal XmlDataTreeWriter(DataTable dt, bool writeHierarchy)
	{
		_dt = dt;
		_fFromTable = true;
		if (dt.DataSet == null)
		{
			_dTables.Add(dt);
			_topLevelTables = new DataTable[1] { dt };
			return;
		}
		_ds = dt.DataSet;
		_dTables.Add(dt);
		if (writeHierarchy)
		{
			_writeHierarchy = true;
			CreateTablesHierarchy(dt);
			_topLevelTables = CreateToplevelTables();
		}
		else
		{
			_topLevelTables = new DataTable[1] { dt };
		}
	}

	private DataTable[] CreateToplevelTables()
	{
		ArrayList arrayList = new ArrayList();
		for (int i = 0; i < _dTables.Count; i++)
		{
			DataTable dataTable = (DataTable)_dTables[i];
			if (dataTable.ParentRelations.Count == 0)
			{
				arrayList.Add(dataTable);
				continue;
			}
			bool flag = false;
			for (int j = 0; j < dataTable.ParentRelations.Count; j++)
			{
				if (dataTable.ParentRelations[j].Nested)
				{
					if (dataTable.ParentRelations[j].ParentTable == dataTable)
					{
						flag = false;
						break;
					}
					flag = true;
				}
			}
			if (!flag)
			{
				arrayList.Add(dataTable);
			}
		}
		if (arrayList.Count == 0)
		{
			return Array.Empty<DataTable>();
		}
		DataTable[] array = new DataTable[arrayList.Count];
		arrayList.CopyTo(array, 0);
		return array;
	}

	private void CreateTablesHierarchy(DataTable dt)
	{
		foreach (DataRelation childRelation in dt.ChildRelations)
		{
			if (!_dTables.Contains(childRelation.ChildTable))
			{
				_dTables.Add(childRelation.ChildTable);
				CreateTablesHierarchy(childRelation.ChildTable);
			}
		}
	}

	internal static bool RowHasErrors(DataRow row)
	{
		int count = row.Table.Columns.Count;
		if (row.HasErrors && row.RowError.Length > 0)
		{
			return true;
		}
		for (int i = 0; i < count; i++)
		{
			DataColumn column = row.Table.Columns[i];
			string columnError = row.GetColumnError(column);
			if (columnError != null && columnError.Length != 0)
			{
				return true;
			}
		}
		return false;
	}

	[RequiresUnreferencedCode("Members from serialized types may be trimmed if not referenced directly.")]
	internal void SaveDiffgramData(XmlWriter xw, Hashtable rowsOrder)
	{
		_xmlw = DataTextWriter.CreateWriter(xw);
		_isDiffgram = true;
		_rowsOrder = rowsOrder;
		string prefix = ((_ds == null) ? ((_dt.Namespace.Length == 0) ? "" : _dt.Prefix) : ((_ds.Namespace.Length == 0) ? "" : _ds.Prefix));
		if (_ds == null || _ds.DataSetName == null || _ds.DataSetName.Length == 0)
		{
			_xmlw.WriteStartElement(prefix, "DocumentElement", (_dt.Namespace == null) ? "" : _dt.Namespace);
		}
		else
		{
			_xmlw.WriteStartElement(prefix, XmlConvert.EncodeLocalName(_ds.DataSetName), _ds.Namespace);
		}
		for (int i = 0; i < _dTables.Count; i++)
		{
			DataTable dataTable = (DataTable)_dTables[i];
			foreach (DataRow row in dataTable.Rows)
			{
				if (row.RowState != DataRowState.Deleted)
				{
					int nestedParentCount = row.GetNestedParentCount();
					if (nestedParentCount == 0)
					{
						DataTable dataTable2 = (DataTable)_dTables[i];
						XmlDataRowWriter(row, dataTable2.EncodedTableName);
					}
					else if (nestedParentCount > 1)
					{
						throw ExceptionBuilder.MultipleParentRows((dataTable.Namespace.Length == 0) ? dataTable.TableName : (dataTable.Namespace + dataTable.TableName));
					}
				}
			}
		}
		_xmlw.WriteEndElement();
		_xmlw.Flush();
	}

	[RequiresUnreferencedCode("Members from serialized types may be trimmed if not referenced directly.")]
	internal void Save(XmlWriter xw, bool writeSchema)
	{
		_xmlw = DataTextWriter.CreateWriter(xw);
		int num = _topLevelTables.Length;
		bool flag = true;
		string prefix = ((_ds == null) ? ((_dt.Namespace.Length == 0) ? "" : _dt.Prefix) : ((_ds.Namespace.Length == 0) ? "" : _ds.Prefix));
		if (!writeSchema && _ds != null && _ds._fTopLevelTable && num == 1 && _ds.TopLevelTables()[0].Rows.Count == 1)
		{
			flag = false;
		}
		if (flag)
		{
			if (_ds == null)
			{
				_xmlw.WriteStartElement(prefix, "DocumentElement", _dt.Namespace);
			}
			else if (_ds.DataSetName == null || _ds.DataSetName.Length == 0)
			{
				_xmlw.WriteStartElement(prefix, "DocumentElement", _ds.Namespace);
			}
			else
			{
				_xmlw.WriteStartElement(prefix, XmlConvert.EncodeLocalName(_ds.DataSetName), _ds.Namespace);
			}
			for (int i = 0; i < _dTables.Count; i++)
			{
				if (((DataTable)_dTables[i])._xmlText != null)
				{
					_xmlw.WriteAttributeString("xmlns", "xsi", "http://www.w3.org/2000/xmlns/", "http://www.w3.org/2001/XMLSchema-instance");
					break;
				}
			}
			if (writeSchema)
			{
				if (!_fFromTable)
				{
					new XmlTreeGen(SchemaFormat.Public).Save(_ds, _xmlw);
				}
				else
				{
					new XmlTreeGen(SchemaFormat.Public).Save(null, _dt, _xmlw, _writeHierarchy);
				}
			}
		}
		for (int j = 0; j < _dTables.Count; j++)
		{
			foreach (DataRow row in ((DataTable)_dTables[j]).Rows)
			{
				if (row.RowState != DataRowState.Deleted)
				{
					int nestedParentCount = row.GetNestedParentCount();
					if (nestedParentCount == 0)
					{
						XmlDataRowWriter(row, ((DataTable)_dTables[j]).EncodedTableName);
					}
					else if (nestedParentCount > 1)
					{
						DataTable dataTable = (DataTable)_dTables[j];
						throw ExceptionBuilder.MultipleParentRows((dataTable.Namespace.Length == 0) ? dataTable.TableName : (dataTable.Namespace + dataTable.TableName));
					}
				}
			}
		}
		if (flag)
		{
			_xmlw.WriteEndElement();
		}
		_xmlw.Flush();
	}

	private ArrayList GetNestedChildRelations(DataRow row)
	{
		ArrayList arrayList = new ArrayList();
		foreach (DataRelation childRelation in row.Table.ChildRelations)
		{
			if (childRelation.Nested)
			{
				arrayList.Add(childRelation);
			}
		}
		return arrayList;
	}

	[RequiresUnreferencedCode("Members from serialized types may be trimmed if not referenced directly.")]
	internal void XmlDataRowWriter(DataRow row, string encodedTableName)
	{
		string prefix = ((row.Table.Namespace.Length == 0) ? "" : row.Table.Prefix);
		_xmlw.WriteStartElement(prefix, encodedTableName, row.Table.Namespace);
		if (_isDiffgram)
		{
			_xmlw.WriteAttributeString("diffgr", "id", "urn:schemas-microsoft-com:xml-diffgram-v1", row.Table.TableName + row.rowID.ToString(CultureInfo.InvariantCulture));
			_xmlw.WriteAttributeString("msdata", "rowOrder", "urn:schemas-microsoft-com:xml-msdata", _rowsOrder[row].ToString());
			if (row.RowState == DataRowState.Added)
			{
				_xmlw.WriteAttributeString("diffgr", "hasChanges", "urn:schemas-microsoft-com:xml-diffgram-v1", "inserted");
			}
			if (row.RowState == DataRowState.Modified)
			{
				_xmlw.WriteAttributeString("diffgr", "hasChanges", "urn:schemas-microsoft-com:xml-diffgram-v1", "modified");
			}
			if (RowHasErrors(row))
			{
				_xmlw.WriteAttributeString("diffgr", "hasErrors", "urn:schemas-microsoft-com:xml-diffgram-v1", "true");
			}
		}
		foreach (DataColumn column in row.Table.Columns)
		{
			if (column._columnMapping == MappingType.Attribute)
			{
				object obj = row[column];
				string prefix2 = ((column.Namespace.Length == 0) ? "" : column.Prefix);
				if (obj != DBNull.Value && (!column.ImplementsINullable || !DataStorage.IsObjectSqlNull(obj)))
				{
					XmlTreeGen.ValidateColumnMapping(column.DataType);
					_xmlw.WriteAttributeString(prefix2, column.EncodedColumnName, column.Namespace, column.ConvertObjectToXml(obj));
				}
			}
			if (_isDiffgram && column._columnMapping == MappingType.Hidden)
			{
				object obj = row[column];
				if (obj != DBNull.Value && (!column.ImplementsINullable || !DataStorage.IsObjectSqlNull(obj)))
				{
					XmlTreeGen.ValidateColumnMapping(column.DataType);
					_xmlw.WriteAttributeString("msdata", "hidden" + column.EncodedColumnName, "urn:schemas-microsoft-com:xml-msdata", column.ConvertObjectToXml(obj));
				}
			}
		}
		foreach (DataColumn column2 in row.Table.Columns)
		{
			if (column2._columnMapping == MappingType.Hidden)
			{
				continue;
			}
			object obj = row[column2];
			string prefix3 = ((column2.Namespace.Length == 0) ? "" : column2.Prefix);
			bool flag = true;
			if ((obj == DBNull.Value || (column2.ImplementsINullable && DataStorage.IsObjectSqlNull(obj))) && column2.ColumnMapping == MappingType.SimpleContent)
			{
				_xmlw.WriteAttributeString("xsi", "nil", "http://www.w3.org/2001/XMLSchema-instance", "true");
			}
			if (obj == DBNull.Value || (column2.ImplementsINullable && DataStorage.IsObjectSqlNull(obj)) || column2._columnMapping == MappingType.Attribute)
			{
				continue;
			}
			if (column2._columnMapping != MappingType.SimpleContent && (!column2.IsCustomType || !column2.IsValueCustomTypeInstance(obj) || typeof(IXmlSerializable).IsAssignableFrom(obj.GetType())))
			{
				_xmlw.WriteStartElement(prefix3, column2.EncodedColumnName, column2.Namespace);
				flag = false;
			}
			Type type = obj.GetType();
			if (!column2.IsCustomType)
			{
				if ((type == typeof(char) || type == typeof(string)) && PreserveSpace(obj))
				{
					_xmlw.WriteAttributeString("xml", "space", "http://www.w3.org/XML/1998/namespace", "preserve");
				}
				_xmlw.WriteString(column2.ConvertObjectToXml(obj));
			}
			else if (column2.IsValueCustomTypeInstance(obj))
			{
				if (!flag && type != column2.DataType)
				{
					_xmlw.WriteAttributeString("msdata", "InstanceType", "urn:schemas-microsoft-com:xml-msdata", DataStorage.GetQualifiedName(type));
				}
				if (!flag)
				{
					column2.ConvertObjectToXml(obj, _xmlw, null);
				}
				else
				{
					if (obj.GetType() != column2.DataType)
					{
						throw ExceptionBuilder.PolymorphismNotSupported(type.AssemblyQualifiedName);
					}
					XmlRootAttribute xmlRootAttribute = new XmlRootAttribute(column2.EncodedColumnName);
					xmlRootAttribute.Namespace = column2.Namespace;
					column2.ConvertObjectToXml(obj, _xmlw, xmlRootAttribute);
				}
			}
			else
			{
				if (type == typeof(Type) || type == typeof(Guid) || type == typeof(char) || DataStorage.IsSqlType(type))
				{
					_xmlw.WriteAttributeString("msdata", "InstanceType", "urn:schemas-microsoft-com:xml-msdata", type.FullName);
				}
				else if (obj is Type)
				{
					_xmlw.WriteAttributeString("msdata", "InstanceType", "urn:schemas-microsoft-com:xml-msdata", "Type");
				}
				else
				{
					string value = "xs:" + XmlTreeGen.XmlDataTypeName(type);
					_xmlw.WriteAttributeString("xsi", "type", "http://www.w3.org/2001/XMLSchema-instance", value);
					_xmlw.WriteAttributeString("xs", "xmlns", "http://www.w3.org/2001/XMLSchema", value);
				}
				if (!DataStorage.IsSqlType(type))
				{
					_xmlw.WriteString(column2.ConvertObjectToXml(obj));
				}
				else
				{
					column2.ConvertObjectToXml(obj, _xmlw, null);
				}
			}
			if (column2._columnMapping != MappingType.SimpleContent && !flag)
			{
				_xmlw.WriteEndElement();
			}
		}
		if (_ds != null)
		{
			foreach (DataRelation nestedChildRelation in GetNestedChildRelations(row))
			{
				DataRow[] childRows = row.GetChildRows(nestedChildRelation);
				foreach (DataRow row2 in childRows)
				{
					XmlDataRowWriter(row2, nestedChildRelation.ChildTable.EncodedTableName);
				}
			}
		}
		_xmlw.WriteEndElement();
	}

	internal static bool PreserveSpace(object value)
	{
		string text = value.ToString();
		if (text.Length == 0)
		{
			return false;
		}
		for (int i = 0; i < text.Length; i++)
		{
			if (!char.IsWhiteSpace(text, i))
			{
				return false;
			}
		}
		return true;
	}
}
