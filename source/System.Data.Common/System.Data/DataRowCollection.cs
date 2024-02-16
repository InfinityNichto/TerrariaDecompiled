using System.Collections;

namespace System.Data;

public sealed class DataRowCollection : InternalDataCollectionBase
{
	private sealed class DataRowTree : RBTree<DataRow>
	{
		internal DataRowTree()
			: base(TreeAccessMethod.INDEX_ONLY)
		{
		}

		protected override int CompareNode(DataRow record1, DataRow record2)
		{
			throw ExceptionBuilder.InternalRBTreeError(RBTreeError.CompareNodeInDataRowTree);
		}

		protected override int CompareSateliteTreeNode(DataRow record1, DataRow record2)
		{
			throw ExceptionBuilder.InternalRBTreeError(RBTreeError.CompareSateliteTreeNodeInDataRowTree);
		}
	}

	private readonly DataTable _table;

	private readonly DataRowTree _list = new DataRowTree();

	internal int _nullInList;

	public override int Count => _list.Count;

	public DataRow this[int index] => _list[index];

	internal DataRowCollection(DataTable table)
	{
		_table = table;
	}

	public void Add(DataRow row)
	{
		_table.AddRow(row, -1);
	}

	public void InsertAt(DataRow row, int pos)
	{
		if (pos < 0)
		{
			throw ExceptionBuilder.RowInsertOutOfRange(pos);
		}
		if (pos >= _list.Count)
		{
			_table.AddRow(row, -1);
		}
		else
		{
			_table.InsertRow(row, -1, pos);
		}
	}

	internal void DiffInsertAt(DataRow row, int pos)
	{
		if (pos < 0 || pos == _list.Count)
		{
			_table.AddRow(row, (pos > -1) ? (pos + 1) : (-1));
		}
		else if (_table.NestedParentRelations.Length != 0)
		{
			if (pos < _list.Count)
			{
				if (_list[pos] != null)
				{
					throw ExceptionBuilder.RowInsertTwice(pos, _table.TableName);
				}
				_list.RemoveAt(pos);
				_nullInList--;
				_table.InsertRow(row, pos + 1, pos);
			}
			else
			{
				while (pos > _list.Count)
				{
					_list.Add(null);
					_nullInList++;
				}
				_table.AddRow(row, pos + 1);
			}
		}
		else
		{
			_table.InsertRow(row, pos + 1, (pos > _list.Count) ? (-1) : pos);
		}
	}

	public int IndexOf(DataRow? row)
	{
		if (row != null && row.Table == _table && (row.RBTreeNodeId != 0 || row.RowState != DataRowState.Detached))
		{
			return _list.IndexOf(row.RBTreeNodeId, row);
		}
		return -1;
	}

	internal DataRow AddWithColumnEvents(params object[] values)
	{
		DataRow dataRow = _table.NewRow(-1);
		dataRow.ItemArray = values;
		_table.AddRow(dataRow, -1);
		return dataRow;
	}

	public DataRow Add(params object?[] values)
	{
		int record = _table.NewRecordFromArray(values);
		DataRow dataRow = _table.NewRow(record);
		_table.AddRow(dataRow, -1);
		return dataRow;
	}

	internal void ArrayAdd(DataRow row)
	{
		row.RBTreeNodeId = _list.Add(row);
	}

	internal void ArrayInsert(DataRow row, int pos)
	{
		row.RBTreeNodeId = _list.Insert(pos, row);
	}

	internal void ArrayClear()
	{
		_list.Clear();
	}

	internal void ArrayRemove(DataRow row)
	{
		if (row.RBTreeNodeId == 0)
		{
			throw ExceptionBuilder.InternalRBTreeError(RBTreeError.AttachedNodeWithZerorbTreeNodeId);
		}
		_list.RBDelete(row.RBTreeNodeId);
		row.RBTreeNodeId = 0;
	}

	public DataRow? Find(object? key)
	{
		return _table.FindByPrimaryKey(key);
	}

	public DataRow? Find(object?[] keys)
	{
		return _table.FindByPrimaryKey(keys);
	}

	public void Clear()
	{
		_table.Clear(clearAll: false);
	}

	public bool Contains(object? key)
	{
		return _table.FindByPrimaryKey(key) != null;
	}

	public bool Contains(object?[] keys)
	{
		return _table.FindByPrimaryKey(keys) != null;
	}

	public override void CopyTo(Array ar, int index)
	{
		_list.CopyTo(ar, index);
	}

	public void CopyTo(DataRow[] array, int index)
	{
		_list.CopyTo(array, index);
	}

	public override IEnumerator GetEnumerator()
	{
		return _list.GetEnumerator();
	}

	public void Remove(DataRow row)
	{
		if (row == null || row.Table != _table || -1 == row.rowID)
		{
			throw ExceptionBuilder.RowOutOfRange();
		}
		if (row.RowState != DataRowState.Deleted && row.RowState != DataRowState.Detached)
		{
			row.Delete();
		}
		if (row.RowState != DataRowState.Detached)
		{
			row.AcceptChanges();
		}
	}

	public void RemoveAt(int index)
	{
		Remove(this[index]);
	}
}
