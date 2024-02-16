namespace System.Transactions;

internal class TransactionStatePromotedP0Wave : TransactionStatePromotedBase
{
	internal override void EnterState(InternalTransaction tx)
	{
		CommonEnterState(tx);
	}

	internal override void BeginCommit(InternalTransaction tx, bool asyncCommit, AsyncCallback asyncCallback, object asyncState)
	{
		throw TransactionException.CreateTransactionStateException(tx._innerException, tx.DistributedTxId);
	}

	internal override void Phase0VolatilePrepareDone(InternalTransaction tx)
	{
		try
		{
			TransactionState.TransactionStatePromotedCommitting.EnterState(tx);
		}
		catch (TransactionException ex)
		{
			if (tx._innerException == null)
			{
				tx._innerException = ex;
			}
			TransactionsEtwProvider log = TransactionsEtwProvider.Log;
			if (log.IsEnabled())
			{
				log.ExceptionConsumed(ex);
			}
		}
	}

	internal override bool ContinuePhase0Prepares()
	{
		return true;
	}

	internal override void ChangeStateTransactionAborted(InternalTransaction tx, Exception e)
	{
		if (tx._innerException == null)
		{
			tx._innerException = e;
		}
		TransactionState.TransactionStatePromotedP0Aborting.EnterState(tx);
	}
}
