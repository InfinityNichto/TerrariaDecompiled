using System.Threading;
using System.Transactions.Distributed;

namespace System.Transactions;

internal abstract class TransactionStatePromotedBase : TransactionState
{
	internal override TransactionStatus get_Status(InternalTransaction tx)
	{
		return TransactionStatus.Active;
	}

	internal override Enlistment EnlistVolatile(InternalTransaction tx, IEnlistmentNotification enlistmentNotification, EnlistmentOptions enlistmentOptions, Transaction atomicTransaction)
	{
		Monitor.Exit(tx);
		try
		{
			Enlistment enlistment = new Enlistment(enlistmentNotification, tx, atomicTransaction);
			EnlistmentState.EnlistmentStatePromoted.EnterState(enlistment.InternalEnlistment);
			enlistment.InternalEnlistment.PromotedEnlistment = tx.PromotedTransaction.EnlistVolatile(enlistment.InternalEnlistment, enlistmentOptions);
			return enlistment;
		}
		finally
		{
			Monitor.Enter(tx);
		}
	}

	internal override Enlistment EnlistVolatile(InternalTransaction tx, ISinglePhaseNotification enlistmentNotification, EnlistmentOptions enlistmentOptions, Transaction atomicTransaction)
	{
		Monitor.Exit(tx);
		try
		{
			Enlistment enlistment = new Enlistment(enlistmentNotification, tx, atomicTransaction);
			EnlistmentState.EnlistmentStatePromoted.EnterState(enlistment.InternalEnlistment);
			enlistment.InternalEnlistment.PromotedEnlistment = tx.PromotedTransaction.EnlistVolatile(enlistment.InternalEnlistment, enlistmentOptions);
			return enlistment;
		}
		finally
		{
			Monitor.Enter(tx);
		}
	}

	internal override Enlistment EnlistDurable(InternalTransaction tx, Guid resourceManagerIdentifier, IEnlistmentNotification enlistmentNotification, EnlistmentOptions enlistmentOptions, Transaction atomicTransaction)
	{
		tx.ThrowIfPromoterTypeIsNotMSDTC();
		Monitor.Exit(tx);
		try
		{
			Enlistment enlistment = new Enlistment(resourceManagerIdentifier, tx, enlistmentNotification, null, atomicTransaction);
			EnlistmentState.EnlistmentStatePromoted.EnterState(enlistment.InternalEnlistment);
			enlistment.InternalEnlistment.PromotedEnlistment = tx.PromotedTransaction.EnlistDurable(resourceManagerIdentifier, (DurableInternalEnlistment)enlistment.InternalEnlistment, v: false, enlistmentOptions);
			return enlistment;
		}
		finally
		{
			Monitor.Enter(tx);
		}
	}

	internal override Enlistment EnlistDurable(InternalTransaction tx, Guid resourceManagerIdentifier, ISinglePhaseNotification enlistmentNotification, EnlistmentOptions enlistmentOptions, Transaction atomicTransaction)
	{
		tx.ThrowIfPromoterTypeIsNotMSDTC();
		Monitor.Exit(tx);
		try
		{
			Enlistment enlistment = new Enlistment(resourceManagerIdentifier, tx, enlistmentNotification, enlistmentNotification, atomicTransaction);
			EnlistmentState.EnlistmentStatePromoted.EnterState(enlistment.InternalEnlistment);
			enlistment.InternalEnlistment.PromotedEnlistment = tx.PromotedTransaction.EnlistDurable(resourceManagerIdentifier, (DurableInternalEnlistment)enlistment.InternalEnlistment, v: true, enlistmentOptions);
			return enlistment;
		}
		finally
		{
			Monitor.Enter(tx);
		}
	}

	internal override void Rollback(InternalTransaction tx, Exception e)
	{
		if (tx._innerException == null)
		{
			tx._innerException = e;
		}
		Monitor.Exit(tx);
		try
		{
			tx.PromotedTransaction.Rollback();
		}
		finally
		{
			Monitor.Enter(tx);
		}
	}

	internal override Guid get_Identifier(InternalTransaction tx)
	{
		if (tx != null && tx.PromotedTransaction != null)
		{
			return tx.PromotedTransaction.Identifier;
		}
		return Guid.Empty;
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
		TransactionState.TransactionStatePromotedCommitting.EnterState(tx);
	}

