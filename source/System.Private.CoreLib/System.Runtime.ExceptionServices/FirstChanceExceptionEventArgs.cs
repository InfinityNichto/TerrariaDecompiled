namespace System.Runtime.ExceptionServices;

public class FirstChanceExceptionEventArgs : EventArgs
{
	public Exception Exception { get; }

	public FirstChanceExceptionEventArgs(Exception exception)
	{
		Exception = exception;
	}
}
