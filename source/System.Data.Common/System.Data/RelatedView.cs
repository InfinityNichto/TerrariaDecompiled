using System.Diagnostics.CodeAnalysis;

namespace System.Data;

internal sealed class RelatedView : DataView, IFilter
{
	private readonly DataKey? _parentKey;

	private readonly DataKey _childKey;

	private readonly DataRowView _parentRowView;

	private readonly object[] _filterValues;

	public RelatedView(DataColumn[] columns, object[] values)
		: base(columns[0].Table, locked: false)
	{
		if (values == null)
		{
			throw ExceptionBuilder.ArgumentNull("values");
		}
		_parentRowView = null;
		_parentKey = null;
		_childKey = new DataKey(columns, copyColumns: true);
		_filterValues = values;
		ResetRowViewCache();
	}

	public RelatedView(DataRowView parentRowView, DataKey parentKey, DataColumn[] childKeyColumns)
		: base(childKeyColumns[0].Table, locked: false)
	{
		_filterValues = null;
		_parentRowView = parentRowView;
		_parentKey = parentKey;
		_childKey = new DataKey(childKeyColumns, copyColumns: true);
		ResetRowViewCache();
	}

	private object[] GetParentValues()
	{
		if (_filterValues != null)
		{
			return _filterValues;
		}
		if (!_parentRowView.HasRecord())
		{
			return null;
		}
		return _parentKey.Value.GetKeyValues(_parentRowView.GetRecord());
	}

	public bool Invoke(DataRow row, DataRowVersion version)
	{
		object[] parentValues = GetParentValues();
		if (parentValues == null)
		{
			return false;
		}
		object[] keyValues = row.GetKeyValues(_childKey, version);
		bool flag = keyValues.AsSpan().SequenceEqual(parentValues, null);
		IFilter filter = base.GetFilter();
		if (filter != null)
		{
			flag &= filter.Invoke(row, version);
		}
		return flag;
	}

	internal override IFilter GetFilter()
	{
		return this;
	}

	public override DataRowView AddNew()
	{
		DataRowView dataRowView = base.AddNew();
		dataRowView.Row.SetKeyValues(_childKey, GetParentValues());
		return dataRowView;
	}

	internal override void SetIndex(string newSort, DataViewRowState newRowStates, IFilter newRowFilter)
	{
		SetIndex2(newSort, newRowStates, newRowFilter, fireEvent: false);
		Reset();
	}

	public override bool Equals([NotNullWhen(true)] DataView dv)
	{
		if (!(dv is RelatedView relatedView))
		{
			return false;
		}
		if (!base.Equals(dv))
		{
			return false;
		}
		object[] columnsReference;
		if (_filterValues != null)
		{
			columnsReference = _childKey.ColumnsReference;
			object[] value = columnsReference;
			columnsReference = relatedView._childKey.ColumnsReference;
			if (CompareArray(value, columnsReference))
			{
				return CompareArray(_filterValues, relatedView._filterValues);
			}
			return false;
		}
		if (relatedView._filterValues != null)
		{
			return false;
		}
		columnsReference = _childKey.ColumnsReference;
		object[] value2 = columnsReference;
		columnsReference = relatedView._childKey.ColumnsReference;
		if (CompareArray(value2, columnsReference))
		{
			columnsReference = _parentKey.Value.ColumnsReference;
			object[] value3 = columnsReference;
			columnsReference = _parentKey.Value.ColumnsReference;
			if (CompareArray(value3, columnsReference))
			{
				return _parentRowView.Equals(relatedView._parentRowView);
			}
		}
		return false;
	}

	private bool CompareArray(object[] value1, object[] value2)
	{
		if (value1 == null || value2 == null)
		{
			return value1 == value2;
		}
		if (value1.Length != value2.Length)
		{
			return false;
		}
		for (int i = 0; i < value1.Length; i++)
		{
			if (value1[i] != value2[i])
			{
				return false;
			}
		}
		return true;
	}
}
