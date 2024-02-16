namespace System.Transactions;

internal sealed class TransactionStatePhase0 : EnlistableStates
{
	internal override void EnterState(InternalTransaction tx)
	{
		CommonEnterState(tx);
		int volatileEnlistmentCount = tx._phase0Volatiles._volatileEnlistmentCount;
		int dependentClones = tx._phase0Volatiles._dependentClones;
		tx._phase0VolatileWaveCount = volatileEnlistmentCount;
		if (tx._phase0Volatiles._preparedVolatileEnlistments < volatileEnlistmentCount + dependentClones)
		{
			for (int i = 0; i < volatileEnlistmentCount; i++)
			{
				tx._phase0Volatiles._volatileEnlistments[i]._twoPhaseState.ChangeStatePreparing(tx._phase0Volatiles._volatileEnlistments[i]);
				if (!tx.State.ContinuePhase0Prepares())
				{
					break;
				}
			}
		}
		else
		{
			TransactionState.TransactionStateVolatilePhase1.EnterState(tx);
		}
	}

	internal override Enlistment EnlistDurable(InternalTransaction tx, Guid resourceManagerIdentifier, IEnlistmentNotification enlistmentNotification, EnlistmentOptions enlistmentOptions, Transaction atomicTransaction)
	{
		tx.ThrowIfPromoterTypeIsNotMSDTC();
		Enlistment result = base.EnlistDurable(tx, resourceManagerIdentifier, enlistmentNotification, enlistmentOptions, atomicTransaction);
		tx.State.RestartCommitIfNeeded(tx);
		return result;
	}

	internal override Enlistment EnlistDurable(InternalTransaction tx, Guid resourceManagerIdentifier, ISinglePhaseNotification enlistmentNotification, EnlistmentOptions enlistmentOptions, Transaction atomicTransaction)
	{
		tx.ThrowIfPromoterTypeIsNotMSDTC();
		Enlistment result = base.EnlistDurable(tx, resourceManagerIdentifier, enlistmentNotification, enlistmentOptions, atomicTransaction);
		tx.State.RestartCommitIfNeeded(tx);
		return result;
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

	internal override void Rollback(InternalTransaction tx, Exception e)
	{
		ChangeStateTransactionAborted(tx, e);
	}

	internal override bool EnlistPromotableSinglePhase(InternalTransaction tx, IPromotableSinglePhaseNotification promotableSinglePhaseNotification, Transaction atomicTransaction, Guid promoterType)
	{
		if (tx._durableEnlistment != null)
		{
			return false;
		}
		TransactionState.TransactionStatePSPEOperation.Phase0PSPEInitialize(tx, promotableSinglePhaseNotification, promoterType);
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
		int volatileEnlistmentCount = tx._phase0Volatiles._volatileEnlistmentCount;
		int dependentClones = tx._phase0Volatiles._dependentClones;
		tx._phase0VolatileWaveCount = volatileEnlistmentCount;
		if (tx._phase0Volatiles._preparedVolatileEnlistments < volatileEnlistmentCount + dependentClones)
		{
			for (int i = 0; i < volatileEnlistmentCount; i++)
			{
				tx._phase0Volatiles._volatileEnlistments[i]._twoPhaseState.ChangeStatePreparing(tx._phase0Volatiles._volatileEnlistments[i]);
				if (!tx.State.ContinuePhase0Prepares())
				{
					break;
				}
			}
		}
		else
		{
			TransactionState.TransactionStateVolatilePhase1.EnterState(tx);
		}
	}

	internal override void Phase1VolatilePrepareDone(InternalTransaction tx)
	{
	}

	internal override void RestartCommitIfNeeded(InternalTransaction tx)
	{
	}

	internal override bool ContinuePhase0Prepares()
	{
		return true;
	}

	internal override void Promote(InternalTransaction tx)
	{
		tx._promoteState.EnterState(tx);
		tx.State.CheckForFinishedTransaction(tx);
		tx.State.RestartCommitIfNeeded(tx);
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
