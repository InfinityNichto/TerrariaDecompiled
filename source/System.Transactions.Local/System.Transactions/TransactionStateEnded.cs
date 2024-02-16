using System.Threading;

namespace System.Transactions;

internal abstract class TransactionStateEnded : TransactionState
{
	internal override void EnterState(InternalTransaction tx)
	{
		if (tx._needPulse)
		{
			Monitor.Pulse(tx);
		}
	}

	internal override void AddOutcomeRegistrant(InternalTransaction tx, TransactionCompletedEventHandler transactionCompletedDelegate)
	{
		if (transactionCompletedDelegate != null)
		{
			TransactionEventArgs transactionEventArgs = new TransactionEventArgs();
			transactionEventArgs._transaction = tx._outcomeSource.InternalClone();
			transactionCompletedDelegate(transactionEventArgs._transaction, transactionEventArgs);
		}
	}

	internal override bool IsCompleted(InternalTransaction tx)
	{
		return true;
	}
}
