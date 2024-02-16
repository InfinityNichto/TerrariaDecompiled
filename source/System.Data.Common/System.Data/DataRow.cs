using System.Collections;
using System.ComponentModel;
using System.Data.Common;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Xml;

namespace System.Data;

public class DataRow
{
	private readonly DataTable _table;

	private readonly DataColumnCollection _columns;

	internal int _oldRecord = -1;

	internal int _newRecord = -1;

	internal int _tempRecord;

	internal long _rowID = -1L;

	internal DataRowAction _action;

	internal bool _inChangingEvent;

	internal bool _inDeletingEvent;

	internal bool _inCascade;

	private DataColumn _lastChangedColumn;

	private int _countColumnChange;

	private DataError _error;

	private object _element;

	private int _rbTreeNodeId;

	private static int s_objectTypeCount;

	internal readonly int _objectID = Interlocked.Increment(ref s_objectTypeCount);

	internal XmlBoundElement? Element
	{
		get
		{
			return (XmlBoundElement)_element;
		}
		set
		{
			_element = value;
		}
	}

	internal DataColumn? LastChangedColumn
	{
		get
		{
			if (_countColumnChange == 1)
			{
				return _lastChangedColumn;
			}
			return null;
		}
		set
		{
			_countColumnChange++;
			_lastChangedColumn = value;
		}
	}

	internal bool HasPropertyChanged => 0 < _countColumnChange;

	internal int RBTreeNodeId
	{
		get
		{
			return _rbTreeNodeId;
		}
		set
		{
			DataCommonEventSource.Log.Trace("<ds.DataRow.set_RBTreeNodeId|INFO> {0}, value={1}", _objectID, value);
			_rbTreeNodeId = value;
		}
	}

	public string RowError
	{
		get
		{
			if (_error != null)
			{
				return _error.Text;
			}
			return string.Empty;
		}
		[param: AllowNull]
		set
		{
			DataCommonEventSource.Log.Trace("<ds.DataRow.set_RowError|API> {0}, value='{1}'", _objectID, value);
			if (_error == null)
			{
				if (!string.IsNullOrEmpty(value))
				{
					_error = new DataError(value);
				}
				RowErrorChanged();
			}
			else if (_error.Text != value)
			{
				_error.Text = value;
				RowErrorChanged();
			}
		}
	}

	internal long rowID
	{
		get
		{
			return _rowID;
		}
		set
		{
			ResetLastChangedColumn();
			_rowID = value;
		}
	}

	public DataRowState RowState
	{
		get
		{
			if (_oldRecord == _newRecord)
			{
				if (_oldRecord == -1)
				{
					return DataRowState.Detached;
				}
				if (0 < _columns.ColumnsImplementingIChangeTrackingCount)
				{
					DataColumn[] columnsImplementingIChangeTracking = _columns.ColumnsImplementingIChangeTracking;
					foreach (DataColumn column in columnsImplementingIChangeTracking)
					{
						object obj = this[column];
						if (DBNull.Value != obj && ((IChangeTracking)obj).IsChanged)
						{
							return DataRowState.Modified;
						}
					}
				}
				return DataRowState.Unchanged;
			}
			if (_oldRecord == -1)
			{
				return DataRowState.Added;
			}
			if (_newRecord == -1)
			{
				return DataRowState.Deleted;
			}
			return DataRowState.Modified;
		}
	}

	public DataTable Table => _table;

	public object this[int columnIndex]
	{
		get
		{
			DataColumn dataColumn = _columns[columnIndex];
			int defaultRecord = GetDefaultRecord();
			return dataColumn[defaultRecord];
		}
		[param: AllowNull]
		set
		{
			DataColumn column = _columns[columnIndex];
			this[column] = value;
		}
	}

	public object this[string columnName]
	{
		get
		{
			DataColumn dataColumn = GetDataColumn(columnName);
			int defaultRecord = GetDefaultRecord();
			return dataColumn[defaultRecord];
		}
		[param: AllowNull]
		set
		{
			DataColumn dataColumn = GetDataColumn(columnName);
			this[dataColumn] = value;
		}
	}

