namespace System.Transactions;

internal sealed class DurableEnlistmentEnded : DurableEnlistmentState
{
	internal override void EnterState(InternalEnlistment enlistment)
	{
		enlistment.State = this;
	}

	internal override void InternalAborted(InternalEnlistment enlistment)
	{
	}

	internal override void InDoubt(InternalEnlistment enlistment, Exception e)
	{
	}
}
