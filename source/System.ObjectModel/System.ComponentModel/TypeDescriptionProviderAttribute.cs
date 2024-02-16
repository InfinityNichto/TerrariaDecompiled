using System.Diagnostics.CodeAnalysis;

namespace System.ComponentModel;

[AttributeUsage(AttributeTargets.Class, Inherited = true)]
public sealed class TypeDescriptionProviderAttribute : Attribute
{
	[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]
	public string TypeName { get; }

	public TypeDescriptionProviderAttribute([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)] string typeName)
	{
		if (typeName == null)
		{
			throw new ArgumentNullException("typeName");
		}
		TypeName = typeName;
	}

	public TypeDescriptionProviderAttribute([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)] Type type)
	{
		if (type == null)
		{
			throw new ArgumentNullException("type");
		}
		TypeName = type.AssemblyQualifiedName;
	}
}
