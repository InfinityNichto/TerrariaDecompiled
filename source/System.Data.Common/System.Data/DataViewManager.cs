using System.Collections;
using System.ComponentModel;
using System.Data.Common;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Text;
using System.Xml;

namespace System.Data;

[Designer("Microsoft.VSDesigner.Data.VS.DataViewManagerDesigner, Microsoft.VSDesigner, Version=10.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
public class DataViewManager : MarshalByValueComponent, IBindingList, IList, ICollection, IEnumerable, ITypedList
{
	private DataViewSettingCollection _dataViewSettingsCollection;

	private DataSet _dataSet;

	private readonly DataViewManagerListItemTypeDescriptor _item;

	private readonly bool _locked;

	internal int _nViews;

	private static readonly NotSupportedException s_notSupported = new NotSupportedException();

	[DefaultValue(null)]
	public DataSet? DataSet
	{
		get
		{
			return _dataSet;
		}
		[param: DisallowNull]
		set
		{
			if (value == null)
			{
				throw ExceptionBuilder.SetFailed("DataSet to null");
			}
			if (_locked)
			{
				throw ExceptionBuilder.SetDataSetFailed();
			}
			if (_dataSet != null)
			{
				if (_nViews > 0)
				{
					throw ExceptionBuilder.CanNotSetDataSet();
				}
				_dataSet.Tables.CollectionChanged -= TableCollectionChanged;
				_dataSet.Relations.CollectionChanged -= RelationCollectionChanged;
			}
			_dataSet = value;
			_dataSet.Tables.CollectionChanged += TableCollectionChanged;
			_dataSet.Relations.CollectionChanged += RelationCollectionChanged;
			_dataViewSettingsCollection = new DataViewSettingCollection(this);
			_item.Reset();
		}
	}

	[DesignerSerializationVisibility(DesignerSerializationVisibility.Content)]
	public DataViewSettingCollection DataViewSettings => _dataViewSettingsCollection;

	public string DataViewSettingCollectionString
	{
		get
		{
			if (_dataSet == null)
			{
				return string.Empty;
			}
			StringBuilder stringBuilder = new StringBuilder();
			stringBuilder.Append("<DataViewSettingCollectionString>");
			foreach (DataTable table in _dataSet.Tables)
			{
				DataViewSetting dataViewSetting = _dataViewSettingsCollection[table];
				StringBuilder stringBuilder2 = stringBuilder;
				IFormatProvider invariantCulture = CultureInfo.InvariantCulture;
				StringBuilder.AppendInterpolatedStringHandler handler = new StringBuilder.AppendInterpolatedStringHandler(42, 4, stringBuilder2, invariantCulture);
				handler.AppendLiteral("<");
				handler.AppendFormatted(table.EncodedTableName);
				handler.AppendLiteral(" Sort=\"");
				handler.AppendFormatted(dataViewSetting.Sort);
				handler.AppendLiteral("\" RowFilter=\"");
				handler.AppendFormatted(dataViewSetting.RowFilter);
				handler.AppendLiteral("\" RowStateFilter=\"");
				handler.AppendFormatted(dataViewSetting.RowStateFilter);
				handler.AppendLiteral("\"/>");
				stringBuilder2.Append(invariantCulture, ref handler);
			}
			stringBuilder.Append("</DataViewSettingCollectionString>");
			return stringBuilder.ToString();
		}
		[RequiresUnreferencedCode("Members of types used in the RowFilter expression might be trimmed.")]
		set
		{
			if (string.IsNullOrEmpty(value))
			{
				return;
			}
			XmlTextReader xmlTextReader = new XmlTextReader(new StringReader(value));
			xmlTextReader.WhitespaceHandling = WhitespaceHandling.None;
			xmlTextReader.Read();
			if (xmlTextReader.Name != "DataViewSettingCollectionString")
			{
				throw ExceptionBuilder.SetFailed("DataViewSettingCollectionString");
			}
			while (xmlTextReader.Read())
			{
				if (xmlTextReader.NodeType == XmlNodeType.Element)
				{
					string tableName = XmlConvert.DecodeName(xmlTextReader.LocalName);
					if (xmlTextReader.MoveToAttribute("Sort"))
					{
						_dataViewSettingsCollection[tableName].Sort = xmlTextReader.Value;
					}
					if (xmlTextReader.MoveToAttribute("RowFilter"))
					{
						_dataViewSettingsCollection[tableName].RowFilter = xmlTextReader.Value;
					}
					if (xmlTextReader.MoveToAttribute("RowStateFilter"))
					{
						_dataViewSettingsCollection[tableName].RowStateFilter = (DataViewRowState)Enum.Parse(typeof(DataViewRowState), xmlTextReader.Value);
					}
				}
			}
		}
	}

	int ICollection.Count => 1;

	object ICollection.SyncRoot => this;

	bool ICollection.IsSynchronized => false;

	bool IList.IsReadOnly => true;

	bool IList.IsFixedSize => true;

	object? IList.this[int index]
	{
		get
		{
			return _item;
		}
		set
		{
			throw ExceptionBuilder.CannotModifyCollection();
		}
	}

	bool IBindingList.AllowNew => false;

	bool IBindingList.AllowEdit => false;

	bool IBindingList.AllowRemove => false;

	bool IBindingList.SupportsChangeNotification => true;

	bool IBindingList.SupportsSearching => false;

	bool IBindingList.SupportsSorting => false;

	bool IBindingList.IsSorted
	{
		get
		{
			throw s_notSupported;
		}
	}

	PropertyDescriptor IBindingList.SortProperty
	{
		get
		{
			throw s_notSupported;
		}
	}

	ListSortDirection IBindingList.SortDirection
	{
		get
		{
			throw s_notSupported;
		}
	}

	public event ListChangedEventHandler? ListChanged;

	public DataViewManager()
		: this(null, locked: false)
	{
	}

	public DataViewManager(DataSet? dataSet)
		: this(dataSet, locked: false)
	{
	}

	internal DataViewManager(DataSet dataSet, bool locked)
	{
		GC.SuppressFinalize(this);
		_dataSet = dataSet;
		if (_dataSet != null)
		{
			_dataSet.Tables.CollectionChanged += TableCollectionChanged;
			_dataSet.Relations.CollectionChanged += RelationCollectionChanged;
		}
		_locked = locked;
		_item = new DataViewManagerListItemTypeDescriptor(this);
		_dataViewSettingsCollection = new DataViewSettingCollection(this);
	}

	IEnumerator IEnumerable.GetEnumerator()
	{
		DataViewManagerListItemTypeDescriptor[] array = new DataViewManagerListItemTypeDescriptor[1];
		((ICollection)this).CopyTo((Array)array, 0);
		return array.GetEnumerator();
	}

	void ICollection.CopyTo(Array array, int index)
	{
		array.SetValue(new DataViewManagerListItemTypeDescriptor(this), index);
	}

	int IList.Add(object value)
	{
		throw ExceptionBuilder.CannotModifyCollection();
	}

	void IList.Clear()
	{
		throw ExceptionBuilder.CannotModifyCollection();
	}

	bool IList.Contains(object value)
	{
		return value == _item;
	}

	int IList.IndexOf(object value)
	{
		if (value != _item)
		{
			return -1;
		}
		return 1;
	}

	void IList.Insert(int index, object value)
	{
		throw ExceptionBuilder.CannotModifyCollection();
	}

	void IList.Remove(object value)
	{
		throw ExceptionBuilder.CannotModifyCollection();
	}

	void IList.RemoveAt(int index)
	{
		throw ExceptionBuilder.CannotModifyCollection();
	}

	object IBindingList.AddNew()
	{
		throw s_notSupported;
	}

	void IBindingList.AddIndex(PropertyDescriptor property)
	{
	}

	void IBindingList.ApplySort(PropertyDescriptor property, ListSortDirection direction)
	{
		throw s_notSupported;
	}

	int IBindingList.Find(PropertyDescriptor property, object key)
	{
		throw s_notSupported;
	}

	void IBindingList.RemoveIndex(PropertyDescriptor property)
	{
	}

	void IBindingList.RemoveSort()
	{
		throw s_notSupported;
	}

	string ITypedList.GetListName(PropertyDescriptor[] listAccessors)
	{
		DataSet dataSet = DataSet;
		if (dataSet == null)
		{
			throw ExceptionBuilder.CanNotUseDataViewManager();
		}
		if (listAccessors == null || listAccessors.Length == 0)
		{
			return dataSet.DataSetName;
		}
		DataTable dataTable = dataSet.FindTable(null, listAccessors, 0);
		if (dataTable != null)
		{
			return dataTable.TableName;
		}
		return string.Empty;
	}

	PropertyDescriptorCollection ITypedList.GetItemProperties(PropertyDescriptor[] listAccessors)
	{
		DataSet dataSet = DataSet;
		if (dataSet == null)
		{
			throw ExceptionBuilder.CanNotUseDataViewManager();
		}
		if (listAccessors == null || listAccessors.Length == 0)
		{
			return new DataViewManagerListItemTypeDescriptor(this).GetPropertiesInternal();
		}
		DataTable dataTable = dataSet.FindTable(null, listAccessors, 0);
		if (dataTable != null)
		{
			return dataTable.GetPropertyDescriptorCollection(null);
		}
		return new PropertyDescriptorCollection(null);
	}

	public DataView CreateDataView(DataTable table)
	{
		if (_dataSet == null)
		{
			throw ExceptionBuilder.CanNotUseDataViewManager();
		}
		DataView dataView = new DataView(table);
		dataView.SetDataViewManager(this);
		return dataView;
	}

	protected virtual void OnListChanged(ListChangedEventArgs e)
	{
		try
		{
			this.ListChanged?.Invoke(this, e);
		}
		catch (Exception e2) when (ADP.IsCatchableExceptionType(e2))
		{
			ExceptionBuilder.TraceExceptionWithoutRethrow(e2);
		}
	}

	protected virtual void TableCollectionChanged(object? sender, CollectionChangeEventArgs e)
	{
		PropertyDescriptor propDesc = null;
		OnListChanged((e.Action == CollectionChangeAction.Add) ? new ListChangedEventArgs(ListChangedType.PropertyDescriptorAdded, new DataTablePropertyDescriptor((DataTable)e.Element)) : ((e.Action == CollectionChangeAction.Refresh) ? new ListChangedEventArgs(ListChangedType.PropertyDescriptorChanged, propDesc) : ((e.Action == CollectionChangeAction.Remove) ? new ListChangedEventArgs(ListChangedType.PropertyDescriptorDeleted, new DataTablePropertyDescriptor((DataTable)e.Element)) : null)));
	}

	protected virtual void RelationCollectionChanged(object? sender, CollectionChangeEventArgs e)
	{
		DataRelationPropertyDescriptor propDesc = null;
		OnListChanged((e.Action == CollectionChangeAction.Add) ? new ListChangedEventArgs(ListChangedType.PropertyDescriptorAdded, new DataRelationPropertyDescriptor((DataRelation)e.Element)) : ((e.Action == CollectionChangeAction.Refresh) ? new ListChangedEventArgs(ListChangedType.PropertyDescriptorChanged, propDesc) : ((e.Action == CollectionChangeAction.Remove) ? new ListChangedEventArgs(ListChangedType.PropertyDescriptorDeleted, new DataRelationPropertyDescriptor((DataRelation)e.Element)) : null)));
	}
}
