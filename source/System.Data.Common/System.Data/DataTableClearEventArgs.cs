namespace System.Data;

public sealed class DataTableClearEventArgs : EventArgs
{
	public DataTable Table { get; }

	public string TableName => Table.TableName;

	public string TableNamespace => Table.Namespace;

	public DataTableClearEventArgs(DataTable dataTable)
	{
		Table = dataTable;
	}
}
