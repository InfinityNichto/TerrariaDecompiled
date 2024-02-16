namespace System.Reflection;

internal static class TypeAttributesExtensions
{
	public static bool IsForwarder(this TypeAttributes flags)
	{
		return (flags & (TypeAttributes)2097152) != 0;
	}

	public static bool IsNested(this TypeAttributes flags)
	{
		return (flags & TypeAttributes.NestedFamANDAssem) != 0;
	}
}
