namespace System.Runtime.Serialization;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, Inherited = true, AllowMultiple = true)]
public sealed class KnownTypeAttribute : Attribute
{
	public string? MethodName { get; }

	public Type? Type { get; }

	public KnownTypeAttribute(Type type)
	{
		Type = type;
	}

	public KnownTypeAttribute(string methodName)
	{
		MethodName = methodName;
	}
}
