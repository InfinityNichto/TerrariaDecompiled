namespace System.ComponentModel;

public class RefreshEventArgs : EventArgs
{
	public object? ComponentChanged { get; }

	public Type? TypeChanged { get; }

	public RefreshEventArgs(object? componentChanged)
	{
		ComponentChanged = componentChanged;
		TypeChanged = componentChanged?.GetType();
	}

	public RefreshEventArgs(Type? typeChanged)
	{
		TypeChanged = typeChanged;
	}
}
