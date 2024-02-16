namespace System.ComponentModel.Design.Serialization;

public class ResolveNameEventArgs : EventArgs
{
	public string? Name { get; }

	public object? Value { get; set; }

	public ResolveNameEventArgs(string? name)
	{
		Name = name;
		Value = null;
	}
}
