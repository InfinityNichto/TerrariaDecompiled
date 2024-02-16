using System.Transactions.Distributed;

namespace System.Transactions;

internal sealed class TransactionStateDelegatedNonMSDTC : TransactionStatePromotedNonMSDTCBase
{
	internal override void EnterState(InternalTransaction tx)
	{
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
			if (tx.promotedToken == null)
			{
				tx.State.ChangeStateAbortedDuringPromotion(tx);
			}
		}
	}
}
