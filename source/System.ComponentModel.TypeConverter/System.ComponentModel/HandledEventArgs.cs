namespace System.ComponentModel;

public class HandledEventArgs : EventArgs
{
	public bool Handled { get; set; }

	public HandledEventArgs()
		: this(defaultHandledValue: false)
	{
	}

	public HandledEventArgs(bool defaultHandledValue)
	{
		Handled = defaultHandledValue;
	}
}
