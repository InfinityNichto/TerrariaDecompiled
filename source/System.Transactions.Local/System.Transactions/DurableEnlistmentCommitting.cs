using System.Threading;

namespace System.Transactions;

internal sealed class DurableEnlistmentCommitting : DurableEnlistmentState
{
	internal override void EnterState(InternalEnlistment enlistment)
	{
		bool flag = false;
		enlistment.State = this;
		Monitor.Exit(enlistment.Transaction);
		try
		{
			TransactionsEtwProvider log = TransactionsEtwProvider.Log;
			if (log.IsEnabled())
			{
				log.EnlistmentStatus(enlistment, NotificationCall.SinglePhaseCommit);
			}
			if (enlistment.SinglePhaseNotification != null)
			{
				enlistment.SinglePhaseNotification.SinglePhaseCommit(enlistment.SinglePhaseEnlistment);
			}
			else
			{
				enlistment.PromotableSinglePhaseNotification.SinglePhaseCommit(enlistment.SinglePhaseEnlistment);
			}
			flag = true;
		}
		finally
		{
			if (!flag)
			{
				enlistment.SinglePhaseEnlistment.InDoubt();
			}
			Monitor.Enter(enlistment.Transaction);
		}
	}

	internal override void EnlistmentDone(InternalEnlistment enlistment)
	{
		DurableEnlistmentState.DurableEnlistmentEnded.EnterState(enlistment);
		enlistment.Transaction.State.ChangeStateTransactionCommitted(enlistment.Transaction);
	}

	internal override void Committed(InternalEnlistment enlistment)
	{
		DurableEnlistmentState.DurableEnlistmentEnded.EnterState(enlistment);
		enlistment.Transaction.State.ChangeStateTransactionCommitted(enlistment.Transaction);
	}

	internal override void Aborted(InternalEnlistment enlistment, Exception e)
	{
		DurableEnlistmentState.DurableEnlistmentEnded.EnterState(enlistment);
		enlistment.Transaction.State.ChangeStateTransactionAborted(enlistment.Transaction, e);
	}

	internal override void InDoubt(InternalEnlistment enlistment, Exception e)
	{
		DurableEnlistmentState.DurableEnlistmentEnded.EnterState(enlistment);
		if (enlistment.Transaction._innerException == null)
		{
			enlistment.Transaction._innerException = e;
		}
		enlistment.Transaction.State.InDoubtFromEnlistment(enlistment.Transaction);
	}
}
