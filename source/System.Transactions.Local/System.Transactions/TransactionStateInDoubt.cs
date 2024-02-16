namespace System.Transactions;

internal sealed class TransactionStateInDoubt : TransactionStateEnded
{
	internal override void EnterState(InternalTransaction tx)
	{
		base.EnterState(tx);
		CommonEnterState(tx);
		for (int i = 0; i < tx._phase0Volatiles._volatileEnlistmentCount; i++)
		{
			tx._phase0Volatiles._volatileEnlistments[i]._twoPhaseState.InternalIndoubt(tx._phase0Volatiles._volatileEnlistments[i]);
		}
		for (int j = 0; j < tx._phase1Volatiles._volatileEnlistmentCount; j++)
		{
			tx._phase1Volatiles._volatileEnlistments[j]._twoPhaseState.InternalIndoubt(tx._phase1Volatiles._volatileEnlistments[j]);
		}
		TransactionManager.TransactionTable.Remove(tx);
		TransactionsEtwProvider log = TransactionsEtwProvider.Log;
		if (log.IsEnabled())
		{
			log.TransactionInDoubt(tx.TransactionTraceId);
		}
		tx.FireCompletion();
		if (tx._asyncCommit)
		{
			tx.SignalAsyncCompletion();
		}
	}

	internal override TransactionStatus get_Status(InternalTransaction tx)
	{
		return TransactionStatus.InDoubt;
	}

	internal override void Rollback(InternalTransaction tx, Exception e)
	{
		throw TransactionException.CreateTransactionStateException(tx._innerException, tx.DistributedTxId);
	}

	internal override void EndCommit(InternalTransaction tx)
	{
		throw TransactionInDoubtException.Create(TraceSourceType.TraceSourceBase, System.SR.TransactionIndoubt, tx._innerException, tx.DistributedTxId);
	}

	internal override void CheckForFinishedTransaction(InternalTransaction tx)
	{
		throw TransactionInDoubtException.Create(TraceSourceType.TraceSourceBase, System.SR.TransactionIndoubt, tx._innerException, tx.DistributedTxId);
	}
}
