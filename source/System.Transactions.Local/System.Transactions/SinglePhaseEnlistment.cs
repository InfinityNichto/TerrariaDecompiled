namespace System.Transactions;

public class SinglePhaseEnlistment : Enlistment
{
	internal SinglePhaseEnlistment(InternalEnlistment enlistment)
		: base(enlistment)
	{
	}

	public void Aborted()
	{
		TransactionsEtwProvider log = TransactionsEtwProvider.Log;
		if (log.IsEnabled())
		{
			log.MethodEnter(TraceSourceType.TraceSourceLtm, this, "Aborted");
			log.EnlistmentAborted(_internalEnlistment);
		}
		lock (_internalEnlistment.SyncRoot)
		{
			_internalEnlistment.State.Aborted(_internalEnlistment, null);
		}
		if (log.IsEnabled())
		{
			log.MethodExit(TraceSourceType.TraceSourceLtm, this, "Aborted");
		}
	}

	public void Aborted(Exception? e)
	{
		TransactionsEtwProvider log = TransactionsEtwProvider.Log;
		if (log.IsEnabled())
		{
			log.MethodEnter(TraceSourceType.TraceSourceLtm, this, "Aborted");
			log.EnlistmentAborted(_internalEnlistment);
		}
		lock (_internalEnlistment.SyncRoot)
		{
			_internalEnlistment.State.Aborted(_internalEnlistment, e);
		}
		if (log.IsEnabled())
		{
			log.MethodExit(TraceSourceType.TraceSourceLtm, this, "Aborted");
		}
	}

	public void Committed()
	{
		TransactionsEtwProvider log = TransactionsEtwProvider.Log;
		if (log.IsEnabled())
		{
			log.MethodEnter(TraceSourceType.TraceSourceLtm, this, "Committed");
			log.EnlistmentCommitted(_internalEnlistment);
		}
		lock (_internalEnlistment.SyncRoot)
		{
			_internalEnlistment.State.Committed(_internalEnlistment);
		}
		if (log.IsEnabled())
		{
			log.MethodExit(TraceSourceType.TraceSourceLtm, this, "Committed");
		}
	}

	public void InDoubt()
	{
		TransactionsEtwProvider log = TransactionsEtwProvider.Log;
		if (log.IsEnabled())
		{
			log.MethodEnter(TraceSourceType.TraceSourceLtm, this, "InDoubt");
		}
		lock (_internalEnlistment.SyncRoot)
		{
			if (log.IsEnabled())
			{
				log.EnlistmentInDoubt(_internalEnlistment);
			}
			_internalEnlistment.State.InDoubt(_internalEnlistment, null);
		}
		if (log.IsEnabled())
		{
			log.MethodExit(TraceSourceType.TraceSourceLtm, this, "InDoubt");
		}
	}

	public void InDoubt(Exception? e)
	{
		TransactionsEtwProvider log = TransactionsEtwProvider.Log;
		if (log.IsEnabled())
		{
			log.MethodEnter(TraceSourceType.TraceSourceLtm, this, "InDoubt");
		}
		lock (_internalEnlistment.SyncRoot)
		{
			if (log.IsEnabled())
			{
				log.EnlistmentInDoubt(_internalEnlistment);
			}
			_internalEnlistment.State.InDoubt(_internalEnlistment, e);
		}
		if (log.IsEnabled())
		{
			log.MethodExit(TraceSourceType.TraceSourceLtm, this, "InDoubt");
		}
	}
}
