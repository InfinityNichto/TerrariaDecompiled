using System.Collections.Generic;
using System.ComponentModel;

namespace System.Data;

internal sealed class DataViewListener
{
	private readonly WeakReference _dvWeak;

	private DataTable _table;

	private Index _index;

	internal readonly int _objectID;

	internal DataViewListener(DataView dv)
	{
		_objectID = dv.ObjectID;
		_dvWeak = new WeakReference(dv);
	}

	private void ChildRelationCollectionChanged(object sender, CollectionChangeEventArgs e)
	{
		DataView dataView = (DataView)_dvWeak.Target;
		if (dataView != null)
		{
			dataView.ChildRelationCollectionChanged(sender, e);
		}
		else
		{
			CleanUp(updateListeners: true);
		}
	}

	private void ParentRelationCollectionChanged(object sender, CollectionChangeEventArgs e)
	{
		DataView dataView = (DataView)_dvWeak.Target;
		if (dataView != null)
		{
			dataView.ParentRelationCollectionChanged(sender, e);
		}
		else
		{
			CleanUp(updateListeners: true);
		}
	}

	private void ColumnCollectionChanged(object sender, CollectionChangeEventArgs e)
	{
		DataView dataView = (DataView)_dvWeak.Target;
		if (dataView != null)
		{
			dataView.ColumnCollectionChangedInternal(sender, e);
		}
		else
		{
			CleanUp(updateListeners: true);
		}
	}

	internal void MaintainDataView(ListChangedType changedType, DataRow row, bool trackAddRemove)
	{
		DataView dataView = (DataView)_dvWeak.Target;
		if (dataView != null)
		{
			dataView.MaintainDataView(changedType, row, trackAddRemove);
		}
		else
		{
			CleanUp(updateListeners: true);
		}
	}

	internal void IndexListChanged(ListChangedEventArgs e)
	{
		DataView dataView = (DataView)_dvWeak.Target;
		if (dataView != null)
		{
			dataView.IndexListChangedInternal(e);
		}
		else
		{
			CleanUp(updateListeners: true);
		}
	}

	internal void RegisterMetaDataEvents(DataTable table)
	{
		_table = table;
		if (table != null)
		{
			RegisterListener(table);
			CollectionChangeEventHandler value = ColumnCollectionChanged;
			table.Columns.ColumnPropertyChanged += value;
			table.Columns.CollectionChanged += value;
			CollectionChangeEventHandler value2 = ChildRelationCollectionChanged;
			((DataRelationCollection.DataTableRelationCollection)table.ChildRelations).RelationPropertyChanged += value2;
			table.ChildRelations.CollectionChanged += value2;
			CollectionChangeEventHandler value3 = ParentRelationCollectionChanged;
			((DataRelationCollection.DataTableRelationCollection)table.ParentRelations).RelationPropertyChanged += value3;
			table.ParentRelations.CollectionChanged += value3;
		}
	}

	internal void UnregisterMetaDataEvents()
	{
		UnregisterMetaDataEvents(updateListeners: true);
	}

	private void UnregisterMetaDataEvents(bool updateListeners)
	{
		DataTable table = _table;
		_table = null;
		if (table == null)
		{
			return;
		}
		CollectionChangeEventHandler value = ColumnCollectionChanged;
		table.Columns.ColumnPropertyChanged -= value;
		table.Columns.CollectionChanged -= value;
		CollectionChangeEventHandler value2 = ChildRelationCollectionChanged;
		((DataRelationCollection.DataTableRelationCollection)table.ChildRelations).RelationPropertyChanged -= value2;
		table.ChildRelations.CollectionChanged -= value2;
		CollectionChangeEventHandler value3 = ParentRelationCollectionChanged;
		((DataRelationCollection.DataTableRelationCollection)table.ParentRelations).RelationPropertyChanged -= value3;
		table.ParentRelations.CollectionChanged -= value3;
		if (updateListeners)
		{
			List<DataViewListener> listeners = table.GetListeners();
			lock (listeners)
			{
				listeners.Remove(this);
			}
		}
	}

	internal void RegisterListChangedEvent(Index index)
	{
		_index = index;
		if (index != null)
		{
			lock (index)
			{
				index.AddRef();
				index.ListChangedAdd(this);
			}
		}
	}

	internal void UnregisterListChangedEvent()
	{
		Index index = _index;
		_index = null;
		if (index == null)
		{
			return;
		}
		lock (index)
		{
			index.ListChangedRemove(this);
			if (index.RemoveRef() <= 1)
			{
				index.RemoveRef();
			}
		}
	}

	private void CleanUp(bool updateListeners)
	{
		UnregisterMetaDataEvents(updateListeners);
		UnregisterListChangedEvent();
	}

	private void RegisterListener(DataTable table)
	{
		List<DataViewListener> listeners = table.GetListeners();
		lock (listeners)
		{
			int num = listeners.Count - 1;
			while (0 <= num)
			{
				DataViewListener dataViewListener = listeners[num];
				if (!dataViewListener._dvWeak.IsAlive)
				{
					listeners.RemoveAt(num);
					dataViewListener.CleanUp(updateListeners: false);
				}
				num--;
			}
			listeners.Add(this);
		}
	}
}
