namespace System.Data.Common;

public class RowUpdatedEventArgs : EventArgs
{
	private readonly IDbCommand _command;

	private StatementType _statementType;

	private readonly DataTableMapping _tableMapping;

	private Exception _errors;

	private DataRow _dataRow;

	private DataRow[] _dataRows;

	private UpdateStatus _status;

	private int _recordsAffected;

	public IDbCommand? Command => _command;

	public Exception? Errors
	{
		get
		{
			return _errors;
		}
		set
		{
			_errors = value;
		}
	}

	public int RecordsAffected => _recordsAffected;

	public DataRow Row => _dataRow;

	internal DataRow[]? Rows => _dataRows;

	public int RowCount
	{
		get
		{
			DataRow[] dataRows = _dataRows;
			if (dataRows == null)
			{
				if (_dataRow == null)
				{
					return 0;
				}
				return 1;
			}
			return dataRows.Length;
		}
	}

	public StatementType StatementType => _statementType;

	public UpdateStatus Status
	{
		get
		{
			return _status;
		}
		set
		{
			if ((uint)value <= 3u)
			{
				_status = value;
				return;
			}
			throw ADP.InvalidUpdateStatus(value);
		}
	}

	public DataTableMapping TableMapping => _tableMapping;

	public RowUpdatedEventArgs(DataRow dataRow, IDbCommand? command, StatementType statementType, DataTableMapping tableMapping)
	{
		if ((uint)statementType > 4u)
		{
			throw ADP.InvalidStatementType(statementType);
		}
		_dataRow = dataRow;
		_command = command;
		_statementType = statementType;
		_tableMapping = tableMapping;
	}

	internal void AdapterInit(DataRow[] dataRows)
	{
		_statementType = StatementType.Batch;
		_dataRows = dataRows;
		if (dataRows != null && 1 == dataRows.Length)
		{
			_dataRow = dataRows[0];
		}
	}

	internal void AdapterInit(int recordsAffected)
	{
		_recordsAffected = recordsAffected;
	}

	public void CopyToRows(DataRow[] array)
	{
		CopyToRows(array, 0);
	}

	public void CopyToRows(DataRow[] array, int arrayIndex)
	{
		DataRow[] dataRows = _dataRows;
		if (dataRows != null)
		{
			dataRows.CopyTo(array, arrayIndex);
			return;
		}
		if (array == null)
		{
			throw ADP.ArgumentNull("array");
		}
		array[arrayIndex] = Row;
	}
}
