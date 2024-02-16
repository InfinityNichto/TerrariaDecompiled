using System.Threading;

namespace System.Transactions;

internal abstract class TransactionState
{
	private static TransactionStateActive s_transactionStateActive;

	private static TransactionStateSubordinateActive s_transactionStateSubordinateActive;

	private static TransactionStatePhase0 s_transactionStatePhase0;

	private static TransactionStateVolatilePhase1 s_transactionStateVolatilePhase1;

	private static TransactionStateVolatileSPC s_transactionStateVolatileSPC;

	private static TransactionStateSPC s_transactionStateSPC;

	private static TransactionStateAborted s_transactionStateAborted;

	private static TransactionStateCommitted s_transactionStateCommitted;

	private static TransactionStateInDoubt s_transactionStateInDoubt;

	private static TransactionStatePromoted s_transactionStatePromoted;

	private static TransactionStateNonCommittablePromoted s_transactionStateNonCommittablePromoted;

	private static TransactionStatePromotedP0Wave s_transactionStatePromotedP0Wave;

	private static TransactionStatePromotedCommitting s_transactionStatePromotedCommitting;

	private static TransactionStatePromotedP0Aborting s_transactionStatePromotedP0Aborting;

	private static TransactionStatePromotedAborted s_transactionStatePromotedAborted;

	private static TransactionStatePromotedCommitted s_transactionStatePromotedCommitted;

	private static TransactionStatePromotedIndoubt s_transactionStatePromotedIndoubt;

	private static TransactionStateDelegated s_transactionStateDelegated;

	private static TransactionStateDelegatedSubordinate s_transactionStateDelegatedSubordinate;

	private static TransactionStateDelegatedP0Wave s_transactionStateDelegatedP0Wave;

	private static TransactionStateDelegatedCommitting s_transactionStateDelegatedCommitting;

	private static TransactionStateDelegatedAborting s_transactionStateDelegatedAborting;

	private static TransactionStatePSPEOperation s_transactionStatePSPEOperation;

	private static TransactionStateDelegatedNonMSDTC s_transactionStateDelegatedNonMSDTC;

	private static TransactionStatePromotedNonMSDTCPhase0 s_transactionStatePromotedNonMSDTCPhase0;

	private static TransactionStatePromotedNonMSDTCVolatilePhase1 s_transactionStatePromotedNonMSDTCVolatilePhase1;

	private static TransactionStatePromotedNonMSDTCSinglePhaseCommit s_transactionStatePromotedNonMSDTCSinglePhaseCommit;

	private static TransactionStatePromotedNonMSDTCAborted s_transactionStatePromotedNonMSDTCAborted;

	private static TransactionStatePromotedNonMSDTCCommitted s_transactionStatePromotedNonMSDTCCommitted;

	private static TransactionStatePromotedNonMSDTCIndoubt s_transactionStatePromotedNonMSDTCIndoubt;

	internal static object s_classSyncObject;

	internal static TransactionStateActive TransactionStateActive => LazyInitializer.EnsureInitialized(ref s_transactionStateActive, ref s_classSyncObject, () => new TransactionStateActive());

	internal static TransactionStateSubordinateActive TransactionStateSubordinateActive => LazyInitializer.EnsureInitialized(ref s_transactionStateSubordinateActive, ref s_classSyncObject, () => new TransactionStateSubordinateActive());

	internal static TransactionStatePSPEOperation TransactionStatePSPEOperation => LazyInitializer.EnsureInitialized(ref s_transactionStatePSPEOperation, ref s_classSyncObject, () => new TransactionStatePSPEOperation());

	protected static TransactionStatePhase0 TransactionStatePhase0 => LazyInitializer.EnsureInitialized(ref s_transactionStatePhase0, ref s_classSyncObject, () => new TransactionStatePhase0());

	protected static TransactionStateVolatilePhase1 TransactionStateVolatilePhase1 => LazyInitializer.EnsureInitialized(ref s_transactionStateVolatilePhase1, ref s_classSyncObject, () => new TransactionStateVolatilePhase1());

