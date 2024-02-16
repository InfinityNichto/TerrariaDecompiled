using System.Collections.Generic;
using System.ComponentModel;
using System.Threading;

namespace System.Data;

internal sealed class Index
{
	private sealed class IndexTree : RBTree<int>
	{
		private readonly Index _index;

		internal IndexTree(Index index)
			: base(TreeAccessMethod.KEY_SEARCH_AND_INDEX)
		{
			_index = index;
		}

		protected override int CompareNode(int record1, int record2)
		{
			return _index.CompareRecords(record1, record2);
		}

		protected override int CompareSateliteTreeNode(int record1, int record2)
		{
			return _index.CompareDuplicateRecords(record1, record2);
		}
	}

	internal delegate int ComparisonBySelector<TKey, TRow>(TKey key, TRow row) where TRow : DataRow;

	private readonly DataTable _table;

	internal readonly IndexField[] _indexFields;

	private readonly Comparison<DataRow> _comparison;

	private readonly DataViewRowState _recordStates;

	private readonly WeakReference _rowFilter;

	private IndexTree _records;

	private int _recordCount;

	private int _refCount;

	private readonly Listeners<DataViewListener> _listeners;

	private bool _suspendEvents;

	private readonly bool _isSharable;

	private readonly bool _hasRemoteAggregate;

	private static int s_objectTypeCount;

	private readonly int _objectID = Interlocked.Increment(ref s_objectTypeCount);

	internal bool HasRemoteAggregate => _hasRemoteAggregate;

	internal int ObjectID => _objectID;

	public DataViewRowState RecordStates => _recordStates;

	public IFilter RowFilter => (IFilter)((_rowFilter != null) ? _rowFilter.Target : null);

	public bool HasDuplicates => _records.HasDuplicates;

	public int RecordCount => _recordCount;

	public bool IsSharable => _isSharable;

	public int RefCount => _refCount;

	private bool DoListChanged
	{
		get
		{
			if (!_suspendEvents && _listeners.HasListeners)
			{
				return !_table.AreIndexEventsSuspended;
			}
			return false;
		}
	}

	public Index(DataTable table, IndexField[] indexFields, DataViewRowState recordStates, IFilter rowFilter)
		: this(table, indexFields, null, recordStates, rowFilter)
	{
	}

	public Index(DataTable table, Comparison<DataRow> comparison, DataViewRowState recordStates, IFilter rowFilter)
		: this(table, GetAllFields(table.Columns), comparison, recordStates, rowFilter)
	{
	}

	private static IndexField[] GetAllFields(DataColumnCollection columns)
	{
		IndexField[] array = new IndexField[columns.Count];
		for (int i = 0; i < array.Length; i++)
		{
			array[i] = new IndexField(columns[i], isDescending: false);
		}
		return array;
	}

	private Index(DataTable table, IndexField[] indexFields, Comparison<DataRow> comparison, DataViewRowState recordStates, IFilter rowFilter)
	{
		DataCommonEventSource.Log.Trace("<ds.Index.Index|API> {0}, table={1}, recordStates={2}", ObjectID, table?.ObjectID ?? 0, recordStates);
		if (((uint)recordStates & 0xFFFFFFC1u) != 0)
		{
			throw ExceptionBuilder.RecordStateRange();
		}
		_table = table;
		_listeners = new Listeners<DataViewListener>(ObjectID, (DataViewListener listener) => listener != null);
		_indexFields = indexFields;
		_recordStates = recordStates;
		_comparison = comparison;
		_isSharable = rowFilter == null && comparison == null;
		if (rowFilter != null)
		{
			_rowFilter = new WeakReference(rowFilter);
			if (rowFilter is DataExpression dataExpression)
			{
				_hasRemoteAggregate = dataExpression.HasRemoteAggregate();
			}
		}
		InitRecords(rowFilter);
	}

	public bool Equal(IndexField[] indexDesc, DataViewRowState recordStates, IFilter rowFilter)
	{
		if (!_isSharable || _indexFields.Length != indexDesc.Length || _recordStates != recordStates || rowFilter != null)
		{
			return false;
		}
		for (int i = 0; i < _indexFields.Length; i++)
		{
			if (_indexFields[i].Column != indexDesc[i].Column || _indexFields[i].IsDescending != indexDesc[i].IsDescending)
			{
				return false;
			}
		}
		return true;
	}

