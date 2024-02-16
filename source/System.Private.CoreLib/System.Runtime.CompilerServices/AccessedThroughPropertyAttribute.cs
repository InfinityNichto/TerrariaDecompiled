namespace System.Runtime.CompilerServices;

[AttributeUsage(AttributeTargets.Field)]
public sealed class AccessedThroughPropertyAttribute : Attribute
{
	public string PropertyName { get; }

	public AccessedThroughPropertyAttribute(string propertyName)
	{
		PropertyName = propertyName;
	}
}
