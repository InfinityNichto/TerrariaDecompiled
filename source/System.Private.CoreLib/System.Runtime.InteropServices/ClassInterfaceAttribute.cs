namespace System.Runtime.InteropServices;

[AttributeUsage(AttributeTargets.Assembly | AttributeTargets.Class, Inherited = false)]
public sealed class ClassInterfaceAttribute : Attribute
{
	public ClassInterfaceType Value { get; }

	public ClassInterfaceAttribute(ClassInterfaceType classInterfaceType)
	{
		Value = classInterfaceType;
	}

	public ClassInterfaceAttribute(short classInterfaceType)
	{
		Value = (ClassInterfaceType)classInterfaceType;
	}
}
