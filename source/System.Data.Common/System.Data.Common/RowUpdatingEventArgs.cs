namespace System.Data.Common;

public class RowUpdatingEventArgs : EventArgs
{
	private IDbCommand _command;

	private readonly StatementType _statementType;

	private readonly DataTableMapping _tableMapping;

	private Exception _errors;

	private readonly DataRow _dataRow;

	private UpdateStatus _status;

	protected virtual IDbCommand? BaseCommand
	{
		get
		{
			return _command;
		}
		set
		{
			_command = value;
		}
	}

	public IDbCommand? Command
	{
		get
		{
			return BaseCommand;
		}
		set
		{
			BaseCommand = value;
		}
	}

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

	public DataRow Row => _dataRow;

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

	public RowUpdatingEventArgs(DataRow dataRow, IDbCommand? command, StatementType statementType, DataTableMapping tableMapping)
	{
		ADP.CheckArgumentNull(dataRow, "dataRow");
		ADP.CheckArgumentNull(tableMapping, "tableMapping");
		switch (statementType)
		{
		case StatementType.Batch:
			throw ADP.NotSupportedStatementType(statementType, "RowUpdatingEventArgs");
		default:
			throw ADP.InvalidStatementType(statementType);
		case StatementType.Select:
		case StatementType.Insert:
		case StatementType.Update:
		case StatementType.Delete:
			_dataRow = dataRow;
			_command = command;
			_statementType = statementType;
			_tableMapping = tableMapping;
			break;
		}
	}
}
