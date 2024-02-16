using System.Threading;

namespace System.Transactions;

internal sealed class EnlistmentStatePromoted : EnlistmentState
{
	internal override void EnterState(InternalEnlistment enlistment)
	{
		enlistment.State = this;
	}

	internal override void EnlistmentDone(InternalEnlistment enlistment)
	{
		Monitor.Exit(enlistment.SyncRoot);
		try
		{
			enlistment.PromotedEnlistment.EnlistmentDone();
		}
		finally
		{
			Monitor.Enter(enlistment.SyncRoot);
		}
	}

	internal override void Prepared(InternalEnlistment enlistment)
	{
		Monitor.Exit(enlistment.SyncRoot);
		try
		{
			enlistment.PromotedEnlistment.Prepared();
		}
		finally
		{
			Monitor.Enter(enlistment.SyncRoot);
		}
	}

	internal override void ForceRollback(InternalEnlistment enlistment, Exception e)
	{
		Monitor.Exit(enlistment.SyncRoot);
		try
		{
			enlistment.PromotedEnlistment.ForceRollback(e);
		}
		finally
		{
			Monitor.Enter(enlistment.SyncRoot);
		}
	}

	internal override void Committed(InternalEnlistment enlistment)
	{
		Monitor.Exit(enlistment.SyncRoot);
		try
		{
			enlistment.PromotedEnlistment.Committed();
		}
		finally
		{
			Monitor.Enter(enlistment.SyncRoot);
		}
	}

	internal override void Aborted(InternalEnlistment enlistment, Exception e)
	{
		Monitor.Exit(enlistment.SyncRoot);
		try
		{
			enlistment.PromotedEnlistment.Aborted(e);
		}
		finally
		{
			Monitor.Enter(enlistment.SyncRoot);
		}
	}

	internal override void InDoubt(InternalEnlistment enlistment, Exception e)
	{
		Monitor.Exit(enlistment.SyncRoot);
		try
		{
			enlistment.PromotedEnlistment.InDoubt(e);
		}
		finally
		{
			Monitor.Enter(enlistment.SyncRoot);
		}
	}

	internal override byte[] RecoveryInformation(InternalEnlistment enlistment)
	{
		Monitor.Exit(enlistment.SyncRoot);
		try
		{
			return enlistment.PromotedEnlistment.GetRecoveryInformation();
		}
		finally
		{
			Monitor.Enter(enlistment.SyncRoot);
		}
	}
}
