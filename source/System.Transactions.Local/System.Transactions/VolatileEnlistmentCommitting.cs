using System.Threading;

namespace System.Transactions;

internal sealed class VolatileEnlistmentCommitting : VolatileEnlistmentState
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
				log.EnlistmentStatus(enlistment, NotificationCall.Commit);
			}
			enlistment.EnlistmentNotification.Commit(enlistment.Enlistment);
		}
		finally
		{
			Monitor.Enter(enlistment.Transaction);
		}
	}

	internal override void EnlistmentDone(InternalEnlistment enlistment)
	{
		VolatileEnlistmentState.VolatileEnlistmentEnded.EnterState(enlistment);
	}
}
