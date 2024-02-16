using System.Collections.Immutable;
using System.Reflection.Metadata.Ecma335;

namespace System.Reflection.Metadata;

public struct NamespaceDefinition
{
	private readonly NamespaceData _data;

	public StringHandle Name => _data.Name;

	public NamespaceDefinitionHandle Parent => _data.Parent;

	public ImmutableArray<NamespaceDefinitionHandle> NamespaceDefinitions => _data.NamespaceDefinitions;

	public ImmutableArray<TypeDefinitionHandle> TypeDefinitions => _data.TypeDefinitions;

	public ImmutableArray<ExportedTypeHandle> ExportedTypes => _data.ExportedTypes;

	internal NamespaceDefinition(NamespaceData data)
	{
		_data = data;
	}
}
