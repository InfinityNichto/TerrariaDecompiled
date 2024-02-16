using System.Collections;
using System.Transactions.Distributed;

namespace System.Transactions;

internal class TransactionStatePromoted : TransactionStatePromotedBase
{
	internal override void EnterState(InternalTransaction tx)
	{
		tx.SetPromoterTypeToMSDTC();
		if (tx._outcomeSource._isoLevel == IsolationLevel.Snapshot)
		{
			throw TransactionException.CreateInvalidOperationException(TraceSourceType.TraceSourceLtm, System.SR.CannotPromoteSnapshot, null);
		}
		CommonEnterState(tx);
		DistributedCommittableTransaction distributedCommittableTransaction = null;
		try
		{
			TimeSpan timeSpan;
			if (tx.AbsoluteTimeout == long.MaxValue)
			{
				timeSpan = TimeSpan.Zero;
			}
			else
			{
				timeSpan = TransactionManager.TransactionTable.RecalcTimeout(tx);
				if (timeSpan <= TimeSpan.Zero)
				{
					return;
				}
			}
			TransactionOptions options = default(TransactionOptions);
			options.IsolationLevel = tx._outcomeSource._isoLevel;
			options.Timeout = timeSpan;
			distributedCommittableTransaction = TransactionManager.DistributedTransactionManager.CreateTransaction(options);
			distributedCommittableTransaction.SavedLtmPromotedTransaction = tx._outcomeSource;
			TransactionsEtwProvider log = TransactionsEtwProvider.Log;
			if (log.IsEnabled())
			{
				log.TransactionPromoted(tx.TransactionTraceId, distributedCommittableTransaction.TransactionTraceId);
			}
		}
		catch (TransactionException innerException)
		{
			TransactionException exception = (TransactionException)(tx._innerException = innerException);
			TransactionsEtwProvider log2 = TransactionsEtwProvider.Log;
			if (log2.IsEnabled())
			{
				log2.ExceptionConsumed(exception);
			}
			return;
		}
		finally
		{
			if (distributedCommittableTransaction == null)
			{
				tx.State.ChangeStateAbortedDuringPromotion(tx);
			}
		}
		tx.PromotedTransaction = distributedCommittableTransaction;
		Hashtable promotedTransactionTable = TransactionManager.PromotedTransactionTable;
		lock (promotedTransactionTable)
		{
			tx._finalizedObject = new FinalizedObject(tx, distributedCommittableTransaction.Identifier);
			WeakReference value = new WeakReference(tx._outcomeSource, trackResurrection: false);
			promotedTransactionTable[distributedCommittableTransaction.Identifier] = value;
		}
		TransactionManager.FireDistributedTransactionStarted(tx._outcomeSource);
		PromoteEnlistmentsAndOutcome(tx);
	}

	protected bool PromotePhaseVolatiles(InternalTransaction tx, ref VolatileEnlistmentSet volatiles, bool phase0)
	{
		if (volatiles._volatileEnlistmentCount + volatiles._dependentClones > 0)
		{
			if (phase0)
			{
				volatiles.VolatileDemux = new Phase0VolatileDemultiplexer(tx);
			}
			else
			{
				volatiles.VolatileDemux = new Phase1VolatileDemultiplexer(tx);
			}
			volatiles.VolatileDemux._promotedEnlistment = tx.PromotedTransaction.EnlistVolatile(volatiles.VolatileDemux, phase0 ? EnlistmentOptions.EnlistDuringPrepareRequired : EnlistmentOptions.None);
		}
		return true;
	}

	internal virtual bool PromoteDurable(InternalTransaction tx)
	{
		if (tx._durableEnlistment != null)
		{
			InternalEnlistment durableEnlistment = tx._durableEnlistment;
			IPromotedEnlistment promotedEnlistment = tx.PromotedTransaction.EnlistDurable(durableEnlistment.ResourceManagerIdentifier, (DurableInternalEnlistment)durableEnlistment, durableEnlistment.SinglePhaseNotification != null, EnlistmentOptions.None);
			tx._durableEnlistment.State.ChangeStatePromoted(tx._durableEnlistment, promotedEnlistment);
		}
		return true;
	}

	internal virtual void PromoteEnlistmentsAndOutcome(InternalTransaction tx)
	{
		bool flag = false;
		tx.PromotedTransaction.RealTransaction.InternalTransaction = tx;
		try
		{
			flag = PromotePhaseVolatiles(tx, ref tx._phase0Volatiles, phase0: true);
		}
		catch (TransactionException innerException)
		{
			TransactionException exception = (TransactionException)(tx._innerException = innerException);
			TransactionsEtwProvider log = TransactionsEtwProvider.Log;
			if (log.IsEnabled())
			{
				log.ExceptionConsumed(exception);
			}
			return;
		}
		finally
		{
			if (!flag)
			{
				tx.PromotedTransaction.Rollback();
				tx.State.ChangeStateAbortedDuringPromotion(tx);
			}
		}
		flag = false;
		try
		{
			flag = PromotePhaseVolatiles(tx, ref tx._phase1Volatiles, phase0: false);
		}
		catch (TransactionException innerException2)
		{
			TransactionException exception2 = (TransactionException)(tx._innerException = innerException2);
			TransactionsEtwProvider log2 = TransactionsEtwProvider.Log;
			if (log2.IsEnabled())
			{
				log2.ExceptionConsumed(exception2);
			}
			return;
		}
		finally
		{
			if (!flag)
			{
				tx.PromotedTransaction.Rollback();
				tx.State.ChangeStateAbortedDuringPromotion(tx);
			}
		}
		flag = false;
		try
		{
			flag = PromoteDurable(tx);
		}
		catch (TransactionException innerException3)
		{
			TransactionException exception3 = (TransactionException)(tx._innerException = innerException3);
			TransactionsEtwProvider log3 = TransactionsEtwProvider.Log;
			if (log3.IsEnabled())
			{
				log3.ExceptionConsumed(exception3);
			}
		}
		finally
		{
			if (!flag)
			{
				tx.PromotedTransaction.Rollback();
				tx.State.ChangeStateAbortedDuringPromotion(tx);
			}
		}
	}

	internal override void DisposeRoot(InternalTransaction tx)
	{
		tx.State.Rollback(tx, null);
	}
}
