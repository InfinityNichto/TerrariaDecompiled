using System.Collections.Generic;
using System.Data.Common;
using System.Data.SqlTypes;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Xml;

namespace System.Data.ProviderBase;

internal sealed class SchemaMapping
{
	private readonly DataSet _dataSet;

	private DataTable _dataTable;

	private readonly DataAdapter _adapter;

	private readonly DataReaderContainer _dataReader;

	private readonly DataTable _schemaTable;

	private readonly DataTableMapping _tableMapping;

	private readonly string[] _fieldNames;

	private readonly object[] _readerDataValues;

	private object[] _mappedDataValues;

	private int[] _indexMap;

	private bool[] _chapterMap;

	private int[] _xmlMap;

	private int _mappedMode;

	private int _mappedLength;

	private readonly LoadOption _loadOption;

	internal DataReaderContainer DataReader => _dataReader;

	internal DataTable DataTable => _dataTable;

	internal object[] DataValues => _readerDataValues;

	[RequiresUnreferencedCode("chapterValue and dataReader schema table rows DataTypes type cannot be statically analyzed.")]
	internal SchemaMapping(DataAdapter adapter, DataSet dataset, DataTable datatable, DataReaderContainer dataReader, bool keyInfo, SchemaType schemaType, string sourceTableName, bool gettingData, DataColumn parentChapterColumn, object parentChapterValue)
	{
		_dataSet = dataset;
		_dataTable = datatable;
		_adapter = adapter;
		_dataReader = dataReader;
		if (keyInfo)
		{
			_schemaTable = dataReader.GetSchemaTable();
		}
		if (adapter.ShouldSerializeFillLoadOption())
		{
			_loadOption = adapter.FillLoadOption;
		}
		else if (adapter.AcceptChangesDuringFill)
		{
			_loadOption = (LoadOption)4;
		}
		else
		{
			_loadOption = (LoadOption)5;
		}
		MissingMappingAction missingMappingAction;
		MissingSchemaAction schemaAction;
		if (SchemaType.Mapped == schemaType)
		{
			missingMappingAction = _adapter.MissingMappingAction;
			schemaAction = _adapter.MissingSchemaAction;
			if (!string.IsNullOrEmpty(sourceTableName))
			{
				_tableMapping = _adapter.GetTableMappingBySchemaAction(sourceTableName, sourceTableName, missingMappingAction);
			}
			else if (_dataTable != null)
			{
				int num = _adapter.IndexOfDataSetTable(_dataTable.TableName);
				if (-1 != num)
				{
					_tableMapping = _adapter.TableMappings[num];
				}
				else
				{
					_tableMapping = missingMappingAction switch
					{
						MissingMappingAction.Passthrough => new DataTableMapping(_dataTable.TableName, _dataTable.TableName), 
						MissingMappingAction.Ignore => null, 
						MissingMappingAction.Error => throw ADP.MissingTableMappingDestination(_dataTable.TableName), 
						_ => throw ADP.InvalidMissingMappingAction(missingMappingAction), 
					};
				}
			}
		}
		else
		{
			if (SchemaType.Source != schemaType)
			{
				throw ADP.InvalidSchemaType(schemaType);
			}
			missingMappingAction = MissingMappingAction.Passthrough;
			schemaAction = MissingSchemaAction.Add;
			if (!string.IsNullOrEmpty(sourceTableName))
			{
				_tableMapping = DataTableMappingCollection.GetTableMappingBySchemaAction(null, sourceTableName, sourceTableName, missingMappingAction);
			}
			else if (_dataTable != null)
			{
				int num2 = _adapter.IndexOfDataSetTable(_dataTable.TableName);
				if (-1 != num2)
				{
					_tableMapping = _adapter.TableMappings[num2];
				}
				else
				{
					_tableMapping = new DataTableMapping(_dataTable.TableName, _dataTable.TableName);
				}
			}
		}
		if (_tableMapping == null)
		{
			return;
		}
		if (_dataTable == null)
		{
			_dataTable = _tableMapping.GetDataTableBySchemaAction(_dataSet, schemaAction);
		}
		if (_dataTable != null)
		{
			_fieldNames = GenerateFieldNames(dataReader);
			if (_schemaTable == null)
			{
				_readerDataValues = SetupSchemaWithoutKeyInfo(missingMappingAction, schemaAction, gettingData, parentChapterColumn, parentChapterValue);
			}
			else
			{
				_readerDataValues = SetupSchemaWithKeyInfo(missingMappingAction, schemaAction, gettingData, parentChapterColumn, parentChapterValue);
			}
		}
	}

