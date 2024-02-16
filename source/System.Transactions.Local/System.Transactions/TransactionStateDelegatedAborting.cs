using System.Threading;

namespace System.Transactions;

internal sealed class TransactionStateDelegatedAborting : TransactionStatePromotedAborted
{
	internal override void EnterState(InternalTransaction tx)
	{
		CommonEnterState(tx);
		Monitor.Exit(tx);
		try
		{
			TransactionsEtwProvider log = TransactionsEtwProvider.Log;
			if (log.IsEnabled())
			{
				log.EnlistmentStatus(tx._durableEnlistment, NotificationCall.Rollback);
			}
			tx._durableEnlistment.PromotableSinglePhaseNotification.Rollback(tx._durableEnlistment.SinglePhaseEnlistment);
		}
		finally
		{
			Monitor.Enter(tx);
		}
	}

	internal override void BeginCommit(InternalTransaction tx, bool asyncCommit, AsyncCallback asyncCallback, object asyncState)
	{
		throw TransactionException.CreateTransactionStateException(tx._innerException, tx.DistributedTxId);
	}

	internal override void ChangeStatePromotedAborted(InternalTransaction tx)
	{
		TransactionState.TransactionStatePromotedAborted.EnterState(tx);
	}
}
