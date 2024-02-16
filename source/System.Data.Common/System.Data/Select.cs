using System.Collections.Generic;
using System.Data.Common;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;

namespace System.Data;

internal sealed class Select
{
	private sealed class ColumnInfo
	{
		public bool flag;

		public bool equalsOperator;

		public BinaryNode expr;
	}

	private readonly DataTable _table;

	private readonly IndexField[] _indexFields;

	private readonly DataViewRowState _recordStates;

	private readonly DataExpression _rowFilter;

	private readonly ExpressionNode _expression;

	private Index _index;

	private int[] _records;

	private int _recordCount;

	private ExpressionNode _linearExpression;

	private bool _candidatesForBinarySearch;

	private ColumnInfo[] _candidateColumns;

	private int _nCandidates;

	private int _matchedCandidates;

	[RequiresUnreferencedCode("Members of types used in the filter expression might be trimmed.")]
	public Select(DataTable table, string filterExpression, string sort, DataViewRowState recordStates)
	{
		_table = table;
		_indexFields = table.ParseSortString(sort);
		if (filterExpression != null && filterExpression.Length > 0)
		{
			_rowFilter = new DataExpression(_table, filterExpression);
			_expression = _rowFilter.ExpressionNode;
		}
		_recordStates = recordStates;
	}

	private bool IsSupportedOperator(int op)
	{
		if ((op < 7 || op > 11) && op != 13)
		{
			return op == 39;
		}
		return true;
	}

	private void AnalyzeExpression(BinaryNode expr)
	{
		if (_linearExpression == _expression)
		{
			return;
		}
		if (expr._op == 27)
		{
			_linearExpression = _expression;
			return;
		}
		if (expr._op == 26)
		{
			bool flag = false;
			bool flag2 = false;
			if (expr._left is BinaryNode)
			{
				AnalyzeExpression((BinaryNode)expr._left);
				if (_linearExpression == _expression)
				{
					return;
				}
				flag = true;
			}
			else
			{
				UnaryNode unaryNode = expr._left as UnaryNode;
				if (unaryNode != null)
				{
					while (unaryNode._op == 0 && unaryNode._right is UnaryNode && ((UnaryNode)unaryNode._right)._op == 0)
					{
						unaryNode = (UnaryNode)unaryNode._right;
					}
					if (unaryNode._op == 0 && unaryNode._right is BinaryNode)
					{
						AnalyzeExpression((BinaryNode)unaryNode._right);
						if (_linearExpression == _expression)
						{
							return;
						}
						flag = true;
					}
				}
			}
			if (expr._right is BinaryNode)
			{
				AnalyzeExpression((BinaryNode)expr._right);
				if (_linearExpression == _expression)
				{
					return;
				}
				flag2 = true;
			}
			else
			{
				UnaryNode unaryNode2 = expr._right as UnaryNode;
				if (unaryNode2 != null)
				{
					while (unaryNode2._op == 0 && unaryNode2._right is UnaryNode && ((UnaryNode)unaryNode2._right)._op == 0)
					{
						unaryNode2 = (UnaryNode)unaryNode2._right;
					}
					if (unaryNode2._op == 0 && unaryNode2._right is BinaryNode)
					{
						AnalyzeExpression((BinaryNode)unaryNode2._right);
						if (_linearExpression == _expression)
						{
							return;
						}
						flag2 = true;
					}
				}
			}
			if (!(flag && flag2))
			{
				ExpressionNode expressionNode = (flag ? expr._right : expr._left);
				_linearExpression = ((_linearExpression == null) ? expressionNode : new BinaryNode(_table, 26, expressionNode, _linearExpression));
			}
			return;
		}
		if (IsSupportedOperator(expr._op))
		{
			if (expr._left is NameNode && expr._right is ConstNode)
			{
				ColumnInfo columnInfo = _candidateColumns[((NameNode)expr._left)._column.Ordinal];
				columnInfo.expr = ((columnInfo.expr == null) ? expr : new BinaryNode(_table, 26, expr, columnInfo.expr));
				if (expr._op == 7)
				{
					columnInfo.equalsOperator = true;
				}
				_candidatesForBinarySearch = true;
				return;
			}
			if (expr._right is NameNode && expr._left is ConstNode)
			{
				ExpressionNode left = expr._left;
				expr._left = expr._right;
				expr._right = left;
				switch (expr._op)
				{
				case 8:
					expr._op = 9;
					break;
				case 9:
					expr._op = 8;
					break;
				case 10:
					expr._op = 11;
					break;
				case 11:
					expr._op = 10;
					break;
				}
				ColumnInfo columnInfo2 = _candidateColumns[((NameNode)expr._left)._column.Ordinal];
				columnInfo2.expr = ((columnInfo2.expr == null) ? expr : new BinaryNode(_table, 26, expr, columnInfo2.expr));
				if (expr._op == 7)
				{
					columnInfo2.equalsOperator = true;
				}
				_candidatesForBinarySearch = true;
				return;
			}
		}
		_linearExpression = ((_linearExpression == null) ? expr : new BinaryNode(_table, 26, expr, _linearExpression));
	}

