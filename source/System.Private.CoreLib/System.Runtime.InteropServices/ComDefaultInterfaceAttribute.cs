namespace System.Runtime.InteropServices;

[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class ComDefaultInterfaceAttribute : Attribute
{
	public Type Value { get; }

	public ComDefaultInterfaceAttribute(Type defaultInterface)
	{
		Value = defaultInterface;
	}
}
