namespace System.Transactions;

internal sealed class TransactionStatePromotedIndoubt : TransactionStatePromotedEnded
{
	internal override void EnterState(InternalTransaction tx)
	{
		base.EnterState(tx);
		if (tx._phase1Volatiles.VolatileDemux != null)
		{
			tx._phase1Volatiles.VolatileDemux.BroadcastInDoubt(ref tx._phase1Volatiles);
		}
		if (tx._phase0Volatiles.VolatileDemux != null)
		{
			tx._phase0Volatiles.VolatileDemux.BroadcastInDoubt(ref tx._phase0Volatiles);
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

	internal override void RestartCommitIfNeeded(InternalTransaction tx)
	{
	}

	internal override void InDoubtFromEnlistment(InternalTransaction tx)
	{
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

	internal override void ChangeStatePromotedAborted(InternalTransaction tx)
	{
	}

	internal override void ChangeStatePromotedCommitted(InternalTransaction tx)
	{
	}
}