	private bool CompareSortIndexDesc(IndexField[] fields)
	{
		if (fields.Length < _indexFields.Length)
		{
			return false;
		}
		int num = 0;
		for (int i = 0; i < fields.Length; i++)
		{
			if (num >= _indexFields.Length)
			{
				break;
			}
			if (fields[i] == _indexFields[num])
			{
				num++;
				continue;
			}
			ColumnInfo columnInfo = _candidateColumns[fields[i].Column.Ordinal];
			if (columnInfo == null || !columnInfo.equalsOperator)
			{
				return false;
			}
		}
		return num == _indexFields.Length;
	}

	private bool FindSortIndex()
	{
		_index = null;
		_table._indexesLock.EnterUpgradeableReadLock();
		try
		{
			int count = _table._indexes.Count;
			for (int i = 0; i < count; i++)
			{
				Index index = _table._indexes[i];
				if (index.RecordStates == _recordStates && index.IsSharable && CompareSortIndexDesc(index._indexFields))
				{
					_index = index;
					return true;
				}
			}
		}
		finally
		{
			_table._indexesLock.ExitUpgradeableReadLock();
		}
		return false;
	}

	private int CompareClosestCandidateIndexDesc(IndexField[] fields)
	{
		int num = ((fields.Length < _nCandidates) ? fields.Length : _nCandidates);
		int i;
		for (i = 0; i < num; i++)
		{
			ColumnInfo columnInfo = _candidateColumns[fields[i].Column.Ordinal];
			if (columnInfo == null || columnInfo.expr == null)
			{
				break;
			}
			if (!columnInfo.equalsOperator)
			{
				return i + 1;
			}
		}
		return i;
	}

	private bool FindClosestCandidateIndex()
	{
		_index = null;
		_matchedCandidates = 0;
		bool flag = true;
		_table._indexesLock.EnterUpgradeableReadLock();
		try
		{
			int count = _table._indexes.Count;
			for (int i = 0; i < count; i++)
			{
				Index index = _table._indexes[i];
				if (index.RecordStates != _recordStates || !index.IsSharable)
				{
					continue;
				}
				int num = CompareClosestCandidateIndexDesc(index._indexFields);
				if (num > _matchedCandidates || (num == _matchedCandidates && !flag))
				{
					_matchedCandidates = num;
					_index = index;
					flag = CompareSortIndexDesc(index._indexFields);
					if (_matchedCandidates == _nCandidates && flag)
					{
						return true;
					}
				}
			}
		}
		finally
		{
			_table._indexesLock.ExitUpgradeableReadLock();
		}
		if (_index == null)
		{
			return false;
		}
		return flag;
	}

