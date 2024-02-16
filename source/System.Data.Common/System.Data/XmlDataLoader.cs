using System.Collections;
using System.Collections.Generic;
using System.Data.Common;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text;
using System.Xml;
using System.Xml.Serialization;

namespace System.Data;

internal sealed class XmlDataLoader
{
	private readonly DataSet _dataSet;

	private XmlToDatasetMap _nodeToSchemaMap;

	private readonly Hashtable _nodeToRowMap;

	private readonly Stack<DataRow> _childRowsStack;

	private readonly bool _fIsXdr;

	internal bool _isDiffgram;

	private XmlElement _topMostNode;

	private readonly bool _ignoreSchema;

	private readonly DataTable _dataTable;

	private readonly bool _isTableLevel;

	private bool _fromInference;

	private XmlReader _dataReader;

	private object _XSD_XMLNS_NS;

	private object _XDR_SCHEMA;

	private object _XDRNS;

	private object _SQL_SYNC;

	private object _UPDGNS;

	private object _XSD_SCHEMA;

	private object _XSDNS;

	private object _DFFNS;

	private object _MSDNS;

	private object _DIFFID;

	private object _HASCHANGES;

	private object _ROWORDER;

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

	internal XmlDataLoader(DataSet dataset, bool IsXdr, bool ignoreSchema)
	{
		_dataSet = dataset;
		_nodeToRowMap = new Hashtable();
		_fIsXdr = IsXdr;
		_ignoreSchema = ignoreSchema;
	}

	internal XmlDataLoader(DataSet dataset, bool IsXdr, XmlElement topNode, bool ignoreSchema)
	{
		_dataSet = dataset;
		_nodeToRowMap = new Hashtable();
		_fIsXdr = IsXdr;
		_childRowsStack = new Stack<DataRow>(50);
		_topMostNode = topNode;
		_ignoreSchema = ignoreSchema;
	}

	internal XmlDataLoader(DataTable datatable, bool IsXdr, bool ignoreSchema)
	{
		_dataSet = null;
		_dataTable = datatable;
		_isTableLevel = true;
		_nodeToRowMap = new Hashtable();
		_fIsXdr = IsXdr;
		_ignoreSchema = ignoreSchema;
	}

	internal XmlDataLoader(DataTable datatable, bool IsXdr, XmlElement topNode, bool ignoreSchema)
	{
		_dataSet = null;
		_dataTable = datatable;
		_isTableLevel = true;
		_nodeToRowMap = new Hashtable();
		_fIsXdr = IsXdr;
		_childRowsStack = new Stack<DataRow>(50);
		_topMostNode = topNode;
		_ignoreSchema = ignoreSchema;
	}

	[RequiresUnreferencedCode("Members from serialized types may be trimmed if not referenced directly.")]
	private void AttachRows(DataRow parentRow, XmlNode parentElement)
	{
		if (parentElement == null)
		{
			return;
		}
		for (XmlNode xmlNode = parentElement.FirstChild; xmlNode != null; xmlNode = xmlNode.NextSibling)
		{
			if (xmlNode.NodeType == XmlNodeType.Element)
			{
				XmlElement e = (XmlElement)xmlNode;
				DataRow rowFromElement = GetRowFromElement(e);
				if (rowFromElement != null && rowFromElement.RowState == DataRowState.Detached)
				{
					if (parentRow != null)
					{
						rowFromElement.SetNestedParentRow(parentRow, setNonNested: false);
					}
					rowFromElement.Table.Rows.Add(rowFromElement);
				}
				else if (rowFromElement == null)
				{
					AttachRows(parentRow, xmlNode);
				}
				AttachRows(rowFromElement, xmlNode);
			}
		}
	}

	private int CountNonNSAttributes(XmlNode node)
	{
		int num = 0;
		for (int i = 0; i < node.Attributes.Count; i++)
		{
			if (!FExcludedNamespace(node.Attributes[i].NamespaceURI))
			{
				num++;
			}
		}
		return num;
	}

