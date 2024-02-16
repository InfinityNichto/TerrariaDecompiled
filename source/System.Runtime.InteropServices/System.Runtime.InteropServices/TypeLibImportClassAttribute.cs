namespace System.Runtime.InteropServices;

[AttributeUsage(AttributeTargets.Interface, Inherited = false)]
public sealed class TypeLibImportClassAttribute : Attribute
{
	public string Value { get; }

	public TypeLibImportClassAttribute(Type importClass)
	{
		Value = importClass.ToString();
	}
}