	internal void ApplyToDataRow(DataRow dataRow)
	{
		DataColumnCollection columns = dataRow.Table.Columns;
		_dataReader.GetValues(_readerDataValues);
		object[] mappedValues = GetMappedValues();
		bool[] array = new bool[mappedValues.Length];
		for (int i = 0; i < array.Length; i++)
		{
			array[i] = columns[i].ReadOnly;
		}
		try
		{
			try
			{
				for (int j = 0; j < array.Length; j++)
				{
					if (columns[j].Expression.Length == 0)
					{
						columns[j].ReadOnly = false;
					}
				}
				for (int k = 0; k < mappedValues.Length; k++)
				{
					object obj = mappedValues[k];
					if (obj != null)
					{
						dataRow[k] = obj;
					}
				}
			}
			finally
			{
				for (int l = 0; l < array.Length; l++)
				{
					if (columns[l].Expression.Length == 0)
					{
						columns[l].ReadOnly = array[l];
					}
				}
			}
		}
		finally
		{
			if (_chapterMap != null)
			{
				FreeDataRowChapters();
			}
		}
	}

	private void MappedChapterIndex()
	{
		int mappedLength = _mappedLength;
		for (int i = 0; i < mappedLength; i++)
		{
			int num = _indexMap[i];
			if (0 <= num)
			{
				_mappedDataValues[num] = _readerDataValues[i];
				if (_chapterMap[i])
				{
					_mappedDataValues[num] = null;
				}
			}
		}
	}

	private void MappedChapter()
	{
		int mappedLength = _mappedLength;
		for (int i = 0; i < mappedLength; i++)
		{
			_mappedDataValues[i] = _readerDataValues[i];
			if (_chapterMap[i])
			{
				_mappedDataValues[i] = null;
			}
		}
	}

	private void MappedIndex()
	{
		int mappedLength = _mappedLength;
		for (int i = 0; i < mappedLength; i++)
		{
			int num = _indexMap[i];
			if (0 <= num)
			{
				_mappedDataValues[num] = _readerDataValues[i];
			}
		}
	}

	private void MappedValues()
	{
		int mappedLength = _mappedLength;
		for (int i = 0; i < mappedLength; i++)
		{
			_mappedDataValues[i] = _readerDataValues[i];
		}
	}

	private object[] GetMappedValues()
	{
		if (_xmlMap != null)
		{
			for (int i = 0; i < _xmlMap.Length; i++)
			{
				if (_xmlMap[i] == 0)
				{
					continue;
				}
				string text = _readerDataValues[i] as string;
				if (text == null && _readerDataValues[i] is SqlString sqlString)
				{
					if (!sqlString.IsNull)
					{
						text = sqlString.Value;
					}
					else
					{
						object[] readerDataValues = _readerDataValues;
						int num = i;
						int num2 = _xmlMap[i];
						object obj = ((num2 != 1) ? ((object)DBNull.Value) : ((object)SqlXml.Null));
						readerDataValues[num] = obj;
					}
				}
				if (text != null)
				{
					switch (_xmlMap[i])
					{
					case 1:
					{
						XmlReaderSettings xmlReaderSettings = new XmlReaderSettings();
						xmlReaderSettings.ConformanceLevel = ConformanceLevel.Fragment;
						XmlReader value = XmlReader.Create((TextReader)new StringReader(text), xmlReaderSettings, (string?)null);
						_readerDataValues[i] = new SqlXml(value);
						break;
					}
					case 2:
					{
						XmlDocument xmlDocument = new XmlDocument();
						xmlDocument.LoadXml(text);
						_readerDataValues[i] = xmlDocument;
						break;
					}
					}
				}
			}
		}
		switch (_mappedMode)
		{
		default:
			return _readerDataValues;
		case 1:
			MappedValues();
			break;
		case 2:
			MappedIndex();
			break;
		case 3:
			MappedChapter();
			break;
		case 4:
			MappedChapterIndex();
			break;
		}
		return _mappedDataValues;
	}