	private string GetValueForTextOnlyColums(XmlNode n)
	{
		string text = null;
		while (n != null && (n.NodeType == XmlNodeType.Whitespace || !IsTextLikeNode(n.NodeType)))
		{
			n = n.NextSibling;
		}
		if (n != null)
		{
			if (IsTextLikeNode(n.NodeType) && (n.NextSibling == null || !IsTextLikeNode(n.NodeType)))
			{
				text = n.Value;
				n = n.NextSibling;
			}
			else
			{
				StringBuilder stringBuilder = new StringBuilder();
				while (n != null && IsTextLikeNode(n.NodeType))
				{
					stringBuilder.Append(n.Value);
					n = n.NextSibling;
				}
				text = stringBuilder.ToString();
			}
		}
		if (text == null)
		{
			text = string.Empty;
		}
		return text;
	}

	private string GetInitialTextFromNodes(ref XmlNode n)
	{
		string text = null;
		if (n != null)
		{
			while (n.NodeType == XmlNodeType.Whitespace)
			{
				n = n.NextSibling;
			}
			if (IsTextLikeNode(n.NodeType) && (n.NextSibling == null || !IsTextLikeNode(n.NodeType)))
			{
				text = n.Value;
				n = n.NextSibling;
			}
			else
			{
				StringBuilder stringBuilder = new StringBuilder();
				while (n != null && IsTextLikeNode(n.NodeType))
				{
					stringBuilder.Append(n.Value);
					n = n.NextSibling;
				}
				text = stringBuilder.ToString();
			}
		}
		if (text == null)
		{
			text = string.Empty;
		}
		return text;
	}

	private DataColumn GetTextOnlyColumn(DataRow row)
	{
		DataColumnCollection columns = row.Table.Columns;
		int count = columns.Count;
		for (int i = 0; i < count; i++)
		{
			DataColumn dataColumn = columns[i];
			if (IsTextOnly(dataColumn))
			{
				return dataColumn;
			}
		}
		return null;
	}

	internal DataRow GetRowFromElement(XmlElement e)
	{
		return (DataRow)_nodeToRowMap[e];
	}

	internal bool FColumnElement(XmlElement e)
	{
		if (_nodeToSchemaMap.GetColumnSchema(e, FIgnoreNamespace(e)) == null)
		{
			return false;
		}
		if (CountNonNSAttributes(e) > 0)
		{
			return false;
		}
		for (XmlNode xmlNode = e.FirstChild; xmlNode != null; xmlNode = xmlNode.NextSibling)
		{
			if (xmlNode is XmlElement)
			{
				return false;
			}
		}
		return true;
	}

	private bool FExcludedNamespace(string ns)
	{
		return ns.Equals("http://www.w3.org/2000/xmlns/");
	}

	private bool FIgnoreNamespace(XmlNode node)
	{
		if (!_fIsXdr)
		{
			return false;
		}
		XmlNode xmlNode = ((!(node is XmlAttribute)) ? node : ((XmlAttribute)node).OwnerElement);
		if (xmlNode.NamespaceURI.StartsWith("x-schema:#", StringComparison.Ordinal))
		{
			return true;
		}
		return false;
	}

	private bool FIgnoreNamespace(XmlReader node)
	{
		if (_fIsXdr && node.NamespaceURI.StartsWith("x-schema:#", StringComparison.Ordinal))
		{
			return true;
		}
		return false;
	}

	internal bool IsTextLikeNode(XmlNodeType n)
	{
		switch (n)
		{
		case XmlNodeType.EntityReference:
			throw ExceptionBuilder.FoundEntity();
		case XmlNodeType.Text:
		case XmlNodeType.CDATA:
		case XmlNodeType.Whitespace:
		case XmlNodeType.SignificantWhitespace:
			return true;
		default:
			return false;
		}
	}

	internal bool IsTextOnly(DataColumn c)
	{
		if (c.ColumnMapping != MappingType.SimpleContent)
		{
			return false;
		}
		return true;
	}

