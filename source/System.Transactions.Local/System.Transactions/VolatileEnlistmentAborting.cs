using System.Threading;

namespace System.Transactions;

internal sealed class VolatileEnlistmentAborting : VolatileEnlistmentState
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
				log.EnlistmentStatus(enlistment, NotificationCall.Rollback);
			}
			enlistment.EnlistmentNotification.Rollback(enlistment.SinglePhaseEnlistment);
		}
		finally
		{
			Monitor.Enter(enlistment.Transaction);
		}
	}

	internal override void ChangeStatePreparing(InternalEnlistment enlistment)
	{
	}

	internal override void EnlistmentDone(InternalEnlistment enlistment)
	{
		VolatileEnlistmentState.VolatileEnlistmentEnded.EnterState(enlistment);
	}

	internal override void InternalAborted(InternalEnlistment enlistment)
	{
	}
}
