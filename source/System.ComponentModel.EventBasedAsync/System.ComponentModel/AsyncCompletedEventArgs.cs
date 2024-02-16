using System.Reflection;

namespace System.ComponentModel;

public class AsyncCompletedEventArgs : EventArgs
{
	public bool Cancelled { get; }

	public Exception? Error { get; }

	public object? UserState { get; }

	public AsyncCompletedEventArgs(Exception? error, bool cancelled, object? userState)
	{
		Cancelled = cancelled;
		Error = error;
		UserState = userState;
	}

	protected void RaiseExceptionIfNecessary()
	{
		if (Error != null)
		{
			throw new TargetInvocationException(System.SR.Async_ExceptionOccurred, Error);
		}
		if (Cancelled)
		{
			throw new InvalidOperationException(System.SR.Async_OperationCancelled);
		}
	}
}
