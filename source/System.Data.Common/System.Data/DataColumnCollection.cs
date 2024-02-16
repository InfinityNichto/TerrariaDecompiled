using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data.Common;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;

namespace System.Data;

[DefaultEvent("CollectionChanged")]
[Editor("Microsoft.VSDesigner.Data.Design.ColumnsCollectionEditor, Microsoft.VSDesigner, Version=10.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a", "System.Drawing.Design.UITypeEditor, System.Drawing, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
public sealed class DataColumnCollection : InternalDataCollectionBase
{
	private readonly DataTable _table;

	private readonly ArrayList _list = new ArrayList();

	private int _defaultNameIndex = 1;

	private DataColumn[] _delayedAddRangeColumns;

	private readonly Dictionary<string, DataColumn> _columnFromName;

	private bool _fInClear;

	private DataColumn[] _columnsImplementingIChangeTracking = Array.Empty<DataColumn>();

	private int _nColumnsImplementingIChangeTracking;

	private int _nColumnsImplementingIRevertibleChangeTracking;

	protected override ArrayList List => _list;

	internal DataColumn[] ColumnsImplementingIChangeTracking => _columnsImplementingIChangeTracking;

	internal int ColumnsImplementingIChangeTrackingCount => _nColumnsImplementingIChangeTracking;

	internal int ColumnsImplementingIRevertibleChangeTrackingCount => _nColumnsImplementingIRevertibleChangeTracking;

	public DataColumn this[int index]
	{
		get
		{
			try
			{
				return (DataColumn)_list[index];
			}
			catch (ArgumentOutOfRangeException)
			{
				throw ExceptionBuilder.ColumnOutOfRange(index);
			}
		}
	}

	public DataColumn? this[string name]
	{
		get
		{
			if (name == null)
			{
				throw ExceptionBuilder.ArgumentNull("name");
			}
			if (!_columnFromName.TryGetValue(name, out var value) || value == null)
			{
				int num = IndexOfCaseInsensitive(name);
				if (0 <= num)
				{
					return (DataColumn)_list[num];
				}
				if (-2 == num)
				{
					throw ExceptionBuilder.CaseInsensitiveNameConflict(name);
				}
			}
			return value;
		}
	}

	internal DataColumn? this[string name, string ns]
	{
		get
		{
			if (_columnFromName.TryGetValue(name, out var value) && value != null && value.Namespace == ns)
			{
				return value;
			}
			return null;
		}
	}

	public event CollectionChangeEventHandler? CollectionChanged;

	internal event CollectionChangeEventHandler? CollectionChanging;

	internal event CollectionChangeEventHandler? ColumnPropertyChanged;

	internal DataColumnCollection(DataTable table)
	{
		_table = table;
		_columnFromName = new Dictionary<string, DataColumn>();
	}

	internal void EnsureAdditionalCapacity(int capacity)
	{
		if (_list.Capacity < capacity + _list.Count)
		{
			_list.Capacity = capacity + _list.Count;
		}
	}

	public void Add(DataColumn column)
	{
		AddAt(-1, column);
	}

	internal void AddAt(int index, DataColumn column)
	{
		if (column != null && column.ColumnMapping == MappingType.SimpleContent)
		{
			if (_table.XmlText != null && _table.XmlText != column)
			{
				throw ExceptionBuilder.CannotAddColumn3();
			}
			if (_table.ElementColumnCount > 0)
			{
				throw ExceptionBuilder.CannotAddColumn4(column.ColumnName);
			}
			OnCollectionChanging(new CollectionChangeEventArgs(CollectionChangeAction.Add, column));
			BaseAdd(column);
			if (index != -1)
			{
				ArrayAdd(index, column);
			}
			else
			{
				ArrayAdd(column);
			}
			_table.XmlText = column;
		}
		else
		{
			OnCollectionChanging(new CollectionChangeEventArgs(CollectionChangeAction.Add, column));
			BaseAdd(column);
			if (index != -1)
			{
				ArrayAdd(index, column);
			}
			else
			{
				ArrayAdd(column);
			}
			if (column.ColumnMapping == MappingType.Element)
			{
				_table.ElementColumnCount++;
			}
		}
		if (!_table.fInitInProgress && column != null && column.Computed)
		{
			column.CopyExpressionFrom(column);
		}
		OnCollectionChanged(new CollectionChangeEventArgs(CollectionChangeAction.Add, column));
	}

	public void AddRange(DataColumn[] columns)
	{
		if (_table.fInitInProgress)
		{
			_delayedAddRangeColumns = columns;
		}
		else
		{
			if (columns == null)
			{
				return;
			}
			foreach (DataColumn dataColumn in columns)
			{
				if (dataColumn != null)
				{
					Add(dataColumn);
				}
			}
		}
	}

	[RequiresUnreferencedCode("Members might be trimmed for some data types or expressions.")]
	public DataColumn Add(string? columnName, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicFields | DynamicallyAccessedMemberTypes.PublicProperties)] Type type, string expression)
	{
		DataColumn dataColumn = new DataColumn(columnName, type, expression);
		Add(dataColumn);
		return dataColumn;
	}

	public DataColumn Add(string? columnName, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicFields | DynamicallyAccessedMemberTypes.PublicProperties)] Type type)
	{
		DataColumn dataColumn = new DataColumn(columnName, type);
		Add(dataColumn);
		return dataColumn;
	}

	public DataColumn Add(string? columnName)
	{
		DataColumn dataColumn = new DataColumn(columnName);
		Add(dataColumn);
		return dataColumn;
	}

	public DataColumn Add()
	{
		DataColumn dataColumn = new DataColumn();
		Add(dataColumn);
		return dataColumn;
	}

	private void ArrayAdd(DataColumn column)
	{
		_list.Add(column);
		column.SetOrdinalInternal(_list.Count - 1);
		CheckIChangeTracking(column);
	}

	private void ArrayAdd(int index, DataColumn column)
	{
		_list.Insert(index, column);
		CheckIChangeTracking(column);
	}

	private void ArrayRemove(DataColumn column)
	{
		column.SetOrdinalInternal(-1);
		_list.Remove(column);
		int count = _list.Count;
		for (int i = 0; i < count; i++)
		{
			((DataColumn)_list[i]).SetOrdinalInternal(i);
		}
		if (column.ImplementsIChangeTracking)
		{
			RemoveColumnsImplementingIChangeTrackingList(column);
		}
	}

	internal string AssignName()
	{
		string text = MakeName(_defaultNameIndex++);
		while (_columnFromName.ContainsKey(text))
		{
			text = MakeName(_defaultNameIndex++);
		}
		return text;
	}

	private void BaseAdd([NotNull] DataColumn column)
	{
		if (column == null)
		{
			throw ExceptionBuilder.ArgumentNull("column");
		}
		if (column._table == _table)
		{
			throw ExceptionBuilder.CannotAddColumn1(column.ColumnName);
		}
		if (column._table != null)
		{
			throw ExceptionBuilder.CannotAddColumn2(column.ColumnName);
		}
		if (column.ColumnName.Length == 0)
		{
			column.ColumnName = AssignName();
		}
		RegisterColumnName(column.ColumnName, column);
		try
		{
			column.SetTable(_table);
			if (!_table.fInitInProgress && column.Computed && column.DataExpression.DependsOn(column))
			{
				throw ExceptionBuilder.ExpressionCircular();
			}
			if (0 < _table.RecordCapacity)
			{
				column.SetCapacity(_table.RecordCapacity);
			}
			for (int i = 0; i < _table.RecordCapacity; i++)
			{
				column.InitializeRecord(i);
			}
			if (_table.DataSet != null)
			{
				column.OnSetDataSet();
			}
		}
		catch (Exception e) when (ADP.IsCatchableOrSecurityExceptionType(e))
		{
			UnregisterName(column.ColumnName);
			throw;
		}
	}

	private void BaseGroupSwitch(DataColumn[] oldArray, int oldLength, DataColumn[] newArray, int newLength)
	{
		int num = 0;
		for (int i = 0; i < oldLength; i++)
		{
			bool flag = false;
			for (int j = num; j < newLength; j++)
			{
				if (oldArray[i] == newArray[j])
				{
					if (num == j)
					{
						num++;
					}
					flag = true;
					break;
				}
			}
			if (!flag && oldArray[i].Table == _table)
			{
				BaseRemove(oldArray[i]);
				_list.Remove(oldArray[i]);
				oldArray[i].SetOrdinalInternal(-1);
			}
		}
		for (int k = 0; k < newLength; k++)
		{
			if (newArray[k].Table != _table)
			{
				BaseAdd(newArray[k]);
				_list.Add(newArray[k]);
			}
			newArray[k].SetOrdinalInternal(k);
		}
	}

	private void BaseRemove(DataColumn column)
	{
		if (!CanRemove(column, fThrowException: true))
		{
			return;
		}
		if (column._errors > 0)
		{
			for (int i = 0; i < _table.Rows.Count; i++)
			{
				_table.Rows[i].ClearError(column);
			}
		}
		UnregisterName(column.ColumnName);
		column.SetTable(null);
	}

	public bool CanRemove(DataColumn? column)
	{
		return CanRemove(column, fThrowException: false);
	}

	internal bool CanRemove(DataColumn column, bool fThrowException)
	{
		if (column == null)
		{
			if (!fThrowException)
			{
				return false;
			}
			throw ExceptionBuilder.ArgumentNull("column");
		}
		if (column._table != _table)
		{
			if (!fThrowException)
			{
				return false;
			}
			throw ExceptionBuilder.CannotRemoveColumn();
		}
		_table.OnRemoveColumnInternal(column);
		if (_table._primaryKey != null && _table._primaryKey.Key.ContainsColumn(column))
		{
			if (!fThrowException)
			{
				return false;
			}
			throw ExceptionBuilder.CannotRemovePrimaryKey();
		}
		for (int i = 0; i < _table.ParentRelations.Count; i++)
		{
			if (_table.ParentRelations[i].ChildKey.ContainsColumn(column))
			{
				if (!fThrowException)
				{
					return false;
				}
				throw ExceptionBuilder.CannotRemoveChildKey(_table.ParentRelations[i].RelationName);
			}
		}
		for (int j = 0; j < _table.ChildRelations.Count; j++)
		{
			if (_table.ChildRelations[j].ParentKey.ContainsColumn(column))
			{
				if (!fThrowException)
				{
					return false;
				}
				throw ExceptionBuilder.CannotRemoveChildKey(_table.ChildRelations[j].RelationName);
			}
		}
		for (int k = 0; k < _table.Constraints.Count; k++)
		{
			if (_table.Constraints[k].ContainsColumn(column))
			{
				if (!fThrowException)
				{
					return false;
				}
				throw ExceptionBuilder.CannotRemoveConstraint(_table.Constraints[k].ConstraintName, _table.Constraints[k].Table.TableName);
			}
		}
		if (_table.DataSet != null)
		{
			ParentForeignKeyConstraintEnumerator parentForeignKeyConstraintEnumerator = new ParentForeignKeyConstraintEnumerator(_table.DataSet, _table);
			while (parentForeignKeyConstraintEnumerator.GetNext())
			{
				Constraint constraint = parentForeignKeyConstraintEnumerator.GetConstraint();
				if (((ForeignKeyConstraint)constraint).ParentKey.ContainsColumn(column))
				{
					if (!fThrowException)
					{
						return false;
					}
					throw ExceptionBuilder.CannotRemoveConstraint(constraint.ConstraintName, constraint.Table.TableName);
				}
			}
		}
		if (column._dependentColumns != null)
		{
			for (int l = 0; l < column._dependentColumns.Count; l++)
			{
				DataColumn dataColumn = column._dependentColumns[l];
				if ((_fInClear && (dataColumn.Table == _table || dataColumn.Table == null)) || dataColumn.Table == null)
				{
					continue;
				}
				DataExpression dataExpression = dataColumn.DataExpression;
				if (dataExpression != null && dataExpression.DependsOn(column))
				{
					if (!fThrowException)
					{
						return false;
					}
					throw ExceptionBuilder.CannotRemoveExpression(dataColumn.ColumnName, dataColumn.Expression);
				}
			}
		}
		foreach (Index liveIndex in _table.LiveIndexes)
		{
		}
		return true;
	}

	private void CheckIChangeTracking(DataColumn column)
	{
		if (column.ImplementsIRevertibleChangeTracking)
		{
			_nColumnsImplementingIRevertibleChangeTracking++;
			_nColumnsImplementingIChangeTracking++;
			AddColumnsImplementingIChangeTrackingList(column);
		}
		else if (column.ImplementsIChangeTracking)
		{
			_nColumnsImplementingIChangeTracking++;
			AddColumnsImplementingIChangeTrackingList(column);
		}
	}

	public void Clear()
	{
		int count = _list.Count;
		DataColumn[] array = new DataColumn[_list.Count];
		_list.CopyTo(array, 0);
		OnCollectionChanging(InternalDataCollectionBase.s_refreshEventArgs);
		if (_table.fInitInProgress && _delayedAddRangeColumns != null)
		{
			_delayedAddRangeColumns = null;
		}
		try
		{
			_fInClear = true;
			BaseGroupSwitch(array, count, Array.Empty<DataColumn>(), 0);
			_fInClear = false;
		}
		catch (Exception e) when (ADP.IsCatchableOrSecurityExceptionType(e))
		{
			_fInClear = false;
			BaseGroupSwitch(Array.Empty<DataColumn>(), 0, array, count);
			_list.Clear();
			for (int i = 0; i < count; i++)
			{
				_list.Add(array[i]);
			}
			throw;
		}
		_list.Clear();
		_table.ElementColumnCount = 0;
		OnCollectionChanged(InternalDataCollectionBase.s_refreshEventArgs);
	}

	public bool Contains(string name)
	{
		if (_columnFromName.TryGetValue(name, out var value) && value != null)
		{
			return true;
		}
		return IndexOfCaseInsensitive(name) >= 0;
	}

	internal bool Contains(string name, bool caseSensitive)
	{
		if (_columnFromName.TryGetValue(name, out var value) && value != null)
		{
			return true;
		}
		if (!caseSensitive)
		{
			return IndexOfCaseInsensitive(name) >= 0;
		}
		return false;
	}

	public void CopyTo(DataColumn[] array, int index)
	{
		if (array == null)
		{
			throw ExceptionBuilder.ArgumentNull("array");
		}
		if (index < 0)
		{
			throw ExceptionBuilder.ArgumentOutOfRange("index");
		}
		if (array.Length - index < _list.Count)
		{
			throw ExceptionBuilder.InvalidOffsetLength();
		}
		for (int i = 0; i < _list.Count; i++)
		{
			array[index + i] = (DataColumn)_list[i];
		}
	}

	public int IndexOf(DataColumn? column)
	{
		int count = _list.Count;
		for (int i = 0; i < count; i++)
		{
			if (column == (DataColumn)_list[i])
			{
				return i;
			}
		}
		return -1;
	}

	public int IndexOf(string? columnName)
	{
		if (columnName != null && 0 < columnName.Length)
		{
			int count = Count;
			if (!_columnFromName.TryGetValue(columnName, out var value) || value == null)
			{
				int num = IndexOfCaseInsensitive(columnName);
				if (num >= 0)
				{
					return num;
				}
				return -1;
			}
			for (int i = 0; i < count; i++)
			{
				if (value == _list[i])
				{
					return i;
				}
			}
		}
		return -1;
	}

	internal int IndexOfCaseInsensitive(string name)
	{
		int specialHashCode = _table.GetSpecialHashCode(name);
		int num = -1;
		DataColumn dataColumn = null;
		for (int i = 0; i < Count; i++)
		{
			dataColumn = (DataColumn)_list[i];
			if ((specialHashCode == 0 || dataColumn._hashCode == 0 || dataColumn._hashCode == specialHashCode) && NamesEqual(dataColumn.ColumnName, name, fCaseSensitive: false, _table.Locale) != 0)
			{
				if (num != -1)
				{
					return -2;
				}
				num = i;
			}
		}
		return num;
	}

	internal void FinishInitCollection()
	{
		if (_delayedAddRangeColumns == null)
		{
			return;
		}
		DataColumn[] delayedAddRangeColumns = _delayedAddRangeColumns;
		foreach (DataColumn dataColumn in delayedAddRangeColumns)
		{
			if (dataColumn != null)
			{
				Add(dataColumn);
			}
		}
		DataColumn[] delayedAddRangeColumns2 = _delayedAddRangeColumns;
		for (int j = 0; j < delayedAddRangeColumns2.Length; j++)
		{
			delayedAddRangeColumns2[j]?.FinishInitInProgress();
		}
		_delayedAddRangeColumns = null;
	}

	private string MakeName(int index)
	{
		if (index != 1)
		{
			return "Column" + index.ToString(CultureInfo.InvariantCulture);
		}
		return "Column1";
	}

	internal void MoveTo(DataColumn column, int newPosition)
	{
		if (0 > newPosition || newPosition > Count - 1)
		{
			throw ExceptionBuilder.InvalidOrdinal("ordinal", newPosition);
		}
		if (column.ImplementsIChangeTracking)
		{
			RemoveColumnsImplementingIChangeTrackingList(column);
		}
		_list.Remove(column);
		_list.Insert(newPosition, column);
		int count = _list.Count;
		for (int i = 0; i < count; i++)
		{
			((DataColumn)_list[i]).SetOrdinalInternal(i);
		}
		CheckIChangeTracking(column);
		OnCollectionChanged(new CollectionChangeEventArgs(CollectionChangeAction.Refresh, column));
	}

	private void OnCollectionChanged(CollectionChangeEventArgs ccevent)
	{
		_table.UpdatePropertyDescriptorCollectionCache();
		this.CollectionChanged?.Invoke(this, ccevent);
	}

	private void OnCollectionChanging(CollectionChangeEventArgs ccevent)
	{
		this.CollectionChanging?.Invoke(this, ccevent);
	}

	internal void OnColumnPropertyChanged(CollectionChangeEventArgs ccevent)
	{
		_table.UpdatePropertyDescriptorCollectionCache();
		this.ColumnPropertyChanged?.Invoke(this, ccevent);
	}

	internal void RegisterColumnName(string name, DataColumn column)
	{
		try
		{
			_columnFromName.Add(name, column);
			if (column != null)
			{
				column._hashCode = _table.GetSpecialHashCode(name);
			}
		}
		catch (ArgumentException)
		{
			if (_columnFromName[name] != null)
			{
				if (column != null)
				{
					throw ExceptionBuilder.CannotAddDuplicate(name);
				}
				throw ExceptionBuilder.CannotAddDuplicate3(name);
			}
			throw ExceptionBuilder.CannotAddDuplicate2(name);
		}
		if (column == null && NamesEqual(name, MakeName(_defaultNameIndex), fCaseSensitive: true, _table.Locale) != 0)
		{
			do
			{
				_defaultNameIndex++;
			}
			while (Contains(MakeName(_defaultNameIndex)));
		}
	}

	internal bool CanRegisterName(string name)
	{
		return !_columnFromName.ContainsKey(name);
	}

	public void Remove(DataColumn column)
	{
		OnCollectionChanging(new CollectionChangeEventArgs(CollectionChangeAction.Remove, column));
		BaseRemove(column);
		ArrayRemove(column);
		OnCollectionChanged(new CollectionChangeEventArgs(CollectionChangeAction.Remove, column));
		if (column.ColumnMapping == MappingType.Element)
		{
			_table.ElementColumnCount--;
		}
	}

	public void RemoveAt(int index)
	{
		DataColumn dataColumn = this[index];
		if (dataColumn == null)
		{
			throw ExceptionBuilder.ColumnOutOfRange(index);
		}
		Remove(dataColumn);
	}

	public void Remove(string name)
	{
		DataColumn dataColumn = this[name];
		if (dataColumn == null)
		{
			throw ExceptionBuilder.ColumnNotInTheTable(name, _table.TableName);
		}
		Remove(dataColumn);
	}

	internal void UnregisterName(string name)
	{
		_columnFromName.Remove(name);
		if (NamesEqual(name, MakeName(_defaultNameIndex - 1), fCaseSensitive: true, _table.Locale) != 0)
		{
			do
			{
				_defaultNameIndex--;
			}
			while (_defaultNameIndex > 1 && !Contains(MakeName(_defaultNameIndex - 1)));
		}
	}

	private void AddColumnsImplementingIChangeTrackingList(DataColumn dataColumn)
	{
		DataColumn[] columnsImplementingIChangeTracking = _columnsImplementingIChangeTracking;
		DataColumn[] array = new DataColumn[columnsImplementingIChangeTracking.Length + 1];
		columnsImplementingIChangeTracking.CopyTo(array, 0);
		array[columnsImplementingIChangeTracking.Length] = dataColumn;
		_columnsImplementingIChangeTracking = array;
	}

	private void RemoveColumnsImplementingIChangeTrackingList(DataColumn dataColumn)
	{
		DataColumn[] columnsImplementingIChangeTracking = _columnsImplementingIChangeTracking;
		DataColumn[] array = new DataColumn[columnsImplementingIChangeTracking.Length - 1];
		int i = 0;
		int num = 0;
		for (; i < columnsImplementingIChangeTracking.Length; i++)
		{
			if (columnsImplementingIChangeTracking[i] != dataColumn)
			{
				array[num++] = columnsImplementingIChangeTracking[i];
			}
		}
		_columnsImplementingIChangeTracking = array;
	}
}
