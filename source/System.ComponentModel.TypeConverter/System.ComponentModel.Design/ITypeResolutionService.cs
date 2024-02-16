using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace System.ComponentModel.Design;

public interface ITypeResolutionService
{
	Assembly? GetAssembly(AssemblyName name);

	Assembly? GetAssembly(AssemblyName name, bool throwOnError);

	[return: DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]
	Type? GetType([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)] string name);

	[return: DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]
	Type? GetType([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)] string name, bool throwOnError);

	[return: DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]
	Type? GetType([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)] string name, bool throwOnError, bool ignoreCase);

	void ReferenceAssembly(AssemblyName name);

	string? GetPathOfAssembly(AssemblyName name);
}
