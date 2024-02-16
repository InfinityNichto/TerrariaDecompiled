namespace System.Reflection.Metadata;

public readonly struct ManifestResource
{
	private readonly MetadataReader _reader;

	private readonly int _rowId;

	private ManifestResourceHandle Handle => ManifestResourceHandle.FromRowId(_rowId);

	public long Offset => _reader.ManifestResourceTable.GetOffset(Handle);

	public ManifestResourceAttributes Attributes => _reader.ManifestResourceTable.GetFlags(Handle);

	public StringHandle Name => _reader.ManifestResourceTable.GetName(Handle);

	public EntityHandle Implementation => _reader.ManifestResourceTable.GetImplementation(Handle);

	internal ManifestResource(MetadataReader reader, ManifestResourceHandle handle)
	{
		_reader = reader;
		_rowId = handle.RowId;
	}

	public CustomAttributeHandleCollection GetCustomAttributes()
	{
		return new CustomAttributeHandleCollection(_reader, Handle);
	}
}
