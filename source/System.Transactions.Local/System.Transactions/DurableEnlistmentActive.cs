namespace System.Transactions;

internal sealed class DurableEnlistmentActive : DurableEnlistmentState
{
	internal override void EnterState(InternalEnlistment enlistment)
	{
		enlistment.State = this;
	}

	internal override void EnlistmentDone(InternalEnlistment enlistment)
	{
		DurableEnlistmentState.DurableEnlistmentEnded.EnterState(enlistment);
	}

	internal override void InternalAborted(InternalEnlistment enlistment)
	{
		DurableEnlistmentState.DurableEnlistmentAborting.EnterState(enlistment);
	}

	internal override void ChangeStateCommitting(InternalEnlistment enlistment)
	{
		DurableEnlistmentState.DurableEnlistmentCommitting.EnterState(enlistment);
	}

	internal override void ChangeStatePromoted(InternalEnlistment enlistment, IPromotedEnlistment promotedEnlistment)
	{
		enlistment.PromotedEnlistment = promotedEnlistment;
		EnlistmentState.EnlistmentStatePromoted.EnterState(enlistment);
	}

	internal override void ChangeStateDelegated(InternalEnlistment enlistment)
	{
		DurableEnlistmentState.DurableEnlistmentDelegated.EnterState(enlistment);
	}
}
