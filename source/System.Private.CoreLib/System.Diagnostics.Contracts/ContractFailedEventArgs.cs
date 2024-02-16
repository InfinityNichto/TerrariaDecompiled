namespace System.Diagnostics.Contracts;

public sealed class ContractFailedEventArgs : EventArgs
{
	private readonly ContractFailureKind _failureKind;

	private readonly string _message;

	private readonly string _condition;

	private readonly Exception _originalException;

	private bool _handled;

	private bool _unwind;

	internal Exception thrownDuringHandler;

	public string? Message => _message;

	public string? Condition => _condition;

	public ContractFailureKind FailureKind => _failureKind;

	public Exception? OriginalException => _originalException;

	public bool Handled => _handled;

	public bool Unwind => _unwind;

	public ContractFailedEventArgs(ContractFailureKind failureKind, string? message, string? condition, Exception? originalException)
	{
		_failureKind = failureKind;
		_message = message;
		_condition = condition;
		_originalException = originalException;
	}

	public void SetHandled()
	{
		_handled = true;
	}

	public void SetUnwind()
	{
		_unwind = true;
	}
}
