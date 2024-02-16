namespace System.Threading.Tasks;

public class UnobservedTaskExceptionEventArgs : EventArgs
{
	private readonly AggregateException m_exception;

	internal bool m_observed;

	public bool Observed => m_observed;

	public AggregateException Exception => m_exception;

	public UnobservedTaskExceptionEventArgs(AggregateException exception)
	{
		m_exception = exception;
	}

	public void SetObserved()
	{
		m_observed = true;
	}
}
