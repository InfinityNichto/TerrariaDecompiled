using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Xml;
using System.Xml.Serialization;

namespace System.Data;

internal sealed class XMLDiffLoader
{
	private ArrayList _tables;

	private DataSet _dataSet;

	private DataTable _dataTable;

	[RequiresUnreferencedCode("Members from serialized types may be trimmed if not referenced directly.")]
	internal void LoadDiffGram(DataSet ds, XmlReader dataTextReader)
	{
		XmlReader xmlReader = DataTextReader.CreateReader(dataTextReader);
		_dataSet = ds;
		while (xmlReader.LocalName == "before" && xmlReader.NamespaceURI == "urn:schemas-microsoft-com:xml-diffgram-v1")
		{
			ProcessDiffs(ds, xmlReader);
			xmlReader.Read();
		}
		while (xmlReader.LocalName == "errors" && xmlReader.NamespaceURI == "urn:schemas-microsoft-com:xml-diffgram-v1")
		{
			ProcessErrors(ds, xmlReader);
			xmlReader.Read();
		}
	}

	private void CreateTablesHierarchy(DataTable dt)
	{
		foreach (DataRelation childRelation in dt.ChildRelations)
		{
			if (!_tables.Contains(childRelation.ChildTable))
			{
				_tables.Add(childRelation.ChildTable);
				CreateTablesHierarchy(childRelation.ChildTable);
			}
		}
	}

	[RequiresUnreferencedCode("Members from serialized types may be trimmed if not referenced directly.")]
	internal void LoadDiffGram(DataTable dt, XmlReader dataTextReader)
	{
		XmlReader xmlReader = DataTextReader.CreateReader(dataTextReader);
		_dataTable = dt;
		_tables = new ArrayList();
		_tables.Add(dt);
		CreateTablesHierarchy(dt);
		while (xmlReader.LocalName == "before" && xmlReader.NamespaceURI == "urn:schemas-microsoft-com:xml-diffgram-v1")
		{
			ProcessDiffs(_tables, xmlReader);
			xmlReader.Read();
		}
		while (xmlReader.LocalName == "errors" && xmlReader.NamespaceURI == "urn:schemas-microsoft-com:xml-diffgram-v1")
		{
			ProcessErrors(_tables, xmlReader);
			xmlReader.Read();
		}
	}

	[RequiresUnreferencedCode("Members from serialized types may be trimmed if not referenced directly.")]
	internal void ProcessDiffs(DataSet ds, XmlReader ssync)
	{
		int pos = -1;
		int depth = ssync.Depth;
		ssync.Read();
		SkipWhitespaces(ssync);
		while (depth < ssync.Depth)
		{
			DataTable table = null;
			string text = null;
			int num = -1;
			text = ssync.GetAttribute("id", "urn:schemas-microsoft-com:xml-diffgram-v1");
			bool flag = ssync.GetAttribute("hasErrors", "urn:schemas-microsoft-com:xml-diffgram-v1") == "true";
			num = ReadOldRowData(ds, ref table, ref pos, ssync);
			if (num == -1)
			{
				continue;
			}
			if (table == null)
			{
				throw ExceptionBuilder.DiffgramMissingSQL();
			}
			DataRow dataRow = (DataRow)table.RowDiffId[text];
			if (dataRow != null)
			{
				dataRow._oldRecord = num;
				table._recordManager[num] = dataRow;
				continue;
			}
			dataRow = table.NewEmptyRow();
			table._recordManager[num] = dataRow;
			dataRow._oldRecord = num;
			dataRow._newRecord = num;
			table.Rows.DiffInsertAt(dataRow, pos);
			dataRow.Delete();
			if (flag)
			{
				table.RowDiffId[text] = dataRow;
			}
		}
	}

	[RequiresUnreferencedCode("Members from serialized types may be trimmed if not referenced directly.")]
	internal void ProcessDiffs(ArrayList tableList, XmlReader ssync)
	{
		int pos = -1;
		int depth = ssync.Depth;
		ssync.Read();
		while (depth < ssync.Depth)
		{
			DataTable table = null;
			int num = -1;
			string attribute = ssync.GetAttribute("id", "urn:schemas-microsoft-com:xml-diffgram-v1");
			bool flag = ssync.GetAttribute("hasErrors", "urn:schemas-microsoft-com:xml-diffgram-v1") == "true";
			num = ReadOldRowData(_dataSet, ref table, ref pos, ssync);
			if (num == -1)
			{
				continue;
			}
			if (table == null)
			{
				throw ExceptionBuilder.DiffgramMissingSQL();
			}
			DataRow dataRow = (DataRow)table.RowDiffId[attribute];
			if (dataRow != null)
			{
				dataRow._oldRecord = num;
				table._recordManager[num] = dataRow;
				continue;
			}
			dataRow = table.NewEmptyRow();
			table._recordManager[num] = dataRow;
			dataRow._oldRecord = num;
			dataRow._newRecord = num;
			table.Rows.DiffInsertAt(dataRow, pos);
			dataRow.Delete();
			if (flag)
			{
				table.RowDiffId[attribute] = dataRow;
			}
		}
	}

