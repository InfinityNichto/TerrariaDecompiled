namespace System.Transactions;

public interface IPromotableSinglePhaseNotification : ITransactionPromoter
{
	void Initialize();

	void SinglePhaseCommit(SinglePhaseEnlistment singlePhaseEnlistment);

	void Rollback(SinglePhaseEnlistment singlePhaseEnlistment);
}
