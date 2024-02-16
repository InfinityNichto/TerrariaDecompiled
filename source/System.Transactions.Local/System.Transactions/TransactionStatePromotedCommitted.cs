namespace System.Transactions;

internal sealed class TransactionStatePromotedCommitted : TransactionStatePromotedEnded
{
	internal override void EnterState(InternalTransaction tx)
	{
		base.EnterState(tx);
		if (tx._phase1Volatiles.VolatileDemux != null)
		{
			tx._phase1Volatiles.VolatileDemux.BroadcastCommitted(ref tx._phase1Volatiles);
		}
		if (tx._phase0Volatiles.VolatileDemux != null)
		{
			tx._phase0Volatiles.VolatileDemux.BroadcastCommitted(ref tx._phase0Volatiles);
		}
		tx.FireCompletion();
		TransactionsEtwProvider log = TransactionsEtwProvider.Log;
		if (log.IsEnabled())
		{
			log.TransactionCommitted(tx.TransactionTraceId);
		}
	}

	internal override TransactionStatus get_Status(InternalTransaction tx)
	{
		return TransactionStatus.Committed;
	}

	internal override void ChangeStatePromotedCommitted(InternalTransaction tx)
	{
	}

	protected override void PromotedTransactionOutcome(InternalTransaction tx)
	{
	}

	internal override void InDoubtFromEnlistment(InternalTransaction tx)
	{
	}
}
