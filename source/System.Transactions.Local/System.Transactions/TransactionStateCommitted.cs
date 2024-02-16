namespace System.Transactions;

internal sealed class TransactionStateCommitted : TransactionStateEnded
{
	internal override void EnterState(InternalTransaction tx)
	{
		base.EnterState(tx);
		CommonEnterState(tx);
		for (int i = 0; i < tx._phase0Volatiles._volatileEnlistmentCount; i++)
		{
			tx._phase0Volatiles._volatileEnlistments[i]._twoPhaseState.InternalCommitted(tx._phase0Volatiles._volatileEnlistments[i]);
		}
		for (int j = 0; j < tx._phase1Volatiles._volatileEnlistmentCount; j++)
		{
			tx._phase1Volatiles._volatileEnlistments[j]._twoPhaseState.InternalCommitted(tx._phase1Volatiles._volatileEnlistments[j]);
		}
		TransactionManager.TransactionTable.Remove(tx);
		TransactionsEtwProvider log = TransactionsEtwProvider.Log;
		if (log.IsEnabled())
		{
			log.TransactionCommitted(tx.TransactionTraceId);
		}
		tx.FireCompletion();
		if (tx._asyncCommit)
		{
			tx.SignalAsyncCompletion();
		}
	}

	internal override TransactionStatus get_Status(InternalTransaction tx)
	{
		return TransactionStatus.Committed;
	}

	internal override void Rollback(InternalTransaction tx, Exception e)
	{
		throw TransactionException.CreateTransactionStateException(tx._innerException, tx.DistributedTxId);
	}

	internal override void EndCommit(InternalTransaction tx)
	{
	}
}
