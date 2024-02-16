namespace System.Reflection.Metadata;

public readonly struct AssemblyFile
{
	private readonly MetadataReader _reader;

	private readonly int _rowId;

	private AssemblyFileHandle Handle => AssemblyFileHandle.FromRowId(_rowId);

	public bool ContainsMetadata => _reader.FileTable.GetFlags(Handle) == 0;

	public StringHandle Name => _reader.FileTable.GetName(Handle);

	public BlobHandle HashValue => _reader.FileTable.GetHashValue(Handle);

	internal AssemblyFile(MetadataReader reader, AssemblyFileHandle handle)
	{
		_reader = reader;
		_rowId = handle.RowId;
	}

	public CustomAttributeHandleCollection GetCustomAttributes()
	{
		return new CustomAttributeHandleCollection(_reader, Handle);
	}
}
