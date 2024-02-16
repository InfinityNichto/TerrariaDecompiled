namespace System.Reflection;

[AttributeUsage(AttributeTargets.Assembly, Inherited = false)]
public sealed class AssemblyKeyFileAttribute : Attribute
{
	public string KeyFile { get; }

	public AssemblyKeyFileAttribute(string keyFile)
	{
		KeyFile = keyFile;
	}
}
