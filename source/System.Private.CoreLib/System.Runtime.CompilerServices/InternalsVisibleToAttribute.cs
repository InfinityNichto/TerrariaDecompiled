namespace System.Runtime.CompilerServices;

[AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true, Inherited = false)]
public sealed class InternalsVisibleToAttribute : Attribute
{
	public string AssemblyName { get; }

	public bool AllInternalsVisible { get; set; } = true;


	public InternalsVisibleToAttribute(string assemblyName)
	{
		AssemblyName = assemblyName;
	}
}
