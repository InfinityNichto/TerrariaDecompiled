namespace System.Transactions;

internal sealed class TransactionStatePromotedNonMSDTCVolatilePhase1 : TransactionStatePromotedNonMSDTCBase
{
	internal override void EnterState(InternalTransaction tx)
	{
		CommonEnterState(tx);
		tx._committableTransaction._complete = true;
		if (tx._phase1Volatiles._dependentClones != 0)
		{
			ChangeStateTransactionAborted(tx, null);
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
			TransactionState.TransactionStatePromotedNonMSDTCSinglePhaseCommit.EnterState(tx);
		}
	}

	internal override void BeginCommit(InternalTransaction tx, bool asyncCommit, AsyncCallback asyncCallback, object asyncState)
	{
		throw TransactionException.CreateTransactionStateException(tx._innerException, tx.DistributedTxId);
	}

	internal override void Rollback(InternalTransaction tx, Exception e)
	{
		ChangeStateTransactionAborted(tx, e);
	}

	internal override void Phase1VolatilePrepareDone(InternalTransaction tx)
	{
		TransactionState.TransactionStatePromotedNonMSDTCSinglePhaseCommit.EnterState(tx);
	}

	internal override bool ContinuePhase1Prepares()
	{
		return true;
	}

	internal override Enlistment EnlistVolatile(InternalTransaction tx, IEnlistmentNotification enlistmentNotification, EnlistmentOptions enlistmentOptions, Transaction atomicTransaction)
	{
		throw TransactionException.Create(System.SR.TooLate, tx?.DistributedTxId ?? Guid.Empty);
	}

	internal override Enlistment EnlistVolatile(InternalTransaction tx, ISinglePhaseNotification enlistmentNotification, EnlistmentOptions enlistmentOptions, Transaction atomicTransaction)
	{
		throw TransactionException.Create(System.SR.TooLate, tx?.DistributedTxId ?? Guid.Empty);
	}

	internal override bool EnlistPromotableSinglePhase(InternalTransaction tx, IPromotableSinglePhaseNotification promotableSinglePhaseNotification, Transaction atomicTransaction, Guid promoterType)
	{
		throw TransactionException.Create(System.SR.TooLate, tx?.DistributedTxId ?? Guid.Empty);
	}

	internal override void CreateBlockingClone(InternalTransaction tx)
	{
		throw TransactionException.Create(System.SR.TooLate, tx?.DistributedTxId ?? Guid.Empty);
	}

	internal override void CreateAbortingClone(InternalTransaction tx)
	{
		throw TransactionException.Create(System.SR.TooLate, tx?.DistributedTxId ?? Guid.Empty);
	}
}
