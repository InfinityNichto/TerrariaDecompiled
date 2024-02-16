namespace System.Transactions;

internal sealed class TransactionStatePromotedNonMSDTCAborted : TransactionStatePromotedNonMSDTCEnded
{
	internal override void EnterState(InternalTransaction tx)
	{
		base.EnterState(tx);
		for (int i = 0; i < tx._phase0Volatiles._volatileEnlistmentCount; i++)
		{
			tx._phase0Volatiles._volatileEnlistments[i]._twoPhaseState.InternalAborted(tx._phase0Volatiles._volatileEnlistments[i]);
		}
		for (int j = 0; j < tx._phase1Volatiles._volatileEnlistmentCount; j++)
		{
			tx._phase1Volatiles._volatileEnlistments[j]._twoPhaseState.InternalAborted(tx._phase1Volatiles._volatileEnlistments[j]);
		}
		if (tx._durableEnlistment != null)
		{
			tx._durableEnlistment.State.InternalAborted(tx._durableEnlistment);
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

	internal override void Phase0VolatilePrepareDone(InternalTransaction tx)
	{
	}

	internal override void Phase1VolatilePrepareDone(InternalTransaction tx)
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
}
