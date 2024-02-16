namespace System.Runtime.CompilerServices;

[AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true)]
public sealed class DependencyAttribute : Attribute
{
	public string DependentAssembly { get; }

	public LoadHint LoadHint { get; }

	public DependencyAttribute(string dependentAssemblyArgument, LoadHint loadHintArgument)
	{
		DependentAssembly = dependentAssemblyArgument;
		LoadHint = loadHintArgument;
	}
}
