namespace System.Reflection;

[AttributeUsage(AttributeTargets.Assembly, AllowMultiple = false, Inherited = false)]
public sealed class ObfuscateAssemblyAttribute : Attribute
{
	public bool AssemblyIsPrivate { get; }

	public bool StripAfterObfuscation { get; set; } = true;


	public ObfuscateAssemblyAttribute(bool assemblyIsPrivate)
	{
		AssemblyIsPrivate = assemblyIsPrivate;
	}
}
