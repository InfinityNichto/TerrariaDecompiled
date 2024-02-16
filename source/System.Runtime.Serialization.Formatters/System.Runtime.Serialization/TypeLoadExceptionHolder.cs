namespace System.Runtime.Serialization;

public sealed class TypeLoadExceptionHolder
{
	internal string? TypeName { get; }

	internal TypeLoadExceptionHolder(string typeName)
	{
		TypeName = typeName;
	}
}