	public object this[DataColumn column]
	{
		get
		{
			CheckColumn(column);
			int defaultRecord = GetDefaultRecord();
			return column[defaultRecord];
		}
		[param: AllowNull]
		set
		{
			CheckColumn(column);
			if (_inChangingEvent)
			{
				throw ExceptionBuilder.EditInRowChanging();
			}
			if (-1 != rowID && column.ReadOnly)
			{
				throw ExceptionBuilder.ReadOnly(column.ColumnName);
			}
			DataColumnChangeEventArgs dataColumnChangeEventArgs = null;
			if (_table.NeedColumnChangeEvents)
			{
				dataColumnChangeEventArgs = new DataColumnChangeEventArgs(this, column, value);
				_table.OnColumnChanging(dataColumnChangeEventArgs);
			}
			if (column.Table != _table)
			{
				throw ExceptionBuilder.ColumnNotInTheTable(column.ColumnName, _table.TableName);
			}
			if (-1 != rowID && column.ReadOnly)
			{
				throw ExceptionBuilder.ReadOnly(column.ColumnName);
			}
			object obj = ((dataColumnChangeEventArgs != null) ? dataColumnChangeEventArgs.ProposedValue : value);
			if (obj == null)
			{
				if (column.IsValueType)
				{
					throw ExceptionBuilder.CannotSetToNull(column);
				}
				obj = DBNull.Value;
			}
			bool flag = BeginEditInternal();
			try
			{
				int proposedRecordNo = GetProposedRecordNo();
				column[proposedRecordNo] = obj;
			}
			catch (Exception e) when (ADP.IsCatchableOrSecurityExceptionType(e))
			{
				if (flag)
				{
					CancelEdit();
				}
				throw;
			}
			LastChangedColumn = column;
			if (dataColumnChangeEventArgs != null)
			{
				_table.OnColumnChanged(dataColumnChangeEventArgs);
			}
			if (flag)
			{
				EndEdit();
			}
		}
	}

	public object this[int columnIndex, DataRowVersion version]
	{
		get
		{
			DataColumn dataColumn = _columns[columnIndex];
			int recordFromVersion = GetRecordFromVersion(version);
			return dataColumn[recordFromVersion];
		}
	}

	public object this[string columnName, DataRowVersion version]
	{
		get
		{
			DataColumn dataColumn = GetDataColumn(columnName);
			int recordFromVersion = GetRecordFromVersion(version);
			return dataColumn[recordFromVersion];
		}
	}

	public object this[DataColumn column, DataRowVersion version]
	{
		get
		{
			CheckColumn(column);
			int recordFromVersion = GetRecordFromVersion(version);
			return column[recordFromVersion];
		}
	}

	public object?[] ItemArray
	{
		get
		{
			int defaultRecord = GetDefaultRecord();
			object[] array = new object[_columns.Count];
			for (int i = 0; i < array.Length; i++)
			{
				DataColumn dataColumn = _columns[i];
				array[i] = dataColumn[defaultRecord];
			}
			return array;
		}
		set
		{
			if (value == null)
			{
				throw ExceptionBuilder.ArgumentNull("ItemArray");
			}
			if (_columns.Count < value.Length)
			{
				throw ExceptionBuilder.ValueArrayLength();
			}
			DataColumnChangeEventArgs dataColumnChangeEventArgs = null;
			if (_table.NeedColumnChangeEvents)
			{
				dataColumnChangeEventArgs = new DataColumnChangeEventArgs(this);
			}
			bool flag = BeginEditInternal();
			for (int i = 0; i < value.Length; i++)
			{
				object obj = value[i];
				if (obj == null)
				{
					continue;
				}
				DataColumn dataColumn = _columns[i];
				if (-1 != rowID && dataColumn.ReadOnly)
				{
					throw ExceptionBuilder.ReadOnly(dataColumn.ColumnName);
				}
				if (dataColumnChangeEventArgs != null)
				{
					dataColumnChangeEventArgs.InitializeColumnChangeEvent(dataColumn, obj);
					_table.OnColumnChanging(dataColumnChangeEventArgs);
				}
				if (dataColumn.Table != _table)
				{
					throw ExceptionBuilder.ColumnNotInTheTable(dataColumn.ColumnName, _table.TableName);
				}
				if (-1 != rowID && dataColumn.ReadOnly)
				{
					throw ExceptionBuilder.ReadOnly(dataColumn.ColumnName);
				}
				if (_tempRecord == -1)
				{
					BeginEditInternal();
				}
				object obj2 = ((dataColumnChangeEventArgs != null) ? dataColumnChangeEventArgs.ProposedValue : obj);
				if (obj2 == null)
				{
					if (dataColumn.IsValueType)
					{
						throw ExceptionBuilder.CannotSetToNull(dataColumn);
					}
					obj2 = DBNull.Value;
				}
				try
				{
					int proposedRecordNo = GetProposedRecordNo();
					dataColumn[proposedRecordNo] = obj2;
				}
				catch (Exception e) when (ADP.IsCatchableOrSecurityExceptionType(e))
				{
					if (flag)
					{
						CancelEdit();
					}
					throw;
				}
				LastChangedColumn = dataColumn;
				if (dataColumnChangeEventArgs != null)
				{
					_table.OnColumnChanged(dataColumnChangeEventArgs);
				}
			}
			EndEdit();
		}
	}

