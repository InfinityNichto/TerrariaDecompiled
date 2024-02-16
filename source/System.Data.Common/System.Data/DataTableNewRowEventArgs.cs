namespace System.Data;

public sealed class DataTableNewRowEventArgs : EventArgs
{
	public DataRow Row { get; }

	public DataTableNewRowEventArgs(DataRow dataRow)
	{
		Row = dataRow;
	}
}
