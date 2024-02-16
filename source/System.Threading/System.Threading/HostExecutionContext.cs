namespace System.Threading;

public class HostExecutionContext : IDisposable
{
	protected internal object? State { get; set; }

	public HostExecutionContext()
	{
	}

	public HostExecutionContext(object? state)
	{
		State = state;
	}

	public virtual HostExecutionContext CreateCopy()
	{
		return new HostExecutionContext(State);
	}

	public void Dispose()
	{
		Dispose(disposing: true);
	}

	public virtual void Dispose(bool disposing)
	{
	}
}
