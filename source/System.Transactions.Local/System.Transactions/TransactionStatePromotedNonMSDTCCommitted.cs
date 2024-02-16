namespace System.Transactions;

internal sealed class TransactionStatePromotedNonMSDTCCommitted : TransactionStatePromotedNonMSDTCEnded
{
	internal override void EnterState(InternalTransaction tx)
	{
		base.EnterState(tx);
		for (int i = 0; i < tx._phase0Volatiles._volatileEnlistmentCount; i++)
		{
			tx._phase0Volatiles._volatileEnlistments[i]._twoPhaseState.InternalCommitted(tx._phase0Volatiles._volatileEnlistments[i]);
		}
		for (int j = 0; j < tx._phase1Volatiles._volatileEnlistmentCount; j++)
		{
			tx._phase1Volatiles._volatileEnlistments[j]._twoPhaseState.InternalCommitted(tx._phase1Volatiles._volatileEnlistments[j]);
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

	protected override void PromotedTransactionOutcome(InternalTransaction tx)
	{
	}
}