	private void InitCandidateColumns()
	{
		_nCandidates = 0;
		_candidateColumns = new ColumnInfo[_table.Columns.Count];
		if (_rowFilter == null)
		{
			return;
		}
		DataColumn[] dependency = _rowFilter.GetDependency();
		for (int i = 0; i < dependency.Length; i++)
		{
			if (dependency[i].Table == _table)
			{
				_candidateColumns[dependency[i].Ordinal] = new ColumnInfo();
				_nCandidates++;
			}
		}
	}

	private void CreateIndex()
	{
		if (_index != null)
		{
			return;
		}
		if (_nCandidates == 0)
		{
			_index = new Index(_table, _indexFields, _recordStates, null);
			_index.AddRef();
			return;
		}
		int num = _candidateColumns.Length;
		int num2 = _indexFields.Length;
		bool flag = true;
		int i;
		for (i = 0; i < num; i++)
		{
			if (_candidateColumns[i] != null && !_candidateColumns[i].equalsOperator)
			{
				flag = false;
				break;
			}
		}
		int num3 = 0;
		for (i = 0; i < num2; i++)
		{
			ColumnInfo columnInfo = _candidateColumns[_indexFields[i].Column.Ordinal];
			if (columnInfo != null)
			{
				columnInfo.flag = true;
				num3++;
			}
		}
		int num4 = num2 - num3;
		IndexField[] array = new IndexField[_nCandidates + num4];
		if (flag)
		{
			num3 = 0;
			for (i = 0; i < num; i++)
			{
				if (_candidateColumns[i] != null)
				{
					array[num3++] = new IndexField(_table.Columns[i], isDescending: false);
					_candidateColumns[i].flag = false;
				}
			}
			for (i = 0; i < num2; i++)
			{
				ColumnInfo columnInfo2 = _candidateColumns[_indexFields[i].Column.Ordinal];
				if (columnInfo2 == null || columnInfo2.flag)
				{
					array[num3++] = _indexFields[i];
					if (columnInfo2 != null)
					{
						columnInfo2.flag = false;
					}
				}
			}
			for (i = 0; i < _candidateColumns.Length; i++)
			{
				if (_candidateColumns[i] != null)
				{
					_candidateColumns[i].flag = false;
				}
			}
			_index = new Index(_table, array, _recordStates, null);
			if (!IsOperatorIn(_expression))
			{
				_index.AddRef();
			}
			_matchedCandidates = _nCandidates;
			return;
		}
		for (i = 0; i < num2; i++)
		{
			array[i] = _indexFields[i];
			ColumnInfo columnInfo3 = _candidateColumns[_indexFields[i].Column.Ordinal];
			if (columnInfo3 != null)
			{
				columnInfo3.flag = true;
			}
		}
		num3 = i;
		for (i = 0; i < num; i++)
		{
			if (_candidateColumns[i] != null)
			{
				if (!_candidateColumns[i].flag)
				{
					array[num3++] = new IndexField(_table.Columns[i], isDescending: false);
				}
				else
				{
					_candidateColumns[i].flag = false;
				}
			}
		}
		_index = new Index(_table, array, _recordStates, null);
		_matchedCandidates = 0;
		if (_linearExpression != _expression)
		{
			IndexField[] indexFields = _index._indexFields;
			while (_matchedCandidates < num3)
			{
				ColumnInfo columnInfo4 = _candidateColumns[indexFields[_matchedCandidates].Column.Ordinal];
				if (columnInfo4 == null || columnInfo4.expr == null)
				{
					break;
				}
				_matchedCandidates++;
				if (!columnInfo4.equalsOperator)
				{
					break;
				}
			}
		}
		for (i = 0; i < _candidateColumns.Length; i++)
		{
			if (_candidateColumns[i] != null)
			{
				_candidateColumns[i].flag = false;
			}
		}
	}

	private bool IsOperatorIn(ExpressionNode enode)
	{
		if (enode is BinaryNode binaryNode && (5 == binaryNode._op || IsOperatorIn(binaryNode._right) || IsOperatorIn(binaryNode._left)))
		{
			return true;
		}
		return false;
	}

