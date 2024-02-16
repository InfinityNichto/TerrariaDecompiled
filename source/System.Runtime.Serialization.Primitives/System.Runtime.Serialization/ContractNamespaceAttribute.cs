namespace System.Runtime.Serialization;

[AttributeUsage(AttributeTargets.Assembly | AttributeTargets.Module, Inherited = false, AllowMultiple = true)]
public sealed class ContractNamespaceAttribute : Attribute
{
	public string? ClrNamespace { get; set; }

	public string ContractNamespace { get; }

	public ContractNamespaceAttribute(string contractNamespace)
	{
		ContractNamespace = contractNamespace;
	}
}
