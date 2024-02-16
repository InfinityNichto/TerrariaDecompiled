namespace System.Reflection.Metadata;

public readonly struct ImportDefinition
{
	private readonly Handle _typeOrNamespace;

	public ImportDefinitionKind Kind { get; }

	public BlobHandle Alias { get; }

	public AssemblyReferenceHandle TargetAssembly { get; }

	public BlobHandle TargetNamespace => (BlobHandle)_typeOrNamespace;

	public EntityHandle TargetType => (EntityHandle)_typeOrNamespace;

	internal ImportDefinition(ImportDefinitionKind kind, BlobHandle alias = default(BlobHandle), AssemblyReferenceHandle assembly = default(AssemblyReferenceHandle), Handle typeOrNamespace = default(Handle))
	{
		Kind = kind;
		Alias = alias;
		TargetAssembly = assembly;
		_typeOrNamespace = typeOrNamespace;
	}
}