	public int GetRecord(int recordIndex)
	{
		return _records[recordIndex];
	}

	private bool AcceptRecord(int record)
	{
		return AcceptRecord(record, RowFilter);
	}

	private bool AcceptRecord(int record, IFilter filter)
	{
		DataCommonEventSource.Log.Trace("<ds.Index.AcceptRecord|API> {0}, record={1}", ObjectID, record);
		if (filter == null)
		{
			return true;
		}
		DataRow dataRow = _table._recordManager[record];
		if (dataRow == null)
		{
			return true;
		}
		DataRowVersion version = DataRowVersion.Default;
		if (dataRow._oldRecord == record)
		{
			version = DataRowVersion.Original;
		}
		else if (dataRow._newRecord == record)
		{
			version = DataRowVersion.Current;
		}
		else if (dataRow._tempRecord == record)
		{
			version = DataRowVersion.Proposed;
		}
		return filter.Invoke(dataRow, version);
	}

	internal void ListChangedAdd(DataViewListener listener)
	{
		_listeners.Add(listener);
	}

	internal void ListChangedRemove(DataViewListener listener)
	{
		_listeners.Remove(listener);
	}

	public void AddRef()
	{
		DataCommonEventSource.Log.Trace("<ds.Index.AddRef|API> {0}", ObjectID);
		_table._indexesLock.EnterWriteLock();
		try
		{
			if (_refCount == 0)
			{
				_table.ShadowIndexCopy();
				_table._indexes.Add(this);
			}
			_refCount++;
		}
		finally
		{
			_table._indexesLock.ExitWriteLock();
		}
	}

	public int RemoveRef()
	{
		DataCommonEventSource.Log.Trace("<ds.Index.RemoveRef|API> {0}", ObjectID);
		_table._indexesLock.EnterWriteLock();
		int result;
		try
		{
			result = --_refCount;
			if (_refCount <= 0)
			{
				_table.ShadowIndexCopy();
				_table._indexes.Remove(this);
			}
		}
		finally
		{
			_table._indexesLock.ExitWriteLock();
		}
		return result;
	}

	private void ApplyChangeAction(int record, int action, int changeRecord)
	{
		if (action == 0)
		{
			return;
		}
		if (action > 0)
		{
			if (AcceptRecord(record))
			{
				InsertRecord(record, fireEvent: true);
			}
		}
		else if (_comparison != null && -1 != record)
		{
			DeleteRecord(GetIndex(record, changeRecord));
		}
		else
		{
			DeleteRecord(GetIndex(record));
		}
	}

	public bool CheckUnique()
	{
		return !HasDuplicates;
	}

	private int CompareRecords(int record1, int record2)
	{
		if (_comparison != null)
		{
			return CompareDataRows(record1, record2);
		}
		if (_indexFields.Length != 0)
		{
			for (int i = 0; i < _indexFields.Length; i++)
			{
				int num = _indexFields[i].Column.Compare(record1, record2);
				if (num != 0)
				{
					if (!_indexFields[i].IsDescending)
					{
						return num;
					}
					return -num;
				}
			}
			return 0;
		}
		DataRow dataRow = _table._recordManager[record1];
		DataRow dataRow2 = _table._recordManager[record2];
		DataRow row = dataRow;
		DataRow row2 = dataRow2;
		return _table.Rows.IndexOf(row).CompareTo(_table.Rows.IndexOf(row2));
	}

	private int CompareDataRows(int record1, int record2)
	{
		return _comparison(_table._recordManager[record1], _table._recordManager[record2]);
	}

	private int CompareDuplicateRecords(int record1, int record2)
	{
		DataRow dataRow = _table._recordManager[record1];
		DataRow dataRow2 = _table._recordManager[record2];
		DataRow dataRow3 = dataRow;
		DataRow dataRow4 = dataRow2;
		if (dataRow3 == null)
		{
			if (dataRow4 != null)
			{
				return -1;
			}
			return 0;
		}
		if (dataRow4 == null)
		{
			return 1;
		}
		int num = dataRow3.rowID.CompareTo(dataRow4.rowID);
		if (num == 0 && record1 != record2)
		{
			num = ((int)dataRow3.GetRecordState(record1)).CompareTo((int)dataRow4.GetRecordState(record2));
		}
		return num;
	}

