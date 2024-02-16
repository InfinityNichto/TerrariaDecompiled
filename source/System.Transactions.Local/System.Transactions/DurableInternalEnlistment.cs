namespace System.Transactions;

internal class DurableInternalEnlistment : InternalEnlistment
{
	internal Guid _resourceManagerIdentifier;

	internal override Guid ResourceManagerIdentifier => _resourceManagerIdentifier;

	internal DurableInternalEnlistment(Enlistment enlistment, Guid resourceManagerIdentifier, InternalTransaction transaction, IEnlistmentNotification twoPhaseNotifications, ISinglePhaseNotification singlePhaseNotifications, Transaction atomicTransaction)
		: base(enlistment, transaction, twoPhaseNotifications, singlePhaseNotifications, atomicTransaction)
	{
		_resourceManagerIdentifier = resourceManagerIdentifier;
	}

	protected DurableInternalEnlistment(Enlistment enlistment, IEnlistmentNotification twoPhaseNotifications)
		: base(enlistment, twoPhaseNotifications)
	{
	}
}
