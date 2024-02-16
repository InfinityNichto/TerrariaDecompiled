namespace System.ComponentModel.Design;

public class DesignerTransactionCloseEventArgs : EventArgs
{
	public bool TransactionCommitted { get; }

	public bool LastTransaction { get; }

	[Obsolete("This constructor has been deprecated. Use DesignerTransactionCloseEventArgs(bool, bool) instead.")]
	public DesignerTransactionCloseEventArgs(bool commit)
		: this(commit, lastTransaction: true)
	{
	}

	public DesignerTransactionCloseEventArgs(bool commit, bool lastTransaction)
	{
		TransactionCommitted = commit;
		LastTransaction = lastTransaction;
	}
}
