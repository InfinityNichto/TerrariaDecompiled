namespace System.Threading;

internal sealed class QueueUserWorkItemCallback : QueueUserWorkItemCallbackBase
{
	private WaitCallback _callback;

	private readonly object _state;

	private readonly ExecutionContext _context;

	private static readonly Action<QueueUserWorkItemCallback> s_executionContextShim = delegate(QueueUserWorkItemCallback quwi)
	{
		WaitCallback callback = quwi._callback;
		quwi._callback = null;
		callback(quwi._state);
	};

	internal QueueUserWorkItemCallback(WaitCallback callback, object state, ExecutionContext context)
	{
		_callback = callback;
		_state = state;
		_context = context;
	}

	public override void Execute()
	{
		base.Execute();
		ExecutionContext.RunForThreadPoolUnsafe(_context, s_executionContextShim, in this);
	}
}
internal sealed class QueueUserWorkItemCallback<TState> : QueueUserWorkItemCallbackBase
{
	private Action<TState> _callback;

	private readonly TState _state;

	private readonly ExecutionContext _context;

	internal QueueUserWorkItemCallback(Action<TState> callback, TState state, ExecutionContext context)
	{
		_callback = callback;
		_state = state;
		_context = context;
	}

	public override void Execute()
	{
		base.Execute();
		Action<TState> callback = _callback;
		_callback = null;
		ExecutionContext.RunForThreadPoolUnsafe(_context, callback, in _state);
	}
}