	[RequiresUnreferencedCode("Members from serialized types may be trimmed if not referenced directly.")]
	internal void LoadData(XmlDocument xdoc)
	{
		if (xdoc.DocumentElement == null)
		{
			return;
		}
		bool enforceConstraints;
		if (_isTableLevel)
		{
			enforceConstraints = _dataTable.EnforceConstraints;
			_dataTable.EnforceConstraints = false;
		}
		else
		{
			enforceConstraints = _dataSet.EnforceConstraints;
			_dataSet.EnforceConstraints = false;
			_dataSet._fInReadXml = true;
		}
		if (_isTableLevel)
		{
			_nodeToSchemaMap = new XmlToDatasetMap(_dataTable, xdoc.NameTable);
		}
		else
		{
			_nodeToSchemaMap = new XmlToDatasetMap(_dataSet, xdoc.NameTable);
		}
		DataRow dataRow = null;
		if (_isTableLevel || (_dataSet != null && _dataSet._fTopLevelTable))
		{
			XmlElement documentElement = xdoc.DocumentElement;
			DataTable dataTable = (DataTable)_nodeToSchemaMap.GetSchemaForNode(documentElement, FIgnoreNamespace(documentElement));
			if (dataTable != null)
			{
				dataRow = dataTable.CreateEmptyRow();
				_nodeToRowMap[documentElement] = dataRow;
				LoadRowData(dataRow, documentElement);
				dataTable.Rows.Add(dataRow);
			}
		}
		LoadRows(dataRow, xdoc.DocumentElement);
		AttachRows(dataRow, xdoc.DocumentElement);
		if (_isTableLevel)
		{
			_dataTable.EnforceConstraints = enforceConstraints;
			return;
		}
		_dataSet._fInReadXml = false;
		_dataSet.EnforceConstraints = enforceConstraints;
	}

	[RequiresUnreferencedCode("Members from serialized types may be trimmed if not referenced directly.")]
	private void LoadRowData(DataRow row, XmlElement rowElement)
	{
		DataTable table = row.Table;
		if (FromInference)
		{
			table.Prefix = rowElement.Prefix;
		}
		Hashtable hashtable = new Hashtable();
		row.BeginEdit();
		XmlNode xmlNode = rowElement.FirstChild;
		DataColumn textOnlyColumn = GetTextOnlyColumn(row);
		if (textOnlyColumn != null)
		{
			hashtable[textOnlyColumn] = textOnlyColumn;
			string valueForTextOnlyColums = GetValueForTextOnlyColums(xmlNode);
			if (XMLSchema.GetBooleanAttribute(rowElement, "nil", "http://www.w3.org/2001/XMLSchema-instance", defVal: false) && string.IsNullOrEmpty(valueForTextOnlyColums))
			{
				row[textOnlyColumn] = DBNull.Value;
			}
			else
			{
				SetRowValueFromXmlText(row, textOnlyColumn, valueForTextOnlyColums);
			}
		}
		while (xmlNode != null && xmlNode != rowElement)
		{
			if (xmlNode.NodeType == XmlNodeType.Element)
			{
				XmlElement xmlElement = (XmlElement)xmlNode;
				object obj = _nodeToSchemaMap.GetSchemaForNode(xmlElement, FIgnoreNamespace(xmlElement));
				if (obj is DataTable && FColumnElement(xmlElement))
				{
					obj = _nodeToSchemaMap.GetColumnSchema(xmlElement, FIgnoreNamespace(xmlElement));
				}
				if (obj == null || obj is DataColumn)
				{
					xmlNode = xmlElement.FirstChild;
					if (obj != null && obj is DataColumn)
					{
						DataColumn dataColumn = (DataColumn)obj;
						if (dataColumn.Table == row.Table && dataColumn.ColumnMapping != MappingType.Attribute && hashtable[dataColumn] == null)
						{
							hashtable[dataColumn] = dataColumn;
							string valueForTextOnlyColums2 = GetValueForTextOnlyColums(xmlNode);
							if (XMLSchema.GetBooleanAttribute(xmlElement, "nil", "http://www.w3.org/2001/XMLSchema-instance", defVal: false) && string.IsNullOrEmpty(valueForTextOnlyColums2))
							{
								row[dataColumn] = DBNull.Value;
							}
							else
							{
								SetRowValueFromXmlText(row, dataColumn, valueForTextOnlyColums2);
							}
						}
					}
					else if (obj == null && xmlNode != null)
					{
						continue;
					}
					if (xmlNode == null)
					{
						xmlNode = xmlElement;
					}
				}
			}
			while (xmlNode != rowElement && xmlNode.NextSibling == null)
			{
				xmlNode = xmlNode.ParentNode;
			}
			if (xmlNode != rowElement)
			{
				xmlNode = xmlNode.NextSibling;
			}
		}
		foreach (XmlAttribute attribute in rowElement.Attributes)
		{
			object columnSchema = _nodeToSchemaMap.GetColumnSchema(attribute, FIgnoreNamespace(attribute));
			if (columnSchema != null && columnSchema is DataColumn)
			{
				DataColumn dataColumn2 = (DataColumn)columnSchema;
				if (dataColumn2.ColumnMapping == MappingType.Attribute && hashtable[dataColumn2] == null)
				{
					hashtable[dataColumn2] = dataColumn2;
					xmlNode = attribute.FirstChild;
					SetRowValueFromXmlText(row, dataColumn2, GetInitialTextFromNodes(ref xmlNode));
				}
			}
		}
		foreach (DataColumn column in row.Table.Columns)
		{
			if (hashtable[column] != null || !XmlToDatasetMap.IsMappedColumn(column))
			{
				continue;
			}
			if (!column.AutoIncrement)
			{
				if (column.AllowDBNull)
				{
					row[column] = DBNull.Value;
				}
				else
				{
					row[column] = column.DefaultValue;
				}
			}
			else
			{
				column.Init(row._tempRecord);
			}
		}
		row.EndEdit();
	}

