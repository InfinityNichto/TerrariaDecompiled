using System.Transactions.Distributed;

namespace System.Transactions;

internal sealed class TransactionStatePSPEOperation : TransactionState
{
	internal override void EnterState(InternalTransaction tx)
	{
		throw new InvalidOperationException();
	}

	internal override TransactionStatus get_Status(InternalTransaction tx)
	{
		throw TransactionException.CreateTransactionStateException(tx._innerException, tx.DistributedTxId);
	}

	internal void PSPEInitialize(InternalTransaction tx, IPromotableSinglePhaseNotification promotableSinglePhaseNotification, Guid promoterType)
	{
		CommonEnterState(tx);
		try
		{
			promotableSinglePhaseNotification.Initialize();
			tx._promoterType = promoterType;
		}
		finally
		{
			TransactionState.TransactionStateActive.CommonEnterState(tx);
		}
	}

	internal void Phase0PSPEInitialize(InternalTransaction tx, IPromotableSinglePhaseNotification promotableSinglePhaseNotification, Guid promoterType)
	{
		CommonEnterState(tx);
		try
		{
			promotableSinglePhaseNotification.Initialize();
			tx._promoterType = promoterType;
		}
		finally
		{
			TransactionState.TransactionStatePhase0.CommonEnterState(tx);
		}
	}

	internal DistributedTransaction PSPEPromote(InternalTransaction tx)
	{
		bool flag = true;
		TransactionState state = tx.State;
		CommonEnterState(tx);
		DistributedTransaction distributedTransaction = null;
		try
		{
			if (tx._attemptingPSPEPromote)
			{
				throw TransactionException.CreateInvalidOperationException(TraceSourceType.TraceSourceLtm, System.SR.PromotedReturnedInvalidValue, null, tx.DistributedTxId);
			}
			tx._attemptingPSPEPromote = true;
			byte[] array = tx._promoter.Promote();
			if (tx._promoterType != TransactionInterop.PromoterTypeDtc)
			{
				if (array == null)
				{
					throw TransactionException.CreateInvalidOperationException(TraceSourceType.TraceSourceLtm, System.SR.PromotedReturnedInvalidValue, null, tx.DistributedTxId);
				}
				tx.promotedToken = array;
				return null;
			}
			if (array == null)
			{
				if (tx.PromotedTransaction == null)
				{
					throw TransactionException.CreateInvalidOperationException(TraceSourceType.TraceSourceLtm, System.SR.PromotedReturnedInvalidValue, null, tx.DistributedTxId);
				}
				flag = false;
				distributedTransaction = tx.PromotedTransaction;
			}
			if (distributedTransaction == null)
			{
				try
				{
					distributedTransaction = TransactionInterop.GetDistributedTransactionFromTransmitterPropagationToken(array);
				}
				catch (ArgumentException innerException)
				{
					throw TransactionException.CreateInvalidOperationException(TraceSourceType.TraceSourceLtm, System.SR.PromotedReturnedInvalidValue, innerException, tx.DistributedTxId);
				}
				if (TransactionManager.FindPromotedTransaction(distributedTransaction.Identifier) != null)
				{
					distributedTransaction.Dispose();
					throw TransactionException.CreateInvalidOperationException(TraceSourceType.TraceSourceLtm, System.SR.PromotedTransactionExists, null, tx.DistributedTxId);
				}
			}
		}
		finally
		{
			tx._attemptingPSPEPromote = false;
			if (flag)
			{
				state.CommonEnterState(tx);
			}
		}
		return distributedTransaction;
	}

	internal override Enlistment PromoteAndEnlistDurable(InternalTransaction tx, Guid resourceManagerIdentifier, IPromotableSinglePhaseNotification promotableNotification, ISinglePhaseNotification enlistmentNotification, EnlistmentOptions enlistmentOptions, Transaction atomicTransaction)
	{
		if (!tx._attemptingPSPEPromote)
		{
			throw TransactionException.CreateTransactionStateException(tx._innerException, tx.DistributedTxId);
		}
		if (promotableNotification != tx._promoter)
		{
			throw TransactionException.CreateInvalidOperationException(TraceSourceType.TraceSourceLtm, System.SR.InvalidIPromotableSinglePhaseNotificationSpecified, null, tx.DistributedTxId);
		}
		tx._durableEnlistment = null;
		tx._promoteState = TransactionState.TransactionStatePromoted;
		tx._promoteState.EnterState(tx);
		Enlistment enlistment = tx.State.EnlistDurable(tx, resourceManagerIdentifier, enlistmentNotification, enlistmentOptions, atomicTransaction);
		tx._durableEnlistment = enlistment.InternalEnlistment;
		return enlistment;
	}

	internal override void SetDistributedTransactionId(InternalTransaction tx, IPromotableSinglePhaseNotification promotableNotification, Guid distributedTransactionIdentifier)
	{
		if (!tx._attemptingPSPEPromote)
		{
			throw TransactionException.CreateTransactionStateException(tx._innerException, tx.DistributedTxId);
		}
		if (promotableNotification != tx._promoter)
		{
			throw TransactionException.CreateInvalidOperationException(TraceSourceType.TraceSourceLtm, System.SR.InvalidIPromotableSinglePhaseNotificationSpecified, null, tx.DistributedTxId);
		}
		tx._distributedTransactionIdentifierNonMSDTC = distributedTransactionIdentifier;
	}
}
