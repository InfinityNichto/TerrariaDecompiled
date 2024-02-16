namespace System.Transactions.Distributed;

internal sealed class DistributedDependentTransaction : DistributedTransaction
{
	internal void Complete()
	{
		throw DistributedTransaction.NotSupported();
	}
}