	[RequiresUnreferencedCode("Row chapter column types cannot be statically analyzed")]
	internal void LoadDataRowWithClear()
	{
		for (int i = 0; i < _readerDataValues.Length; i++)
		{
			_readerDataValues[i] = null;
		}
		LoadDataRow();
	}

	[RequiresUnreferencedCode("Row chapter column types cannot be statically analyzed")]
	internal void LoadDataRow()
	{
		try
		{
			_dataReader.GetValues(_readerDataValues);
			object[] mappedValues = GetMappedValues();
			DataRow dataRow;
			switch (_loadOption)
			{
			case LoadOption.OverwriteChanges:
			case LoadOption.PreserveChanges:
			case LoadOption.Upsert:
				dataRow = _dataTable.LoadDataRow(mappedValues, _loadOption);
				break;
			case (LoadOption)4:
				dataRow = _dataTable.LoadDataRow(mappedValues, fAcceptChanges: true);
				break;
			case (LoadOption)5:
				dataRow = _dataTable.LoadDataRow(mappedValues, fAcceptChanges: false);
				break;
			default:
				throw ADP.InvalidLoadOption(_loadOption);
			}
			if (_chapterMap != null && _dataSet != null)
			{
				LoadDataRowChapters(dataRow);
			}
		}
		finally
		{
			if (_chapterMap != null)
			{
				FreeDataRowChapters();
			}
		}
	}

	private void FreeDataRowChapters()
	{
		for (int i = 0; i < _chapterMap.Length; i++)
		{
			if (_chapterMap[i] && _readerDataValues[i] is IDisposable disposable)
			{
				_readerDataValues[i] = null;
				disposable.Dispose();
			}
		}
	}

	[RequiresUnreferencedCode("Row chapter column types cannot be statically analyzed")]
	internal int LoadDataRowChapters(DataRow dataRow)
	{
		int num = 0;
		int num2 = _chapterMap.Length;
		for (int i = 0; i < num2; i++)
		{
			if (!_chapterMap[i])
			{
				continue;
			}
			object obj = _readerDataValues[i];
			if (obj == null || Convert.IsDBNull(obj))
			{
				continue;
			}
			_readerDataValues[i] = null;
			using IDataReader dataReader = (IDataReader)obj;
			if (!dataReader.IsClosed)
			{
				DataColumn dataColumn;
				object parentChapterValue;
				if (_indexMap == null)
				{
					dataColumn = _dataTable.Columns[i];
					parentChapterValue = dataRow[dataColumn];
				}
				else
				{
					dataColumn = _dataTable.Columns[_indexMap[i]];
					parentChapterValue = dataRow[dataColumn];
				}
				string srcTable = _tableMapping.SourceTable + _fieldNames[i];
				DataReaderContainer dataReader2 = DataReaderContainer.Create(dataReader, _dataReader.ReturnProviderSpecificTypes);
				num += _adapter.FillFromReader(_dataSet, null, srcTable, dataReader2, 0, 0, dataColumn, parentChapterValue);
			}
		}
		return num;
	}

	private int[] CreateIndexMap(int count, int index)
	{
		int[] array = new int[count];
		for (int i = 0; i < index; i++)
		{
			array[i] = i;
		}
		return array;
	}