	protected static TransactionStateVolatileSPC TransactionStateVolatileSPC => LazyInitializer.EnsureInitialized(ref s_transactionStateVolatileSPC, ref s_classSyncObject, () => new TransactionStateVolatileSPC());

	protected static TransactionStateSPC TransactionStateSPC => LazyInitializer.EnsureInitialized(ref s_transactionStateSPC, ref s_classSyncObject, () => new TransactionStateSPC());

	protected static TransactionStateAborted TransactionStateAborted => LazyInitializer.EnsureInitialized(ref s_transactionStateAborted, ref s_classSyncObject, () => new TransactionStateAborted());

	protected static TransactionStateCommitted TransactionStateCommitted => LazyInitializer.EnsureInitialized(ref s_transactionStateCommitted, ref s_classSyncObject, () => new TransactionStateCommitted());

	protected static TransactionStateInDoubt TransactionStateInDoubt => LazyInitializer.EnsureInitialized(ref s_transactionStateInDoubt, ref s_classSyncObject, () => new TransactionStateInDoubt());

	internal static TransactionStatePromoted TransactionStatePromoted => LazyInitializer.EnsureInitialized(ref s_transactionStatePromoted, ref s_classSyncObject, () => new TransactionStatePromoted());

	internal static TransactionStateNonCommittablePromoted TransactionStateNonCommittablePromoted => LazyInitializer.EnsureInitialized(ref s_transactionStateNonCommittablePromoted, ref s_classSyncObject, () => new TransactionStateNonCommittablePromoted());

	protected static TransactionStatePromotedP0Wave TransactionStatePromotedP0Wave => LazyInitializer.EnsureInitialized(ref s_transactionStatePromotedP0Wave, ref s_classSyncObject, () => new TransactionStatePromotedP0Wave());

	protected static TransactionStatePromotedCommitting TransactionStatePromotedCommitting => LazyInitializer.EnsureInitialized(ref s_transactionStatePromotedCommitting, ref s_classSyncObject, () => new TransactionStatePromotedCommitting());

	protected static TransactionStatePromotedP0Aborting TransactionStatePromotedP0Aborting => LazyInitializer.EnsureInitialized(ref s_transactionStatePromotedP0Aborting, ref s_classSyncObject, () => new TransactionStatePromotedP0Aborting());

	protected static TransactionStatePromotedAborted TransactionStatePromotedAborted => LazyInitializer.EnsureInitialized(ref s_transactionStatePromotedAborted, ref s_classSyncObject, () => new TransactionStatePromotedAborted());

	protected static TransactionStatePromotedCommitted TransactionStatePromotedCommitted => LazyInitializer.EnsureInitialized(ref s_transactionStatePromotedCommitted, ref s_classSyncObject, () => new TransactionStatePromotedCommitted());

	protected static TransactionStatePromotedIndoubt TransactionStatePromotedIndoubt => LazyInitializer.EnsureInitialized(ref s_transactionStatePromotedIndoubt, ref s_classSyncObject, () => new TransactionStatePromotedIndoubt());

	protected static TransactionStateDelegated TransactionStateDelegated => LazyInitializer.EnsureInitialized(ref s_transactionStateDelegated, ref s_classSyncObject, () => new TransactionStateDelegated());

	internal static TransactionStateDelegatedSubordinate TransactionStateDelegatedSubordinate => LazyInitializer.EnsureInitialized(ref s_transactionStateDelegatedSubordinate, ref s_classSyncObject, () => new TransactionStateDelegatedSubordinate());

	protected static TransactionStateDelegatedP0Wave TransactionStateDelegatedP0Wave => LazyInitializer.EnsureInitialized(ref s_transactionStateDelegatedP0Wave, ref s_classSyncObject, () => new TransactionStateDelegatedP0Wave());

	protected static TransactionStateDelegatedCommitting TransactionStateDelegatedCommitting => LazyInitializer.EnsureInitialized(ref s_transactionStateDelegatedCommitting, ref s_classSyncObject, () => new TransactionStateDelegatedCommitting());

