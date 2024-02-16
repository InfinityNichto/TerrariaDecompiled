namespace System.Runtime.ConstrainedExecution;

[Obsolete("The Constrained Execution Region (CER) feature is not supported.", DiagnosticId = "SYSLIB0004", UrlFormat = "https://aka.ms/dotnet-warnings/{0}")]
[AttributeUsage(AttributeTargets.Assembly | AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Constructor | AttributeTargets.Method | AttributeTargets.Interface, Inherited = false)]
public sealed class ReliabilityContractAttribute : Attribute
{
	public Consistency ConsistencyGuarantee { get; }

	public Cer Cer { get; }

	public ReliabilityContractAttribute(Consistency consistencyGuarantee, Cer cer)
	{
		ConsistencyGuarantee = consistencyGuarantee;
		Cer = cer;
	}
}