	[RequiresUnreferencedCode("Members from serialized types may be trimmed if not referenced directly.")]
	private void LoadRows(DataRow parentRow, XmlNode parentElement)
	{
		if (parentElement == null || (parentElement.LocalName == "schema" && parentElement.NamespaceURI == "http://www.w3.org/2001/XMLSchema") || (parentElement.LocalName == "sync" && parentElement.NamespaceURI == "urn:schemas-microsoft-com:xml-updategram") || (parentElement.LocalName == "Schema" && parentElement.NamespaceURI == "urn:schemas-microsoft-com:xml-data"))
		{
			return;
		}
		for (XmlNode xmlNode = parentElement.FirstChild; xmlNode != null; xmlNode = xmlNode.NextSibling)
		{
			if (!(xmlNode is XmlElement))
			{
				continue;
			}
			XmlElement xmlElement = (XmlElement)xmlNode;
			object schemaForNode = _nodeToSchemaMap.GetSchemaForNode(xmlElement, FIgnoreNamespace(xmlElement));
			if (schemaForNode != null && schemaForNode is DataTable)
			{
				DataRow dataRow = GetRowFromElement(xmlElement);
				if (dataRow == null)
				{
					if (parentRow != null && FColumnElement(xmlElement))
					{
						continue;
					}
					dataRow = ((DataTable)schemaForNode).CreateEmptyRow();
					_nodeToRowMap[xmlElement] = dataRow;
					LoadRowData(dataRow, xmlElement);
				}
				LoadRows(dataRow, xmlNode);
			}
			else
			{
				LoadRows(null, xmlNode);
			}
		}
	}

	[RequiresUnreferencedCode("Members from serialized types may be trimmed if not referenced directly.")]
	private void SetRowValueFromXmlText(DataRow row, DataColumn col, string xmlText)
	{
		row[col] = col.ConvertXmlToObject(xmlText);
	}

	private void InitNameTable()
	{
		XmlNameTable nameTable = _dataReader.NameTable;
		_XSD_XMLNS_NS = nameTable.Add("http://www.w3.org/2000/xmlns/");
		_XDR_SCHEMA = nameTable.Add("Schema");
		_XDRNS = nameTable.Add("urn:schemas-microsoft-com:xml-data");
		_SQL_SYNC = nameTable.Add("sync");
		_UPDGNS = nameTable.Add("urn:schemas-microsoft-com:xml-updategram");
		_XSD_SCHEMA = nameTable.Add("schema");
		_XSDNS = nameTable.Add("http://www.w3.org/2001/XMLSchema");
		_DFFNS = nameTable.Add("urn:schemas-microsoft-com:xml-diffgram-v1");
		_MSDNS = nameTable.Add("urn:schemas-microsoft-com:xml-msdata");
		_DIFFID = nameTable.Add("id");
		_HASCHANGES = nameTable.Add("hasChanges");
		_ROWORDER = nameTable.Add("rowOrder");
	}

