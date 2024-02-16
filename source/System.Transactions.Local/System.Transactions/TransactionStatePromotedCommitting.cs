using System.Transactions.Distributed;

namespace System.Transactions;

internal class TransactionStatePromotedCommitting : TransactionStatePromotedBase
{
	internal override void EnterState(InternalTransaction tx)
	{
		CommonEnterState(tx);
		DistributedCommittableTransaction distributedCommittableTransaction = (DistributedCommittableTransaction)tx.PromotedTransaction;
		distributedCommittableTransaction.BeginCommit(tx);
	}

	internal override void BeginCommit(InternalTransaction tx, bool asyncCommit, AsyncCallback asyncCallback, object asyncState)
	{
		throw TransactionException.CreateTransactionStateException(tx._innerException, tx.DistributedTxId);
	}
}