	private void BuildLinearExpression()
	{
		IndexField[] indexFields = _index._indexFields;
		int num = indexFields.Length;
		for (int i = 0; i < _matchedCandidates; i++)
		{
			ColumnInfo columnInfo = _candidateColumns[indexFields[i].Column.Ordinal];
			columnInfo.flag = true;
		}
		int num2 = _candidateColumns.Length;
		for (int i = 0; i < num2; i++)
		{
			if (_candidateColumns[i] == null)
			{
				continue;
			}
			if (!_candidateColumns[i].flag)
			{
				BinaryNode expr = _candidateColumns[i].expr;
				if (expr != null)
				{
					_linearExpression = ((_linearExpression == null) ? _candidateColumns[i].expr : new BinaryNode(_table, 26, expr, _linearExpression));
				}
			}
			else
			{
				_candidateColumns[i].flag = false;
			}
		}
	}

	public DataRow[] SelectRows()
	{
		bool flag = true;
		InitCandidateColumns();
		if (_expression is BinaryNode)
		{
			AnalyzeExpression((BinaryNode)_expression);
			if (!_candidatesForBinarySearch)
			{
				_linearExpression = _expression;
			}
			if (_linearExpression == _expression)
			{
				for (int i = 0; i < _candidateColumns.Length; i++)
				{
					if (_candidateColumns[i] != null)
					{
						_candidateColumns[i].equalsOperator = false;
						_candidateColumns[i].expr = null;
					}
				}
			}
			else
			{
				flag = !FindClosestCandidateIndex();
			}
		}
		else
		{
			_linearExpression = _expression;
		}
		if (_index == null && (_indexFields.Length != 0 || _linearExpression == _expression))
		{
			flag = !FindSortIndex();
		}
		if (_index == null)
		{
			CreateIndex();
			flag = false;
		}
		if (_index.RecordCount == 0)
		{
			return _table.NewRowArray(0);
		}
		Range range;
		if (_matchedCandidates == 0)
		{
			range = new Range(0, _index.RecordCount - 1);
			_linearExpression = _expression;
			return GetLinearFilteredRows(range);
		}
		range = GetBinaryFilteredRecords();
		if (range.Count == 0)
		{
			return _table.NewRowArray(0);
		}
		if (_matchedCandidates < _nCandidates)
		{
			BuildLinearExpression();
		}
		if (!flag)
		{
			return GetLinearFilteredRows(range);
		}
		_records = GetLinearFilteredRecords(range);
		_recordCount = _records.Length;
		if (_recordCount == 0)
		{
			return _table.NewRowArray(0);
		}
		Sort(0, _recordCount - 1);
		return GetRows();
	}

	public DataRow[] GetRows()
	{
		DataRow[] array = _table.NewRowArray(_recordCount);
		for (int i = 0; i < array.Length; i++)
		{
			array[i] = _table._recordManager[_records[i]];
		}
		return array;
	}

