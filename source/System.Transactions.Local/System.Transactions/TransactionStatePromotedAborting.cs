namespace System.Transactions;

internal abstract class TransactionStatePromotedAborting : TransactionStatePromotedBase
{
	internal override void EnterState(InternalTransaction tx)
	{
		CommonEnterState(tx);
	}

	internal override TransactionStatus get_Status(InternalTransaction tx)
	{
		return TransactionStatus.Aborted;
	}

	internal override void BeginCommit(InternalTransaction tx, bool asyncCommit, AsyncCallback asyncCallback, object asyncState)
	{
		throw TransactionException.CreateTransactionStateException(tx._innerException, tx.DistributedTxId);
	}

	internal override void CreateBlockingClone(InternalTransaction tx)
	{
		throw TransactionException.CreateTransactionStateException(tx._innerException, tx.DistributedTxId);
	}

	internal override void CreateAbortingClone(InternalTransaction tx)
	{
		throw TransactionException.CreateTransactionStateException(tx._innerException, tx.DistributedTxId);
	}

	internal override void ChangeStatePromotedAborted(InternalTransaction tx)
	{
		TransactionState.TransactionStatePromotedAborted.EnterState(tx);
	}

	internal override void ChangeStateTransactionAborted(InternalTransaction tx, Exception e)
	{
	}

	internal override void RestartCommitIfNeeded(InternalTransaction tx)
	{
	}
}
