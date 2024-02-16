namespace System.Transactions.Distributed;

internal sealed class DistributedCommittableTransaction : DistributedTransaction
{
	internal void BeginCommit(InternalTransaction tx)
	{
		throw DistributedTransaction.NotSupported();
	}
}
