namespace System.Transactions;

internal sealed class TransactionStateDelegated : TransactionStateDelegatedBase
{
	internal override void BeginCommit(InternalTransaction tx, bool asyncCommit, AsyncCallback asyncCallback, object asyncState)
	{
		tx._asyncCommit = asyncCommit;
		tx._asyncCallback = asyncCallback;
		tx._asyncState = asyncState;
		TransactionState.TransactionStateDelegatedCommitting.EnterState(tx);
	}

	internal override bool PromoteDurable(InternalTransaction tx)
	{
		tx._durableEnlistment.State.ChangeStateDelegated(tx._durableEnlistment);
		return true;
	}

	internal override void RestartCommitIfNeeded(InternalTransaction tx)
	{
		TransactionState.TransactionStateDelegatedP0Wave.EnterState(tx);
	}

	internal override void Rollback(InternalTransaction tx, Exception e)
	{
		if (tx._innerException == null)
		{
			tx._innerException = e;
		}
		TransactionState.TransactionStateDelegatedAborting.EnterState(tx);
	}
}
