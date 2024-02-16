namespace System.Transactions;

internal abstract class VolatileDemultiplexer : IEnlistmentNotificationInternal
{
	protected InternalTransaction _transaction;

	internal IPromotedEnlistment _promotedEnlistment;

	internal IPromotedEnlistment _preparingEnlistment;

	public VolatileDemultiplexer(InternalTransaction transaction)
	{
		_transaction = transaction;
	}

	internal void BroadcastCommitted(ref VolatileEnlistmentSet volatiles)
	{
		for (int i = 0; i < volatiles._volatileEnlistmentCount; i++)
		{
			volatiles._volatileEnlistments[i]._twoPhaseState.InternalCommitted(volatiles._volatileEnlistments[i]);
		}
	}

	internal void BroadcastRollback(ref VolatileEnlistmentSet volatiles)
	{
		for (int i = 0; i < volatiles._volatileEnlistmentCount; i++)
		{
			volatiles._volatileEnlistments[i]._twoPhaseState.InternalAborted(volatiles._volatileEnlistments[i]);
		}
	}

	internal void BroadcastInDoubt(ref VolatileEnlistmentSet volatiles)
	{
		for (int i = 0; i < volatiles._volatileEnlistmentCount; i++)
		{
			volatiles._volatileEnlistments[i]._twoPhaseState.InternalIndoubt(volatiles._volatileEnlistments[i]);
		}
	}
}