	public bool HasErrors
	{
		get
		{
			if (_error != null)
			{
				return _error.HasErrors;
			}
			return false;
		}
	}

	protected internal DataRow(DataRowBuilder builder)
	{
		_tempRecord = builder._record;
		_table = builder._table;
		_columns = _table.Columns;
	}

	private void RowErrorChanged()
	{
		if (_oldRecord != -1)
		{
			_table.RecordChanged(_oldRecord);
		}
		if (_newRecord != -1)
		{
			_table.RecordChanged(_newRecord);
		}
	}

	internal void CheckForLoops(DataRelation rel)
	{
		if (_table._fInLoadDiffgram || (_table.DataSet != null && _table.DataSet._fInLoadDiffgram))
		{
			return;
		}
		int count = _table.Rows.Count;
		int num = 0;
		for (DataRow parentRow = GetParentRow(rel); parentRow != null; parentRow = parentRow.GetParentRow(rel))
		{
			if (parentRow == this || num > count)
			{
				throw ExceptionBuilder.NestedCircular(_table.TableName);
			}
			num++;
		}
	}

	internal int GetNestedParentCount()
	{
		int num = 0;
		DataRelation[] nestedParentRelations = _table.NestedParentRelations;
		DataRelation[] array = nestedParentRelations;
		foreach (DataRelation dataRelation in array)
		{
			if (dataRelation != null)
			{
				if (dataRelation.ParentTable == _table)
				{
					CheckForLoops(dataRelation);
				}
				DataRow parentRow = GetParentRow(dataRelation);
				if (parentRow != null)
				{
					num++;
				}
			}
		}
		return num;
	}

	public void AcceptChanges()
	{
		long scopeId = DataCommonEventSource.Log.EnterScope("<ds.DataRow.AcceptChanges|API> {0}", _objectID);
		try
		{
			EndEdit();
			if (RowState != DataRowState.Detached && RowState != DataRowState.Deleted && _columns.ColumnsImplementingIChangeTrackingCount > 0)
			{
				DataColumn[] columnsImplementingIChangeTracking = _columns.ColumnsImplementingIChangeTracking;
				foreach (DataColumn column in columnsImplementingIChangeTracking)
				{
					object obj = this[column];
					if (DBNull.Value != obj)
					{
						IChangeTracking changeTracking = (IChangeTracking)obj;
						if (changeTracking.IsChanged)
						{
							changeTracking.AcceptChanges();
						}
					}
				}
			}
			_table.CommitRow(this);
		}
		finally
		{
			DataCommonEventSource.Log.ExitScope(scopeId);
		}
	}

	[EditorBrowsable(EditorBrowsableState.Advanced)]
	public void BeginEdit()
	{
		BeginEditInternal();
	}

