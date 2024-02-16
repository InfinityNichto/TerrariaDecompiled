namespace System.Transactions;

internal class TransactionStatePromotedAborted : TransactionStatePromotedEnded
{
	internal override void EnterState(InternalTransaction tx)
	{
		base.EnterState(tx);
		if (tx._phase1Volatiles.VolatileDemux != null)
		{
			tx._phase1Volatiles.VolatileDemux.BroadcastRollback(ref tx._phase1Volatiles);
		}
		if (tx._phase0Volatiles.VolatileDemux != null)
		{
			tx._phase0Volatiles.VolatileDemux.BroadcastRollback(ref tx._phase0Volatiles);
		}
		tx.FireCompletion();
		TransactionsEtwProvider log = TransactionsEtwProvider.Log;
		if (log.IsEnabled())
		{
			log.TransactionAborted(tx.TransactionTraceId);
		}
	}

	internal override TransactionStatus get_Status(InternalTransaction tx)
	{
		return TransactionStatus.Aborted;
	}

	internal override void Rollback(InternalTransaction tx, Exception e)
	{
	}

	internal override void BeginCommit(InternalTransaction tx, bool asyncCommit, AsyncCallback asyncCallback, object asyncState)
	{
		throw TransactionAbortedException.Create(System.SR.TransactionAborted, tx._innerException, tx.DistributedTxId);
	}

	internal override void CreateBlockingClone(InternalTransaction tx)
	{
		throw TransactionAbortedException.Create(System.SR.TransactionAborted, tx._innerException, tx.DistributedTxId);
	}

	internal override void CreateAbortingClone(InternalTransaction tx)
	{
		throw TransactionAbortedException.Create(System.SR.TransactionAborted, tx._innerException, tx.DistributedTxId);
	}

	internal override void RestartCommitIfNeeded(InternalTransaction tx)
	{
	}

	internal override void Phase0VolatilePrepareDone(InternalTransaction tx)
	{
	}

	internal override void Phase1VolatilePrepareDone(InternalTransaction tx)
	{
	}

	internal override void ChangeStatePromotedAborted(InternalTransaction tx)
	{
	}

	internal override void ChangeStateTransactionAborted(InternalTransaction tx, Exception e)
	{
	}

	protected override void PromotedTransactionOutcome(InternalTransaction tx)
	{
		if (tx._innerException == null && tx.PromotedTransaction != null)
		{
			tx._innerException = tx.PromotedTransaction.InnerException;
		}
		throw TransactionAbortedException.Create(System.SR.TransactionAborted, tx._innerException, tx.DistributedTxId);
	}

	internal override void CheckForFinishedTransaction(InternalTransaction tx)
	{
		throw new TransactionAbortedException(tx._innerException, tx.DistributedTxId);
	}

	internal override void InDoubtFromEnlistment(InternalTransaction tx)
	{
	}
}
