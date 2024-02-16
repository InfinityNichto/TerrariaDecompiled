namespace System.Transactions;

internal sealed class VolatileEnlistmentPreparingAborting : VolatileEnlistmentState
{
	internal override void EnterState(InternalEnlistment enlistment)
	{
		enlistment.State = this;
	}

	internal override void EnlistmentDone(InternalEnlistment enlistment)
	{
		VolatileEnlistmentState.VolatileEnlistmentEnded.EnterState(enlistment);
	}

	internal override void Prepared(InternalEnlistment enlistment)
	{
		VolatileEnlistmentState.VolatileEnlistmentAborting.EnterState(enlistment);
		enlistment.FinishEnlistment();
	}

	internal override void ForceRollback(InternalEnlistment enlistment, Exception e)
	{
		VolatileEnlistmentState.VolatileEnlistmentEnded.EnterState(enlistment);
		if (enlistment.Transaction._innerException == null)
		{
			enlistment.Transaction._innerException = e;
		}
		enlistment.FinishEnlistment();
	}

	internal override void InternalAborted(InternalEnlistment enlistment)
	{
	}
}
