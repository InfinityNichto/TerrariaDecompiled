using System.Configuration.Assemblies;

namespace System.Reflection;

[AttributeUsage(AttributeTargets.Assembly, Inherited = false)]
public sealed class AssemblyAlgorithmIdAttribute : Attribute
{
	[CLSCompliant(false)]
	public uint AlgorithmId { get; }

	public AssemblyAlgorithmIdAttribute(AssemblyHashAlgorithm algorithmId)
	{
		AlgorithmId = (uint)algorithmId;
	}

	[CLSCompliant(false)]
	public AssemblyAlgorithmIdAttribute(uint algorithmId)
	{
		AlgorithmId = algorithmId;
	}
}
