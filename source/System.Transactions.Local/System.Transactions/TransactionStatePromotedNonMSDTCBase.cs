namespace System.Transactions;

internal abstract class TransactionStatePromotedNonMSDTCBase : TransactionState
{
	internal override TransactionStatus get_Status(InternalTransaction tx)
	{
		return TransactionStatus.Active;
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

	internal override Enlistment EnlistDurable(InternalTransaction tx, Guid resourceManagerIdentifier, IEnlistmentNotification enlistmentNotification, EnlistmentOptions enlistmentOptions, Transaction atomicTransaction)
	{
		throw new TransactionPromotionException(System.SR.Format(System.SR.PromoterTypeUnrecognized, tx._promoterType.ToString()), tx._innerException);
	}

	internal override Enlistment EnlistDurable(InternalTransaction tx, Guid resourceManagerIdentifier, ISinglePhaseNotification enlistmentNotification, EnlistmentOptions enlistmentOptions, Transaction atomicTransaction)
	{
		throw new TransactionPromotionException(System.SR.Format(System.SR.PromoterTypeUnrecognized, tx._promoterType.ToString()), tx._innerException);
	}

	internal override bool EnlistPromotableSinglePhase(InternalTransaction tx, IPromotableSinglePhaseNotification promotableSinglePhaseNotification, Transaction atomicTransaction, Guid promoterType)
	{
		return false;
	}

	internal override void Rollback(InternalTransaction tx, Exception e)
	{
		if (tx._innerException == null)
		{
			tx._innerException = e;
		}
		TransactionState.TransactionStateAborted.EnterState(tx);
	}

	internal override Guid get_Identifier(InternalTransaction tx)
	{
		return tx._distributedTransactionIdentifierNonMSDTC;
	}

	internal override void AddOutcomeRegistrant(InternalTransaction tx, TransactionCompletedEventHandler transactionCompletedDelegate)
	{
		tx._transactionCompletedDelegate = (TransactionCompletedEventHandler)Delegate.Combine(tx._transactionCompletedDelegate, transactionCompletedDelegate);
	}

	internal override void BeginCommit(InternalTransaction tx, bool asyncCommit, AsyncCallback asyncCallback, object asyncState)
	{
		tx._asyncCommit = asyncCommit;
		tx._asyncCallback = asyncCallback;
		tx._asyncState = asyncState;
		TransactionState.TransactionStatePromotedNonMSDTCPhase0.EnterState(tx);
	}

	internal override void CompleteBlockingClone(InternalTransaction tx)
	{
		if (tx._phase0Volatiles._dependentClones > 0)
		{
			tx._phase0Volatiles._dependentClones--;
			if (tx._phase0Volatiles._preparedVolatileEnlistments == tx._phase0VolatileWaveCount + tx._phase0Volatiles._dependentClones)
			{
				tx.State.Phase0VolatilePrepareDone(tx);
			}
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

	internal override bool ContinuePhase0Prepares()
	{
		return true;
	}

	internal override void ChangeStateTransactionAborted(InternalTransaction tx, Exception e)
	{
		if (tx._innerException == null)
		{
			tx._innerException = e;
		}
		TransactionState.TransactionStateAborted.EnterState(tx);
	}

	internal override void InDoubtFromEnlistment(InternalTransaction tx)
	{
		TransactionState.TransactionStatePromotedNonMSDTCIndoubt.EnterState(tx);
	}

	internal override void ChangeStateAbortedDuringPromotion(InternalTransaction tx)
	{
		TransactionState.TransactionStateAborted.EnterState(tx);
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

	internal override void Promote(InternalTransaction tx)
	{
	}

	internal override void Phase0VolatilePrepareDone(InternalTransaction tx)
	{
	}

	internal override void Phase1VolatilePrepareDone(InternalTransaction tx)
	{
	}

	internal override byte[] PromotedToken(InternalTransaction tx)
	{
		return tx.promotedToken;
	}

	internal override void DisposeRoot(InternalTransaction tx)
	{
		tx.State.Rollback(tx, null);
	}
}
