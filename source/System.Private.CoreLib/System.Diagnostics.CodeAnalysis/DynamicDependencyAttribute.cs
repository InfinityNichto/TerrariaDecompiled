namespace System.Diagnostics.CodeAnalysis;

[AttributeUsage(AttributeTargets.Constructor | AttributeTargets.Method | AttributeTargets.Field, AllowMultiple = true, Inherited = false)]
public sealed class DynamicDependencyAttribute : Attribute
{
	public string? MemberSignature { get; }

	public DynamicallyAccessedMemberTypes MemberTypes { get; }

	public Type? Type { get; }

	public string? TypeName { get; }

	public string? AssemblyName { get; }

	public string? Condition { get; set; }

	public DynamicDependencyAttribute(string memberSignature)
	{
		MemberSignature = memberSignature;
	}

	public DynamicDependencyAttribute(string memberSignature, Type type)
	{
		MemberSignature = memberSignature;
		Type = type;
	}

	public DynamicDependencyAttribute(string memberSignature, string typeName, string assemblyName)
	{
		MemberSignature = memberSignature;
		TypeName = typeName;
		AssemblyName = assemblyName;
	}

	public DynamicDependencyAttribute(DynamicallyAccessedMemberTypes memberTypes, Type type)
	{
		MemberTypes = memberTypes;
		Type = type;
	}

	public DynamicDependencyAttribute(DynamicallyAccessedMemberTypes memberTypes, string typeName, string assemblyName)
	{
		MemberTypes = memberTypes;
		TypeName = typeName;
		AssemblyName = assemblyName;
	}
}
