namespace System.Runtime.InteropServices;

public sealed class BStrWrapper
{
	public string? WrappedObject { get; }

	public BStrWrapper(string? value)
	{
		WrappedObject = value;
	}

	public BStrWrapper(object? value)
	{
		WrappedObject = (string)value;
	}
}
