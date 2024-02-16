using System.Collections.Generic;
using System.Data.Common;

namespace System.Data;

internal sealed class RecordManager
{
	private readonly DataTable _table;

	private int _lastFreeRecord;

	private int _minimumCapacity = 50;

	private int _recordCapacity;

	private readonly List<int> _freeRecordList = new List<int>();

	private DataRow[] _rows;

	internal int LastFreeRecord => _lastFreeRecord;

	internal int MinimumCapacity
	{
		get
		{
			return _minimumCapacity;
		}
		set
		{
			if (_minimumCapacity != value)
			{
				if (value < 0)
				{
					throw ExceptionBuilder.NegativeMinimumCapacity();
				}
				_minimumCapacity = value;
			}
		}
	}

	internal int RecordCapacity
	{
		get
		{
			return _recordCapacity;
		}
		set
		{
			if (_recordCapacity != value)
			{
				for (int i = 0; i < _table.Columns.Count; i++)
				{
					_table.Columns[i].SetCapacity(value);
				}
				_recordCapacity = value;
			}
		}
	}

	internal DataRow this[int record]
	{
		get
		{
			return _rows[record];
		}
		set
		{
			_rows[record] = value;
		}
	}

	internal RecordManager(DataTable table)
	{
		if (table == null)
		{
			throw ExceptionBuilder.ArgumentNull("table");
		}
		_table = table;
	}

	private void GrowRecordCapacity()
	{
		RecordCapacity = ((NewCapacity(_recordCapacity) < NormalizedMinimumCapacity(_minimumCapacity)) ? NormalizedMinimumCapacity(_minimumCapacity) : NewCapacity(_recordCapacity));
		DataRow[] array = _table.NewRowArray(_recordCapacity);
		if (_rows != null)
		{
			Array.Copy(_rows, array, Math.Min(_lastFreeRecord, _rows.Length));
		}
		_rows = array;
	}

	internal static int NewCapacity(int capacity)
	{
		if (capacity >= 128)
		{
			return capacity + capacity;
		}
		return 128;
	}

	private int NormalizedMinimumCapacity(int capacity)
	{
		if (capacity < 1014)
		{
			if (capacity < 246)
			{
				if (capacity < 54)
				{
					return 64;
				}
				return 256;
			}
			return 1024;
		}
		return (capacity + 10 >> 10) + 1 << 10;
	}

	internal int NewRecordBase()
	{
		int result;
		if (_freeRecordList.Count != 0)
		{
			result = _freeRecordList[_freeRecordList.Count - 1];
			_freeRecordList.RemoveAt(_freeRecordList.Count - 1);
		}
		else
		{
			if (_lastFreeRecord >= _recordCapacity)
			{
				GrowRecordCapacity();
			}
			result = _lastFreeRecord;
			_lastFreeRecord++;
		}
		return result;
	}

	internal void FreeRecord(ref int record)
	{
		if (-1 != record)
		{
			_rows[record] = null;
			int count = _table._columnCollection.Count;
			for (int i = 0; i < count; i++)
			{
				_table._columnCollection[i].FreeRecord(record);
			}
			if (_lastFreeRecord == record + 1)
			{
				_lastFreeRecord--;
			}
			else if (record < _lastFreeRecord)
			{
				_freeRecordList.Add(record);
			}
			record = -1;
		}
	}

	internal void Clear(bool clearAll)
	{
		if (clearAll)
		{
			for (int i = 0; i < _recordCapacity; i++)
			{
				_rows[i] = null;
			}
			int count = _table._columnCollection.Count;
			for (int j = 0; j < count; j++)
			{
				DataColumn dataColumn = _table._columnCollection[j];
				for (int k = 0; k < _recordCapacity; k++)
				{
					dataColumn.FreeRecord(k);
				}
			}
			_lastFreeRecord = 0;
			_freeRecordList.Clear();
			return;
		}
		_freeRecordList.Capacity = _freeRecordList.Count + _table.Rows.Count;
		for (int l = 0; l < _recordCapacity; l++)
		{
			DataRow dataRow = _rows[l];
			if (dataRow != null && dataRow.rowID != -1)
			{
				int record = l;
				FreeRecord(ref record);
			}
		}
	}

	internal int ImportRecord(DataTable src, int record)
	{
		return CopyRecord(src, record, -1);
	}

	internal int CopyRecord(DataTable src, int record, int copy)
	{
		if (record == -1)
		{
			return copy;
		}
		int record2 = -1;
		try
		{
			record2 = ((copy == -1) ? _table.NewUninitializedRecord() : copy);
			int count = _table.Columns.Count;
			for (int i = 0; i < count; i++)
			{
				DataColumn dataColumn = _table.Columns[i];
				DataColumn dataColumn2 = src.Columns[dataColumn.ColumnName];
				if (dataColumn2 != null)
				{
					object obj = dataColumn2[record];
					if (obj is ICloneable cloneable)
					{
						dataColumn[record2] = cloneable.Clone();
					}
					else
					{
						dataColumn[record2] = obj;
					}
				}
				else if (-1 == copy)
				{
					dataColumn.Init(record2);
				}
			}
			return record2;
		}
		catch (Exception e) when (ADP.IsCatchableOrSecurityExceptionType(e))
		{
			if (-1 == copy)
			{
				FreeRecord(ref record2);
			}
			throw;
		}
	}

	internal void SetRowCache(DataRow[] newRows)
	{
		_rows = newRows;
		_lastFreeRecord = _rows.Length;
		_recordCapacity = _lastFreeRecord;
	}
}
