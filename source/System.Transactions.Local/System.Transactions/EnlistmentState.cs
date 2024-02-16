using System.Threading;

namespace System.Transactions;

internal abstract class EnlistmentState
{
	internal static EnlistmentStatePromoted _enlistmentStatePromoted;

	private static object s_classSyncObject;

	internal static EnlistmentStatePromoted EnlistmentStatePromoted => LazyInitializer.EnsureInitialized(ref _enlistmentStatePromoted, ref s_classSyncObject, () => new EnlistmentStatePromoted());

	internal abstract void EnterState(InternalEnlistment enlistment);

	internal virtual void EnlistmentDone(InternalEnlistment enlistment)
	{
		throw TransactionException.CreateEnlistmentStateException(null, enlistment?.DistributedTxId ?? Guid.Empty);
	}

	internal virtual void Prepared(InternalEnlistment enlistment)
	{
		throw TransactionException.CreateEnlistmentStateException(null, enlistment?.DistributedTxId ?? Guid.Empty);
	}

	internal virtual void ForceRollback(InternalEnlistment enlistment, Exception e)
	{
		throw TransactionException.CreateEnlistmentStateException(null, enlistment?.DistributedTxId ?? Guid.Empty);
	}

	internal virtual void Committed(InternalEnlistment enlistment)
	{
		throw TransactionException.CreateEnlistmentStateException(null, enlistment?.DistributedTxId ?? Guid.Empty);
	}

	internal virtual void Aborted(InternalEnlistment enlistment, Exception e)
	{
		throw TransactionException.CreateEnlistmentStateException(null, enlistment?.DistributedTxId ?? Guid.Empty);
	}

	internal virtual void InDoubt(InternalEnlistment enlistment, Exception e)
	{
		throw TransactionException.CreateEnlistmentStateException(null, enlistment?.DistributedTxId ?? Guid.Empty);
	}

	internal virtual byte[] RecoveryInformation(InternalEnlistment enlistment)
	{
		throw TransactionException.CreateEnlistmentStateException(null, enlistment?.DistributedTxId ?? Guid.Empty);
	}

	internal virtual void InternalAborted(InternalEnlistment enlistment)
	{
		throw TransactionException.CreateEnlistmentStateException(null, enlistment?.DistributedTxId ?? Guid.Empty);
	}

	internal virtual void InternalCommitted(InternalEnlistment enlistment)
	{
		throw TransactionException.CreateEnlistmentStateException(null, enlistment?.DistributedTxId ?? Guid.Empty);
	}

	internal virtual void InternalIndoubt(InternalEnlistment enlistment)
	{
		throw TransactionException.CreateEnlistmentStateException(null, enlistment?.DistributedTxId ?? Guid.Empty);
	}

	internal virtual void ChangeStateCommitting(InternalEnlistment enlistment)
	{
		throw TransactionException.CreateEnlistmentStateException(null, enlistment?.DistributedTxId ?? Guid.Empty);
	}

	internal virtual void ChangeStatePromoted(InternalEnlistment enlistment, IPromotedEnlistment promotedEnlistment)
	{
		throw TransactionException.CreateEnlistmentStateException(null, enlistment?.DistributedTxId ?? Guid.Empty);
	}

	internal virtual void ChangeStateDelegated(InternalEnlistment enlistment)
	{
		throw TransactionException.CreateEnlistmentStateException(null, enlistment?.DistributedTxId ?? Guid.Empty);
	}

	internal virtual void ChangeStatePreparing(InternalEnlistment enlistment)
	{
		throw TransactionException.CreateEnlistmentStateException(null, enlistment?.DistributedTxId ?? Guid.Empty);
	}

	internal virtual void ChangeStateSinglePhaseCommit(InternalEnlistment enlistment)
	{
		throw TransactionException.CreateEnlistmentStateException(null, enlistment?.DistributedTxId ?? Guid.Empty);
	}
}
