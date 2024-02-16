using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;

namespace System.Data;

[TypeConverter(typeof(ExpandableObjectConverter))]
public class DataViewSetting
{
	private DataViewManager _dataViewManager;

	private DataTable _table;

	private string _sort = string.Empty;

	private string _rowFilter = string.Empty;

	private DataViewRowState _rowStateFilter = DataViewRowState.CurrentRows;

	private bool _applyDefaultSort;

	public bool ApplyDefaultSort
	{
		get
		{
			return _applyDefaultSort;
		}
		set
		{
			if (_applyDefaultSort != value)
			{
				_applyDefaultSort = value;
			}
		}
	}

	[Browsable(false)]
	public DataViewManager? DataViewManager => _dataViewManager;

	[Browsable(false)]
	public DataTable? Table => _table;

	public string RowFilter
	{
		get
		{
			return _rowFilter;
		}
		[RequiresUnreferencedCode("Members of types used in the filter expression might be trimmed.")]
		[param: AllowNull]
		set
		{
			if (value == null)
			{
				value = string.Empty;
			}
			if (_rowFilter != value)
			{
				_rowFilter = value;
			}
		}
	}

	public DataViewRowState RowStateFilter
	{
		get
		{
			return _rowStateFilter;
		}
		set
		{
			if (_rowStateFilter != value)
			{
				_rowStateFilter = value;
			}
		}
	}

	public string Sort
	{
		get
		{
			return _sort;
		}
		[param: AllowNull]
		set
		{
			if (value == null)
			{
				value = string.Empty;
			}
			if (_sort != value)
			{
				_sort = value;
			}
		}
	}

	internal DataViewSetting()
	{
	}

	internal void SetDataViewManager(DataViewManager dataViewManager)
	{
		if (_dataViewManager != dataViewManager)
		{
			_dataViewManager = dataViewManager;
		}
	}

	internal void SetDataTable(DataTable table)
	{
		if (_table != table)
		{
			_table = table;
		}
	}
}
