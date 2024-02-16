using System.Collections;
using System.Data.Common;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;

namespace System.Data;

public sealed class DataTableReader : DbDataReader
{
	private readonly DataTable[] _tables;

	private bool _isOpen = true;

	private DataTable _schemaTable;

	private int _tableCounter = -1;

	private int _rowCounter = -1;

	private DataTable _currentDataTable;

	private DataRow _currentDataRow;

	private bool _hasRows = true;

	private bool _reachEORows;

	private bool _currentRowRemoved;

	private bool _schemaIsChanged;

	private bool _started;

	private bool _readerIsInvalid;

	private DataTableReaderListener _listener;

	private bool _tableCleared;

	private bool ReaderIsInvalid
	{
		get
		{
			return _readerIsInvalid;
		}
		set
		{
			if (_readerIsInvalid != value)
			{
				_readerIsInvalid = value;
				if (_readerIsInvalid && _listener != null)
				{
					_listener.CleanUp();
				}
			}
		}
	}

	private bool IsSchemaChanged
	{
		get
		{
			return _schemaIsChanged;
		}
		set
		{
			if (value && _schemaIsChanged != value)
			{
				_schemaIsChanged = value;
				if (_listener != null)
				{
					_listener.CleanUp();
				}
			}
		}
	}

	internal DataTable CurrentDataTable => _currentDataTable;

	public override int Depth
	{
		get
		{
			ValidateOpen("Depth");
			ValidateReader();
			return 0;
		}
	}

	public override bool IsClosed => !_isOpen;

	public override int RecordsAffected
	{
		get
		{
			ValidateReader();
			return 0;
		}
	}

	public override bool HasRows
	{
		get
		{
			ValidateOpen("HasRows");
			ValidateReader();
			return _hasRows;
		}
	}

	public override object this[int ordinal]
	{
		get
		{
			ValidateOpen("Item");
			ValidateReader();
			if (_currentDataRow == null || _currentDataRow.RowState == DataRowState.Deleted)
			{
				ReaderIsInvalid = true;
				throw ExceptionBuilder.InvalidDataTableReader(_currentDataTable.TableName);
			}
			try
			{
				return _currentDataRow[ordinal];
			}
			catch (IndexOutOfRangeException e)
			{
				ExceptionBuilder.TraceExceptionWithoutRethrow(e);
				throw ExceptionBuilder.ArgumentOutOfRange("ordinal");
			}
		}
	}

	public override object this[string name]
	{
		get
		{
			ValidateOpen("Item");
			ValidateReader();
			if (_currentDataRow == null || _currentDataRow.RowState == DataRowState.Deleted)
			{
				ReaderIsInvalid = true;
				throw ExceptionBuilder.InvalidDataTableReader(_currentDataTable.TableName);
			}
			return _currentDataRow[name];
		}
	}

	public override int FieldCount
	{
		get
		{
			ValidateOpen("FieldCount");
			ValidateReader();
			return _currentDataTable.Columns.Count;
		}
	}

	public DataTableReader(DataTable dataTable)
	{
		if (dataTable == null)
		{
			throw ExceptionBuilder.ArgumentNull("DataTable");
		}
		_tables = new DataTable[1] { dataTable };
		Init();
	}

	public DataTableReader(DataTable[] dataTables)
	{
		if (dataTables == null)
		{
			throw ExceptionBuilder.ArgumentNull("DataTable");
		}
		if (dataTables.Length == 0)
		{
			throw ExceptionBuilder.DataTableReaderArgumentIsEmpty();
		}
		_tables = new DataTable[dataTables.Length];
		for (int i = 0; i < dataTables.Length; i++)
		{
			if (dataTables[i] == null)
			{
				throw ExceptionBuilder.ArgumentNull("DataTable");
			}
			_tables[i] = dataTables[i];
		}
		Init();
	}

