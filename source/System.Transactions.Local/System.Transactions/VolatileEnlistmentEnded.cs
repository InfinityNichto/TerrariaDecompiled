namespace System.Transactions;

internal class VolatileEnlistmentEnded : VolatileEnlistmentState
{
	internal override void EnterState(InternalEnlistment enlistment)
	{
		enlistment.State = this;
	}

	internal override void ChangeStatePreparing(InternalEnlistment enlistment)
	{
	}

	internal override void InternalAborted(InternalEnlistment enlistment)
	{
	}

	internal override void InternalCommitted(InternalEnlistment enlistment)
	{
	}

	internal override void InternalIndoubt(InternalEnlistment enlistment)
	{
	}

	internal override void InDoubt(InternalEnlistment enlistment, Exception e)
	{
	}
}