	private bool BeginEditInternal()
	{
		if (_inChangingEvent)
		{
			throw ExceptionBuilder.BeginEditInRowChanging();
		}
		if (_tempRecord != -1)
		{
			if (_tempRecord < _table._recordManager.LastFreeRecord)
			{
				return false;
			}
			_tempRecord = -1;
		}
		if (_oldRecord != -1 && _newRecord == -1)
		{
			throw ExceptionBuilder.DeletedRowInaccessible();
		}
		ResetLastChangedColumn();
		_tempRecord = _table.NewRecord(_newRecord);
		return true;
	}

	[EditorBrowsable(EditorBrowsableState.Advanced)]
	public void CancelEdit()
	{
		if (_inChangingEvent)
		{
			throw ExceptionBuilder.CancelEditInRowChanging();
		}
		_table.FreeRecord(ref _tempRecord);
		ResetLastChangedColumn();
	}

	private void CheckColumn(DataColumn column)
	{
		if (column == null)
		{
			throw ExceptionBuilder.ArgumentNull("column");
		}
		if (column.Table != _table)
		{
			throw ExceptionBuilder.ColumnNotInTheTable(column.ColumnName, _table.TableName);
		}
	}

	internal void CheckInTable()
	{
		if (rowID == -1)
		{
			throw ExceptionBuilder.RowNotInTheTable();
		}
	}

	public void Delete()
	{
		if (_inDeletingEvent)
		{
			throw ExceptionBuilder.DeleteInRowDeleting();
		}
		if (_newRecord != -1)
		{
			_table.DeleteRow(this);
		}
	}

	[EditorBrowsable(EditorBrowsableState.Advanced)]
	public void EndEdit()
	{
		if (_inChangingEvent)
		{
			throw ExceptionBuilder.EndEditInRowChanging();
		}
		if (_newRecord == -1 || _tempRecord == -1)
		{
			return;
		}
		try
		{
			_table.SetNewRecord(this, _tempRecord, DataRowAction.Change, isInMerge: false, fireEvent: true, suppressEnsurePropertyChanged: true);
		}
		finally
		{
			ResetLastChangedColumn();
		}
	}

	public void SetColumnError(int columnIndex, string? error)
	{
		DataColumn dataColumn = _columns[columnIndex];
		if (dataColumn == null)
		{
			throw ExceptionBuilder.ColumnOutOfRange(columnIndex);
		}
		SetColumnError(dataColumn, error);
	}

	public void SetColumnError(string columnName, string? error)
	{
		DataColumn dataColumn = GetDataColumn(columnName);
		SetColumnError(dataColumn, error);
	}

	public void SetColumnError(DataColumn column, string? error)
	{
		CheckColumn(column);
		long scopeId = DataCommonEventSource.Log.EnterScope("<ds.DataRow.SetColumnError|API> {0}, column={1}, error='{2}'", _objectID, column.ObjectID, error);
		try
		{
			if (_error == null)
			{
				_error = new DataError();
			}
			if (GetColumnError(column) != error)
			{
				_error.SetColumnError(column, error);
				RowErrorChanged();
			}
		}
		finally
		{
			DataCommonEventSource.Log.ExitScope(scopeId);
		}
	}

	public string GetColumnError(int columnIndex)
	{
		return GetColumnError(_columns[columnIndex]);
	}

	public string GetColumnError(string columnName)
	{
		return GetColumnError(GetDataColumn(columnName));
	}

	public string GetColumnError(DataColumn column)
	{
		CheckColumn(column);
		if (_error == null)
		{
			_error = new DataError();
		}
		return _error.GetColumnError(column);
	}

	public void ClearErrors()
	{
		if (_error != null)
		{
			_error.Clear();
			RowErrorChanged();
		}
	}

	internal void ClearError(DataColumn column)
	{
		if (_error != null)
		{
			_error.Clear(column);
			RowErrorChanged();
		}
	}

	public DataColumn[] GetColumnsInError()
	{
		if (_error != null)
		{
			return _error.GetColumnsInError();
		}
		return Array.Empty<DataColumn>();
	}

	public DataRow[] GetChildRows(string? relationName)
	{
		return GetChildRows(_table.ChildRelations[relationName], DataRowVersion.Default);
	}

