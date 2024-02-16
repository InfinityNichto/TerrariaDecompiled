using System.Collections;
using System.Data.Common;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Xml;
using System.Xml.Serialization;

namespace System.Data;

internal sealed class NewDiffgramGen
{
	internal XmlDocument _doc;

	internal DataSet _ds;

	internal DataTable _dt;

	internal XmlWriter _xmlw;

	private bool _fBefore;

	private bool _fErrors;

	internal Hashtable _rowsOrder;

	private readonly ArrayList _tables = new ArrayList();

	private readonly bool _writeHierarchy;

	internal NewDiffgramGen(DataSet ds)
	{
		_ds = ds;
		_dt = null;
		_doc = new XmlDocument();
		for (int i = 0; i < ds.Tables.Count; i++)
		{
			_tables.Add(ds.Tables[i]);
		}
		DoAssignments(_tables);
	}

	internal NewDiffgramGen(DataTable dt, bool writeHierarchy)
	{
		_ds = null;
		_dt = dt;
		_doc = new XmlDocument();
		_tables.Add(dt);
		if (writeHierarchy)
		{
			_writeHierarchy = true;
			CreateTableHierarchy(dt);
		}
		DoAssignments(_tables);
	}

	private void CreateTableHierarchy(DataTable dt)
	{
		foreach (DataRelation childRelation in dt.ChildRelations)
		{
			if (!_tables.Contains(childRelation.ChildTable))
			{
				_tables.Add(childRelation.ChildTable);
				CreateTableHierarchy(childRelation.ChildTable);
			}
		}
	}

	[MemberNotNull("_rowsOrder")]
	private void DoAssignments(ArrayList tables)
	{
		int num = 0;
		for (int i = 0; i < tables.Count; i++)
		{
			num += ((DataTable)tables[i]).Rows.Count;
		}
		_rowsOrder = new Hashtable(num);
		for (int j = 0; j < tables.Count; j++)
		{
			DataTable dataTable = (DataTable)tables[j];
			DataRowCollection rows = dataTable.Rows;
			num = rows.Count;
			for (int k = 0; k < num; k++)
			{
				_rowsOrder[rows[k]] = k;
			}
		}
	}

	private bool EmptyData()
	{
		for (int i = 0; i < _tables.Count; i++)
		{
			if (((DataTable)_tables[i]).Rows.Count > 0)
			{
				return false;
			}
		}
		return true;
	}

	[RequiresUnreferencedCode("Members from serialized types may be trimmed if not referenced directly.")]
	internal void Save(XmlWriter xmlw)
	{
		Save(xmlw, null);
	}

	[RequiresUnreferencedCode("Members from serialized types may be trimmed if not referenced directly.")]
	internal void Save(XmlWriter xmlw, DataTable table)
	{
		_xmlw = DataTextWriter.CreateWriter(xmlw);
		_xmlw.WriteStartElement("diffgr", "diffgram", "urn:schemas-microsoft-com:xml-diffgram-v1");
		_xmlw.WriteAttributeString("xmlns", "msdata", null, "urn:schemas-microsoft-com:xml-msdata");
		if (!EmptyData())
		{
			if (table != null)
			{
				new XmlDataTreeWriter(table, _writeHierarchy).SaveDiffgramData(_xmlw, _rowsOrder);
			}
			else
			{
				new XmlDataTreeWriter(_ds).SaveDiffgramData(_xmlw, _rowsOrder);
			}
			if (table == null)
			{
				for (int i = 0; i < _ds.Tables.Count; i++)
				{
					GenerateTable(_ds.Tables[i]);
				}
			}
			else
			{
				for (int j = 0; j < _tables.Count; j++)
				{
					GenerateTable((DataTable)_tables[j]);
				}
			}
			if (_fBefore)
			{
				_xmlw.WriteEndElement();
			}
			if (table == null)
			{
				for (int k = 0; k < _ds.Tables.Count; k++)
				{
					GenerateTableErrors(_ds.Tables[k]);
				}
			}
			else
			{
				for (int l = 0; l < _tables.Count; l++)
				{
					GenerateTableErrors((DataTable)_tables[l]);
				}
			}
			if (_fErrors)
			{
				_xmlw.WriteEndElement();
			}
		}
		_xmlw.WriteEndElement();
		_xmlw.Flush();
	}

