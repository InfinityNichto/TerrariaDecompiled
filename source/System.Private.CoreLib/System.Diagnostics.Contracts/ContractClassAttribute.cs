namespace System.Diagnostics.Contracts;

[Conditional("CONTRACTS_FULL")]
[Conditional("DEBUG")]
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface | AttributeTargets.Delegate, AllowMultiple = false, Inherited = false)]
public sealed class ContractClassAttribute : Attribute
{
	private readonly Type _typeWithContracts;

	public Type TypeContainingContracts => _typeWithContracts;

	public ContractClassAttribute(Type typeContainingContracts)
	{
		_typeWithContracts = typeContainingContracts;
	}
}
