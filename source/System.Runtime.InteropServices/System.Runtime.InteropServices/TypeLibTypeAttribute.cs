namespace System.Runtime.InteropServices;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Enum | AttributeTargets.Interface, Inherited = false)]
public sealed class TypeLibTypeAttribute : Attribute
{
	public TypeLibTypeFlags Value { get; }

	public TypeLibTypeAttribute(TypeLibTypeFlags flags)
	{
		Value = flags;
	}

	public TypeLibTypeAttribute(short flags)
	{
		Value = (TypeLibTypeFlags)flags;
	}
}
