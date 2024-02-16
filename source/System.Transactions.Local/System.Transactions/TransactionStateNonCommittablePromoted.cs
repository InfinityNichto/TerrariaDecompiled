namespace System.Transactions;

internal sealed class TransactionStateNonCommittablePromoted : TransactionStatePromotedBase
{
	internal override void EnterState(InternalTransaction tx)
	{
		CommonEnterState(tx);
		tx.PromotedTransaction.RealTransaction.InternalTransaction = tx;
	}
}
