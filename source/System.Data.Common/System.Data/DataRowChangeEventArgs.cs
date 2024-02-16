namespace System.Data;

public class DataRowChangeEventArgs : EventArgs
{
	public DataRow Row { get; }

	public DataRowAction Action { get; }

	public DataRowChangeEventArgs(DataRow row, DataRowAction action)
	{
		Row = row;
		Action = action;
	}
}
