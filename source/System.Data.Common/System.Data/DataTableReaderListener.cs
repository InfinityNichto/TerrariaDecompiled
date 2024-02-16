using System.ComponentModel;

namespace System.Data;

internal sealed class DataTableReaderListener
{
	private DataTable _currentDataTable;

	private bool _isSubscribed;

	private readonly WeakReference _readerWeak;

	internal DataTableReaderListener(DataTableReader reader)
	{
		if (reader == null)
		{
			throw ExceptionBuilder.ArgumentNull("DataTableReader");
		}
		if (_currentDataTable != null)
		{
			UnSubscribeEvents();
		}
		_readerWeak = new WeakReference(reader);
		_currentDataTable = reader.CurrentDataTable;
		if (_currentDataTable != null)
		{
			SubscribeEvents();
		}
	}

	internal void CleanUp()
	{
		UnSubscribeEvents();
	}

	internal void UpdataTable(DataTable datatable)
	{
		if (datatable == null)
		{
			throw ExceptionBuilder.ArgumentNull("DataTable");
		}
		UnSubscribeEvents();
		_currentDataTable = datatable;
		SubscribeEvents();
	}

	private void SubscribeEvents()
	{
		if (_currentDataTable != null && !_isSubscribed)
		{
			_currentDataTable.Columns.ColumnPropertyChanged += SchemaChanged;
			_currentDataTable.Columns.CollectionChanged += SchemaChanged;
			_currentDataTable.RowChanged += DataChanged;
			_currentDataTable.RowDeleted += DataChanged;
			_currentDataTable.TableCleared += DataTableCleared;
			_isSubscribed = true;
		}
	}

	private void UnSubscribeEvents()
	{
		if (_currentDataTable != null && _isSubscribed)
		{
			_currentDataTable.Columns.ColumnPropertyChanged -= SchemaChanged;
			_currentDataTable.Columns.CollectionChanged -= SchemaChanged;
			_currentDataTable.RowChanged -= DataChanged;
			_currentDataTable.RowDeleted -= DataChanged;
			_currentDataTable.TableCleared -= DataTableCleared;
			_isSubscribed = false;
		}
	}

	private void DataTableCleared(object sender, DataTableClearEventArgs e)
	{
		DataTableReader dataTableReader = (DataTableReader)_readerWeak.Target;
		if (dataTableReader != null)
		{
			dataTableReader.DataTableCleared();
		}
		else
		{
			UnSubscribeEvents();
		}
	}

	private void SchemaChanged(object sender, CollectionChangeEventArgs e)
	{
		DataTableReader dataTableReader = (DataTableReader)_readerWeak.Target;
		if (dataTableReader != null)
		{
			dataTableReader.SchemaChanged();
		}
		else
		{
			UnSubscribeEvents();
		}
	}

	private void DataChanged(object sender, DataRowChangeEventArgs args)
	{
		DataTableReader dataTableReader = (DataTableReader)_readerWeak.Target;
		if (dataTableReader != null)
		{
			dataTableReader.DataChanged(args);
		}
		else
		{
			UnSubscribeEvents();
		}
	}
}
