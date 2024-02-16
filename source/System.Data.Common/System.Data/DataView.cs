using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data.Common;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text;
using System.Threading;

namespace System.Data;

[Designer("Microsoft.VSDesigner.Data.VS.DataViewDesigner, Microsoft.VSDesigner, Version=10.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
[DefaultProperty("Table")]
[DefaultEvent("PositionChanged")]
[Editor("Microsoft.VSDesigner.Data.Design.DataSourceEditor, Microsoft.VSDesigner, Version=10.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a", "System.Drawing.Design.UITypeEditor, System.Drawing, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
public class DataView : MarshalByValueComponent, IBindingListView, IBindingList, IList, ICollection, IEnumerable, ITypedList, ISupportInitializeNotification, ISupportInitialize
{
	private sealed class DataRowReferenceComparer : IEqualityComparer<DataRow>
	{
		internal static readonly DataRowReferenceComparer s_default = new DataRowReferenceComparer();

		private DataRowReferenceComparer()
		{
		}

		public bool Equals(DataRow x, DataRow y)
		{
			return x == y;
		}

		public int GetHashCode(DataRow obj)
		{
			return obj._objectID;
		}
	}

	private sealed class RowPredicateFilter : IFilter
	{
		internal readonly Predicate<DataRow> _predicateFilter;

		internal RowPredicateFilter(Predicate<DataRow> predicate)
		{
			_predicateFilter = predicate;
		}

		bool IFilter.Invoke(DataRow row, DataRowVersion version)
		{
			return _predicateFilter(row);
		}
	}

	private DataViewManager _dataViewManager;

	private DataTable _table;

	private bool _locked;

	private Index _index;

	private Dictionary<string, Index> _findIndexes;

	private string _sort = string.Empty;

	private Comparison<DataRow> _comparison;

	private IFilter _rowFilter;

	private DataViewRowState _recordStates = DataViewRowState.CurrentRows;

	private bool _shouldOpen = true;

	private bool _open;

	private bool _allowNew = true;

	private bool _allowEdit = true;

	private bool _allowDelete = true;

	private bool _applyDefaultSort;

	internal DataRow _addNewRow;

	private ListChangedEventArgs _addNewMoved;

	private ListChangedEventHandler _onListChanged;

	internal static ListChangedEventArgs s_resetEventArgs = new ListChangedEventArgs(ListChangedType.Reset, -1);

	private DataTable _delayedTable;

	private string _delayedRowFilter;

	private string _delayedSort;

	private DataViewRowState _delayedRecordStates = (DataViewRowState)(-1);

	private bool _fInitInProgress;

	private bool _fEndInitInProgress;

	private Dictionary<DataRow, DataRowView> _rowViewCache = new Dictionary<DataRow, DataRowView>(DataRowReferenceComparer.s_default);

	private readonly Dictionary<DataRow, DataRowView> _rowViewBuffer = new Dictionary<DataRow, DataRowView>(DataRowReferenceComparer.s_default);

	private readonly DataViewListener _dvListener;

	private static int s_objectTypeCount;

	private readonly int _objectID = Interlocked.Increment(ref s_objectTypeCount);

	[DefaultValue(true)]
	public bool AllowDelete
	{
		get
		{
			return _allowDelete;
		}
		set
		{
			if (_allowDelete != value)
			{
				_allowDelete = value;
				OnListChanged(s_resetEventArgs);
			}
		}
	}

	[RefreshProperties(RefreshProperties.All)]
	[DefaultValue(false)]
	public bool ApplyDefaultSort
	{
		get
		{
			return _applyDefaultSort;
		}
		set
		{
			DataCommonEventSource.Log.Trace("<ds.DataView.set_ApplyDefaultSort|API> {0}, {1}", ObjectID, value);
			if (_applyDefaultSort != value)
			{
				_comparison = null;
				_applyDefaultSort = value;
				UpdateIndex(force: true);
				OnListChanged(s_resetEventArgs);
			}
		}
	}

	[DefaultValue(true)]
	public bool AllowEdit
	{
		get
		{
			return _allowEdit;
		}
		set
		{
			if (_allowEdit != value)
			{
				_allowEdit = value;
				OnListChanged(s_resetEventArgs);
			}
		}
	}

	[DefaultValue(true)]
	public bool AllowNew
	{
		get
		{
			return _allowNew;
		}
		set
		{
			if (_allowNew != value)
			{
				_allowNew = value;
				OnListChanged(s_resetEventArgs);
			}
		}
	}

	[Browsable(false)]
	public int Count => _rowViewCache.Count;

	private int CountFromIndex => ((_index != null) ? _index.RecordCount : 0) + ((_addNewRow != null) ? 1 : 0);

	[Browsable(false)]
	public DataViewManager? DataViewManager => _dataViewManager;

	[Browsable(false)]
	public bool IsInitialized => !_fInitInProgress;

	[Browsable(false)]
	protected bool IsOpen => _open;

	bool ICollection.IsSynchronized => false;

	[DefaultValue("")]
	public virtual string? RowFilter
	{
		get
		{
			if (_rowFilter is DataExpression dataExpression)
			{
				return dataExpression.Expression;
			}
			return "";
		}
		[RequiresUnreferencedCode("Members of types used in the filter expression might be trimmed.")]
		set
		{
			if (value == null)
			{
				value = string.Empty;
			}
			DataCommonEventSource.Log.Trace("<ds.DataView.set_RowFilter|API> {0}, '{1}'", ObjectID, value);
			if (_fInitInProgress)
			{
				_delayedRowFilter = value;
				return;
			}
			CultureInfo culture = ((_table != null) ? _table.Locale : CultureInfo.CurrentCulture);
			if (_rowFilter == null || string.Compare(RowFilter, value, ignoreCase: false, culture) != 0)
			{
				DataExpression newRowFilter = new DataExpression(_table, value);
				SetIndex(_sort, _recordStates, newRowFilter);
			}
		}
	}

	internal Predicate<DataRow>? RowPredicate
	{
		get
		{
			if (!(GetFilter() is RowPredicateFilter rowPredicateFilter))
			{
				return null;
			}
			return rowPredicateFilter._predicateFilter;
		}
		set
		{
			if ((object)RowPredicate != value)
			{
				SetIndex(Sort, RowStateFilter, (value != null) ? new RowPredicateFilter(value) : null);
			}
		}
	}

	[DefaultValue(DataViewRowState.CurrentRows)]
	public DataViewRowState RowStateFilter
	{
		get
		{
			return _recordStates;
		}
		set
		{
			DataCommonEventSource.Log.Trace("<ds.DataView.set_RowStateFilter|API> {0}, {1}", ObjectID, value);
			if (_fInitInProgress)
			{
				_delayedRecordStates = value;
				return;
			}
			if (((uint)value & 0xFFFFFFC1u) != 0)
			{
				throw ExceptionBuilder.RecordStateRange();
			}
			if ((value & DataViewRowState.ModifiedOriginal) != 0 && (value & DataViewRowState.ModifiedCurrent) != 0)
			{
				throw ExceptionBuilder.SetRowStateFilter();
			}
			if (_recordStates != value)
			{
				SetIndex(_sort, value, _rowFilter);
			}
		}
	}

	[DefaultValue("")]
	public string Sort
	{
		get
		{
			if (_sort.Length == 0 && _applyDefaultSort && _table != null && _table._primaryIndex.Length != 0)
			{
				return _table.FormatSortString(_table._primaryIndex);
			}
			return _sort;
		}
		[param: AllowNull]
		set
		{
			if (value == null)
			{
				value = string.Empty;
			}
			DataCommonEventSource.Log.Trace("<ds.DataView.set_Sort|API> {0}, '{1}'", ObjectID, value);
			if (_fInitInProgress)
			{
				_delayedSort = value;
				return;
			}
			CultureInfo culture = ((_table != null) ? _table.Locale : CultureInfo.CurrentCulture);
			if (string.Compare(_sort, value, ignoreCase: false, culture) != 0 || _comparison != null)
			{
				CheckSort(value);
				_comparison = null;
				SetIndex(value, _recordStates, _rowFilter);
			}
		}
	}

	internal Comparison<DataRow>? SortComparison
	{
		get
		{
			return _comparison;
		}
		set
		{
			DataCommonEventSource.Log.Trace("<ds.DataView.set_SortComparison|API> {0}", ObjectID);
			if ((object)_comparison != value)
			{
				_comparison = value;
				SetIndex("", _recordStates, _rowFilter);
			}
		}
	}

	object ICollection.SyncRoot => this;

	[TypeConverter(typeof(DataTableTypeConverter))]
	[DefaultValue(null)]
	[RefreshProperties(RefreshProperties.All)]
	public DataTable? Table
	{
		get
		{
			return _table;
		}
		set
		{
			DataCommonEventSource.Log.Trace("<ds.DataView.set_Table|API> {0}, {1}", ObjectID, value?.ObjectID ?? 0);
			if (_fInitInProgress && value != null)
			{
				_delayedTable = value;
				return;
			}
			if (_locked)
			{
				throw ExceptionBuilder.SetTable();
			}
			if (_dataViewManager != null)
			{
				throw ExceptionBuilder.CanNotSetTable();
			}
			if (value != null && value.TableName.Length == 0)
			{
				throw ExceptionBuilder.CanNotBindTable();
			}
			if (_table != value)
			{
				_dvListener.UnregisterMetaDataEvents();
				_table = value;
				if (_table != null)
				{
					_dvListener.RegisterMetaDataEvents(_table);
				}
				SetIndex2("", DataViewRowState.CurrentRows, null, fireEvent: false);
				if (_table != null)
				{
					OnListChanged(new ListChangedEventArgs(ListChangedType.PropertyDescriptorChanged, new DataTablePropertyDescriptor(_table)));
				}
				OnListChanged(s_resetEventArgs);
			}
		}
	}

	object? IList.this[int recordIndex]
	{
		get
		{
			return this[recordIndex];
		}
		set
		{
			throw ExceptionBuilder.SetIListObject();
		}
	}

	public DataRowView this[int recordIndex] => GetRowView(GetRow(recordIndex));

	bool IList.IsReadOnly => false;

	bool IList.IsFixedSize => false;

	bool IBindingList.AllowNew => AllowNew;

	bool IBindingList.AllowEdit => AllowEdit;

	bool IBindingList.AllowRemove => AllowDelete;

	bool IBindingList.SupportsChangeNotification => true;

	bool IBindingList.SupportsSearching => true;

	bool IBindingList.SupportsSorting => true;

	bool IBindingList.IsSorted => Sort.Length != 0;

	PropertyDescriptor? IBindingList.SortProperty => GetSortProperty();

	ListSortDirection IBindingList.SortDirection
	{
		get
		{
			if (_index._indexFields.Length != 1 || !_index._indexFields[0].IsDescending)
			{
				return ListSortDirection.Ascending;
			}
			return ListSortDirection.Descending;
		}
	}

	string? IBindingListView.Filter
	{
		get
		{
			return RowFilter;
		}
		[RequiresUnreferencedCode("Members of types used in the filter expression might be trimmed.")]
		set
		{
			RowFilter = value;
		}
	}

	ListSortDescriptionCollection IBindingListView.SortDescriptions => GetSortDescriptions();

	bool IBindingListView.SupportsAdvancedSorting => true;

	bool IBindingListView.SupportsFiltering => true;

	internal int ObjectID => _objectID;

	public event ListChangedEventHandler? ListChanged
	{
		add
		{
			DataCommonEventSource.Log.Trace("<ds.DataView.add_ListChanged|API> {0}", ObjectID);
			_onListChanged = (ListChangedEventHandler)Delegate.Combine(_onListChanged, value);
		}
		remove
		{
			DataCommonEventSource.Log.Trace("<ds.DataView.remove_ListChanged|API> {0}", ObjectID);
			_onListChanged = (ListChangedEventHandler)Delegate.Remove(_onListChanged, value);
		}
	}

	public event EventHandler? Initialized;

	internal DataView(DataTable table, bool locked)
	{
		GC.SuppressFinalize(this);
		DataCommonEventSource.Log.Trace("<ds.DataView.DataView|INFO> {0}, table={1}, locked={2}", ObjectID, table?.ObjectID ?? 0, locked);
		_dvListener = new DataViewListener(this);
		_locked = locked;
		_table = table;
		_dvListener.RegisterMetaDataEvents(_table);
	}

	public DataView()
		: this(null)
	{
		SetIndex2("", DataViewRowState.CurrentRows, null, fireEvent: true);
	}

	public DataView(DataTable? table)
		: this(table, locked: false)
	{
		SetIndex2("", DataViewRowState.CurrentRows, null, fireEvent: true);
	}

	[RequiresUnreferencedCode("Members of types used in the filter expression might be trimmed.")]
	public DataView(DataTable table, string? RowFilter, string? Sort, DataViewRowState RowState)
	{
		GC.SuppressFinalize(this);
		DataCommonEventSource.Log.Trace("<ds.DataView.DataView|API> {0}, table={1}, RowFilter='{2}', Sort='{3}', RowState={4}", ObjectID, table?.ObjectID ?? 0, RowFilter, Sort, RowState);
		if (table == null)
		{
			throw ExceptionBuilder.CanNotUse();
		}
		_dvListener = new DataViewListener(this);
		_locked = false;
		_table = table;
		_dvListener.RegisterMetaDataEvents(_table);
		if (((uint)RowState & 0xFFFFFFC1u) != 0)
		{
			throw ExceptionBuilder.RecordStateRange();
		}
		if ((RowState & DataViewRowState.ModifiedOriginal) != 0 && (RowState & DataViewRowState.ModifiedCurrent) != 0)
		{
			throw ExceptionBuilder.SetRowStateFilter();
		}
		if (Sort == null)
		{
			Sort = string.Empty;
		}
		if (RowFilter == null)
		{
			RowFilter = string.Empty;
		}
		DataExpression newRowFilter = new DataExpression(table, RowFilter);
		SetIndex(Sort, RowState, newRowFilter);
	}

	internal DataView(DataTable table, Predicate<DataRow> predicate, Comparison<DataRow> comparison, DataViewRowState RowState)
	{
		GC.SuppressFinalize(this);
		DataCommonEventSource.Log.Trace("<ds.DataView.DataView|API> %d#, table=%d, RowState=%d{ds.DataViewRowState}\n", ObjectID, table?.ObjectID ?? 0, (int)RowState);
		if (table == null)
		{
			throw ExceptionBuilder.CanNotUse();
		}
		_dvListener = new DataViewListener(this);
		_locked = false;
		_table = table;
		_dvListener.RegisterMetaDataEvents(table);
		if (((uint)RowState & 0xFFFFFFC1u) != 0)
		{
			throw ExceptionBuilder.RecordStateRange();
		}
		if ((RowState & DataViewRowState.ModifiedOriginal) != 0 && (RowState & DataViewRowState.ModifiedCurrent) != 0)
		{
			throw ExceptionBuilder.SetRowStateFilter();
		}
		_comparison = comparison;
		SetIndex2("", RowState, (predicate != null) ? new RowPredicateFilter(predicate) : null, fireEvent: true);
	}

	public virtual DataRowView AddNew()
	{
		long scopeId = DataCommonEventSource.Log.EnterScope("<ds.DataView.AddNew|API> {0}", ObjectID);
		try
		{
			CheckOpen();
			if (!AllowNew)
			{
				throw ExceptionBuilder.AddNewNotAllowNull();
			}
			if (_addNewRow != null)
			{
				_rowViewCache[_addNewRow].EndEdit();
			}
			_addNewRow = _table.NewRow();
			DataRowView dataRowView = new DataRowView(this, _addNewRow);
			_rowViewCache.Add(_addNewRow, dataRowView);
			OnListChanged(new ListChangedEventArgs(ListChangedType.ItemAdded, IndexOf(dataRowView)));
			return dataRowView;
		}
		finally
		{
			DataCommonEventSource.Log.ExitScope(scopeId);
		}
	}

	public void BeginInit()
	{
		_fInitInProgress = true;
	}

	[UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:RequiresUnreferencedCode", Justification = "Warning related to RowFilter has already been shown when RowFilter was delay set.")]
	public void EndInit()
	{
		if (_delayedTable != null && _delayedTable.fInitInProgress)
		{
			_delayedTable._delayedViews.Add(this);
			return;
		}
		_fInitInProgress = false;
		_fEndInitInProgress = true;
		if (_delayedTable != null)
		{
			Table = _delayedTable;
			_delayedTable = null;
		}
		if (_delayedSort != null)
		{
			Sort = _delayedSort;
			_delayedSort = null;
		}
		if (_delayedRowFilter != null)
		{
			RowFilter = _delayedRowFilter;
			_delayedRowFilter = null;
		}
		if (_delayedRecordStates != (DataViewRowState)(-1))
		{
			RowStateFilter = _delayedRecordStates;
			_delayedRecordStates = (DataViewRowState)(-1);
		}
		_fEndInitInProgress = false;
		SetIndex(Sort, RowStateFilter, _rowFilter);
		OnInitialized();
	}

	private void CheckOpen()
	{
		if (!IsOpen)
		{
			throw ExceptionBuilder.NotOpen();
		}
	}

	private void CheckSort(string sort)
	{
		if (_table == null)
		{
			throw ExceptionBuilder.CanNotUse();
		}
		if (sort.Length != 0)
		{
			_table.ParseSortString(sort);
		}
	}

	protected void Close()
	{
		_shouldOpen = false;
		UpdateIndex();
		_dvListener.UnregisterMetaDataEvents();
	}

	public void CopyTo(Array array, int index)
	{
		if (_index != null)
		{
			RBTree<int>.RBTreeEnumerator enumerator = _index.GetEnumerator(0);
			while (enumerator.MoveNext())
			{
				array.SetValue(GetRowView(enumerator.Current), index);
				index = checked(index + 1);
			}
		}
		if (_addNewRow != null)
		{
			array.SetValue(_rowViewCache[_addNewRow], index);
		}
	}

	private void CopyTo(DataRowView[] array, int index)
	{
		if (_index != null)
		{
			RBTree<int>.RBTreeEnumerator enumerator = _index.GetEnumerator(0);
			while (enumerator.MoveNext())
			{
				array[index] = GetRowView(enumerator.Current);
				index = checked(index + 1);
			}
		}
		if (_addNewRow != null)
		{
			array[index] = _rowViewCache[_addNewRow];
		}
	}

	public void Delete(int index)
	{
		Delete(GetRow(index));
	}

	internal void Delete(DataRow row)
	{
		if (row == null)
		{
			return;
		}
		long scopeId = DataCommonEventSource.Log.EnterScope("<ds.DataView.Delete|API> {0}, row={1}", ObjectID, row._objectID);
		try
		{
			CheckOpen();
			if (row == _addNewRow)
			{
				FinishAddNew(success: false);
				return;
			}
			if (!AllowDelete)
			{
				throw ExceptionBuilder.CanNotDelete();
			}
			row.Delete();
		}
		finally
		{
			DataCommonEventSource.Log.ExitScope(scopeId);
		}
	}

	protected override void Dispose(bool disposing)
	{
		if (disposing)
		{
			Close();
		}
		base.Dispose(disposing);
	}

	public int Find(object? key)
	{
		return FindByKey(key);
	}

	internal virtual int FindByKey(object key)
	{
		return _index.FindRecordByKey(key);
	}

	public int Find(object?[] key)
	{
		return FindByKey(key);
	}

	internal virtual int FindByKey(object[] key)
	{
		return _index.FindRecordByKey(key);
	}

	public DataRowView[] FindRows(object? key)
	{
		return FindRowsByKey(new object[1] { key });
	}

	public DataRowView[] FindRows(object?[] key)
	{
		return FindRowsByKey(key);
	}

	internal virtual DataRowView[] FindRowsByKey(object[] key)
	{
		long scopeId = DataCommonEventSource.Log.EnterScope("<ds.DataView.FindRows|API> {0}", ObjectID);
		try
		{
			Range range = _index.FindRecords(key);
			return GetDataRowViewFromRange(range);
		}
		finally
		{
			DataCommonEventSource.Log.ExitScope(scopeId);
		}
	}

	internal Range FindRecords<TKey, TRow>(Index.ComparisonBySelector<TKey, TRow> comparison, TKey key) where TRow : DataRow?
	{
		return _index.FindRecords(comparison, key);
	}

	internal DataRowView[] GetDataRowViewFromRange(Range range)
	{
		if (range.IsNull)
		{
			return Array.Empty<DataRowView>();
		}
		DataRowView[] array = new DataRowView[range.Count];
		for (int i = 0; i < array.Length; i++)
		{
			array[i] = this[i + range.Min];
		}
		return array;
	}

	internal void FinishAddNew(bool success)
	{
		DataCommonEventSource.Log.Trace("<ds.DataView.FinishAddNew|INFO> {0}, success={1}", ObjectID, success);
		DataRow addNewRow = _addNewRow;
		if (success)
		{
			if (DataRowState.Detached == addNewRow.RowState)
			{
				_table.Rows.Add(addNewRow);
			}
			else
			{
				addNewRow.EndEdit();
			}
		}
		if (addNewRow == _addNewRow)
		{
			bool flag = _rowViewCache.Remove(_addNewRow);
			_addNewRow = null;
			if (!success)
			{
				addNewRow.CancelEdit();
			}
			OnListChanged(new ListChangedEventArgs(ListChangedType.ItemDeleted, Count));
		}
	}

	public IEnumerator GetEnumerator()
	{
		DataRowView[] array = new DataRowView[Count];
		CopyTo(array, 0);
		return array.GetEnumerator();
	}

	int IList.Add(object value)
	{
		if (value == null)
		{
			AddNew();
			return Count - 1;
		}
		throw ExceptionBuilder.AddExternalObject();
	}

	void IList.Clear()
	{
		throw ExceptionBuilder.CanNotClear();
	}

	bool IList.Contains(object value)
	{
		return 0 <= IndexOf(value as DataRowView);
	}

	int IList.IndexOf(object value)
	{
		return IndexOf(value as DataRowView);
	}

	internal int IndexOf(DataRowView rowview)
	{
		if (rowview != null)
		{
			if (_addNewRow == rowview.Row)
			{
				return Count - 1;
			}
			if (_index != null && DataRowState.Detached != rowview.Row.RowState && _rowViewCache.TryGetValue(rowview.Row, out var value) && value == rowview)
			{
				return IndexOfDataRowView(rowview);
			}
		}
		return -1;
	}

	private int IndexOfDataRowView(DataRowView rowview)
	{
		return _index.GetIndex(rowview.Row.GetRecordFromVersion(rowview.Row.GetDefaultRowVersion(RowStateFilter) & (DataRowVersion)(-1025)));
	}

	void IList.Insert(int index, object value)
	{
		throw ExceptionBuilder.InsertExternalObject();
	}

	void IList.Remove(object value)
	{
		int num = IndexOf(value as DataRowView);
		if (0 <= num)
		{
			((IList)this).RemoveAt(num);
			return;
		}
		throw ExceptionBuilder.RemoveExternalObject();
	}

	void IList.RemoveAt(int index)
	{
		Delete(index);
	}

	internal Index GetFindIndex(string column, bool keepIndex)
	{
		if (_findIndexes == null)
		{
			_findIndexes = new Dictionary<string, Index>();
		}
		if (_findIndexes.TryGetValue(column, out var value))
		{
			if (!keepIndex)
			{
				_findIndexes.Remove(column);
				value.RemoveRef();
				if (value.RefCount == 1)
				{
					value.RemoveRef();
				}
			}
		}
		else if (keepIndex)
		{
			value = _table.GetIndex(column, _recordStates, GetFilter());
			_findIndexes[column] = value;
			value.AddRef();
		}
		return value;
	}

	object IBindingList.AddNew()
	{
		return AddNew();
	}

	internal PropertyDescriptor GetSortProperty()
	{
		if (_table != null && _index != null && _index._indexFields.Length == 1)
		{
			return new DataColumnPropertyDescriptor(_index._indexFields[0].Column);
		}
		return null;
	}

	void IBindingList.AddIndex(PropertyDescriptor property)
	{
		GetFindIndex(property.Name, keepIndex: true);
	}

	void IBindingList.ApplySort(PropertyDescriptor property, ListSortDirection direction)
	{
		Sort = CreateSortString(property, direction);
	}

	int IBindingList.Find(PropertyDescriptor property, object key)
	{
		if (property != null)
		{
			bool flag = false;
			Index value = null;
			try
			{
				if (_findIndexes == null || !_findIndexes.TryGetValue(property.Name, out value))
				{
					flag = true;
					value = _table.GetIndex(property.Name, _recordStates, GetFilter());
					value.AddRef();
				}
				Range range = value.FindRecords(key);
				if (!range.IsNull)
				{
					return _index.GetIndex(value.GetRecord(range.Min));
				}
			}
			finally
			{
				if (flag && value != null)
				{
					value.RemoveRef();
					if (value.RefCount == 1)
					{
						value.RemoveRef();
					}
				}
			}
		}
		return -1;
	}

	void IBindingList.RemoveIndex(PropertyDescriptor property)
	{
		GetFindIndex(property.Name, keepIndex: false);
	}

	void IBindingList.RemoveSort()
	{
		DataCommonEventSource.Log.Trace("<ds.DataView.RemoveSort|API> {0}", ObjectID);
		Sort = string.Empty;
	}

	void IBindingListView.ApplySort(ListSortDescriptionCollection sorts)
	{
		if (sorts == null)
		{
			throw ExceptionBuilder.ArgumentNull("sorts");
		}
		StringBuilder stringBuilder = new StringBuilder();
		bool flag = false;
		foreach (ListSortDescription item in (IEnumerable)sorts)
		{
			if (item == null)
			{
				throw ExceptionBuilder.ArgumentContainsNull("sorts");
			}
			PropertyDescriptor propertyDescriptor = item.PropertyDescriptor;
			if (propertyDescriptor == null)
			{
				throw ExceptionBuilder.ArgumentNull("PropertyDescriptor");
			}
			if (!_table.Columns.Contains(propertyDescriptor.Name))
			{
				throw ExceptionBuilder.ColumnToSortIsOutOfRange(propertyDescriptor.Name);
			}
			ListSortDirection sortDirection = item.SortDirection;
			if (flag)
			{
				stringBuilder.Append(',');
			}
			stringBuilder.Append(CreateSortString(propertyDescriptor, sortDirection));
			if (!flag)
			{
				flag = true;
			}
		}
		Sort = stringBuilder.ToString();
	}

	private string CreateSortString(PropertyDescriptor property, ListSortDirection direction)
	{
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.Append('[');
		stringBuilder.Append(property.Name);
		stringBuilder.Append(']');
		if (ListSortDirection.Descending == direction)
		{
			stringBuilder.Append(" DESC");
		}
		return stringBuilder.ToString();
	}

	[UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:RequiresUnreferencedCode", Justification = "Safe because filter is set to empty string.")]
	void IBindingListView.RemoveFilter()
	{
		DataCommonEventSource.Log.Trace("<ds.DataView.RemoveFilter|API> {0}", ObjectID);
		RowFilter = string.Empty;
	}

	internal ListSortDescriptionCollection GetSortDescriptions()
	{
		ListSortDescription[] array = Array.Empty<ListSortDescription>();
		if (_table != null && _index != null && _index._indexFields.Length != 0)
		{
			array = new ListSortDescription[_index._indexFields.Length];
			for (int i = 0; i < _index._indexFields.Length; i++)
			{
				DataColumnPropertyDescriptor property = new DataColumnPropertyDescriptor(_index._indexFields[i].Column);
				if (_index._indexFields[i].IsDescending)
				{
					array[i] = new ListSortDescription(property, ListSortDirection.Descending);
				}
				else
				{
					array[i] = new ListSortDescription(property, ListSortDirection.Ascending);
				}
			}
		}
		return new ListSortDescriptionCollection(array);
	}

	string ITypedList.GetListName(PropertyDescriptor[] listAccessors)
	{
		if (_table != null)
		{
			if (listAccessors == null || listAccessors.Length == 0)
			{
				return _table.TableName;
			}
			DataSet dataSet = _table.DataSet;
			if (dataSet != null)
			{
				DataTable dataTable = dataSet.FindTable(_table, listAccessors, 0);
				if (dataTable != null)
				{
					return dataTable.TableName;
				}
			}
		}
		return string.Empty;
	}

	PropertyDescriptorCollection ITypedList.GetItemProperties(PropertyDescriptor[] listAccessors)
	{
		if (_table != null)
		{
			if (listAccessors == null || listAccessors.Length == 0)
			{
				return _table.GetPropertyDescriptorCollection(null);
			}
			DataSet dataSet = _table.DataSet;
			if (dataSet == null)
			{
				return new PropertyDescriptorCollection(null);
			}
			DataTable dataTable = dataSet.FindTable(_table, listAccessors, 0);
			if (dataTable != null)
			{
				return dataTable.GetPropertyDescriptorCollection(null);
			}
		}
		return new PropertyDescriptorCollection(null);
	}

	internal virtual IFilter GetFilter()
	{
		return _rowFilter;
	}

	private int GetRecord(int recordIndex)
	{
		if ((uint)Count <= (uint)recordIndex)
		{
			throw ExceptionBuilder.RowOutOfRange(recordIndex);
		}
		if (recordIndex != _index.RecordCount)
		{
			return _index.GetRecord(recordIndex);
		}
		return _addNewRow.GetDefaultRecord();
	}

	internal DataRow GetRow(int index)
	{
		int count = Count;
		if ((uint)count <= (uint)index)
		{
			throw ExceptionBuilder.GetElementIndex(index);
		}
		if (index == count - 1 && _addNewRow != null)
		{
			return _addNewRow;
		}
		return _table._recordManager[GetRecord(index)];
	}

	private DataRowView GetRowView(int record)
	{
		return GetRowView(_table._recordManager[record]);
	}

	private DataRowView GetRowView(DataRow dr)
	{
		return _rowViewCache[dr];
	}

	protected virtual void IndexListChanged(object sender, ListChangedEventArgs e)
	{
		if (e.ListChangedType != 0)
		{
			OnListChanged(e);
		}
		if (_addNewRow != null && _index.RecordCount == 0)
		{
			FinishAddNew(success: false);
		}
		if (e.ListChangedType == ListChangedType.Reset)
		{
			OnListChanged(e);
		}
	}

	internal void IndexListChangedInternal(ListChangedEventArgs e)
	{
		_rowViewBuffer.Clear();
		if (ListChangedType.ItemAdded == e.ListChangedType && _addNewMoved != null && _addNewMoved.NewIndex != _addNewMoved.OldIndex)
		{
			ListChangedEventArgs addNewMoved = _addNewMoved;
			_addNewMoved = null;
			IndexListChanged(this, addNewMoved);
		}
		IndexListChanged(this, e);
	}

	internal void MaintainDataView(ListChangedType changedType, DataRow row, bool trackAddRemove)
	{
		DataRowView value = null;
		switch (changedType)
		{
		case ListChangedType.ItemAdded:
			if (trackAddRemove && _rowViewBuffer.TryGetValue(row, out value))
			{
				bool flag = _rowViewBuffer.Remove(row);
			}
			if (row == _addNewRow)
			{
				int newIndex = IndexOfDataRowView(_rowViewCache[_addNewRow]);
				_addNewRow = null;
				_addNewMoved = new ListChangedEventArgs(ListChangedType.ItemMoved, newIndex, Count - 1);
			}
			else if (!_rowViewCache.ContainsKey(row))
			{
				_rowViewCache.Add(row, value ?? new DataRowView(this, row));
			}
			break;
		case ListChangedType.ItemDeleted:
			if (trackAddRemove)
			{
				_rowViewCache.TryGetValue(row, out value);
				if (value != null)
				{
					_rowViewBuffer.Add(row, value);
				}
			}
			_rowViewCache.Remove(row);
			break;
		case ListChangedType.Reset:
			ResetRowViewCache();
			break;
		case ListChangedType.ItemMoved:
		case ListChangedType.ItemChanged:
		case ListChangedType.PropertyDescriptorAdded:
		case ListChangedType.PropertyDescriptorDeleted:
		case ListChangedType.PropertyDescriptorChanged:
			break;
		}
	}

	protected virtual void OnListChanged(ListChangedEventArgs e)
	{
		DataCommonEventSource.Log.Trace("<ds.DataView.OnListChanged|INFO> {0}, ListChangedType={1}", ObjectID, e.ListChangedType);
		try
		{
			DataColumn dataColumn = null;
			string text = null;
			switch (e.ListChangedType)
			{
			case ListChangedType.ItemMoved:
			case ListChangedType.ItemChanged:
				if (0 <= e.NewIndex)
				{
					DataRow row = GetRow(e.NewIndex);
					if (row.HasPropertyChanged)
					{
						dataColumn = row.LastChangedColumn;
						text = ((dataColumn != null) ? dataColumn.ColumnName : string.Empty);
					}
				}
				break;
			}
			if (_onListChanged != null)
			{
				if (dataColumn != null && e.NewIndex == e.OldIndex)
				{
					ListChangedEventArgs e2 = new ListChangedEventArgs(e.ListChangedType, e.NewIndex, new DataColumnPropertyDescriptor(dataColumn));
					_onListChanged(this, e2);
				}
				else
				{
					_onListChanged(this, e);
				}
			}
			if (text != null)
			{
				this[e.NewIndex].RaisePropertyChangedEvent(text);
			}
		}
		catch (Exception e3) when (ADP.IsCatchableExceptionType(e3))
		{
			ExceptionBuilder.TraceExceptionWithoutRethrow(e3);
		}
	}

	private void OnInitialized()
	{
		this.Initialized?.Invoke(this, EventArgs.Empty);
	}

	protected void Open()
	{
		_shouldOpen = true;
		UpdateIndex();
		_dvListener.RegisterMetaDataEvents(_table);
	}

	protected void Reset()
	{
		if (IsOpen)
		{
			_index.Reset();
		}
	}

	internal void ResetRowViewCache()
	{
		Dictionary<DataRow, DataRowView> dictionary = new Dictionary<DataRow, DataRowView>(CountFromIndex, DataRowReferenceComparer.s_default);
		DataRowView value;
		if (_index != null)
		{
			RBTree<int>.RBTreeEnumerator enumerator = _index.GetEnumerator(0);
			while (enumerator.MoveNext())
			{
				DataRow dataRow = _table._recordManager[enumerator.Current];
				if (!_rowViewCache.TryGetValue(dataRow, out value))
				{
					value = new DataRowView(this, dataRow);
				}
				dictionary.Add(dataRow, value);
			}
		}
		if (_addNewRow != null)
		{
			_rowViewCache.TryGetValue(_addNewRow, out value);
			dictionary.Add(_addNewRow, value);
		}
		_rowViewCache = dictionary;
	}

	internal void SetDataViewManager(DataViewManager dataViewManager)
	{
		if (_table == null)
		{
			throw ExceptionBuilder.CanNotUse();
		}
		if (_dataViewManager == dataViewManager)
		{
			return;
		}
		if (dataViewManager != null)
		{
			dataViewManager._nViews--;
		}
		_dataViewManager = dataViewManager;
		if (dataViewManager != null)
		{
			dataViewManager._nViews++;
			DataViewSetting dataViewSetting = dataViewManager.DataViewSettings[_table];
			try
			{
				_applyDefaultSort = dataViewSetting.ApplyDefaultSort;
				DataExpression newRowFilter = CreateDataExpressionFromDataViewSettings(dataViewSetting);
				SetIndex(dataViewSetting.Sort, dataViewSetting.RowStateFilter, newRowFilter);
			}
			catch (Exception e) when (ADP.IsCatchableExceptionType(e))
			{
				ExceptionBuilder.TraceExceptionWithoutRethrow(e);
			}
			_locked = true;
		}
		else
		{
			SetIndex("", DataViewRowState.CurrentRows, null);
		}
	}

	[UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:RequiresUnreferencedCode", Justification = "RowFilter is marked as unsafe because it can be used in DataExpression so that we only display warning when user is assigning an expression which means that in here we're either assigning empty filter which is safe or user has already seen a warning.")]
	private DataExpression CreateDataExpressionFromDataViewSettings(DataViewSetting dataViewSetting)
	{
		return new DataExpression(_table, dataViewSetting.RowFilter);
	}

	internal virtual void SetIndex(string newSort, DataViewRowState newRowStates, IFilter newRowFilter)
	{
		SetIndex2(newSort, newRowStates, newRowFilter, fireEvent: true);
	}

	internal void SetIndex2(string newSort, DataViewRowState newRowStates, IFilter newRowFilter, bool fireEvent)
	{
		DataCommonEventSource.Log.Trace("<ds.DataView.SetIndex|INFO> {0}, newSort='{1}', newRowStates={2}", ObjectID, newSort, newRowStates);
		_sort = newSort;
		_recordStates = newRowStates;
		_rowFilter = newRowFilter;
		if (_fEndInitInProgress)
		{
			return;
		}
		if (fireEvent)
		{
			UpdateIndex(force: true);
		}
		else
		{
			UpdateIndex(force: true, fireEvent: false);
		}
		if (_findIndexes == null)
		{
			return;
		}
		Dictionary<string, Index> findIndexes = _findIndexes;
		_findIndexes = null;
		foreach (KeyValuePair<string, Index> item in findIndexes)
		{
			item.Value.RemoveRef();
		}
	}

	protected void UpdateIndex()
	{
		UpdateIndex(force: false);
	}

	protected virtual void UpdateIndex(bool force)
	{
		UpdateIndex(force, fireEvent: true);
	}

	internal void UpdateIndex(bool force, bool fireEvent)
	{
		long scopeId = DataCommonEventSource.Log.EnterScope("<ds.DataView.UpdateIndex|INFO> {0}, force={1}", ObjectID, force);
		try
		{
			if (!(_open != _shouldOpen || force))
			{
				return;
			}
			_open = _shouldOpen;
			Index index = null;
			if (_open && _table != null)
			{
				if (SortComparison != null)
				{
					index = new Index(_table, SortComparison, _recordStates, GetFilter());
					index.AddRef();
				}
				else
				{
					index = _table.GetIndex(Sort, _recordStates, GetFilter());
				}
			}
			if (_index != index)
			{
				if (_index != null)
				{
					_dvListener.UnregisterListChangedEvent();
				}
				_index = index;
				if (_index != null)
				{
					_dvListener.RegisterListChangedEvent(_index);
				}
				ResetRowViewCache();
				if (fireEvent)
				{
					OnListChanged(s_resetEventArgs);
				}
			}
		}
		finally
		{
			DataCommonEventSource.Log.ExitScope(scopeId);
		}
	}

	internal void ChildRelationCollectionChanged(object sender, CollectionChangeEventArgs e)
	{
		DataRelationPropertyDescriptor propDesc = null;
		OnListChanged((e.Action == CollectionChangeAction.Add) ? new ListChangedEventArgs(ListChangedType.PropertyDescriptorAdded, new DataRelationPropertyDescriptor((DataRelation)e.Element)) : ((e.Action == CollectionChangeAction.Refresh) ? new ListChangedEventArgs(ListChangedType.PropertyDescriptorChanged, propDesc) : ((e.Action == CollectionChangeAction.Remove) ? new ListChangedEventArgs(ListChangedType.PropertyDescriptorDeleted, new DataRelationPropertyDescriptor((DataRelation)e.Element)) : null)));
	}

	internal void ParentRelationCollectionChanged(object sender, CollectionChangeEventArgs e)
	{
		DataRelationPropertyDescriptor propDesc = null;
		OnListChanged((e.Action == CollectionChangeAction.Add) ? new ListChangedEventArgs(ListChangedType.PropertyDescriptorAdded, new DataRelationPropertyDescriptor((DataRelation)e.Element)) : ((e.Action == CollectionChangeAction.Refresh) ? new ListChangedEventArgs(ListChangedType.PropertyDescriptorChanged, propDesc) : ((e.Action == CollectionChangeAction.Remove) ? new ListChangedEventArgs(ListChangedType.PropertyDescriptorDeleted, new DataRelationPropertyDescriptor((DataRelation)e.Element)) : null)));
	}

	protected virtual void ColumnCollectionChanged(object? sender, CollectionChangeEventArgs e)
	{
		DataColumnPropertyDescriptor propDesc = null;
		OnListChanged((e.Action == CollectionChangeAction.Add) ? new ListChangedEventArgs(ListChangedType.PropertyDescriptorAdded, new DataColumnPropertyDescriptor((DataColumn)e.Element)) : ((e.Action == CollectionChangeAction.Refresh) ? new ListChangedEventArgs(ListChangedType.PropertyDescriptorChanged, propDesc) : ((e.Action == CollectionChangeAction.Remove) ? new ListChangedEventArgs(ListChangedType.PropertyDescriptorDeleted, new DataColumnPropertyDescriptor((DataColumn)e.Element)) : null)));
	}

	internal void ColumnCollectionChangedInternal(object sender, CollectionChangeEventArgs e)
	{
		ColumnCollectionChanged(sender, e);
	}

	public DataTable ToTable()
	{
		return ToTable(null, false);
	}

	public DataTable ToTable(string? tableName)
	{
		return ToTable(tableName, false);
	}

	public DataTable ToTable(bool distinct, params string[] columnNames)
	{
		return ToTable(null, distinct, columnNames);
	}

	public DataTable ToTable(string? tableName, bool distinct, params string[] columnNames)
	{
		DataCommonEventSource.Log.Trace("<ds.DataView.ToTable|API> {0}, TableName='{1}', distinct={2}", ObjectID, tableName, distinct);
		if (columnNames == null)
		{
			throw ExceptionBuilder.ArgumentNull("columnNames");
		}
		DataTable dataTable = new DataTable();
		dataTable.Locale = _table.Locale;
		dataTable.CaseSensitive = _table.CaseSensitive;
		dataTable.TableName = ((tableName != null) ? tableName : _table.TableName);
		dataTable.Namespace = _table.Namespace;
		dataTable.Prefix = _table.Prefix;
		if (columnNames.Length == 0)
		{
			columnNames = new string[Table.Columns.Count];
			for (int i = 0; i < columnNames.Length; i++)
			{
				columnNames[i] = Table.Columns[i].ColumnName;
			}
		}
		int[] array = new int[columnNames.Length];
		List<object[]> list = new List<object[]>();
		for (int j = 0; j < columnNames.Length; j++)
		{
			DataColumn dataColumn = Table.Columns[columnNames[j]];
			if (dataColumn == null)
			{
				throw ExceptionBuilder.ColumnNotInTheUnderlyingTable(columnNames[j], Table.TableName);
			}
			dataTable.Columns.Add(dataColumn.Clone());
			array[j] = Table.Columns.IndexOf(dataColumn);
		}
		IEnumerator enumerator = GetEnumerator();
		try
		{
			while (enumerator.MoveNext())
			{
				DataRowView dataRowView = (DataRowView)enumerator.Current;
				object[] array2 = new object[columnNames.Length];
				for (int k = 0; k < array.Length; k++)
				{
					array2[k] = dataRowView[array[k]];
				}
				if (!distinct || !RowExist(list, array2))
				{
					dataTable.Rows.Add(array2);
					list.Add(array2);
				}
			}
			return dataTable;
		}
		finally
		{
			IDisposable disposable = enumerator as IDisposable;
			if (disposable != null)
			{
				disposable.Dispose();
			}
		}
	}

	private bool RowExist(List<object[]> arraylist, object[] objectArray)
	{
		for (int i = 0; i < arraylist.Count; i++)
		{
			object[] array = arraylist[i];
			bool flag = true;
			for (int j = 0; j < objectArray.Length; j++)
			{
				flag &= array[j].Equals(objectArray[j]);
			}
			if (flag)
			{
				return true;
			}
		}
		return false;
	}

	public virtual bool Equals(DataView? view)
	{
		if (view == null || Table != view.Table || Count != view.Count || !string.Equals(RowFilter, view.RowFilter, StringComparison.OrdinalIgnoreCase) || !string.Equals(Sort, view.Sort, StringComparison.OrdinalIgnoreCase) || (object)SortComparison != view.SortComparison || (object)RowPredicate != view.RowPredicate || RowStateFilter != view.RowStateFilter || DataViewManager != view.DataViewManager || AllowDelete != view.AllowDelete || AllowNew != view.AllowNew || AllowEdit != view.AllowEdit)
		{
			return false;
		}
		return true;
	}
}