	[RequiresUnreferencedCode("Members from serialized types may be trimmed if not referenced directly.")]
	internal void ProcessErrors(DataSet ds, XmlReader ssync)
	{
		int depth = ssync.Depth;
		ssync.Read();
		while (depth < ssync.Depth)
		{
			DataTable table = ds.Tables.GetTable(XmlConvert.DecodeName(ssync.LocalName), ssync.NamespaceURI);
			if (table == null)
			{
				throw ExceptionBuilder.DiffgramMissingSQL();
			}
			string attribute = ssync.GetAttribute("id", "urn:schemas-microsoft-com:xml-diffgram-v1");
			DataRow dataRow = (DataRow)table.RowDiffId[attribute];
			string attribute2 = ssync.GetAttribute("Error", "urn:schemas-microsoft-com:xml-diffgram-v1");
			if (attribute2 != null)
			{
				dataRow.RowError = attribute2;
			}
			int depth2 = ssync.Depth;
			ssync.Read();
			while (depth2 < ssync.Depth)
			{
				if (XmlNodeType.Element == ssync.NodeType)
				{
					DataColumn column = table.Columns[XmlConvert.DecodeName(ssync.LocalName), ssync.NamespaceURI];
					string attribute3 = ssync.GetAttribute("Error", "urn:schemas-microsoft-com:xml-diffgram-v1");
					dataRow.SetColumnError(column, attribute3);
				}
				ssync.Read();
			}
			while (ssync.NodeType == XmlNodeType.EndElement && depth < ssync.Depth)
			{
				ssync.Read();
			}
		}
	}

	[RequiresUnreferencedCode("Members from serialized types may be trimmed if not referenced directly.")]
	internal void ProcessErrors(ArrayList dt, XmlReader ssync)
	{
		int depth = ssync.Depth;
		ssync.Read();
		while (depth < ssync.Depth)
		{
			DataTable table = GetTable(XmlConvert.DecodeName(ssync.LocalName), ssync.NamespaceURI);
			if (table == null)
			{
				throw ExceptionBuilder.DiffgramMissingSQL();
			}
			string attribute = ssync.GetAttribute("id", "urn:schemas-microsoft-com:xml-diffgram-v1");
			DataRow dataRow = (DataRow)table.RowDiffId[attribute];
			if (dataRow == null)
			{
				for (int i = 0; i < dt.Count; i++)
				{
					dataRow = (DataRow)((DataTable)dt[i]).RowDiffId[attribute];
					if (dataRow != null)
					{
						table = dataRow.Table;
						break;
					}
				}
			}
			string attribute2 = ssync.GetAttribute("Error", "urn:schemas-microsoft-com:xml-diffgram-v1");
			if (attribute2 != null)
			{
				dataRow.RowError = attribute2;
			}
			int depth2 = ssync.Depth;
			ssync.Read();
			while (depth2 < ssync.Depth)
			{
				if (XmlNodeType.Element == ssync.NodeType)
				{
					DataColumn column = table.Columns[XmlConvert.DecodeName(ssync.LocalName), ssync.NamespaceURI];
					string attribute3 = ssync.GetAttribute("Error", "urn:schemas-microsoft-com:xml-diffgram-v1");
					dataRow.SetColumnError(column, attribute3);
				}
				ssync.Read();
			}
			while (ssync.NodeType == XmlNodeType.EndElement && depth < ssync.Depth)
			{
				ssync.Read();
			}
		}
	}

	private DataTable GetTable(string tableName, string ns)
	{
		if (_tables == null)
		{
			return _dataSet.Tables.GetTable(tableName, ns);
		}
		if (_tables.Count == 0)
		{
			return (DataTable)_tables[0];
		}
		for (int i = 0; i < _tables.Count; i++)
		{
			DataTable dataTable = (DataTable)_tables[i];
			if (string.Equals(dataTable.TableName, tableName, StringComparison.Ordinal) && string.Equals(dataTable.Namespace, ns, StringComparison.Ordinal))
			{
				return dataTable;
			}
		}
		return null;
	}

