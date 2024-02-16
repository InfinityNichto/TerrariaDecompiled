namespace System.Transactions;

internal interface IPromotedEnlistment
{
	void EnlistmentDone();

	void Prepared();

	void ForceRollback();

	void ForceRollback(Exception e);

	void Committed();

	void Aborted(Exception e);

	void InDoubt(Exception e);

	byte[] GetRecoveryInformation();
}