	private static string[] GenerateFieldNames(DataReaderContainer dataReader)
	{
		string[] array = new string[dataReader.FieldCount];
		for (int i = 0; i < array.Length; i++)
		{
			array[i] = dataReader.GetName(i);
		}
		ADP.BuildSchemaTableInfoTableNames(array);
		return array;
	}

	private DataColumn[] ResizeColumnArray(DataColumn[] rgcol, int len)
	{
		DataColumn[] array = new DataColumn[len];
		Array.Copy(rgcol, array, len);
		return array;
	}

	private void AddItemToAllowRollback(ref List<object> items, object value)
	{
		if (items == null)
		{
			items = new List<object>();
		}
		items.Add(value);
	}

	private void RollbackAddedItems(List<object> items)
	{
		if (items == null)
		{
			return;
		}
		int num = items.Count - 1;
		while (0 <= num)
		{
			if (items[num] != null)
			{
				if (items[num] is DataColumn dataColumn)
				{
					if (dataColumn.Table != null)
					{
						dataColumn.Table.Columns.Remove(dataColumn);
					}
				}
				else if (items[num] is DataTable { DataSet: not null } dataTable)
				{
					dataTable.DataSet.Tables.Remove(dataTable);
				}
			}
			num--;
		}
	}

	[RequiresUnreferencedCode("chapterValue's type cannot be statically analyzed")]
	private object[] SetupSchemaWithoutKeyInfo(MissingMappingAction mappingAction, MissingSchemaAction schemaAction, bool gettingData, DataColumn parentChapterColumn, object chapterValue)
	{
		int[] array = null;
		bool[] array2 = null;
		int num = 0;
		int fieldCount = _dataReader.FieldCount;
		object[] result = null;
		List<object> items = null;
		try
		{
			DataColumnCollection columns = _dataTable.Columns;
			columns.EnsureAdditionalCapacity(fieldCount + ((chapterValue != null) ? 1 : 0));
			bool flag = _dataTable.Columns.Count == 0 && (_tableMapping.ColumnMappings == null || _tableMapping.ColumnMappings.Count == 0) && mappingAction == MissingMappingAction.Passthrough;
			for (int i = 0; i < fieldCount; i++)
			{
				bool flag2 = false;
				Type type = _dataReader.GetFieldType(i);
				if (null == type)
				{
					throw ADP.MissingDataReaderFieldType(i);
				}
				if (typeof(IDataReader).IsAssignableFrom(type))
				{
					if (array2 == null)
					{
						array2 = new bool[fieldCount];
					}
					flag2 = (array2[i] = true);
					type = typeof(int);
				}
				else if (typeof(SqlXml).IsAssignableFrom(type))
				{
					if (_xmlMap == null)
					{
						_xmlMap = new int[fieldCount];
					}
					_xmlMap[i] = 1;
				}
				else if (typeof(XmlReader).IsAssignableFrom(type))
				{
					type = typeof(string);
					if (_xmlMap == null)
					{
						_xmlMap = new int[fieldCount];
					}
					_xmlMap[i] = 2;
				}
				DataColumn dataColumn = ((!flag) ? _tableMapping.GetDataColumn(_fieldNames[i], type, _dataTable, mappingAction, schemaAction) : DataColumnMapping.CreateDataColumnBySchemaAction(_fieldNames[i], _fieldNames[i], _dataTable, type, schemaAction));
				if (dataColumn == null)
				{
					if (array == null)
					{
						array = CreateIndexMap(fieldCount, i);
					}
					array[i] = -1;
					continue;
				}
				if (_xmlMap != null && _xmlMap[i] != 0)
				{
					if (typeof(SqlXml) == dataColumn.DataType)
					{
						_xmlMap[i] = 1;
					}
					else if (typeof(XmlDocument) == dataColumn.DataType)
					{
						_xmlMap[i] = 2;
					}
					else
					{
						_xmlMap[i] = 0;
						int num2 = 0;
						for (int j = 0; j < _xmlMap.Length; j++)
						{
							num2 += _xmlMap[j];
						}
						if (num2 == 0)
						{
							_xmlMap = null;
						}
					}
				}
				if (dataColumn.Table == null)
				{
					if (flag2)
					{
						dataColumn.AllowDBNull = false;
						dataColumn.AutoIncrement = true;
						dataColumn.ReadOnly = true;
					}
					AddItemToAllowRollback(ref items, dataColumn);
					columns.Add(dataColumn);
				}
				else if (flag2 && !dataColumn.AutoIncrement)
				{
					throw ADP.FillChapterAutoIncrement();
				}
				if (array != null)
				{
					array[i] = dataColumn.Ordinal;
				}
				else if (i != dataColumn.Ordinal)
				{
					array = CreateIndexMap(fieldCount, i);
					array[i] = dataColumn.Ordinal;
				}
				num++;
			}
			bool flag3 = false;
			DataColumn dataColumn2 = null;
			if (chapterValue != null)
			{
				Type type2 = chapterValue.GetType();
				dataColumn2 = _tableMapping.GetDataColumn(_tableMapping.SourceTable, type2, _dataTable, mappingAction, schemaAction);
				if (dataColumn2 != null)
				{
					if (dataColumn2.Table == null)
					{
						AddItemToAllowRollback(ref items, dataColumn2);
						columns.Add(dataColumn2);
						flag3 = parentChapterColumn != null;
					}
					num++;
				}
			}
			if (0 < num)
			{
				if (_dataSet != null && _dataTable.DataSet == null)
				{
					AddItemToAllowRollback(ref items, _dataTable);
					_dataSet.Tables.Add(_dataTable);
				}
				if (gettingData)
				{
					if (columns == null)
					{
						columns = _dataTable.Columns;
					}
					_indexMap = array;
					_chapterMap = array2;
					result = SetupMapping(fieldCount, columns, dataColumn2, chapterValue);
				}
				else
				{
					_mappedMode = -1;
				}
			}
			else
			{
				_dataTable = null;
			}
			if (flag3)
			{
				AddRelation(parentChapterColumn, dataColumn2);
			}
		}
		catch (Exception e) when (ADP.IsCatchableOrSecurityExceptionType(e))
		{
			RollbackAddedItems(items);
			throw;
		}
		return result;
	}

