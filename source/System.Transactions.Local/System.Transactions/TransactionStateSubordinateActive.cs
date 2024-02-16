namespace System.Transactions;

internal sealed class TransactionStateSubordinateActive : TransactionStateActive
{
	internal override void EnterState(InternalTransaction tx)
	{
		CommonEnterState(tx);
	}

	internal override void Rollback(InternalTransaction tx, Exception e)
	{
		if (tx._innerException == null)
		{
			tx._innerException = e;
		}
		((ISimpleTransactionSuperior)tx._promoter).Rollback();
		TransactionState.TransactionStateAborted.EnterState(tx);
	}

	internal override Enlistment EnlistVolatile(InternalTransaction tx, IEnlistmentNotification enlistmentNotification, EnlistmentOptions enlistmentOptions, Transaction atomicTransaction)
	{
		tx._promoteState.EnterState(tx);
		return tx.State.EnlistVolatile(tx, enlistmentNotification, enlistmentOptions, atomicTransaction);
	}

	internal override Enlistment EnlistVolatile(InternalTransaction tx, ISinglePhaseNotification enlistmentNotification, EnlistmentOptions enlistmentOptions, Transaction atomicTransaction)
	{
		tx._promoteState.EnterState(tx);
		return tx.State.EnlistVolatile(tx, enlistmentNotification, enlistmentOptions, atomicTransaction);
	}

	internal override TransactionStatus get_Status(InternalTransaction tx)
	{
		tx._promoteState.EnterState(tx);
		return tx.State.get_Status(tx);
	}

	internal override void AddOutcomeRegistrant(InternalTransaction tx, TransactionCompletedEventHandler transactionCompletedDelegate)
	{
		tx._promoteState.EnterState(tx);
		tx.State.AddOutcomeRegistrant(tx, transactionCompletedDelegate);
	}

	internal override bool EnlistPromotableSinglePhase(InternalTransaction tx, IPromotableSinglePhaseNotification promotableSinglePhaseNotification, Transaction atomicTransaction, Guid promoterType)
	{
		return false;
	}

	internal override void CreateBlockingClone(InternalTransaction tx)
	{
		tx._promoteState.EnterState(tx);
		tx.State.CreateBlockingClone(tx);
	}

	internal override void CreateAbortingClone(InternalTransaction tx)
	{
		tx._promoteState.EnterState(tx);
		tx.State.CreateAbortingClone(tx);
	}
}
