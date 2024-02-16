namespace System.Transactions;

internal sealed class VolatileEnlistmentDone : VolatileEnlistmentEnded
{
	internal override void EnterState(InternalEnlistment enlistment)
	{
		enlistment.State = this;
	}

	internal override void ChangeStatePreparing(InternalEnlistment enlistment)
	{
		enlistment.CheckComplete();
	}
}