	[RequiresUnreferencedCode("chapterValue and _schemaTable schema rows DataTypes type cannot be statically analyzed. When _loadOption is set, members from types used in the expression column may be trimmed if not referenced directly.")]
	private object[] SetupSchemaWithKeyInfo(MissingMappingAction mappingAction, MissingSchemaAction schemaAction, bool gettingData, DataColumn parentChapterColumn, object chapterValue)
	{
		DbSchemaRow[] sortedSchemaRows = DbSchemaRow.GetSortedSchemaRows(_schemaTable, _dataReader.ReturnProviderSpecificTypes);
		if (sortedSchemaRows.Length == 0)
		{
			_dataTable = null;
			return null;
		}
		bool flag = (_dataTable.PrimaryKey.Length == 0 && ((LoadOption)4 <= _loadOption || _dataTable.Rows.Count == 0)) || _dataTable.Columns.Count == 0;
		DataColumn[] array = null;
		int num = 0;
		bool flag2 = true;
		string text = null;
		string text2 = null;
		bool flag3 = false;
		bool flag4 = false;
		int[] array2 = null;
		bool[] array3 = null;
		int num2 = 0;
		object[] result = null;
		List<object> items = null;
		DataColumnCollection columns = _dataTable.Columns;
		try
		{
			for (int i = 0; i < sortedSchemaRows.Length; i++)
			{
				DbSchemaRow dbSchemaRow = sortedSchemaRows[i];
				int unsortedIndex = dbSchemaRow.UnsortedIndex;
				bool flag5 = false;
				Type type = dbSchemaRow.DataType;
				if (null == type)
				{
					type = _dataReader.GetFieldType(i);
				}
				if (null == type)
				{
					throw ADP.MissingDataReaderFieldType(i);
				}
				if (typeof(IDataReader).IsAssignableFrom(type))
				{
					if (array3 == null)
					{
						array3 = new bool[sortedSchemaRows.Length];
					}
					flag5 = (array3[unsortedIndex] = true);
					type = typeof(int);
				}
				else if (typeof(SqlXml).IsAssignableFrom(type))
				{
					if (_xmlMap == null)
					{
						_xmlMap = new int[sortedSchemaRows.Length];
					}
					_xmlMap[i] = 1;
				}
				else if (typeof(XmlReader).IsAssignableFrom(type))
				{
					type = typeof(string);
					if (_xmlMap == null)
					{
						_xmlMap = new int[sortedSchemaRows.Length];
					}
					_xmlMap[i] = 2;
				}
				DataColumn dataColumn = null;
				if (!dbSchemaRow.IsHidden)
				{
					dataColumn = _tableMapping.GetDataColumn(_fieldNames[i], type, _dataTable, mappingAction, schemaAction);
				}
				string baseTableName = dbSchemaRow.BaseTableName;
				if (dataColumn == null)
				{
					if (array2 == null)
					{
						array2 = CreateIndexMap(sortedSchemaRows.Length, unsortedIndex);
					}
					array2[unsortedIndex] = -1;
					if (dbSchemaRow.IsKey && (flag3 || dbSchemaRow.BaseTableName == text))
					{
						flag = false;
						array = null;
					}
					continue;
				}
				if (_xmlMap != null && _xmlMap[i] != 0)
				{
					if (typeof(SqlXml) == dataColumn.DataType)
					{
						_xmlMap[i] = 1;
					}
					else if (typeof(XmlDocument) == dataColumn.DataType)
					{
						_xmlMap[i] = 2;
					}
					else
					{
						_xmlMap[i] = 0;
						int num3 = 0;
						for (int j = 0; j < _xmlMap.Length; j++)
						{
							num3 += _xmlMap[j];
						}
						if (num3 == 0)
						{
							_xmlMap = null;
						}
					}
				}
				if (dbSchemaRow.IsKey && baseTableName != text)
				{
					if (text == null)
					{
						text = baseTableName;
					}
					else
					{
						flag3 = true;
					}
				}
				if (flag5)
				{
					if (dataColumn.Table == null)
					{
						dataColumn.AllowDBNull = false;
						dataColumn.AutoIncrement = true;
						dataColumn.ReadOnly = true;
					}
					else if (!dataColumn.AutoIncrement)
					{
						throw ADP.FillChapterAutoIncrement();
					}
				}
				else
				{
					if (!flag4 && baseTableName != text2 && !string.IsNullOrEmpty(baseTableName))
					{
						if (text2 == null)
						{
							text2 = baseTableName;
						}
						else
						{
							flag4 = true;
						}
					}
					if ((LoadOption)4 <= _loadOption)
					{
						if (dbSchemaRow.IsAutoIncrement && DataColumn.IsAutoIncrementType(type))
						{
							dataColumn.AutoIncrement = true;
							if (!dbSchemaRow.AllowDBNull)
							{
								dataColumn.AllowDBNull = false;
							}
						}
						if (type == typeof(string))
						{
							dataColumn.MaxLength = ((dbSchemaRow.Size > 0) ? dbSchemaRow.Size : (-1));
						}
						if (dbSchemaRow.IsReadOnly)
						{
							dataColumn.ReadOnly = true;
						}
						if (!dbSchemaRow.AllowDBNull && (!dbSchemaRow.IsReadOnly || dbSchemaRow.IsKey))
						{
							dataColumn.AllowDBNull = false;
						}
						if (dbSchemaRow.IsUnique && !dbSchemaRow.IsKey && !type.IsArray)
						{
							dataColumn.Unique = true;
							if (!dbSchemaRow.AllowDBNull)
							{
								dataColumn.AllowDBNull = false;
							}
						}
					}
					else if (dataColumn.Table == null)
					{
						dataColumn.AutoIncrement = dbSchemaRow.IsAutoIncrement;
						dataColumn.AllowDBNull = dbSchemaRow.AllowDBNull;
						dataColumn.ReadOnly = dbSchemaRow.IsReadOnly;
						dataColumn.Unique = dbSchemaRow.IsUnique;
						if (type == typeof(string) || type == typeof(SqlString))
						{
							dataColumn.MaxLength = dbSchemaRow.Size;
						}
					}
				}
				if (dataColumn.Table == null)
				{
					if ((LoadOption)4 > _loadOption)
					{
						AddAdditionalProperties(dataColumn, dbSchemaRow.DataRow);
					}
					AddItemToAllowRollback(ref items, dataColumn);
					columns.Add(dataColumn);
				}
				if (flag && dbSchemaRow.IsKey)
				{
					if (array == null)
					{
						array = new DataColumn[sortedSchemaRows.Length];
					}
					array[num++] = dataColumn;
					if (flag2 && dataColumn.AllowDBNull)
					{
						flag2 = false;
					}
				}
				if (array2 != null)
				{
					array2[unsortedIndex] = dataColumn.Ordinal;
				}
				else if (unsortedIndex != dataColumn.Ordinal)
				{
					array2 = CreateIndexMap(sortedSchemaRows.Length, unsortedIndex);
					array2[unsortedIndex] = dataColumn.Ordinal;
				}
				num2++;
			}
			bool flag6 = false;
			DataColumn dataColumn2 = null;
			if (chapterValue != null)
			{
				Type type2 = chapterValue.GetType();
				dataColumn2 = _tableMapping.GetDataColumn(_tableMapping.SourceTable, type2, _dataTable, mappingAction, schemaAction);
				if (dataColumn2 != null)
				{
					if (dataColumn2.Table == null)
					{
						dataColumn2.ReadOnly = true;
						dataColumn2.AllowDBNull = false;
						AddItemToAllowRollback(ref items, dataColumn2);
						columns.Add(dataColumn2);
						flag6 = parentChapterColumn != null;
					}
					num2++;
				}
			}
			if (0 < num2)
			{
				if (_dataSet != null && _dataTable.DataSet == null)
				{
					AddItemToAllowRollback(ref items, _dataTable);
					_dataSet.Tables.Add(_dataTable);
				}
				if (flag && array != null)
				{
					if (num < array.Length)
					{
						array = ResizeColumnArray(array, num);
					}
					if (flag2)
					{
						_dataTable.PrimaryKey = array;
					}
					else
					{
						UniqueConstraint uniqueConstraint = new UniqueConstraint("", array);
						ConstraintCollection constraints = _dataTable.Constraints;
						int count = constraints.Count;
						for (int k = 0; k < count; k++)
						{
							if (uniqueConstraint.Equals(constraints[k]))
							{
								uniqueConstraint = null;
								break;
							}
						}
						if (uniqueConstraint != null)
						{
							constraints.Add(uniqueConstraint);
						}
					}
				}
				if (!flag4 && !string.IsNullOrEmpty(text2) && string.IsNullOrEmpty(_dataTable.TableName))
				{
					_dataTable.TableName = text2;
				}
				if (gettingData)
				{
					_indexMap = array2;
					_chapterMap = array3;
					result = SetupMapping(sortedSchemaRows.Length, columns, dataColumn2, chapterValue);
				}
				else
				{
					_mappedMode = -1;
				}
			}
			else
			{
				_dataTable = null;
			}
			if (flag6)
			{
				AddRelation(parentChapterColumn, dataColumn2);
			}
		}
		catch (Exception e) when (ADP.IsCatchableOrSecurityExceptionType(e))
		{
			RollbackAddedItems(items);
			throw;
		}
		return result;
	}

