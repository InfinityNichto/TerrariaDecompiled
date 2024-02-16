namespace System.Transactions;

internal class TransactionStateActive : EnlistableStates
{
	internal override void EnterState(InternalTransaction tx)
	{
		CommonEnterState(tx);
	}

	internal override void BeginCommit(InternalTransaction tx, bool asyncCommit, AsyncCallback asyncCallback, object asyncState)
	{
		tx._asyncCommit = asyncCommit;
		tx._asyncCallback = asyncCallback;
		tx._asyncState = asyncState;
		TransactionState.TransactionStatePhase0.EnterState(tx);
	}

	internal override void Rollback(InternalTransaction tx, Exception e)
	{
		if (tx._innerException == null)
		{
			tx._innerException = e;
		}
		TransactionState.TransactionStateAborted.EnterState(tx);
	}

	internal override Enlistment EnlistVolatile(InternalTransaction tx, IEnlistmentNotification enlistmentNotification, EnlistmentOptions enlistmentOptions, Transaction atomicTransaction)
	{
		Enlistment enlistment = new Enlistment(tx, enlistmentNotification, null, atomicTransaction, enlistmentOptions);
		if ((enlistmentOptions & EnlistmentOptions.EnlistDuringPrepareRequired) != 0)
		{
			AddVolatileEnlistment(ref tx._phase0Volatiles, enlistment);
		}
		else
		{
			AddVolatileEnlistment(ref tx._phase1Volatiles, enlistment);
		}
		TransactionsEtwProvider log = TransactionsEtwProvider.Log;
		if (log.IsEnabled())
		{
			log.TransactionstateEnlist(enlistment.InternalEnlistment.EnlistmentTraceId, EnlistmentType.Volatile, enlistmentOptions);
		}
		return enlistment;
	}

	internal override Enlistment EnlistVolatile(InternalTransaction tx, ISinglePhaseNotification enlistmentNotification, EnlistmentOptions enlistmentOptions, Transaction atomicTransaction)
	{
		Enlistment enlistment = new Enlistment(tx, enlistmentNotification, enlistmentNotification, atomicTransaction, enlistmentOptions);
		if ((enlistmentOptions & EnlistmentOptions.EnlistDuringPrepareRequired) != 0)
		{
			AddVolatileEnlistment(ref tx._phase0Volatiles, enlistment);
		}
		else
		{
			AddVolatileEnlistment(ref tx._phase1Volatiles, enlistment);
		}
		TransactionsEtwProvider log = TransactionsEtwProvider.Log;
		if (log.IsEnabled())
		{
			log.TransactionstateEnlist(enlistment.InternalEnlistment.EnlistmentTraceId, EnlistmentType.Volatile, enlistmentOptions);
		}
		return enlistment;
	}

	internal override bool EnlistPromotableSinglePhase(InternalTransaction tx, IPromotableSinglePhaseNotification promotableSinglePhaseNotification, Transaction atomicTransaction, Guid promoterType)
	{
		if (tx._durableEnlistment != null)
		{
			return false;
		}
		TransactionState.TransactionStatePSPEOperation.PSPEInitialize(tx, promotableSinglePhaseNotification, promoterType);
		Enlistment enlistment = new Enlistment(tx, promotableSinglePhaseNotification, atomicTransaction);
		tx._durableEnlistment = enlistment.InternalEnlistment;
		TransactionsEtwProvider log = TransactionsEtwProvider.Log;
		if (log.IsEnabled())
		{
			log.TransactionstateEnlist(tx._durableEnlistment.EnlistmentTraceId, EnlistmentType.PromotableSinglePhase, EnlistmentOptions.None);
		}
		tx._promoter = promotableSinglePhaseNotification;
		if (tx._promoterType == TransactionInterop.PromoterTypeDtc)
		{
			tx._promoteState = TransactionState.TransactionStateDelegated;
		}
		else
		{
			tx._promoteState = TransactionState.TransactionStateDelegatedNonMSDTC;
		}
		DurableEnlistmentState.DurableEnlistmentActive.EnterState(tx._durableEnlistment);
		return true;
	}

	internal override void Phase0VolatilePrepareDone(InternalTransaction tx)
	{
	}

	internal override void Phase1VolatilePrepareDone(InternalTransaction tx)
	{
	}

	internal override void DisposeRoot(InternalTransaction tx)
	{
		tx.State.Rollback(tx, null);
	}
}
