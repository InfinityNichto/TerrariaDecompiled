namespace System.IO;

public class ErrorEventArgs : EventArgs
{
	private readonly Exception _exception;

	public ErrorEventArgs(Exception exception)
	{
		_exception = exception;
	}

	public virtual Exception GetException()
	{
		return _exception;
	}
}