	[RequiresUnreferencedCode("Members from types used in the expression column may be trimmed if not referenced directly.")]
	private void AddAdditionalProperties(DataColumn targetColumn, DataRow schemaRow)
	{
		DataColumnCollection columns = schemaRow.Table.Columns;
		DataColumn dataColumn = columns[SchemaTableOptionalColumn.DefaultValue];
		if (dataColumn != null)
		{
			targetColumn.DefaultValue = schemaRow[dataColumn];
		}
		dataColumn = columns[SchemaTableOptionalColumn.AutoIncrementSeed];
		if (dataColumn != null)
		{
			object obj = schemaRow[dataColumn];
			if (DBNull.Value != obj)
			{
				targetColumn.AutoIncrementSeed = ((IConvertible)obj).ToInt64(CultureInfo.InvariantCulture);
			}
		}
		dataColumn = columns[SchemaTableOptionalColumn.AutoIncrementStep];
		if (dataColumn != null)
		{
			object obj2 = schemaRow[dataColumn];
			if (DBNull.Value != obj2)
			{
				targetColumn.AutoIncrementStep = ((IConvertible)obj2).ToInt64(CultureInfo.InvariantCulture);
			}
		}
		dataColumn = columns[SchemaTableOptionalColumn.ColumnMapping];
		if (dataColumn != null)
		{
			object obj3 = schemaRow[dataColumn];
			if (DBNull.Value != obj3)
			{
				targetColumn.ColumnMapping = (MappingType)((IConvertible)obj3).ToInt32(CultureInfo.InvariantCulture);
			}
		}
		dataColumn = columns[SchemaTableOptionalColumn.BaseColumnNamespace];
		if (dataColumn != null)
		{
			object obj4 = schemaRow[dataColumn];
			if (DBNull.Value != obj4)
			{
				targetColumn.Namespace = ((IConvertible)obj4).ToString(CultureInfo.InvariantCulture);
			}
		}
		dataColumn = columns[SchemaTableOptionalColumn.Expression];
		if (dataColumn != null)
		{
			object obj5 = schemaRow[dataColumn];
			if (DBNull.Value != obj5)
			{
				targetColumn.Expression = ((IConvertible)obj5).ToString(CultureInfo.InvariantCulture);
			}
		}
	}

