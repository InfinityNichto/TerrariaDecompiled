namespace System.Transactions;

public class PreparingEnlistment : Enlistment
{
	internal PreparingEnlistment(InternalEnlistment enlistment)
		: base(enlistment)
	{
	}

	public void Prepared()
	{
		TransactionsEtwProvider log = TransactionsEtwProvider.Log;
		if (log.IsEnabled())
		{
			log.MethodEnter(TraceSourceType.TraceSourceLtm, this, "Prepared");
			log.EnlistmentPrepared(_internalEnlistment);
		}
		lock (_internalEnlistment.SyncRoot)
		{
			_internalEnlistment.State.Prepared(_internalEnlistment);
		}
		if (log.IsEnabled())
		{
			log.MethodExit(TraceSourceType.TraceSourceLtm, this, "Prepared");
		}
	}

	public void ForceRollback()
	{
		TransactionsEtwProvider log = TransactionsEtwProvider.Log;
		if (log.IsEnabled())
		{
			log.MethodEnter(TraceSourceType.TraceSourceLtm, this, "ForceRollback");
			log.EnlistmentForceRollback(_internalEnlistment);
		}
		lock (_internalEnlistment.SyncRoot)
		{
			_internalEnlistment.State.ForceRollback(_internalEnlistment, null);
		}
		if (log.IsEnabled())
		{
			log.MethodExit(TraceSourceType.TraceSourceLtm, this, "ForceRollback");
		}
	}

	public void ForceRollback(Exception? e)
	{
		TransactionsEtwProvider log = TransactionsEtwProvider.Log;
		if (log.IsEnabled())
		{
			log.MethodEnter(TraceSourceType.TraceSourceLtm, this, "ForceRollback");
			log.EnlistmentForceRollback(_internalEnlistment);
		}
		lock (_internalEnlistment.SyncRoot)
		{
			_internalEnlistment.State.ForceRollback(_internalEnlistment, e);
		}
		if (log.IsEnabled())
		{
			log.MethodExit(TraceSourceType.TraceSourceLtm, this, "ForceRollback");
		}
	}

	public byte[] RecoveryInformation()
	{
		TransactionsEtwProvider log = TransactionsEtwProvider.Log;
		if (log.IsEnabled())
		{
			log.MethodEnter(TraceSourceType.TraceSourceLtm, this, "RecoveryInformation");
		}
		try
		{
			lock (_internalEnlistment.SyncRoot)
			{
				return _internalEnlistment.State.RecoveryInformation(_internalEnlistment);
			}
		}
		finally
		{
			if (log.IsEnabled())
			{
				log.MethodExit(TraceSourceType.TraceSourceLtm, this, "RecoveryInformation");
			}
		}
	}
}
