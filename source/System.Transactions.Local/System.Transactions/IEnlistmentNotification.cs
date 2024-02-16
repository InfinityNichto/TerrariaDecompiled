namespace System.Transactions;

public interface IEnlistmentNotification
{
	void Prepare(PreparingEnlistment preparingEnlistment);

	void Commit(Enlistment enlistment);

	void Rollback(Enlistment enlistment);

	void InDoubt(Enlistment enlistment);
}
