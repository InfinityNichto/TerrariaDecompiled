namespace System.ComponentModel;

public class CancelEventArgs : EventArgs
{
	public bool Cancel { get; set; }

	public CancelEventArgs()
	{
	}

	public CancelEventArgs(bool cancel)
	{
		Cancel = cancel;
	}
}
