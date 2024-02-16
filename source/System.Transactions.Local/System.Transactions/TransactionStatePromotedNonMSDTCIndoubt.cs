namespace System.Transactions;

internal sealed class TransactionStatePromotedNonMSDTCIndoubt : TransactionStatePromotedNonMSDTCEnded
{
	internal override void EnterState(InternalTransaction tx)
	{
		base.EnterState(tx);
		for (int i = 0; i < tx._phase0Volatiles._volatileEnlistmentCount; i++)
		{
			tx._phase0Volatiles._volatileEnlistments[i]._twoPhaseState.InternalIndoubt(tx._phase0Volatiles._volatileEnlistments[i]);
		}
		for (int j = 0; j < tx._phase1Volatiles._volatileEnlistmentCount; j++)
		{
			tx._phase1Volatiles._volatileEnlistments[j]._twoPhaseState.InternalIndoubt(tx._phase1Volatiles._volatileEnlistments[j]);
		}
		tx.FireCompletion();
		TransactionsEtwProvider log = TransactionsEtwProvider.Log;
		if (log.IsEnabled())
		{
			log.TransactionInDoubt(tx.TransactionTraceId);
		}
	}

	internal override TransactionStatus get_Status(InternalTransaction tx)
	{
		return TransactionStatus.InDoubt;
	}

	protected override void PromotedTransactionOutcome(InternalTransaction tx)
	{
		if (tx._innerException == null && tx.PromotedTransaction != null)
		{
			tx._innerException = tx.PromotedTransaction.InnerException;
		}
		throw TransactionInDoubtException.Create(TraceSourceType.TraceSourceBase, System.SR.TransactionIndoubt, tx._innerException, tx.DistributedTxId);
	}

	internal override void CheckForFinishedTransaction(InternalTransaction tx)
	{
		throw TransactionInDoubtException.Create(TraceSourceType.TraceSourceBase, System.SR.TransactionIndoubt, tx._innerException, tx.DistributedTxId);
	}

	internal override void CreateBlockingClone(InternalTransaction tx)
	{
		throw TransactionException.Create(System.SR.TransactionAborted, tx._innerException, tx.DistributedTxId);
	}

	internal override void CreateAbortingClone(InternalTransaction tx)
	{
		throw TransactionException.Create(System.SR.TransactionAborted, tx._innerException, tx.DistributedTxId);
	}
}