	private int CompareRecordToKey(int record1, object[] vals)
	{
		for (int i = 0; i < _indexFields.Length; i++)
		{
			int num = _indexFields[i].Column.CompareValueTo(record1, vals[i]);
			if (num != 0)
			{
				if (!_indexFields[i].IsDescending)
				{
					return num;
				}
				return -num;
			}
		}
		return 0;
	}

	public void DeleteRecordFromIndex(int recordIndex)
	{
		DeleteRecord(recordIndex, fireEvent: false);
	}

	private void DeleteRecord(int recordIndex)
	{
		DeleteRecord(recordIndex, fireEvent: true);
	}

	private void DeleteRecord(int recordIndex, bool fireEvent)
	{
		DataCommonEventSource.Log.Trace("<ds.Index.DeleteRecord|INFO> {0}, recordIndex={1}, fireEvent={2}", ObjectID, recordIndex, fireEvent);
		if (recordIndex >= 0)
		{
			_recordCount--;
			int record = _records.DeleteByIndex(recordIndex);
			MaintainDataView(ListChangedType.ItemDeleted, record, !fireEvent);
			if (fireEvent)
			{
				OnListChanged(ListChangedType.ItemDeleted, recordIndex);
			}
		}
	}

	public RBTree<int>.RBTreeEnumerator GetEnumerator(int startIndex)
	{
		return new RBTree<int>.RBTreeEnumerator(_records, startIndex);
	}

	public int GetIndex(int record)
	{
		return _records.GetIndexByKey(record);
	}

	private int GetIndex(int record, int changeRecord)
	{
		DataRow dataRow = _table._recordManager[record];
		int newRecord = dataRow._newRecord;
		int oldRecord = dataRow._oldRecord;
		try
		{
			switch (changeRecord)
			{
			case 1:
				dataRow._newRecord = record;
				break;
			case 2:
				dataRow._oldRecord = record;
				break;
			}
			return _records.GetIndexByKey(record);
		}
		finally
		{
			switch (changeRecord)
			{
			case 1:
				dataRow._newRecord = newRecord;
				break;
			case 2:
				dataRow._oldRecord = oldRecord;
				break;
			}
		}
	}

	public object[] GetUniqueKeyValues()
	{
		if (_indexFields == null || _indexFields.Length == 0)
		{
			return Array.Empty<object>();
		}
		List<object[]> list = new List<object[]>();
		GetUniqueKeyValues(list, _records.root);
		return list.ToArray();
	}

	public int FindRecord(int record)
	{
		int num = _records.Search(record);
		if (num != 0)
		{
			return _records.GetIndexByNode(num);
		}
		return -1;
	}

	public int FindRecordByKey(object key)
	{
		int num = FindNodeByKey(key);
		if (num != 0)
		{
			return _records.GetIndexByNode(num);
		}
		return -1;
	}

	public int FindRecordByKey(object[] key)
	{
		int num = FindNodeByKeys(key);
		if (num != 0)
		{
			return _records.GetIndexByNode(num);
		}
		return -1;
	}

	private int FindNodeByKey(object originalKey)
	{
		if (_indexFields.Length != 1)
		{
			throw ExceptionBuilder.IndexKeyLength(_indexFields.Length, 1);
		}
		int num = _records.root;
		if (num != 0)
		{
			DataColumn column = _indexFields[0].Column;
			object value = column.ConvertValue(originalKey);
			num = _records.root;
			if (_indexFields[0].IsDescending)
			{
				while (num != 0)
				{
					int num2 = column.CompareValueTo(_records.Key(num), value);
					if (num2 == 0)
					{
						break;
					}
					num = ((num2 >= 0) ? _records.Right(num) : _records.Left(num));
				}
			}
			else
			{
				while (num != 0)
				{
					int num2 = column.CompareValueTo(_records.Key(num), value);
					if (num2 == 0)
					{
						break;
					}
					num = ((num2 <= 0) ? _records.Right(num) : _records.Left(num));
				}
			}
		}
		return num;
	}