	[RequiresUnreferencedCode("Members from serialized types may be trimmed if not referenced directly.")]
	private void GenerateTable(DataTable table)
	{
		int count = table.Rows.Count;
		if (count > 0)
		{
			for (int i = 0; i < count; i++)
			{
				GenerateRow(table.Rows[i]);
			}
		}
	}

	private void GenerateTableErrors(DataTable table)
	{
		int count = table.Rows.Count;
		int count2 = table.Columns.Count;
		if (count <= 0)
		{
			return;
		}
		for (int i = 0; i < count; i++)
		{
			bool flag = false;
			DataRow dataRow = table.Rows[i];
			string prefix = ((table.Namespace.Length != 0) ? table.Prefix : string.Empty);
			if (dataRow.HasErrors && dataRow.RowError.Length > 0)
			{
				if (!_fErrors)
				{
					_xmlw.WriteStartElement("diffgr", "errors", "urn:schemas-microsoft-com:xml-diffgram-v1");
					_fErrors = true;
				}
				_xmlw.WriteStartElement(prefix, dataRow.Table.EncodedTableName, dataRow.Table.Namespace);
				_xmlw.WriteAttributeString("diffgr", "id", "urn:schemas-microsoft-com:xml-diffgram-v1", dataRow.Table.TableName + dataRow.rowID.ToString(CultureInfo.InvariantCulture));
				_xmlw.WriteAttributeString("diffgr", "Error", "urn:schemas-microsoft-com:xml-diffgram-v1", dataRow.RowError);
				flag = true;
			}
			if (count2 <= 0)
			{
				continue;
			}
			for (int j = 0; j < count2; j++)
			{
				DataColumn dataColumn = table.Columns[j];
				string columnError = dataRow.GetColumnError(dataColumn);
				string prefix2 = ((dataColumn.Namespace.Length != 0) ? dataColumn.Prefix : string.Empty);
				if (columnError == null || columnError.Length == 0)
				{
					continue;
				}
				if (!flag)
				{
					if (!_fErrors)
					{
						_xmlw.WriteStartElement("diffgr", "errors", "urn:schemas-microsoft-com:xml-diffgram-v1");
						_fErrors = true;
					}
					_xmlw.WriteStartElement(prefix, dataRow.Table.EncodedTableName, dataRow.Table.Namespace);
					_xmlw.WriteAttributeString("diffgr", "id", "urn:schemas-microsoft-com:xml-diffgram-v1", dataRow.Table.TableName + dataRow.rowID.ToString(CultureInfo.InvariantCulture));
					flag = true;
				}
				_xmlw.WriteStartElement(prefix2, dataColumn.EncodedColumnName, dataColumn.Namespace);
				_xmlw.WriteAttributeString("diffgr", "Error", "urn:schemas-microsoft-com:xml-diffgram-v1", columnError);
				_xmlw.WriteEndElement();
			}
			if (flag)
			{
				_xmlw.WriteEndElement();
			}
		}
	}

	[RequiresUnreferencedCode("Members from serialized types may be trimmed if not referenced directly.")]
	private void GenerateRow(DataRow row)
	{
		DataRowState rowState = row.RowState;
		if (rowState == DataRowState.Unchanged || rowState == DataRowState.Added)
		{
			return;
		}
		if (!_fBefore)
		{
			_xmlw.WriteStartElement("diffgr", "before", "urn:schemas-microsoft-com:xml-diffgram-v1");
			_fBefore = true;
		}
		DataTable table = row.Table;
		int count = table.Columns.Count;
		string value = table.TableName + row.rowID.ToString(CultureInfo.InvariantCulture);
		string text = null;
		if (rowState == DataRowState.Deleted && row.Table.NestedParentRelations.Length != 0)
		{
			DataRow nestedParentRow = row.GetNestedParentRow(DataRowVersion.Original);
			if (nestedParentRow != null)
			{
				text = nestedParentRow.Table.TableName + nestedParentRow.rowID.ToString(CultureInfo.InvariantCulture);
			}
		}
		string prefix = ((table.Namespace.Length != 0) ? table.Prefix : string.Empty);
		_xmlw.WriteStartElement(prefix, row.Table.EncodedTableName, row.Table.Namespace);
		_xmlw.WriteAttributeString("diffgr", "id", "urn:schemas-microsoft-com:xml-diffgram-v1", value);
		if (rowState == DataRowState.Deleted && XmlDataTreeWriter.RowHasErrors(row))
		{
			_xmlw.WriteAttributeString("diffgr", "hasErrors", "urn:schemas-microsoft-com:xml-diffgram-v1", "true");
		}
		if (text != null)
		{
			_xmlw.WriteAttributeString("diffgr", "parentId", "urn:schemas-microsoft-com:xml-diffgram-v1", text);
		}
		_xmlw.WriteAttributeString("msdata", "rowOrder", "urn:schemas-microsoft-com:xml-msdata", _rowsOrder[row].ToString());
		for (int i = 0; i < count; i++)
		{
			if (row.Table.Columns[i].ColumnMapping == MappingType.Attribute || row.Table.Columns[i].ColumnMapping == MappingType.Hidden)
			{
				GenerateColumn(row, row.Table.Columns[i], DataRowVersion.Original);
			}
		}
		for (int j = 0; j < count; j++)
		{
			if (row.Table.Columns[j].ColumnMapping == MappingType.Element || row.Table.Columns[j].ColumnMapping == MappingType.SimpleContent)
			{
				GenerateColumn(row, row.Table.Columns[j], DataRowVersion.Original);
			}
		}
		_xmlw.WriteEndElement();
	}

