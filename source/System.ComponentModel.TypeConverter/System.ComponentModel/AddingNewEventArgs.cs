namespace System.ComponentModel;

public class AddingNewEventArgs : EventArgs
{
	public object? NewObject { get; set; }

	public AddingNewEventArgs()
	{
	}

	public AddingNewEventArgs(object? newObject)
	{
		NewObject = newObject;
	}
}