	public DataRow[] GetChildRows(string? relationName, DataRowVersion version)
	{
		return GetChildRows(_table.ChildRelations[relationName], version);
	}

	public DataRow[] GetChildRows(DataRelation? relation)
	{
		return GetChildRows(relation, DataRowVersion.Default);
	}

	public DataRow[] GetChildRows(DataRelation? relation, DataRowVersion version)
	{
		if (relation == null)
		{
			return _table.NewRowArray(0);
		}
		if (relation.DataSet != _table.DataSet)
		{
			throw ExceptionBuilder.RowNotInTheDataSet();
		}
		if (relation.ParentKey.Table != _table)
		{
			throw ExceptionBuilder.RelationForeignTable(relation.ParentTable.TableName, _table.TableName);
		}
		return DataRelation.GetChildRows(relation.ParentKey, relation.ChildKey, this, version);
	}

	internal DataColumn GetDataColumn(string columnName)
	{
		DataColumn dataColumn = _columns[columnName];
		if (dataColumn != null)
		{
			return dataColumn;
		}
		throw ExceptionBuilder.ColumnNotInTheTable(columnName, _table.TableName);
	}

	public DataRow? GetParentRow(string? relationName)
	{
		return GetParentRow(_table.ParentRelations[relationName], DataRowVersion.Default);
	}

	public DataRow? GetParentRow(string? relationName, DataRowVersion version)
	{
		return GetParentRow(_table.ParentRelations[relationName], version);
	}

	public DataRow? GetParentRow(DataRelation? relation)
	{
		return GetParentRow(relation, DataRowVersion.Default);
	}

	public DataRow? GetParentRow(DataRelation? relation, DataRowVersion version)
	{
		if (relation == null)
		{
			return null;
		}
		if (relation.DataSet != _table.DataSet)
		{
			throw ExceptionBuilder.RelationForeignRow();
		}
		if (relation.ChildKey.Table != _table)
		{
			throw ExceptionBuilder.GetParentRowTableMismatch(relation.ChildTable.TableName, _table.TableName);
		}
		return DataRelation.GetParentRow(relation.ParentKey, relation.ChildKey, this, version);
	}

	internal DataRow GetNestedParentRow(DataRowVersion version)
	{
		DataRelation[] nestedParentRelations = _table.NestedParentRelations;
		DataRelation[] array = nestedParentRelations;
		foreach (DataRelation dataRelation in array)
		{
			if (dataRelation != null)
			{
				if (dataRelation.ParentTable == _table)
				{
					CheckForLoops(dataRelation);
				}
				DataRow parentRow = GetParentRow(dataRelation, version);
				if (parentRow != null)
				{
					return parentRow;
				}
			}
		}
		return null;
	}

	public DataRow[] GetParentRows(string? relationName)
	{
		return GetParentRows(_table.ParentRelations[relationName], DataRowVersion.Default);
	}

	public DataRow[] GetParentRows(string? relationName, DataRowVersion version)
	{
		return GetParentRows(_table.ParentRelations[relationName], version);
	}

	public DataRow[] GetParentRows(DataRelation? relation)
	{
		return GetParentRows(relation, DataRowVersion.Default);
	}

	public DataRow[] GetParentRows(DataRelation? relation, DataRowVersion version)
	{
		if (relation == null)
		{
			return _table.NewRowArray(0);
		}
		if (relation.DataSet != _table.DataSet)
		{
			throw ExceptionBuilder.RowNotInTheDataSet();
		}
		if (relation.ChildKey.Table != _table)
		{
			throw ExceptionBuilder.GetParentRowTableMismatch(relation.ChildTable.TableName, _table.TableName);
		}
		return DataRelation.GetParentRows(relation.ParentKey, relation.ChildKey, this, version);
	}

	internal object[] GetColumnValues(DataColumn[] columns)
	{
		return GetColumnValues(columns, DataRowVersion.Default);
	}

	internal object[] GetColumnValues(DataColumn[] columns, DataRowVersion version)
	{
		DataKey key = new DataKey(columns, copyColumns: false);
		return GetKeyValues(key, version);
	}

	internal object[] GetKeyValues(DataKey key)
	{
		int defaultRecord = GetDefaultRecord();
		return key.GetKeyValues(defaultRecord);
	}

