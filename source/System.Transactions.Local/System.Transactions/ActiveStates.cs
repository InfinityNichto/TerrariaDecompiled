namespace System.Transactions;

internal abstract class ActiveStates : TransactionState
{
	internal override TransactionStatus get_Status(InternalTransaction tx)
	{
		return TransactionStatus.Active;
	}

	internal override void AddOutcomeRegistrant(InternalTransaction tx, TransactionCompletedEventHandler transactionCompletedDelegate)
	{
		tx._transactionCompletedDelegate = (TransactionCompletedEventHandler)Delegate.Combine(tx._transactionCompletedDelegate, transactionCompletedDelegate);
	}
}
