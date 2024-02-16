namespace System.Runtime.InteropServices;

[AttributeUsage(AttributeTargets.Method, Inherited = false)]
public sealed class TypeLibFuncAttribute : Attribute
{
	public TypeLibFuncFlags Value { get; }

	public TypeLibFuncAttribute(TypeLibFuncFlags flags)
	{
		Value = flags;
	}

	public TypeLibFuncAttribute(short flags)
	{
		Value = (TypeLibFuncFlags)flags;
	}
}
