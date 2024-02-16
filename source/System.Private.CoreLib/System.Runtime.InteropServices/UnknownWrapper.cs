namespace System.Runtime.InteropServices;

public sealed class UnknownWrapper
{
	public object? WrappedObject { get; }

	public UnknownWrapper(object? obj)
	{
		WrappedObject = obj;
	}
}
