namespace System.Reflection.Metadata;

public readonly struct ExportedType
{
	internal readonly MetadataReader reader;

	internal readonly int rowId;

	private ExportedTypeHandle Handle => ExportedTypeHandle.FromRowId(rowId);

	public TypeAttributes Attributes => reader.ExportedTypeTable.GetFlags(rowId);

	public bool IsForwarder
	{
		get
		{
			if (Attributes.IsForwarder())
			{
				return Implementation.Kind == HandleKind.AssemblyReference;
			}
			return false;
		}
	}

	public StringHandle Name => reader.ExportedTypeTable.GetTypeName(rowId);

	public StringHandle Namespace => reader.ExportedTypeTable.GetTypeNamespaceString(rowId);

	public NamespaceDefinitionHandle NamespaceDefinition => reader.ExportedTypeTable.GetTypeNamespace(rowId);

	public EntityHandle Implementation => reader.ExportedTypeTable.GetImplementation(rowId);

	internal ExportedType(MetadataReader reader, int rowId)
	{
		this.reader = reader;
		this.rowId = rowId;
	}

	public CustomAttributeHandleCollection GetCustomAttributes()
	{
		return new CustomAttributeHandleCollection(reader, Handle);
	}
}
