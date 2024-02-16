namespace System.Transactions;

internal abstract class EnlistableStates : ActiveStates
{
	internal override Enlistment EnlistDurable(InternalTransaction tx, Guid resourceManagerIdentifier, IEnlistmentNotification enlistmentNotification, EnlistmentOptions enlistmentOptions, Transaction atomicTransaction)
	{
		tx.ThrowIfPromoterTypeIsNotMSDTC();
		tx._promoteState.EnterState(tx);
		return tx.State.EnlistDurable(tx, resourceManagerIdentifier, enlistmentNotification, enlistmentOptions, atomicTransaction);
	}

	internal override Enlistment EnlistDurable(InternalTransaction tx, Guid resourceManagerIdentifier, ISinglePhaseNotification enlistmentNotification, EnlistmentOptions enlistmentOptions, Transaction atomicTransaction)
	{
		tx.ThrowIfPromoterTypeIsNotMSDTC();
		if (tx._durableEnlistment != null || (enlistmentOptions & EnlistmentOptions.EnlistDuringPrepareRequired) != 0)
		{
			tx._promoteState.EnterState(tx);
			return tx.State.EnlistDurable(tx, resourceManagerIdentifier, enlistmentNotification, enlistmentOptions, atomicTransaction);
		}
		Enlistment enlistment = new Enlistment(resourceManagerIdentifier, tx, enlistmentNotification, enlistmentNotification, atomicTransaction);
		tx._durableEnlistment = enlistment.InternalEnlistment;
		DurableEnlistmentState.DurableEnlistmentActive.EnterState(tx._durableEnlistment);
		TransactionsEtwProvider log = TransactionsEtwProvider.Log;
		if (log.IsEnabled())
		{
			log.TransactionstateEnlist(tx._durableEnlistment.EnlistmentTraceId, EnlistmentType.Durable, EnlistmentOptions.None);
		}
		return enlistment;
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

	internal override void CompleteBlockingClone(InternalTransaction tx)
	{
		tx._phase0Volatiles._dependentClones--;
		if (tx._phase0Volatiles._preparedVolatileEnlistments == tx._phase0VolatileWaveCount + tx._phase0Volatiles._dependentClones)
		{
			tx.State.Phase0VolatilePrepareDone(tx);
		}
	}

	internal override void CompleteAbortingClone(InternalTransaction tx)
	{
		tx._phase1Volatiles._dependentClones--;
	}

	internal override void CreateBlockingClone(InternalTransaction tx)
	{
		tx._phase0Volatiles._dependentClones++;
	}

	internal override void CreateAbortingClone(InternalTransaction tx)
	{
		tx._phase1Volatiles._dependentClones++;
	}

	internal override void Promote(InternalTransaction tx)
	{
		tx._promoteState.EnterState(tx);
		tx.State.CheckForFinishedTransaction(tx);
	}

	internal override byte[] PromotedToken(InternalTransaction tx)
	{
		if (tx.promotedToken == null)
		{
			tx._promoteState.EnterState(tx);
			tx.State.CheckForFinishedTransaction(tx);
		}
		return tx.promotedToken;
	}
}
