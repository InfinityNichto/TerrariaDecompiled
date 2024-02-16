using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Threading;

namespace System.Data;

[DefaultEvent("CollectionChanged")]
[Editor("Microsoft.VSDesigner.Data.Design.TablesCollectionEditor, Microsoft.VSDesigner, Version=10.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a", "System.Drawing.Design.UITypeEditor, System.Drawing, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
[ListBindable(false)]
public sealed class DataTableCollection : InternalDataCollectionBase
{
	private readonly DataSet _dataSet;

	private readonly ArrayList _list = new ArrayList();

	private int _defaultNameIndex = 1;

	private DataTable[] _delayedAddRangeTables;

	private CollectionChangeEventHandler _onCollectionChangedDelegate;

	private CollectionChangeEventHandler _onCollectionChangingDelegate;

	private static int s_objectTypeCount;

	private readonly int _objectID = Interlocked.Increment(ref s_objectTypeCount);

	protected override ArrayList List => _list;

	internal int ObjectID => _objectID;

	public DataTable this[int index]
	{
		get
		{
			try
			{
				return (DataTable)_list[index];
			}
			catch (ArgumentOutOfRangeException)
			{
				throw ExceptionBuilder.TableOutOfRange(index);
			}
		}
	}

	public DataTable? this[string? name]
	{
		get
		{
			int num = InternalIndexOf(name);
			if (num == -2)
			{
				throw ExceptionBuilder.CaseInsensitiveNameConflict(name);
			}
			if (num == -3)
			{
				throw ExceptionBuilder.NamespaceNameConflict(name);
			}
			if (num >= 0)
			{
				return (DataTable)_list[num];
			}
			return null;
		}
	}

	public DataTable? this[string? name, string tableNamespace]
	{
		get
		{
			if (tableNamespace == null)
			{
				throw ExceptionBuilder.ArgumentNull("tableNamespace");
			}
			int num = InternalIndexOf(name, tableNamespace);
			if (num == -2)
			{
				throw ExceptionBuilder.CaseInsensitiveNameConflict(name);
			}
			if (num >= 0)
			{
				return (DataTable)_list[num];
			}
			return null;
		}
	}

	public event CollectionChangeEventHandler? CollectionChanged
	{
		add
		{
			DataCommonEventSource.Log.Trace("<ds.DataTableCollection.add_CollectionChanged|API> {0}", ObjectID);
			_onCollectionChangedDelegate = (CollectionChangeEventHandler)Delegate.Combine(_onCollectionChangedDelegate, value);
		}
		remove
		{
			DataCommonEventSource.Log.Trace("<ds.DataTableCollection.remove_CollectionChanged|API> {0}", ObjectID);
			_onCollectionChangedDelegate = (CollectionChangeEventHandler)Delegate.Remove(_onCollectionChangedDelegate, value);
		}
	}

	public event CollectionChangeEventHandler? CollectionChanging
	{
		add
		{
			DataCommonEventSource.Log.Trace("<ds.DataTableCollection.add_CollectionChanging|API> {0}", ObjectID);
			_onCollectionChangingDelegate = (CollectionChangeEventHandler)Delegate.Combine(_onCollectionChangingDelegate, value);
		}
		remove
		{
			DataCommonEventSource.Log.Trace("<ds.DataTableCollection.remove_CollectionChanging|API> {0}", ObjectID);
			_onCollectionChangingDelegate = (CollectionChangeEventHandler)Delegate.Remove(_onCollectionChangingDelegate, value);
		}
	}

	internal DataTableCollection(DataSet dataSet)
	{
		DataCommonEventSource.Log.Trace("<ds.DataTableCollection.DataTableCollection|INFO> {0}, dataSet={1}", ObjectID, dataSet?.ObjectID ?? 0);
		_dataSet = dataSet;
	}

	internal DataTable GetTable(string name, string ns)
	{
		for (int i = 0; i < _list.Count; i++)
		{
			DataTable dataTable = (DataTable)_list[i];
			if (dataTable.TableName == name && dataTable.Namespace == ns)
			{
				return dataTable;
			}
		}
		return null;
	}

	internal DataTable GetTableSmart(string name, string ns)
	{
		int num = 0;
		DataTable result = null;
		for (int i = 0; i < _list.Count; i++)
		{
			DataTable dataTable = (DataTable)_list[i];
			if (dataTable.TableName == name)
			{
				if (dataTable.Namespace == ns)
				{
					return dataTable;
				}
				num++;
				result = dataTable;
			}
		}
		if (num != 1)
		{
			return null;
		}
		return result;
	}

	public void Add(DataTable table)
	{
		long scopeId = DataCommonEventSource.Log.EnterScope("<ds.DataTableCollection.Add|API> {0}, table={1}", ObjectID, table?.ObjectID ?? 0);
		try
		{
			OnCollectionChanging(new CollectionChangeEventArgs(CollectionChangeAction.Add, table));
			BaseAdd(table);
			ArrayAdd(table);
			if (table.SetLocaleValue(_dataSet.Locale, userSet: false, resetIndexes: false) || table.SetCaseSensitiveValue(_dataSet.CaseSensitive, userSet: false, resetIndexes: false))
			{
				table.ResetIndexes();
			}
			OnCollectionChanged(new CollectionChangeEventArgs(CollectionChangeAction.Add, table));
		}
		finally
		{
			DataCommonEventSource.Log.ExitScope(scopeId);
		}
	}

	public void AddRange(DataTable?[]? tables)
	{
		long scopeId = DataCommonEventSource.Log.EnterScope("<ds.DataTableCollection.AddRange|API> {0}", ObjectID);
		try
		{
			if (_dataSet._fInitInProgress)
			{
				_delayedAddRangeTables = tables;
			}
			else
			{
				if (tables == null)
				{
					return;
				}
				foreach (DataTable dataTable in tables)
				{
					if (dataTable != null)
					{
						Add(dataTable);
					}
				}
			}
		}
		finally
		{
			DataCommonEventSource.Log.ExitScope(scopeId);
		}
	}

	public DataTable Add(string? name)
	{
		DataTable dataTable = new DataTable(name);
		Add(dataTable);
		return dataTable;
	}

	public DataTable Add(string? name, string? tableNamespace)
	{
		DataTable dataTable = new DataTable(name, tableNamespace);
		Add(dataTable);
		return dataTable;
	}

	public DataTable Add()
	{
		DataTable dataTable = new DataTable();
		Add(dataTable);
		return dataTable;
	}

	private void ArrayAdd(DataTable table)
	{
		_list.Add(table);
	}

	internal string AssignName()
	{
		string text = null;
		while (Contains(text = MakeName(_defaultNameIndex)))
		{
			_defaultNameIndex++;
		}
		return text;
	}

	private void BaseAdd([NotNull] DataTable table)
	{
		if (table == null)
		{
			throw ExceptionBuilder.ArgumentNull("table");
		}
		if (table.DataSet == _dataSet)
		{
			throw ExceptionBuilder.TableAlreadyInTheDataSet();
		}
		if (table.DataSet != null)
		{
			throw ExceptionBuilder.TableAlreadyInOtherDataSet();
		}
		if (table.TableName.Length == 0)
		{
			table.TableName = AssignName();
		}
		else
		{
			if (NamesEqual(table.TableName, _dataSet.DataSetName, fCaseSensitive: false, _dataSet.Locale) != 0 && !table._fNestedInDataset)
			{
				throw ExceptionBuilder.DatasetConflictingName(_dataSet.DataSetName);
			}
			RegisterName(table.TableName, table.Namespace);
		}
		table.SetDataSet(_dataSet);
	}

	private void BaseGroupSwitch(DataTable[] oldArray, int oldLength, DataTable[] newArray, int newLength)
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
			if (!flag && oldArray[i].DataSet == _dataSet)
			{
				BaseRemove(oldArray[i]);
			}
		}
		for (int k = 0; k < newLength; k++)
		{
			if (newArray[k].DataSet != _dataSet)
			{
				BaseAdd(newArray[k]);
				_list.Add(newArray[k]);
			}
		}
	}

	private void BaseRemove(DataTable table)
	{
		if (CanRemove(table, fThrowException: true))
		{
			UnregisterName(table.TableName);
			table.SetDataSet(null);
		}
		_list.Remove(table);
		_dataSet.OnRemovedTable(table);
	}

	public bool CanRemove(DataTable? table)
	{
		return CanRemove(table, fThrowException: false);
	}

	internal bool CanRemove([NotNullWhen(true)] DataTable table, bool fThrowException)
	{
		long scopeId = DataCommonEventSource.Log.EnterScope("<ds.DataTableCollection.CanRemove|INFO> {0}, table={1}, fThrowException={2}", ObjectID, table?.ObjectID ?? 0, fThrowException);
		try
		{
			if (table == null)
			{
				if (!fThrowException)
				{
					return false;
				}
				throw ExceptionBuilder.ArgumentNull("table");
			}
			if (table.DataSet != _dataSet)
			{
				if (!fThrowException)
				{
					return false;
				}
				throw ExceptionBuilder.TableNotInTheDataSet(table.TableName);
			}
			_dataSet.OnRemoveTable(table);
			if (table.ChildRelations.Count != 0 || table.ParentRelations.Count != 0)
			{
				if (!fThrowException)
				{
					return false;
				}
				throw ExceptionBuilder.TableInRelation();
			}
			ParentForeignKeyConstraintEnumerator parentForeignKeyConstraintEnumerator = new ParentForeignKeyConstraintEnumerator(_dataSet, table);
			while (parentForeignKeyConstraintEnumerator.GetNext())
			{
				ForeignKeyConstraint foreignKeyConstraint = parentForeignKeyConstraintEnumerator.GetForeignKeyConstraint();
				if (foreignKeyConstraint.Table != table || foreignKeyConstraint.RelatedTable != table)
				{
					if (!fThrowException)
					{
						return false;
					}
					throw ExceptionBuilder.TableInConstraint(table, foreignKeyConstraint);
				}
			}
			ChildForeignKeyConstraintEnumerator childForeignKeyConstraintEnumerator = new ChildForeignKeyConstraintEnumerator(_dataSet, table);
			while (childForeignKeyConstraintEnumerator.GetNext())
			{
				ForeignKeyConstraint foreignKeyConstraint2 = childForeignKeyConstraintEnumerator.GetForeignKeyConstraint();
				if (foreignKeyConstraint2.Table != table || foreignKeyConstraint2.RelatedTable != table)
				{
					if (!fThrowException)
					{
						return false;
					}
					throw ExceptionBuilder.TableInConstraint(table, foreignKeyConstraint2);
				}
			}
			return true;
		}
		finally
		{
			DataCommonEventSource.Log.ExitScope(scopeId);
		}
	}

	public void Clear()
	{
		long scopeId = DataCommonEventSource.Log.EnterScope("<ds.DataTableCollection.Clear|API> {0}", ObjectID);
		try
		{
			int count = _list.Count;
			DataTable[] array = new DataTable[_list.Count];
			_list.CopyTo(array, 0);
			OnCollectionChanging(InternalDataCollectionBase.s_refreshEventArgs);
			if (_dataSet._fInitInProgress && _delayedAddRangeTables != null)
			{
				_delayedAddRangeTables = null;
			}
			BaseGroupSwitch(array, count, Array.Empty<DataTable>(), 0);
			_list.Clear();
			OnCollectionChanged(InternalDataCollectionBase.s_refreshEventArgs);
		}
		finally
		{
			DataCommonEventSource.Log.ExitScope(scopeId);
		}
	}

	public bool Contains(string? name)
	{
		return InternalIndexOf(name) >= 0;
	}

	public bool Contains(string name, string tableNamespace)
	{
		if (name == null)
		{
			throw ExceptionBuilder.ArgumentNull("name");
		}
		if (tableNamespace == null)
		{
			throw ExceptionBuilder.ArgumentNull("tableNamespace");
		}
		return InternalIndexOf(name, tableNamespace) >= 0;
	}

	internal bool Contains(string name, string tableNamespace, bool checkProperty, bool caseSensitive)
	{
		if (!caseSensitive)
		{
			return InternalIndexOf(name) >= 0;
		}
		int count = _list.Count;
		for (int i = 0; i < count; i++)
		{
			DataTable dataTable = (DataTable)_list[i];
			string text = (checkProperty ? dataTable.Namespace : dataTable._tableNamespace);
			if (NamesEqual(dataTable.TableName, name, fCaseSensitive: true, _dataSet.Locale) == 1 && text == tableNamespace)
			{
				return true;
			}
		}
		return false;
	}

	internal bool Contains(string name, bool caseSensitive)
	{
		if (!caseSensitive)
		{
			return InternalIndexOf(name) >= 0;
		}
		int count = _list.Count;
		for (int i = 0; i < count; i++)
		{
			DataTable dataTable = (DataTable)_list[i];
			if (NamesEqual(dataTable.TableName, name, fCaseSensitive: true, _dataSet.Locale) == 1)
			{
				return true;
			}
		}
		return false;
	}

	public void CopyTo(DataTable[] array, int index)
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
			array[index + i] = (DataTable)_list[i];
		}
	}

	public int IndexOf(DataTable? table)
	{
		int count = _list.Count;
		for (int i = 0; i < count; i++)
		{
			if (table == (DataTable)_list[i])
			{
				return i;
			}
		}
		return -1;
	}

	public int IndexOf(string? tableName)
	{
		int num = InternalIndexOf(tableName);
		if (num >= 0)
		{
			return num;
		}
		return -1;
	}

	public int IndexOf(string tableName, string tableNamespace)
	{
		return IndexOf(tableName, tableNamespace, chekforNull: true);
	}

	internal int IndexOf(string tableName, string tableNamespace, bool chekforNull)
	{
		if (chekforNull)
		{
			if (tableName == null)
			{
				throw ExceptionBuilder.ArgumentNull("tableName");
			}
			if (tableNamespace == null)
			{
				throw ExceptionBuilder.ArgumentNull("tableNamespace");
			}
		}
		int num = InternalIndexOf(tableName, tableNamespace);
		if (num >= 0)
		{
			return num;
		}
		return -1;
	}

	internal void ReplaceFromInference(List<DataTable> tableList)
	{
		_list.Clear();
		_list.AddRange(tableList);
	}

	internal int InternalIndexOf(string tableName)
	{
		int num = -1;
		if (tableName != null && 0 < tableName.Length)
		{
			int count = _list.Count;
			int num2 = 0;
			for (int i = 0; i < count; i++)
			{
				DataTable dataTable = (DataTable)_list[i];
				switch (NamesEqual(dataTable.TableName, tableName, fCaseSensitive: false, _dataSet.Locale))
				{
				case 1:
				{
					for (int j = i + 1; j < count; j++)
					{
						DataTable dataTable2 = (DataTable)_list[j];
						if (NamesEqual(dataTable2.TableName, tableName, fCaseSensitive: false, _dataSet.Locale) == 1)
						{
							return -3;
						}
					}
					return i;
				}
				case -1:
					num = ((num == -1) ? i : (-2));
					break;
				}
			}
		}
		return num;
	}

	internal int InternalIndexOf(string tableName, string tableNamespace)
	{
		int num = -1;
		if (tableName != null && 0 < tableName.Length)
		{
			int count = _list.Count;
			int num2 = 0;
			for (int i = 0; i < count; i++)
			{
				DataTable dataTable = (DataTable)_list[i];
				num2 = NamesEqual(dataTable.TableName, tableName, fCaseSensitive: false, _dataSet.Locale);
				if (num2 == 1 && dataTable.Namespace == tableNamespace)
				{
					return i;
				}
				if (num2 == -1 && dataTable.Namespace == tableNamespace)
				{
					num = ((num == -1) ? i : (-2));
				}
			}
		}
		return num;
	}

	internal void FinishInitCollection()
	{
		if (_delayedAddRangeTables == null)
		{
			return;
		}
		DataTable[] delayedAddRangeTables = _delayedAddRangeTables;
		foreach (DataTable dataTable in delayedAddRangeTables)
		{
			if (dataTable != null)
			{
				Add(dataTable);
			}
		}
		_delayedAddRangeTables = null;
	}

	private string MakeName(int index)
	{
		if (1 != index)
		{
			return "Table" + index.ToString(CultureInfo.InvariantCulture);
		}
		return "Table1";
	}

	private void OnCollectionChanged(CollectionChangeEventArgs ccevent)
	{
		if (_onCollectionChangedDelegate != null)
		{
			DataCommonEventSource.Log.Trace("<ds.DataTableCollection.OnCollectionChanged|INFO> {0}", ObjectID);
			_onCollectionChangedDelegate(this, ccevent);
		}
	}

	private void OnCollectionChanging(CollectionChangeEventArgs ccevent)
	{
		if (_onCollectionChangingDelegate != null)
		{
			DataCommonEventSource.Log.Trace("<ds.DataTableCollection.OnCollectionChanging|INFO> {0}", ObjectID);
			_onCollectionChangingDelegate(this, ccevent);
		}
	}

	internal void RegisterName(string name, string tbNamespace)
	{
		DataCommonEventSource.Log.Trace("<ds.DataTableCollection.RegisterName|INFO> {0}, name='{1}', tbNamespace='{2}'", ObjectID, name, tbNamespace);
		CultureInfo locale = _dataSet.Locale;
		int count = _list.Count;
		for (int i = 0; i < count; i++)
		{
			DataTable dataTable = (DataTable)_list[i];
			if (NamesEqual(name, dataTable.TableName, fCaseSensitive: true, locale) != 0 && tbNamespace == dataTable.Namespace)
			{
				throw ExceptionBuilder.DuplicateTableName(((DataTable)_list[i]).TableName);
			}
		}
		if (NamesEqual(name, MakeName(_defaultNameIndex), fCaseSensitive: true, locale) != 0)
		{
			_defaultNameIndex++;
		}
	}

	public void Remove(DataTable table)
	{
		long scopeId = DataCommonEventSource.Log.EnterScope("<ds.DataTableCollection.Remove|API> {0}, table={1}", ObjectID, table?.ObjectID ?? 0);
		try
		{
			OnCollectionChanging(new CollectionChangeEventArgs(CollectionChangeAction.Remove, table));
			BaseRemove(table);
			OnCollectionChanged(new CollectionChangeEventArgs(CollectionChangeAction.Remove, table));
		}
		finally
		{
			DataCommonEventSource.Log.ExitScope(scopeId);
		}
	}

	public void RemoveAt(int index)
	{
		long scopeId = DataCommonEventSource.Log.EnterScope("<ds.DataTableCollection.RemoveAt|API> {0}, index={1}", ObjectID, index);
		try
		{
			DataTable dataTable = this[index];
			if (dataTable == null)
			{
				throw ExceptionBuilder.TableOutOfRange(index);
			}
			Remove(dataTable);
		}
		finally
		{
			DataCommonEventSource.Log.ExitScope(scopeId);
		}
	}

	public void Remove(string name)
	{
		long scopeId = DataCommonEventSource.Log.EnterScope("<ds.DataTableCollection.Remove|API> {0}, name='{1}'", ObjectID, name);
		try
		{
			DataTable dataTable = this[name];
			if (dataTable == null)
			{
				throw ExceptionBuilder.TableNotInTheDataSet(name);
			}
			Remove(dataTable);
		}
		finally
		{
			DataCommonEventSource.Log.ExitScope(scopeId);
		}
	}

	public void Remove(string name, string tableNamespace)
	{
		if (name == null)
		{
			throw ExceptionBuilder.ArgumentNull("name");
		}
		if (tableNamespace == null)
		{
			throw ExceptionBuilder.ArgumentNull("tableNamespace");
		}
		DataTable dataTable = this[name, tableNamespace];
		if (dataTable == null)
		{
			throw ExceptionBuilder.TableNotInTheDataSet(name);
		}
		Remove(dataTable);
	}

	internal void UnregisterName(string name)
	{
		DataCommonEventSource.Log.Trace("<ds.DataTableCollection.UnregisterName|INFO> {0}, name='{1}'", ObjectID, name);
		if (NamesEqual(name, MakeName(_defaultNameIndex - 1), fCaseSensitive: true, _dataSet.Locale) != 0)
		{
			do
			{
				_defaultNameIndex--;
			}
			while (_defaultNameIndex > 1 && !Contains(MakeName(_defaultNameIndex - 1)));
		}
	}
}
