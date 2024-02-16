namespace System.Transactions;

internal enum NotificationCall
{
	Prepare,
	Commit,
	Rollback,
	InDoubt,
	SinglePhaseCommit,
	Promote
}