	protected static TransactionStateDelegatedAborting TransactionStateDelegatedAborting => LazyInitializer.EnsureInitialized(ref s_transactionStateDelegatedAborting, ref s_classSyncObject, () => new TransactionStateDelegatedAborting());

	protected static TransactionStateDelegatedNonMSDTC TransactionStateDelegatedNonMSDTC => LazyInitializer.EnsureInitialized(ref s_transactionStateDelegatedNonMSDTC, ref s_classSyncObject, () => new TransactionStateDelegatedNonMSDTC());

	protected static TransactionStatePromotedNonMSDTCPhase0 TransactionStatePromotedNonMSDTCPhase0 => LazyInitializer.EnsureInitialized(ref s_transactionStatePromotedNonMSDTCPhase0, ref s_classSyncObject, () => new TransactionStatePromotedNonMSDTCPhase0());

	protected static TransactionStatePromotedNonMSDTCVolatilePhase1 TransactionStatePromotedNonMSDTCVolatilePhase1 => LazyInitializer.EnsureInitialized(ref s_transactionStatePromotedNonMSDTCVolatilePhase1, ref s_classSyncObject, () => new TransactionStatePromotedNonMSDTCVolatilePhase1());

	protected static TransactionStatePromotedNonMSDTCSinglePhaseCommit TransactionStatePromotedNonMSDTCSinglePhaseCommit => LazyInitializer.EnsureInitialized(ref s_transactionStatePromotedNonMSDTCSinglePhaseCommit, ref s_classSyncObject, () => new TransactionStatePromotedNonMSDTCSinglePhaseCommit());

	protected static TransactionStatePromotedNonMSDTCAborted TransactionStatePromotedNonMSDTCAborted => LazyInitializer.EnsureInitialized(ref s_transactionStatePromotedNonMSDTCAborted, ref s_classSyncObject, () => new TransactionStatePromotedNonMSDTCAborted());

	protected static TransactionStatePromotedNonMSDTCCommitted TransactionStatePromotedNonMSDTCCommitted => LazyInitializer.EnsureInitialized(ref s_transactionStatePromotedNonMSDTCCommitted, ref s_classSyncObject, () => new TransactionStatePromotedNonMSDTCCommitted());

	protected static TransactionStatePromotedNonMSDTCIndoubt TransactionStatePromotedNonMSDTCIndoubt => LazyInitializer.EnsureInitialized(ref s_transactionStatePromotedNonMSDTCIndoubt, ref s_classSyncObject, () => new TransactionStatePromotedNonMSDTCIndoubt());

	internal void CommonEnterState(InternalTransaction tx)
	{
		tx.State = this;
	}

	internal abstract void EnterState(InternalTransaction tx);

	internal virtual void BeginCommit(InternalTransaction tx, bool asyncCommit, AsyncCallback asyncCallback, object asyncState)
	{
		throw TransactionException.CreateTransactionStateException(tx._innerException, tx.DistributedTxId);
	}

	internal virtual void EndCommit(InternalTransaction tx)
	{
		throw TransactionException.CreateTransactionStateException(tx._innerException, tx.DistributedTxId);
	}

	internal virtual void Rollback(InternalTransaction tx, Exception e)
	{
		throw TransactionException.CreateTransactionStateException(tx._innerException, tx.DistributedTxId);
	}

	internal virtual Enlistment EnlistDurable(InternalTransaction tx, Guid resourceManagerIdentifier, IEnlistmentNotification enlistmentNotification, EnlistmentOptions enlistmentOptions, Transaction atomicTransaction)
	{
		throw TransactionException.CreateTransactionStateException(tx._innerException, tx.DistributedTxId);
	}

	internal virtual Enlistment EnlistDurable(InternalTransaction tx, Guid resourceManagerIdentifier, ISinglePhaseNotification enlistmentNotification, EnlistmentOptions enlistmentOptions, Transaction atomicTransaction)
	{
		throw TransactionException.CreateTransactionStateException(tx._innerException, tx.DistributedTxId);
	}

