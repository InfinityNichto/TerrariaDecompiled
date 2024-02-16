namespace System.Reflection.Metadata;

public readonly struct LocalConstant
{
	private readonly MetadataReader _reader;

	private readonly int _rowId;

	private LocalConstantHandle Handle => LocalConstantHandle.FromRowId(_rowId);

	public StringHandle Name => _reader.LocalConstantTable.GetName(Handle);

	public BlobHandle Signature => _reader.LocalConstantTable.GetSignature(Handle);

	internal LocalConstant(MetadataReader reader, LocalConstantHandle handle)
	{
		_reader = reader;
		_rowId = handle.RowId;
	}
}
