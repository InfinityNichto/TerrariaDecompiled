namespace System.Transactions;

internal sealed class TransactionStateAborted : TransactionStateEnded
{
	internal override void EnterState(InternalTransaction tx)
	{
		base.EnterState(tx);
		CommonEnterState(tx);
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
		TransactionManager.TransactionTable.Remove(tx);
		TransactionsEtwProvider log = TransactionsEtwProvider.Log;
		if (log.IsEnabled())
		{
			log.TransactionTimeout(tx.TransactionTraceId);
		}
		tx.FireCompletion();
		if (tx._asyncCommit)
		{
			tx.SignalAsyncCompletion();
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
		throw CreateTransactionAbortedException(tx);
	}

	internal override void EndCommit(InternalTransaction tx)
	{
		throw CreateTransactionAbortedException(tx);
	}

	internal override void RestartCommitIfNeeded(InternalTransaction tx)
	{
	}

	internal override void Timeout(InternalTransaction tx)
	{
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

	internal override void ChangeStatePromotedAborted(InternalTransaction tx)
	{
	}

	internal override void ChangeStateAbortedDuringPromotion(InternalTransaction tx)
	{
	}

	internal override void CreateBlockingClone(InternalTransaction tx)
	{
		throw CreateTransactionAbortedException(tx);
	}

	internal override void CreateAbortingClone(InternalTransaction tx)
	{
		throw CreateTransactionAbortedException(tx);
	}

	internal override void CheckForFinishedTransaction(InternalTransaction tx)
	{
		throw CreateTransactionAbortedException(tx);
	}

	private TransactionException CreateTransactionAbortedException(InternalTransaction tx)
	{
		return TransactionAbortedException.Create(System.SR.TransactionAborted, tx._innerException, tx.DistributedTxId);
	}
}
