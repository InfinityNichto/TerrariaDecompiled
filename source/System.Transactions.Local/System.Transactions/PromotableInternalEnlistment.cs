namespace System.Transactions;

internal sealed class PromotableInternalEnlistment : InternalEnlistment
{
	private readonly IPromotableSinglePhaseNotification _promotableNotificationInterface;

	internal override IPromotableSinglePhaseNotification PromotableSinglePhaseNotification => _promotableNotificationInterface;

	internal PromotableInternalEnlistment(Enlistment enlistment, InternalTransaction transaction, IPromotableSinglePhaseNotification promotableSinglePhaseNotification, Transaction atomicTransaction)
		: base(enlistment, transaction, atomicTransaction)
	{
		_promotableNotificationInterface = promotableSinglePhaseNotification;
	}
}
