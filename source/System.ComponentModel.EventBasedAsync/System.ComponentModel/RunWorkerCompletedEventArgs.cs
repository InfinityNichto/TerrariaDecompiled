namespace System.ComponentModel;

public class RunWorkerCompletedEventArgs : AsyncCompletedEventArgs
{
	private readonly object _result;

	public object? Result
	{
		get
		{
			RaiseExceptionIfNecessary();
			return _result;
		}
	}

	[EditorBrowsable(EditorBrowsableState.Never)]
	public new object? UserState => base.UserState;

	public RunWorkerCompletedEventArgs(object? result, Exception? error, bool cancelled)
		: base(error, cancelled, null)
	{
		_result = result;
	}
}
