namespace System.Threading;

public class HostExecutionContextManager
{
	private sealed class HostExecutionContextSwitcher
	{
		public readonly HostExecutionContext _currentContext;

		public AsyncLocal<bool> _asyncLocal;

		public HostExecutionContextSwitcher(HostExecutionContext currentContext)
		{
			_currentContext = currentContext;
			_asyncLocal = new AsyncLocal<bool>();
			_asyncLocal.Value = true;
		}
	}

	[ThreadStatic]
	private static HostExecutionContext t_currentContext;

	public virtual HostExecutionContext? Capture()
	{
		return null;
	}

	public virtual object SetHostExecutionContext(HostExecutionContext hostExecutionContext)
	{
		if (hostExecutionContext == null)
		{
			throw new InvalidOperationException(System.SR.HostExecutionContextManager_InvalidOperation_NotNewCaptureContext);
		}
		HostExecutionContextSwitcher result = new HostExecutionContextSwitcher(hostExecutionContext);
		t_currentContext = hostExecutionContext;
		return result;
	}

	public virtual void Revert(object previousState)
	{
		if (!(previousState is HostExecutionContextSwitcher hostExecutionContextSwitcher))
		{
			throw new InvalidOperationException(System.SR.HostExecutionContextManager_InvalidOperation_CannotOverrideSetWithoutRevert);
		}
		if (t_currentContext != hostExecutionContextSwitcher._currentContext || hostExecutionContextSwitcher._asyncLocal == null || !hostExecutionContextSwitcher._asyncLocal.Value)
		{
			throw new InvalidOperationException(System.SR.HostExecutionContextManager_InvalidOperation_CannotUseSwitcherOtherThread);
		}
		hostExecutionContextSwitcher._asyncLocal = null;
		t_currentContext = null;
	}
}
