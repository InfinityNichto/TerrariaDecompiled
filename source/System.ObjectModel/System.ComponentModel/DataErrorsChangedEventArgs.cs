namespace System.ComponentModel;

public class DataErrorsChangedEventArgs : EventArgs
{
	public virtual string? PropertyName { get; }

	public DataErrorsChangedEventArgs(string? propertyName)
	{
		PropertyName = propertyName;
	}
}
