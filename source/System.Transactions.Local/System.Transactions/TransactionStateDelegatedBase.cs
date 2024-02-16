using System.Collections;
using System.Transactions.Distributed;

namespace System.Transactions;

internal abstract class TransactionStateDelegatedBase : TransactionStatePromoted
{
	internal override void EnterState(InternalTransaction tx)
	{
		if (tx._outcomeSource._isoLevel == IsolationLevel.Snapshot)
		{
			throw TransactionException.CreateInvalidOperationException(TraceSourceType.TraceSourceLtm, System.SR.CannotPromoteSnapshot, null, tx?.DistributedTxId ?? Guid.Empty);
		}
		CommonEnterState(tx);
		DistributedTransaction distributedTransaction = null;
		try
		{
			if (tx._durableEnlistment != null)
			{
				TransactionsEtwProvider log = TransactionsEtwProvider.Log;
				if (log.IsEnabled())
				{
					log.EnlistmentStatus(tx._durableEnlistment, NotificationCall.Promote);
				}
			}
			distributedTransaction = TransactionState.TransactionStatePSPEOperation.PSPEPromote(tx);
		}
		catch (TransactionPromotionException innerException)
		{
			TransactionPromotionException exception = (TransactionPromotionException)(tx._innerException = innerException);
			TransactionsEtwProvider log2 = TransactionsEtwProvider.Log;
			if (log2.IsEnabled())
			{
				log2.ExceptionConsumed(exception);
			}
		}
		finally
		{
			if (distributedTransaction == null)
			{
				tx.State.ChangeStateAbortedDuringPromotion(tx);
			}
		}
		if (distributedTransaction != null && tx.PromotedTransaction != distributedTransaction)
		{
			tx.PromotedTransaction = distributedTransaction;
			Hashtable promotedTransactionTable = TransactionManager.PromotedTransactionTable;
			lock (promotedTransactionTable)
			{
				tx._finalizedObject = new FinalizedObject(tx, tx.PromotedTransaction.Identifier);
				WeakReference value = new WeakReference(tx._outcomeSource, trackResurrection: false);
				promotedTransactionTable[tx.PromotedTransaction.Identifier] = value;
			}
			TransactionManager.FireDistributedTransactionStarted(tx._outcomeSource);
			TransactionsEtwProvider log3 = TransactionsEtwProvider.Log;
			if (log3.IsEnabled())
			{
				log3.TransactionPromoted(tx.TransactionTraceId, distributedTransaction.TransactionTraceId);
			}
			PromoteEnlistmentsAndOutcome(tx);
		}
	}
}
