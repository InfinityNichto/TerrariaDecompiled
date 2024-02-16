using System.Collections.Immutable;

namespace System.Reflection.Metadata.Ecma335;

internal sealed class NamespaceData
{
	public readonly StringHandle Name;

	public readonly string FullName;

	public readonly NamespaceDefinitionHandle Parent;

	public readonly ImmutableArray<NamespaceDefinitionHandle> NamespaceDefinitions;

	public readonly ImmutableArray<TypeDefinitionHandle> TypeDefinitions;

	public readonly ImmutableArray<ExportedTypeHandle> ExportedTypes;

	public NamespaceData(StringHandle name, string fullName, NamespaceDefinitionHandle parent, ImmutableArray<NamespaceDefinitionHandle> namespaceDefinitions, ImmutableArray<TypeDefinitionHandle> typeDefinitions, ImmutableArray<ExportedTypeHandle> exportedTypes)
	{
		Name = name;
		FullName = fullName;
		Parent = parent;
		NamespaceDefinitions = namespaceDefinitions;
		TypeDefinitions = typeDefinitions;
		ExportedTypes = exportedTypes;
	}
}
