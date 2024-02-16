using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace System.Data;

public abstract class EnumerableRowCollection : IEnumerable
{
	internal abstract Type ElementType { get; }

	internal abstract DataTable? Table { get; }

	internal EnumerableRowCollection()
	{
	}

	IEnumerator IEnumerable.GetEnumerator()
	{
		return null;
	}
}
public class EnumerableRowCollection<TRow> : EnumerableRowCollection, IEnumerable<TRow>, IEnumerable
{
	private readonly DataTable _table;

	private readonly IEnumerable<TRow> _enumerableRows;

	private readonly List<Func<TRow, bool>> _listOfPredicates;

	private readonly SortExpressionBuilder<TRow> _sortExpression;

	private readonly Func<TRow, TRow> _selector;

	internal override Type ElementType => typeof(TRow);

	internal IEnumerable<TRow> EnumerableRows => _enumerableRows;

	internal override DataTable? Table => _table;

	internal EnumerableRowCollection(IEnumerable<TRow> enumerableRows, bool isDataViewable, DataTable table)
	{
		_enumerableRows = enumerableRows;
		if (isDataViewable)
		{
			_table = table;
		}
		_listOfPredicates = new List<Func<TRow, bool>>();
		_sortExpression = new SortExpressionBuilder<TRow>();
	}

	internal EnumerableRowCollection(DataTable table)
	{
		_table = table;
		_enumerableRows = table.Rows.Cast<TRow>();
		_listOfPredicates = new List<Func<TRow, bool>>();
		_sortExpression = new SortExpressionBuilder<TRow>();
	}

	internal EnumerableRowCollection(EnumerableRowCollection<TRow> source, IEnumerable<TRow> enumerableRows, Func<TRow, TRow> selector)
	{
		_enumerableRows = enumerableRows;
		_selector = selector;
		if (source != null)
		{
			if (source._selector == null)
			{
				_table = source._table;
			}
			_listOfPredicates = new List<Func<TRow, bool>>(source._listOfPredicates);
			_sortExpression = source._sortExpression.Clone();
		}
		else
		{
			_listOfPredicates = new List<Func<TRow, bool>>();
			_sortExpression = new SortExpressionBuilder<TRow>();
		}
	}

	IEnumerator IEnumerable.GetEnumerator()
	{
		return GetEnumerator();
	}

	public IEnumerator<TRow> GetEnumerator()
	{
		return _enumerableRows.GetEnumerator();
	}

	internal LinqDataView GetLinqDataView()
	{
		if (_table == null || !typeof(DataRow).IsAssignableFrom(typeof(TRow)))
		{
			throw DataSetUtil.NotSupported(System.SR.ToLDVUnsupported);
		}
		LinqDataView linqDataView = null;
		Func<DataRow, bool> finalPredicate = null;
		if (_selector != null && 0 < _listOfPredicates.Count)
		{
			finalPredicate = delegate(DataRow row)
			{
				if (row != (object)_selector((TRow)(object)row))
				{
					throw DataSetUtil.NotSupported(System.SR.ToLDVUnsupported);
				}
				foreach (Func<TRow, bool> listOfPredicate in _listOfPredicates)
				{
					if (!listOfPredicate((TRow)(object)row))
					{
						return false;
					}
				}
				return true;
			};
		}
		else if (_selector != null)
		{
			finalPredicate = delegate(DataRow row)
			{
				if (row != (object)_selector((TRow)(object)row))
				{
					throw DataSetUtil.NotSupported(System.SR.ToLDVUnsupported);
				}
				return true;
			};
		}
		else if (0 < _listOfPredicates.Count)
		{
			finalPredicate = delegate(DataRow row)
			{
				foreach (Func<TRow, bool> listOfPredicate2 in _listOfPredicates)
				{
					if (!listOfPredicate2((TRow)(object)row))
					{
						return false;
					}
				}
				return true;
			};
		}
		if (finalPredicate != null && 0 < _sortExpression.Count)
		{
			return new LinqDataView(_table, (DataRow row) => finalPredicate(row), (DataRow a, DataRow b) => _sortExpression.Compare(_sortExpression.Select((TRow)(object)a), _sortExpression.Select((TRow)(object)b)), (object key, DataRow row) => _sortExpression.Compare((List<object>)key, _sortExpression.Select((TRow)(object)row)), _sortExpression.CloneCast<DataRow>());
		}
		if (finalPredicate != null)
		{
			return new LinqDataView(_table, (DataRow row) => finalPredicate(row), null, null, _sortExpression.CloneCast<DataRow>());
		}
		if (0 < _sortExpression.Count)
		{
			return new LinqDataView(_table, null, (DataRow a, DataRow b) => _sortExpression.Compare(_sortExpression.Select((TRow)(object)a), _sortExpression.Select((TRow)(object)b)), (object key, DataRow row) => _sortExpression.Compare((List<object>)key, _sortExpression.Select((TRow)(object)row)), _sortExpression.CloneCast<DataRow>());
		}
		return new LinqDataView(_table, _sortExpression.CloneCast<DataRow>());
	}

	internal void AddPredicate(Func<TRow, bool> pred)
	{
		_listOfPredicates.Add(pred);
	}

	internal void AddSortExpression<TKey>(Func<TRow, TKey> keySelector, bool isDescending, bool isOrderBy)
	{
		AddSortExpression(keySelector, Comparer<TKey>.Default, isDescending, isOrderBy);
	}

	internal void AddSortExpression<TKey>(Func<TRow, TKey> keySelector, IComparer<TKey> comparer, bool isDescending, bool isOrderBy)
	{
		DataSetUtil.CheckArgumentNull(keySelector, "keySelector");
		DataSetUtil.CheckArgumentNull(comparer, "comparer");
		_sortExpression.Add((TRow input) => keySelector(input), (object val1, object val2) => ((!isDescending) ? 1 : (-1)) * comparer.Compare((TKey)val1, (TKey)val2), isOrderBy);
	}
}
