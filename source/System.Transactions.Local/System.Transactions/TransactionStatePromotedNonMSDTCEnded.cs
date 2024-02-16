using System.Threading;

namespace System.Transactions;

internal abstract class TransactionStatePromotedNonMSDTCEnded : TransactionStateEnded
{
	private static WaitCallback s_signalMethod;

	private static WaitCallback SignalMethod => LazyInitializer.EnsureInitialized<WaitCallback>(ref s_signalMethod, ref TransactionState.s_classSyncObject, () => SignalCallback);

	internal override void EnterState(InternalTransaction tx)
	{
		base.EnterState(tx);
		CommonEnterState(tx);
		if (!ThreadPool.QueueUserWorkItem(SignalMethod, tx))
		{
			throw TransactionException.CreateInvalidOperationException(TraceSourceType.TraceSourceLtm, System.SR.UnexpectedFailureOfThreadPool, null, tx?.DistributedTxId ?? Guid.Empty);
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

	internal override void EndCommit(InternalTransaction tx)
	{
		PromotedTransactionOutcome(tx);
	}

	internal override void CompleteBlockingClone(InternalTransaction tx)
	{
	}

	internal override void CompleteAbortingClone(InternalTransaction tx)
	{
	}

	internal override void CreateBlockingClone(InternalTransaction tx)
	{
		throw TransactionException.CreateTransactionStateException(tx._innerException, tx.DistributedTxId);
	}

	internal override void CreateAbortingClone(InternalTransaction tx)
	{
		throw TransactionException.CreateTransactionStateException(tx._innerException, tx.DistributedTxId);
	}

	internal override Guid get_Identifier(InternalTransaction tx)
	{
		return tx._distributedTransactionIdentifierNonMSDTC;
	}

	internal override void Promote(InternalTransaction tx)
	{
	}

	protected abstract void PromotedTransactionOutcome(InternalTransaction tx);

	private static void SignalCallback(object state)
	{
		InternalTransaction internalTransaction = (InternalTransaction)state;
		lock (internalTransaction)
		{
			internalTransaction.SignalAsyncCompletion();
		}
	}
}