	private int FindNodeByKeys(object[] originalKey)
	{
		int num = ((originalKey != null) ? originalKey.Length : 0);
		if (originalKey == null || num == 0 || _indexFields.Length != num)
		{
			throw ExceptionBuilder.IndexKeyLength(_indexFields.Length, num);
		}
		int num2 = _records.root;
		if (num2 != 0)
		{
			object[] array = new object[originalKey.Length];
			for (int i = 0; i < originalKey.Length; i++)
			{
				array[i] = _indexFields[i].Column.ConvertValue(originalKey[i]);
			}
			for (num2 = _records.root; num2 != 0; num2 = ((num <= 0) ? _records.Right(num2) : _records.Left(num2)))
			{
				num = CompareRecordToKey(_records.Key(num2), array);
				if (num == 0)
				{
					break;
				}
			}
		}
		return num2;
	}

	private int FindNodeByKeyRecord(int record)
	{
		int num = _records.root;
		if (num != 0)
		{
			num = _records.root;
			while (num != 0)
			{
				int num2 = CompareRecords(_records.Key(num), record);
				if (num2 == 0)
				{
					break;
				}
				num = ((num2 <= 0) ? _records.Right(num) : _records.Left(num));
			}
		}
		return num;
	}

	internal Range FindRecords<TKey, TRow>(ComparisonBySelector<TKey, TRow> comparison, TKey key) where TRow : DataRow
	{
		int num = _records.root;
		while (num != 0)
		{
			int num2 = comparison(key, (TRow)_table._recordManager[_records.Key(num)]);
			if (num2 == 0)
			{
				break;
			}
			num = ((num2 >= 0) ? _records.Right(num) : _records.Left(num));
		}
		return GetRangeFromNode(num);
	}

	private Range GetRangeFromNode(int nodeId)
	{
		if (nodeId == 0)
		{
			return default(Range);
		}
		int indexByNode = _records.GetIndexByNode(nodeId);
		if (_records.Next(nodeId) == 0)
		{
			return new Range(indexByNode, indexByNode);
		}
		int num = _records.SubTreeSize(_records.Next(nodeId));
		return new Range(indexByNode, indexByNode + num - 1);
	}

	public Range FindRecords(object key)
	{
		int nodeId = FindNodeByKey(key);
		return GetRangeFromNode(nodeId);
	}

	public Range FindRecords(object[] key)
	{
		int nodeId = FindNodeByKeys(key);
		return GetRangeFromNode(nodeId);
	}

	internal void FireResetEvent()
	{
		DataCommonEventSource.Log.Trace("<ds.Index.FireResetEvent|API> {0}", ObjectID);
		if (DoListChanged)
		{
			OnListChanged(DataView.s_resetEventArgs);
		}
	}

	private int GetChangeAction(DataViewRowState oldState, DataViewRowState newState)
	{
		int num = (((_recordStates & oldState) != 0) ? 1 : 0);
		int num2 = (((_recordStates & newState) != 0) ? 1 : 0);
		return num2 - num;
	}

	private static int GetReplaceAction(DataViewRowState oldState)
	{
		if ((DataViewRowState.CurrentRows & oldState) == 0)
		{
			if ((DataViewRowState.OriginalRows & oldState) == 0)
			{
				return 0;
			}
			return 2;
		}
		return 1;
	}

	public DataRow GetRow(int i)
	{
		return _table._recordManager[GetRecord(i)];
	}

	public DataRow[] GetRows(object[] values)
	{
		return GetRows(FindRecords(values));
	}

	public DataRow[] GetRows(Range range)
	{
		DataRow[] array = _table.NewRowArray(range.Count);
		if (array.Length != 0)
		{
			RBTree<int>.RBTreeEnumerator enumerator = GetEnumerator(range.Min);
			for (int i = 0; i < array.Length; i++)
			{
				if (!enumerator.MoveNext())
				{
					break;
				}
				array[i] = _table._recordManager[enumerator.Current];
			}
		}
		return array;
	}

