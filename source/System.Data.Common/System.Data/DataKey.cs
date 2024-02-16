namespace System.Data;

internal readonly struct DataKey
{
	private readonly DataColumn[] _columns;

	internal DataColumn[] ColumnsReference => _columns;

	internal bool HasValue => _columns != null;

	internal DataTable Table => _columns[0].Table;

	internal DataKey(DataColumn[] columns, bool copyColumns)
	{
		if (columns == null)
		{
			throw ExceptionBuilder.ArgumentNull("columns");
		}
		if (columns.Length == 0)
		{
			throw ExceptionBuilder.KeyNoColumns();
		}
		if (columns.Length > 32)
		{
			throw ExceptionBuilder.KeyTooManyColumns(32);
		}
		for (int i = 0; i < columns.Length; i++)
		{
			if (columns[i] == null)
			{
				throw ExceptionBuilder.ArgumentNull("column");
			}
		}
		for (int j = 0; j < columns.Length; j++)
		{
			for (int k = 0; k < j; k++)
			{
				if (columns[j] == columns[k])
				{
					throw ExceptionBuilder.KeyDuplicateColumns(columns[j].ColumnName);
				}
			}
		}
		if (copyColumns)
		{
			_columns = new DataColumn[columns.Length];
			for (int l = 0; l < columns.Length; l++)
			{
				_columns[l] = columns[l];
			}
		}
		else
		{
			_columns = columns;
		}
		CheckState();
	}

	internal void CheckState()
	{
		DataTable table = _columns[0].Table;
		if (table == null)
		{
			throw ExceptionBuilder.ColumnNotInAnyTable();
		}
		for (int i = 1; i < _columns.Length; i++)
		{
			if (_columns[i].Table == null)
			{
				throw ExceptionBuilder.ColumnNotInAnyTable();
			}
			if (_columns[i].Table != table)
			{
				throw ExceptionBuilder.KeyTableMismatch();
			}
		}
	}

	internal bool ColumnsEqual(DataKey key)
	{
		return ColumnsEqual(_columns, key._columns);
	}

	internal static bool ColumnsEqual(DataColumn[] column1, DataColumn[] column2)
	{
		if (column1 == column2)
		{
			return true;
		}
		if (column1 == null || column2 == null)
		{
			return false;
		}
		if (column1.Length != column2.Length)
		{
			return false;
		}
		for (int i = 0; i < column1.Length; i++)
		{
			bool flag = false;
			for (int j = 0; j < column2.Length; j++)
			{
				if (column1[i].Equals(column2[j]))
				{
					flag = true;
					break;
				}
			}
			if (!flag)
			{
				return false;
			}
		}
		return true;
	}

	internal bool ContainsColumn(DataColumn column)
	{
		for (int i = 0; i < _columns.Length; i++)
		{
			if (column == _columns[i])
			{
				return true;
			}
		}
		return false;
	}

	public override int GetHashCode()
	{
		return base.GetHashCode();
	}

	public override bool Equals(object value)
	{
		return Equals((DataKey)value);
	}

	internal bool Equals(DataKey value)
	{
		DataColumn[] columns = _columns;
		DataColumn[] columns2 = value._columns;
		if (columns == columns2)
		{
			return true;
		}
		if (columns == null || columns2 == null)
		{
			return false;
		}
		return columns.AsSpan().SequenceEqual(columns2, null);
	}

	internal string[] GetColumnNames()
	{
		string[] array = new string[_columns.Length];
		for (int i = 0; i < _columns.Length; i++)
		{
			array[i] = _columns[i].ColumnName;
		}
		return array;
	}

	internal IndexField[] GetIndexDesc()
	{
		IndexField[] array = new IndexField[_columns.Length];
		for (int i = 0; i < _columns.Length; i++)
		{
			array[i] = new IndexField(_columns[i], isDescending: false);
		}
		return array;
	}

	internal object[] GetKeyValues(int record)
	{
		object[] array = new object[_columns.Length];
		for (int i = 0; i < _columns.Length; i++)
		{
			array[i] = _columns[i][record];
		}
		return array;
	}

	internal Index GetSortIndex()
	{
		return GetSortIndex(DataViewRowState.CurrentRows);
	}

	internal Index GetSortIndex(DataViewRowState recordStates)
	{
		IndexField[] indexDesc = GetIndexDesc();
		return _columns[0].Table.GetIndex(indexDesc, recordStates, null);
	}

	internal bool RecordsEqual(int record1, int record2)
	{
		for (int i = 0; i < _columns.Length; i++)
		{
			if (_columns[i].Compare(record1, record2) != 0)
			{
				return false;
			}
		}
		return true;
	}

	internal DataColumn[] ToArray()
	{
		DataColumn[] array = new DataColumn[_columns.Length];
		for (int i = 0; i < _columns.Length; i++)
		{
			array[i] = _columns[i];
		}
		return array;
	}
}