	internal override void RestartCommitIfNeeded(InternalTransaction tx)
	{
		TransactionState.TransactionStatePromotedP0Wave.EnterState(tx);
	}

	internal override bool EnlistPromotableSinglePhase(InternalTransaction tx, IPromotableSinglePhaseNotification promotableSinglePhaseNotification, Transaction atomicTransaction, Guid promoterType)
	{
		return false;
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
			return;
		}
		tx._phase0WaveDependentCloneCount--;
		if (tx._phase0WaveDependentCloneCount != 0)
		{
			return;
		}
		DistributedDependentTransaction phase0WaveDependentClone = tx._phase0WaveDependentClone;
		tx._phase0WaveDependentClone = null;
		Monitor.Exit(tx);
		try
		{
			try
			{
				phase0WaveDependentClone.Complete();
			}
			finally
			{
				phase0WaveDependentClone.Dispose();
			}
		}
		finally
		{
			Monitor.Enter(tx);
		}
	}

	internal override void CompleteAbortingClone(InternalTransaction tx)
	{
		if (tx._phase1Volatiles.VolatileDemux != null)
		{
			tx._phase1Volatiles._dependentClones--;
			return;
		}
		tx._abortingDependentCloneCount--;
		if (tx._abortingDependentCloneCount != 0)
		{
			return;
		}
		DistributedDependentTransaction abortingDependentClone = tx._abortingDependentClone;
		tx._abortingDependentClone = null;
		Monitor.Exit(tx);
		try
		{
			try
			{
				abortingDependentClone.Complete();
			}
			finally
			{
				abortingDependentClone.Dispose();
			}
		}
		finally
		{
			Monitor.Enter(tx);
		}
	}

	internal override void CreateBlockingClone(InternalTransaction tx)
	{
		if (tx._phase0WaveDependentClone == null)
		{
			tx._phase0WaveDependentClone = tx.PromotedTransaction.DependentClone(v: true);
		}
		tx._phase0WaveDependentCloneCount++;
	}

	internal override void CreateAbortingClone(InternalTransaction tx)
	{
		if (tx._phase1Volatiles.VolatileDemux != null)
		{
			tx._phase1Volatiles._dependentClones++;
			return;
		}
		if (tx._abortingDependentClone == null)
		{
			tx._abortingDependentClone = tx.PromotedTransaction.DependentClone(v: false);
		}
		tx._abortingDependentCloneCount++;
	}

	internal override bool ContinuePhase0Prepares()
	{
		return true;
	}

	internal override void ChangeStatePromotedAborted(InternalTransaction tx)
	{
		TransactionState.TransactionStatePromotedAborted.EnterState(tx);
	}

	internal override void ChangeStatePromotedCommitted(InternalTransaction tx)
	{
		TransactionState.TransactionStatePromotedCommitted.EnterState(tx);
	}

	internal override void InDoubtFromEnlistment(InternalTransaction tx)
	{
		TransactionState.TransactionStatePromotedIndoubt.EnterState(tx);
	}

	internal override void ChangeStateAbortedDuringPromotion(InternalTransaction tx)
	{
		TransactionState.TransactionStateAborted.EnterState(tx);
	}

	internal override void Timeout(InternalTransaction tx)
	{
		try
		{
			if (tx._innerException == null)
			{
				tx._innerException = new TimeoutException(System.SR.TraceTransactionTimeout);
			}
			tx.PromotedTransaction.Rollback();
			TransactionsEtwProvider log = TransactionsEtwProvider.Log;
			if (log.IsEnabled())
			{
				log.TransactionTimeout(tx.TransactionTraceId);
			}
		}
		catch (TransactionException exception)
		{
			TransactionsEtwProvider log2 = TransactionsEtwProvider.Log;
			if (log2.IsEnabled())
			{
				log2.ExceptionConsumed(exception);
			}
		}
	}

	internal override void Promote(InternalTransaction tx)
	{
	}

	internal override byte[] PromotedToken(InternalTransaction tx)
	{
		return tx.promotedToken;
	}

	internal override void Phase0VolatilePrepareDone(InternalTransaction tx)
	{
	}

	internal override void Phase1VolatilePrepareDone(InternalTransaction tx)
	{
	}
}
