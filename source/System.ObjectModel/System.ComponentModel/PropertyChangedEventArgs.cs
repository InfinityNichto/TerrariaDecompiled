namespace System.ComponentModel;

public class PropertyChangedEventArgs : EventArgs
{
	public virtual string? PropertyName { get; }

	public PropertyChangedEventArgs(string? propertyName)
	{
		PropertyName = propertyName;
	}
}
