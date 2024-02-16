namespace System.Transactions;

public sealed class DependentTransaction : Transaction
{
	private readonly bool _blocking;

	internal DependentTransaction(IsolationLevel isoLevel, InternalTransaction internalTransaction, bool blocking)
		: base(isoLevel, internalTransaction)
	{
		_blocking = blocking;
		lock (_internalTransaction)
		{
			if (blocking)
			{
				_internalTransaction.State.CreateBlockingClone(_internalTransaction);
			}
			else
			{
				_internalTransaction.State.CreateAbortingClone(_internalTransaction);
			}
		}
	}

	public void Complete()
	{
		TransactionsEtwProvider log = TransactionsEtwProvider.Log;
		if (log.IsEnabled())
		{
			log.MethodEnter(TraceSourceType.TraceSourceLtm, this, "Complete");
		}
		lock (_internalTransaction)
		{
			if (base.Disposed)
			{
				throw new ObjectDisposedException("DependentTransaction");
			}
			if (_complete)
			{
				throw TransactionException.CreateTransactionCompletedException(base.DistributedTxId);
			}
			_complete = true;
			if (_blocking)
			{
				_internalTransaction.State.CompleteBlockingClone(_internalTransaction);
			}
			else
			{
				_internalTransaction.State.CompleteAbortingClone(_internalTransaction);
			}
		}
		if (log.IsEnabled())
		{
			log.TransactionDependentCloneComplete(this, "DependentTransaction");
			log.MethodExit(TraceSourceType.TraceSourceLtm, this, "Complete");
		}
	}
}