	[RequiresUnreferencedCode("Members from serialized types may be trimmed if not referenced directly.")]
	private void GenerateColumn(DataRow row, DataColumn col, DataRowVersion version)
	{
		string text = null;
		text = col.GetColumnValueAsString(row, version);
		if (text == null)
		{
			if (col.ColumnMapping == MappingType.SimpleContent)
			{
				_xmlw.WriteAttributeString("xsi", "nil", "http://www.w3.org/2001/XMLSchema-instance", "true");
			}
			return;
		}
		string prefix = ((col.Namespace.Length != 0) ? col.Prefix : string.Empty);
		switch (col.ColumnMapping)
		{
		case MappingType.Attribute:
			_xmlw.WriteAttributeString(prefix, col.EncodedColumnName, col.Namespace, text);
			break;
		case MappingType.Hidden:
			_xmlw.WriteAttributeString("msdata", "hidden" + col.EncodedColumnName, "urn:schemas-microsoft-com:xml-msdata", text);
			break;
		case MappingType.SimpleContent:
			_xmlw.WriteString(text);
			break;
		case MappingType.Element:
		{
			bool flag = true;
			object obj = row[col, version];
			if (!col.IsCustomType || !col.IsValueCustomTypeInstance(obj) || typeof(IXmlSerializable).IsAssignableFrom(obj.GetType()))
			{
				_xmlw.WriteStartElement(prefix, col.EncodedColumnName, col.Namespace);
				flag = false;
			}
			Type type = obj.GetType();
			if (!col.IsCustomType)
			{
				if ((type == typeof(char) || type == typeof(string)) && XmlDataTreeWriter.PreserveSpace(text))
				{
					_xmlw.WriteAttributeString("xml", "space", "http://www.w3.org/XML/1998/namespace", "preserve");
				}
				_xmlw.WriteString(text);
			}
			else if (obj != DBNull.Value && (!col.ImplementsINullable || !DataStorage.IsObjectSqlNull(obj)))
			{
				if (col.IsValueCustomTypeInstance(obj))
				{
					if (!flag && obj.GetType() != col.DataType)
					{
						_xmlw.WriteAttributeString("msdata", "InstanceType", "urn:schemas-microsoft-com:xml-msdata", DataStorage.GetQualifiedName(type));
					}
					if (!flag)
					{
						col.ConvertObjectToXml(obj, _xmlw, null);
					}
					else
					{
						if (obj.GetType() != col.DataType)
						{
							throw ExceptionBuilder.PolymorphismNotSupported(type.AssemblyQualifiedName);
						}
						XmlRootAttribute xmlRootAttribute = new XmlRootAttribute(col.EncodedColumnName);
						xmlRootAttribute.Namespace = col.Namespace;
						col.ConvertObjectToXml(obj, _xmlw, xmlRootAttribute);
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
						_xmlw.WriteString(col.ConvertObjectToXml(obj));
					}
					else
					{
						col.ConvertObjectToXml(obj, _xmlw, null);
					}
				}
			}
			if (!flag)
			{
				_xmlw.WriteEndElement();
			}
			break;
		}
		}
	}

	internal static string QualifiedName(string prefix, string name)
	{
		if (prefix != null)
		{
			return prefix + ":" + name;
		}
		return name;
	}
}
