using System.Threading;

namespace System.Transactions;

internal sealed class TransactionStatePromotedP0Aborting : TransactionStatePromotedAborting
{
	internal override void EnterState(InternalTransaction tx)
	{
		CommonEnterState(tx);
		ChangeStatePromotedAborted(tx);
		if (tx._phase0Volatiles.VolatileDemux._preparingEnlistment != null)
		{
			Monitor.Exit(tx);
			try
			{
				tx._phase0Volatiles.VolatileDemux._promotedEnlistment.ForceRollback();
				return;
			}
			finally
			{
				Monitor.Enter(tx);
			}
		}
		tx.PromotedTransaction.Rollback();
	}

	internal override void Phase0VolatilePrepareDone(InternalTransaction tx)
	{
	}
}
