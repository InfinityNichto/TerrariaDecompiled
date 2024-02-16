namespace System.Data;

public sealed class StatementCompletedEventArgs : EventArgs
{
	public int RecordCount { get; }

	public StatementCompletedEventArgs(int recordCount)
	{
		RecordCount = recordCount;
	}
}