	internal virtual Enlistment EnlistVolatile(InternalTransaction tx, IEnlistmentNotification enlistmentNotification, EnlistmentOptions enlistmentOptions, Transaction atomicTransaction)
	{
		throw TransactionException.CreateTransactionStateException(tx._innerException, tx.DistributedTxId);
	}

	internal virtual Enlistment EnlistVolatile(InternalTransaction tx, ISinglePhaseNotification enlistmentNotification, EnlistmentOptions enlistmentOptions, Transaction atomicTransaction)
	{
		throw TransactionException.CreateTransactionStateException(tx._innerException, tx.DistributedTxId);
	}

	internal virtual void CheckForFinishedTransaction(InternalTransaction tx)
	{
	}

	internal virtual Guid get_Identifier(InternalTransaction tx)
	{
		return Guid.Empty;
	}

	internal abstract TransactionStatus get_Status(InternalTransaction tx);

	internal virtual void AddOutcomeRegistrant(InternalTransaction tx, TransactionCompletedEventHandler transactionCompletedDelegate)
	{
		throw TransactionException.CreateTransactionStateException(tx._innerException, tx.DistributedTxId);
	}

	internal virtual bool EnlistPromotableSinglePhase(InternalTransaction tx, IPromotableSinglePhaseNotification promotableSinglePhaseNotification, Transaction atomicTransaction, Guid promoterType)
	{
		throw TransactionException.CreateTransactionStateException(tx._innerException, tx.DistributedTxId);
	}

	internal virtual void CompleteBlockingClone(InternalTransaction tx)
	{
	}

	internal virtual void CompleteAbortingClone(InternalTransaction tx)
	{
	}

	internal virtual void CreateBlockingClone(InternalTransaction tx)
	{
		throw TransactionException.CreateTransactionStateException(tx._innerException, tx.DistributedTxId);
	}

	internal virtual void CreateAbortingClone(InternalTransaction tx)
	{
		throw TransactionException.CreateTransactionStateException(tx._innerException, tx.DistributedTxId);
	}

	internal virtual void ChangeStateTransactionAborted(InternalTransaction tx, Exception e)
	{
		TransactionsEtwProvider log = TransactionsEtwProvider.Log;
		if (log.IsEnabled())
		{
			log.TransactionExceptionTrace(TransactionExceptionType.InvalidOperationException, tx?.TransactionTraceId.TransactionIdentifier ?? string.Empty, e.ToString());
		}
		throw new InvalidOperationException();
	}

	internal virtual void ChangeStateTransactionCommitted(InternalTransaction tx)
	{
		TransactionsEtwProvider log = TransactionsEtwProvider.Log;
		if (log.IsEnabled())
		{
			log.TransactionExceptionTrace(TransactionExceptionType.InvalidOperationException, tx?.TransactionTraceId.TransactionIdentifier ?? string.Empty, string.Empty);
		}
		throw new InvalidOperationException();
	}

	internal virtual void InDoubtFromEnlistment(InternalTransaction tx)
	{
		TransactionsEtwProvider log = TransactionsEtwProvider.Log;
		if (log.IsEnabled())
		{
			log.TransactionExceptionTrace(TransactionExceptionType.InvalidOperationException, tx?.TransactionTraceId.TransactionIdentifier ?? string.Empty, string.Empty);
		}
		throw new InvalidOperationException();
	}

	internal virtual void ChangeStatePromotedAborted(InternalTransaction tx)
	{
		TransactionsEtwProvider log = TransactionsEtwProvider.Log;
		if (log.IsEnabled())
		{
			log.TransactionExceptionTrace(TransactionExceptionType.InvalidOperationException, tx?.TransactionTraceId.TransactionIdentifier ?? string.Empty, string.Empty);
		}
		throw new InvalidOperationException();
	}