	[RequiresUnreferencedCode("Members from serialized types may be trimmed if not referenced directly.")]
	internal void LoadData(XmlReader reader)
	{
		_dataReader = DataTextReader.CreateReader(reader);
		int depth = _dataReader.Depth;
		bool enforceConstraints = (_isTableLevel ? _dataTable.EnforceConstraints : _dataSet.EnforceConstraints);
		InitNameTable();
		if (_nodeToSchemaMap == null)
		{
			_nodeToSchemaMap = (_isTableLevel ? new XmlToDatasetMap(_dataReader.NameTable, _dataTable) : new XmlToDatasetMap(_dataReader.NameTable, _dataSet));
		}
		if (_isTableLevel)
		{
			_dataTable.EnforceConstraints = false;
		}
		else
		{
			_dataSet.EnforceConstraints = false;
			_dataSet._fInReadXml = true;
		}
		if (_topMostNode != null)
		{
			if (!_isDiffgram && !_isTableLevel && _nodeToSchemaMap.GetSchemaForNode(_topMostNode, FIgnoreNamespace(_topMostNode)) is DataTable table)
			{
				LoadTopMostTable(table);
			}
			_topMostNode = null;
		}
		while (!_dataReader.EOF && _dataReader.Depth >= depth)
		{
			if (reader.NodeType != XmlNodeType.Element)
			{
				_dataReader.Read();
				continue;
			}
			DataTable tableForNode = _nodeToSchemaMap.GetTableForNode(_dataReader, FIgnoreNamespace(_dataReader));
			if (tableForNode == null)
			{
				if (!ProcessXsdSchema())
				{
					_dataReader.Read();
				}
			}
			else
			{
				LoadTable(tableForNode, isNested: false);
			}
		}
		if (_isTableLevel)
		{
			_dataTable.EnforceConstraints = enforceConstraints;
			return;
		}
		_dataSet._fInReadXml = false;
		_dataSet.EnforceConstraints = enforceConstraints;
	}

	[RequiresUnreferencedCode("Members from serialized types may be trimmed if not referenced directly.")]
	private void LoadTopMostTable(DataTable table)
	{
		bool flag = _isTableLevel || _dataSet.DataSetName != table.TableName;
		DataRow dataRow = null;
		bool flag2 = false;
		int num = _dataReader.Depth - 1;
		int count = _childRowsStack.Count;
		DataColumnCollection columns = table.Columns;
		object[] array = new object[columns.Count];
		foreach (XmlAttribute attribute in _topMostNode.Attributes)
		{
			if (_nodeToSchemaMap.GetColumnSchema(attribute, FIgnoreNamespace(attribute)) is DataColumn { ColumnMapping: MappingType.Attribute } dataColumn)
			{
				XmlNode n = attribute.FirstChild;
				array[dataColumn.Ordinal] = dataColumn.ConvertXmlToObject(GetInitialTextFromNodes(ref n));
				flag2 = true;
			}
		}
		while (num < _dataReader.Depth)
		{
			switch (_dataReader.NodeType)
			{
			case XmlNodeType.Element:
			{
				object columnSchema = _nodeToSchemaMap.GetColumnSchema(table, _dataReader, FIgnoreNamespace(_dataReader));
				if (columnSchema is DataColumn dataColumn2)
				{
					if (array[dataColumn2.Ordinal] == null)
					{
						LoadColumn(dataColumn2, array);
						flag2 = true;
					}
					else
					{
						_dataReader.Read();
					}
				}
				else if (columnSchema is DataTable table2)
				{
					LoadTable(table2, isNested: true);
					flag2 = true;
				}
				else if (!ProcessXsdSchema())
				{
					if (!(flag2 || flag))
					{
						return;
					}
					_dataReader.Read();
				}
				break;
			}
			case XmlNodeType.EntityReference:
				throw ExceptionBuilder.FoundEntity();
			case XmlNodeType.Text:
			case XmlNodeType.CDATA:
			case XmlNodeType.Whitespace:
			case XmlNodeType.SignificantWhitespace:
			{
				string s = _dataReader.ReadString();
				DataColumn xmlText = table._xmlText;
				if (xmlText != null && array[xmlText.Ordinal] == null)
				{
					array[xmlText.Ordinal] = xmlText.ConvertXmlToObject(s);
				}
				break;
			}
			default:
				_dataReader.Read();
				break;
			}
		}
		_dataReader.Read();
		for (int num2 = array.Length - 1; num2 >= 0; num2--)
		{
			if (array[num2] == null)
			{
				DataColumn xmlText = columns[num2];
				if (xmlText.AllowDBNull && xmlText.ColumnMapping != MappingType.Hidden && !xmlText.AutoIncrement)
				{
					array[num2] = DBNull.Value;
				}
			}
		}
		dataRow = table.Rows.AddWithColumnEvents(array);
		while (count < _childRowsStack.Count)
		{
			DataRow dataRow2 = _childRowsStack.Pop();
			bool flag3 = dataRow2.RowState == DataRowState.Unchanged;
			dataRow2.SetNestedParentRow(dataRow, setNonNested: false);
			if (flag3)
			{
				dataRow2._oldRecord = dataRow2._newRecord;
			}
		}
	}