	internal object[] GetKeyValues(DataKey key, DataRowVersion version)
	{
		int recordFromVersion = GetRecordFromVersion(version);
		return key.GetKeyValues(recordFromVersion);
	}

	internal int GetCurrentRecordNo()
	{
		if (_newRecord == -1)
		{
			throw ExceptionBuilder.NoCurrentData();
		}
		return _newRecord;
	}

	internal int GetDefaultRecord()
	{
		if (_tempRecord != -1)
		{
			return _tempRecord;
		}
		if (_newRecord != -1)
		{
			return _newRecord;
		}
		throw (_oldRecord == -1) ? ExceptionBuilder.RowRemovedFromTheTable() : ExceptionBuilder.DeletedRowInaccessible();
	}

	internal int GetOriginalRecordNo()
	{
		if (_oldRecord == -1)
		{
			throw ExceptionBuilder.NoOriginalData();
		}
		return _oldRecord;
	}

	private int GetProposedRecordNo()
	{
		if (_tempRecord == -1)
		{
			throw ExceptionBuilder.NoProposedData();
		}
		return _tempRecord;
	}

	internal int GetRecordFromVersion(DataRowVersion version)
	{
		return version switch
		{
			DataRowVersion.Original => GetOriginalRecordNo(), 
			DataRowVersion.Current => GetCurrentRecordNo(), 
			DataRowVersion.Proposed => GetProposedRecordNo(), 
			DataRowVersion.Default => GetDefaultRecord(), 
			_ => throw ExceptionBuilder.InvalidRowVersion(), 
		};
	}

	internal DataRowVersion GetDefaultRowVersion(DataViewRowState viewState)
	{
		if (_oldRecord == _newRecord)
		{
			_ = _oldRecord;
			_ = -1;
			return DataRowVersion.Default;
		}
		if (_oldRecord == -1)
		{
			return DataRowVersion.Default;
		}
		if (_newRecord == -1)
		{
			return DataRowVersion.Original;
		}
		if ((DataViewRowState.ModifiedCurrent & viewState) != 0)
		{
			return DataRowVersion.Default;
		}
		return DataRowVersion.Original;
	}

	internal DataViewRowState GetRecordState(int record)
	{
		if (record == -1)
		{
			return DataViewRowState.None;
		}
		if (record == _oldRecord && record == _newRecord)
		{
			return DataViewRowState.Unchanged;
		}
		if (record == _oldRecord)
		{
			if (_newRecord == -1)
			{
				return DataViewRowState.Deleted;
			}
			return DataViewRowState.ModifiedOriginal;
		}
		if (record == _newRecord)
		{
			if (_oldRecord == -1)
			{
				return DataViewRowState.Added;
			}
			return DataViewRowState.ModifiedCurrent;
		}
		return DataViewRowState.None;
	}

	internal bool HasKeyChanged(DataKey key)
	{
		return HasKeyChanged(key, DataRowVersion.Current, DataRowVersion.Proposed);
	}

	internal bool HasKeyChanged(DataKey key, DataRowVersion version1, DataRowVersion version2)
	{
		if (!HasVersion(version1) || !HasVersion(version2))
		{
			return true;
		}
		return !key.RecordsEqual(GetRecordFromVersion(version1), GetRecordFromVersion(version2));
	}

	public bool HasVersion(DataRowVersion version)
	{
		return version switch
		{
			DataRowVersion.Original => _oldRecord != -1, 
			DataRowVersion.Current => _newRecord != -1, 
			DataRowVersion.Proposed => _tempRecord != -1, 
			DataRowVersion.Default => _tempRecord != -1 || _newRecord != -1, 
			_ => throw ExceptionBuilder.InvalidRowVersion(), 
		};
	}

	internal bool HasChanges()
	{
		if (!HasVersion(DataRowVersion.Original) || !HasVersion(DataRowVersion.Current))
		{
			return true;
		}
		foreach (DataColumn column in Table.Columns)
		{
			if (column.Compare(_oldRecord, _newRecord) != 0)
			{
				return true;
			}
		}
		return false;
	}

