namespace System.Data;

public class MergeFailedEventArgs : EventArgs
{
	public DataTable? Table { get; }

	public string Conflict { get; }

	public MergeFailedEventArgs(DataTable? table, string conflict)
	{
		Table = table;
		Conflict = conflict;
	}
}