	[RequiresUnreferencedCode("Members from serialized types may be trimmed if not referenced directly.")]
	private void LoadTable(DataTable table, bool isNested)
	{
		DataRow dataRow = null;
		int depth = _dataReader.Depth;
		int count = _childRowsStack.Count;
		DataColumnCollection columns = table.Columns;
		object[] array = new object[columns.Count];
		int pos = -1;
		string key = string.Empty;
		string text = null;
		bool flag = false;
		for (int num = _dataReader.AttributeCount - 1; num >= 0; num--)
		{
			_dataReader.MoveToAttribute(num);
			if (_nodeToSchemaMap.GetColumnSchema(table, _dataReader, FIgnoreNamespace(_dataReader)) is DataColumn { ColumnMapping: MappingType.Attribute } dataColumn)
			{
				array[dataColumn.Ordinal] = dataColumn.ConvertXmlToObject(_dataReader.Value);
			}
			if (_isDiffgram)
			{
				if (_dataReader.NamespaceURI == "urn:schemas-microsoft-com:xml-diffgram-v1")
				{
					switch (_dataReader.LocalName)
					{
					case "id":
						key = _dataReader.Value;
						break;
					case "hasChanges":
						text = _dataReader.Value;
						break;
					case "hasErrors":
						flag = (bool)Convert.ChangeType(_dataReader.Value, typeof(bool), CultureInfo.InvariantCulture);
						break;
					}
				}
				else if (_dataReader.NamespaceURI == "urn:schemas-microsoft-com:xml-msdata")
				{
					if (_dataReader.LocalName == "rowOrder")
					{
						pos = (int)Convert.ChangeType(_dataReader.Value, typeof(int), CultureInfo.InvariantCulture);
					}
					else if (_dataReader.LocalName.StartsWith("hidden", StringComparison.Ordinal))
					{
						DataColumn dataColumn2 = columns[XmlConvert.DecodeName(_dataReader.LocalName.Substring(6))];
						if (dataColumn2 != null && dataColumn2.ColumnMapping == MappingType.Hidden)
						{
							array[dataColumn2.Ordinal] = dataColumn2.ConvertXmlToObject(_dataReader.Value);
						}
					}
				}
			}
		}
		if (_dataReader.Read() && depth < _dataReader.Depth)
		{
			while (depth < _dataReader.Depth)
			{
				switch (_dataReader.NodeType)
				{
				case XmlNodeType.Element:
				{
					object columnSchema = _nodeToSchemaMap.GetColumnSchema(table, _dataReader, FIgnoreNamespace(_dataReader));
					if (columnSchema is DataColumn dataColumn3)
					{
						if (array[dataColumn3.Ordinal] == null)
						{
							LoadColumn(dataColumn3, array);
						}
						else
						{
							_dataReader.Read();
						}
					}
					else if (columnSchema is DataTable table2)
					{
						LoadTable(table2, isNested: true);
					}
					else if (!ProcessXsdSchema())
					{
						DataTable tableForNode = _nodeToSchemaMap.GetTableForNode(_dataReader, FIgnoreNamespace(_dataReader));
						if (tableForNode != null)
						{
							LoadTable(tableForNode, isNested: false);
						}
						else
						{
							_dataReader.Read();
						}
					}
					break;
				}
				case XmlNodeType.EntityReference:
					throw ExceptionBuilder.FoundEntity();
				case XmlNodeType.Text:
				case XmlNodeType.CDATA:
				case XmlNodeType.Whitespace:
				case XmlNodeType.SignificantWhitespace:
				{
					string s = _dataReader.ReadString();
					DataColumn dataColumn2 = table._xmlText;
					if (dataColumn2 != null && array[dataColumn2.Ordinal] == null)
					{
						array[dataColumn2.Ordinal] = dataColumn2.ConvertXmlToObject(s);
					}
					break;
				}
				default:
					_dataReader.Read();
					break;
				}
			}
			_dataReader.Read();
		}
		if (_isDiffgram)
		{
			dataRow = table.NewRow(table.NewUninitializedRecord());
			dataRow.BeginEdit();
			for (int num2 = array.Length - 1; num2 >= 0; num2--)
			{
				DataColumn dataColumn2 = columns[num2];
				dataColumn2[dataRow._tempRecord] = ((array[num2] != null) ? array[num2] : DBNull.Value);
			}
			dataRow.EndEdit();
			table.Rows.DiffInsertAt(dataRow, pos);
			if (text == null)
			{
				dataRow._oldRecord = dataRow._newRecord;
			}
			if (text == "modified" || flag)
			{
				table.RowDiffId[key] = dataRow;
			}
		}
		else
		{
			for (int num3 = array.Length - 1; num3 >= 0; num3--)
			{
				if (array[num3] == null)
				{
					DataColumn dataColumn2 = columns[num3];
					if (dataColumn2.AllowDBNull && dataColumn2.ColumnMapping != MappingType.Hidden && !dataColumn2.AutoIncrement)
					{
						array[num3] = DBNull.Value;
					}
				}
			}
			dataRow = table.Rows.AddWithColumnEvents(array);
		}
		while (count < _childRowsStack.Count)
		{
			DataRow dataRow2 = _childRowsStack.Pop();
			bool flag2 = dataRow2.RowState == DataRowState.Unchanged;
			dataRow2.SetNestedParentRow(dataRow, setNonNested: false);
			if (flag2)
			{
				dataRow2._oldRecord = dataRow2._newRecord;
			}
		}
		if (isNested)
		{
			_childRowsStack.Push(dataRow);
		}
	}

