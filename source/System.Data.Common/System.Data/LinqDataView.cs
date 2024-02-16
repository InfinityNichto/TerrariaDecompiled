using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;

namespace System.Data;

internal sealed class LinqDataView : DataView, IBindingList, IList, ICollection, IEnumerable, IBindingListView
{
	internal Func<object, DataRow, int> comparerKeyRow;

	internal readonly SortExpressionBuilder<DataRow> sortExpressionBuilder;

	public override string RowFilter
	{
		get
		{
			if (base.RowPredicate == null)
			{
				return base.RowFilter;
			}
			return null;
		}
		[RequiresUnreferencedCode("Members of types used in the filter expression might be trimmed.")]
		set
		{
			if (value == null)
			{
				base.RowPredicate = null;
				base.RowFilter = string.Empty;
			}
			else
			{
				base.RowFilter = value;
				base.RowPredicate = null;
			}
		}
	}

	PropertyDescriptor IBindingList.SortProperty
	{
		get
		{
			if (base.SortComparison != null)
			{
				return null;
			}
			return GetSortProperty();
		}
	}

	ListSortDescriptionCollection IBindingListView.SortDescriptions
	{
		get
		{
			if (base.SortComparison == null)
			{
				return GetSortDescriptions();
			}
			return new ListSortDescriptionCollection();
		}
	}

	bool IBindingList.IsSorted
	{
		get
		{
			if (base.SortComparison == null)
			{
				return base.Sort.Length != 0;
			}
			return true;
		}
	}

	internal LinqDataView(DataTable table, SortExpressionBuilder<DataRow> sortExpressionBuilder)
		: base(table)
	{
		this.sortExpressionBuilder = sortExpressionBuilder ?? new SortExpressionBuilder<DataRow>();
	}

	internal LinqDataView(DataTable table, Predicate<DataRow> predicate_system, Comparison<DataRow> comparison, Func<object, DataRow, int> comparerKeyRow, SortExpressionBuilder<DataRow> sortExpressionBuilder)
		: base(table, predicate_system, comparison, DataViewRowState.CurrentRows)
	{
		this.sortExpressionBuilder = ((sortExpressionBuilder == null) ? this.sortExpressionBuilder : sortExpressionBuilder);
		this.comparerKeyRow = comparerKeyRow;
	}

	internal override int FindByKey(object key)
	{
		if (!string.IsNullOrEmpty(base.Sort))
		{
			return base.FindByKey(key);
		}
		if (base.SortComparison == null)
		{
			throw ExceptionBuilder.IndexKeyLength(0, 0);
		}
		if (sortExpressionBuilder.Count != 1)
		{
			throw DataSetUtil.InvalidOperation(System.SR.Format(System.SR.LDV_InvalidNumOfKeys, sortExpressionBuilder.Count));
		}
		Index.ComparisonBySelector<object, DataRow> comparison = comparerKeyRow.Invoke;
		List<object> list = new List<object>();
		list.Add(key);
		Range range = FindRecords(comparison, list);
		if (range.Count != 0)
		{
			return range.Min;
		}
		return -1;
	}

	internal override int FindByKey(object[] key)
	{
		if (base.SortComparison == null && string.IsNullOrEmpty(base.Sort))
		{
			throw ExceptionBuilder.IndexKeyLength(0, 0);
		}
		if (base.SortComparison != null && key.Length != sortExpressionBuilder.Count)
		{
			throw DataSetUtil.InvalidOperation(System.SR.Format(System.SR.LDV_InvalidNumOfKeys, sortExpressionBuilder.Count));
		}
		if (base.SortComparison == null)
		{
			return base.FindByKey(key);
		}
		Index.ComparisonBySelector<object, DataRow> comparison = comparerKeyRow.Invoke;
		List<object> list = new List<object>();
		foreach (object item in key)
		{
			list.Add(item);
		}
		Range range = FindRecords(comparison, list);
		if (range.Count != 0)
		{
			return range.Min;
		}
		return -1;
	}

	internal override DataRowView[] FindRowsByKey(object[] key)
	{
		if (base.SortComparison == null && string.IsNullOrEmpty(base.Sort))
		{
			throw ExceptionBuilder.IndexKeyLength(0, 0);
		}
		if (base.SortComparison != null && key.Length != sortExpressionBuilder.Count)
		{
			throw DataSetUtil.InvalidOperation(System.SR.Format(System.SR.LDV_InvalidNumOfKeys, sortExpressionBuilder.Count));
		}
		if (base.SortComparison == null)
		{
			return base.FindRowsByKey(key);
		}
		Range range = FindRecords<object, DataRow>(comparerKeyRow.Invoke, new List<object>(key));
		return GetDataRowViewFromRange(range);
	}

	internal override void SetIndex(string newSort, DataViewRowState newRowStates, IFilter newRowFilter)
	{
		if ((base.SortComparison != null || base.RowPredicate != null) && newRowStates != DataViewRowState.CurrentRows)
		{
			throw DataSetUtil.Argument(System.SR.LDVRowStateError);
		}
		base.SetIndex(newSort, newRowStates, newRowFilter);
	}

	void IBindingList.RemoveSort()
	{
		base.Sort = string.Empty;
		base.SortComparison = null;
	}
}
