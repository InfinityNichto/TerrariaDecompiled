namespace System.Reflection.Metadata;

public readonly struct ModuleReference
{
	private readonly MetadataReader _reader;

	private readonly int _rowId;

	private ModuleReferenceHandle Handle => ModuleReferenceHandle.FromRowId(_rowId);

	public StringHandle Name => _reader.ModuleRefTable.GetName(Handle);

	internal ModuleReference(MetadataReader reader, ModuleReferenceHandle handle)
	{
		_reader = reader;
		_rowId = handle.RowId;
	}

	public CustomAttributeHandleCollection GetCustomAttributes()
	{
		return new CustomAttributeHandleCollection(_reader, Handle);
	}
}
