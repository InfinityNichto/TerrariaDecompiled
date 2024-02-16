namespace System.Runtime.InteropServices;

[AttributeUsage(AttributeTargets.Field, Inherited = false)]
public sealed class TypeLibVarAttribute : Attribute
{
	public TypeLibVarFlags Value { get; }

	public TypeLibVarAttribute(TypeLibVarFlags flags)
	{
		Value = flags;
	}

	public TypeLibVarAttribute(short flags)
	{
		Value = (TypeLibVarFlags)flags;
	}
}