	private void InitRecords(IFilter filter)
	{
		DataViewRowState recordStates = _recordStates;
		bool append = _indexFields.Length == 0;
		_records = new IndexTree(this);
		_recordCount = 0;
		foreach (DataRow row in _table.Rows)
		{
			int num = -1;
			if (row._oldRecord == row._newRecord)
			{
				if ((recordStates & DataViewRowState.Unchanged) != 0)
				{
					num = row._oldRecord;
				}
			}
			else if (row._oldRecord == -1)
			{
				if ((recordStates & DataViewRowState.Added) != 0)
				{
					num = row._newRecord;
				}
			}
			else if (row._newRecord == -1)
			{
				if ((recordStates & DataViewRowState.Deleted) != 0)
				{
					num = row._oldRecord;
				}
			}
			else if ((recordStates & DataViewRowState.ModifiedCurrent) != 0)
			{
				num = row._newRecord;
			}
			else if ((recordStates & DataViewRowState.ModifiedOriginal) != 0)
			{
				num = row._oldRecord;
			}
			if (num != -1 && AcceptRecord(num, filter))
			{
				_records.InsertAt(-1, num, append);
				_recordCount++;
			}
		}
	}

	public int InsertRecordToIndex(int record)
	{
		int result = -1;
		if (AcceptRecord(record))
		{
			result = InsertRecord(record, fireEvent: false);
		}
		return result;
	}

	private int InsertRecord(int record, bool fireEvent)
	{
		DataCommonEventSource.Log.Trace("<ds.Index.InsertRecord|INFO> {0}, record={1}, fireEvent={2}", ObjectID, record, fireEvent);
		bool append = false;
		if (_indexFields.Length == 0 && _table != null)
		{
			DataRow row = _table._recordManager[record];
			append = _table.Rows.IndexOf(row) + 1 == _table.Rows.Count;
		}
		int node = _records.InsertAt(-1, record, append);
		_recordCount++;
		MaintainDataView(ListChangedType.ItemAdded, record, !fireEvent);
		if (fireEvent)
		{
			if (DoListChanged)
			{
				OnListChanged(ListChangedType.ItemAdded, _records.GetIndexByNode(node));
			}
			return 0;
		}
		return _records.GetIndexByNode(node);
	}

	public bool IsKeyInIndex(object key)
	{
		int num = FindNodeByKey(key);
		return num != 0;
	}

	public bool IsKeyInIndex(object[] key)
	{
		int num = FindNodeByKeys(key);
		return num != 0;
	}

	public bool IsKeyRecordInIndex(int record)
	{
		int num = FindNodeByKeyRecord(record);
		return num != 0;
	}

	private void OnListChanged(ListChangedType changedType, int newIndex, int oldIndex)
	{
		if (DoListChanged)
		{
			OnListChanged(new ListChangedEventArgs(changedType, newIndex, oldIndex));
		}
	}

	private void OnListChanged(ListChangedType changedType, int index)
	{
		if (DoListChanged)
		{
			OnListChanged(new ListChangedEventArgs(changedType, index));
		}
	}

	private void OnListChanged(ListChangedEventArgs e)
	{
		DataCommonEventSource.Log.Trace("<ds.Index.OnListChanged|INFO> {0}", ObjectID);
		_listeners.Notify(e, arg2: false, arg3: false, delegate(DataViewListener listener, ListChangedEventArgs args, bool arg2, bool arg3)
		{
			listener.IndexListChanged(args);
		});
	}

	private void MaintainDataView(ListChangedType changedType, int record, bool trackAddRemove)
	{
		_listeners.Notify(changedType, (0 <= record) ? _table._recordManager[record] : null, trackAddRemove, delegate(DataViewListener listener, ListChangedType type, DataRow row, bool track)
		{
			listener.MaintainDataView(changedType, row, track);
		});
	}

	public void Reset()
	{
		DataCommonEventSource.Log.Trace("<ds.Index.Reset|API> {0}", ObjectID);
		InitRecords(RowFilter);
		MaintainDataView(ListChangedType.Reset, -1, trackAddRemove: false);
		FireResetEvent();
	}

	public void RecordChanged(int record)
	{
		DataCommonEventSource.Log.Trace("<ds.Index.RecordChanged|API> {0}, record={1}", ObjectID, record);
		if (DoListChanged)
		{
			int index = GetIndex(record);
			if (index >= 0)
			{
				OnListChanged(ListChangedType.ItemChanged, index);
			}
		}
	}

