using System.Threading;

namespace System.Transactions;

internal sealed class VolatileEnlistmentPreparing : VolatileEnlistmentState
{
	internal override void EnterState(InternalEnlistment enlistment)
	{
		enlistment.State = this;
		Monitor.Exit(enlistment.Transaction);
		try
		{
			TransactionsEtwProvider log = TransactionsEtwProvider.Log;
			if (log.IsEnabled())
			{
				log.EnlistmentStatus(enlistment, NotificationCall.Prepare);
			}
			enlistment.EnlistmentNotification.Prepare(enlistment.PreparingEnlistment);
		}
		finally
		{
			Monitor.Enter(enlistment.Transaction);
		}
	}

	internal override void EnlistmentDone(InternalEnlistment enlistment)
	{
		VolatileEnlistmentState.VolatileEnlistmentDone.EnterState(enlistment);
		enlistment.FinishEnlistment();
	}

	internal override void Prepared(InternalEnlistment enlistment)
	{
		VolatileEnlistmentState.VolatileEnlistmentPrepared.EnterState(enlistment);
		enlistment.FinishEnlistment();
	}

	internal override void ForceRollback(InternalEnlistment enlistment, Exception e)
	{
		VolatileEnlistmentState.VolatileEnlistmentEnded.EnterState(enlistment);
		enlistment.Transaction.State.ChangeStateTransactionAborted(enlistment.Transaction, e);
		enlistment.FinishEnlistment();
	}

	internal override void ChangeStatePreparing(InternalEnlistment enlistment)
	{
	}

	internal override void InternalAborted(InternalEnlistment enlistment)
	{
		VolatileEnlistmentState.VolatileEnlistmentPreparingAborting.EnterState(enlistment);
	}
}