	internal virtual void ChangeStatePromotedCommitted(InternalTransaction tx)
	{
		TransactionsEtwProvider log = TransactionsEtwProvider.Log;
		if (log.IsEnabled())
		{
			log.TransactionExceptionTrace(TransactionExceptionType.InvalidOperationException, tx?.TransactionTraceId.TransactionIdentifier ?? string.Empty, string.Empty);
		}
		throw new InvalidOperationException();
	}

	internal virtual void ChangeStateAbortedDuringPromotion(InternalTransaction tx)
	{
		TransactionsEtwProvider log = TransactionsEtwProvider.Log;
		if (log.IsEnabled())
		{
			log.TransactionExceptionTrace(TransactionExceptionType.InvalidOperationException, tx?.TransactionTraceId.TransactionIdentifier ?? string.Empty, string.Empty);
		}
		throw new InvalidOperationException();
	}

	internal virtual void Timeout(InternalTransaction tx)
	{
	}

	internal virtual void Phase0VolatilePrepareDone(InternalTransaction tx)
	{
		throw TransactionException.CreateTransactionStateException(tx._innerException, tx.DistributedTxId);
	}

	internal virtual void Phase1VolatilePrepareDone(InternalTransaction tx)
	{
		throw TransactionException.CreateTransactionStateException(tx._innerException, tx.DistributedTxId);
	}

	internal virtual void RestartCommitIfNeeded(InternalTransaction tx)
	{
		TransactionsEtwProvider log = TransactionsEtwProvider.Log;
		if (log.IsEnabled())
		{
			log.TransactionExceptionTrace(TransactionExceptionType.InvalidOperationException, tx?.TransactionTraceId.TransactionIdentifier ?? string.Empty, string.Empty);
		}
		throw new InvalidOperationException();
	}

	internal virtual bool ContinuePhase0Prepares()
	{
		return false;
	}

	internal virtual bool ContinuePhase1Prepares()
	{
		return false;
	}

	internal virtual void Promote(InternalTransaction tx)
	{
		throw TransactionException.CreateTransactionStateException(tx._innerException, tx.DistributedTxId);
	}

	internal virtual byte[] PromotedToken(InternalTransaction tx)
	{
		throw TransactionException.CreateTransactionStateException(tx._innerException, tx.DistributedTxId);
	}

	internal virtual Enlistment PromoteAndEnlistDurable(InternalTransaction tx, Guid resourceManagerIdentifier, IPromotableSinglePhaseNotification promotableNotification, ISinglePhaseNotification enlistmentNotification, EnlistmentOptions enlistmentOptions, Transaction atomicTransaction)
	{
		throw TransactionException.CreateTransactionStateException(tx._innerException, tx.DistributedTxId);
	}

	internal virtual void SetDistributedTransactionId(InternalTransaction tx, IPromotableSinglePhaseNotification promotableNotification, Guid distributedTransactionIdentifier)
	{
		throw TransactionException.CreateTransactionStateException(tx._innerException, tx.DistributedTxId);
	}

	internal virtual void DisposeRoot(InternalTransaction tx)
	{
	}

	internal virtual bool IsCompleted(InternalTransaction tx)
	{
		tx._needPulse = true;
		return false;
	}

	protected void AddVolatileEnlistment(ref VolatileEnlistmentSet enlistments, Enlistment enlistment)
	{
		if (enlistments._volatileEnlistmentCount == enlistments._volatileEnlistmentSize)
		{
			InternalEnlistment[] array = new InternalEnlistment[enlistments._volatileEnlistmentSize + 8];
			if (enlistments._volatileEnlistmentSize > 0)
			{
				Array.Copy(enlistments._volatileEnlistments, 0, array, 0, enlistments._volatileEnlistmentSize);
			}
			enlistments._volatileEnlistmentSize += 8;
			enlistments._volatileEnlistments = array;
		}
		enlistments._volatileEnlistments[enlistments._volatileEnlistmentCount] = enlistment.InternalEnlistment;
		enlistments._volatileEnlistmentCount++;
		VolatileEnlistmentState.VolatileEnlistmentActive.EnterState(enlistments._volatileEnlistments[enlistments._volatileEnlistmentCount - 1]);
	}
}