	[RequiresUnreferencedCode("Members from serialized types may be trimmed if not referenced directly.")]
	private int ReadOldRowData(DataSet ds, ref DataTable table, ref int pos, XmlReader row)
	{
		if (ds != null)
		{
			table = ds.Tables.GetTable(XmlConvert.DecodeName(row.LocalName), row.NamespaceURI);
		}
		else
		{
			table = GetTable(XmlConvert.DecodeName(row.LocalName), row.NamespaceURI);
		}
		if (table == null)
		{
			row.Skip();
			return -1;
		}
		int depth = row.Depth;
		string text = null;
		text = row.GetAttribute("rowOrder", "urn:schemas-microsoft-com:xml-msdata");
		if (!string.IsNullOrEmpty(text))
		{
			pos = (int)Convert.ChangeType(text, typeof(int), null);
		}
		int num = table.NewRecord();
		foreach (DataColumn column in table.Columns)
		{
			column[num] = DBNull.Value;
		}
		foreach (DataColumn column2 in table.Columns)
		{
			if (column2.ColumnMapping != MappingType.Element && column2.ColumnMapping != MappingType.SimpleContent)
			{
				text = ((column2.ColumnMapping != MappingType.Hidden) ? row.GetAttribute(column2.EncodedColumnName, column2.Namespace) : row.GetAttribute("hidden" + column2.EncodedColumnName, "urn:schemas-microsoft-com:xml-msdata"));
				if (text != null)
				{
					column2[num] = column2.ConvertXmlToObject(text);
				}
			}
		}
		row.Read();
		SkipWhitespaces(row);
		int depth2 = row.Depth;
		if (depth2 <= depth)
		{
			if (depth2 == depth && row.NodeType == XmlNodeType.EndElement)
			{
				row.Read();
				SkipWhitespaces(row);
			}
			return num;
		}
		if (table.XmlText != null)
		{
			DataColumn xmlText = table.XmlText;
			xmlText[num] = xmlText.ConvertXmlToObject(row.ReadString());
		}
		else
		{
			while (row.Depth > depth)
			{
				string text2 = XmlConvert.DecodeName(row.LocalName);
				string namespaceURI = row.NamespaceURI;
				DataColumn dataColumn3 = table.Columns[text2, namespaceURI];
				if (dataColumn3 == null)
				{
					while (row.NodeType != XmlNodeType.EndElement && row.LocalName != text2 && row.NamespaceURI != namespaceURI)
					{
						row.Read();
					}
					row.Read();
					continue;
				}
				if (dataColumn3.IsCustomType)
				{
					bool flag = dataColumn3.DataType == typeof(object) || row.GetAttribute("InstanceType", "urn:schemas-microsoft-com:xml-msdata") != null || row.GetAttribute("type", "http://www.w3.org/2001/XMLSchema-instance") != null;
					bool flag2 = false;
					if (dataColumn3.Table.DataSet != null && dataColumn3.Table.DataSet._udtIsWrapped)
					{
						row.Read();
						flag2 = true;
					}
					XmlRootAttribute xmlRootAttribute = null;
					if (!flag && !dataColumn3.ImplementsIXMLSerializable)
					{
						if (flag2)
						{
							xmlRootAttribute = new XmlRootAttribute(row.LocalName);
							xmlRootAttribute.Namespace = row.NamespaceURI;
						}
						else
						{
							xmlRootAttribute = new XmlRootAttribute(dataColumn3.EncodedColumnName);
							xmlRootAttribute.Namespace = dataColumn3.Namespace;
						}
					}
					dataColumn3[num] = dataColumn3.ConvertXmlToObject(row, xmlRootAttribute);
					if (flag2)
					{
						row.Read();
					}
					continue;
				}
				int depth3 = row.Depth;
				row.Read();
				if (row.Depth > depth3)
				{
					if (row.NodeType == XmlNodeType.Text || row.NodeType == XmlNodeType.Whitespace || row.NodeType == XmlNodeType.SignificantWhitespace)
					{
						string s = row.ReadString();
						dataColumn3[num] = dataColumn3.ConvertXmlToObject(s);
						row.Read();
					}
				}
				else if (dataColumn3.DataType == typeof(string))
				{
					dataColumn3[num] = string.Empty;
				}
			}
		}
		row.Read();
		SkipWhitespaces(row);
		return num;
	}

	internal void SkipWhitespaces(XmlReader reader)
	{
		while (reader.NodeType == XmlNodeType.Whitespace || reader.NodeType == XmlNodeType.SignificantWhitespace)
		{
			reader.Read();
		}
	}
}
