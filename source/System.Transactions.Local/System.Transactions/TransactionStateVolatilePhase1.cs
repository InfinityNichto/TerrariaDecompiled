namespace System.Transactions;

internal sealed class TransactionStateVolatilePhase1 : ActiveStates
{
	internal override void EnterState(InternalTransaction tx)
	{
		CommonEnterState(tx);
		tx._committableTransaction._complete = true;
		if (tx._phase1Volatiles._dependentClones != 0)
		{
			TransactionState.TransactionStateAborted.EnterState(tx);
		}
		else if (tx._phase1Volatiles._volatileEnlistmentCount == 1 && tx._durableEnlistment == null && tx._phase1Volatiles._volatileEnlistments[0].SinglePhaseNotification != null)
		{
			TransactionState.TransactionStateVolatileSPC.EnterState(tx);
		}
		else if (tx._phase1Volatiles._volatileEnlistmentCount > 0)
		{
			for (int i = 0; i < tx._phase1Volatiles._volatileEnlistmentCount; i++)
			{
				tx._phase1Volatiles._volatileEnlistments[i]._twoPhaseState.ChangeStatePreparing(tx._phase1Volatiles._volatileEnlistments[i]);
				if (!tx.State.ContinuePhase1Prepares())
				{
					break;
				}
			}
		}
		else
		{
			TransactionState.TransactionStateSPC.EnterState(tx);
		}
	}

	internal override void Rollback(InternalTransaction tx, Exception e)
	{
		ChangeStateTransactionAborted(tx, e);
	}

	internal override void ChangeStateTransactionAborted(InternalTransaction tx, Exception e)
	{
		if (tx._innerException == null)
		{
			tx._innerException = e;
		}
		TransactionState.TransactionStateAborted.EnterState(tx);
	}

	internal override void Phase1VolatilePrepareDone(InternalTransaction tx)
	{
		TransactionState.TransactionStateSPC.EnterState(tx);
	}

	internal override bool ContinuePhase1Prepares()
	{
		return true;
	}

	internal override void Timeout(InternalTransaction tx)
	{
		TransactionsEtwProvider log = TransactionsEtwProvider.Log;
		if (log.IsEnabled())
		{
			log.TransactionTimeout(tx.TransactionTraceId);
		}
		TimeoutException e = new TimeoutException(System.SR.TraceTransactionTimeout);
		Rollback(tx, e);
	}
}
