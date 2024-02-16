namespace System.Transactions;

internal sealed class Phase1VolatileEnlistment : InternalEnlistment
{
	public Phase1VolatileEnlistment(Enlistment enlistment, InternalTransaction transaction, IEnlistmentNotification twoPhaseNotifications, ISinglePhaseNotification singlePhaseNotifications, Transaction atomicTransaction)
		: base(enlistment, transaction, twoPhaseNotifications, singlePhaseNotifications, atomicTransaction)
	{
	}

	internal override void FinishEnlistment()
	{
		_transaction._phase1Volatiles._preparedVolatileEnlistments++;
		CheckComplete();
	}

	internal override void CheckComplete()
	{
		if (_transaction._phase1Volatiles._preparedVolatileEnlistments == _transaction._phase1Volatiles._volatileEnlistmentCount + _transaction._phase1Volatiles._dependentClones)
		{
			_transaction.State.Phase1VolatilePrepareDone(_transaction);
		}
	}
}
