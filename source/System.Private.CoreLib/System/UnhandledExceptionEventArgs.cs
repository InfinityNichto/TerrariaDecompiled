namespace System;

public class UnhandledExceptionEventArgs : EventArgs
{
	private readonly object _exception;

	private readonly bool _isTerminating;

	public object ExceptionObject => _exception;

	public bool IsTerminating => _isTerminating;

	public UnhandledExceptionEventArgs(object exception, bool isTerminating)
	{
		_exception = exception;
		_isTerminating = isTerminating;
	}
}