	[UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:RequiresUnreferencedCode", Justification = "All constructors are marked as unsafe.")]
	private bool AcceptRecord(int record)
	{
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
		object value = _linearExpression.Eval(dataRow, version);
		try
		{
			return DataExpression.ToBoolean(value);
		}
		catch (Exception e) when (ADP.IsCatchableExceptionType(e))
		{
			throw ExprException.FilterConvertion(_rowFilter.Expression);
		}
	}

	[UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:RequiresUnreferencedCode", Justification = "All constructors are marked as unsafe.")]
	private int Eval(BinaryNode expr, DataRow row, DataRowVersion version)
	{
		if (expr._op == 26)
		{
			int num = Eval((BinaryNode)expr._left, row, version);
			if (num != 0)
			{
				return num;
			}
			int num2 = Eval((BinaryNode)expr._right, row, version);
			if (num2 != 0)
			{
				return num2;
			}
			return 0;
		}
		long num3 = 0L;
		object obj = expr._left.Eval(row, version);
		if (expr._op != 13 && expr._op != 39)
		{
			object obj2 = expr._right.Eval(row, version);
			bool flag = expr._left is ConstNode;
			bool flag2 = expr._right is ConstNode;
			if (obj == DBNull.Value || (expr._left.IsSqlColumn && DataStorage.IsObjectSqlNull(obj)))
			{
				return -1;
			}
			if (obj2 == DBNull.Value || (expr._right.IsSqlColumn && DataStorage.IsObjectSqlNull(obj2)))
			{
				return 1;
			}
			StorageType storageType = DataStorage.GetStorageType(obj.GetType());
			if (StorageType.Char == storageType)
			{
				obj2 = ((!flag2 && expr._right.IsSqlColumn) ? SqlConvert.ChangeType2(obj2, StorageType.Char, typeof(char), _table.FormatProvider) : ((object)Convert.ToChar(obj2, _table.FormatProvider)));
			}
			StorageType storageType2 = DataStorage.GetStorageType(obj2.GetType());
			StorageType storageType3 = ((!expr._left.IsSqlColumn && !expr._right.IsSqlColumn) ? expr.ResultType(storageType, storageType2, flag, flag2, expr._op) : expr.ResultSqlType(storageType, storageType2, flag, flag2, expr._op));
			if (storageType3 == StorageType.Empty)
			{
				expr.SetTypeMismatchError(expr._op, obj.GetType(), obj2.GetType());
			}
			NameNode nameNode = null;
			CompareInfo comparer = (((flag && !flag2 && storageType == StorageType.String && storageType2 == StorageType.Guid && expr._right is NameNode nameNode2 && nameNode2._column.DataType == typeof(Guid)) || (flag2 && !flag && storageType2 == StorageType.String && storageType == StorageType.Guid && expr._left is NameNode nameNode3 && nameNode3._column.DataType == typeof(Guid))) ? CultureInfo.InvariantCulture.CompareInfo : null);
			num3 = expr.BinaryCompare(obj, obj2, storageType3, expr._op, comparer);
		}
		switch (expr._op)
		{
		case 7:
			num3 = ((num3 != 0L) ? ((num3 >= 0) ? 1 : (-1)) : 0);
			break;
		case 8:
			num3 = ((num3 <= 0) ? (-1) : 0);
			break;
		case 9:
			num3 = ((num3 >= 0) ? 1 : 0);
			break;
		case 10:
			num3 = ((num3 < 0) ? (-1) : 0);
			break;
		case 11:
			num3 = ((num3 > 0) ? 1 : 0);
			break;
		case 13:
			num3 = ((obj != DBNull.Value) ? (-1) : 0);
			break;
		case 39:
			num3 = ((obj == DBNull.Value) ? 1 : 0);
			break;
		}
		return (int)num3;
	}

	private int Evaluate(int record)
	{
		DataRow dataRow = _table._recordManager[record];
		if (dataRow == null)
		{
			return 0;
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
		IndexField[] indexFields = _index._indexFields;
		for (int i = 0; i < _matchedCandidates; i++)
		{
			ColumnInfo columnInfo = _candidateColumns[indexFields[i].Column.Ordinal];
			int num = Eval(columnInfo.expr, dataRow, version);
			if (num != 0)
			{
				if (!indexFields[i].IsDescending)
				{
					return num;
				}
				return -num;
			}
		}
		return 0;
	}

	private int FindFirstMatchingRecord()
	{
		int result = -1;
		int num = 0;
		int num2 = _index.RecordCount - 1;
		while (num <= num2)
		{
			int num3 = num + num2 >> 1;
			int record = _index.GetRecord(num3);
			int num4 = Evaluate(record);
			if (num4 == 0)
			{
				result = num3;
			}
			if (num4 < 0)
			{
				num = num3 + 1;
			}
			else
			{
				num2 = num3 - 1;
			}
		}
		return result;
	}

	private int FindLastMatchingRecord(int lo)
	{
		int result = -1;
		int num = _index.RecordCount - 1;
		while (lo <= num)
		{
			int num2 = lo + num >> 1;
			int record = _index.GetRecord(num2);
			int num3 = Evaluate(record);
			if (num3 == 0)
			{
				result = num2;
			}
			if (num3 <= 0)
			{
				lo = num2 + 1;
			}
			else
			{
				num = num2 - 1;
			}
		}
		return result;
	}

	private Range GetBinaryFilteredRecords()
	{
		if (_matchedCandidates == 0)
		{
			return new Range(0, _index.RecordCount - 1);
		}
		int num = FindFirstMatchingRecord();
		if (num == -1)
		{
			return default(Range);
		}
		int max = FindLastMatchingRecord(num);
		return new Range(num, max);
	}

	private int[] GetLinearFilteredRecords(Range range)
	{
		if (_linearExpression == null)
		{
			int[] array = new int[range.Count];
			RBTree<int>.RBTreeEnumerator enumerator = _index.GetEnumerator(range.Min);
			for (int i = 0; i < range.Count; i++)
			{
				if (!enumerator.MoveNext())
				{
					break;
				}
				array[i] = enumerator.Current;
			}
			return array;
		}
		List<int> list = new List<int>();
		RBTree<int>.RBTreeEnumerator enumerator2 = _index.GetEnumerator(range.Min);
		for (int j = 0; j < range.Count; j++)
		{
			if (!enumerator2.MoveNext())
			{
				break;
			}
			if (AcceptRecord(enumerator2.Current))
			{
				list.Add(enumerator2.Current);
			}
		}
		return list.ToArray();
	}

	private DataRow[] GetLinearFilteredRows(Range range)
	{
		if (_linearExpression == null)
		{
			return _index.GetRows(range);
		}
		List<DataRow> list = new List<DataRow>();
		RBTree<int>.RBTreeEnumerator enumerator = _index.GetEnumerator(range.Min);
		for (int i = 0; i < range.Count; i++)
		{
			if (!enumerator.MoveNext())
			{
				break;
			}
			if (AcceptRecord(enumerator.Current))
			{
				list.Add(_table._recordManager[enumerator.Current]);
			}
		}
		DataRow[] array = _table.NewRowArray(list.Count);
		list.CopyTo(array);
		return array;
	}

	private int CompareRecords(int record1, int record2)
	{
		int num = _indexFields.Length;
		for (int i = 0; i < num; i++)
		{
			int num2 = _indexFields[i].Column.Compare(record1, record2);
			if (num2 != 0)
			{
				if (_indexFields[i].IsDescending)
				{
					num2 = -num2;
				}
				return num2;
			}
		}
		DataRow dataRow = _table._recordManager[record1];
		DataRow dataRow2 = _table._recordManager[record2];
		DataRow dataRow3 = dataRow;
		DataRow dataRow4 = dataRow2;
		long num3 = dataRow3?.rowID ?? 0;
		long num4 = dataRow4?.rowID ?? 0;
		int num5 = ((num3 < num4) ? (-1) : ((num4 < num3) ? 1 : 0));
		if (num5 == 0 && record1 != record2 && dataRow3 != null && dataRow4 != null)
		{
			num3 = (long)dataRow3.GetRecordState(record1);
			num4 = (long)dataRow4.GetRecordState(record2);
			num5 = ((num3 < num4) ? (-1) : ((num4 < num3) ? 1 : 0));
		}
		return num5;
	}

	private void Sort(int left, int right)
	{
		int num;
		do
		{
			num = left;
			int num2 = right;
			int record = _records[num + num2 >> 1];
			while (true)
			{
				if (CompareRecords(_records[num], record) < 0)
				{
					num++;
					continue;
				}
				while (CompareRecords(_records[num2], record) > 0)
				{
					num2--;
				}
				if (num <= num2)
				{
					int num3 = _records[num];
					_records[num] = _records[num2];
					_records[num2] = num3;
					num++;
					num2--;
				}
				if (num > num2)
				{
					break;
				}
			}
			if (left < num2)
			{
				Sort(left, num2);
			}
			left = num;
		}
		while (num < right);
	}
}
