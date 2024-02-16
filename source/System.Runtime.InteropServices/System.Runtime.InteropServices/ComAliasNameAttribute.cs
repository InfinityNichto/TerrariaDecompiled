namespace System.Runtime.InteropServices;

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter | AttributeTargets.ReturnValue, Inherited = false)]
public sealed class ComAliasNameAttribute : Attribute
{
	public string Value { get; }

	public ComAliasNameAttribute(string alias)
	{
		Value = alias;
	}
}