	internal bool HaveValuesChanged(DataColumn[] columns)
	{
		return HaveValuesChanged(columns, DataRowVersion.Current, DataRowVersion.Proposed);
	}

	internal bool HaveValuesChanged(DataColumn[] columns, DataRowVersion version1, DataRowVersion version2)
	{
		for (int i = 0; i < columns.Length; i++)
		{
			CheckColumn(columns[i]);
		}
		DataKey key = new DataKey(columns, copyColumns: false);
		return HasKeyChanged(key, version1, version2);
	}

	public bool IsNull(int columnIndex)
	{
		DataColumn dataColumn = _columns[columnIndex];
		int defaultRecord = GetDefaultRecord();
		return dataColumn.IsNull(defaultRecord);
	}

	public bool IsNull(string columnName)
	{
		DataColumn dataColumn = GetDataColumn(columnName);
		int defaultRecord = GetDefaultRecord();
		return dataColumn.IsNull(defaultRecord);
	}

	public bool IsNull(DataColumn column)
	{
		CheckColumn(column);
		int defaultRecord = GetDefaultRecord();
		return column.IsNull(defaultRecord);
	}

	public bool IsNull(DataColumn column, DataRowVersion version)
	{
		CheckColumn(column);
		int recordFromVersion = GetRecordFromVersion(version);
		return column.IsNull(recordFromVersion);
	}

	public void RejectChanges()
	{
		long scopeId = DataCommonEventSource.Log.EnterScope("<ds.DataRow.RejectChanges|API> {0}", _objectID);
		try
		{
			if (RowState != DataRowState.Detached)
			{
				if (_columns.ColumnsImplementingIChangeTrackingCount != _columns.ColumnsImplementingIRevertibleChangeTrackingCount)
				{
					DataColumn[] columnsImplementingIChangeTracking = _columns.ColumnsImplementingIChangeTracking;
					foreach (DataColumn dataColumn in columnsImplementingIChangeTracking)
					{
						if (!dataColumn.ImplementsIRevertibleChangeTracking)
						{
							object obj = null;
							obj = ((RowState == DataRowState.Deleted) ? this[dataColumn, DataRowVersion.Original] : this[dataColumn]);
							if (DBNull.Value != obj && ((IChangeTracking)obj).IsChanged)
							{
								throw ExceptionBuilder.UDTImplementsIChangeTrackingButnotIRevertible(dataColumn.DataType.AssemblyQualifiedName);
							}
						}
					}
				}
				DataColumn[] columnsImplementingIChangeTracking2 = _columns.ColumnsImplementingIChangeTracking;
				foreach (DataColumn column in columnsImplementingIChangeTracking2)
				{
					object obj2 = null;
					obj2 = ((RowState == DataRowState.Deleted) ? this[column, DataRowVersion.Original] : this[column]);
					if (DBNull.Value != obj2)
					{
						IChangeTracking changeTracking = (IChangeTracking)obj2;
						if (changeTracking.IsChanged)
						{
							((IRevertibleChangeTracking)obj2).RejectChanges();
						}
					}
				}
			}
			_table.RollbackRow(this);
		}
		finally
		{
			DataCommonEventSource.Log.ExitScope(scopeId);
		}
	}

	internal void ResetLastChangedColumn()
	{
		_lastChangedColumn = null;
		_countColumnChange = 0;
	}

	internal void SetKeyValues(DataKey key, object[] keyValues)
	{
		bool flag = true;
		bool flag2 = _tempRecord == -1;
		for (int i = 0; i < keyValues.Length; i++)
		{
			object obj = this[key.ColumnsReference[i]];
			if (!obj.Equals(keyValues[i]))
			{
				if (flag2 && flag)
				{
					flag = false;
					BeginEditInternal();
				}
				this[key.ColumnsReference[i]] = keyValues[i];
			}
		}
		if (!flag)
		{
			EndEdit();
		}
	}

	protected void SetNull(DataColumn column)
	{
		this[column] = DBNull.Value;
	}