	private void AddRelation(DataColumn parentChapterColumn, DataColumn chapterColumn)
	{
		if (_dataSet != null)
		{
			string columnName = chapterColumn.ColumnName;
			DataRelation dataRelation = new DataRelation(columnName, new DataColumn[1] { parentChapterColumn }, new DataColumn[1] { chapterColumn }, createConstraints: false);
			int num = 1;
			string relationName = columnName;
			DataRelationCollection relations = _dataSet.Relations;
			while (-1 != relations.IndexOf(relationName))
			{
				relationName = columnName + num;
				num++;
			}
			dataRelation.RelationName = relationName;
			relations.Add(dataRelation);
		}
	}

	private object[] SetupMapping(int count, DataColumnCollection columnCollection, DataColumn chapterColumn, object chapterValue)
	{
		object[] result = new object[count];
		if (_indexMap == null)
		{
			int count2 = columnCollection.Count;
			bool flag = _chapterMap != null;
			if (count != count2 || flag)
			{
				_mappedDataValues = new object[count2];
				if (flag)
				{
					_mappedMode = 3;
					_mappedLength = count;
				}
				else
				{
					_mappedMode = 1;
					_mappedLength = Math.Min(count, count2);
				}
			}
			else
			{
				_mappedMode = 0;
			}
		}
		else
		{
			_mappedDataValues = new object[columnCollection.Count];
			_mappedMode = ((_chapterMap == null) ? 2 : 4);
			_mappedLength = count;
		}
		if (chapterColumn != null)
		{
			_mappedDataValues[chapterColumn.Ordinal] = chapterValue;
		}
		return result;
	}
}
