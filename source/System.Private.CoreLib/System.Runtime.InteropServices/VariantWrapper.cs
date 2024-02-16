namespace System.Runtime.InteropServices;

public sealed class VariantWrapper
{
	public object? WrappedObject { get; }

	public VariantWrapper(object? obj)
	{
		WrappedObject = obj;
	}
}
