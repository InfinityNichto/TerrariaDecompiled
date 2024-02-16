namespace System.Transactions;

internal sealed class TransactionStatePromotedNonMSDTCPhase0 : TransactionStatePromotedNonMSDTCBase
{
	internal override void EnterState(InternalTransaction tx)
	{
		CommonEnterState(tx);
		int volatileEnlistmentCount = tx._phase0Volatiles._volatileEnlistmentCount;
		int dependentClones = tx._phase0Volatiles._dependentClones;
		tx._phase0VolatileWaveCount = volatileEnlistmentCount;
		if (tx._phase0Volatiles._preparedVolatileEnlistments < volatileEnlistmentCount + dependentClones)
		{
			for (int i = 0; i < volatileEnlistmentCount; i++)
			{
				tx._phase0Volatiles._volatileEnlistments[i]._twoPhaseState.ChangeStatePreparing(tx._phase0Volatiles._volatileEnlistments[i]);
				if (!tx.State.ContinuePhase0Prepares())
				{
					break;
				}
			}
		}
		else
		{
			TransactionState.TransactionStatePromotedNonMSDTCVolatilePhase1.EnterState(tx);
		}
	}

	internal override void BeginCommit(InternalTransaction tx, bool asyncCommit, AsyncCallback asyncCallback, object asyncState)
	{
		throw TransactionException.CreateTransactionStateException(tx._innerException, tx.DistributedTxId);
	}

	internal override void Rollback(InternalTransaction tx, Exception e)
	{
		ChangeStateTransactionAborted(tx, e);
	}

	internal override void Phase0VolatilePrepareDone(InternalTransaction tx)
	{
		int volatileEnlistmentCount = tx._phase0Volatiles._volatileEnlistmentCount;
		int dependentClones = tx._phase0Volatiles._dependentClones;
		tx._phase0VolatileWaveCount = volatileEnlistmentCount;
		if (tx._phase0Volatiles._preparedVolatileEnlistments < volatileEnlistmentCount + dependentClones)
		{
			for (int i = 0; i < volatileEnlistmentCount; i++)
			{
				tx._phase0Volatiles._volatileEnlistments[i]._twoPhaseState.ChangeStatePreparing(tx._phase0Volatiles._volatileEnlistments[i]);
				if (!tx.State.ContinuePhase0Prepares())
				{
					break;
				}
			}
		}
		else
		{
			TransactionState.TransactionStatePromotedNonMSDTCVolatilePhase1.EnterState(tx);
		}
	}

	internal override void Phase1VolatilePrepareDone(InternalTransaction tx)
	{
	}

	internal override bool ContinuePhase0Prepares()
	{
		return true;
	}
}
