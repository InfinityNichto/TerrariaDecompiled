namespace System.Transactions;

internal sealed class TransactionStateSPC : ActiveStates
{
	internal override void EnterState(InternalTransaction tx)
	{
		CommonEnterState(tx);
		if (tx._durableEnlistment != null)
		{
			tx._durableEnlistment.State.ChangeStateCommitting(tx._durableEnlistment);
		}
		else
		{
			TransactionState.TransactionStateCommitted.EnterState(tx);
		}
	}

	internal override void ChangeStateTransactionCommitted(InternalTransaction tx)
	{
		TransactionState.TransactionStateCommitted.EnterState(tx);
	}

	internal override void InDoubtFromEnlistment(InternalTransaction tx)
	{
		TransactionState.TransactionStateInDoubt.EnterState(tx);
	}

	internal override void ChangeStateTransactionAborted(InternalTransaction tx, Exception e)
	{
		if (tx._innerException == null)
		{
			tx._innerException = e;
		}
		TransactionState.TransactionStateAborted.EnterState(tx);
	}
}