	internal void SetNestedParentRow(DataRow parentRow, bool setNonNested)
	{
		if (parentRow == null)
		{
			SetParentRowToDBNull();
			return;
		}
		foreach (DataRelation parentRelation in _table.ParentRelations)
		{
			if (!(parentRelation.Nested || setNonNested) || parentRelation.ParentKey.Table != parentRow._table)
			{
				continue;
			}
			object[] keyValues = parentRow.GetKeyValues(parentRelation.ParentKey);
			SetKeyValues(parentRelation.ChildKey, keyValues);
			if (parentRelation.Nested)
			{
				if (parentRow._table == _table)
				{
					CheckForLoops(parentRelation);
				}
				else
				{
					GetParentRow(parentRelation);
				}
			}
		}
	}

	public void SetParentRow(DataRow? parentRow)
	{
		SetNestedParentRow(parentRow, setNonNested: true);
	}

	public void SetParentRow(DataRow? parentRow, DataRelation? relation)
	{
		if (relation == null)
		{
			SetParentRow(parentRow);
			return;
		}
		if (parentRow == null)
		{
			SetParentRowToDBNull(relation);
			return;
		}
		if (_table.DataSet != parentRow._table.DataSet)
		{
			throw ExceptionBuilder.ParentRowNotInTheDataSet();
		}
		if (relation.ChildKey.Table != _table)
		{
			throw ExceptionBuilder.SetParentRowTableMismatch(relation.ChildKey.Table.TableName, _table.TableName);
		}
		if (relation.ParentKey.Table != parentRow._table)
		{
			throw ExceptionBuilder.SetParentRowTableMismatch(relation.ParentKey.Table.TableName, parentRow._table.TableName);
		}
		object[] keyValues = parentRow.GetKeyValues(relation.ParentKey);
		SetKeyValues(relation.ChildKey, keyValues);
	}

	internal void SetParentRowToDBNull()
	{
		foreach (DataRelation parentRelation in _table.ParentRelations)
		{
			SetParentRowToDBNull(parentRelation);
		}
	}

	internal void SetParentRowToDBNull(DataRelation relation)
	{
		if (relation.ChildKey.Table != _table)
		{
			throw ExceptionBuilder.SetParentRowTableMismatch(relation.ChildKey.Table.TableName, _table.TableName);
		}
		SetKeyValues(keyValues: new object[1] { DBNull.Value }, key: relation.ChildKey);
	}

	public void SetAdded()
	{
		if (RowState == DataRowState.Unchanged)
		{
			_table.SetOldRecord(this, -1);
			return;
		}
		throw ExceptionBuilder.SetAddedAndModifiedCalledOnnonUnchanged();
	}

	public void SetModified()
	{
		if (RowState == DataRowState.Unchanged)
		{
			_tempRecord = _table.NewRecord(_newRecord);
			if (_tempRecord != -1)
			{
				_table.SetNewRecord(this, _tempRecord, DataRowAction.Change, isInMerge: false, fireEvent: true, suppressEnsurePropertyChanged: true);
			}
			return;
		}
		throw ExceptionBuilder.SetAddedAndModifiedCalledOnnonUnchanged();
	}

	internal int CopyValuesIntoStore(ArrayList storeList, ArrayList nullbitList, int storeIndex)
	{
		int num = 0;
		if (_oldRecord != -1)
		{
			for (int i = 0; i < _columns.Count; i++)
			{
				_columns[i].CopyValueIntoStore(_oldRecord, storeList[i], (BitArray)nullbitList[i], storeIndex);
			}
			num++;
			storeIndex++;
		}
		DataRowState rowState = RowState;
		if (DataRowState.Added == rowState || DataRowState.Modified == rowState)
		{
			for (int j = 0; j < _columns.Count; j++)
			{
				_columns[j].CopyValueIntoStore(_newRecord, storeList[j], (BitArray)nullbitList[j], storeIndex);
			}
			num++;
			storeIndex++;
		}
		if (-1 != _tempRecord)
		{
			for (int k = 0; k < _columns.Count; k++)
			{
				_columns[k].CopyValueIntoStore(_tempRecord, storeList[k], (BitArray)nullbitList[k], storeIndex);
			}
			num++;
			storeIndex++;
		}
		return num;
	}
}