	private void Init()
	{
		_tableCounter = 0;
		_reachEORows = false;
		_schemaIsChanged = false;
		_currentDataTable = _tables[_tableCounter];
		_hasRows = _currentDataTable.Rows.Count > 0;
		ReaderIsInvalid = false;
		_listener = new DataTableReaderListener(this);
	}

	public override void Close()
	{
		if (_isOpen)
		{
			if (_listener != null)
			{
				_listener.CleanUp();
			}
			_listener = null;
			_schemaTable = null;
			_isOpen = false;
		}
	}

	public override DataTable GetSchemaTable()
	{
		ValidateOpen("GetSchemaTable");
		ValidateReader();
		if (_schemaTable == null)
		{
			_schemaTable = GetSchemaTableFromDataTable(_currentDataTable);
		}
		return _schemaTable;
	}

	public override bool NextResult()
	{
		ValidateOpen("NextResult");
		if (_tableCounter == _tables.Length - 1)
		{
			return false;
		}
		_currentDataTable = _tables[++_tableCounter];
		if (_listener != null)
		{
			_listener.UpdataTable(_currentDataTable);
		}
		_schemaTable = null;
		_rowCounter = -1;
		_currentRowRemoved = false;
		_reachEORows = false;
		_schemaIsChanged = false;
		_started = false;
		ReaderIsInvalid = false;
		_tableCleared = false;
		_hasRows = _currentDataTable.Rows.Count > 0;
		return true;
	}

	public override bool Read()
	{
		if (!_started)
		{
			_started = true;
		}
		ValidateOpen("Read");
		ValidateReader();
		if (_reachEORows)
		{
			return false;
		}
		if (_rowCounter >= _currentDataTable.Rows.Count - 1)
		{
			_reachEORows = true;
			if (_listener != null)
			{
				_listener.CleanUp();
			}
			return false;
		}
		_rowCounter++;
		ValidateRow(_rowCounter);
		_currentDataRow = _currentDataTable.Rows[_rowCounter];
		while (_currentDataRow.RowState == DataRowState.Deleted)
		{
			_rowCounter++;
			if (_rowCounter == _currentDataTable.Rows.Count)
			{
				_reachEORows = true;
				if (_listener != null)
				{
					_listener.CleanUp();
				}
				return false;
			}
			ValidateRow(_rowCounter);
			_currentDataRow = _currentDataTable.Rows[_rowCounter];
		}
		if (_currentRowRemoved)
		{
			_currentRowRemoved = false;
		}
		return true;
	}

