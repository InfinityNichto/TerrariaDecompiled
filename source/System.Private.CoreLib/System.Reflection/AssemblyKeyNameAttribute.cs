namespace System.Reflection;

[AttributeUsage(AttributeTargets.Assembly, Inherited = false)]
public sealed class AssemblyKeyNameAttribute : Attribute
{
	public string KeyName { get; }

	public AssemblyKeyNameAttribute(string keyName)
	{
		KeyName = keyName;
	}
}
