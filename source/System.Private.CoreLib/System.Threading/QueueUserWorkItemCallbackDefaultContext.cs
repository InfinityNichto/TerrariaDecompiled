namespace System.Threading;

internal sealed class QueueUserWorkItemCallbackDefaultContext : QueueUserWorkItemCallbackBase
{
	private WaitCallback _callback;

	private readonly object _state;

	internal QueueUserWorkItemCallbackDefaultContext(WaitCallback callback, object state)
	{
		_callback = callback;
		_state = state;
	}

	public override void Execute()
	{
		base.Execute();
		WaitCallback callback = _callback;
		_callback = null;
		callback(_state);
	}
}
internal sealed class QueueUserWorkItemCallbackDefaultContext<TState> : QueueUserWorkItemCallbackBase
{
	private Action<TState> _callback;

	private readonly TState _state;

	internal QueueUserWorkItemCallbackDefaultContext(Action<TState> callback, TState state)
	{
		_callback = callback;
		_state = state;
	}

	public override void Execute()
	{
		base.Execute();
		Action<TState> callback = _callback;
		_callback = null;
		callback(_state);
	}
}
