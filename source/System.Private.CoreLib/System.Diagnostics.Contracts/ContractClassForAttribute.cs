namespace System.Diagnostics.Contracts;

[Conditional("CONTRACTS_FULL")]
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public sealed class ContractClassForAttribute : Attribute
{
	private readonly Type _typeIAmAContractFor;

	public Type TypeContractsAreFor => _typeIAmAContractFor;

	public ContractClassForAttribute(Type typeContractsAreFor)
	{
		_typeIAmAContractFor = typeContractsAreFor;
	}
}
