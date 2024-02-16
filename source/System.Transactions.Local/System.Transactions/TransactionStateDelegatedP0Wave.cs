namespace System.Transactions;

internal sealed class TransactionStateDelegatedP0Wave : TransactionStatePromotedP0Wave
{
	internal override void Phase0VolatilePrepareDone(InternalTransaction tx)
	{
		TransactionState.TransactionStateDelegatedCommitting.EnterState(tx);
	}
}
