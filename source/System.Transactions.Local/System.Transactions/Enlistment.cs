namespace System.Transactions;

public class Enlistment
{
	internal InternalEnlistment _internalEnlistment;

	internal InternalEnlistment InternalEnlistment => _internalEnlistment;

	internal Enlistment(InternalEnlistment internalEnlistment)
	{
		_internalEnlistment = internalEnlistment;
	}

	internal Enlistment(Guid resourceManagerIdentifier, InternalTransaction transaction, IEnlistmentNotification twoPhaseNotifications, ISinglePhaseNotification singlePhaseNotifications, Transaction atomicTransaction)
	{
		_internalEnlistment = new DurableInternalEnlistment(this, resourceManagerIdentifier, transaction, twoPhaseNotifications, singlePhaseNotifications, atomicTransaction);
	}

	internal Enlistment(InternalTransaction transaction, IEnlistmentNotification twoPhaseNotifications, ISinglePhaseNotification singlePhaseNotifications, Transaction atomicTransaction, EnlistmentOptions enlistmentOptions)
	{
		if ((enlistmentOptions & EnlistmentOptions.EnlistDuringPrepareRequired) != 0)
		{
			_internalEnlistment = new InternalEnlistment(this, transaction, twoPhaseNotifications, singlePhaseNotifications, atomicTransaction);
		}
		else
		{
			_internalEnlistment = new Phase1VolatileEnlistment(this, transaction, twoPhaseNotifications, singlePhaseNotifications, atomicTransaction);
		}
	}

	internal Enlistment(InternalTransaction transaction, IPromotableSinglePhaseNotification promotableSinglePhaseNotification, Transaction atomicTransaction)
	{
		_internalEnlistment = new PromotableInternalEnlistment(this, transaction, promotableSinglePhaseNotification, atomicTransaction);
	}

	internal Enlistment(IEnlistmentNotification twoPhaseNotifications, InternalTransaction transaction, Transaction atomicTransaction)
	{
		_internalEnlistment = new InternalEnlistment(this, twoPhaseNotifications, transaction, atomicTransaction);
	}

	internal Enlistment(IEnlistmentNotification twoPhaseNotifications, object syncRoot)
	{
		_internalEnlistment = new RecoveringInternalEnlistment(this, twoPhaseNotifications, syncRoot);
	}

	public void Done()
	{
		TransactionsEtwProvider log = TransactionsEtwProvider.Log;
		if (log.IsEnabled())
		{
			log.MethodEnter(TraceSourceType.TraceSourceLtm, this, "Done");
			log.EnlistmentDone(_internalEnlistment);
		}
		lock (_internalEnlistment.SyncRoot)
		{
			_internalEnlistment.State.EnlistmentDone(_internalEnlistment);
		}
		if (log.IsEnabled())
		{
			log.MethodExit(TraceSourceType.TraceSourceLtm, this, "Done");
		}
	}
}
