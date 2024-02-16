namespace System.Runtime.InteropServices;

[AttributeUsage(AttributeTargets.Interface, Inherited = false)]
public sealed class InterfaceTypeAttribute : Attribute
{
	public ComInterfaceType Value { get; }

	public InterfaceTypeAttribute(ComInterfaceType interfaceType)
	{
		Value = interfaceType;
	}

	public InterfaceTypeAttribute(short interfaceType)
	{
		Value = (ComInterfaceType)interfaceType;
	}
}
