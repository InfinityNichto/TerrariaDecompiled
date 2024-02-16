namespace System.Transactions;

internal sealed class TransactionStateVolatileSPC : ActiveStates
{
	internal override void EnterState(InternalTransaction tx)
	{
		CommonEnterState(tx);
		tx._phase1Volatiles._volatileEnlistments[0]._twoPhaseState.ChangeStateSinglePhaseCommit(tx._phase1Volatiles._volatileEnlistments[0]);
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
