namespace System.ComponentModel;

public class PropertyChangingEventArgs : EventArgs
{
	public virtual string? PropertyName { get; }

	public PropertyChangingEventArgs(string? propertyName)
	{
		PropertyName = propertyName;
	}
}
