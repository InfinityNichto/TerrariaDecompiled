namespace System.Runtime.InteropServices;

[AttributeUsage(AttributeTargets.Struct | AttributeTargets.Enum | AttributeTargets.Interface | AttributeTargets.Delegate, AllowMultiple = false, Inherited = false)]
public sealed class TypeIdentifierAttribute : Attribute
{
	public string? Scope { get; }

	public string? Identifier { get; }

	public TypeIdentifierAttribute()
	{
	}

	public TypeIdentifierAttribute(string? scope, string? identifier)
	{
		Scope = scope;
		Identifier = identifier;
	}
}