	[return: DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicFields | DynamicallyAccessedMemberTypes.PublicProperties)]
	public override Type GetProviderSpecificFieldType(int ordinal)
	{
		ValidateOpen("GetProviderSpecificFieldType");
		ValidateReader();
		return GetFieldType(ordinal);
	}

	public override object GetProviderSpecificValue(int ordinal)
	{
		ValidateOpen("GetProviderSpecificValue");
		ValidateReader();
		return GetValue(ordinal);
	}

	public override int GetProviderSpecificValues(object[] values)
	{
		ValidateOpen("GetProviderSpecificValues");
		ValidateReader();
		return GetValues(values);
	}

	public override bool GetBoolean(int ordinal)
	{
		ValidateState("GetBoolean");
		ValidateReader();
		try
		{
			return (bool)_currentDataRow[ordinal];
		}
		catch (IndexOutOfRangeException e)
		{
			ExceptionBuilder.TraceExceptionWithoutRethrow(e);
			throw ExceptionBuilder.ArgumentOutOfRange("ordinal");
		}
	}

	public override byte GetByte(int ordinal)
	{
		ValidateState("GetByte");
		ValidateReader();
		try
		{
			return (byte)_currentDataRow[ordinal];
		}
		catch (IndexOutOfRangeException e)
		{
			ExceptionBuilder.TraceExceptionWithoutRethrow(e);
			throw ExceptionBuilder.ArgumentOutOfRange("ordinal");
		}
	}

	public override long GetBytes(int ordinal, long dataIndex, byte[]? buffer, int bufferIndex, int length)
	{
		ValidateState("GetBytes");
		ValidateReader();
		byte[] array;
		try
		{
			array = (byte[])_currentDataRow[ordinal];
		}
		catch (IndexOutOfRangeException e)
		{
			ExceptionBuilder.TraceExceptionWithoutRethrow(e);
			throw ExceptionBuilder.ArgumentOutOfRange("ordinal");
		}
		if (buffer == null)
		{
			return array.Length;
		}
		int num = (int)dataIndex;
		int num2 = Math.Min(array.Length - num, length);
		if (num < 0)
		{
			throw ADP.InvalidSourceBufferIndex(array.Length, num, "dataIndex");
		}
		if (bufferIndex < 0 || (bufferIndex > 0 && bufferIndex >= buffer.Length))
		{
			throw ADP.InvalidDestinationBufferIndex(buffer.Length, bufferIndex, "bufferIndex");
		}
		if (0 < num2)
		{
			Array.Copy(array, dataIndex, buffer, bufferIndex, num2);
		}
		else
		{
			if (length < 0)
			{
				throw ADP.InvalidDataLength(length);
			}
			num2 = 0;
		}
		return num2;
	}

	public override char GetChar(int ordinal)
	{
		ValidateState("GetChar");
		ValidateReader();
		try
		{
			return (char)_currentDataRow[ordinal];
		}
		catch (IndexOutOfRangeException e)
		{
			ExceptionBuilder.TraceExceptionWithoutRethrow(e);
			throw ExceptionBuilder.ArgumentOutOfRange("ordinal");
		}
	}

	public override long GetChars(int ordinal, long dataIndex, char[]? buffer, int bufferIndex, int length)
	{
		ValidateState("GetChars");
		ValidateReader();
		char[] array;
		try
		{
			array = (char[])_currentDataRow[ordinal];
		}
		catch (IndexOutOfRangeException e)
		{
			ExceptionBuilder.TraceExceptionWithoutRethrow(e);
			throw ExceptionBuilder.ArgumentOutOfRange("ordinal");
		}
		if (buffer == null)
		{
			return array.Length;
		}
		int num = (int)dataIndex;
		int num2 = Math.Min(array.Length - num, length);
		if (num < 0)
		{
			throw ADP.InvalidSourceBufferIndex(array.Length, num, "dataIndex");
		}
		if (bufferIndex < 0 || (bufferIndex > 0 && bufferIndex >= buffer.Length))
		{
			throw ADP.InvalidDestinationBufferIndex(buffer.Length, bufferIndex, "bufferIndex");
		}
		if (0 < num2)
		{
			Array.Copy(array, dataIndex, buffer, bufferIndex, num2);
		}
		else
		{
			if (length < 0)
			{
				throw ADP.InvalidDataLength(length);
			}
			num2 = 0;
		}
		return num2;
	}

	public override string GetDataTypeName(int ordinal)
	{
		ValidateOpen("GetDataTypeName");
		ValidateReader();
		return GetFieldType(ordinal).Name;
	}

	public override DateTime GetDateTime(int ordinal)
	{
		ValidateState("GetDateTime");
		ValidateReader();
		try
		{
			return (DateTime)_currentDataRow[ordinal];
		}
		catch (IndexOutOfRangeException e)
		{
			ExceptionBuilder.TraceExceptionWithoutRethrow(e);
			throw ExceptionBuilder.ArgumentOutOfRange("ordinal");
		}
	}

	public override decimal GetDecimal(int ordinal)
	{
		ValidateState("GetDecimal");
		ValidateReader();
		try
		{
			return (decimal)_currentDataRow[ordinal];
		}
		catch (IndexOutOfRangeException e)
		{
			ExceptionBuilder.TraceExceptionWithoutRethrow(e);
			throw ExceptionBuilder.ArgumentOutOfRange("ordinal");
		}
	}

	public override double GetDouble(int ordinal)
	{
		ValidateState("GetDouble");
		ValidateReader();
		try
		{
			return (double)_currentDataRow[ordinal];
		}
		catch (IndexOutOfRangeException e)
		{
			ExceptionBuilder.TraceExceptionWithoutRethrow(e);
			throw ExceptionBuilder.ArgumentOutOfRange("ordinal");
		}
	}

	[return: DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicFields | DynamicallyAccessedMemberTypes.PublicProperties)]
	public override Type GetFieldType(int ordinal)
	{
		ValidateOpen("GetFieldType");
		ValidateReader();
		try
		{
			return _currentDataTable.Columns[ordinal].DataType;
		}
		catch (IndexOutOfRangeException e)
		{
			ExceptionBuilder.TraceExceptionWithoutRethrow(e);
			throw ExceptionBuilder.ArgumentOutOfRange("ordinal");
		}
	}

	public override float GetFloat(int ordinal)
	{
		ValidateState("GetFloat");
		ValidateReader();
		try
		{
			return (float)_currentDataRow[ordinal];
		}
		catch (IndexOutOfRangeException e)
		{
			ExceptionBuilder.TraceExceptionWithoutRethrow(e);
			throw ExceptionBuilder.ArgumentOutOfRange("ordinal");
		}
	}

	public override Guid GetGuid(int ordinal)
	{
		ValidateState("GetGuid");
		ValidateReader();
		try
		{
			return (Guid)_currentDataRow[ordinal];
		}
		catch (IndexOutOfRangeException e)
		{
			ExceptionBuilder.TraceExceptionWithoutRethrow(e);
			throw ExceptionBuilder.ArgumentOutOfRange("ordinal");
		}
	}

	public override short GetInt16(int ordinal)
	{
		ValidateState("GetInt16");
		ValidateReader();
		try
		{
			return (short)_currentDataRow[ordinal];
		}
		catch (IndexOutOfRangeException e)
		{
			ExceptionBuilder.TraceExceptionWithoutRethrow(e);
			throw ExceptionBuilder.ArgumentOutOfRange("ordinal");
		}
	}

	public override int GetInt32(int ordinal)
	{
		ValidateState("GetInt32");
		ValidateReader();
		try
		{
			return (int)_currentDataRow[ordinal];
		}
		catch (IndexOutOfRangeException e)
		{
			ExceptionBuilder.TraceExceptionWithoutRethrow(e);
			throw ExceptionBuilder.ArgumentOutOfRange("ordinal");
		}
	}

	public override long GetInt64(int ordinal)
	{
		ValidateState("GetInt64");
		ValidateReader();
		try
		{
			return (long)_currentDataRow[ordinal];
		}
		catch (IndexOutOfRangeException e)
		{
			ExceptionBuilder.TraceExceptionWithoutRethrow(e);
			throw ExceptionBuilder.ArgumentOutOfRange("ordinal");
		}
	}

	public override string GetName(int ordinal)
	{
		ValidateOpen("GetName");
		ValidateReader();
		try
		{
			return _currentDataTable.Columns[ordinal].ColumnName;
		}
		catch (IndexOutOfRangeException e)
		{
			ExceptionBuilder.TraceExceptionWithoutRethrow(e);
			throw ExceptionBuilder.ArgumentOutOfRange("ordinal");
		}
	}

	public override int GetOrdinal(string name)
	{
		ValidateOpen("GetOrdinal");
		ValidateReader();
		DataColumn dataColumn = _currentDataTable.Columns[name];
		if (dataColumn != null)
		{
			return dataColumn.Ordinal;
		}
		throw ExceptionBuilder.ColumnNotInTheTable(name, _currentDataTable.TableName);
	}

	public override string GetString(int ordinal)
	{
		ValidateState("GetString");
		ValidateReader();
		try
		{
			return (string)_currentDataRow[ordinal];
		}
		catch (IndexOutOfRangeException e)
		{
			ExceptionBuilder.TraceExceptionWithoutRethrow(e);
			throw ExceptionBuilder.ArgumentOutOfRange("ordinal");
		}
	}

	public override object GetValue(int ordinal)
	{
		ValidateState("GetValue");
		ValidateReader();
		try
		{
			return _currentDataRow[ordinal];
		}
		catch (IndexOutOfRangeException e)
		{
			ExceptionBuilder.TraceExceptionWithoutRethrow(e);
			throw ExceptionBuilder.ArgumentOutOfRange("ordinal");
		}
	}

	public override int GetValues(object[] values)
	{
		ValidateState("GetValues");
		ValidateReader();
		if (values == null)
		{
			throw ExceptionBuilder.ArgumentNull("values");
		}
		Array.Copy(_currentDataRow.ItemArray, values, (_currentDataRow.ItemArray.Length > values.Length) ? values.Length : _currentDataRow.ItemArray.Length);
		if (_currentDataRow.ItemArray.Length <= values.Length)
		{
			return _currentDataRow.ItemArray.Length;
		}
		return values.Length;
	}

	public override bool IsDBNull(int ordinal)
	{
		ValidateState("IsDBNull");
		ValidateReader();
		try
		{
			return _currentDataRow.IsNull(ordinal);
		}
		catch (IndexOutOfRangeException e)
		{
			ExceptionBuilder.TraceExceptionWithoutRethrow(e);
			throw ExceptionBuilder.ArgumentOutOfRange("ordinal");
		}
	}

	public override IEnumerator GetEnumerator()
	{
		ValidateOpen("GetEnumerator");
		return new DbEnumerator((IDataReader)this);
	}

	internal static DataTable GetSchemaTableFromDataTable(DataTable table)
	{
		if (table == null)
		{
			throw ExceptionBuilder.ArgumentNull("DataTable");
		}
		DataTable dataTable = new DataTable("SchemaTable");
		dataTable.Locale = CultureInfo.InvariantCulture;
		DataColumn column = new DataColumn(SchemaTableColumn.ColumnName, typeof(string));
		DataColumn column2 = new DataColumn(SchemaTableColumn.ColumnOrdinal, typeof(int));
		DataColumn dataColumn = new DataColumn(SchemaTableColumn.ColumnSize, typeof(int));
		DataColumn column3 = new DataColumn(SchemaTableColumn.NumericPrecision, typeof(short));
		DataColumn column4 = new DataColumn(SchemaTableColumn.NumericScale, typeof(short));
		DataColumn column5 = GetSystemTypeDataColumn();
		DataColumn column6 = new DataColumn(SchemaTableColumn.ProviderType, typeof(int));
		DataColumn dataColumn2 = new DataColumn(SchemaTableColumn.IsLong, typeof(bool));
		DataColumn column7 = new DataColumn(SchemaTableColumn.AllowDBNull, typeof(bool));
		DataColumn dataColumn3 = new DataColumn(SchemaTableOptionalColumn.IsReadOnly, typeof(bool));
		DataColumn dataColumn4 = new DataColumn(SchemaTableOptionalColumn.IsRowVersion, typeof(bool));
		DataColumn column8 = new DataColumn(SchemaTableColumn.IsUnique, typeof(bool));
		DataColumn dataColumn5 = new DataColumn(SchemaTableColumn.IsKey, typeof(bool));
		DataColumn dataColumn6 = new DataColumn(SchemaTableOptionalColumn.IsAutoIncrement, typeof(bool));
		DataColumn column9 = new DataColumn(SchemaTableColumn.BaseSchemaName, typeof(string));
		DataColumn dataColumn7 = new DataColumn(SchemaTableOptionalColumn.BaseCatalogName, typeof(string));
		DataColumn dataColumn8 = new DataColumn(SchemaTableColumn.BaseTableName, typeof(string));
		DataColumn column10 = new DataColumn(SchemaTableColumn.BaseColumnName, typeof(string));
		DataColumn dataColumn9 = new DataColumn(SchemaTableOptionalColumn.AutoIncrementSeed, typeof(long));
		DataColumn dataColumn10 = new DataColumn(SchemaTableOptionalColumn.AutoIncrementStep, typeof(long));
		DataColumn column11 = new DataColumn(SchemaTableOptionalColumn.DefaultValue, typeof(object));
		DataColumn column12 = new DataColumn(SchemaTableOptionalColumn.Expression, typeof(string));
		DataColumn column13 = new DataColumn(SchemaTableOptionalColumn.ColumnMapping, typeof(MappingType));
		DataColumn dataColumn11 = new DataColumn(SchemaTableOptionalColumn.BaseTableNamespace, typeof(string));
		DataColumn column14 = new DataColumn(SchemaTableOptionalColumn.BaseColumnNamespace, typeof(string));
		dataColumn.DefaultValue = -1;
		if (table.DataSet != null)
		{
			dataColumn7.DefaultValue = table.DataSet.DataSetName;
		}
		dataColumn8.DefaultValue = table.TableName;
		dataColumn11.DefaultValue = table.Namespace;
		dataColumn4.DefaultValue = false;
		dataColumn2.DefaultValue = false;
		dataColumn3.DefaultValue = false;
		dataColumn5.DefaultValue = false;
		dataColumn6.DefaultValue = false;
		dataColumn9.DefaultValue = 0;
		dataColumn10.DefaultValue = 1;
		dataTable.Columns.Add(column);
		dataTable.Columns.Add(column2);
		dataTable.Columns.Add(dataColumn);
		dataTable.Columns.Add(column3);
		dataTable.Columns.Add(column4);
		dataTable.Columns.Add(column5);
		dataTable.Columns.Add(column6);
		dataTable.Columns.Add(dataColumn2);
		dataTable.Columns.Add(column7);
		dataTable.Columns.Add(dataColumn3);
		dataTable.Columns.Add(dataColumn4);
		dataTable.Columns.Add(column8);
		dataTable.Columns.Add(dataColumn5);
		dataTable.Columns.Add(dataColumn6);
		dataTable.Columns.Add(dataColumn7);
		dataTable.Columns.Add(column9);
		dataTable.Columns.Add(dataColumn8);
		dataTable.Columns.Add(column10);
		dataTable.Columns.Add(dataColumn9);
		dataTable.Columns.Add(dataColumn10);
		dataTable.Columns.Add(column11);
		dataTable.Columns.Add(column12);
		dataTable.Columns.Add(column13);
		dataTable.Columns.Add(dataColumn11);
		dataTable.Columns.Add(column14);
		foreach (DataColumn column15 in table.Columns)
		{
			DataRow dataRow = dataTable.NewRow();
			dataRow[column] = column15.ColumnName;
			dataRow[column2] = column15.Ordinal;
			dataRow[column5] = column15.DataType;
			if (column15.DataType == typeof(string))
			{
				dataRow[dataColumn] = column15.MaxLength;
			}
			dataRow[column7] = column15.AllowDBNull;
			dataRow[dataColumn3] = column15.ReadOnly;
			dataRow[column8] = column15.Unique;
			if (column15.AutoIncrement)
			{
				dataRow[dataColumn6] = true;
				dataRow[dataColumn9] = column15.AutoIncrementSeed;
				dataRow[dataColumn10] = column15.AutoIncrementStep;
			}
			if (column15.DefaultValue != DBNull.Value)
			{
				dataRow[column11] = column15.DefaultValue;
			}
			if (column15.Expression.Length != 0)
			{
				bool flag = false;
				DataColumn[] dependency = column15.DataExpression.GetDependency();
				for (int i = 0; i < dependency.Length; i++)
				{
					if (dependency[i].Table != table)
					{
						flag = true;
						break;
					}
				}
				if (!flag)
				{
					dataRow[column12] = column15.Expression;
				}
			}
			dataRow[column13] = column15.ColumnMapping;
			dataRow[column10] = column15.ColumnName;
			dataRow[column14] = column15.Namespace;
			dataTable.Rows.Add(dataRow);
		}
		DataColumn[] primaryKey = table.PrimaryKey;
		foreach (DataColumn dataColumn13 in primaryKey)
		{
			dataTable.Rows[dataColumn13.Ordinal][dataColumn5] = true;
		}
		dataTable.AcceptChanges();
		return dataTable;
		[UnconditionalSuppressMessage("ReflectionAnalysis", "IL2111:ReflectionToDynamicallyAccessedMembers", Justification = "The problem is Type.TypeInitializer which requires constructors on the Type instance.In this case the TypeInitializer property is not accessed dynamically.")]
		static DataColumn GetSystemTypeDataColumn()
		{
			return new DataColumn(SchemaTableColumn.DataType, typeof(Type));
		}
	}

	private void ValidateOpen(string caller)
	{
		if (!_isOpen)
		{
			throw ADP.DataReaderClosed(caller);
		}
	}

	private void ValidateReader()
	{
		if (ReaderIsInvalid)
		{
			throw ExceptionBuilder.InvalidDataTableReader(_currentDataTable.TableName);
		}
		if (IsSchemaChanged)
		{
			throw ExceptionBuilder.DataTableReaderSchemaIsInvalid(_currentDataTable.TableName);
		}
	}

	private void ValidateState(string caller)
	{
		ValidateOpen(caller);
		if (_tableCleared)
		{
			throw ExceptionBuilder.EmptyDataTableReader(_currentDataTable.TableName);
		}
		if (_currentDataRow == null || _currentDataTable == null)
		{
			ReaderIsInvalid = true;
			throw ExceptionBuilder.InvalidDataTableReader(_currentDataTable.TableName);
		}
		if (_currentDataRow.RowState == DataRowState.Deleted || _currentDataRow.RowState == DataRowState.Detached || _currentRowRemoved)
		{
			throw ExceptionBuilder.InvalidCurrentRowInDataTableReader();
		}
		if (0 > _rowCounter || _currentDataTable.Rows.Count <= _rowCounter)
		{
			ReaderIsInvalid = true;
			throw ExceptionBuilder.InvalidDataTableReader(_currentDataTable.TableName);
		}
	}

	private void ValidateRow(int rowPosition)
	{
		if (ReaderIsInvalid)
		{
			throw ExceptionBuilder.InvalidDataTableReader(_currentDataTable.TableName);
		}
		if (0 > rowPosition || _currentDataTable.Rows.Count <= rowPosition)
		{
			ReaderIsInvalid = true;
			throw ExceptionBuilder.InvalidDataTableReader(_currentDataTable.TableName);
		}
	}

	internal void SchemaChanged()
	{
		IsSchemaChanged = true;
	}

	internal void DataTableCleared()
	{
		if (_started)
		{
			_rowCounter = -1;
			if (!_reachEORows)
			{
				_currentRowRemoved = true;
			}
		}
	}

	internal void DataChanged(DataRowChangeEventArgs args)
	{
		if (!_started || (_rowCounter == -1 && !_tableCleared))
		{
			return;
		}
		switch (args.Action)
		{
		case DataRowAction.Add:
			ValidateRow(_rowCounter + 1);
			if (_currentDataRow == _currentDataTable.Rows[_rowCounter + 1])
			{
				_rowCounter++;
			}
			break;
		case DataRowAction.Delete:
		case DataRowAction.Rollback:
		case DataRowAction.Commit:
			if (args.Row.RowState != DataRowState.Detached)
			{
				break;
			}
			if (args.Row != _currentDataRow)
			{
				if (_rowCounter != 0)
				{
					ValidateRow(_rowCounter - 1);
					if (_currentDataRow == _currentDataTable.Rows[_rowCounter - 1])
					{
						_rowCounter--;
					}
				}
			}
			else
			{
				_currentRowRemoved = true;
				if (_rowCounter > 0)
				{
					_rowCounter--;
					_currentDataRow = _currentDataTable.Rows[_rowCounter];
				}
				else
				{
					_rowCounter = -1;
					_currentDataRow = null;
				}
			}
			break;
		}
	}
}
