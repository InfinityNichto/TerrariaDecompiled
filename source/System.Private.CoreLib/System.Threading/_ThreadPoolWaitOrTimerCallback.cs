namespace System.Threading;

internal sealed class _ThreadPoolWaitOrTimerCallback
{
	private readonly WaitOrTimerCallback _waitOrTimerCallback;

	private readonly ExecutionContext _executionContext;

	private readonly object _state;

	private static readonly ContextCallback _ccbt = WaitOrTimerCallback_Context_t;

	private static readonly ContextCallback _ccbf = WaitOrTimerCallback_Context_f;

	internal _ThreadPoolWaitOrTimerCallback(WaitOrTimerCallback waitOrTimerCallback, object state, bool flowExecutionContext)
	{
		_waitOrTimerCallback = waitOrTimerCallback;
		_state = state;
		if (flowExecutionContext)
		{
			_executionContext = ExecutionContext.Capture();
		}
	}

	private static void WaitOrTimerCallback_Context_t(object state)
	{
		WaitOrTimerCallback_Context(state, timedOut: true);
	}

	private static void WaitOrTimerCallback_Context_f(object state)
	{
		WaitOrTimerCallback_Context(state, timedOut: false);
	}

	private static void WaitOrTimerCallback_Context(object state, bool timedOut)
	{
		_ThreadPoolWaitOrTimerCallback threadPoolWaitOrTimerCallback = (_ThreadPoolWaitOrTimerCallback)state;
		threadPoolWaitOrTimerCallback._waitOrTimerCallback(threadPoolWaitOrTimerCallback._state, timedOut);
	}

	internal static void PerformWaitOrTimerCallback(_ThreadPoolWaitOrTimerCallback helper, bool timedOut)
	{
		ExecutionContext executionContext = helper._executionContext;
		if (executionContext == null)
		{
			WaitOrTimerCallback waitOrTimerCallback = helper._waitOrTimerCallback;
			waitOrTimerCallback(helper._state, timedOut);
		}
		else
		{
			ExecutionContext.Run(executionContext, timedOut ? _ccbt : _ccbf, helper);
		}
	}
}
