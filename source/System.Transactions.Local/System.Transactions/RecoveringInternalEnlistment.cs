namespace System.Transactions;

internal sealed class RecoveringInternalEnlistment : DurableInternalEnlistment
{
	private readonly object _syncRoot;

	internal override object SyncRoot => _syncRoot;

	internal RecoveringInternalEnlistment(Enlistment enlistment, IEnlistmentNotification twoPhaseNotifications, object syncRoot)
		: base(enlistment, twoPhaseNotifications)
	{
		_syncRoot = syncRoot;
	}
}
