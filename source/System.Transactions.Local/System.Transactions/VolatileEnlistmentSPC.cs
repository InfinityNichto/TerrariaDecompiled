using System.Threading;

namespace System.Transactions;

internal sealed class VolatileEnlistmentSPC : VolatileEnlistmentState
{
	internal override void EnterState(InternalEnlistment enlistment)
	{
		bool flag = false;
		enlistment.State = this;
		TransactionsEtwProvider log = TransactionsEtwProvider.Log;
		if (log.IsEnabled())
		{
			log.EnlistmentStatus(enlistment, NotificationCall.SinglePhaseCommit);
		}
		Monitor.Exit(enlistment.Transaction);
		try
		{
			enlistment.SinglePhaseNotification.SinglePhaseCommit(enlistment.SinglePhaseEnlistment);
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
		VolatileEnlistmentState.VolatileEnlistmentEnded.EnterState(enlistment);
		enlistment.Transaction.State.ChangeStateTransactionCommitted(enlistment.Transaction);
	}

	internal override void Committed(InternalEnlistment enlistment)
	{
		VolatileEnlistmentState.VolatileEnlistmentEnded.EnterState(enlistment);
		enlistment.Transaction.State.ChangeStateTransactionCommitted(enlistment.Transaction);
	}

	internal override void Aborted(InternalEnlistment enlistment, Exception e)
	{
		VolatileEnlistmentState.VolatileEnlistmentEnded.EnterState(enlistment);
		enlistment.Transaction.State.ChangeStateTransactionAborted(enlistment.Transaction, e);
	}

	internal override void InDoubt(InternalEnlistment enlistment, Exception e)
	{
		VolatileEnlistmentState.VolatileEnlistmentEnded.EnterState(enlistment);
		if (enlistment.Transaction._innerException == null)
		{
			enlistment.Transaction._innerException = e;
		}
		enlistment.Transaction.State.InDoubtFromEnlistment(enlistment.Transaction);
	}
}
