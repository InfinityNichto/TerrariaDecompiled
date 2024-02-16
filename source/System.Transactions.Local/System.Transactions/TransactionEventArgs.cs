namespace System.Transactions;

public class TransactionEventArgs : EventArgs
{
	internal Transaction _transaction;

	public Transaction? Transaction => _transaction;
}
