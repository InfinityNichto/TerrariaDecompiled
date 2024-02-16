namespace System.Transactions;

internal sealed class VolatileEnlistmentPrepared : VolatileEnlistmentState
{
	internal override void EnterState(InternalEnlistment enlistment)
	{
		enlistment.State = this;
	}

	internal override void InternalAborted(InternalEnlistment enlistment)
	{
		VolatileEnlistmentState.VolatileEnlistmentAborting.EnterState(enlistment);
	}

	internal override void InternalCommitted(InternalEnlistment enlistment)
	{
		VolatileEnlistmentState.VolatileEnlistmentCommitting.EnterState(enlistment);
	}

	internal override void InternalIndoubt(InternalEnlistment enlistment)
	{
		VolatileEnlistmentState.VolatileEnlistmentInDoubt.EnterState(enlistment);
	}

	internal override void ChangeStatePreparing(InternalEnlistment enlistment)
	{
	}
}
