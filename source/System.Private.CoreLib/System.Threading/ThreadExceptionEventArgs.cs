namespace System.Threading;

public class ThreadExceptionEventArgs : EventArgs
{
	private readonly Exception m_exception;

	public Exception Exception => m_exception;

	public ThreadExceptionEventArgs(Exception t)
	{
		m_exception = t;
	}
}