	[RequiresUnreferencedCode("Members from serialized types may be trimmed if not referenced directly.")]
	private void LoadColumn(DataColumn column, object[] foundColumns)
	{
		string text = string.Empty;
		string text2 = null;
		int depth = _dataReader.Depth;
		if (_dataReader.AttributeCount > 0)
		{
			text2 = _dataReader.GetAttribute("nil", "http://www.w3.org/2001/XMLSchema-instance");
		}
		if (column.IsCustomType)
		{
			object obj = null;
			string text3 = null;
			string text4 = null;
			XmlRootAttribute xmlRootAttribute = null;
			if (_dataReader.AttributeCount > 0)
			{
				text3 = _dataReader.GetAttribute("type", "http://www.w3.org/2001/XMLSchema-instance");
				text4 = _dataReader.GetAttribute("InstanceType", "urn:schemas-microsoft-com:xml-msdata");
			}
			bool flag = !column.ImplementsIXMLSerializable && !(column.DataType == typeof(object)) && text4 == null && text3 == null;
			if (text2 != null && XmlConvert.ToBoolean(text2))
			{
				if (!flag && text4 != null && text4.Length > 0)
				{
					obj = SqlUdtStorage.GetStaticNullForUdtType(DataStorage.GetType(text4));
				}
				if (obj == null)
				{
					obj = DBNull.Value;
				}
				if (!_dataReader.IsEmptyElement)
				{
					while (_dataReader.Read() && depth < _dataReader.Depth)
					{
					}
				}
				_dataReader.Read();
			}
			else
			{
				bool flag2 = false;
				if (column.Table.DataSet != null && column.Table.DataSet._udtIsWrapped)
				{
					_dataReader.Read();
					flag2 = true;
				}
				if (flag)
				{
					if (flag2)
					{
						xmlRootAttribute = new XmlRootAttribute(_dataReader.LocalName);
						xmlRootAttribute.Namespace = _dataReader.NamespaceURI;
					}
					else
					{
						xmlRootAttribute = new XmlRootAttribute(column.EncodedColumnName);
						xmlRootAttribute.Namespace = column.Namespace;
					}
				}
				obj = column.ConvertXmlToObject(_dataReader, xmlRootAttribute);
				if (flag2)
				{
					_dataReader.Read();
				}
			}
			foundColumns[column.Ordinal] = obj;
			return;
		}
		if (_dataReader.Read() && depth < _dataReader.Depth)
		{
			while (depth < _dataReader.Depth)
			{
				switch (_dataReader.NodeType)
				{
				case XmlNodeType.Text:
				case XmlNodeType.CDATA:
				case XmlNodeType.Whitespace:
				case XmlNodeType.SignificantWhitespace:
					if (text.Length == 0)
					{
						text = _dataReader.Value;
						StringBuilder stringBuilder = null;
						while (_dataReader.Read() && depth < _dataReader.Depth && IsTextLikeNode(_dataReader.NodeType))
						{
							if (stringBuilder == null)
							{
								stringBuilder = new StringBuilder(text);
							}
							stringBuilder.Append(_dataReader.Value);
						}
						if (stringBuilder != null)
						{
							text = stringBuilder.ToString();
						}
					}
					else
					{
						_dataReader.ReadString();
					}
					break;
				case XmlNodeType.Element:
				{
					if (ProcessXsdSchema())
					{
						break;
					}
					object columnSchema = _nodeToSchemaMap.GetColumnSchema(column.Table, _dataReader, FIgnoreNamespace(_dataReader));
					if (columnSchema is DataColumn dataColumn)
					{
						if (foundColumns[dataColumn.Ordinal] == null)
						{
							LoadColumn(dataColumn, foundColumns);
						}
						else
						{
							_dataReader.Read();
						}
						break;
					}
					if (columnSchema is DataTable table)
					{
						LoadTable(table, isNested: true);
						break;
					}
					DataTable tableForNode = _nodeToSchemaMap.GetTableForNode(_dataReader, FIgnoreNamespace(_dataReader));
					if (tableForNode != null)
					{
						LoadTable(tableForNode, isNested: false);
					}
					else
					{
						_dataReader.Read();
					}
					break;
				}
				case XmlNodeType.EntityReference:
					throw ExceptionBuilder.FoundEntity();
				default:
					_dataReader.Read();
					break;
				}
			}
			_dataReader.Read();
		}
		if (text.Length == 0 && text2 != null && XmlConvert.ToBoolean(text2))
		{
			foundColumns[column.Ordinal] = DBNull.Value;
		}
		else
		{
			foundColumns[column.Ordinal] = column.ConvertXmlToObject(text);
		}
	}

	[RequiresUnreferencedCode("Members from serialized types may be trimmed if not referenced directly.")]
	private bool ProcessXsdSchema()
	{
		if (_dataReader.LocalName == _XSD_SCHEMA && _dataReader.NamespaceURI == _XSDNS)
		{
			if (_ignoreSchema)
			{
				_dataReader.Skip();
			}
			else if (_isTableLevel)
			{
				_dataTable.ReadXSDSchema(_dataReader, denyResolving: false);
				_nodeToSchemaMap = new XmlToDatasetMap(_dataReader.NameTable, _dataTable);
			}
			else
			{
				_dataSet.ReadXSDSchema(_dataReader, denyResolving: false);
				_nodeToSchemaMap = new XmlToDatasetMap(_dataReader.NameTable, _dataSet);
			}
		}
		else
		{
			if ((_dataReader.LocalName != _XDR_SCHEMA || _dataReader.NamespaceURI != _XDRNS) && (_dataReader.LocalName != _SQL_SYNC || _dataReader.NamespaceURI != _UPDGNS))
			{
				return false;
			}
			_dataReader.Skip();
		}
		return true;
	}
}
