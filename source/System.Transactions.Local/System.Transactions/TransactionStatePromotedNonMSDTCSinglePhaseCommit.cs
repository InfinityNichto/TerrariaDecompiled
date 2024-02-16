namespace System.Transactions;

internal sealed class TransactionStatePromotedNonMSDTCSinglePhaseCommit : TransactionStatePromotedNonMSDTCBase
{
	internal override void EnterState(InternalTransaction tx)
	{
		CommonEnterState(tx);
		TransactionsEtwProvider log = TransactionsEtwProvider.Log;
		if (log.IsEnabled())
		{
			log.EnlistmentStatus(tx._durableEnlistment, NotificationCall.SinglePhaseCommit);
		}
		TransactionManager.TransactionTable.Remove(tx);
		tx._durableEnlistment.State.ChangeStateCommitting(tx._durableEnlistment);
	}

	internal override void BeginCommit(InternalTransaction tx, bool asyncCommit, AsyncCallback asyncCallback, object asyncState)
	{
		throw TransactionException.CreateTransactionStateException(tx._innerException, tx.DistributedTxId);
	}

	internal override void Rollback(InternalTransaction tx, Exception e)
	{
		throw TransactionException.CreateTransactionStateException(tx._innerException, tx.DistributedTxId);
	}

	internal override void ChangeStateTransactionCommitted(InternalTransaction tx)
	{
		TransactionState.TransactionStatePromotedNonMSDTCCommitted.EnterState(tx);
	}

	internal override void InDoubtFromEnlistment(InternalTransaction tx)
	{
		TransactionState.TransactionStatePromotedNonMSDTCIndoubt.EnterState(tx);
	}

	internal override void ChangeStateTransactionAborted(InternalTransaction tx, Exception e)
	{
		if (tx._innerException == null)
		{
			tx._innerException = e;
		}
		TransactionState.TransactionStatePromotedNonMSDTCAborted.EnterState(tx);
	}

	internal override void ChangeStateAbortedDuringPromotion(InternalTransaction tx)
	{
		TransactionState.TransactionStateAborted.EnterState(tx);
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
