namespace System.Transactions;

internal sealed class VolatileEnlistmentActive : VolatileEnlistmentState
{
	internal override void EnterState(InternalEnlistment enlistment)
	{
		enlistment.State = this;
	}

	internal override void EnlistmentDone(InternalEnlistment enlistment)
	{
		VolatileEnlistmentState.VolatileEnlistmentDone.EnterState(enlistment);
		enlistment.FinishEnlistment();
	}

	internal override void ChangeStatePreparing(InternalEnlistment enlistment)
	{
		VolatileEnlistmentState.VolatileEnlistmentPreparing.EnterState(enlistment);
	}

	internal override void ChangeStateSinglePhaseCommit(InternalEnlistment enlistment)
	{
		VolatileEnlistmentState.VolatileEnlistmentSPC.EnterState(enlistment);
	}

	internal override void InternalAborted(InternalEnlistment enlistment)
	{
		VolatileEnlistmentState.VolatileEnlistmentAborting.EnterState(enlistment);
	}
}
