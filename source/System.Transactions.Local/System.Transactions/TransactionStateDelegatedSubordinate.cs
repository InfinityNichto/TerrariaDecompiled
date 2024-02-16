namespace System.Transactions;

internal sealed class TransactionStateDelegatedSubordinate : TransactionStateDelegatedBase
{
	internal override bool PromoteDurable(InternalTransaction tx)
	{
		return true;
	}

	internal override void Rollback(InternalTransaction tx, Exception e)
	{
		if (tx._innerException == null)
		{
			tx._innerException = e;
		}
		tx.PromotedTransaction.Rollback();
		TransactionState.TransactionStatePromotedAborted.EnterState(tx);
	}
}
