namespace System.ComponentModel;

public class DoWorkEventArgs : CancelEventArgs
{
	public object? Argument { get; }

	public object? Result { get; set; }

	public DoWorkEventArgs(object? argument)
	{
		Argument = argument;
	}
}
