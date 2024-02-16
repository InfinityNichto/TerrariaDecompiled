namespace System.Data;

public class DataColumnChangeEventArgs : EventArgs
{
	private DataColumn _column;

	public DataColumn? Column => _column;

	public DataRow Row { get; }

	public object? ProposedValue { get; set; }

	internal DataColumnChangeEventArgs(DataRow row)
	{
		Row = row;
	}

	public DataColumnChangeEventArgs(DataRow row, DataColumn? column, object? value)
	{
		Row = row;
		_column = column;
		ProposedValue = value;
	}

	internal void InitializeColumnChangeEvent(DataColumn column, object value)
	{
		_column = column;
		ProposedValue = value;
	}
}
