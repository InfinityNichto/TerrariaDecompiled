using System.Collections;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;

namespace System.Data;

[Editor("Microsoft.VSDesigner.Data.Design.DataViewSettingsCollectionEditor, Microsoft.VSDesigner, Version=10.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a", "System.Drawing.Design.UITypeEditor, System.Drawing, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
public class DataViewSettingCollection : ICollection, IEnumerable
{
	private sealed class DataViewSettingsEnumerator : IEnumerator
	{
		private readonly DataViewSettingCollection _dataViewSettings;

		private readonly IEnumerator _tableEnumerator;

		public object Current => _dataViewSettings[(DataTable)_tableEnumerator.Current];

		public DataViewSettingsEnumerator(DataViewManager dvm)
		{
			DataSet dataSet = dvm.DataSet;
			if (dataSet != null)
			{
				_dataViewSettings = dvm.DataViewSettings;
				_tableEnumerator = dvm.DataSet.Tables.GetEnumerator();
			}
			else
			{
				_dataViewSettings = null;
				_tableEnumerator = Array.Empty<DataTable>().GetEnumerator();
			}
		}

		public bool MoveNext()
		{
			return _tableEnumerator.MoveNext();
		}

		public void Reset()
		{
			_tableEnumerator.Reset();
		}
	}

	private readonly DataViewManager _dataViewManager;

	private readonly Hashtable _list = new Hashtable();

	public virtual DataViewSetting this[DataTable table]
	{
		get
		{
			if (table == null)
			{
				throw ExceptionBuilder.ArgumentNull("table");
			}
			DataViewSetting dataViewSetting = (DataViewSetting)_list[table];
			if (dataViewSetting == null)
			{
				dataViewSetting = (this[table] = new DataViewSetting());
			}
			return dataViewSetting;
		}
		set
		{
			if (table == null)
			{
				throw ExceptionBuilder.ArgumentNull("table");
			}
			value.SetDataViewManager(_dataViewManager);
			value.SetDataTable(table);
			_list[table] = value;
		}
	}

	public virtual DataViewSetting? this[string tableName]
	{
		get
		{
			DataTable table = GetTable(tableName);
			if (table != null)
			{
				return this[table];
			}
			return null;
		}
	}

	public virtual DataViewSetting? this[int index]
	{
		get
		{
			DataTable table = GetTable(index);
			if (table != null)
			{
				return this[table];
			}
			return null;
		}
		[param: DisallowNull]
		set
		{
			DataTable table = GetTable(index);
			if (table != null)
			{
				this[table] = value;
			}
		}
	}

	[Browsable(false)]
	public virtual int Count => _dataViewManager.DataSet?.Tables.Count ?? 0;

	[Browsable(false)]
	public bool IsReadOnly => true;

	[Browsable(false)]
	public bool IsSynchronized => false;

	[Browsable(false)]
	public object SyncRoot => this;

	internal DataViewSettingCollection(DataViewManager dataViewManager)
	{
		if (dataViewManager == null)
		{
			throw ExceptionBuilder.ArgumentNull("dataViewManager");
		}
		_dataViewManager = dataViewManager;
	}

	private DataTable GetTable(string tableName)
	{
		DataTable result = null;
		DataSet dataSet = _dataViewManager.DataSet;
		if (dataSet != null)
		{
			result = dataSet.Tables[tableName];
		}
		return result;
	}

	private DataTable GetTable(int index)
	{
		DataTable result = null;
		DataSet dataSet = _dataViewManager.DataSet;
		if (dataSet != null)
		{
			result = dataSet.Tables[index];
		}
		return result;
	}

	public void CopyTo(Array ar, int index)
	{
		IEnumerator enumerator = GetEnumerator();
		while (enumerator.MoveNext())
		{
			ar.SetValue(enumerator.Current, index++);
		}
	}

	public void CopyTo(DataViewSetting[] ar, int index)
	{
		IEnumerator enumerator = GetEnumerator();
		while (enumerator.MoveNext())
		{
			ar.SetValue(enumerator.Current, index++);
		}
	}

	public IEnumerator GetEnumerator()
	{
		return new DataViewSettingsEnumerator(_dataViewManager);
	}

	internal void Remove(DataTable table)
	{
		_list.Remove(table);
	}
}