	public void RecordChanged(int oldIndex, int newIndex)
	{
		DataCommonEventSource.Log.Trace("<ds.Index.RecordChanged|API> {0}, oldIndex={1}, newIndex={2}", ObjectID, oldIndex, newIndex);
		if (oldIndex > -1 || newIndex > -1)
		{
			if (oldIndex == newIndex)
			{
				OnListChanged(ListChangedType.ItemChanged, newIndex, oldIndex);
			}
			else if (oldIndex == -1)
			{
				OnListChanged(ListChangedType.ItemAdded, newIndex, oldIndex);
			}
			else if (newIndex == -1)
			{
				OnListChanged(ListChangedType.ItemDeleted, oldIndex);
			}
			else
			{
				OnListChanged(ListChangedType.ItemMoved, newIndex, oldIndex);
			}
		}
	}

	public void RecordStateChanged(int record, DataViewRowState oldState, DataViewRowState newState)
	{
		DataCommonEventSource.Log.Trace("<ds.Index.RecordStateChanged|API> {0}, record={1}, oldState={2}, newState={3}", ObjectID, record, oldState, newState);
		int changeAction = GetChangeAction(oldState, newState);
		ApplyChangeAction(record, changeAction, GetReplaceAction(oldState));
	}

	public void RecordStateChanged(int oldRecord, DataViewRowState oldOldState, DataViewRowState oldNewState, int newRecord, DataViewRowState newOldState, DataViewRowState newNewState)
	{
		DataCommonEventSource.Log.Trace("<ds.Index.RecordStateChanged|API> {0}, oldRecord={1}, oldOldState={2}, oldNewState={3}, newRecord={4}, newOldState={5}, newNewState={6}", ObjectID, oldRecord, oldOldState, oldNewState, newRecord, newOldState, newNewState);
		int changeAction = GetChangeAction(oldOldState, oldNewState);
		int changeAction2 = GetChangeAction(newOldState, newNewState);
		if (changeAction == -1 && changeAction2 == 1 && AcceptRecord(newRecord))
		{
			int num = ((_comparison == null || changeAction >= 0) ? GetIndex(oldRecord) : GetIndex(oldRecord, GetReplaceAction(oldOldState)));
			if (_comparison == null && num != -1 && CompareRecords(oldRecord, newRecord) == 0)
			{
				_records.UpdateNodeKey(oldRecord, newRecord);
				int index = GetIndex(newRecord);
				OnListChanged(ListChangedType.ItemChanged, index, index);
				return;
			}
			_suspendEvents = true;
			if (num != -1)
			{
				_records.DeleteByIndex(num);
				_recordCount--;
			}
			_records.Insert(newRecord);
			_recordCount++;
			_suspendEvents = false;
			int index2 = GetIndex(newRecord);
			if (num == index2)
			{
				OnListChanged(ListChangedType.ItemChanged, index2, num);
			}
			else if (num == -1)
			{
				MaintainDataView(ListChangedType.ItemAdded, newRecord, trackAddRemove: false);
				OnListChanged(ListChangedType.ItemAdded, GetIndex(newRecord));
			}
			else
			{
				OnListChanged(ListChangedType.ItemMoved, index2, num);
			}
		}
		else
		{
			ApplyChangeAction(oldRecord, changeAction, GetReplaceAction(oldOldState));
			ApplyChangeAction(newRecord, changeAction2, GetReplaceAction(newOldState));
		}
	}

	private void GetUniqueKeyValues(List<object[]> list, int curNodeId)
	{
		if (curNodeId != 0)
		{
			GetUniqueKeyValues(list, _records.Left(curNodeId));
			int record = _records.Key(curNodeId);
			object[] array = new object[_indexFields.Length];
			for (int i = 0; i < array.Length; i++)
			{
				array[i] = _indexFields[i].Column[record];
			}
			list.Add(array);
			GetUniqueKeyValues(list, _records.Right(curNodeId));
		}
	}

	internal static int IndexOfReference<T>(List<T> list, T item) where T : class
	{
		if (list != null)
		{
			for (int i = 0; i < list.Count; i++)
			{
				if (list[i] == item)
				{
					return i;
				}
			}
		}
		return -1;
	}
}
